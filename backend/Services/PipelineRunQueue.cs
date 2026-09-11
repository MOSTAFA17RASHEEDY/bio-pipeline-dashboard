using System.Threading.Channels;

namespace BioPipeline.Api.Services;

/// <summary>
/// A simple in-process FIFO queue of run IDs waiting to be processed.
/// PipelineRunnerService reads from it one at a time, so only one Nextflow
/// run ever executes at once -- which is also what makes the "Queued"
/// status in the dashboard meaningful: a second upload genuinely waits its
/// turn rather than racing the first for CPU/Docker resources.
/// </summary>
public class PipelineRunQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

    public ValueTask EnqueueAsync(int runId, CancellationToken ct = default) =>
        _channel.Writer.WriteAsync(runId, ct);

    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken ct) =>
        _channel.Reader.ReadAllAsync(ct);
}
