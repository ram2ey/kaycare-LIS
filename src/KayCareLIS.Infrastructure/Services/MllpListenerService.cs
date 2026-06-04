using System.Net;
using System.Net.Sockets;
using System.Text;
using KayCareLIS.Core.Constants;
using KayCareLIS.Core.Entities;
using KayCareLIS.Core.Interfaces;
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
        var labResults = scope.ServiceProvider.GetRequiredService<ILabResultService>();

        var success = await labResults.ProcessHl7MessageAsync(rawMessage, ct);
        if (success)
        {
            _logger.LogInformation("HL7 result parsed and saved successfully via MLLP listener.");
        }
        else
        {
            _logger.LogWarning("HL7 message processing failed or duplicate accession received.");
        }
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
