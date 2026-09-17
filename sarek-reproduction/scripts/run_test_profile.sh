#!/usr/bin/env bash
# Runs nf-core/sarek's built-in test profile (Phase 1 of sarek-reproduction).
# Run from inside WSL (BioPipelineUbuntu), NOT from a native-filesystem path
# issue -- see README.md "Windows note" for why this must run from WSL's own
# home directory rather than /mnt/e/...
set -euo pipefail

WORKDIR="$HOME/sarek-work"
REPO_DIR="/mnt/e/Projects/Bioinformatics/Bio Pipeline Dashboard/sarek-reproduction"
mkdir -p "$WORKDIR"
cd "$WORKDIR"

# -c laptop.config overrides sarek's own test-profile memory cap (15 GB,
# see conf/test.config in the pipeline) down to what this machine's WSL2
# instance actually has -- see laptop.config for details.
nextflow run nf-core/sarek -r 3.10.0 -profile test,docker \
  -c "$REPO_DIR/laptop.config" \
  --outdir results/test_profile \
  -w work \
  2>&1 | tee run_test_profile.log
