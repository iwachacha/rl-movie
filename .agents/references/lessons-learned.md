# Lessons Learned

Curate this file through `../skills/lessons-maintenance/SKILL.md`.
Keep this file small.
Use it for verified, reusable lessons that prevent repeated mistakes across multiple RL Movie tasks.

Read this file when:
- the task spans multiple phases
- a bug or validator failure looks familiar
- a review finding or handoff note should change future behavior

Add a lesson only when all are true:
- the cause was confirmed from repo behavior, logs, validation, or code
- the lesson is likely to matter again outside the current task
- the lesson does not belong more naturally in a task-specific skill or reference
- the entry can stay short

Do not add:
- one-off diary notes
- long postmortems
- routine checklist items that already live in a task reference
- guesses about causes that were not verified

Preferred format:

```md
## Short Area Name
- Trigger: when this lesson applies
- Action: what to do
- Why: the failure or confusion it prevents
```

## Scene and Build Safety

- Trigger: after Unity scene edits or UnityMCP batches, before validator or build steps
- Action: save the active scene and inspect `Console` before running `RLMovie > Validate Current Scenario` or `Build for Colab`
- Why: unsaved scenes and silent serialization fallout can make validation and export results misleading

- Trigger: when changing `scenario_manifest.yaml`, `BehaviorParameters`, or training YAML selection
- Action: treat manifest values, `Behavior Name`, and `training_config` as one contract and update them together
- Why: drift between scene, manifest, and training config is a common source of validator and training failures

## Experiment Hygiene

- Trigger: when starting a new training comparison
- Action: keep `1 run = 1 hypothesis`, record `baseline_run`, and bump `spec_version` when the behavior contract changes
- Why: run comparisons stop being trustworthy when multiple changes are bundled into one run or version drift is unclear

## Evidence and Handoff

- Trigger: when adopting a model, rejecting a run, or handing results to a later step
- Action: archive the `.onnx`, `run_summary.json`, and short evaluation notes under `RunArchive/<scenario>/runs/<run_id>/`
- Why: model decisions should depend on durable evidence instead of chat memory

## Common Component Consistency

- Trigger: when adding a new backbone-aware component or patching an existing one
- Action: ensure auto-resolve via `ScenarioGoldenSpine` follows the same pattern (role/roles/team + `TryAutoResolveReferences` in `Awake`/`Update`)
- Why: inconsistent resolution patterns cause wiring bugs that only surface in specific scene configurations

## Headless Mode Granularity

- Trigger: when deciding what to disable under `RLMovieRuntime.IsHeadless`
- Action: disable only UI and rendering features; keep data export (JSONL, telemetry) active
- Why: headless training in Colab still benefits from structured telemetry output for post-hoc analysis
