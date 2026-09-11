namespace BioPipeline.Api.Services;

/// <summary>
/// Bound from the "Pipeline" section of appsettings.json. PipelineDir and
/// RunsStorageDir start as relative paths in config and get resolved to
/// absolute ones at startup (see Program.cs) so the rest of the app never
/// has to think about the working directory.
/// </summary>
public class PipelineOptions
{
    /// <summary>Absolute path to the pipeline/ directory (contains main.nf).</summary>
    public string PipelineDir { get; set; } = "";

    /// <summary>Name of the WSL distro Nextflow runs inside (see pipeline/README.md).</summary>
    public string WslDistro { get; set; } = "BioPipelineUbuntu";

    /// <summary>Where uploaded input files and per-run results/logs are stored.</summary>
    public string RunsStorageDir { get; set; } = "";
}
