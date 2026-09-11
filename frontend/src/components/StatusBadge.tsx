import type { RunStatus } from "../types";

const STYLES: Record<RunStatus, string> = {
  Queued: "bg-surface-container-highest text-on-surface-variant",
  Running: "bg-surface-container-highest text-secondary",
  Done: "bg-surface-container-highest text-primary",
  Failed: "bg-error-container text-error",
};

export function StatusBadge({ status }: { status: RunStatus }) {
  return (
    <span
      className={`inline-flex items-center gap-1 rounded px-2 py-0.5 font-mono text-xs font-semibold ${STYLES[status]}`}
    >
      {status === "Running" && <span className="h-1.5 w-1.5 animate-pulse rounded-full bg-secondary" />}
      {status}
    </span>
  );
}
