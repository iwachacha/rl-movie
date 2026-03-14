# Golden Spine Starter

`_Template` is the official starter source for new RL Movie scenarios.

Use:
- `RLMovie/Create Golden Scenario Starter Files` to generate a new scenario folder, manifest, training config, agent stub, and scene builder.
- After Unity recompiles, run the generated `RLMovie/Create <Scenario> Scene` menu item.

What is standardized:
- `scenario_manifest.yaml` contract shape
- `EnvironmentRoot`, `ScenarioGoldenSpine`, `EnvironmentManager`
- `TrainingVisualizer`, `RecordingHelper`, and camera anchors
- starter PPO config and naming alignment

What should be customized per scenario:
- observations
- actions
- reward logic
- hazards, props, physics rules
- theme, art, VFX, and recording polish
