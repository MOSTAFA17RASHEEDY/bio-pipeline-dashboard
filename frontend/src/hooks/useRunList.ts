import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { RunListItem } from "../types";

const POLL_MS = 4000;

/** Loads the run list, then quietly re-fetches on an interval so statuses
 * (Queued -> Running -> Done) update without the user refreshing the page. */
export function useRunList() {
  const [runs, setRuns] = useState<RunListItem[] | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;

    const load = () => {
      api
        .listRuns()
        .then((data) => {
          if (!cancelled) {
            setRuns(data);
            setError(null);
          }
        })
        .catch((err) => {
          if (!cancelled) setError(err instanceof Error ? err.message : String(err));
        });
    };

    load();
    const interval = setInterval(load, POLL_MS);
    return () => {
      cancelled = true;
      clearInterval(interval);
    };
  }, []);

  return { runs, error, loading: runs === null && error === null };
}
