import { useEffect, useState } from "react";
import { api } from "../api/client";
import type { RunDetail } from "../types";

const POLL_MS = 3000;

/** Loads one run's detail, then keeps polling while it's still Queued/Running
 * and stops automatically once it reaches Done or Failed. */
export function useRun(id: number) {
  const [run, setRun] = useState<RunDetail | null>(null);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    let cancelled = false;
    let interval: ReturnType<typeof setInterval> | undefined;

    const load = () => {
      api
        .getRun(id)
        .then((data) => {
          if (cancelled) return;
          setRun(data);
          setError(null);
          if ((data.status === "Done" || data.status === "Failed") && interval) {
            clearInterval(interval);
          }
        })
        .catch((err) => {
          if (!cancelled) setError(err instanceof Error ? err.message : String(err));
        });
    };

    load();
    interval = setInterval(load, POLL_MS);
    return () => {
      cancelled = true;
      if (interval) clearInterval(interval);
    };
  }, [id]);

  return { run, error };
}
