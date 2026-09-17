using System.Diagnostics;
using System.Text.Json;
using BioPipeline.Api.Data;
using BioPipeline.Api.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BioPipeline.Api.Services;

/// <summary>
/// A BackgroundService is ASP.NET Core's mechanism for a long-running task
/// that starts with the app and shares its lifetime. This one owns the
/// entire "run the pipeline" side of the app: it pulls run IDs off
/// PipelineRunQueue one at a time, shells out to `wsl.exe ... nextflow run`,
/// and writes the outcome back to the database.
///
/// It's a singleton (see Program.cs), so it can't depend on the DbContext
/// directly (DbContext is scoped, one-per-request); instead it asks
/// IServiceScopeFactory for a fresh scope -- and therefore a fresh
/// DbContext -- each time it processes a run.
/// </summary>
public class PipelineRunnerService(
    PipelineRunQueue queue,
    IServiceScopeFactory scopeFactory,
    IOptions<PipelineOptions> options,
    ILogger<PipelineRunnerService> logger) : BackgroundService
{
    private readonly PipelineOptions _options = options.Value;
    private readonly object _logLock = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var runId in queue.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessRunAsync(runId, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled error processing run {RunId}", runId);
            }
        }
    }

    private async Task ProcessRunAsync(int runId, CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BioPipelineDbContext>();

        var run = await db.Runs.FindAsync([runId], ct);
        if (run is null)
        {
            logger.LogWarning("Run {RunId} disappeared before processing started", runId);
            return;
        }

        run.Status = RunStatus.Running;
        run.StartedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);

        try
        {
            if (run.Kind == RunKind.RealSarek)
            {
                await RunSarekAsync(run, db, ct);
                await LoadSarekResultsAsync(run, db, ct);
            }
            else
            {
                await RunNextflowAsync(run, ct);
                await LoadResultsAsync(run, db, ct);
            }
            run.Status = RunStatus.Done;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Run {RunId} failed", run.Id);
            run.Status = RunStatus.Failed;
            run.ErrorMessage = ex.Message;
        }
        finally
        {
            run.CompletedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(ct);
        }
    }

    private async Task RunNextflowAsync(Run run, CancellationToken ct)
    {
        var runDir = Path.GetDirectoryName(run.ResultsDir)!;
        var workDir = Path.Combine(runDir, "work");
        var logPath = Path.Combine(runDir, "pipeline.log");

        var pipelineDirWsl = WslPath.FromWindows(_options.PipelineDir);
        var readsWsl = WslPath.FromWindows(run.InputFilePath);
        var outdirWsl = WslPath.FromWindows(run.ResultsDir);
        var workDirWsl = WslPath.FromWindows(workDir);

        // Single quotes around each path guard against spaces (this repo's
        // own path has one: "Bio Pipeline Dashboard").
        var nextflowCommand =
            $"cd '{pipelineDirWsl}' && nextflow run main.nf " +
            $"-w '{workDirWsl}' --reads '{readsWsl}' --outdir '{outdirWsl}'";

        var psi = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("-d");
        psi.ArgumentList.Add(_options.WslDistro);
        psi.ArgumentList.Add("--");
        psi.ArgumentList.Add("bash");
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(nextflowCommand);

        await using var logWriter = new StreamWriter(logPath, append: false) { AutoFlush = true };

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => WriteLogLine(logWriter, e.Data);
        process.ErrorDataReceived += (_, e) => WriteLogLine(logWriter, e.Data is null ? null : $"[stderr] {e.Data}");

        logger.LogInformation("Run {RunId}: starting nextflow ({Command})", run.Id, nextflowCommand);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();
        await process.WaitForExitAsync(ct);

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"Nextflow exited with code {process.ExitCode}. See pipeline.log in the run's folder for details.");
        }
    }

    private void WriteLogLine(StreamWriter writer, string? line)
    {
        if (line is null) return;
        lock (_logLock)
        {
            writer.WriteLine(line);
        }
    }

    /// <summary>
    /// Runs the real nf-core/sarek pipeline against the fixed, pre-sliced
    /// NA12878 chr20 dataset built in sarek-reproduction/ (see
    /// RealSarekOptions doc comments for what each input path is). Unlike
    /// RunNextflowAsync, this also parses live progress out of the stdout
    /// stream via SarekProgressParser and persists it onto `run` throughout
    /// the run, since a real run takes minutes rather than seconds.
    ///
    /// No `-resume` and no detach/setsid wrapper: each click is meant to be a
    /// genuine fresh run (by design -- this is demonstrating the real
    /// pipeline actually executing, not replaying a cache), and detachment
    /// isn't needed because this Process is already kept alive for its whole
    /// duration by `await process.WaitForExitAsync(ct)` below, the same
    /// mechanism RunNextflowAsync already relies on for its own (much
    /// shorter) runs.
    /// </summary>
    private async Task RunSarekAsync(Run run, BioPipelineDbContext db, CancellationToken ct)
    {
        var sarek = _options.RealSarek;
        var runWorkDirWsl = $"{sarek.WorkRootDir}/{run.Id}";
        var outdirWsl = $"{runWorkDirWsl}/results";
        var nextflowWorkDirWsl = $"{runWorkDirWsl}/work";
        var dataDir = sarek.InputDataDir;

        // Set before the process starts: GetLogs/DownloadVcf/LoadSarekResultsAsync
        // all derive paths from run.ResultsDir, and the run row is already
        // visible to GET /api/runs/{id} polls while this is in flight.
        run.ResultsDir = WslPath.ToUncPath(outdirWsl, _options.WslDistro);
        await db.SaveChangesAsync(ct);

        var laptopConfigWsl = WslPath.FromWindows(sarek.LaptopConfigPath);
        var subsetConfigWsl = WslPath.FromWindows(sarek.SubsetConfigPath);

        var nextflowCommand =
            $"mkdir -p '{runWorkDirWsl}' && cd '{runWorkDirWsl}' && " +
            $"nextflow run nf-core/sarek -r {sarek.Revision} -profile docker " +
            $"-c '{laptopConfigWsl}' -c '{subsetConfigWsl}' " +
            $"--input '{dataDir}/samplesheet.csv' --outdir '{outdirWsl}' " +
            $"--fasta '{dataDir}/chr20.fa' --fasta_fai '{dataDir}/chr20.fa.fai' --dict '{dataDir}/chr20.dict' " +
            $"--known_indels '{dataDir}/*_chr20.vcf.gz' --known_indels_tbi '{dataDir}/*_chr20.vcf.gz.tbi' " +
            $"--intervals '{dataDir}/target_chr20_10M.bed' --tools strelka " +
            $"-w '{nextflowWorkDirWsl}'";

        var runDirUnc = Path.GetDirectoryName(run.ResultsDir)!;
        var logPath = Path.Combine(runDirUnc, "pipeline.log");

        // The run's WSL directory doesn't exist yet at this point -- the
        // nextflow command's own `mkdir -p` only runs once the WSL process
        // below actually starts, but we need somewhere to write pipeline.log
        // (via the same UNC path) before that. Create it from the .NET side
        // first; the command's `mkdir -p` then just becomes a harmless no-op.
        Directory.CreateDirectory(runDirUnc);

        var psi = new ProcessStartInfo
        {
            FileName = "wsl.exe",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };
        psi.ArgumentList.Add("-d");
        psi.ArgumentList.Add(_options.WslDistro);
        psi.ArgumentList.Add("--");
        psi.ArgumentList.Add("bash");
        psi.ArgumentList.Add("-c");
        psi.ArgumentList.Add(nextflowCommand);

        await using var logWriter = new StreamWriter(logPath, append: false) { AutoFlush = true };

        // Shared between the OutputDataReceived callbacks (below) and the
        // progress-persisting loop (PersistSarekProgressAsync) -- both run
        // concurrently with WaitForExitAsync, so access is locked.
        var processes = new Dictionary<string, (int Done, int Total)>();
        var state = new SarekProgressState();
        var stateLock = new object();

        void HandleLine(string? line, bool isStderr)
        {
            WriteLogLine(logWriter, isStderr && line is not null ? $"[stderr] {line}" : line);
            if (line is null) return;
            lock (stateLock) { SarekProgressParser.ApplyLine(line, processes, state); }
        }

        using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        process.OutputDataReceived += (_, e) => HandleLine(e.Data, isStderr: false);
        process.ErrorDataReceived += (_, e) => HandleLine(e.Data, isStderr: true);

        logger.LogInformation("Run {RunId}: starting real sarek pipeline ({Command})", run.Id, nextflowCommand);
        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        var persistTask = PersistSarekProgressAsync(run, db, state, stateLock, process, ct);
        await process.WaitForExitAsync(ct);
        await persistTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                $"nf-core/sarek exited with code {process.ExitCode}. See pipeline.log in the run's folder for details.");
        }
    }

    /// <summary>
    /// Runs alongside WaitForExitAsync in RunSarekAsync, writing the parsed
    /// progress state onto `run` roughly once a second -- only when the
    /// percent actually changed, so a long-idle step (e.g. a slow container
    /// pull) doesn't spam identical DB writes.
    /// </summary>
    private static async Task PersistSarekProgressAsync(
        Run run, BioPipelineDbContext db, SarekProgressState state, object stateLock, Process process, CancellationToken ct)
    {
        double lastPercent = -1;
        while (!process.HasExited)
        {
            try { await Task.Delay(1000, ct); }
            catch (OperationCanceledException) { break; }

            double percent;
            string stage;
            int stepsCompleted;
            int? etaSeconds;
            lock (stateLock)
            {
                etaSeconds = null;
                if (state.StepsCompleted >= 1 && run.StartedAt is not null)
                {
                    var elapsed = DateTime.UtcNow - run.StartedAt.Value;
                    var estimatedTotal = elapsed / ((double)state.StepsCompleted / SarekProgressParser.KnownTotalProcesses);
                    etaSeconds = (int)Math.Max(0, (estimatedTotal - elapsed).TotalSeconds);
                }
                percent = state.PercentComplete;
                stage = state.CurrentStage;
                stepsCompleted = state.StepsCompleted;
            }

            if (Math.Abs(percent - lastPercent) < 0.01) continue;
            lastPercent = percent;

            run.ProgressPercent = percent;
            run.CurrentStage = stage;
            run.StepsCompleted = stepsCompleted;
            run.StepsTotal = SarekProgressParser.KnownTotalProcesses;
            run.EtaSecondsRemaining = etaSeconds;
            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Parses the real pipeline's Strelka VCF straight off the WSL filesystem
    /// (via the \\wsl.localhost UNC path in run.ResultsDir -- no copy step)
    /// into the same Variant rows the toy pipeline populates, so the frontend
    /// needs no per-kind branching to render results. Only PASS-filter calls
    /// are loaded, matching how the toy pipeline's caller only ever reports
    /// calls it's confident in.
    /// </summary>
    private static async Task LoadSarekResultsAsync(Run run, BioPipelineDbContext db, CancellationToken ct)
    {
        var vcfPath = Path.Combine(run.ResultsDir, "variant_calling", "strelka", "NA12878", "NA12878.strelka.variants.vcf.gz");
        if (!File.Exists(vcfPath))
            throw new InvalidOperationException($"Expected Strelka VCF not found at {vcfPath}.");

        int total = 0, snp = 0, ins = 0, del = 0;

        await using (var fs = new FileStream(vcfPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        await using (var gz = new System.IO.Compression.GZipStream(fs, System.IO.Compression.CompressionMode.Decompress))
        using (var reader = new StreamReader(gz))
        {
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) is not null)
            {
                if (line.StartsWith('#')) continue;
                var cols = line.Split('\t');
                if (cols.Length < 10) continue;

                var chrom = cols[0];
                var posStr = cols[1];
                var refBase = cols[3];
                var alt = cols[4];
                var qualStr = cols[5];
                var filter = cols[6];
                var format = cols[8];
                var sample = cols[9];

                if (filter != "PASS") continue;

                var formatKeys = format.Split(':');
                var sampleVals = sample.Split(':');
                var fields = new Dictionary<string, string>();
                for (int i = 0; i < formatKeys.Length && i < sampleVals.Length; i++)
                    fields[formatKeys[i]] = sampleVals[i];

                var ad = fields.TryGetValue("AD", out var adStr)
                    ? adStr.Split(',').Select(int.Parse).ToArray()
                    : [0, 0];
                var refDepth = ad.Length > 0 ? ad[0] : 0;
                var altDepth = ad.Length > 1 ? ad[1] : 0;

                // Strelka SNV records carry DP; indel records carry DPI instead.
                int depth = fields.TryGetValue("DP", out var dpStr) ? int.Parse(dpStr)
                    : fields.TryGetValue("DPI", out var dpiStr) ? int.Parse(dpiStr)
                    : refDepth + altDepth;

                var type = refBase.Length == 1 && alt.Length == 1 ? "SNP"
                    : alt.Length > refBase.Length ? "INS"
                    : "DEL";

                db.Variants.Add(new Models.Variant
                {
                    RunId = run.Id,
                    Chrom = chrom,
                    Position = int.Parse(posStr),
                    Ref = refBase,
                    Alt = alt,
                    Type = type,
                    Qual = double.Parse(qualStr, System.Globalization.CultureInfo.InvariantCulture),
                    Depth = depth,
                    SupportingReads = altDepth,
                    AlleleFrequency = depth > 0 ? (double)altDepth / depth : 0.0,
                });

                total++;
                if (type == "SNP") snp++;
                else if (type == "INS") ins++;
                else del++;
            }
        }

        run.TotalVariants = total;
        run.SnpCount = snp;
        run.InsCount = ins;
        run.DelCount = del;
        run.ProgressPercent = 100;
        run.CurrentStage = "Done";
        run.EtaSecondsRemaining = 0;

        // Best-effort: the exact nested path for mosdepth's summary varies by
        // sample/step, so search rather than hardcode it. Non-critical --
        // any failure here just leaves MeanDepth null, it doesn't fail the run.
        //
        // Deliberately NOT using the summary file's own "mean" column for the
        // "*_region" row: mosdepth reports that row's "length" as the whole
        // chr20 length (64,444,167) even though "bases" correctly counts only
        // covered bases within the --intervals BED, so "mean" comes out as a
        // misleading ~0.1 instead of the real ~31x over the actual 200,001 bp
        // target (confirmed empirically against a real run's output; matches
        // how sarek-reproduction/README.md's own 31.6x figure was computed:
        // bases / target region length, not mosdepth's own mean).
        const int TargetRegionLengthBp = 200_001; // sarek-reproduction's target_chr20_10M.bed span
        try
        {
            var mosdepthFile = Directory.EnumerateFiles(run.ResultsDir, "*.mosdepth.summary.txt", SearchOption.AllDirectories).FirstOrDefault();
            if (mosdepthFile is not null)
            {
                var summaryLines = await File.ReadAllLinesAsync(mosdepthFile, ct);
                var regionRow = summaryLines.FirstOrDefault(l => l.StartsWith("chr20_region\t"))
                    ?? summaryLines.FirstOrDefault(l => l.StartsWith("total_region\t"));
                if (regionRow is not null)
                {
                    var parts = regionRow.Split('\t');
                    if (parts.Length > 2 && double.TryParse(parts[2], System.Globalization.CultureInfo.InvariantCulture, out var coveredBases))
                        run.MeanDepth = coveredBases / TargetRegionLengthBp;
                }
            }
        }
        catch { /* best-effort only */ }
    }

    private static async Task LoadResultsAsync(Run run, BioPipelineDbContext db, CancellationToken ct)
    {
        var qcReportPath = Path.Combine(run.ResultsDir, "qc", "qc_report.json");
        if (File.Exists(qcReportPath))
        {
            var qc = JsonSerializer.Deserialize<QcReportJson>(await File.ReadAllTextAsync(qcReportPath, ct));
            if (qc is not null)
            {
                run.QcTotalReads = qc.TotalReads;
                run.QcMeanQuality = qc.MeanPhredQuality;
                run.QcPassRatePercent = qc.PassRatePercent;
                run.QcStatus = qc.Status;
            }
        }

        var variantsSummaryPath = Path.Combine(run.ResultsDir, "variants", "variants_summary.json");
        if (File.Exists(variantsSummaryPath))
        {
            var vs = JsonSerializer.Deserialize<VariantsSummaryJson>(
                await File.ReadAllTextAsync(variantsSummaryPath, ct));
            if (vs is not null)
            {
                run.TotalVariants = vs.TotalVariants;
                run.SnpCount = vs.CountsByType?.Snp ?? 0;
                run.InsCount = vs.CountsByType?.Ins ?? 0;
                run.DelCount = vs.CountsByType?.Del ?? 0;

                foreach (var v in vs.Variants)
                {
                    db.Variants.Add(new Models.Variant
                    {
                        RunId = run.Id,
                        Chrom = v.Chrom,
                        Position = v.Pos,
                        Ref = v.Ref,
                        Alt = v.Alt,
                        Type = v.Type,
                        Qual = v.Qual,
                        Depth = v.Depth,
                        SupportingReads = v.SupportingReads,
                        AlleleFrequency = v.AlleleFrequency,
                    });
                }
            }
        }
    }
}
