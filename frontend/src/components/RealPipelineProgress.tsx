import type { RunDetail } from "../types";
import { StatCard } from "./StatCard";

// Deliberately a separate component from StepTracker, not a variant of it:
// StepTracker's whole point is honestly saying "we don't track fine-grained
// progress" for the toy pipeline (see its own comment), which stays true for
// that pipeline. This component exists because, for the real pipeline, we
// genuinely do parse live progress out of Nextflow's own output -- see
// backend/Services/SarekProgressParser.cs.

function formatEta(seconds: number | null): string {
  if (seconds == null) return "Estimating…";
  if (seconds <= 0) return "Almost done";
  const m = Math.floor(seconds / 60);
  const s = seconds % 60;
  return m > 0 ? `~${m}m ${s}s remaining` : `~${s}s remaining`;
}

export function RealPipelineProgress({ run }: { run: RunDetail }) {
  const percent = Math.round(run.progressPercent ?? 0);
  const stage = run.currentStage ?? "Starting up (pulling containers, preparing inputs)…";

  return (
    <div className="rounded bg-surface-container-low p-4 shadow-sm">
      <div className="mb-3 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-on-surface">Real pipeline run — nf-core/sarek</h3>
        <span className="font-mono text-[11px] text-on-surface-variant">
          Genuine ~6-10 min compute, not simulated
        </span>
      </div>

      <div className="h-2 w-full overflow-hidden rounded-full bg-surface-container-high">
        <div
          className="h-full rounded-full bg-primary transition-all duration-500"
          style={{ width: `${percent}%` }}
        />
      </div>
      <p className="mt-2 font-mono text-[11px] text-on-surface-variant">{stage}</p>

      <div className="mt-4 grid grid-cols-1 gap-3 sm:grid-cols-3">
        <StatCard label="Progress" value={`${percent}%`} icon="progress_activity" />
        <StatCard
          label="Steps"
          value={run.stepsTotal ? `${run.stepsCompleted ?? 0} of ${run.stepsTotal}` : "—"}
          icon="checklist"
        />
        <StatCard label="ETA" value={formatEta(run.etaSecondsRemaining)} icon="schedule" />
      </div>
    </div>
  );
}
