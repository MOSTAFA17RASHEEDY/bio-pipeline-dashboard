import { useState } from "react";
import { Link, useParams } from "react-router-dom";
import { api } from "../api/client";
import { AiExplanationDrawer } from "../components/AiExplanationDrawer";
import { StatusBadge } from "../components/StatusBadge";
import { StepTracker } from "../components/StepTracker";
import { VariantTable } from "../components/VariantTable";
import { useRun } from "../hooks/useRun";

export function RunDetailPage() {
  const { id } = useParams();
  const runId = Number(id);
  const { run, error } = useRun(runId);
  const [logs, setLogs] = useState<string | null>(null);
  const [logsOpen, setLogsOpen] = useState(false);
  const [logsLoading, setLogsLoading] = useState(false);
  const [aiOpen, setAiOpen] = useState(false);
  const [cachedAi, setCachedAi] = useState<string | null>(null);

  async function toggleLogs() {
    if (!logsOpen && logs === null) {
      setLogsLoading(true);
      try {
        setLogs(await api.getLogs(runId));
      } catch (err) {
        setLogs(`Failed to load logs: ${err instanceof Error ? err.message : String(err)}`);
      } finally {
        setLogsLoading(false);
      }
    }
    setLogsOpen((v) => !v);
  }

  if (error) {
    return (
      <div className="p-6">
        <div className="rounded border border-error/40 bg-error-container/20 p-4 text-sm text-error">
          Couldn't load run #{runId}: {error}
        </div>
      </div>
    );
  }

  if (!run) {
    return <div className="p-6 text-sm text-on-surface-variant">Loading run…</div>;
  }

  return (
    <div className="space-y-6 p-6">
      <div>
        <Link to="/" className="text-xs text-on-surface-variant hover:text-on-surface">
          ← Pipeline Runs
        </Link>
        <div className="mt-1 flex flex-wrap items-center justify-between gap-3">
          <div>
            <h1 className="text-xl font-semibold tracking-tight text-on-surface">
              RUN-{run.id}: <span className="font-mono text-lg">{run.sampleFileName}</span>
            </h1>
            <div className="mt-1.5 flex items-center gap-2">
              <StatusBadge status={run.status} />
              {run.completedAt && run.startedAt && (
                <span className="font-mono text-xs text-on-surface-variant">
                  completed in {formatDuration(run.startedAt, run.completedAt)}
                </span>
              )}
            </div>
          </div>

          {run.status === "Done" && (
            <div className="flex items-center gap-2">
              <button
                onClick={() => setAiOpen(true)}
                className="flex items-center gap-1.5 rounded bg-primary-container px-4 py-2 text-sm font-semibold text-on-primary-container shadow-md hover:bg-primary"
              >
                <span className="material-symbols-outlined text-sm">auto_awesome</span>
                Explain with AI
              </button>
              <a
                href={api.vcfDownloadUrl(run.id)}
                className="flex items-center gap-1.5 rounded bg-surface-container-low px-4 py-2 text-sm font-medium text-on-surface shadow-sm hover:bg-surface-container"
              >
                <span className="material-symbols-outlined text-sm">file_download</span>
                Download VCF
              </a>
            </div>
          )}
        </div>
      </div>

      <StepTracker run={run} />

      {run.status === "Done" && (
        <>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-4">
            <ResultStat label="Total Variants" value={String(run.totalVariants ?? 0)} highlight />
            <ResultStat label="SNPs" value={String(run.snpCount ?? 0)} />
            <ResultStat label="Insertions" value={String(run.insCount ?? 0)} />
            <ResultStat label="Deletions" value={String(run.delCount ?? 0)} />
          </div>

          <VariantTable variants={run.variants} />
        </>
      )}

      <div className="overflow-hidden rounded bg-surface-container-low shadow-sm">
        <button
          onClick={toggleLogs}
          className="flex w-full items-center justify-between p-3 text-left text-sm text-on-surface-variant transition-colors hover:text-on-surface"
        >
          <span className="flex items-center gap-2">
            <span className={`material-symbols-outlined text-xs transition-transform ${logsOpen ? "rotate-90" : ""}`}>
              chevron_right
            </span>
            Nextflow log
          </span>
          {logsLoading && <span className="text-xs">Loading…</span>}
        </button>
        {logsOpen && (
          <pre className="max-h-80 overflow-auto bg-surface-container-lowest p-4 font-mono text-[11px] leading-relaxed text-on-surface-variant">
            {logs || "(no log output yet)"}
          </pre>
        )}
      </div>

      {aiOpen && (
        <AiExplanationDrawer
          runId={run.id}
          cachedExplanation={cachedAi ?? run.aiExplanation}
          onExplained={setCachedAi}
          onClose={() => setAiOpen(false)}
        />
      )}
    </div>
  );
}

function ResultStat({ label, value, highlight }: { label: string; value: string; highlight?: boolean }) {
  return (
    <div className="rounded bg-surface-container-low p-4 shadow-sm">
      <span className="font-mono text-[11px] uppercase tracking-wider text-on-surface-variant">{label}</span>
      <div className={`mt-2 text-2xl font-semibold ${highlight ? "text-primary" : "text-on-surface"}`}>{value}</div>
    </div>
  );
}

function formatDuration(startIso: string, endIso: string): string {
  const ms = new Date(endIso + (endIso.endsWith("Z") ? "" : "Z")).getTime() -
    new Date(startIso + (startIso.endsWith("Z") ? "" : "Z")).getTime();
  const totalSeconds = Math.max(0, Math.round(ms / 1000));
  const mins = Math.floor(totalSeconds / 60);
  const secs = totalSeconds % 60;
  return mins > 0 ? `${mins}m ${secs}s` : `${secs}s`;
}
