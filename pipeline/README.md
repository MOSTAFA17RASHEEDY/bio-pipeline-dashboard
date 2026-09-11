# Pipeline layer

A 3-step Nextflow DSL2 pipeline, each step running in its own small Docker
container, operating on a tiny synthetic DNA dataset.

```
data/reference.fasta ─┐
                       ├─▶ QUALITY_CHECK ──▶ qc_report.json, qc_summary.txt
data/sample_reads.fastq ┘
                       │
                       ├─▶ ALIGNMENT ──▶ aligned.sorted.bam (+.bai)
                       │        │
                       │        ▼
                       └─▶ VARIANT_CALLING ──▶ variants.vcf, variants_summary.json
```

## Concepts, quickly, since this is your first time with these tools

- **Nextflow** is a workflow orchestrator: you describe a pipeline as a
  handful of `process` blocks connected by data (see `main.nf` and
  `modules/*.nf`) and it handles running them, in the right order, with
  each one's declared inputs/outputs wired up automatically.
- **DSL2** is Nextflow's current syntax, where each step lives in its own
  reusable `process` and pipelines `include` them (see `modules/`) rather
  than defining everything in one big script.
- **Docker per step**: each process's `script:` block doesn't run on your
  machine directly — Nextflow launches it inside the Docker container named
  in `nextflow.config`, so the pipeline needs zero tools installed on the
  host beyond Docker and Nextflow itself. This also means the pipeline
  behaves identically on any machine.
- **`work/`**: Nextflow runs every task in its own isolated subdirectory
  under `work/` (hashed names, e.g. `work/3f/a91c2.../`) and only copies the
  files you declare in `publishDir` out to `results/`. If something goes
  wrong, the real place to debug is that task's `work/` folder — Nextflow
  prints the exact path when a task fails.

## One-time setup: build the 3 Docker images

Run from this `pipeline/` directory:

```bash
docker build -f docker/qc/Dockerfile           -t bio-pipeline/qc:latest .
docker build -f docker/align/Dockerfile        -t bio-pipeline/align:latest .
docker build -f docker/variant_call/Dockerfile -t bio-pipeline/variant-call:latest .
```

## Run the pipeline

```bash
nextflow run main.nf
```

Results land in `results/qc/`, `results/align/`, `results/variants/`.
Check `results/variants/variants_summary.json` for the headline number
(`total_variants`) and `results/variants/variants.vcf` for the raw calls.

To sanity-check correctness: `python scripts/generate_sample_data.py` prints
the 6 "ground truth" variants baked into the synthetic sample. The pipeline's
`variants.vcf` should report positions matching those (small position drift
of ±1-2bp around the insertion/deletion is expected and normal — different
aligners can anchor indels at slightly different equivalent positions when
there's ambiguity in exactly where a run of bases "starts").

## Regenerating the sample data

The committed `data/reference.fasta` / `data/sample_reads.fastq` were
produced by `scripts/generate_sample_data.py` with a fixed random seed, so
they're deterministic and already checked in — you don't need to regenerate
them. If you ever want a different (still deterministic) toy dataset, change
`REF_LENGTH` / the variant positions in that script and re-run it.

## Windows note

Nextflow needs a real POSIX/bash environment, which native Windows doesn't
provide, so it runs inside a WSL2 Ubuntu distro (`BioPipelineUbuntu`) rather
than PowerShell/CMD. See the repo root `PLAN.md` for how that distro was set
up and *why* (short version: Docker Desktop's Windows engine can't cleanly
serve as the Docker backend for a Nextflow process running inside WSL — path
translation breaks volume mounts — so this distro runs its own independent
Docker Engine instead).

That inner Docker Engine has no systemd/autostart, so after a fresh reboot
(or if `docker ps` below fails) start it first:

```bash
wsl -d BioPipelineUbuntu -- bash -c "nohup dockerd > /var/log/dockerd.log 2>&1 & disown; sleep 4; docker ps"
```

Then build the images and run the pipeline the same way as above, just
prefixed through WSL, e.g. from a Windows terminal (PowerShell/Git Bash):

```bash
wsl -d BioPipelineUbuntu -- bash -c 'cd "/mnt/e/Projects/Bioinformatics/Bio Pipeline Dashboard/pipeline" && nextflow run main.nf'
```

(adjust the `/mnt/e/...` path if your project lives on a different drive —
Windows drive `E:\` maps to `/mnt/e/` inside WSL).
