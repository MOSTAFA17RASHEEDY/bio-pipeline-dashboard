namespace BioPipeline.Api.Models;

/// <summary>
/// One row of variants.vcf, parsed out of variants_summary.json and stored
/// so the API can serve a sortable/filterable table without re-reading
/// files on every request.
/// </summary>
public class Variant
{
    public int Id { get; set; }

    public int RunId { get; set; }
    public Run? Run { get; set; }

    public string Chrom { get; set; } = "";
    public int Position { get; set; }
    public string Ref { get; set; } = "";
    public string Alt { get; set; } = "";
    public string Type { get; set; } = ""; // "SNP", "INS", or "DEL"
    public double Qual { get; set; }
    public int Depth { get; set; }
    public int SupportingReads { get; set; }
    public double AlleleFrequency { get; set; }
}
