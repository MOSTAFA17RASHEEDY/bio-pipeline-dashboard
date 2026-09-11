using System.Text.Json.Serialization;

namespace BioPipeline.Api.Services;

// These mirror the JSON shapes written by pipeline/scripts/run_qc.py and
// pipeline/scripts/call_variants.py. Kept separate from the EF Core models
// (Models/Run.cs, Models/Variant.cs) because "what the pipeline writes to
// disk" and "what we store/serve" are different concerns that happen to
// overlap right now -- they're allowed to drift independently later.

public class QcReportJson
{
    [JsonPropertyName("total_reads")] public int TotalReads { get; set; }
    [JsonPropertyName("mean_phred_quality")] public double MeanPhredQuality { get; set; }
    [JsonPropertyName("pass_rate_percent")] public double PassRatePercent { get; set; }
    [JsonPropertyName("status")] public string Status { get; set; } = "";
}

public class VariantsSummaryJson
{
    [JsonPropertyName("total_variants")] public int TotalVariants { get; set; }
    [JsonPropertyName("counts_by_type")] public CountsByTypeJson? CountsByType { get; set; }
    [JsonPropertyName("variants")] public List<VariantJson> Variants { get; set; } = new();
}

public class CountsByTypeJson
{
    [JsonPropertyName("SNP")] public int Snp { get; set; }
    [JsonPropertyName("INS")] public int Ins { get; set; }
    [JsonPropertyName("DEL")] public int Del { get; set; }
}

public class VariantJson
{
    [JsonPropertyName("chrom")] public string Chrom { get; set; } = "";
    [JsonPropertyName("pos")] public int Pos { get; set; }
    [JsonPropertyName("ref")] public string Ref { get; set; } = "";
    [JsonPropertyName("alt")] public string Alt { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("qual")] public double Qual { get; set; }
    [JsonPropertyName("depth")] public int Depth { get; set; }
    [JsonPropertyName("supporting_reads")] public int SupportingReads { get; set; }
    [JsonPropertyName("allele_frequency")] public double AlleleFrequency { get; set; }
}
