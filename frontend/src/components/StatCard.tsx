export function StatCard({
  label,
  value,
  icon,
  detail,
}: {
  label: string;
  value: string;
  icon: string;
  detail?: string;
}) {
  return (
    <div className="flex flex-col justify-between rounded bg-surface-container-low p-4 shadow-sm">
      <div className="flex items-center justify-between">
        <span className="font-mono text-[11px] uppercase tracking-wider text-on-surface-variant">{label}</span>
        <span className="material-symbols-outlined text-sm text-on-surface-variant">{icon}</span>
      </div>
      <div className="mt-3 text-2xl font-semibold tracking-tight text-on-surface">{value}</div>
      {detail && <div className="mt-1 font-mono text-[11px] text-on-surface-variant">{detail}</div>}
    </div>
  );
}
