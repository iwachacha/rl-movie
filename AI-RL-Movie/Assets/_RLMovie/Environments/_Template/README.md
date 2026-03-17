# Golden Spine Starter

`_Template` is the official starter source for new RL Movie scenarios.

Use:
- `RLMovie/Create Golden Scenario Starter Files` to generate a new scenario folder, manifest, training config, agent stub, and scene builder.
- After Unity recompiles, run the generated `RLMovie/Create <Scenario> Scene` menu item.

What is standardized:
- `scenario_manifest.yaml` contract shape
- `viewer_promise`, `visual_hooks`, and `thumbnail_moment` as the viewer-facing contract captured before build
- `training_config` in the manifest selects the active training YAML for validation, build, and Colab
- `EnvironmentRoot`, `ScenarioGoldenSpine`, `EnvironmentManager`
- `TrainingVisualizer`, `RecordingHelper`, and wide/follow camera anchors
- `ScenarioBroadcastOverlay` for viewer-facing HUD and reward popups
- `ScenarioHighlightTracker` for sparse highlight + snapshot export
- starter PPO config and naming alignment

What should be customized per scenario:
- observations
- actions
- reward logic
- hazards, props, physics rules
- theme, art, VFX, and recording polish
