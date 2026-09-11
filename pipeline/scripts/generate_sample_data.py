#!/usr/bin/env python3
"""
generate_sample_data.py

Creates the toy "reference genome" and a simulated sample FASTQ read set
for the Bio Pipeline Dashboard portfolio project.

This is intentionally tiny and fake -- a few hundred base pairs, not a real
chromosome -- so the whole pipeline runs in seconds and the Docker images
stay small. It exists purely so we have deterministic, reproducible input
data with KNOWN variants baked in, which makes it easy to sanity-check that
the variant-calling step actually works correctly.

Run once (outputs are committed to the repo so the pipeline works out of
the box):

    python pipeline/scripts/generate_sample_data.py
"""
import random

random.seed(42)  # deterministic -> same "reference genome" every time

BASES = "ACGT"
REF_LENGTH = 600
READ_LENGTH = 100
READ_STEP = 40  # overlap between consecutive reads, like real sequencing coverage

OUT_DIR = "pipeline/data"


def random_sequence(length: int) -> str:
    return "".join(random.choice(BASES) for _ in range(length))


def introduce_variants(seq: str) -> tuple[str, list[dict]]:
    """Mutate a copy of `seq` at a handful of known positions and return
    (mutated_sequence, list_of_variants_introduced) so we know the ground
    truth the variant caller should find."""
    seq = list(seq)
    variants = []

    # A few single-nucleotide substitutions (SNPs)
    for pos in (75, 210, 330, 480):
        ref_base = seq[pos]
        alt_base = random.choice([b for b in BASES if b != ref_base])
        seq[pos] = alt_base
        variants.append({"pos": pos + 1, "ref": ref_base, "alt": alt_base, "type": "SNP"})

    # A small insertion (3 bases) after position 150
    ins_pos = 150
    inserted = "GAT"
    seq[ins_pos:ins_pos] = list(inserted)
    variants.append({"pos": ins_pos + 1, "ref": seq[ins_pos - 1] if ins_pos > 0 else "N",
                      "alt": (seq[ins_pos - 1] if ins_pos > 0 else "N") + inserted, "type": "INS"})

    # A small deletion (2 bases) around position 400 (before insertion shift is irrelevant,
    # we do deletion on the already-inserted list to keep offsets simple for humans reading this)
    del_pos = 400
    deleted = "".join(seq[del_pos:del_pos + 2])
    anchor = seq[del_pos - 1]
    del seq[del_pos:del_pos + 2]
    variants.append({"pos": del_pos, "ref": anchor + deleted, "alt": anchor, "type": "DEL"})

    return "".join(seq), variants


def simulate_reads(sample_seq: str) -> list[tuple[str, str, str]]:
    """Slice the mutated sequence into overlapping "reads" and assign each
    a fake but plausible Phred quality string. A few reads get deliberately
    lower quality so the QC step has something real to flag."""
    reads = []
    read_id = 0
    pos = 0
    while pos + READ_LENGTH <= len(sample_seq):
        read_seq = sample_seq[pos:pos + READ_LENGTH]
        read_id += 1

        # Most reads: high quality (Phred ~35-40). Every 7th read: lower quality
        # (Phred ~15-25) to simulate a bit of realistic noise for the QC step.
        if read_id % 7 == 0:
            quals = [random.randint(12, 24) for _ in range(READ_LENGTH)]
        else:
            quals = [random.randint(33, 40) for _ in range(READ_LENGTH)]
        qual_str = "".join(chr(q + 33) for q in quals)  # Phred+33 (Illumina 1.8+) encoding

        name = f"@READ_{read_id:03d} pos={pos + 1}"
        reads.append((name, read_seq, qual_str))
        pos += READ_STEP
    return reads


def write_fasta(path: str, header: str, seq: str, wrap: int = 70) -> None:
    with open(path, "w") as f:
        f.write(f">{header}\n")
        for i in range(0, len(seq), wrap):
            f.write(seq[i:i + wrap] + "\n")


def write_fastq(path: str, reads: list[tuple[str, str, str]]) -> None:
    with open(path, "w") as f:
        for name, seq, qual in reads:
            f.write(f"{name}\n{seq}\n+\n{qual}\n")


def main() -> None:
    reference_seq = random_sequence(REF_LENGTH)
    sample_seq, ground_truth_variants = introduce_variants(reference_seq)
    reads = simulate_reads(sample_seq)

    write_fasta(f"{OUT_DIR}/reference.fasta", "toy_chromosome_1 synthetic reference", reference_seq)
    write_fastq(f"{OUT_DIR}/sample_reads.fastq", reads)

    print(f"Wrote {OUT_DIR}/reference.fasta ({len(reference_seq)} bp)")
    print(f"Wrote {OUT_DIR}/sample_reads.fastq ({len(reads)} reads, {READ_LENGTH}bp each)")
    print("\nGround-truth variants baked into the sample (for sanity-checking the caller):")
    for v in ground_truth_variants:
        print(f"  pos={v['pos']:<4} {v['type']:<3} ref={v['ref']:<5} alt={v['alt']}")


if __name__ == "__main__":
    main()
