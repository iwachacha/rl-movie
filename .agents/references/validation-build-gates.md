# Validation and Build Gates

## Validate / Build 前

- アクティブシーンを保存してから進める
- UnityMCP 変更後は `Save` と `Console` を確認してから再検証する

## `RLMovie > Validate Current Scenario` の主な確認項目

- `Assets/_RLMovie/Environments/<SceneName>/Config/` が存在する
- `scenario_manifest.yaml` が存在する
- `Config/` に `scenario_manifest.yaml` と `template_config.yaml` 以外の training YAML が 1 つ以上ある
- manifest の `training_config` が存在し、Config 内の対象 YAML を指している
- manifest に `viewer_promise`、`visual_hooks`、`thumbnail_moment` があり、動画としての約束が空でない
- シーン内に `BaseRLAgent` 継承 Agent がある
- Agent に `BehaviorParameters` と `DecisionRequester` が付いている
- `Behavior Name` が Agent クラス名と training YAML の behaviors キーに一致する
- manifest の `agent_class` と `behavior_name` がシーン上の Agent と一致する
- Agent の object reference 系 `SerializeField` が未設定でない
- `TrainingVisualizer` が存在し、`targetAgent` が現在シーンの Agent を向いている
- `RecordingHelper` が存在する
- `RecordingHelper` で camera switching を有効にするなら camera positions は 2 個以上にする

## 学習ゲート

- `Heuristic` の最低限確認が通るまでは学習へ進まない
- 学習ビルドは `RLMovie > Build for Colab (Current Scene)` を正規手順として使う
- `Build for Colab` は manifest の `training_config` で指定した YAML だけを ZIP に同梱する
- `Build for Colab` は Validator エラーがあると止まる前提で扱う

## モデル取込ゲート

- 学習済みモデル取込は `RLMovie > Import Trained Model` を正規手順として使う
- 取込後は `Behavior Type = Inference Only` と `.onnx` 割当を確認する

## 生成物 / 取込物

- `AI-RL-Movie/ColabBuilds/` を手編集しない
- `AI-RL-Movie/Assets/StreamingAssets/*.onnx` を手編集しない
