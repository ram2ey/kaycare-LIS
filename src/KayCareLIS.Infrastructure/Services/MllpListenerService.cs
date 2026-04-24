using System.Net;
using System.Net.Sockets;
using System.Text;
using KayCareLIS.Core.Constants;
using KayCareLIS.Core.Entities;
using KayCareLIS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KayCareLIS.Infrastructure.Services;

/// <summary>
/// TCP MLLP listener on port 2575. Accepts HL7 v2.x ORU^R01 messages from lab instruments.
/// Each message is saved as a LabResult with observations, matched to an open LabOrderItem by accession.
/// </summary>
public class MllpListenerService : BackgroundService
{
    private const byte StartBlock = 0x0B;
    private const byte EndBlock   = 0x1C;
    private const byte CarriageReturn = 0x0D;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<MllpListenerService> _logger;
    private readonly int _port;

    public MllpListenerService(IServiceScopeFactory scopeFactory, ILogger<MllpListenerService> logger, int port = 2575)
    {
        _scopeFactory = scopeFactory;
        _logger       = logger;
        _port         = port;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var listener = new TcpListener(IPAddress.Any, _port);
        listener.Start();
        _logger.LogInformation("MLLP listener started on port {Port}", _port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                TcpClient client;
                try
                {
                    client = await listener.AcceptTcpClientAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                _ = HandleClientAsync(client, stoppingToken);
            }
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using var stream = client.GetStream();
        try
        {
            var message = await ReadMllpMessageAsync(stream, ct);
            if (message == null) return;

            await ProcessMessageAsync(message, ct);
            await SendAckAsync(stream, "AA", ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MLLP message");
            try { await SendAckAsync(stream, "AE", ct); } catch { /* best effort */ }
        }
        finally
        {
            client.Dispose();
        }
    }

    private static async Task<string?> ReadMllpMessageAsync(NetworkStream stream, CancellationToken ct)
    {
        var buffer = new List<byte>(4096);
        var singleByte = new byte[1];
        bool started = false;

        while (true)
        {
            var read = await stream.ReadAsync(singleByte, ct);
            if (read == 0) return null;

            if (!started)
            {
                if (singleByte[0] == StartBlock) started = true;
                continue;
            }

            if (singleByte[0] == EndBlock)
            {
                // read trailing CR
                await stream.ReadAsync(singleByte, ct);
                break;
            }

            buffer.Add(singleByte[0]);
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private async Task ProcessMessageAsync(string rawMessage, CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Determine tenantId from ORC/PID — for simplicity use first active tenant
        // In production, segment MSH-5 / MSH-6 would carry the tenant identifier
        var tenant = await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.IsActive, ct);
        if (tenant == null) return;

        var parsed = Hl7Parser.ParseOruR01(rawMessage, tenant.TenantId);
        if (parsed == null)
        {
            _logger.LogWarning("Could not parse HL7 message — missing accession number");
            return;
        }

        // Try to match to an open LabOrderItem by AccessionNumber
        var orderItem = await db.LabOrderItems
            .FirstOrDefaultAsync(i => i.AccessionNumber == parsed.AccessionNumber
                && i.TenantId == tenant.TenantId, ct);

        Guid? patientId          = null;
        Guid? orderingDoctorId   = null;

        if (orderItem != null)
        {
            var order = await db.LabOrders.AsNoTracking()
                .FirstOrDefaultAsync(o => o.LabOrderId == orderItem.LabOrderId, ct);
            if (order != null)
            {
                patientId        = order.PatientId;
                orderingDoctorId = order.OrderingDoctorUserId;
            }
        }

        // If we still have no patient, try MRN lookup from PID segment
        if (patientId == null && !string.IsNullOrEmpty(parsed.Hl7PatientId))
        {
            var patient = await db.Patients.AsNoTracking()
                .FirstOrDefaultAsync(p => p.MedicalRecordNumber == parsed.Hl7PatientId
                    && p.TenantId == tenant.TenantId, ct);
            patientId = patient?.PatientId;
        }

        if (patientId == null)
        {
            _logger.LogWarning("HL7 message accession {Acc} — could not resolve patient", parsed.AccessionNumber);
            return;
        }

        // Check for duplicate accession
        var existing = await db.LabResults
            .FirstOrDefaultAsync(r => r.AccessionNumber == parsed.AccessionNumber
                && r.TenantId == tenant.TenantId, ct);

        if (existing != null)
        {
            _logger.LogInformation("Duplicate HL7 message for accession {Acc} — ignoring", parsed.AccessionNumber);
            return;
        }

        var result = new LabResult
        {
            LabResultId          = Guid.NewGuid(),
            PatientId            = patientId.Value,
            OrderingDoctorUserId = orderingDoctorId,
            AccessionNumber      = parsed.AccessionNumber,
            OrderCode            = parsed.OrderCode,
            OrderName            = parsed.OrderName,
            OrderedAt            = parsed.OrderedAt,
            ReceivedAt           = DateTime.UtcNow,
            Status               = LabResultStatus.Received,
            RawHl7               = parsed.RawMessage,
            LabOrderItemId       = orderItem?.LabOrderItemId,
        };
        db.LabResults.Add(result);
        await db.SaveChangesAsync(ct);

        foreach (var obs in parsed.Observations)
        {
            obs.LabResultId = result.LabResultId;
            db.LabObservations.Add(obs);
        }
        await db.SaveChangesAsync(ct);

        // Update linked LabOrderItem status
        if (orderItem != null)
        {
            orderItem.LabResultId = result.LabResultId;
            orderItem.Status      = LabOrderItemStatus.Resulted;
            orderItem.ResultedAt  = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        _logger.LogInformation("HL7 result saved: accession {Acc}, {Count} observations", parsed.AccessionNumber, parsed.Observations.Count);
    }

    private static async Task SendAckAsync(NetworkStream stream, string ackCode, CancellationToken ct)
    {
        var ts  = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
        var ack = $"MSH|^~\\&|LIS|||{ts}||ACK|{Guid.NewGuid():N}|P|2.5\rMSA|{ackCode}|\r";
        var ackBytes = Encoding.UTF8.GetBytes(ack);
        var mllp = new byte[ackBytes.Length + 3];
        mllp[0] = StartBlock;
        ackBytes.CopyTo(mllp, 1);
        mllp[^2] = EndBlock;
        mllp[^1] = CarriageReturn;
        await stream.WriteAsync(mllp, ct);
    }
}
