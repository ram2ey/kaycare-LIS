using KayCareLIS.Core.Entities;

namespace KayCareLIS.Infrastructure.Services;

/// <summary>
/// Parses HL7 v2.x ORU^R01 messages into LabResult + LabObservation records.
/// </summary>
public static class Hl7Parser
{
    public static ParsedHl7Result? ParseOruR01(string rawMessage, Guid tenantId)
    {
        if (string.IsNullOrWhiteSpace(rawMessage)) return null;

        var lines = rawMessage
            .Replace("\r\n", "\r").Replace("\n", "\r")
            .Split('\r', StringSplitOptions.RemoveEmptyEntries);

        string? patientId   = null;
        string? orderCode   = null;
        string? orderName   = null;
        string? accession   = null;
        string? doctorId    = null;
        DateTime? orderedAt = null;

        var observations = new List<LabObservation>();

        foreach (var line in lines)
        {
            var fields = line.Split('|');
            var segId  = fields[0];

            switch (segId)
            {
                case "PID":
                    patientId = GetField(fields, 3)?.Split('^')[0];
                    break;

                case "OBR":
                    accession = GetField(fields, 3)?.Split('^')[0];
                    var codes = GetField(fields, 4)?.Split('^');
                    orderCode  = codes?.ElementAtOrDefault(0);
                    orderName  = codes?.ElementAtOrDefault(1);
                    doctorId   = GetField(fields, 16)?.Split('^')[0];
                    var dateStr = GetField(fields, 7);
                    if (dateStr != null && dateStr.Length >= 8)
                        orderedAt = ParseHl7DateTime(dateStr);
                    break;

                case "OBX":
                    var obxFields    = fields;
                    var seq          = int.TryParse(GetField(obxFields, 1), out var s) ? s : observations.Count + 1;
                    var testCodePart = GetField(obxFields, 3)?.Split('^');
                    var testCode     = testCodePart?.ElementAtOrDefault(0) ?? string.Empty;
                    var testName     = testCodePart?.ElementAtOrDefault(1) ?? string.Empty;
                    var value        = GetField(obxFields, 5);
                    var units        = GetField(obxFields, 6)?.Split('^')[0];
                    var refRange     = GetField(obxFields, 7);
                    var flag         = GetField(obxFields, 8);

                    observations.Add(new LabObservation
                    {
                        LabObservationId = Guid.NewGuid(),
                        TenantId         = tenantId,
                        SequenceNumber   = seq,
                        TestCode         = testCode,
                        TestName         = testName,
                        Value            = value,
                        Units            = units,
                        ReferenceRange   = refRange,
                        AbnormalFlag     = string.IsNullOrEmpty(flag) ? null : flag,
                    });
                    break;
            }
        }

        if (string.IsNullOrEmpty(accession)) return null;

        return new ParsedHl7Result
        {
            AccessionNumber  = accession,
            Hl7PatientId     = patientId,
            OrderCode        = orderCode,
            OrderName        = orderName,
            Hl7DoctorId      = doctorId,
            OrderedAt        = orderedAt,
            RawMessage       = rawMessage,
            Observations     = observations,
        };
    }

    private static string? GetField(string[] fields, int index)
    {
        if (index >= fields.Length) return null;
        var v = fields[index];
        return string.IsNullOrEmpty(v) || v == "\"\"" ? null : v;
    }

    private static DateTime? ParseHl7DateTime(string s)
    {
        var fmt = s.Length >= 14 ? "yyyyMMddHHmmss" :
                  s.Length >= 12 ? "yyyyMMddHHmm"   : "yyyyMMdd";
        if (DateTime.TryParseExact(s[..Math.Min(s.Length, fmt.Length)], fmt,
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var dt))
            return dt;
        return null;
    }
}

public class ParsedHl7Result
{
    public string AccessionNumber { get; set; } = string.Empty;
    public string? Hl7PatientId   { get; set; }
    public string? OrderCode      { get; set; }
    public string? OrderName      { get; set; }
    public string? Hl7DoctorId    { get; set; }
    public DateTime? OrderedAt    { get; set; }
    public string  RawMessage     { get; set; } = string.Empty;
    public List<LabObservation> Observations { get; set; } = [];
}
