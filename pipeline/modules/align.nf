process ALIGNMENT {
    tag "Alignment"
    publishDir "${params.outdir}/align", mode: 'copy'

    input:
    path reference  // reference.fasta
    path reads      // sample_reads.fastq

    output:
    path "aligned.sorted.bam", emit: bam
    path "aligned.sorted.bam.bai", emit: bai

    script:
    // -x sr = minimap2's preset tuned for short, high-accuracy reads (like
    // ours), as opposed to e.g. -x map-ont for noisy long reads.
    // samtools sort produces a coordinate-sorted BAM, which samtools index
    // then indexes -- both are required for pysam to do random-access reads
    // by position in the variant-calling step.
    """
    minimap2 -a -x sr ${reference} ${reads} > aligned.sam
    samtools sort -o aligned.sorted.bam aligned.sam
    samtools index aligned.sorted.bam
    """
}
