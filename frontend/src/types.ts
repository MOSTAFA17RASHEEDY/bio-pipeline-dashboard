// Mirrors backend/Dtos/RunDtos.cs. Kept as one small file since the API
// surface is small -- if this grows, split per-resource like the backend does.

export type RunStatus = "Queued" | "Running" | "Done" | "Failed";

export interface RunListItem {
  id: number;
  sampleFileName: string;
  status: RunStatus;
  createdAt: string;
  startedAt: string | null;
  completedAt: string | null;
  totalVariants: number | null;
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

export interface RunDetail extends RunListItem {
  errorMessage: string | null;
  qcTotalReads: number | null;
  qcMeanQuality: number | null;
  qcPassRatePercent: number | null;
  qcStatus: string | null;
  snpCount: number | null;
  insCount: number | null;
  delCount: number | null;
  aiExplanation: string | null;
  variants: Variant[];
}
