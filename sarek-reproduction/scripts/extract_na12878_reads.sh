#!/usr/bin/env bash
# Phase 2 step 1: pull real NA12878 reads for one small GRCh38 region out of the
# public 30x NYGC CRAM (streamed by byte-range from S3, no multi-GB download) and
# convert them to paired FASTQ for nf-core/sarek.
set -euo pipefail

WORK=/root/na12878-subset
CRAM="https://1000genomes.s3.amazonaws.com/1000G_2504_high_coverage/data/ERR3239334/NA12878.final.cram"
CRAI="https://1000genomes.s3.amazonaws.com/1000G_2504_high_coverage/data/ERR3239334/NA12878.final.cram.crai"
REGION="chr20:10000000-10200000"

mkdir -p "$WORK"
cd "$WORK"

# Local copy of the intact index (the ftp.sra.ebi.ac.uk mirror serves a
# truncated 4 KB one, which is what broke every earlier attempt).
if [ ! -s NA12878.final.cram.crai ] || [ "$(stat -c %s NA12878.final.cram.crai)" -lt 1000000 ]; then
  echo "[1/4] downloading intact .crai"
  curl -sSf -o NA12878.final.cram.crai "$CRAI"
fi
ls -la NA12878.final.cram.crai

echo "[2/4] extracting $REGION from remote CRAM"
samtools view -b -T chr20.fa -X "$CRAM" NA12878.final.cram.crai "$REGION" -o region.bam
samtools index region.bam
echo "alignment records: $(samtools view -c region.bam)"

echo "[3/4] converting to paired FASTQ"
samtools collate -u -O region.bam \
  | samtools fastq -1 NA12878_chr20_10M_R1.fastq.gz -2 NA12878_chr20_10M_R2.fastq.gz \
      -0 /dev/null -s /dev/null -n -
echo "R1 reads: $(zcat NA12878_chr20_10M_R1.fastq.gz | awk 'END{print NR/4}')"
echo "R2 reads: $(zcat NA12878_chr20_10M_R2.fastq.gz | awk 'END{print NR/4}')"
ls -la NA12878_chr20_10M_R1.fastq.gz NA12878_chr20_10M_R2.fastq.gz

echo "[4/4] writing sarek samplesheet"
cat > samplesheet.csv <<EOF
patient,sample,sex,status,lane,fastq_1,fastq_2
NA12878,NA12878,XX,0,L1,$WORK/NA12878_chr20_10M_R1.fastq.gz,$WORK/NA12878_chr20_10M_R2.fastq.gz
EOF
cat samplesheet.csv
echo "DONE"
