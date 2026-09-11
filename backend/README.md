# Backend — ASP.NET Core Web API

Wraps the Nextflow pipeline (see `../pipeline/README.md`) in a web API: upload
a sample, it runs in the background, poll for status, read back results.

## How a run flows through the system

```
POST /api/runs (multipart file)
        │
        ▼
  Run row inserted, Status=Queued ──▶ file saved to pipeline/api_runs/{id}/input/
        │
        ▼
  PipelineRunQueue (in-memory queue)
        │
        ▼
  PipelineRunnerService (background worker, one run at a time)
        │  Status=Running
        │  shells out: wsl.exe -d BioPipelineUbuntu -- nextflow run main.nf
        │              --reads ... --outdir pipeline/api_runs/{id}/results
        ▼
  on exit 0: parse qc_report.json + variants_summary.json → DB, Status=Done
  on exit ≠0: Status=Failed, ErrorMessage set
```

Only one run executes at a time by design (`PipelineRunnerService` is a
single consumer of the queue) — a second upload sits in `Queued` until the
first finishes, which is also what makes that status meaningful in the
dashboard rather than decorative.

## Endpoints

| Method | Path | What it does |
|---|---|---|
| POST | `/api/runs` | Upload a sample (`file` field), starts a run |
| GET | `/api/runs` | List runs, newest first |
| GET | `/api/runs/{id}` | Full detail: status, QC summary, variant table |
| GET | `/api/runs/{id}/vcf` | Download the raw VCF |
| GET | `/api/runs/{id}/logs` | Raw Nextflow stdout/stderr for that run |

Interactive docs at `/swagger` once running.

## Run it

```bash
cd backend
dotnet run --launch-profile http
```

Listens on `http://localhost:5179`. Needs the WSL Docker daemon up first
(see repo root `PLAN.md` Phase 0) — `PipelineRunnerService` will fail runs
with a clear error message in `ErrorMessage` / the logs endpoint if it isn't.

The SQLite database (`biopipeline.db`) and the `pipeline/api_runs/` folder
(uploaded inputs + per-run results/logs) are created automatically on first
run and are gitignored.

## Stretch goal: AI explanation (Gemini)

`POST /api/runs/{id}/explain` sends a completed run's variant summary to
Google's Gemini API and returns a short, plain-language explanation
(cached on the Run row after the first call, so re-requesting it is free).
This is the project's one and only external network call — everything else
runs entirely on your machine.

**Setup** (one-time, local only):

1. Get a free API key from https://aistudio.google.com/apikey (just a
   Google login, no credit card).
2. From the `backend/` directory, store it via .NET's user-secrets — the
   `.env`-equivalent for .NET: a JSON file kept *outside* the repo entirely
   (under your Windows user profile), so there's no risk of ever committing
   it, unlike putting it in appsettings.json:
   ```bash
   dotnet user-secrets init          # already done for this project — run only if starting fresh
   dotnet user-secrets set "Gemini:ApiKey" "<your key>"
   ```
3. Restart the API. `GeminiOptions.Model` (in `appsettings.json`, not a
   secret) controls which model is called — currently `gemini-3.6-flash`.
   If Gemini ever deprecates that model, the API's own error message names
   the current replacement (that's literally how this project found the
   right model name — an earlier attempt with an older model name came back
   404 with the current one in the error text).

If the key isn't configured, the endpoint returns a clear error explaining
how to set it, rather than failing silently.

## Why EF Core `EnsureCreated()` instead of migrations

This is a deliberate simplification: for a single-developer SQLite database
in a learning project, `EnsureCreated()` (create the schema from the current
model, once) is simpler than managing migration files, at the cost of not
supporting incremental schema evolution. A real multi-developer project
would use `dotnet ef migrations add` instead.
