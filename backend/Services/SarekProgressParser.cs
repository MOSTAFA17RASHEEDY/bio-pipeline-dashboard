using System.Text.RegularExpressions;

namespace BioPipeline.Api.Services;


/// <summary>
/// Running progress state for one real-pipeline run, updated in place by
/// repeated calls to SarekProgressParser.ApplyLine as Nextflow's stdout
/// streams in.
/// </summary>
public class SarekProgressState
{
    public int StepsCompleted { get; set; }
    public string CurrentStage { get; set; } = "Starting up (pulling containers, preparing inputs)…";
    public double PercentComplete { get; set; }
}

/// <summary>
/// Parses nf-core/sarek's Nextflow status-board stdout into a running
/// percent-complete + current-stage estimate.
///
/// This is a heuristic, not an exact reconstruction: because stdout here is
/// piped (not a real terminal), Nextflow can't use ANSI cursor movement to
/// redraw its live status board in place, so it just reprints the *entire*
/// board again on every refresh. A line for an already-finished process
/// (e.g. "[1e/d8d8fd] NFC…K4_BASERECALIBRATOR (test) | 1 of 1 ✔") therefore
/// reappears verbatim on every later refresh too -- so lines are applied
/// idempotently, keyed by process name, rather than counted as they arrive.
/// </summary>
public static class SarekProgressParser
{
    /// <summary>
    /// Total Nextflow processes a completely fresh (no -resume) run of the
    /// fixed real-pipeline command (see PipelineRunnerService.RunSarekAsync)
    /// executes. Measured empirically from two real runs of this exact
    /// command/revision/config during sarek-reproduction Phase 2 (final
    /// summary line read "Succeeded : 24" both times, no cached processes).
    /// This is NOT a general nf-core/sarek fact -- it's tied to this specific
    /// fixed samplesheet + --tools strelka + chr20 config, and would need
    /// re-measuring if that command ever changes.
    /// </summary>
    public const int KnownTotalProcesses = 24;

    // Matches a completed-or-in-progress process line, e.g.:
    //   [1e/d8d8fd] NFC…K4_BASERECALIBRATOR (test) | 1 of 1 ✔
    //   [32/c8084a] NFCORE_SAREK:PREPARE_GENOME:BWAMEM1_INDEX (genome.fasta) | 1 of 1, cached: 1 ✔
    // Deliberately does NOT match "[-        ] name -" (not-yet-scheduled)
    // lines, or the "executor > local (N)" summary line -- neither carries
    // per-process completion info.
    private static readonly Regex ProcessLine = new(
        @"^\[[0-9a-f]{2}/[0-9a-f]{6}\]\s*(?<name>\S.*?)\s*(?:\([^)]*\)\s*)?\|\s*(?<done>\d+)\s+of\s+(?<total>\d+)(?:,\s*cached:\s*\d+)?\s*(?<check>✔)?\s*$",
        RegexOptions.Compiled);

    private static readonly (string Keyword, string Label)[] StageLabels =
    {
        ("FASTQC", "Quality-checking reads"),
        ("FASTP", "Quality-checking reads"),
        ("BWAMEM", "Aligning reads (BWA-MEM)"),
        ("MARKDUPLICATES", "Marking duplicate reads"),
        ("BASERECALIBRATOR", "Recalibrating base quality"),
        ("APPLYBQSR", "Recalibrating base quality"),
        ("STRELKA", "Calling variants (Strelka2)"),
        ("MOSDEPTH", "Computing coverage stats"),
        ("SAMTOOLS_STATS", "Computing coverage stats"),
        ("BCFTOOLS", "Summarizing variants"),
        ("VCFTOOLS", "Summarizing variants"),
        ("MULTIQC", "Building QC report"),
    };

    /// <summary>
    /// Feeds one line of stdout into the running state. <paramref name="processes"/>
    /// is the caller-owned per-run dictionary tracking each process's latest
    /// known (done, total) -- pass the same dictionary across every call for
    /// one run.
    /// </summary>
    public static void ApplyLine(string? line, Dictionary<string, (int Done, int Total)> processes, SarekProgressState state)
    {
        if (string.IsNullOrWhiteSpace(line)) return;

        if (line.Contains("Pipeline completed successfully"))
        {
            state.PercentComplete = 100;
            state.CurrentStage = "Finalizing results";
            return;
        }

        var m = ProcessLine.Match(line);
        if (!m.Success) return;

        var name = m.Groups["name"].Value.Trim();
        var done = int.Parse(m.Groups["done"].Value);
        var total = int.Parse(m.Groups["total"].Value);
        processes[name] = (done, total);

        if (done < total)
        {
            state.CurrentStage = LabelFor(name);
        }

        state.StepsCompleted = processes.Values.Count(v => v.Total > 0 && v.Done >= v.Total);

        // Clamp below 100 until the explicit completion line above is seen --
        // KnownTotalProcesses is a fixed estimate, and a run that (for
        // whatever reason) executes a different number of processes should
        // never report 100% before Nextflow itself says it's done.
        state.PercentComplete = Math.Clamp(
            (double)state.StepsCompleted / KnownTotalProcesses * 100.0, 0, 99);
    }

    private static string LabelFor(string rawProcessName)
    {
        foreach (var (keyword, label) in StageLabels)
        {
            if (rawProcessName.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return label;
        }
        return "Running pipeline step";
    }
}
