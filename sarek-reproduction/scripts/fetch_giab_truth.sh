#!/usr/bin/env bash
# Phase 3 step 1: pull the GIAB NA12878 (HG001) GRCh38 v4.2.1 ground-truth
# calls and high-confidence regions for the same 200 kb window used in Phase 2.
set -euo pipefail

WORK=/root/na12878-subset
GIAB="https://ftp-trace.ncbi.nlm.nih.gov/ReferenceSamples/giab/release/NA12878_HG001/NISTv4.2.1/GRCh38"
REGION="chr20:10000000-10200000"
cd "$WORK"

echo "[1/3] truth VCF: remote tabix slice of $REGION"
# Keep only the chr20 contig line in the header so hap.py's reference
# checks don't trip over the 21 chromosomes we don't have in chr20.fa.
tabix -h "$GIAB/HG001_GRCh38_1_22_v4.2.1_benchmark.vcf.gz" "$REGION" \
  | awk '/^##contig=/ { if ($0 ~ /ID=chr20,/) print; next } { print }' \
  | bgzip -c > giab_truth_chr20_10M.vcf.gz
tabix -p vcf giab_truth_chr20_10M.vcf.gz
echo "truth variants in region: $(zcat giab_truth_chr20_10M.vcf.gz | grep -vc '^#')"
echo "  SNVs:   $(zcat giab_truth_chr20_10M.vcf.gz | grep -v '^#' | awk 'length($4)==1 && length($5)==1' | wc -l)"
echo "  indels: $(zcat giab_truth_chr20_10M.vcf.gz | grep -v '^#' | awk 'length($4)!=1 || length($5)!=1' | wc -l)"

echo "[2/3] high-confidence BED: download and clip to the region"
curl -sSf -o giab_confident_full.bed "$GIAB/HG001_GRCh38_1_22_v4.2.1_benchmark.bed"
# BED is 0-based half-open; region 10,000,000-10,200,000 (1-based) = 9999999-10200000.
awk 'BEGIN{OFS="\t"} $1=="chr20" && $3>9999999 && $2<10200000 {
       s=($2<9999999)?9999999:$2; e=($3>10200000)?10200000:$3; print $1,s,e }' \
  giab_confident_full.bed > giab_confident_chr20_10M.bed
rm -f giab_confident_full.bed
echo "confident intervals in region: $(wc -l < giab_confident_chr20_10M.bed)"
echo "confident bases in region:     $(awk '{s+=$3-$2} END{print s}' giab_confident_chr20_10M.bed) of 200001"

echo "[3/3] query VCF: copy the Phase 2 Strelka calls next to the truth"
cp /root/sarek-work-na12878/results/variant_calling/strelka/NA12878/NA12878.strelka.variants.vcf.gz     query_strelka_chr20_10M.vcf.gz
cp /root/sarek-work-na12878/results/variant_calling/strelka/NA12878/NA12878.strelka.variants.vcf.gz.tbi query_strelka_chr20_10M.vcf.gz.tbi
echo "query variants: $(zcat query_strelka_chr20_10M.vcf.gz | grep -vc '^#')"
ls -la giab_truth_chr20_10M.vcf.gz giab_confident_chr20_10M.bed query_strelka_chr20_10M.vcf.gz
echo "DONE"
