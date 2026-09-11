import type { RunDetail, RunListItem } from "../types";

const BASE_URL = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5179";

async function handle<T>(res: Response): Promise<T> {
  if (!res.ok) {
    const text = await res.text().catch(() => "");
    throw new Error(text || `Request failed: ${res.status} ${res.statusText}`);
  }
  // 204 No Content etc.
  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export const api = {
  listRuns: (): Promise<RunListItem[]> =>
    fetch(`${BASE_URL}/api/runs`).then((r) => handle(r)),

  getRun: (id: number): Promise<RunDetail> =>
    fetch(`${BASE_URL}/api/runs/${id}`).then((r) => handle(r)),

  createRun: (file: File): Promise<RunDetail> => {
    const form = new FormData();
    form.append("file", file);
    return fetch(`${BASE_URL}/api/runs`, { method: "POST", body: form }).then((r) => handle(r));
  },

  vcfDownloadUrl: (id: number): string => `${BASE_URL}/api/runs/${id}/vcf`,

  getLogs: (id: number): Promise<string> =>
    fetch(`${BASE_URL}/api/runs/${id}/logs`).then((r) => {
      if (!r.ok) throw new Error(`Failed to load logs: ${r.status}`);
      return r.text();
    }),

  explainRun: (id: number): Promise<{ explanation: string; cached: boolean }> =>
    fetch(`${BASE_URL}/api/runs/${id}/explain`, { method: "POST" }).then((r) => handle(r)),
};
