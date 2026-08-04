using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using KayCareLIS.Infrastructure.Data;
using KayCareLIS.Core.Interfaces;

namespace KayCareLIS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;
    private readonly IBlobStorageService _blob;
    private static readonly HttpClient _httpClient = new();

    private static readonly string[] FreeModelsFallback = new[]
    {
        "meta-llama/llama-3.3-70b-instruct:free",
        "google/gemini-2.0-flash-lite-preview:free",
        "deepseek/deepseek-r1:free",
        "qwen/qwen-2.5-coder-32b-instruct:free"
    };

    private static readonly string[] FreeVisionModelsFallback = new[]
    {
        "google/gemini-2.0-flash-lite-preview:free",
        "meta-llama/llama-3.2-11b-vision-instruct:free"
    };

    public AiController(IConfiguration config, AppDbContext db, IBlobStorageService blob)
    {
        _config = config;
        _db = db;
        _blob = blob;
    }

    [HttpPost("lab-interpreter")]
    public async Task<IActionResult> LabInterpreter([FromBody] LabInterpreterRequest request)
    {
        string resultsJson = JsonSerializer.Serialize(request.Results);
        string prompt = $@"You are an expert Clinical Pathologist. Review the following laboratory results for this patient and provide a concise, structured interpretation for the requesting doctor.
Patient Name: {request.PatientName}
Test Panel: {request.TestName}

Results:
{resultsJson}

Format your response in Markdown with these sections:
1. **Clinical Assessment Summary**: Overview of the findings (e.g. anemia, hyperglycemia).
2. **Abnormal / Critical Flags**: Detail each abnormal value, what it indicates, and severity.
3. **Pathophysiological Correlations**: What potential underlying conditions explain these values.
4. **Recommended Next Steps**: Recommended follow-up tests, monitoring, or clinical interventions.
5. **Medical Disclaimer**: Standard AI clinical guidance disclaimer.

Be concise, technical, and professional.";

        string? result = await CallOpenRouterAsync(prompt, jsonMode: false);
        if (result != null)
        {
            return Ok(new { interpretation = result });
        }

        return StatusCode(503, new { message = "AI Assistant is temporarily unavailable.", error = "AI Assistant is temporarily unavailable." });
    }

    [HttpPost("radiology-drafter/{itemId:guid}")]
    public async Task<IActionResult> RadiologyDrafter(Guid itemId, CancellationToken ct)
    {
        var item = await _db.RadiologyOrderItems
            .Include(i => i.RadiologyOrder)
                .ThenInclude(o => o.Patient)
            .FirstOrDefaultAsync(i => i.RadiologyOrderItemId == itemId, ct);

        if (item == null)
            return NotFound(new { message = "Radiology item not found." });

        if (string.IsNullOrEmpty(item.PacsViewerUrl))
            return BadRequest(new { message = "No scan image uploaded for this item." });

        byte[]? scanBytes = null;
        string key = item.PacsViewerUrl;
        if (key.StartsWith("pacs-studies/"))
        {
            scanBytes = await _blob.DownloadAsync("pacs-studies", Path.GetFileName(key), ct);
        }
        else if (key.Contains("/pacs-studies/"))
        {
            var filename = Path.GetFileName(key);
            scanBytes = await _blob.DownloadAsync("pacs-studies", filename, ct);
        }

        if (scanBytes == null || scanBytes.Length == 0)
        {
            return StatusCode(503, new { message = "AI Assistant is temporarily unavailable.", error = "AI Assistant is temporarily unavailable." });
        }

        string patientName = $"{item.RadiologyOrder.Patient.FirstName} {item.RadiologyOrder.Patient.LastName}";
        string prompt = $@"You are an expert Clinical Radiologist. Review this medical scan image ({item.Modality} of {item.BodyPart}) for patient {patientName} (Procedure: {item.ProcedureName}).
Provide a structured draft radiology report. Return a JSON object with fields:
- findings: Detailed objective observations.
- impression: Radiologist clinical conclusion.
- recommendations: Suggested next steps or follow-ups.

Format value fields as standard plain text strings. Return ONLY a valid JSON object, no surrounding markdown backticks or explanation text.";

        string? result = await CallOpenRouterMultimodalAsync(prompt, scanBytes, "image/png");
        if (result != null)
        {
            try
            {
                var cleaned = result.Trim();
                if (cleaned.StartsWith("```"))
                {
                    cleaned = cleaned.Substring(cleaned.IndexOf('\n')).Trim();
                    if (cleaned.EndsWith("```"))
                        cleaned = cleaned.Substring(0, cleaned.Length - 3).Trim();
                }
                var draft = JsonSerializer.Deserialize<RadiologyDraftResponse>(cleaned, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (draft != null)
                    return Ok(draft);
            }
            catch
            {
                // Fall through to 503
            }
        }

        return StatusCode(503, new { message = "AI Assistant is temporarily unavailable.", error = "AI Assistant is temporarily unavailable." });
    }

    [HttpGet("icd10-finder")]
    public async Task<IActionResult> FindIcd10Codes([FromQuery] string query, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(query)) return BadRequest(new { message = "Query cannot be empty." });

        string prompt = $@"You are a clinical coding specialist. Analyze this natural language diagnostic text and suggest the top 3 best matching ICD-10 billing codes.
Query: {query}

Return a JSON array of objects with fields:
- code: The ICD-10 code (e.g. I20.9)
- description: The official ICD-10 description
- matchConfidence: High, Medium, or Low

Return ONLY the raw JSON array. Do not wrap in markdown backticks or other text.";

        string? result = await CallOpenRouterAsync(prompt, jsonMode: true);
        if (result != null)
        {
            try
            {
                var cleaned = result.Trim();
                if (cleaned.StartsWith("```"))
                {
                    cleaned = cleaned.Substring(cleaned.IndexOf('\n')).Trim();
                    if (cleaned.EndsWith("```"))
                        cleaned = cleaned.Substring(0, cleaned.Length - 3).Trim();
                }
                var codes = JsonSerializer.Deserialize<List<Icd10Recommendation>>(cleaned, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (codes != null)
                    return Ok(codes);
            }
            catch
            {
                // Fall through to 503
            }
        }

        return StatusCode(503, new { message = "AI Assistant is temporarily unavailable.", error = "AI Assistant is temporarily unavailable." });
    }

    [HttpPost("patient-summary")]
    public async Task<IActionResult> GetPatientSummary([FromBody] PatientSummaryRequest request)
    {
        if (string.IsNullOrEmpty(request.ReportText))
            return BadRequest(new { message = "Report text cannot be empty." });

        string prompt = $@"You are a compassionate, patient-friendly medical communicator. Translate the following highly technical clinical report into clear, empathetic language that a patient without medical training can easily understand.
Report:
{request.ReportText}

Explain:
1. What the findings mean in simple terms.
2. If there are abnormal signs, explain them gently without causing undue panic.
3. What the recommended next steps are.

Avoid all medical jargon where possible, or define it immediately. Be brief and supportive.";

        string? result = await CallOpenRouterAsync(prompt, jsonMode: false);
        if (result != null)
        {
            return Ok(new { summary = result });
        }

        return StatusCode(503, new { message = "AI Assistant is temporarily unavailable.", error = "AI Assistant is temporarily unavailable." });
    }

    [HttpPost("hl7-repair")]
    public async Task<IActionResult> RepairHl7([FromBody] Hl7RepairRequest request)
    {
        if (string.IsNullOrEmpty(request.RawHl7))
            return BadRequest(new { message = "Raw HL7 message cannot be empty." });

        string prompt = $@"You are a clinical systems integration expert. The following raw HL7 v2.x message failed parsing due to formatting errors (e.g., mismatching delimiters, missing segment headers, or unescaped characters).
Raw HL7:
{request.RawHl7}

Analyze the message, find the syntax/structural issues, and return a JSON object with fields:
- explanation: Short description of the errors found.
- repairedPayload: The corrected raw HL7 payload with delimiters intact.

Return ONLY a JSON object, no other text or backticks.";

        string? result = await CallOpenRouterAsync(prompt, jsonMode: true);
        if (result != null)
        {
            try
            {
                var cleaned = result.Trim();
                if (cleaned.StartsWith("```"))
                {
                    cleaned = cleaned.Substring(cleaned.IndexOf('\n')).Trim();
                    if (cleaned.EndsWith("```"))
                        cleaned = cleaned.Substring(0, cleaned.Length - 3).Trim();
                }
                var repair = JsonSerializer.Deserialize<Hl7RepairResponse>(cleaned, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (repair != null)
                    return Ok(repair);
            }
            catch
            {
                // Fall through to 503
            }
        }

        return StatusCode(503, new { message = "AI Assistant is temporarily unavailable.", error = "AI Assistant is temporarily unavailable." });
    }

    private async Task<string?> CallOpenRouterAsync(string prompt, bool jsonMode = false)
    {
        var apiKey = _config["OpenRouter:ApiKey"]
                  ?? _config["OPENROUTER_API_KEY"]
                  ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
                  ?? _config["Gemini:ApiKey"]
                  ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrEmpty(apiKey)) return null;

        var customModel = _config["OpenRouter:Model"]
                       ?? _config["OPENROUTER_MODEL"]
                       ?? Environment.GetEnvironmentVariable("OPENROUTER_MODEL");

        var modelsToUse = !string.IsNullOrWhiteSpace(customModel) && customModel != "openrouter/auto"
            ? new[] { customModel }
            : FreeModelsFallback;

        object requestBody;
        if (jsonMode)
        {
            requestBody = new
            {
                models = modelsToUse,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                response_format = new { type = "json_object" }
            };
        }
        else
        {
            requestBody = new
            {
                models = modelsToUse,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };
        }

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
            req.Headers.Add("Authorization", $"Bearer {apiKey}");
            req.Headers.Add("HTTP-Referer", "https://kaycare.com");
            req.Headers.Add("X-Title", "KayCare LIS");
            req.Content = jsonContent;

            var response = await _httpClient.SendAsync(req);
            if (!response.IsSuccessStatusCode) return null;

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                return content.GetString();
            }
        }
        catch
        {
            // Fail silent
        }

        return null;
    }

    private async Task<string?> CallOpenRouterMultimodalAsync(string prompt, byte[] imageBytes, string mimeType)
    {
        var apiKey = _config["OpenRouter:ApiKey"]
                  ?? _config["OPENROUTER_API_KEY"]
                  ?? Environment.GetEnvironmentVariable("OPENROUTER_API_KEY")
                  ?? _config["Gemini:ApiKey"]
                  ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");

        if (string.IsNullOrEmpty(apiKey)) return null;

        var base64Data = Convert.ToBase64String(imageBytes);
        var imageUrl = $"data:{mimeType};base64,{base64Data}";

        var customModel = _config["OpenRouter:Model"]
                       ?? _config["OPENROUTER_MODEL"]
                       ?? Environment.GetEnvironmentVariable("OPENROUTER_MODEL");

        var modelsToUse = !string.IsNullOrWhiteSpace(customModel) && customModel != "openrouter/auto"
            ? new[] { customModel }
            : FreeVisionModelsFallback;

        var requestBody = new
        {
            models = modelsToUse,
            messages = new[]
            {
                new
                {
                    role = "user",
                    content = new object[]
                    {
                        new { type = "text", text = prompt },
                        new
                        {
                            type = "image_url",
                            image_url = new { url = imageUrl }
                        }
                    }
                }
            }
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, "https://openrouter.ai/api/v1/chat/completions");
            req.Headers.Add("Authorization", $"Bearer {apiKey}");
            req.Headers.Add("HTTP-Referer", "https://kaycare.com");
            req.Headers.Add("X-Title", "KayCare LIS");
            req.Content = jsonContent;

            var response = await _httpClient.SendAsync(req);
            if (!response.IsSuccessStatusCode) return null;

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (root.TryGetProperty("choices", out var choices) &&
                choices.GetArrayLength() > 0 &&
                choices[0].TryGetProperty("message", out var message) &&
                message.TryGetProperty("content", out var content))
            {
                return content.GetString();
            }
        }
        catch
        {
            // Fail silent
        }

        return null;
    }
}

public class RadiologyDraftResponse
{
    public string Findings { get; set; } = string.Empty;
    public string Impression { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;
}

public class Icd10Recommendation
{
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MatchConfidence { get; set; } = string.Empty;
}

public class PatientSummaryRequest
{
    public string ReportText { get; set; } = string.Empty;
}

public class Hl7RepairRequest
{
    public string RawHl7 { get; set; } = string.Empty;
}

public class Hl7RepairResponse
{
    public string Explanation { get; set; } = string.Empty;
    public string RepairedPayload { get; set; } = string.Empty;
}

public class LabInterpreterRequest
{
    public string PatientName { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public List<LabResultItem> Results { get; set; } = [];
}

public class LabResultItem
{
    public string TestCode { get; set; } = string.Empty;
    public string TestName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public string RefRange { get; set; } = string.Empty;
    public string Flag { get; set; } = string.Empty;
}
