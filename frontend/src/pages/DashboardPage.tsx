import { useEffect, useMemo, useState } from "react";
import { Link } from "react-router-dom";
import { Pagination } from "../components/Pagination";
import { StatCard } from "../components/StatCard";
import { StatusBadge } from "../components/StatusBadge";
import { useRunList } from "../hooks/useRunList";
import type { RunStatus } from "../types";

const PAGE_SIZE = 10;

function timeAgo(iso: string | null): string {
  if (!iso) return "—";
  const diffMs = Date.now() - new Date(iso + (iso.endsWith("Z") ? "" : "Z")).getTime();
  const mins = Math.round(diffMs / 60000);
  if (mins < 1) return "just now";
  if (mins < 60) return `${mins}m ago`;
  const hours = Math.round(mins / 60);
  if (hours < 24) return `${hours}h ago`;
  return `${Math.round(hours / 24)}d ago`;
}

export function DashboardPage() {
  const { runs, error } = useRunList();
  const [filter, setFilter] = useState<"ALL" | RunStatus>("ALL");
  const [page, setPage] = useState(1);

  const stats = useMemo(() => {
    const list = runs ?? [];
    const today = new Date().toDateString();
    return {
      total: list.length,
      running: list.filter((r) => r.status === "Running" || r.status === "Queued").length,
      doneToday: list.filter((r) => r.status === "Done" && new Date(r.completedAt ?? "").toDateString() === today)
        .length,
      failed: list.filter((r) => r.status === "Failed").length,
    };
  }, [runs]);

  const filtered = useMemo(() => {
    const list = runs ?? [];
    return filter === "ALL" ? list : list.filter((r) => r.status === filter);
  }, [runs, filter]);

  // Reset to page 1 when the filter changes, or when the list's length
  // changes shape (e.g. a new run just got created) so we don't land on a
  // now-empty trailing page.
  useEffect(() => {
    setPage(1);
  }, [filter, runs?.length]);

  const pageRuns = useMemo(
    () => filtered.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE),
    [filtered, page],
  );

  return (
    <div className="space-y-6 p-6">
      <div className="flex flex-col justify-between gap-4 md:flex-row md:items-center">
        <div>
          <h1 className="text-xl font-semibold tracking-tight text-on-surface">Pipeline Runs</h1>
          <p className="mt-0.5 max-w-2xl text-sm text-on-surface-variant">
            Toy DNA samples processed through Quality Check → Alignment → Variant Calling.
          </p>
        </div>
        <Link
          to="/new"
          className="flex items-center gap-1.5 self-start rounded bg-primary-container px-4 py-2 text-sm font-semibold text-on-primary-container shadow-md transition-colors hover:bg-primary md:self-auto"
        >
          <span className="material-symbols-outlined text-base">add</span>
          New Run
        </Link>
      </div>

      {error && (
        <div className="rounded border border-error/40 bg-error-container/20 p-3 text-sm text-error">
          Couldn't reach the API ({error}). Is the backend running at{" "}
          <code>{import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5179"}</code>?
        </div>
      )}

      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label="Total Runs" value={String(stats.total)} icon="hub" />
        <StatCard label="Queued / Running" value={String(stats.running)} icon="autorenew" />
        <StatCard label="Completed Today" value={String(stats.doneToday)} icon="check_circle" />
        <StatCard label="Failed" value={String(stats.failed)} icon="error" />
      </div>

      <div className="flex flex-wrap items-center gap-1 rounded bg-surface-container-low p-2 shadow-sm">
        {(["ALL", "Queued", "Running", "Done", "Failed"] as const).map((s) => (
          <button
            key={s}
            onClick={() => setFilter(s)}
            className={`rounded px-3 py-1.5 font-mono text-xs transition-colors ${
              filter === s
                ? "bg-surface-container-highest font-medium text-primary"
                : "text-on-surface-variant hover:text-on-surface"
            }`}
          >
            {s}
          </button>
        ))}
      </div>

      <div className="overflow-hidden rounded bg-surface-container-low shadow-sm">
        {runs === null ? (
          <div className="p-8 text-center text-sm text-on-surface-variant">Loading runs…</div>
        ) : filtered.length === 0 ? (
          <EmptyState hasAnyRuns={(runs?.length ?? 0) > 0} />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full border-collapse text-left">
              <thead>
                <tr className="bg-surface-container-lowest font-mono text-[11px] uppercase tracking-wider text-on-surface-variant">
                  <th className="px-4 py-2 font-medium">Run</th>
                  <th className="px-4 py-2 font-medium">Sample</th>
                  <th className="px-4 py-2 font-medium">Status</th>
                  <th className="px-4 py-2 font-medium">Started</th>
                  <th className="px-4 py-2 text-right font-medium">Variants</th>
                  <th className="px-4 py-2 text-right font-medium">Actions</th>
                </tr>
              </thead>
              <tbody className="text-sm text-on-surface">
                {pageRuns.map((run) => (
                  <tr key={run.id} className="border-t border-surface-container-high hover:bg-surface-container">
                    <td className="px-4 py-2.5 font-mono text-xs font-medium text-primary">RUN-{run.id}</td>
                    <td className="px-4 py-2.5">
                      <span className="flex items-center gap-1.5 font-mono text-xs">
                        <span className="material-symbols-outlined text-xs text-on-surface-variant">description</span>
                        <span className="max-w-[220px] truncate">{run.sampleFileName}</span>
                      </span>
                    </td>
                    <td className="px-4 py-2.5">
                      <StatusBadge status={run.status} />
                    </td>
                    <td className="px-4 py-2.5 font-mono text-xs text-on-surface-variant">
                      {timeAgo(run.startedAt ?? run.createdAt)}
                    </td>
                    <td className="px-4 py-2.5 text-right font-mono text-xs">
                      {run.totalVariants ?? (run.status === "Failed" ? "—" : "…")}
                    </td>
                    <td className="px-4 py-2.5 text-right">
                      <Link
                        to={`/runs/${run.id}`}
                        className="inline-flex items-center gap-1 font-mono text-xs text-primary hover:text-primary-fixed"
                      >
                        Inspect
                        <span className="material-symbols-outlined text-xs">arrow_forward</span>
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
            <Pagination page={page} pageSize={PAGE_SIZE} totalItems={filtered.length} onPageChange={setPage} />
          </div>
        )}
      </div>
    </div>
  );
}

function EmptyState({ hasAnyRuns }: { hasAnyRuns: boolean }) {
  return (
    <div className="mx-auto flex max-w-xl flex-col items-center justify-center space-y-3 p-12 text-center">
      <div className="flex h-14 w-14 items-center justify-center rounded-full bg-surface-container-highest text-primary shadow-sm">
        <span className="material-symbols-outlined text-2xl">biotech</span>
      </div>
      <h4 className="text-sm font-semibold text-on-surface">
        {hasAnyRuns ? "No runs match this filter" : "No runs yet"}
      </h4>
      <p className="text-sm text-on-surface-variant">
        Upload a sample FASTQ file to run it through Quality Check, Alignment, and Variant Calling.
      </p>
      {!hasAnyRuns && (
        <Link
          to="/new"
          className="mt-2 flex items-center gap-1.5 rounded bg-primary px-4 py-2 text-sm font-semibold text-on-primary shadow-md hover:bg-primary-fixed"
        >
          <span className="material-symbols-outlined text-sm">cloud_upload</span>
          Upload FASTQ Sample
        </Link>
      )}
    </div>
  );
}
