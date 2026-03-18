# RL Movie — Operating Model

## 目的

このプロジェクトは `1人 + AI + UnityMCP` で、強化学習シナリオを「実験として正しい」だけでなく「動画として見たくなる」形で量産することを目指す。

## V2 共通スターター

- 新規シナリオの正式入口は `RLMovie/Create Scenario Starter Files`
- V2 は viewer-facing 契約と common wiring を分離する
- `scenario_manifest.yaml` は動画価値の正本
- `scenario_blueprint.yaml` は camera / recording / overlay / highlight / visual kit の wiring 正本

## 標準パイプライン

1. 必要なら ideation / benchmark を行う
2. `Spec` で `scenario_manifest.yaml` を固める
3. 必要に応じて `scenario_blueprint.yaml` を調整する
4. `Build` で実装する
5. `RLMovie/Validate Current Scenario`
6. Heuristic 確認
7. `RLMovie/Build for Colab (Current Scene)`
8. `Notebooks/rl_movie_training.ipynb`
9. `scenario-evaluate` で採用判断
10. `RLMovie/Import Trained Model`
11. Inference Only 確認
12. `Record`

## 学習前の最低条件

- アクティブシーンが保存されている
- Validator が通る
- Heuristic が通る
- manifest / blueprint / training YAML / Behavior Name が一致している
- `TrainingVisualizer`, `RecordingHelper`, `ScenarioBroadcastOverlay`, `ScenarioHighlightTracker` が共通 backbone と整合している
- `viewer_promise`, `visual_hooks`, `thumbnail_moment`, `camera_plan` が埋まっている
