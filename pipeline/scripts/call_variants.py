#!/usr/bin/env python3
"""
call_variants.py -- Step 3 of the pipeline: Variant Calling

A deliberately simple variant caller: it walks each aligned read's CIGAR
string (via pysam) and compares it, base by base, to the reference
sequence. Any mismatch is a candidate SNP; any run of inserted/deleted
bases is a candidate insertion/deletion. Calls seen in at least
MIN_SUPPORTING_READS reads are kept (a crude stand-in for the statistical
confidence modelling a real caller like GATK HaplotypeCaller does) and
written out as a standard VCF file, plus a summary JSON the API/dashboard
can read without needing to parse VCF.

This is NOT a production-grade caller (no local reassembly, no proper
Bayesian genotype likelihoods, no repeat-region handling) -- it's built to
be readable and to demonstrate the core idea: align reads, find where they
disagree with the reference, report those positions.

Usage:
    python call_variants.py <reference.fasta> <aligned.sorted.bam> \
        <output.vcf> <output_summary.json>
"""
import json
import statistics
import sys
from collections import defaultdict

import pysam

MIN_SUPPORTING_READS = 2  # ignore single-read "variants" -- likely sequencing noise


def load_reference(fasta_path: str) -> tuple[str, str]:
    """Return (contig_name, sequence) for the (single) contig in the FASTA."""
    with pysam.FastxFile(fasta_path) as fh:
        record = next(iter(fh))
        return record.name, record.sequence.upper()


def collect_raw_calls(bam_path: str, ref_name: str, ref_seq: str):
    """Walk every aligned read and yield raw (pos, ref, alt, type, qual) calls,
    one per read per variant it supports. `pos` is 1-based, matching VCF."""
    coverage = [0] * len(ref_seq)

    with pysam.AlignmentFile(bam_path, "rb") as bam:
        for read in bam.fetch(ref_name):
            if read.is_unmapped:
                continue

            for p in range(read.reference_start, read.reference_end):
                coverage[p] += 1

            pairs = read.get_aligned_pairs(matches_only=False)
            seq = read.query_sequence
            quals = read.query_qualities  # list[int] Phred scores, or None
            last_ref_pos = None  # 0-based; last reference position walked past
            i, n = 0, len(pairs)

            while i < n:
                qpos, rpos = pairs[i]

                if qpos is not None and rpos is not None:
                    ref_base = ref_seq[rpos]
                    read_base = seq[qpos]
                    if read_base != ref_base:
                        q = quals[qpos] if quals else 30
                        yield rpos + 1, ref_base, read_base, "SNP", q
                    last_ref_pos = rpos
                    i += 1

                elif qpos is not None and rpos is None:
                    # insertion: consecutive query bases with no reference position
                    ins_bases, ins_quals = [], []
                    while i < n and pairs[i][1] is None and pairs[i][0] is not None:
                        ins_bases.append(seq[pairs[i][0]])
                        if quals:
                            ins_quals.append(quals[pairs[i][0]])
                        i += 1
                    if last_ref_pos is not None:
                        anchor = ref_seq[last_ref_pos]
                        q = round(statistics.mean(ins_quals)) if ins_quals else 30
                        yield last_ref_pos + 1, anchor, anchor + "".join(ins_bases), "INS", q

                elif qpos is None and rpos is not None:
                    # deletion: consecutive reference bases with no query base
                    del_bases, del_start = [], rpos
                    while i < n and pairs[i][0] is None and pairs[i][1] is not None:
                        del_bases.append(ref_seq[pairs[i][1]])
                        i += 1
                    if last_ref_pos is not None:
                        anchor = ref_seq[last_ref_pos]
                        yield (last_ref_pos + 1, anchor + "".join(del_bases), anchor, "DEL",
                               read.mapping_quality or 30)
                    last_ref_pos = del_start + len(del_bases) - 1

                else:
                    i += 1

    return coverage


def main() -> None:
    if len(sys.argv) != 5:
        print("Usage: call_variants.py <reference.fasta> <aligned.sorted.bam> "
              "<output.vcf> <output_summary.json>")
        sys.exit(1)

    ref_fasta, bam_path, vcf_out, summary_out = sys.argv[1:5]
    ref_name, ref_seq = load_reference(ref_fasta)

    # Two passes: first compute coverage (needs a full generator run), then
    # aggregate calls. Simplest correct approach: materialize the generator.
    calls = defaultdict(lambda: {"count": 0, "quals": []})
    coverage = [0] * len(ref_seq)

    with pysam.AlignmentFile(bam_path, "rb") as bam:
        for read in bam.fetch(ref_name):
            if read.is_unmapped:
                continue
            for p in range(read.reference_start, read.reference_end):
                coverage[p] += 1

    for pos, ref, alt, vtype, qual in collect_raw_calls(bam_path, ref_name, ref_seq):
        key = (pos, ref, alt, vtype)
        calls[key]["count"] += 1
        calls[key]["quals"].append(qual)

    variants = []
    for (pos, ref, alt, vtype), data in sorted(calls.items()):
        support = data["count"]
        if support < MIN_SUPPORTING_READS:
            continue
        depth = coverage[pos - 1] if pos - 1 < len(coverage) else support
        variants.append({
            "chrom": ref_name,
            "pos": pos,
            "ref": ref,
            "alt": alt,
            "type": vtype,
            "qual": round(statistics.mean(data["quals"]), 1),
            "depth": depth,
            "supporting_reads": support,
            "allele_frequency": round(support / depth, 3) if depth else 0.0,
        })

    # --- write VCF ---
    with open(vcf_out, "w") as f:
        f.write("##fileformat=VCFv4.2\n")
        f.write("##source=BioPipelineDashboardToyCaller-v1\n")
        f.write(f"##contig=<ID={ref_name},length={len(ref_seq)}>\n")
        f.write(f'##FILTER=<ID=PASS,Description="Supported by >= {MIN_SUPPORTING_READS} reads">\n')
        f.write('##INFO=<ID=DP,Number=1,Type=Integer,Description="Total read depth at position">\n')
        f.write('##INFO=<ID=AD,Number=1,Type=Integer,Description="Reads supporting the variant allele">\n')
        f.write('##INFO=<ID=AF,Number=1,Type=Float,Description="Allele frequency (AD/DP)">\n')
        f.write('##INFO=<ID=TYPE,Number=1,Type=String,Description="Variant type: SNP, INS, or DEL">\n')
        f.write("#CHROM\tPOS\tID\tREF\tALT\tQUAL\tFILTER\tINFO\n")
        for v in variants:
            info = f"DP={v['depth']};AD={v['supporting_reads']};AF={v['allele_frequency']};TYPE={v['type']}"
            f.write(f"{v['chrom']}\t{v['pos']}\t.\t{v['ref']}\t{v['alt']}\t{v['qual']}\tPASS\t{info}\n")

    # --- write summary JSON (what the API/dashboard actually consumes) ---
    summary = {
        "step": "variant_calling",
        "reference": ref_name,
        "reference_length": len(ref_seq),
        "min_supporting_reads": MIN_SUPPORTING_READS,
        "total_variants": len(variants),
        "counts_by_type": {
            t: sum(1 for v in variants if v["type"] == t) for t in ("SNP", "INS", "DEL")
        },
        "variants": variants,
    }
    with open(summary_out, "w") as f:
        json.dump(summary, f, indent=2)

    print(f"[VariantCall] {len(variants)} variant(s) called "
          f"({summary['counts_by_type']}) -> {vcf_out}")


if __name__ == "__main__":
    main()
