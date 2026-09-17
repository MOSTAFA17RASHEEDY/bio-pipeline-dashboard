using BioPipeline.Api.Data;
using BioPipeline.Api.Dtos;
using BioPipeline.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BioPipeline.Api.Endpoints;

public static class RunEndpoints
{
    private static readonly string[] AllowedExtensions = [".fastq", ".fq", ".fasta", ".fa", ".gz"];
    private const long MaxUploadBytes = 20 * 1024 * 1024; // 20 MB -- generous for this toy project's sample sizes

    public static void MapRunEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/runs").WithTags("Runs");

        group.MapPost("/", CreateRun)
            .WithName("CreateRun")
            .WithSummary("Upload a sample DNA file (FASTQ/FASTA) and start a pipeline run")
            .DisableAntiforgery();

        group.MapPost("/sarek", CreateRealRun)
            .WithName("CreateRealSarekRun")
            .WithSummary("Start a real nf-core/sarek run against the fixed NA12878 chr20 dataset (no upload; ~6-10 min)");

        group.MapGet("/", ListRuns)
            .WithName("ListRuns")
            .WithSummary("List past and in-progress runs, newest first");

        group.MapGet("/{id:int}", GetRun)
            .WithName("GetRun")
            .WithSummary("Get full detail for one run, including its variant table once done");

        group.MapGet("/{id:int}/vcf", DownloadVcf)
            .WithName("DownloadRunVcf")
            .WithSummary("Download the raw VCF file produced by a completed run");

        group.MapGet("/{id:int}/logs", GetLogs)
            .WithName("GetRunLogs")
            .WithSummary("Get the Nextflow stdout/stderr log for a run");

        group.MapPost("/{id:int}/explain", ExplainRun)
            .WithName("ExplainRun")
            .WithSummary("Get a short plain-language explanation of a completed run's variants, via Gemini");
    }

    private static async Task<IResult> CreateRun(
        HttpRequest request,
        BioPipelineDbContext db,
        PipelineRunQueue queue,
        IOptions<PipelineOptions> options)
    {
        if (!request.HasFormContentType)
            return Results.BadRequest("Expected multipart/form-data with a 'file' field.");

        var form = await request.ReadFormAsync();
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
            return Results.BadRequest("No file uploaded. Include a 'file' field with a FASTQ/FASTA sample.");

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            return Results.BadRequest(
                $"Unsupported file type '{ext}'. Expected one of: {string.Join(", ", AllowedExtensions)}");
        }

        if (file.Length > MaxUploadBytes)
            return Results.BadRequest($"File too large. Max {MaxUploadBytes / 1024 / 1024} MB for this demo.");

        // Insert first so we get an Id, then use it to lay out this run's folder.
        var run = new Models.Run
        {
            SampleFileName = file.FileName,
            Status = Models.RunStatus.Queued,
            CreatedAt = DateTime.UtcNow,
        };
        db.Runs.Add(run);
        await db.SaveChangesAsync();

        var runDir = Path.Combine(options.Value.RunsStorageDir, run.Id.ToString());
        var inputDir = Path.Combine(runDir, "input");
        Directory.CreateDirectory(inputDir);

        var inputFilePath = Path.Combine(inputDir, file.FileName);
        await using (var stream = File.Create(inputFilePath))
        {
            await file.CopyToAsync(stream);
        }

        run.InputFilePath = inputFilePath;
        run.ResultsDir = Path.Combine(runDir, "results");
        await db.SaveChangesAsync();

        await queue.EnqueueAsync(run.Id);

        return Results.Created($"/api/runs/{run.Id}", run.ToDetailDto());
    }

    private static async Task<IResult> CreateRealRun(BioPipelineDbContext db, PipelineRunQueue queue)
    {
        var alreadyActive = await db.Runs.AnyAsync(r =>
            r.Kind == Models.RunKind.RealSarek &&
            (r.Status == Models.RunStatus.Queued || r.Status == Models.RunStatus.Running));
        if (alreadyActive)
        {
            return Results.Conflict(
                "A real pipeline run is already queued or running. Wait for it to finish before starting another.");
        }

        var run = new Models.Run
        {
            Kind = Models.RunKind.RealSarek,
            SampleFileName = "NA12878 (chr20:10,000,000-10,200,000, fixed dataset)",
            Status = Models.RunStatus.Queued,
            CreatedAt = DateTime.UtcNow,
        };
        db.Runs.Add(run);
        await db.SaveChangesAsync();

        // ResultsDir is filled in by PipelineRunnerService.RunSarekAsync once
        // it knows the DB-assigned run Id (it needs that to build the WSL
        // work dir path) -- left empty until then, same as InputFilePath.

        await queue.EnqueueAsync(run.Id);

        return Results.Created($"/api/runs/{run.Id}", run.ToDetailDto());
    }

    private static async Task<IResult> ListRuns(BioPipelineDbContext db)
    {
        var runs = await db.Runs.OrderByDescending(r => r.CreatedAt).ToListAsync();
        return Results.Ok(runs.Select(r => r.ToListItemDto()));
    }

    private static async Task<IResult> GetRun(int id, BioPipelineDbContext db)
    {
        var run = await db.Runs.Include(r => r.Variants).FirstOrDefaultAsync(r => r.Id == id);
        return run is null ? Results.NotFound() : Results.Ok(run.ToDetailDto());
    }

    private static async Task<IResult> DownloadVcf(int id, BioPipelineDbContext db)
    {
        var run = await db.Runs.FindAsync(id);
        if (run is null) return Results.NotFound();

        var isReal = run.Kind == Models.RunKind.RealSarek;
        var vcfPath = isReal
            ? Path.Combine(run.ResultsDir, "variant_calling", "strelka", "NA12878", "NA12878.strelka.variants.vcf.gz")
            : Path.Combine(run.ResultsDir, "variants", "variants.vcf");

        if (!File.Exists(vcfPath))
            return Results.NotFound("VCF not available yet -- the run may still be in progress.");

        return isReal
            ? Results.File(vcfPath, "application/gzip", $"run-{id}-strelka.vcf.gz")
            : Results.File(vcfPath, "text/plain", $"run-{id}-variants.vcf");
    }

    private static async Task<IResult> GetLogs(int id, BioPipelineDbContext db)
    {
        var run = await db.Runs.FindAsync(id);
        if (run is null) return Results.NotFound();

        var logPath = Path.Combine(Path.GetDirectoryName(run.ResultsDir)!, "pipeline.log");
        if (!File.Exists(logPath)) return Results.Text("");

        // Read-share the file since PipelineRunnerService may still be writing to it.
        await using var stream = new FileStream(logPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return Results.Text(await reader.ReadToEndAsync());
    }

    private static async Task<IResult> ExplainRun(int id, BioPipelineDbContext db, GeminiExplanationService gemini)
    {
        var run = await db.Runs.Include(r => r.Variants).FirstOrDefaultAsync(r => r.Id == id);
        if (run is null) return Results.NotFound();

        if (run.Status != Models.RunStatus.Done)
            return Results.BadRequest("This run must be Done before it can be explained.");

        // Cache the explanation on the Run row so re-opening the drawer (or
        // re-fetching run detail) doesn't burn another API call.
        if (!string.IsNullOrEmpty(run.AiExplanation))
            return Results.Ok(new ExplainRunResponseDto(run.AiExplanation, Cached: true));

        try
        {
            var explanation = await gemini.ExplainAsync(run, CancellationToken.None);
            run.AiExplanation = explanation;
            await db.SaveChangesAsync();
            return Results.Ok(new ExplainRunResponseDto(explanation, Cached: false));
        }
        catch (Exception ex)
        {
            return Results.Problem(ex.Message, statusCode: StatusCodes.Status502BadGateway);
        }
    }
}
