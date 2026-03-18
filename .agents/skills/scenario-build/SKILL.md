---
name: scenario-build
description: Implement or modify RL scenarios in this repository. Use when creating a new scenario or changing scene setup, C# scripts, training YAML, or `scenario_manifest.yaml` under `AI-RL-Movie/Assets/_RLMovie`.
---

# Scenario Build

- Primary mode: `Build`
- Read `../../references/manifest-contract.md`, `../../references/validation-build-gates.md`, and `../../references/common-scenario-backbone.md`.

## Common-First Rule

- For new learning scenes, reuse `_RLMovie/Common` before introducing new starters, archetype scaffolds, or one-off infrastructure.
- Starter expansion is not the first move. First make the shared backbone more reusable if the gap is likely to recur.
- If the work is a small scenario-local fix, do not genericize unrelated code just for the sake of abstraction.

## Default Shared Systems

- `scenario_manifest.yaml` for the viewer-facing contract.
- `scenario_blueprint.yaml` for common wiring, roles, teams, cameras, overlay, highlight, and recording defaults.
- `BaseRLAgent` for agent lifecycle, telemetry, reward tracking, and heuristic support.
- `ScenarioGoldenSpine` for shared references and role binding.
- `RecordingHelper`, `ScenarioBroadcastOverlay`, `ScenarioHighlightTracker`, and `TrainingVisualizer` for viewer readability and capture flow.
- `EnvironmentManager` for simple environment bounds and spawn randomization.
- `ScenarioValidator`, `Build for Colab`, and `Import Trained Model` as the default validation and train/import path.

## Build Workflow

1. State the shared backbone plan before editing.
2. Reuse or extend the common layer where the pattern is likely to recur.
3. Keep scenario-specific gameplay and tuning under `AI-RL-Movie/Assets/_RLMovie/Environments/<Scenario>/`.
4. Keep shared runtime/editor/asset changes under `AI-RL-Movie/Assets/_RLMovie/Common/`.
5. Validate with `RLMovie/Validate Current Scenario`.
6. If relevant, check heuristic play and then use `RLMovie/Build for Colab (Current Scene)`.
7. Use `RLMovie/Import Trained Model` for adoption rather than a custom import path.

## Escalation

- If a requested scene cannot cleanly fit the current common backbone, do not jump straight to a new starter pattern.
- First identify the missing reusable capability and decide whether it belongs in `Common` or only in that scenario.
- Only propose or implement a new starter kind after the shared layer has been tightened enough that multiple future scenes would benefit.
