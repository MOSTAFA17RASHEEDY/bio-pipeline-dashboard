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

    /// <summary>Config for the real nf-core/sarek run mode (see sarek-reproduction/).</summary>
    public RealSarekOptions RealSarek { get; set; } = new();
}

public class RealSarekOptions
{
    /// <summary>nf-core/sarek revision to run (matches sarek-reproduction/README.md).</summary>
    public string Revision { get; set; } = "3.10.0";

    /// <summary>
    /// WSL-native path to the fixed, pre-sliced NA12878 chr20 inputs (samplesheet,
    /// reference, known-sites) built in sarek-reproduction/. Used as-is -- this
    /// path only ever exists inside WSL, never through WslPath.FromWindows.
    /// </summary>
    public string InputDataDir { get; set; } = "/root/na12878-subset";

    /// <summary>
    /// WSL-native root for per-run work/results dirs ("{WorkRootDir}/{runId}/...").
    /// Deliberately separate from sarek-reproduction's own proof-of-work dirs
    /// (/root/sarek-work-na12878) so this feature never touches that case study.
    /// </summary>
    public string WorkRootDir { get; set; } = "/root/sarek-work-live";

    /// <summary>Windows path to sarek-reproduction/laptop.config (resource caps).</summary>
    public string LaptopConfigPath { get; set; } = "";

    /// <summary>Windows path to sarek-reproduction/na12878_subset.config (igenomes_ignore/genome=null).</summary>
    public string SubsetConfigPath { get; set; } = "";
}
