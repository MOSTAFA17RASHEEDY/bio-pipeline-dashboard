import { useRef, useState } from "react";
import { useNavigate } from "react-router-dom";
import { api } from "../api/client";

const ALLOWED_EXTENSIONS = [".fastq", ".fq", ".fasta", ".fa", ".gz"];

function isAllowed(file: File): boolean {
  const name = file.name.toLowerCase();
  return ALLOWED_EXTENSIONS.some((ext) => name.endsWith(ext));
}

export function NewRunPage() {
  const [file, setFile] = useState<File | null>(null);
  const [dragActive, setDragActive] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const inputRef = useRef<HTMLInputElement>(null);
  const navigate = useNavigate();

  function pickFile(f: File | undefined | null) {
    if (!f) return;
    if (!isAllowed(f)) {
      setError(`Unsupported file type. Expected one of: ${ALLOWED_EXTENSIONS.join(", ")}`);
      return;
    }
    setError(null);
    setFile(f);
  }

  async function handleSubmit() {
    if (!file) return;
    setSubmitting(true);
    setError(null);
    try {
      const run = await api.createRun(file);
      navigate(`/runs/${run.id}`);
    } catch (err) {
      setError(err instanceof Error ? err.message : String(err));
      setSubmitting(false);
    }
  }

  return (
    <div className="mx-auto max-w-3xl space-y-6 p-6">
      <div>
        <h1 className="text-xl font-semibold tracking-tight text-on-surface">Launch a Pipeline Run</h1>
        <p className="mt-1 text-sm text-on-surface-variant">
          Upload a sample DNA file to run it through Quality Check → Alignment → Variant Calling.
        </p>
      </div>

      <div
        onDragOver={(e) => {
          e.preventDefault();
          setDragActive(true);
        }}
        onDragLeave={() => setDragActive(false)}
        onDrop={(e) => {
          e.preventDefault();
          setDragActive(false);
          pickFile(e.dataTransfer.files[0]);
        }}
        className={`rounded border-2 border-dashed p-10 text-center transition-colors ${
          dragActive ? "border-primary bg-primary/5" : "border-outline-variant bg-surface-container-low"
        }`}
      >
        <span className="material-symbols-outlined text-3xl text-on-surface-variant">upload_file</span>
        <p className="mt-3 text-sm font-medium text-on-surface">Drag and drop your sample file here</p>
        <p className="mt-1 text-xs text-on-surface-variant">
          Accepts .fastq, .fq, .fasta, .fa, or .gz — max 20 MB (this is a small demo pipeline)
        </p>
        <button
          type="button"
          onClick={() => inputRef.current?.click()}
          className="mt-4 rounded bg-surface-container-highest px-4 py-2 text-sm font-medium text-on-surface hover:bg-surface-container-high"
        >
          Browse Files
        </button>
        <input
          ref={inputRef}
          type="file"
          accept={ALLOWED_EXTENSIONS.join(",")}
          className="hidden"
          onChange={(e) => pickFile(e.target.files?.[0])}
        />
      </div>

      {file && (
        <div className="flex items-center justify-between rounded bg-surface-container-low p-3 shadow-sm">
          <div className="flex items-center gap-2">
            <span className="material-symbols-outlined text-on-surface-variant">description</span>
            <div>
              <p className="text-sm text-on-surface">{file.name}</p>
              <p className="font-mono text-xs text-on-surface-variant">{(file.size / 1024).toFixed(1)} KB</p>
            </div>
          </div>
          <button
            onClick={() => setFile(null)}
            className="rounded px-2 py-1 text-xs text-on-surface-variant hover:text-error"
          >
            Remove
          </button>
        </div>
      )}

      <div className="rounded bg-surface-container-low p-4 shadow-sm">
        <h3 className="mb-3 text-sm font-semibold text-on-surface">What happens next</h3>
        <div className="grid grid-cols-1 gap-3 sm:grid-cols-3">
          {[
            { n: "01", label: "Quality Check", detail: "Read stats, GC%, quality flags" },
            { n: "02", label: "Alignment", detail: "minimap2 + samtools vs. reference" },
            { n: "03", label: "Variant Calling", detail: "pysam-based SNP/INS/DEL caller" },
          ].map((s) => (
            <div key={s.n} className="rounded border border-outline-variant border-dashed p-3">
              <span className="font-mono text-[11px] text-primary">{s.n}</span>
              <p className="text-sm font-medium text-on-surface">{s.label}</p>
              <p className="mt-1 text-xs text-on-surface-variant">{s.detail}</p>
            </div>
          ))}
        </div>
      </div>

      {error && (
        <div className="rounded border border-error/40 bg-error-container/20 p-3 text-sm text-error">{error}</div>
      )}

      <div className="flex items-center justify-end gap-3">
        <button
          disabled={!file || submitting}
          onClick={handleSubmit}
          className="flex items-center gap-2 rounded bg-primary px-5 py-2.5 text-sm font-semibold text-on-primary shadow-md transition-colors hover:bg-primary-fixed disabled:cursor-not-allowed disabled:opacity-40"
        >
          {submitting ? (
            <>
              <span className="material-symbols-outlined animate-spin text-base">progress_activity</span>
              Starting…
            </>
          ) : (
            <>
              <span className="material-symbols-outlined text-base">rocket_launch</span>
              Start Pipeline Run
            </>
          )}
        </button>
      </div>
    </div>
  );
}
