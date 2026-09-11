#!/usr/bin/env nextflow
// Entry point of the pipeline. Run it with:
//   nextflow run main.nf
// (from inside the pipeline/ directory, with Docker running)
nextflow.enable.dsl = 2

include { QUALITY_CHECK }   from './modules/qc.nf'
include { ALIGNMENT }       from './modules/align.nf'
include { VARIANT_CALLING } from './modules/variant_call.nf'

workflow {
    // `file()` resolves a path into a value Nextflow can pass as input to
    // multiple processes (unlike a Channel, which is normally consumed once).
    // That's the right tool here since this toy pipeline handles a single
    // sample against a single reference, not a batch of many samples.
    reads     = file(params.reads)
    reference = file(params.reference)

    QUALITY_CHECK(reads)

    ALIGNMENT(reference, reads)

    // ALIGNMENT.out.bam / .bai are this process's declared outputs (see the
    // `emit:` labels in modules/align.nf) -- this is how data flows from one
    // step to the next in DSL2.
    VARIANT_CALLING(reference, ALIGNMENT.out.bam, ALIGNMENT.out.bai)
}
