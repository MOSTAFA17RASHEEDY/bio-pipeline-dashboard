# Step 1: Quality Check
#
# Deliberately minimal: just Python's standard library, no compiled
# dependencies, so this image builds in seconds and stays a few MB on top
# of the python:3.12-slim base.
#
# Build from the `pipeline/` directory so this can COPY the script in:
#   docker build -f docker/qc/Dockerfile -t bio-pipeline/qc:latest .
FROM python:3.12-slim

COPY scripts/run_qc.py /opt/scripts/run_qc.py

WORKDIR /data
