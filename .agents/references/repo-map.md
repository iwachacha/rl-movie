# RL Movie Repo Map

## 主な作業領域

- `AI-RL-Movie/Assets/_RLMovie/`
  - `Common/`: 共通コードと editor utilities。`BaseRLAgent`、`ScenarioValidator`、`BuildForColab`、`ImportTrainedModel`、`TrainingVisualizer`、`RecordingHelper`
  - `Environments/<Scenario>/`: シナリオ固有の `Scenes/`、`Scripts/`、`Prefabs/`、`Config/`
  - `Environments/_Template/`: `Create Golden Scenario Starter Files` が参照する正本テンプレート
  - `Recording/`: Recorder 設定
- `Notebooks/rl_movie_training.ipynb`: Colab 学習 notebook
- `RunArchive/`: 返却済み training artifact と採用判断の保存先
- `.agents/`: AI 向け instruction 正本
- `docs/`: 人向け補助資料

## 契約と入口

- シナリオ契約の入口: `AI-RL-Movie/Assets/_RLMovie/Environments/<Scenario>/Config/scenario_manifest.yaml`
- 新規シナリオ作成の入口: `RLMovie/Create Golden Scenario Starter Files` → 生成された `Create <Scenario> Scene`
- 学習用 build / validator / import の入口:
  - `AI-RL-Movie/Assets/_RLMovie/Common/Editor/ScenarioValidator.cs`
  - `AI-RL-Movie/Assets/_RLMovie/Common/Editor/BuildForColab.cs`
  - `AI-RL-Movie/Assets/_RLMovie/Common/Editor/ImportTrainedModel.cs`

## 生成物 / 取込物

- `AI-RL-Movie/ColabBuilds/`: `Build for Colab` の生成物
- `AI-RL-Movie/Assets/StreamingAssets/*.onnx`: import した学習済みモデル

## 既定で避ける場所

- `AI-RL-Movie/Assets/ThirdParty/`
- `AI-RL-Movie/Assets/ML-Agents/`
- `AI-RL-Movie/Library/`
- `AI-RL-Movie/Temp/`
- `AI-RL-Movie/Logs/`
- `AI-RL-Movie/UserSettings/`
