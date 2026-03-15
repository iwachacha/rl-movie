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

In this repo, store returned training artifacts under:

```text
RunArchive/<scenario>/runs/<run_id>/
```

Use the following stable shape:

```text
RunArchive/
  <scenario>/
  runs/
    <run_id>/
      run_summary.json
      model.onnx
      eval-notes.md
      comparison.md
```

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
