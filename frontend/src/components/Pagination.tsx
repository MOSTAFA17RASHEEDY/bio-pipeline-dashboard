// Shared client-side pagination control -- both call sites (VariantTable,
// DashboardPage's run list) already have their full filtered/sorted array
// in memory (the backend returns full lists in one call), so this just
// slices locally rather than adding page/pageSize query params to the API.

export function Pagination({
  page,
  pageSize,
  totalItems,
  onPageChange,
}: {
  page: number;
  pageSize: number;
  totalItems: number;
  onPageChange: (page: number) => void;
}) {
  const totalPages = Math.max(1, Math.ceil(totalItems / pageSize));
  if (totalPages <= 1) return null;

  const start = totalItems === 0 ? 0 : (page - 1) * pageSize + 1;
  const end = Math.min(page * pageSize, totalItems);

  return (
    <div className="flex flex-wrap items-center justify-between gap-2 border-t border-surface-container-high px-4 py-2.5">
      <span className="font-mono text-[11px] text-on-surface-variant">
        Showing {start}-{end} of {totalItems}
      </span>
      <div className="flex items-center gap-1">
        <button
          type="button"
          disabled={page <= 1}
          onClick={() => onPageChange(page - 1)}
          className="flex items-center gap-1 rounded px-2 py-1 font-mono text-xs text-on-surface-variant transition-colors hover:text-on-surface disabled:cursor-not-allowed disabled:opacity-40"
        >
          <span className="material-symbols-outlined text-xs">chevron_left</span>
          Prev
        </button>
        <span className="px-2 font-mono text-xs text-on-surface-variant">
          Page {page} of {totalPages}
        </span>
        <button
          type="button"
          disabled={page >= totalPages}
          onClick={() => onPageChange(page + 1)}
          className="flex items-center gap-1 rounded px-2 py-1 font-mono text-xs text-on-surface-variant transition-colors hover:text-on-surface disabled:cursor-not-allowed disabled:opacity-40"
        >
          Next
          <span className="material-symbols-outlined text-xs">chevron_right</span>
        </button>
      </div>
    </div>
  );
}
