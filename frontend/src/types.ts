// Mirrors backend/Dtos/RunDtos.cs. Kept as one small file since the API
// surface is small -- if this grows, split per-resource like the backend does.

export type RunStatus = "Queued" | "Running" | "Done" | "Failed";
export type RunKind = "Toy" | "RealSarek";

export interface RunListItem {
  id: number;
  sampleFileName: string;
  status: RunStatus;
  kind: RunKind;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
  totalVariants: number | null;
  progressPercent: number | null;
}

export interface Variant {
  chrom: string;
  position: number;
  ref: string;
  alt: string;
  type: "SNP" | "INS" | "DEL";
  qual: number;
  depth: number;
  supportingReads: number;
  alleleFrequency: number;
}

// RealSarek runs only -- static accuracy numbers vs. the GIAB truth set,
// not recomputed per run (see backend RunMapping.SarekValidation).
export interface ValidationMetrics {
  snvRecall: number;
  snvPrecision: number;
  snvF1: number;
  snvTruthTotal: number;
  indelRecall: number;
  indelPrecision: number;
  indelF1: number;
  indelTruthTotal: number;
}

export interface RunDetail extends RunListItem {
  errorMessage: string | null;
  qcTotalReads: number | null;
  qcMeanQuality: number | null;
  qcPassRatePercent: number | null;
  qcStatus: string | null;
  snpCount: number | null;
  insCount: number | null;
  delCount: number | null;
  // RealSarek runs only; null for Toy runs.
  currentStage: string | null;
  etaSecondsRemaining: number | null;
  stepsCompleted: number | null;
  stepsTotal: number | null;
  meanDepth: number | null;
  validation: ValidationMetrics | null;
  aiExplanation: string | null;
  variants: Variant[];
}
