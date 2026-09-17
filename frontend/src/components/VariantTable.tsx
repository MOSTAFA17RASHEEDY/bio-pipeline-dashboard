import { useEffect, useMemo, useState } from "react";
import type { Variant } from "../types";
import { Pagination } from "./Pagination";

type SortKey = "position" | "type" | "qual" | "depth" | "alleleFrequency";
const PAGE_SIZE = 25;

const TYPE_STYLES: Record<Variant["type"], string> = {
  SNP: "bg-secondary/15 text-secondary",
  INS: "bg-primary/15 text-primary",
  DEL: "bg-error/15 text-error",
};

export function VariantTable({ variants }: { variants: Variant[] }) {
  const [filter, setFilter] = useState<"ALL" | Variant["type"]>("ALL");
  const [sortKey, setSortKey] = useState<SortKey>("position");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);

  const counts = useMemo(
    () => ({
      ALL: variants.length,
      SNP: variants.filter((v) => v.type === "SNP").length,
      INS: variants.filter((v) => v.type === "INS").length,
      DEL: variants.filter((v) => v.type === "DEL").length,
    }),
    [variants],
  );

  const rows = useMemo(() => {
    let result = variants;
    if (filter !== "ALL") result = result.filter((v) => v.type === filter);
    if (search.trim()) {
      const q = search.trim().toLowerCase();
      result = result.filter(
        (v) => String(v.position).includes(q) || v.ref.toLowerCase().includes(q) || v.alt.toLowerCase().includes(q),
      );
    }
    return [...result].sort((a, b) => {
      if (sortKey === "type") return a.type.localeCompare(b.type);
      return (b[sortKey] as number) - (a[sortKey] as number) || a.position - b.position;
    });
  }, [variants, filter, search, sortKey]);

  // Reset to page 1 whenever the filtered/sorted set changes shape (new
  // filter, new search term, or the underlying variants themselves change
  // e.g. after a run finishes) so we never land on a now-empty page.
  useEffect(() => {
    setPage(1);
  }, [filter, search, sortKey, variants]);

  const pageRows = useMemo(
    () => rows.slice((page - 1) * PAGE_SIZE, page * PAGE_SIZE),
    [rows, page],
  );

  if (variants.length === 0) {
    return (
      <div className="rounded bg-surface-container-low p-8 text-center text-sm text-on-surface-variant shadow-sm">
        No variants found in this sample relative to the reference.
      </div>
    );
  }

  return (
    <div className="overflow-hidden rounded bg-surface-container-low shadow-sm">
      <div className="flex flex-col gap-2 border-b border-surface-container-high p-3 sm:flex-row sm:items-center sm:justify-between">
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Filter by position or base..."
          className="rounded bg-surface-container-lowest px-3 py-1.5 text-sm text-on-surface placeholder:text-on-surface-variant/60 focus:outline-none focus:ring-1 focus:ring-primary"
        />
        <div className="flex flex-wrap items-center gap-1">
          {(["ALL", "SNP", "INS", "DEL"] as const).map((t) => (
            <button
              key={t}
              onClick={() => setFilter(t)}
              className={`rounded px-2 py-1 font-mono text-xs transition-colors ${
                filter === t
                  ? "bg-surface-container-highest font-medium text-primary"
                  : "text-on-surface-variant hover:text-on-surface"
              }`}
            >
              {t} ({counts[t]})
            </button>
          ))}
          <select
            value={sortKey}
            onChange={(e) => setSortKey(e.target.value as SortKey)}
            className="ml-2 rounded bg-surface-container-lowest px-2 py-1 font-mono text-xs text-on-surface"
          >
            <option value="position">Sort: Position</option>
            <option value="qual">Sort: Quality</option>
            <option value="depth">Sort: Depth</option>
            <option value="alleleFrequency">Sort: Allele Freq.</option>
          </select>
        </div>
      </div>

      <div className="overflow-x-auto">
        <table className="w-full border-collapse text-left">
          <thead>
            <tr className="bg-surface-container-lowest font-mono text-[11px] uppercase tracking-wider text-on-surface-variant">
              <th className="px-4 py-2 font-medium">Position</th>
              <th className="px-4 py-2 font-medium">Ref</th>
              <th className="px-4 py-2 font-medium">Alt</th>
              <th className="px-4 py-2 font-medium">Type</th>
              <th className="px-4 py-2 text-right font-medium">Qual</th>
              <th className="px-4 py-2 text-right font-medium">Depth</th>
              <th className="px-4 py-2 text-right font-medium">Reads</th>
              <th className="px-4 py-2 text-right font-medium">Allele Freq.</th>
            </tr>
          </thead>
          <tbody className="text-sm text-on-surface">
            {pageRows.map((v) => (
              <tr
                key={`${v.chrom}-${v.position}-${v.alt}`}
                className="border-t border-surface-container-high transition-colors hover:bg-surface-container"
              >
                <td className="px-4 py-2 font-mono text-xs text-primary">
                  {v.chrom}:{v.position}
                </td>
                <td className="px-4 py-2 font-mono text-xs">{v.ref}</td>
                <td className="px-4 py-2 font-mono text-xs">{v.alt}</td>
                <td className="px-4 py-2">
                  <span className={`rounded px-1.5 py-0.5 font-mono text-[11px] font-medium ${TYPE_STYLES[v.type]}`}>
                    {v.type}
                  </span>
                </td>
                <td className="px-4 py-2 text-right font-mono text-xs">{v.qual}</td>
                <td className="px-4 py-2 text-right font-mono text-xs">{v.depth}x</td>
                <td className="px-4 py-2 text-right font-mono text-xs">{v.supportingReads}</td>
                <td className="px-4 py-2 text-right font-mono text-xs">{Math.round(v.alleleFrequency * 100)}%</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      <Pagination page={page} pageSize={PAGE_SIZE} totalItems={rows.length} onPageChange={setPage} />
    </div>
  );
}
