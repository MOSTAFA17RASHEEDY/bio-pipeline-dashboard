# Step 3: Variant Calling
#
# pysam (the standard Python bindings for htslib, the library underneath
# samtools/bcftools) does the heavy lifting of parsing the BAM file's CIGAR
# alignments; call_variants.py uses it to compare reads against the
# reference and call SNPs/insertions/deletions.
#
# Build from the `pipeline/` directory:
#   docker build -f docker/variant_call/Dockerfile -t bio-pipeline/variant-call:latest .
FROM python:3.12-slim

RUN pip install --no-cache-dir pysam

COPY scripts/call_variants.py /opt/scripts/call_variants.py

WORKDIR /data
