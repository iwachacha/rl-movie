# Shared Scenario Backbone

Use this reference when creating a new learning scene or reshaping a scenario architecture.

## Goal

Build new scenarios on top of the shared `_RLMovie/Common` layer first.
Treat new starter kinds, archetype-specific scaffolds, and one-off infrastructure as a later step.
The first job is to make the common layer broadly reusable across many scene patterns.

## Reuse-First Defaults

- Use `scenario_manifest.yaml` and `scenario_blueprint.yaml` as the default contract pair.
- Build agents on `BaseRLAgent` unless there is a concrete reason not to.
- Use `ScenarioGoldenSpine` for shared references, role bindings, teams, and camera roles.
- Reuse `RecordingHelper`, `ScenarioBroadcastOverlay`, `ScenarioHighlightTracker`, and `TrainingVisualizer` before creating custom recording or HUD code. All three of `TrainingVisualizer`, `ScenarioBroadcastOverlay`, and `ScenarioHighlightTracker` auto-resolve their target agent via `ScenarioGoldenSpine` role bindings, so explicit inspector wiring is optional when a spine is present.
- Reuse `EnvironmentManager` for simple bounds, fall checks, and spawn randomization unless the task clearly needs a different environment controller.
- `ScenarioHighlightTracker` continues to export JSONL highlight and snapshot data even in headless (Colab) mode; only UI banner display is disabled.
- Reuse `ScenarioValidator`, `BuildForColab`, and `ImportTrainedModel` instead of custom validation, export, or import flows.
- Use the V2 readability kit as the minimum visual baseline before adding scene-specific art direction.
- Use `InWorldDisplay` components (`EpisodeCountDisplay`, `EpisodeTimerDisplay`, `RewardGraphDisplay`, `CumulativeRewardDisplay`, `SuccessRateDisplay`) for environment-blending training info panels. Attach to any Quad/Plane with a Renderer. Auto-resolves agent via GoldenSpine.

## Required Design Check

Before writing new scene code, explicitly answer these:

1. Which existing common systems will this scenario reuse as-is?
2. Which common systems need configuration only?
3. Which gap is truly missing from the common layer?
4. Is the gap specific to one scenario, or likely to help multiple future scene patterns?

If a gap is likely to recur across scenarios, prefer extending `_RLMovie/Common` first.
If the gap is truly scenario-specific, keep the change local under that scenario folder.

## Default Placement Rules

- Shared runtime systems belong under `Assets/_RLMovie/Common/Scripts`.
- Shared editor/build/validation helpers belong under `Assets/_RLMovie/Common/Editor`.
- Shared materials, prefabs, recorder presets, and other reusable assets belong under `Assets/_RLMovie/Common/` or `Assets/_RLMovie/Recording/`.
- Scenario-specific gameplay, tuning, and local scene setup belong under `Assets/_RLMovie/Environments/<Scenario>/`.

## Avoid By Default

- Do not fork common runtime scripts into a scenario folder just to make a local variation.
- Do not create a new `starter_kind` or archetype starter as the first response to a new scene request.
- Do not bypass manifest/blueprint/validator/build hooks when the existing flow can support the task.
- Do not create ad-hoc camera, overlay, telemetry, or highlight systems if the existing common ones can be configured or extended.
- Do not add scenario-specific fields to a common class unless the behavior is plausibly reusable.

## When To Extend Common

Extend the common layer when at least one of these is true:

- The feature is already needed by more than one scenario.
- The feature expresses a stable pattern such as team binding, role lookup, recording behavior, validation, telemetry, or viewer readability.
- Keeping it scenario-local would force duplication in the next likely scene.
- The change improves the common contract without baking in one scenario's theme.

Keep it local when the logic is tightly coupled to one task, reward structure, or scene gimmick.

## Expected Output In Planning

For new scenario design or implementation work, include a short "shared backbone plan" that names:

- The common systems being reused.
- Any common systems being extended.
- Any intentionally scenario-local code.
- Any missing reusable gap that should be addressed before starter work.

## Enforcement Mindset

For new learning scenes, "make it work" is not enough.
Prefer "make it reusable without overfitting to one scene."
If there is tension between a fast one-off implementation and a small reusable common-layer improvement, bias toward the reusable improvement when the scope is reasonable.
