namespace BioPipeline.Api.Models;

/// <summary>
/// Which pipeline a run went through: the toy synthetic-data demo
/// (pipeline/), or the real nf-core/sarek pipeline against real NA12878
/// genomic data (see sarek-reproduction/README.md).
/// </summary>
public enum RunKind
{
    Toy,
    RealSarek,
}
