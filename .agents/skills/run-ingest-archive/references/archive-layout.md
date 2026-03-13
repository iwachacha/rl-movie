# Run Archive Layout

Use this file when organizing training outputs and evaluation records.

## Minimum Record Per Run

- `run_id`
- scenario name
- `spec_version`
- `baseline_run`
- hypothesis
- seed
- training config source
- paths to `.onnx`, `run_summary.json`, and related video if present
- adoption status
- short rationale
- next action

## Good Folder Shape

Use a stable run-keyed structure such as:

```text
<scenario>/
  runs/
    <run_id>/
      run_summary.json
      model.onnx
      eval-notes.md
      comparison.md
```

If the repo already uses another structure, preserve it and apply the same metadata model.

## Adoption Status Values

Prefer a small fixed vocabulary:

- `baseline`
- `candidate`
- `adopted`
- `rejected`
- `archived`

## Summary Rules

- One short paragraph for what changed.
- One sentence for why the run matters.
- One sentence for the next action.

## Why This Matters

Without a stable archive, comparisons drift into memory and chat history.
This makes later debugging and reusing good baselines much harder.
