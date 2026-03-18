# V2 Common Starter Source

`_Template` is the generator source for the V2 common scenario starter.

Use:
- `RLMovie/Create Scenario Starter Files`
- After Unity recompiles, run `RLMovie/Create <Scenario> Scene`

What V2 standardizes:
- `scenario_manifest.yaml` as the viewer-facing contract
- `scenario_blueprint.yaml` as the common role/team wiring contract
- `TrainingVisualizer`, `RecordingHelper`, `ScenarioBroadcastOverlay`, and `ScenarioHighlightTracker`
- role-based `ScenarioGoldenSpine` bindings for agents, teams, scene anchors, cameras, and shared backbone references
- the minimum readability kit under `Assets/_RLMovie/Common/Materials` and `Assets/_RLMovie/Common/Prefabs`

What each scenario should customize:
- observations and actions
- reward logic and termination details
- environment rules and archetype-specific builders
- art direction, dressing, and recording polish
