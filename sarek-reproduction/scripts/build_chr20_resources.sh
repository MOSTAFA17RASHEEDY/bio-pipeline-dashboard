#!/usr/bin/env bash
# Phase 2 step 2: build the chr20-only reference resources nf-core/sarek needs
# for a real-data run without pulling the multi-GB genome-wide bundles.
# Everything is sliced by range from the Broad's public GRCh38 resource bucket.
set -euo pipefail

WORK=/root/na12878-subset
BROAD="https://storage.googleapis.com/gcp-public-data--broad-references/hg38/v0"
cd "$WORK"

echo "[1/4] reference index + dict"
[ -s chr20.fa ] || samtools faidx "$BROAD/Homo_sapiens_assembly38.fasta" chr20 > chr20.fa
samtools faidx chr20.fa
samtools dict chr20.fa -o chr20.dict
cat chr20.fa.fai

echo "[2/4] known indels for BQSR, sliced to chr20 (Mills + 1000G gold standard)"
tabix -h "$BROAD/Mills_and_1000G_gold_standard.indels.hg38.vcf.gz" chr20 | bgzip -c > mills_chr20.vcf.gz
tabix -p vcf mills_chr20.vcf.gz
echo "mills records: $(zcat mills_chr20.vcf.gz | grep -vc '^#')"

echo "[3/4] known indels for BQSR, sliced to chr20 (Broad known_indels)"
tabix -h "$BROAD/Homo_sapiens_assembly38.known_indels.vcf.gz" chr20 | bgzip -c > known_indels_chr20.vcf.gz
tabix -p vcf known_indels_chr20.vcf.gz
echo "known_indels records: $(zcat known_indels_chr20.vcf.gz | grep -vc '^#')"

echo "[4/4] target-region BED (restricts variant calling to where the reads are)"
printf 'chr20\t9999999\t10200000\n' > target_chr20_10M.bed
cat target_chr20_10M.bed

ls -la
echo "DONE"
