# RL Movie Repo Map

## 主要ディレクトリ

- `AI-RL-Movie/Assets/_RLMovie/`
  - `Common/`: 共通コードと editor utilities
    - `Common/Scripts/InWorldDisplay/`: シーン内情報表示の共通部品
  - `Environments/<Scenario>/`: 各シナリオ本体
  - `Environments/_Template/`: V2 共通スターターの generator source
  - `Recording/`: Recorder 関連
- `RunArchive/`: 学習 artifacts と採用判断
- `.agents/`: repo 専用 instruction
- `docs/`: 人向け補助資料

## V2 共通スターターの流れ

- 入口:
  - `RLMovie/Create Scenario Starter Files`
- 生成物:
  - `scenario_manifest.yaml`
  - `scenario_blueprint.yaml`
  - training YAML
  - Agent stub
  - SceneBuilder stub
- Scene 作成:
  - `RLMovie/Create <Scenario> Scene`

## 契約と品質ゲート

- viewer-facing 契約:
  - `AI-RL-Movie/Assets/_RLMovie/Environments/<Scenario>/Config/scenario_manifest.yaml`
- common wiring 契約:
  - `AI-RL-Movie/Assets/_RLMovie/Environments/<Scenario>/Config/scenario_blueprint.yaml`
- validator / build / import:
  - `AI-RL-Movie/Assets/_RLMovie/Common/Editor/ScenarioValidator.cs`
  - `AI-RL-Movie/Assets/_RLMovie/Common/Editor/BuildForColab.cs`
  - `AI-RL-Movie/Assets/_RLMovie/Common/Editor/ImportTrainedModel.cs`

## 共通 visual kit

- `AI-RL-Movie/Assets/_RLMovie/Common/Materials/`
- `AI-RL-Movie/Assets/_RLMovie/Common/Prefabs/`

## 通常は読まなくていい場所

- `AI-RL-Movie/Assets/ThirdParty/`
- `AI-RL-Movie/Assets/ML-Agents/`
- `AI-RL-Movie/Library/`
- `AI-RL-Movie/Temp/`
- `AI-RL-Movie/Logs/`
- `AI-RL-Movie/UserSettings/`
