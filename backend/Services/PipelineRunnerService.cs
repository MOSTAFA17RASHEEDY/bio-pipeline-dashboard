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
            await RunNextflowAsync(run, ct);
            await LoadResultsAsync(run, db, ct);
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
