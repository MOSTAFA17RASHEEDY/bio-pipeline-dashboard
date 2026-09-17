using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BioPipeline.Api.Models;
using Microsoft.Extensions.Options;

namespace BioPipeline.Api.Services;

/// <summary>
/// Sends a completed run's variant summary to the Gemini API and gets back a
/// short, plain-language explanation. This is the project's one and only
/// external network call -- everything else runs locally.
/// </summary>
public class GeminiExplanationService(
    HttpClient http,
    IOptions<GeminiOptions> options,
    ILogger<GeminiExplanationService> logger)
{
    private readonly GeminiOptions _options = options.Value;

    public async Task<string> ExplainAsync(Run run, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_options.ApiKey))
        {
            throw new InvalidOperationException(
                "Gemini API key is not configured. Run: dotnet user-secrets set \"Gemini:ApiKey\" \"<your key>\" " +
                "from the backend/ directory (see backend/README.md).");
        }

        var prompt = BuildPrompt(run);
        var url =
            $"https://generativelanguage.googleapis.com/v1beta/models/{_options.Model}:generateContent?key={_options.ApiKey}";

        var requestBody = new
        {
            contents = new[]
            {
                new { parts = new[] { new { text = prompt } } },
            },
        };

        using var response = await http.PostAsJsonAsync(url, requestBody, ct);
        var responseText = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("Gemini API returned {Status}: {Body}", response.StatusCode, responseText);
            throw new InvalidOperationException(
                $"Gemini API request failed ({(int)response.StatusCode} {response.StatusCode}). " +
                "Double-check the API key and model name in your configuration.");
        }

        using var doc = JsonDocument.Parse(responseText);
        var text = doc.RootElement
            .GetProperty("candidates")[0]
            .GetProperty("content")
            .GetProperty("parts")[0]
            .GetProperty("text")
            .GetString();

        return text?.Trim() ?? "(Gemini returned no explanation text.)";
    }

    private static string BuildPrompt(Run run)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "You are explaining bioinformatics pipeline results to someone with no biology or programming background.");
        sb.AppendLine(
            "Write a short, plain-language explanation (4-6 sentences) of what this DNA analysis run found.");
        sb.AppendLine(
            "Avoid jargon; if you must use a technical term, briefly explain it in plain words. " +
            "Only describe what is in the data below -- do not invent clinical significance or diagnoses.");
        sb.AppendLine();
        sb.AppendLine($"Sample file: {run.SampleFileName}");

        if (run.Kind == RunKind.RealSarek)
        {
            sb.AppendLine(
                "This run used nf-core/sarek (a real, published, peer-reviewed bioinformatics pipeline) on a " +
                "small public reference slice of NA12878 -- a well-known, de-identified reference human genome " +
                "used worldwide to validate DNA analysis software. This is real sequencing data, not synthetic.");
            sb.AppendLine(
                $"Total variants found: {run.TotalVariants} " +
                $"({run.SnpCount} single-letter changes, {run.InsCount} insertions, {run.DelCount} deletions).");
        }
        else
        {
            sb.AppendLine("This is synthetic demo data, not a real patient sample.");
            sb.AppendLine(
                $"Quality check: {run.QcTotalReads} reads, mean quality score {run.QcMeanQuality}, " +
                $"{run.QcPassRatePercent}% pass rate ({run.QcStatus}).");
            sb.AppendLine(
                $"Total variants found: {run.TotalVariants} " +
                $"({run.SnpCount} single-letter changes, {run.InsCount} insertions, {run.DelCount} deletions).");
        }
        sb.AppendLine();
        sb.AppendLine("Variant details (position, reference base(s) -> sample base(s), type, read support):");
        foreach (var v in run.Variants.OrderBy(v => v.Position))
        {
            sb.AppendLine($"- Position {v.Position}: {v.Ref} -> {v.Alt} ({v.Type}), " +
                           $"supported by {v.SupportingReads} of {v.Depth} reads covering that position");
        }

        return sb.ToString();
    }
}
