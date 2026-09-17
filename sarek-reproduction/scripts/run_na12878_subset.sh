#!/usr/bin/env bash
# Phase 2 step 3: run nf-core/sarek on the real NA12878 chr20 subset.
# Unlike Phase 1 there is no `-profile test`: the reference, known-sites and
# intervals are all supplied explicitly (built by build_chr20_resources.sh),
# and the reads come from extract_na12878_reads.sh.
set -euo pipefail

WORK=/root/na12878-subset
LAUNCH=/root/sarek-work-na12878
REPO_DIR="/mnt/e/Projects/Bioinformatics/Bio Pipeline Dashboard/sarek-reproduction"

for f in samplesheet.csv chr20.fa chr20.fa.fai chr20.dict mills_chr20.vcf.gz known_indels_chr20.vcf.gz target_chr20_10M.bed; do
  [ -s "$WORK/$f" ] || { echo "missing $WORK/$f" >&2; exit 1; }
done

mkdir -p "$LAUNCH"
cd "$LAUNCH"

# Detached so it survives the invoking WSL session ending (see README).
# known_indels(_tbi) are schema type "file-path-pattern": multiple files are
# given as a glob (quoted so the shell doesn't expand it), not a comma list.
setsid nohup nextflow run nf-core/sarek -r 3.10.0 -profile docker \
  -c "$REPO_DIR/laptop.config" \
  -c "$REPO_DIR/na12878_subset.config" \
  --input "$WORK/samplesheet.csv" \
  --outdir results \
  --fasta "$WORK/chr20.fa" \
  --fasta_fai "$WORK/chr20.fa.fai" \
  --dict "$WORK/chr20.dict" \
  --known_indels "$WORK/*_chr20.vcf.gz" \
  --known_indels_tbi "$WORK/*_chr20.vcf.gz.tbi" \
  --intervals "$WORK/target_chr20_10M.bed" \
  --tools strelka \
  -w work -resume \
  > run_na12878_subset.log 2>&1 < /dev/null &
disown
sleep 5
echo "launched; nextflow processes:"
ps -ef | grep "[n]extflow-26" | awk '{print $2, $8, $9, $10}' | head -3
