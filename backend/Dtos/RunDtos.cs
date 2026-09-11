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
    DateTime CreatedAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    int? TotalVariants);

public record ExplainRunResponseDto(string Explanation, bool Cached);

public record RunDetailDto(
    int Id,
    string SampleFileName,
    string Status,
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
    string? AiExplanation,
    List<VariantDto> Variants);

public static class RunMapping
{
    public static RunListItemDto ToListItemDto(this Run r) => new(
        r.Id, r.SampleFileName, r.Status.ToString(), r.CreatedAt, r.StartedAt, r.CompletedAt, r.TotalVariants);

    public static RunDetailDto ToDetailDto(this Run r) => new(
        r.Id, r.SampleFileName, r.Status.ToString(), r.CreatedAt, r.StartedAt, r.CompletedAt, r.ErrorMessage,
        r.QcTotalReads, r.QcMeanQuality, r.QcPassRatePercent, r.QcStatus,
        r.TotalVariants, r.SnpCount, r.InsCount, r.DelCount, r.AiExplanation,
        r.Variants
            .OrderBy(v => v.Position)
            .Select(v => new VariantDto(
                v.Chrom, v.Position, v.Ref, v.Alt, v.Type, v.Qual, v.Depth, v.SupportingReads, v.AlleleFrequency))
            .ToList());
}
