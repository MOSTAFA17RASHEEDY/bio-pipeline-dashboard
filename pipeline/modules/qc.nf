// A Nextflow "process" is one step of the pipeline. Nextflow runs it inside
// the Docker container configured for it (see nextflow.config), automatically
// stages the declared `input:` files into an isolated work directory, runs
// the `script:` block there, and collects whatever the `output:` block says
// to expect.
process QUALITY_CHECK {
    tag "QC"  // label shown in Nextflow's console/log output for this task

    // Copy this task's outputs into results/qc/ once it finishes, instead of
    // leaving them buried in Nextflow's internal work/ directory.
    publishDir "${params.outdir}/qc", mode: 'copy'

    input:
    path reads  // the FASTQ file, staged into the task's work dir automatically

    output:
    path "qc_report.json", emit: report   // structured data (for the future API)
    path "qc_summary.txt", emit: summary  // human-readable summary

    script:
    // This runs *inside* the bio-pipeline/qc container. `reads` here resolves
    // to the staged file's name in the work dir (Nextflow handles the path).
    """
    python3 /opt/scripts/run_qc.py ${reads} qc_report.json qc_summary.txt
    """
}
