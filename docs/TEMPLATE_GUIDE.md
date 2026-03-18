# 共通テンプレート V2 ガイド

## 目的

V2 は「どの学習テーマでもほぼ必ず要る共通核」だけを高品質にそろえるための共通スターターです。
各シナリオの固有ルールは後段の Agent / SceneBuilder 側で上乗せします。

## 生成フロー

1. `RLMovie/Create Scenario Starter Files`
2. シナリオ名を PascalCase で決める
3. Unity の再コンパイル完了を待つ
4. `RLMovie/Create <Scenario> Scene`

生成されるもの:
- `Config/scenario_manifest.yaml`
- `Config/scenario_blueprint.yaml`
- `Config/<snake_case>_config.yaml`
- `Scripts/<Scenario>Agent.cs`
- `Editor/<Scenario>SceneBuilder.cs`
- `Scenes/<Scenario>.unity` を作るための builder

## V2 の役割分担

- `scenario_manifest.yaml`
  - viewer-facing 契約
  - `viewer_promise`
  - `visual_hooks`
  - `thumbnail_moment`
  - `camera_plan`
  - `acceptance_criteria`
- `scenario_blueprint.yaml`
  - `agents`
  - 共通部品の wiring 契約
  - `scene_roles`
  - `camera_roles`
  - `recording_defaults`
  - `overlay_bindings`
  - `highlight_bindings`
  - `visual_defaults`

## V2 で最初から揃う共通部品

- role-based `ScenarioGoldenSpine`
- `EnvironmentManager`
- `TrainingVisualizer`
- `RecordingHelper`
- `ScenarioBroadcastOverlay`
- `ScenarioHighlightTracker`
- explain / wide / follow 系の camera role
- V2 readability materials と light rig prefab

## 実装時の考え方

- 共通核の外に出すもの:
  - archetype 固有ルール
  - 報酬設計の細部
  - curriculum の個別ロジック
  - 特化 starter 専用の scene 生成
- 共通核に残すもの:
  - viewer-facing 契約
  - camera / recording / overlay / highlight の配線
  - validator / build / import が参照する基準情報

## 最低チェック

- `RLMovie/Validate Current Scenario`
- Heuristic 確認
- `RLMovie/Build for Colab (Current Scene)`
- `RLMovie/Import Trained Model`
