using BioPipeline.Api.Models;

namespace BioPipeline.Api.Dtos;

// What the API actually sends over the wire -- deliberately separate from
// the EF Core entities in Models/ so internal details (file system paths,
// the EF navigation graph) never leak into a response.

public record VariantDto(
    string Chrom,
    int Position,
    string Ref,
    string Alt,
    string Type,
    double Qual,
    int Depth,
    int SupportingReads,
    double AlleleFrequency);

public record RunListItemDto(
    int Id,
    string SampleFileName,
    string Status,
    string Kind,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int? TotalVariants,
    double? ProgressPercent);

public record ExplainRunResponseDto(string Explanation, bool Cached);

// Static, deterministic-for-this-fixed-dataset accuracy numbers (see
// RunMapping.SarekValidation) -- never recomputed per run.
public record ValidationMetricsDto(
    double SnvRecall, double SnvPrecision, double SnvF1, int SnvTruthTotal,
    double IndelRecall, double IndelPrecision, double IndelF1, int IndelTruthTotal);

public record RunDetailDto(
    int Id,
    string SampleFileName,
    string Status,
    string Kind,
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? ErrorMessage,
    int? QcTotalReads,
    double? QcMeanQuality,
    double? QcPassRatePercent,
    string? QcStatus,
    int? TotalVariants,
    int? SnpCount,
    int? InsCount,
    int? DelCount,
    double? ProgressPercent,
    string? CurrentStage,
    int? EtaSecondsRemaining,
    int? StepsCompleted,
    int? StepsTotal,
    double? MeanDepth,
    ValidationMetricsDto? Validation,
    string? AiExplanation,
    List<VariantDto> Variants);

public static class RunMapping
{
    // Source: sarek-reproduction/results/na12878_subset/NA12878_chr20_10M.summary.csv
    // (PASS rows), hap.py vs GIAB HG001 GRCh38 v4.2.1 truth, chr20:10.0-10.2Mb.
    // Static because it's deterministic for this fixed dataset+pipeline
    // config -- re-running hap.py per real run would cost ~2-3 more minutes
    // for the same answer.
    private static readonly ValidationMetricsDto SarekValidation = new(
        SnvRecall: 1.000, SnvPrecision: 1.000, SnvF1: 1.000, SnvTruthTotal: 325,
        IndelRecall: 0.983, IndelPrecision: 0.984, IndelF1: 0.984, IndelTruthTotal: 60);

    public static RunListItemDto ToListItemDto(this Run r) => new(
        r.Id, r.SampleFileName, r.Status.ToString(), r.Kind.ToString(),
        r.CreatedAt, r.StartedAt, r.CompletedAt, r.TotalVariants, r.ProgressPercent);

    public static RunDetailDto ToDetailDto(this Run r) => new(
        r.Id, r.SampleFileName, r.Status.ToString(), r.Kind.ToString(),
        r.CreatedAt, r.StartedAt, r.CompletedAt, r.ErrorMessage,
        r.QcTotalReads, r.QcMeanQuality, r.QcPassRatePercent, r.QcStatus,
        r.TotalVariants, r.SnpCount, r.InsCount, r.DelCount,
        r.ProgressPercent, r.CurrentStage, r.EtaSecondsRemaining, r.StepsCompleted, r.StepsTotal,
        r.MeanDepth, r.Kind == RunKind.RealSarek ? SarekValidation : null,
        r.AiExplanation,
        r.Variants
            .OrderBy(v => v.Position)
            .Select(v => new VariantDto(
                v.Chrom, v.Position, v.Ref, v.Alt, v.Type, v.Qual, v.Depth, v.SupportingReads, v.AlleleFrequency))
            .ToList());
}
