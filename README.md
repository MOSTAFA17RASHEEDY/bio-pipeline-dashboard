# Bio Pipeline Dashboard

A small portfolio project combining a real bioinformatics pipeline with a
web app around it: upload a DNA sample, watch it run through Quality Check
→ Alignment → Variant Calling, and see the results (plus, optionally, a
plain-language AI explanation of what was found).

This is a **learning project**, not a production system — the "biology" is
small, synthetic, toy data (a 600-letter fake genome with known differences
deliberately planted in it), chosen so the whole thing runs in seconds and
stays easy to reason about end to end. The tools and techniques (Nextflow,
Docker, a real variant caller using `pysam`, a real VCF file format) are
the genuine article, just scaled down.

## Architecture

```
┌─────────────┐     upload      ┌──────────────────┐   triggers    ┌──────────────────┐
│   React     │ ───────────────▶│  ASP.NET Core     │──────────────▶│    Nextflow        │
│  Dashboard  │◀─────────────── │  Web API          │◀────────────── │  (DSL2 pipeline)   │
│ (frontend/) │   poll status/  │   (backend/)      │  reads result  │   (pipeline/)      │
└─────────────┘    results      └──────────────────┘     JSON files  └──────────────────┘
                          │                                                  │
                          │ optional: variant summary                       │ 3 Docker containers
                          ▼                                                  ▼
                   ┌─────────────┐                                   QC → Alignment → Variant Calling
                   │   Gemini    │                                  (FastQC-like → minimap2/samtools →
                   │   API       │                                        pysam-based caller)
                   └─────────────┘
```

Three independent layers, each with its own README covering how it works
and how to run it on its own:

| Layer | What it is | Details |
|---|---|---|
| [`pipeline/`](pipeline/README.md) | Nextflow DSL2 pipeline, 3 Docker containers | toy data, Nextflow/Docker concepts explained |
| [`backend/`](backend/README.md) | ASP.NET Core Web API + SQLite | job queue, endpoints, Gemini setup |
| [`frontend/`](frontend/README.md) | React + TypeScript dashboard | pages, design system notes |

`PLAN.md` is the build log — every phase, decision, and problem hit along
the way, kept for anyone (including a future me) picking this project back
up later.

## Quick start

Three things need to be running at once. Order matters — pipeline
infrastructure first, then backend, then frontend.

**1. WSL Docker daemon** (pipeline execution needs this — see
`pipeline/README.md` for why):
```bash
wsl -d BioPipelineUbuntu -- bash -c "nohup dockerd > /var/log/dockerd.log 2>&1 & disown; sleep 4; docker ps"
```

**2. Backend API:**
```bash
cd backend
dotnet run --launch-profile http
```
→ http://localhost:5179 (Swagger at `/swagger`)

**3. Frontend:**
```bash
cd frontend
npm install   # first time only
npm run dev
```
→ http://localhost:5173

Then open the frontend, upload `pipeline/data/sample_reads.fastq` (the
committed toy sample) via **New Run**, and watch it go
Queued → Running → Done.

## What each step actually does

1. **Quality Check** — checks the sample's read count, length, GC content,
   and per-read sequencing confidence (Phred quality scores); flags
   anything below a trust threshold.
2. **Alignment** — `minimap2` figures out where each short DNA read best
   matches against the reference genome; `samtools` sorts and indexes the
   result.
3. **Variant Calling** — walks each aligned read's letters against the
   reference (via `pysam`) and reports every place they disagree — single
   letter swaps (SNPs), insertions, and deletions — as long as multiple
   independent reads agree it's real. Output is a standard VCF file.

(Full plain-language walkthrough of both the biology and the code, phase
by phase, is in the conversation history / can be re-explained on request —
this README stays terse on purpose.)

## Tech stack

- **Pipeline**: Nextflow (DSL2), Docker, `minimap2`, `samtools`, Python (`pysam`)
- **Backend**: ASP.NET Core (.NET 10, minimal APIs), EF Core + SQLite
- **Frontend**: React, TypeScript, Vite, Tailwind CSS v4, React Router
- **AI (optional)**: Google Gemini API

Everything is free and runs locally — no accounts needed except an optional
free Gemini key for the AI explanation feature (see `backend/README.md`).
