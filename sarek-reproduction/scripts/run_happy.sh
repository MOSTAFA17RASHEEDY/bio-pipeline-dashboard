#!/usr/bin/env bash
# Phase 3 step 2: score the Phase 2 Strelka calls against the GIAB truth set
# with hap.py (Illumina's haplotype-aware comparison tool, the standard used
# by GIAB/precisionFDA). Runs in the biocontainers hap.py image.
set -euo pipefail

WORK=/root/na12878-subset
IMG="quay.io/biocontainers/hap.py:0.3.14--py27h5c5a3ab_0"
cd "$WORK"

for f in giab_truth_chr20_10M.vcf.gz giab_confident_chr20_10M.bed query_strelka_chr20_10M.vcf.gz chr20.fa chr20.fa.fai target_chr20_10M.bed; do
  [ -s "$f" ] || { echo "missing $WORK/$f" >&2; exit 1; }
done
mkdir -p happy

# -f : where truth is confident (FPs only count inside these)
# -R : restrict the whole comparison to our 200 kb window
docker run --rm -v "$WORK":/data -w /data "$IMG" \
  hap.py giab_truth_chr20_10M.vcf.gz query_strelka_chr20_10M.vcf.gz \
    -r chr20.fa \
    -f giab_confident_chr20_10M.bed \
    -R target_chr20_10M.bed \
    -o happy/NA12878_chr20_10M \
    --threads 4 \
  2>&1 | tail -n 25

echo
echo "=== summary.csv ==="
tr ',' '\t' < happy/NA12878_chr20_10M.summary.csv
echo "DONE"
