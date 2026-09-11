namespace BioPipeline.Api.Services;

/// <summary>
/// Bound from the "Gemini" section of config. Only Model lives in
/// appsettings.json (not a secret). ApiKey is deliberately never set there --
/// see backend/README.md for how it's configured via `dotnet user-secrets`
/// (the .NET equivalent of the frontend's .env file: kept outside the repo,
/// never committed to git).
/// </summary>
public class GeminiOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gemini-3.6-flash";
}
