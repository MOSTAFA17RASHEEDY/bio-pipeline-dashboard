#!/usr/bin/env python3
"""
run_qc.py -- Step 1 of the pipeline: Quality Check

A deliberately simple, dependency-free stand-in for a tool like FastQC.
Real FastQC needs a JVM and produces an HTML report meant for a human to
click through; here we just want structured numbers a web API can store
and a dashboard can render, so we compute the handful of stats that
actually matter for a toy dataset:

  - read count & length stats
  - GC content
  - average Phred quality per read (parsed from the FASTQ "+" quality line)
  - how many reads fall below a quality threshold

Usage:
    python run_qc.py <input.fastq> <output_report.json> <output_summary.txt>
"""
import json
import statistics
import sys

QUALITY_THRESHOLD = 20  # Phred score below this is considered "low quality"


def phred_quality_scores(qual_line: str) -> list[int]:
    # FASTQ quality strings are Phred+33 (Illumina 1.8+) encoded: subtract 33
    # from each character's ASCII code to get the Phred score.
    return [ord(ch) - 33 for ch in qual_line]


def parse_fastq(path: str):
    """Yield (name, sequence, quality_string) for each read in a FASTQ file."""
    with open(path) as f:
        while True:
            name = f.readline().strip()
            if not name:
                break
            seq = f.readline().strip()
            f.readline()  # the "+" separator line, unused
            qual = f.readline().strip()
            yield name, seq, qual


def main() -> None:
    if len(sys.argv) != 4:
        print("Usage: run_qc.py <input.fastq> <output_report.json> <output_summary.txt>")
        sys.exit(1)

    fastq_path, report_json_path, summary_txt_path = sys.argv[1:4]

    reads = list(parse_fastq(fastq_path))
    if not reads:
        print(f"No reads found in {fastq_path}", file=sys.stderr)
        sys.exit(1)

    lengths = [len(seq) for _, seq, _ in reads]
    gc_counts = [seq.count("G") + seq.count("C") for _, seq, _ in reads]
    total_bases = sum(lengths)
    gc_percent = round(100 * sum(gc_counts) / total_bases, 2) if total_bases else 0.0

    per_read_mean_quality = []
    low_quality_reads = []
    for name, seq, qual in reads:
        scores = phred_quality_scores(qual)
        mean_q = statistics.mean(scores) if scores else 0
        per_read_mean_quality.append(mean_q)
        if mean_q < QUALITY_THRESHOLD:
            low_quality_reads.append({"read": name, "mean_quality": round(mean_q, 1)})

    overall_mean_quality = round(statistics.mean(per_read_mean_quality), 2)
    pass_rate = round(100 * (len(reads) - len(low_quality_reads)) / len(reads), 1)

    report = {
        "step": "quality_check",
        "input_file": fastq_path,
        "total_reads": len(reads),
        "read_length": {
            "min": min(lengths),
            "max": max(lengths),
            "mean": round(statistics.mean(lengths), 1),
        },
        "gc_content_percent": gc_percent,
        "mean_phred_quality": overall_mean_quality,
        "quality_threshold": QUALITY_THRESHOLD,
        "pass_rate_percent": pass_rate,
        "low_quality_reads": low_quality_reads,
        "status": "PASS" if pass_rate >= 80 else "WARN",
    }

    with open(report_json_path, "w") as f:
        json.dump(report, f, indent=2)

    with open(summary_txt_path, "w") as f:
        f.write("=== Quality Check Summary ===\n")
        f.write(f"Input:            {fastq_path}\n")
        f.write(f"Total reads:      {report['total_reads']}\n")
        f.write(f"Read length:      {report['read_length']['min']}-{report['read_length']['max']} "
                f"(mean {report['read_length']['mean']})\n")
        f.write(f"GC content:       {gc_percent}%\n")
        f.write(f"Mean Phred score: {overall_mean_quality}\n")
        f.write(f"Pass rate:        {pass_rate}% (threshold Q{QUALITY_THRESHOLD})\n")
        f.write(f"Status:           {report['status']}\n")
        if low_quality_reads:
            f.write(f"\n{len(low_quality_reads)} low-quality read(s) flagged:\n")
            for r in low_quality_reads:
                f.write(f"  - {r['read']} (mean Q{r['mean_quality']})\n")

    print(f"[QC] {report['total_reads']} reads, {overall_mean_quality} mean Phred, "
          f"status={report['status']}")


if __name__ == "__main__":
    main()
