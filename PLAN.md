# Bio Pipeline Dashboard — Build Plan

A portfolio project combining a Nextflow bioinformatics pipeline with an
ASP.NET Core API and a React dashboard. This file tracks phases so work can
resume in a new session without re-deriving context.

## Decisions locked in
- **Everything free, no accounts needed** except the stretch-goal LLM step,
  which uses a **free Gemini API key** from https://aistudio.google.com/apikey
  (user will grab this themselves when we reach Phase 5; goes in a local
  `.env`, never committed).
- **UI design**: already generated via Stitch, saved in
  `stitch_bio_pipeline_dashboard/` (DESIGN.md = design system: dark
  "High-Density Technical Modernism" theme, teal/cyan accents, Inter +
  JetBrains Mono). 5 reference screens: dashboard/run history, new run
  upload, run detail (in progress), run detail (results), AI explanation
  drawer. **Copy will be adapted** to drop fictional GPU-cluster/production
  flourishes (this is a toy/demo pipeline, not a real cluster) while keeping
  the visual system as-is.
- **Windows + Nextflow**: Nextflow needs a real POSIX/bash environment, which
  Windows doesn't provide natively, and Docker-outside-of-Docker via named
  pipe doesn't work cleanly on Windows. Fix: install a real **WSL2 Ubuntu**
  distro (separate from Docker Desktop's internal `docker-desktop` distro)
  and run Nextflow inside it, using Docker Desktop's daemon (WSL integration)
  for containers. This was kicked off in the background — check status
  before continuing Phase 1 testing.

## Phase 0 — Environment setup
- [x] Confirm Docker Desktop installed & working (had to actually launch it —
      `Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe"`,
      it wasn't running despite being installed)
- [x] Confirm dotnet SDK (10.0.400), Node (24.14.0), npm (11.2.0) present
- [x] WSL2 Ubuntu distro created — **NOT via `wsl --install` (that got stuck
      indefinitely on the Microsoft Store path with zero output/progress,
      likely blocked on something needing interactive consent)**. Instead:
      `docker pull ubuntu:24.04` → `docker create`/`docker export` a
      container to `C:\wsl-setup\ubuntu-rootfs.tar` → `wsl --import
      BioPipelineUbuntu C:\wsl-setup\BioPipelineUbuntu C:\wsl-setup\ubuntu-rootfs.tar --version 2`.
      Distro name: **`BioPipelineUbuntu`**. Boots straight to root, no OOBE/
      username prompt needed (headless-friendly by construction).
- [x] Java + Nextflow installed inside it: `apt-get install -y default-jre-headless`,
      then `curl -s https://get.nextflow.io | bash` → moved to
      `/usr/local/bin/nextflow`. Nextflow 26.04.6 confirmed working.
- [x] **Docker-in-WSL decision, important — read before continuing**: tried
      pointing WSL at the *Windows* Docker Desktop engine two ways, both
      failed:
      1. Docker-outside-of-Docker via named-pipe mount from Windows side —
         pipe path errors, doesn't work on Windows.
      2. Symlinking WSL's `docker` → Windows `docker.exe` (interop trick) —
         `docker ps` and `docker build` worked, but `docker run` with
         volume mounts failed (`.command.sh: No such file or directory`)
         because Nextflow (running in Linux/WSL) generates Linux-style
         mount paths that the Windows `docker.exe` can't translate — real
         Docker Desktop WSL integration would handle this but is a GUI-only
         toggle with no config file we could find to set headlessly.
      **Resolution: installed a real, independent Docker Engine *inside*
      WSL** (`apt-get install -y docker.io`). This distro has no systemd
      (bare imported rootfs), so the daemon does **not** autostart — start
      it manually each time the WSL instance is fresh:
      ```
      wsl -d BioPipelineUbuntu -- bash -c "nohup dockerd > /var/log/dockerd.log 2>&1 & disown; sleep 4; docker ps"
      ```
      (Once started it should keep running as long as the WSL VM is up,
      independent of the Claude session — but check `docker ps` first on
      resume before assuming it's still there.)
      All 3 pipeline images must be built **inside this WSL Docker**, not
      the Windows one (they were built on the Windows engine first, that
      was wasted work — rebuild happens on the WSL engine instead).

## Phase 1 — Nextflow pipeline (standalone, no API/frontend yet)
- [x] Repo scaffolded: `pipeline/{data,modules,scripts,docker,results}`
- [x] `pipeline/scripts/generate_sample_data.py` — generates toy 600bp
      reference genome + 13 simulated FASTQ reads with 6 known baked-in
      variants (4 SNP, 1 INS, 1 DEL), seeded/deterministic
- [x] Sample data generated and committed:
      `pipeline/data/reference.fasta`, `pipeline/data/sample_reads.fastq`
- [x] `pipeline/scripts/run_qc.py` — mock QC step (read counts, length,
      GC%, mean Phred quality, low-quality read flags) → JSON + txt report.
      **Tested locally, works** (13 reads, 35.22 mean Phred, PASS).
- [x] `pipeline/scripts/call_variants.py` — variant caller using `pysam`:
      walks CIGAR via `get_aligned_pairs`, finds SNP/INS/DEL vs reference,
      requires ≥2 supporting reads, outputs real VCF + summary JSON.
      **Written, not yet tested** (needs a BAM file from the alignment step).
- [x] Alignment step: Dockerfile with `minimap2` + `samtools` (Debian apt
      packages), process runs `minimap2 -a -x sr ref.fasta reads.fastq |
      samtools sort -o aligned.sorted.bam` then `samtools index`
- [x] Dockerfiles for all 3 steps — written and content-correct:
  - [x] `pipeline/docker/qc/Dockerfile` (python:3.12-slim, stdlib only)
  - [x] `pipeline/docker/align/Dockerfile` (python:3.12-slim + apt minimap2/samtools)
  - [x] `pipeline/docker/variant_call/Dockerfile` (python:3.12-slim + pip pysam)
- [x] Nextflow DSL2 files — written, and the orchestration mechanics
      (channels/modules/parallel process scheduling) confirmed working:
  - [x] `pipeline/modules/qc.nf`
  - [x] `pipeline/modules/align.nf`
  - [x] `pipeline/modules/variant_call.nf`
  - [x] `pipeline/main.nf` (wires the 3 processes together)
  - [x] `pipeline/nextflow.config` — **has one required fix beyond the
        original plan**: added `process.stageInMode = 'copy'` because
        Nextflow's default symlink-based input staging fails with
        "Operation not permitted" on a Windows drive mounted into WSL2
        (`/mnt/e/...` doesn't support symlinks). Needed regardless of which
        Docker engine ends up being used.
- [~] Build all 3 Docker images — **done once already but on the wrong
      engine** (Windows Docker Desktop, from native Windows shell — this
      was before we discovered the WSL-Docker-in-Docker path problem).
      **On resume: rebuild them on the WSL-internal Docker engine** (start
      dockerd first, see Phase 0), was in progress via this command when
      the session paused:
      ```
      wsl -d BioPipelineUbuntu -- bash -c 'cd "/mnt/e/Projects/Bioinformatics/Bio Pipeline Dashboard/pipeline" && \
        docker build -f docker/qc/Dockerfile -t bio-pipeline/qc:latest . && \
        docker build -f docker/align/Dockerfile -t bio-pipeline/align:latest . && \
        docker build -f docker/variant_call/Dockerfile -t bio-pipeline/variant-call:latest . && \
        docker images bio-pipeline/*'
      ```
      **Check if this already finished** (`wsl -d BioPipelineUbuntu -- docker images bio-pipeline/*`)
      before rerunning — it may have completed after the session ended.
- [x] **Full pipeline run confirmed working end-to-end**, 2026-09-11. All 3
      processes (QUALITY_CHECK, ALIGNMENT, VARIANT_CALLING) completed ✔.
      `variants.vcf` / `variants_summary.json` matched all 6 ground-truth
      variants from `generate_sample_data.py` (4 SNP exact-position matches,
      INS/DEL matched with the expected small anchor-position drift from
      alignment ambiguity — documented in pipeline/README.md). QC correctly
      flagged the one deliberately-noisy read. **Phase 1 is functionally
      done.**
- [ ] Explain Nextflow/Docker concepts to the user (processes, channels,
      containers, DSL2 modules, work dirs) since they're new to both —
      partially done inline in chat; a fuller walkthrough could still help
      once they've seen it run

## Phase 2 — ASP.NET Core Web API — ✅ DONE (2026-09-11), tested end-to-end
- [x] Scaffolded `backend/` (minimal API style, .NET 10, `BioPipeline.Api`),
      added to `BioPipelineDashboard.sln` at repo root
- [x] EF Core + SQLite (`Microsoft.EntityFrameworkCore.Sqlite`), schema via
      `EnsureCreated()` (deliberate simplification over migrations for this
      project — see Program.cs comment)
- [x] `Run` + `Variant` entities (`backend/Models/`), `Run` has one-to-many
      `Variants`; `Status` stored as string via `HasConversion<string>()`
- [x] `POST /api/runs` — multipart upload (field name `file`), validates
      extension (.fastq/.fq/.fasta/.fa/.gz) and size (20MB cap), saves to
      `pipeline/api_runs/{id}/input/`, inserts a `Queued` Run row, enqueues
      onto `PipelineRunQueue` (an in-process `Channel<int>`)
- [x] `PipelineRunnerService` (`BackgroundService`, singleton) — single
      consumer processes one run at a time (so "Queued" is meaningful),
      shells out via `wsl.exe -d BioPipelineUbuntu -- bash -c "nextflow run
      main.nf -w ... --reads ... --outdir ..."`, streams stdout/stderr to
      `pipeline/api_runs/{id}/pipeline.log`, then on success parses
      `qc_report.json` + `variants_summary.json` into the DB (variants as
      child rows), on failure sets `Status=Failed` + `ErrorMessage`.
      Windows→WSL path translation lives in `Services/WslPath.cs`.
- [x] `GET /api/runs` — list, newest first
- [x] `GET /api/runs/{id}` — full detail incl. variant table
- [x] `GET /api/runs/{id}/vcf` — downloads the real VCF
- [x] `GET /api/runs/{id}/logs` — raw Nextflow log (backs the "Live
      STDOUT/STDERR" panel in the Stitch mockup)
- [x] CORS opened for `http://localhost:5173` (Vite dev server, Phase 3)
- [x] Swagger UI wired up (`/swagger`)
- [x] **Tested end-to-end via curl**: uploaded `pipeline/data/sample_reads.fastq`
      → run went Queued→Running→Done in ~10s → variant table matched Phase 1
      exactly (6 variants, 4 SNP/1 INS/1 DEL) → VCF download and logs both
      verified → bad-file-type upload correctly rejected with 400.
      **The base project's core loop (upload → pipeline → results) now
      works end-to-end**, just without a UI yet.

Run it: `cd backend && dotnet run --launch-profile http` → http://localhost:5179
(Swagger at `/swagger`). Requires WSL Docker daemon running — see Phase 0.

## Phase 3 — React dashboard — ✅ DONE (2026-09-11), tested end-to-end in a real browser
- [x] Scaffolded `frontend/` — Vite + React + TypeScript + React Router
- [x] Tailwind **v4** got installed (not v3) — different setup than the
      original plan assumed: no `tailwind.config.js`, tokens instead live in
      `src/index.css` under an `@theme` block. Ported DESIGN.md's colors +
      fonts (Inter/JetBrains Mono) there; spacing/radius needed no custom
      tokens since Tailwind's default numeric scale already matches
      DESIGN.md's scale almost exactly. App is fixed dark (no light-mode
      toggle) — matches the Stitch design system's intentional dark-first,
      always-on-console aesthetic.
- [x] Adapted copy from the Stitch mockups as planned: dropped fictional
      "Production Cluster" / GPU node health / NovaSeq flowcell flourishes,
      kept the visual system. Sidebar footer is honest about this being a
      portfolio project on synthetic data.
- [x] `src/api/client.ts` — typed fetch wrapper for all 5 backend endpoints
- [x] `src/hooks/useRunList.ts` / `useRun.ts` — polling hooks (4s / 3s),
      the latter auto-stops once a run reaches Done/Failed
- [x] Pages: `DashboardPage` (stats row + filterable run table + empty
      state), `NewRunPage` (drag-drop upload with client-side validation
      mirroring the backend's), `RunDetailPage` (step tracker, result
      stats, variant table, VCF download, collapsible log viewer)
- [x] `StepTracker`: **deliberately simplified vs. the mockup** — the
      backend doesn't track fine-grained per-step progress (Nextflow's
      stdout isn't parsed that closely), so instead of fabricating fake
      per-stage percentages, it honestly shows all 3 steps as a group
      while Running, with a caption explaining why. Once Done, each step
      shows real data (QC stats, variant counts) pulled from the API.
- [x] `VariantTable`: sortable + filterable by type (SNP/INS/DEL) + search
- [x] **Verified in a real headless-Chromium browser** (Playwright, since
      `chromium-cli` wasn't available in this environment): dashboard shows
      real run data, run detail page shows real QC + variant results
      matching the backend exactly, and a full upload interaction (pick
      file → submit → navigate to `/runs/{id}` → see live "Running" status)
      worked with **zero console errors**.

Run it: `cd frontend && npm run dev` → http://localhost:5173 (needs the
backend running at :5179 — see Phase 2).

## Phase 4 — Polish & wrap-up — ✅ DONE (2026-09-11)
- [x] Root `README.md` — architecture diagram, quick-start (all 3 services,
      in the right order), what each pipeline step does, tech stack, links
      to each layer's own README for depth
- [x] **Final full-flow sanity check, done fresh after everything (incl.
      Phase 5) was in place** — scripted a real browser through the entire
      product in one pass: Dashboard loads → New Run → upload real sample →
      wait for Queued→Running→Done → variant table renders → Explain with
      AI → VCF download resolves (HTTP 200). **Zero console errors, every
      step passed.** This is the project working as a whole, not just as
      separately-verified pieces.

## Project status: all 5 phases complete
Base project (Phases 1-4) and the stretch goal (Phase 5) are both done and
verified end-to-end. Nothing left on the original plan.

## Phase 5 — Stretch goal: AI explanation — ✅ DONE (2026-09-11), tested end-to-end
- [x] User provided a free Gemini API key from https://aistudio.google.com/apikey
- [x] Stored via `dotnet user-secrets` (backend/), never touches the repo —
      see backend/README.md for the full explanation + setup steps
- [x] `POST /api/runs/{id}/explain` (`Services/GeminiExplanationService.cs`)
      — builds a plain-language prompt from the run's QC + variant data,
      calls the Gemini REST API directly (no SDK needed for one endpoint),
      caches the result on `Run.AiExplanation` so repeat requests are free
- [x] **Model name hiccup, resolved live**: the originally-assumed model
      (`gemini-2.0-flash`) came back 404 "no longer available" — Gemini's
      own error message named the current replacement
      (`gemini-3.6-flash`), which is what's configured now
      (`appsettings.json` → `Gemini:Model`, easy to update again later the
      same way if it happens again)
- [x] Frontend: `AiExplanationDrawer.tsx` — slide-over panel adapted from
      `run_detail_ai_explanation_drawer/code.html`, with the fictional
      "BioMistral 7B"/COSMIC-augmentation framing dropped in favor of
      honestly labeling it "Google Gemini", plus a disclaimer footer
- [x] "Explain with AI" button added to `RunDetailPage` (Done runs only)
- [x] **Verified end-to-end in a real browser**: clicked the button, got a
      genuinely good, accurate plain-language explanation of the 6 real
      variants, confirmed the cache works (second request returns instantly
      with `cached: true`, no extra API call), zero console errors.

---
**Resuming a session?** Read this file first, then check the `[~]` and last
unchecked `[ ]` items above to see exactly where things left off.
