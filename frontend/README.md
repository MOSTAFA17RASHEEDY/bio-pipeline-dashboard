# Frontend — React dashboard

A Vite + React + TypeScript app that talks to the backend API (`../backend`)
to trigger and monitor pipeline runs. Design is ported from the Stitch
screens in `../stitch_bio_pipeline_dashboard/` — same dark "High-Density
Technical Modernism" color system and typography, with copy adapted to be
honest about this being a small demo pipeline (the original mockups had
some fictional "production cluster" flourishes that don't apply here).

## Run it

```bash
cd frontend
npm install   # first time only
npm run dev
```

Opens on `http://localhost:5173`. Needs the backend running at
`http://localhost:5179` (see `../backend/README.md`) — configurable via
`.env` (`VITE_API_BASE_URL`).

## Structure

```
src/
  api/client.ts        typed fetch wrapper for the 5 backend endpoints
  types.ts              mirrors backend/Dtos/RunDtos.cs
  hooks/
    useRunList.ts        polls GET /api/runs every 4s
    useRun.ts             polls GET /api/runs/{id} every 3s, stops at Done/Failed
  components/
    Layout.tsx            sidebar + top bar shell
    StatusBadge.tsx        Queued/Running/Done/Failed pill
    StepTracker.tsx        3-step pipeline progress (see note below)
    VariantTable.tsx       sortable/filterable variant results table
    StatCard.tsx
  pages/
    DashboardPage.tsx      run list + stats
    NewRunPage.tsx          drag-drop upload
    RunDetailPage.tsx      status, step tracker, results, logs
```

## Why the step tracker doesn't show live per-step progress

The Stitch mockup shows a step tracker mid-alignment with a live percentage
("Stage 2/3: BWA-MEM2 (68%)"). This app doesn't fabricate that: the backend
only knows a run's overall status (Queued/Running/Done/Failed), not which of
the 3 Nextflow steps is currently executing or how far along it is — getting
that would mean parsing Nextflow's live log output line-by-line, which this
project doesn't do. So while a run is `Running`, all 3 steps are shown
together with a caption explaining that; once `Done`, each step shows its
real recorded data (QC stats, variant counts) from the database.

## Tailwind v4 note

`npm install tailwindcss` currently installs v4, which works differently
from the v3 setup the original Stitch export assumes (no
`tailwind.config.js`; theme tokens live in `src/index.css` under an
`@theme { ... }` block, wired up via the `@tailwindcss/vite` plugin in
`vite.config.ts`). The color/font tokens from `DESIGN.md` are ported there;
spacing/border-radius needed no overrides since Tailwind's default scale
already matches DESIGN.md's values closely.
