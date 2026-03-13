# Scenario Manifest Contract

## `scenario_manifest.yaml` の必須キー

- `scenario_name`
- `scene_name`
- `agent_class`
- `behavior_name`
- `learning_goal`
- `success_conditions`
- `failure_conditions`
- `observation_contract`
- `action_contract`
- `reward_rules`
- `randomization_knobs`
- `difficulty_stages`
- `visual_theme`
- `camera_plan`
- `acceptance_criteria`
- `baseline_run`
- `spec_version`

## 一致させる名前

- `scenario_name = scene_name = Unity Scene 名 = シナリオフォルダ名`
- `agent_class = Agent クラス名`
- `behavior_name = Behavior Parameters の Behavior Name = training YAML の behaviors キー`

## 新規シナリオの最小形

- 作成先は `AI-RL-Movie/Assets/_RLMovie/Environments/<PascalCase>/`
- 必須フォルダは `Scenes/`、`Scripts/`、`Prefabs/`、`Config/`
- 最低限そろえるものは Scene、Agent、training YAML、`scenario_manifest.yaml`
- `template_config.yaml` はコピー元であり、直接編集しない

## 実装との同期

- Agent は `BaseRLAgent` を継承する
- namespace は `RLMovie.Environments.<Scenario>` を使う
- 観測、行動、報酬、ランダム化、物理挙動を変えるときは manifest も同時に更新する
- 比較実験用の変更で挙動が変わるなら `spec_version` を上げる
