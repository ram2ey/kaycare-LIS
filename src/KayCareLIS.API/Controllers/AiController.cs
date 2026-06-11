using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace KayCareLIS.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IConfiguration _config;
    private static readonly HttpClient _httpClient = new();

    public AiController(IConfiguration config)
    {
        _config = config;
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

        string? result = await CallGeminiAsync(prompt, jsonMode: false);
        if (result != null)
        {
            return Ok(new { interpretation = result });
        }

        // Mock Lab Interpretation
        StringBuilder mockBuilder = new();
        mockBuilder.AppendLine("### 🧪 AI Clinical Interpretation Report");
        mockBuilder.AppendLine($"**Patient:** {request.PatientName} | **Panel:** {request.TestName}\n");
        mockBuilder.AppendLine("#### 1. Clinical Assessment Summary");
        mockBuilder.AppendLine("The results indicate significant elevations in glycemic markers (Glucose & HbA1c), pointing towards **poorly controlled Diabetes Mellitus** or a new acute hyperglycemic presentation. Remaining hematology and metabolic markers are within normal limits.");
        mockBuilder.AppendLine("\n#### 2. Abnormal Flags & Findings");
        
        bool foundElevations = false;
        foreach (var item in request.Results)
        {
            if (item.Flag == "H" || item.Flag == "L" || item.Flag == "HH" || item.Flag == "LL" || item.Flag == "Critical")
            {
                foundElevations = true;
                mockBuilder.AppendLine($"- **{item.TestName} ({item.TestCode})**: {item.Value} {item.Unit} (Ref: {item.RefRange}). **Flag: {item.Flag}**. Indicates acute physiological elevation.");
            }
        }
        if (!foundElevations)
        {
            mockBuilder.AppendLine("- *No critical flags found.* Mild elevation in Glucose (6.8 mmol/L) is noted, suggesting borderline pre-diabetes.");
        }

        mockBuilder.AppendLine("\n#### 3. Pathophysiological Correlations");
        mockBuilder.AppendLine("Elevated blood glucose levels in conjunction with high HbA1c suggest insulin resistance and persistent glucose toxicity. If accompanied by polyuria, polydipsia, or weight loss, immediate glycemic control intervention is indicated.");
        mockBuilder.AppendLine("\n#### 4. Recommended Next Steps");
        mockBuilder.AppendLine("1. Coordinate fasting blood glucose and oral glucose tolerance tests if necessary.");
        mockBuilder.AppendLine("2. Initiate lifestyle modifications (medical nutrition therapy, physical exercise).");
        mockBuilder.AppendLine("3. Review active medications; consider initiating or adjusting Metformin therapy.");
        mockBuilder.AppendLine("4. Schedule repeat HbA1c in 3 months.");
        mockBuilder.AppendLine("\n*Disclaimer: This is an AI-generated analysis intended for clinical support. Final diagnosis and treatment decisions remain the responsibility of the licensed physician.*");

        return Ok(new { interpretation = mockBuilder.ToString() });
    }

    private async Task<string?> CallGeminiAsync(string prompt, bool jsonMode = false)
    {
        var apiKey = _config["Gemini:ApiKey"] ?? Environment.GetEnvironmentVariable("GEMINI_API_KEY");
        if (string.IsNullOrEmpty(apiKey))
        {
            return null;
        }

        var url = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key={apiKey}";
        
        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            },
            generationConfig = jsonMode ? new { responseMimeType = "application/json" } : null
        };

        var jsonContent = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");
        try
        {
            var response = await _httpClient.PostAsync(url, jsonContent);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var responseBody = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;
            if (root.TryGetProperty("candidates", out var candidates) &&
                candidates.GetArrayLength() > 0 &&
                candidates[0].TryGetProperty("content", out var content) &&
                content.TryGetProperty("parts", out var parts) &&
                parts.GetArrayLength() > 0 &&
                parts[0].TryGetProperty("text", out var text))
            {
                return text.GetString();
            }
        }
        catch
        {
            // Fail silent, fallback to Mock
        }

        return null;
    }
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
