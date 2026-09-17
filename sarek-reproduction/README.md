# nf-core/sarek reproduction

This folder is **separate from** the toy pipeline in [`../pipeline/`](../pipeline/README.md).
That one is a small, synthetic, from-scratch Nextflow pipeline built to learn
the concepts. This folder instead runs the **real, industry-standard**
variant-calling pipeline — [nf-core/sarek](https://nf-co.re/sarek) — on its
own built-in test data, and then on a small real-world data subset, to prove
the same environment (WSL2 + Docker) can run production-grade bioinformatics
software, not just a custom toy.

## What is nf-core?

[nf-core](https://nf-co.re) is a community project that maintains a large
collection of Nextflow pipelines for common bioinformatics tasks (variant
calling, RNA-seq, methylation, etc.), all built to a shared standard so any
of them can be run the same way, on any machine, with reproducible results.
Being "an nf-core pipeline" is a real quality signal in the field — every
pipeline is peer-reviewed, versioned, and tested before release.

A few terms used throughout this folder:

- **Pipeline** — the whole workflow (here, `nf-core/sarek`), pulled straight
  from GitHub by Nextflow (`nextflow run nf-core/sarek -r <version>`) rather
  than written by hand like `../pipeline/`.
- **Modules** — nf-core pipelines are built from a shared library of small,
  single-tool building blocks (e.g. "run `bwa-mem2`", "run `GATK
  MarkDuplicates`"), each wrapped in its own Docker/Singularity container.
  This is the same `process`-per-step idea as `../pipeline/modules/*.nf`,
  just at a much bigger scale (sarek uses 50+ modules).
- **Subworkflows** — reusable groups of modules chained together (e.g. "the
  standard GATK best-practices BAM preprocessing steps"), shared across
  multiple nf-core pipelines so each one doesn't reinvent them.
- **Profiles** — named bundles of configuration you turn on with `-profile`.
  `docker` says "run every process in its Docker container"; `test` swaps
  in tiny built-in test data and low resource limits so the whole pipeline
  can run on a laptop in minutes, purely to prove the pipeline mechanics
  work before pointing it at real data.
- **Params** — pipeline options, passed as `--paramName value` (e.g.
  `--outdir`, `--input`, `--tools`). Every nf-core pipeline documents its
  params on its nf-co.re page.
- **Samplesheet** — a CSV file describing what samples to run and where
  their FASTQ/BAM files are (columns like `patient`, `sample`, `lane`,
  `fastq_1`, `fastq_2`). This replaces hand-wiring file paths into the
  pipeline — you just point `--input` at the CSV.

## What sarek actually does

Sarek is a **germline and somatic variant-calling pipeline**: given raw
sequencing reads (FASTQ) for one or more samples, it produces a list of
genetic variants (a VCF), following the same GATK best-practices workflow
real clinical/research labs use:

```
FASTQ ─▶ align (bwa-mem2) ─▶ mark duplicates + base recalibration (GATK4)
      ─▶ variant calling (Strelka2 / GATK HaplotypeCaller / DeepVariant / ...)
      ─▶ annotation (VEP / snpEff) ─▶ QC report (FastQC, MultiQC)
```

This is the same shape as `../pipeline/` (align → call variants → report),
just using real, published tools instead of a hand-written toy aligner and
caller, and covering many more edge cases (multiple callers, tumor/normal
pairs, base quality recalibration, annotation).

## Environment

Same WSL2 (`BioPipelineUbuntu`) + Docker setup as the rest of this repo —
see the root [`PLAN.md`](../PLAN.md) for how that was set up. Additionally
installed for this folder:

- Python 3.12 + `pipx` (`apt-get install python3 python3-pip python3-venv pipx`)
- [`nf-core/tools`](https://github.com/nf-core/tools) — the official nf-core
  command-line helper (pipeline listing/download/linting/etc.), installed
  via `pipx install nf-core`

```bash
wsl -d BioPipelineUbuntu -- nf-core --version
```

## Phase 1 — proving the environment works: the built-in test profile

Before touching any real data, nf-core pipelines ship a `test` config
profile: tiny, fake-but-realistic data bundled with the pipeline, and low
resource limits, so you can confirm your machine/Docker setup can actually
run the pipeline before spending time/bandwidth on real data.

```bash
wsl -d BioPipelineUbuntu -- bash -c 'cd "/mnt/e/Projects/Bioinformatics/Bio Pipeline Dashboard/sarek-reproduction" && \
  nextflow run nf-core/sarek -r 3.10.0 -profile test,docker --outdir results/test_profile -w work'
```

**Resource notes (checked before running):** this machine's WSL2 instance
has ~7.7 GB RAM and 250+ GB free disk. The `test` profile caps each
process's resource request low enough to fit that, but the first run still
needs to download the pipeline's ~15-20 Docker images (one per tool used —
bwa-mem2, GATK4, Strelka2, MultiQC, etc.), roughly 5-10 GB total, plus a
small (a few hundred MB) built-in test dataset. Expect the first run to take
15-40 minutes, mostly image download time.

**Result: pass.** Completed successfully on 2026-09-16. Full command used
(after also lowering resource limits, see `laptop.config`):

```bash
nextflow run nf-core/sarek -r 3.10.0 -profile test,docker \
  -c laptop.config --outdir results/test_profile -w work -resume
```

- **Runtime:** ~50 min wall-clock total across two attempts (the run was
  interrupted partway through when the launching WSL shell session ended,
  killing the un-detached Nextflow process — see "Windows/WSL note" below;
  the resumed run reused cached steps and took 24m 11s to finish).
- **Verified pipeline stages ran for real, end-to-end:** FastQC → BWA-MEM
  alignment → GATK4 MarkDuplicates → GATK4 BaseRecalibrator/ApplyBQSR →
  Strelka2 germline variant calling → bcftools/vcftools VCF stats →
  MultiQC report.
- **Outputs:** `results/test_profile/{preprocessing,variant_calling,
  reports,multiqc,csv,pipeline_info}` inside the WSL work area
  (`~/sarek-work` — see "Windows/WSL note" below for why it's not run
  from `/mnt/e/...` directly).
- **Resource use:** 0.3 CPU-hours, peak 4 CPUs / 6 GB memory (matching
  the `laptop.config` cap), 32.8% of tasks served from cache on the
  resumed run.

**Windows/WSL note:** don't launch long Nextflow runs with
`wsl -d BioPipelineUbuntu -- bash -c '...'` directly in the foreground —
the process dies silently (no error) if that invoking session ends before
the pipeline finishes. Launch detached instead:
`setsid nohup nextflow ... > run.log 2>&1 < /dev/null & disown`, then
verify with `pgrep -af nextflow`. Use `-resume` to continue after any
interruption rather than restarting from scratch.

## Phase 2 — a real data subset

Phase 1 proved the machinery works on toy data; this phase feeds the same
pipeline **real sequencing reads** from NA12878 — the most-studied human
genome, with a public ground-truth variant set (GIAB, used in Phase 3).
Nothing is downloaded whole: every input is sliced by byte-range out of
public GRCh38 resources, so the whole subset is a few MB.

| Input | Source | How it's sliced |
|---|---|---|
| Reads | NA12878 30x WGS (NYGC, run `ERR3239334`), GRCh38 CRAM on the public 1000 Genomes S3 bucket | `samtools view` on the remote CRAM for `chr20:10,000,000-10,200,000`, then `samtools collate \| fastq` → 23,700 read pairs (~35x) |
| Reference | Broad `Homo_sapiens_assembly38.fasta` | `samtools faidx <url> chr20` → `chr20.fa` + `.fai` + `.dict` |
| Known indels (BQSR) | Broad Mills+1000G gold standard, Broad `known_indels` | `tabix <url> chr20` → 27,572 + 40,406 records |
| Intervals | — | one-line BED covering the read region, so variant calling doesn't scan all of chr20 |

Scripts, in order: [`scripts/build_chr20_resources.sh`](scripts/build_chr20_resources.sh),
[`scripts/extract_na12878_reads.sh`](scripts/extract_na12878_reads.sh),
[`scripts/run_na12878_subset.sh`](scripts/run_na12878_subset.sh). Unlike
Phase 1 there is no `-profile test`; the reference, known-sites and
intervals are passed explicitly (`--fasta ... --known_indels ...
--intervals ...`, plus [`na12878_subset.config`](na12878_subset.config)
which sets `igenomes_ignore = true` / `genome = null`), which is how sarek
is run on any non-iGenomes reference. Two schema details that bit on the
first attempt: `--known_indels` takes a **glob** for multiple files (it is
schema type `file-path-pattern`), not a comma-separated list; and boolean
flags are safest set in a config file, since a bare `--igenomes_ignore`
reached the validator as the string `"true"`.

**Gotcha found on the way:** the ENA/SRA mirror
(`ftp.sra.ebi.ac.uk/vol1/run/ERR323/ERR3239334/`) serves a **truncated 4 KB
`.crai`** for this CRAM (the real index is 1.38 MB). Region queries against
it fail with a misleading `zlib_mem_inflate` / "only works for indexed
files" error. The S3 copy
(`1000genomes.s3.amazonaws.com/1000G_2504_high_coverage/data/ERR3239334/`)
has the intact index; `extract_na12878_reads.sh` downloads that one and
passes it with `-X`.

**Result: pass.** Completed 2026-09-16, **6m 33s**, 24/24 tasks succeeded
(fastp → BWA-MEM → MarkDuplicates → BaseRecalibrator/ApplyBQSR → Strelka2
→ bcftools/vcftools stats → MultiQC). The first launch failed schema
validation in under a minute on the two parameter details noted above;
the second launch ran clean.

What the reports say about the data — all numbers are what you'd expect
from good 30x Illumina WGS, which is the point: real reads, real answers.

| Metric | Value | Source |
|---|---|---|
| Reads in / mapped / properly paired | 47,238 / 47,229 (99.98%) / 99.9% | `reports/samtools/NA12878/NA12878.recal.cram.stats` |
| Read length / insert size | 150 bp / 440.8 bp mean | same |
| Duplicate rate | 10.65% (2,514 of 23,610 pairs; 420 optical) | `reports/markduplicates/NA12878/*.metrics` |
| Mismatch rate after BQSR | 0.37% | samtools stats `error rate` |
| Mean depth over the 200 kb target | **31.6x** (6,316,514 covered bases / 200,001 bp; max 65x) | `reports/mosdepth/NA12878/NA12878.recal.mosdepth.summary.txt` |
| Variants called (Strelka2) | **464** — 376 SNVs, 88 indels, 7 multiallelic | `reports/bcftools/strelka/NA12878/` |
| Passing filters | 448 PASS, 16 filtered (`LowGQX`) | `FILTER` column of the VCF |
| Ts/Tv | 1.91 | bcftools stats |

Sanity-check reading: ~2.3 variants per kb and a Ts/Tv near 2 are the
textbook values for a human genome, and 31.6x is the 30x dataset minus
duplicates and trimming. Phase 3 replaces this "looks right" reading with
a precision/recall number against the GIAB truth set.

Outputs live in the WSL work area (`~/sarek-work-na12878/results/`,
same layout as Phase 1). Two small artifacts are copied into this repo for
viewing from Windows:
[`results/na12878_subset/NA12878.strelka.variants.vcf.gz`](results/na12878_subset/)
(+ `.tbi`) and
[`results/na12878_subset/multiqc_report.html`](results/na12878_subset/multiqc_report.html).

## Phase 3 — validating against ground truth (GIAB + hap.py)

Phase 2 ended with "the numbers look textbook". That's a smell test, not a
measurement. NA12878 is special because the **Genome in a Bottle (GIAB)**
consortium at NIST has published a ground-truth call set for it — variants
established by combining many sequencing technologies and pipelines — plus
a BED of the regions where that truth is considered reliable. Comparing
our calls against it gives real precision and recall.

| Input | Source | How it's sliced |
|---|---|---|
| Truth VCF | GIAB HG001 (NA12878) GRCh38 benchmark **v4.2.1** (NIST) | `tabix` on the remote VCF for `chr20:10,000,000-10,200,000` → 402 truth variants (334 SNVs, 68 indels) |
| High-confidence BED | same release | downloaded (15 MB) and clipped to the window → 33 intervals covering 194,250 of 200,001 bp (97.1%) |
| Query VCF | Phase 2 Strelka2 output | copied as-is (464 records, 448 PASS) |

The comparison tool is **hap.py** (Illumina), the standard scorer used by
GIAB and precisionFDA. It is *haplotype-aware*: rather than demanding an
exact textual match, it checks whether truth and query describe the same
haplotype sequence, so e.g. an indel written left-aligned in one file and
right-aligned in the other still counts as a match. Run from the
biocontainers image via Docker; `-f` gives the confident BED (false
positives only count inside it), `-R` restricts everything to the window.

Scripts: [`scripts/fetch_giab_truth.sh`](scripts/fetch_giab_truth.sh),
[`scripts/run_happy.sh`](scripts/run_happy.sh).

**Result (2026-09-17), PASS calls only:**

| Type | Truth | TP | FN | FP | Unscored (outside confident BED) | Recall | Precision | F1 |
|---|---|---|---|---|---|---|---|---|
| SNV | 325 | 325 | 0 | 0 | 39 | **1.000** | **1.000** | **1.000** |
| Indel | 60 | 59 | 1 | 1 | 25 | **0.983** | **0.984** | **0.984** |

How to read it:

- **Every SNV GIAB knows about in this window was called, and nothing
  false was added.** That is the headline, and it is in line with what
  Strelka2 on 30x Illumina data scores genome-wide in published
  benchmarks (SNV F1 ≈ 0.99+).
- Indels are the harder class for every caller; one miss and one extra
  out of 60 is likewise typical. hap.py flags the extra as an *allele*
  mismatch (`FP.al = 1`) rather than a genotype error — see the site note
  below.
- "Truth" here is 385 (325 + 60), not the 402 raw records, because hap.py
  first restricts to the confident BED (`-f`) and normalises overlapping /
  multi-allelic records.
- The 64 unscored calls sit in the ~3% of the window GIAB does *not* vouch
  for — repetitive sequence where every caller produces noisy calls and
  no truth exists either way. Excluding them is the standard convention,
  not a loophole.

**The one imperfect site.** The indel FN and FP are the *same* locus,
`chr20:10,153,762`, an `AAAG` short tandem repeat:

| | REF | ALT | Genotype | Meaning |
|---|---|---|---|---|
| GIAB truth | `AAAAGAAAG` | `AAAAG`, `A` | `2/1` | one haplotype lost one repeat unit, the other lost two |
| Strelka2 | `AAAAGAAAG` | `A` | `1/1` | both haplotypes lost two units |

Strelka got one allele right (the two-unit deletion is real) and missed the
one-unit deletion on the other haplotype — so hap.py scores the truth
record as unmatched (FN) and the query record as an allele mismatch (FP).
Repeat-unit slippage in short tandem repeats is *the* canonical hard case
for short-read indel calling; finding exactly one of these in 60 indels
is a normal result, not a pipeline defect.

Full output: [`results/na12878_subset/NA12878_chr20_10M.summary.csv`](results/na12878_subset/NA12878_chr20_10M.summary.csv)
(the table above) and `NA12878_chr20_10M.extended.csv` (per-subtype and
per-genotype breakdown); hap.py's annotated VCF, with a per-record
TP/FN/FP decision, is at `~/na12878-subset/happy/` in WSL.

**Windows/WSL note (again):** `dockerd` has to be started by hand in this
distro (no init system), and it does not survive a WSL restart — the first
hap.py attempt failed with "cannot connect to the Docker daemon" for that
reason. `setsid nohup dockerd > /var/log/dockerd.log 2>&1 &` brings it
back; a `[boot] command=` line in `/etc/wsl.conf` would make it automatic.
