import type { RunDetail } from "../types";

type StepState = "pending" | "active" | "done";

const STEPS = [
  { key: "qc", label: "01 · Quality Check", icon: "fact_check" },
  { key: "align", label: "02 · Alignment", icon: "sync_alt" },
  { key: "variants", label: "03 · Variant Calling", icon: "biotech" },
] as const;

function stateFor(run: RunDetail): StepState {
  if (run.status === "Done") return "done";
  if (run.status === "Queued") return "pending";
  return "active"; // Running (or Failed mid-flight, shown generically below)
}

export function StepTracker({ run }: { run: RunDetail }) {
  const state = stateFor(run);

  const details: Record<(typeof STEPS)[number]["key"], string | undefined> = {
    qc:
      state === "done" && run.qcTotalReads != null
        ? `${run.qcTotalReads} reads · mean Q${run.qcMeanQuality} · ${run.qcPassRatePercent}% pass (${run.qcStatus})`
        : undefined,
    align: state === "done" ? "Reads aligned to reference with minimap2 + samtools" : undefined,
    variants:
      state === "done" && run.totalVariants != null
        ? `${run.totalVariants} variants — ${run.snpCount} SNP, ${run.insCount} INS, ${run.delCount} DEL`
        : undefined,
  };

  return (
    <div className="rounded bg-surface-container-low p-4 shadow-sm">
      <div className="mb-3 flex items-center justify-between">
        <h3 className="text-sm font-semibold text-on-surface">Pipeline steps</h3>
        {run.status === "Running" && (
          <span className="font-mono text-[11px] text-on-surface-variant">
            Steps run automatically, in order — this demo doesn't track fine-grained per-step progress
          </span>
        )}
      </div>

      <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
        {STEPS.map((step) => {
          const stepState: StepState = run.status === "Failed" ? "pending" : state;
          return (
            <div
              key={step.key}
              className={`rounded border p-3 ${
                stepState === "done"
                  ? "border-surface-container-high bg-surface-container"
                  : stepState === "active"
                    ? "border-primary/40 bg-primary/5"
                    : "border-dashed border-outline-variant"
              }`}
            >
              <div className="flex items-center gap-2">
                <span
                  className={`material-symbols-outlined text-base ${
                    stepState === "done"
                      ? "text-primary"
                      : stepState === "active"
                        ? "animate-pulse text-secondary"
                        : "text-on-surface-variant"
                  }`}
                >
                  {stepState === "done" ? "check_circle" : step.icon}
                </span>
                <span
                  className={`text-sm font-medium ${
                    stepState === "pending" ? "text-on-surface-variant" : "text-on-surface"
                  }`}
                >
                  {step.label}
                </span>
              </div>
              {details[step.key] && (
                <p className="mt-2 font-mono text-[11px] leading-snug text-on-surface-variant">
                  {details[step.key]}
                </p>
              )}
            </div>
          );
        })}
      </div>

      {run.status === "Failed" && run.errorMessage && (
        <div className="mt-3 rounded border border-error/40 bg-error-container/20 p-3 font-mono text-xs text-error">
          {run.errorMessage}
        </div>
      )}
    </div>
  );
}
