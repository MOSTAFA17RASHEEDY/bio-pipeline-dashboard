process VARIANT_CALLING {
    tag "VariantCalling"
    publishDir "${params.outdir}/variants", mode: 'copy'

    input:
    path reference
    path bam
    path bai  // not referenced directly in the script, but must be staged
              // alongside the BAM so pysam can find the .bai index next to it

    output:
    path "variants.vcf", emit: vcf
    path "variants_summary.json", emit: summary

    script:
    """
    python3 /opt/scripts/call_variants.py ${reference} ${bam} variants.vcf variants_summary.json
    """
}
