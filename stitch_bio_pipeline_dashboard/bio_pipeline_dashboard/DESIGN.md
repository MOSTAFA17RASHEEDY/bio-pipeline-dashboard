---
name: Bio Pipeline Dashboard
colors:
  surface: '#0b1326'
  surface-dim: '#0b1326'
  surface-bright: '#31394d'
  surface-container-lowest: '#060e20'
  surface-container-low: '#131b2e'
  surface-container: '#171f33'
  surface-container-high: '#222a3d'
  surface-container-highest: '#2d3449'
  on-surface: '#dae2fd'
  on-surface-variant: '#bbcac6'
  inverse-surface: '#dae2fd'
  inverse-on-surface: '#283044'
  outline: '#859490'
  outline-variant: '#3c4947'
  surface-tint: '#4fdbc8'
  primary: '#4fdbc8'
  on-primary: '#003731'
  primary-container: '#14b8a6'
  on-primary-container: '#00423b'
  inverse-primary: '#006b5f'
  secondary: '#4cd7f6'
  on-secondary: '#003640'
  secondary-container: '#03b5d3'
  on-secondary-container: '#00424e'
  tertiary: '#6bd8cb'
  on-tertiary: '#003732'
  tertiary-container: '#44b5a8'
  on-tertiary-container: '#00423c'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#71f8e4'
  primary-fixed-dim: '#4fdbc8'
  on-primary-fixed: '#00201c'
  on-primary-fixed-variant: '#005048'
  secondary-fixed: '#acedff'
  secondary-fixed-dim: '#4cd7f6'
  on-secondary-fixed: '#001f26'
  on-secondary-fixed-variant: '#004e5c'
  tertiary-fixed: '#89f5e7'
  tertiary-fixed-dim: '#6bd8cb'
  on-tertiary-fixed: '#00201d'
  on-tertiary-fixed-variant: '#005049'
  background: '#0b1326'
  on-background: '#dae2fd'
  surface-variant: '#2d3449'
typography:
  display-lg:
    fontFamily: Inter
    fontSize: 30px
    fontWeight: '600'
    lineHeight: 38px
    letterSpacing: -0.02em
  display-lg-mobile:
    fontFamily: Inter
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
    letterSpacing: -0.01em
  headline-md:
    fontFamily: Inter
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
    letterSpacing: -0.01em
  headline-sm:
    fontFamily: Inter
    fontSize: 16px
    fontWeight: '600'
    lineHeight: 24px
    letterSpacing: -0.005em
  body-md:
    fontFamily: Inter
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
    letterSpacing: 0em
  body-sm:
    fontFamily: Inter
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 18px
    letterSpacing: 0.005em
  label-code-lg:
    fontFamily: JetBrains Mono
    fontSize: 13px
    fontWeight: '500'
    lineHeight: 18px
    letterSpacing: 0.01em
  label-code-md:
    fontFamily: JetBrains Mono
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
    letterSpacing: 0.02em
  label-code-sm:
    fontFamily: JetBrains Mono
    fontSize: 11px
    fontWeight: '400'
    lineHeight: 14px
    letterSpacing: 0.03em
  caption:
    fontFamily: Inter
    fontSize: 11px
    fontWeight: '500'
    lineHeight: 14px
    letterSpacing: 0.02em
rounded:
  sm: 0.125rem
  DEFAULT: 0.25rem
  md: 0.375rem
  lg: 0.5rem
  xl: 0.75rem
  full: 9999px
spacing:
  space-xxs: 0.125rem
  space-xs: 0.25rem
  space-sm: 0.5rem
  space-md: 0.75rem
  space-base: 1rem
  space-lg: 1.5rem
  space-xl: 2rem
  space-2xl: 3rem
  gutter: 1rem
  margin-page: 1.5rem
  sidebar-width: 16rem
  log-panel-height: 14rem
---

## Brand & Style

This design system establishes a high-density, analytical console built specifically for computational biologists, bioinformaticians, and genomic lab engineers. The visual identity merges the razor-sharp, distraction-free efficiency of modern developer tooling with the domain-specific rigor of clinical genomics workbenches.

The interface prioritizes clarity, structural hierarchy, and information density over decorative ornamentation. Its emotional tone is clinical, dependable, and technically precise: sequence reads, pipeline stages, quality scores, and variant tables must be immediately scannable without sensory fatigue during prolonged sessions.

### Design Movement: High-Density Technical Modernism
- **Surface Depth:** Deep, layered navy-slate canvases that reduce ocular strain during deep-dive genomic inspection.
- **Micro-Precision Accents:** Electric teal and cyan phosphors act as operational beacons for system throughput, active processes, and validated sequence data.
- **Structural Integrity:** Crisp, hair-thin 1px borders delimit panels, diagnostic readouts, and tabular data without visual bulk.
- **Domain Specialization:** Built-in conventions for quaternary genomic data (A, C, G, T nucleotide coloring), log streaming, and Phred quality score representation.

## Colors

The palette is engineered dark-first to anchor multi-hour bioinformatics operations. Surfaces rely on cool, deeply desaturated slate blues rather than pure blacks, maintaining spatial separation under complex analytical multi-column layouts.

### Palette Roles & Distribution

- **Primary Accent (`#14B8A6`):** Indicates operational progress, verified metrics, running jobs, active sequence alignments, and focal actions.
- **Secondary Accent (`#06B6D4`):** Denotes pipeline inputs, reference genome selections, genomic navigation breadcrumbs, and primary data filters.
- **Tertiary Accent (`#0D9488`):** Serves as a muted structural highlight for secondary buttons, interactive table headers, and nested container tabs.
- **Neutral Surface Hierarchy:**
  - `Canvas / Root Base`: `#0B0F17` (Deep space navy)
  - `Surface Subdued / Sidebar`: `#0F172A` (Rich slate substrate)
  - `Card / Panel Surface`: `#111827` (Raised workspace layer)
  - `Elevated / Popover / Tooltip`: `#1E293B` (Interacting top layer)
  - `Border / Hairline Separators`: `#1E293B` (Subtle) and `#334155` (Interactive/Strong)
- **Text & Foreground Hierarchy:**
  - `Primary Data Text`: `#F8FAFC` (High contrast, sequence readouts)
  - `Secondary / Metadata`: `#94A3B8` (Parameters, runtimes, timestamps)
  - `Muted / Disabled`: `#64748B` (Inactive steps, empty placeholders)

### Genomic Semantic System
- **Adenine (A):** `#38BDF8` (Sky blue)
- **Cytosine (C):** `#34D399` (Mint emerald)
- **Guanine (G):** `#FBBF24` (Amber yellow)
- **Thymine (T):** `#F87171` (Coral rose)

### Operational Pipeline Statuses
- **Success / Passed QC:** `#10B981` (Emerald)
- **Running / Aligning:** `#06B6D4` (Cyan with pulse indicator)
- **Pending / In Queue:** `#64748B` (Muted slate)
- **Warning / Low Coverage:** `#F59E0B` (Amber)
- **Failed / Variant Artifact:** `#EF4444` (Rose red)

## Typography

The typography architecture balances high-efficiency administrative labeling with strict mathematical tabular alignment. 

- **Primary Interface Font (`Inter`):** Drives layout headers, modal dialogs, configuration panels, and generic dashboard metrics. Standardized with slight negative letter tracking on large headings to produce a solid, compact aesthetic reminiscent of engineering tools.
- **Monospace Workhorse (`JetBrains Mono`):** Applied across all raw analytical data:
  - Sequence representations (FASTA / FASTQ strings, base pair alignments).
  - VCF (Variant Call Format) coordinates (e.g., `chr7:140753336-140753337`).
  - Phred scores (Q30, Q40), execution runtime timers, coverage depths (`120x`), file hashes, and terminal logs.
  - Tabular columns containing numeric data must employ tabular figures (`tnum`) to keep decimal alignment perfectly uniform across thousand-row grids.

## Layout & Spacing

This design system uses an edge-to-edge, screen-constrained layout model engineered for widescreen laboratory workstations and multi-monitor setups. Rather than loose marketing grids, the application relies on an adaptive fluid dashboard shell with persistent primary toolbars and collapsible utility drawers.

### Shell Architecture
- **Global Left Bar:** Fixed `16rem` navigation hosting pipeline catalogs, active run lists, and node health.
- **Top Context Strip:** Compact `3rem` sticky utility row displaying active sample metadata (`SAMPLE_ID`, flow cell ID, aligned reference genome build like `GRCh38`).
- **Main Analysis Canvas:** Multi-pane fluid split-screen with drag-resizable splitters between visual pipelines, IGV-style tracks, and variant inspection tables.
- **Bottom Diagnostic Drawer:** Collapsible `14rem` execution log viewer with real-time stdout/stderr streaming.

### Breakpoints & Reflow
- **Desktop (>= 1280px):** Full multi-column view. Step tracker horizontally spans the active workflow; tables retain full column sets (Allele Frequency, Depth, Quality, Filter).
- **Laptop / Compact Workstation (1024px - 1279px):** Inspector panels switch from split-pane to tabbed overlays. Secondary sequence tags collapse into tooltips.
- **Tablet / Emergency Ops (< 1024px):** Layout stacks into a single linear workflow feed. Data tables shift to card-based horizontal scrolls.

## Elevation & Depth

To maximize visible information density, elevation is communicated through **tonal surface stacking** and **crisp 1px boundary lines** rather than heavy, blurry ambient drop shadows.

### Elevation Levels

- **Base Floor (Canvas - `#0B0F17`):** The non-interactive foundational layer. Holds background canvas and structural layout separators.
- **Level 1 (Panels & Cards - `#111827`):** The operational core. Surrounded by a continuous border of `1px solid #1E293B`. No shadow.
- **Level 2 (Active Cards & Focus Containers - `#0F172A`):** Used for running stages or expanded pipeline cards. Border brightens to `1px solid #334155` with a subtle inside tint of `#14B8A6` (0.05 opacity).
- **Level 3 (Overlays, Dropdowns, Tooltips - `#1E293B`):** Floats above dense data. Encapsulated by `1px solid #475569` and a precision shadow: `0 4px 16px -2px rgba(0, 0, 0, 0.6)`.
- **Level 4 (Modal Critical Dialogs):** Centered view overlays with a solid backdrop wash: `rgba(11, 15, 23, 0.8)` with `backdrop-filter: blur(4px)`.

## Shapes

The design system maintains a **Soft (`1`)** shape language. Precision scientific instruments feel loose and toy-like when heavily rounded; thus, borders utilize crisp, disciplined corners.

- **Micro Components (Badges, Pills, Nucleotide Tags):** `0.25rem` (`4px`) roundedness to maintain compactness when clustered inside dense table cells.
- **Interactive Controls (Inputs, Buttons, Segmented Controls):** `0.375rem` (`6px`) roundedness for clear target acquisition without softening the structural layout.
- **Containers & Data Panels:** `0.5rem` (`8px`) roundedness maximum. 
- **Connectors & Graph Nodes:** Strictly rectangular and linear step lines (`0px` or `2px` corners) to represent rigorous procedural execution pipelines.

## Components

### 1. Pipeline Step Tracker
- **Structure:** Horizontal sequence of distinct nodes: `01: Quality Check` -> `02: Alignment (BWA-MEM)` -> `03: Variant Calling (GATK)`.
- **States:**
  - *Completed:* Slate border, subtle teal text, static checkmark icon in `#10B981`.
  - *Active / Running:* Highlighted container with `#14B8A6` 1px ring, subtle cyan background glow (`rgba(20, 184, 166, 0.08)`), and an animated pulsing terminal dot.
  - *Pending:* Muted typography (`#64748B`), dashed connector border (`#334155`).
  - *Failed:* Solid `#EF4444` border, red badge indicator, execution halt indicator.

### 2. High-Density Genomic Data Tables
- **Header:** Sticky, uppercase `JetBrains Mono` at `11px`, letter-spaced `0.05em`, background `#0F172A`, bottom border `1px solid #334155`.
- **Row Styling:** Row height strictly constrained to `36px`. Hover state triggers a subtle background transition to `#1E293B`.
- **Numeric & Coordinate Alignment:** Numeric fields (Coverage, Quality, Allele Depth) must be right-aligned with monospace font. Chromosome and variant labels are left-aligned.

### 3. Nucleotide & Quality Badges
- **Bases:** Solid color-coded micro-badges (`A`, `C`, `G`, `T`) set at `11px` monospace with `4px` padding and background fills at 15% opacity matched to text color.
- **Phred Scores:** Badges showing `Q30+` utilize `#10B981` (Passing), scores `< Q20` utilize `#EF4444` background warning tags.

### 4. Buttons & Action Triggers
- **Primary Action (Run Pipeline / Export VCF):** Background `#14B8A6`, text `#042F2C` (high contrast dark teal), weight `600`. Hover shifts to `#0D9488`.
- **Secondary Action (Configure Parameters):** Background `transparent`, border `1px solid #334155`, text `#F8FAFC`. Hover shifts to `#1E293B`.
- **Danger Action (Abort Pipeline):** Border `1px solid #EF4444`, background `rgba(239, 68, 68, 0.1)`, text `#FCA5A5`.

### 5. Input Fields & Parameter Controls
- **Style:** Background `#0B0F17`, border `1px solid #334155`, text `#F8FAFC`. Active focus states display a `1px solid #06B6D4` with an inner glow shadow.
- **Inline Gene Search / Coordinate Jumper:** Displays pre-fixed shortcut keys (`⌘K` or `chr:pos`) formatted in small monospace badges on the right edge.

### 6. Terminal Log Viewer
- **Console Body:** Fixed-height bottom viewport in `#05080E` with a continuous top status rail showing stream rates (`lines/sec`), buffer status, and download buttons.
- **Text Rendering:** Monospaced `JetBrains Mono` at `12px` with distinct token highlights for log levels: `[INFO]` (teal), `[WARN]` (amber), `[ERROR]` (coral rose), and `[DEBUG]` (slate).