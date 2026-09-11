namespace BioPipeline.Api.Models;

/// <summary>
/// Lifecycle of a pipeline run. Stored as a string in SQLite (see
/// BioPipelineDbContext) so the database stays human-readable.
/// </summary>
public enum RunStatus
{
    Queued,
    Running,
    Done,
    Failed,
}
