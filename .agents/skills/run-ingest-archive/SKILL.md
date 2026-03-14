---
name: run-ingest-archive
description: Organize, label, and preserve RL training artifacts for this repository. Use when Colab outputs return to the repo, when `run_summary.json`, `.onnx`, notes, and videos need to be grouped by `run_id`, or when adoption decisions need a durable paper trail instead of scattered files and chat context.
---

# Run Ingest Archive

## Overview

Keep experiment artifacts traceable after training and evaluation.
Turn returned files and notes into a clean record of what was trained, what was adopted, and why.

## Workflow

1. Identify the artifact set.
Collect the `run_id`, `spec_version`, `baseline_run`, `.onnx`, `run_summary.json`, any evaluation notes, and any recording or comparison video.
If an artifact is missing, record that explicitly instead of silently dropping it from the archive.

2. Normalize the metadata.
Keep `run_id` as the primary key.
Preserve the hypothesis, scenario name, training config lineage, and adoption status in a single summary note or manifest-friendly record.

3. Separate archival facts from interpretation.
Store immutable facts such as files and IDs separately from judgment such as "best run so far" or "recording-ready."
This keeps later re-evaluation honest.

4. Use the structure in `references/archive-layout.md`.
Prefer a stable folder naming and summary shape so that future comparisons do not depend on memory or chat history.
Keep the archive lightweight and easy to scan.

5. Link the archive to the next decision.
If a run is adopted, connect it to import and recording.
If a run is rejected, capture the rejection reason and the next hypothesis.

## Guardrails

- Never rename `run_id` into a friendlier label and lose the original identifier.
- Do not overwrite an older baseline summary when a new run arrives.
- Keep adopted and rejected runs discoverable; rejected runs still matter for experiment history.
- Prefer append-only summaries over rewriting history.

## Typical Outputs

- A run folder or note keyed by `run_id`.
- A short adoption record with status, rationale, and next action.
- A comparison snapshot linking the current run to its baseline.
- A clean pointer to the imported `.onnx` if the run was promoted.

## Response Shape

- List the artifacts found.
- State the normalized metadata.
- State the adoption status or unresolved state.
- End with the archival result and what downstream skill should use it next.
