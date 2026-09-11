namespace BioPipeline.Api.Models;

/// <summary>
/// One pipeline execution: an uploaded sample file plus everything the
/// background job records about running it through Nextflow.
/// </summary>
public class Run
{
    public int Id { get; set; }

    public string SampleFileName { get; set; } = "";
    public RunStatus Status { get; set; } = RunStatus.Queued;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }

    // Absolute (Windows-side) paths -- never sent to clients directly, only
    // used internally to invoke Nextflow and read its output back.
    public string InputFilePath { get; set; } = "";
    public string ResultsDir { get; set; } = "";

    // --- Quality Check summary (populated once the QC step completes) ---
    public int? QcTotalReads { get; set; }
    public double? QcMeanQuality { get; set; }
    public double? QcPassRatePercent { get; set; }
    public string? QcStatus { get; set; }

    // --- Variant calling summary (populated once the pipeline finishes) ---
    public int? TotalVariants { get; set; }
    public int? SnpCount { get; set; }
    public int? InsCount { get; set; }
    public int? DelCount { get; set; }

    // Stretch goal: cached plain-language explanation from the LLM endpoint.
    public string? AiExplanation { get; set; }

    public List<Variant> Variants { get; set; } = new();
}
