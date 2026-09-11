# Step 2: Alignment
#
# minimap2 aligns the sample reads against the reference genome; samtools
# sorts and indexes the resulting BAM so it's ready for the variant caller.
# Both are real, widely-used bioinformatics tools available straight from
# Debian's package repos -- no need to build from source.
#
# Build from the `pipeline/` directory:
#   docker build -f docker/align/Dockerfile -t bio-pipeline/align:latest .
FROM python:3.12-slim

RUN apt-get update && \
    apt-get install -y --no-install-recommends minimap2 samtools && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /data
