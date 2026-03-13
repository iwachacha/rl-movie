# RL Movie — Project Structure

## ディレクトリ全体図

```text
c:\rl-movie\
|-- AGENTS.md
|-- .agents/
|   |-- core/
|   |   `-- SKILL.md
|   |-- skills/
|   |   |-- agents-maintenance/
|   |   |-- scenario-spec/
|   |   |-- scenario-build/
|   |   |-- scenario-train/
|   |   |-- scenario-record/
|   |   |-- scenario-fix/
|   |   `-- asset-intake/
|   |-- references/
|   |-- workflows/
|   `-- RULES.md
|-- AI-RL-Movie/
|   |-- Assets/
|   |   |-- _RLMovie/
|   |   |   |-- Common/
|   |   |   |-- Environments/
|   |   |   `-- Recording/
|   |   |-- StreamingAssets/
|   |   |-- ThirdParty/
|   |   `-- ML-Agents/
|   |-- ColabBuilds/
|   `-- ProjectSettings/
|-- Notebooks/
`-- docs/
```

## AI 向け instruction の考え方

- `AGENTS.md`: 薄いルータ。repo 非依存の軽い質問では `.agents` を追加読込しない
- `.agents/core/SKILL.md`: repo 共通ガードレール
- `.agents/skills/*/SKILL.md`: タスク別の手順
- `.agents/references/*.md`: 必要時だけ読む詳細契約とチェックリスト
- `.agents/workflows/*.md`: 互換用 alias / checklist。固有ルールの正本ではない
- `.agents/RULES.md`: legacy shim

## 主な実装領域

- `AI-RL-Movie/Assets/_RLMovie/Common/`: 共通コードと editor utilities
- `AI-RL-Movie/Assets/_RLMovie/Environments/<Scenario>/`: シナリオ固有の Scene、Script、Prefab、Config
- `AI-RL-Movie/Assets/_RLMovie/Environments/_Template/`: 新規シナリオのコピー元
- `Notebooks/rl_movie_training.ipynb`: Colab 学習 notebook

## 生成物 / 取込物

- `AI-RL-Movie/ColabBuilds/`: `Build for Colab` の生成物
- `AI-RL-Movie/Assets/StreamingAssets/*.onnx`: import した学習済みモデル

## 原則として避ける場所

- `AI-RL-Movie/Assets/ThirdParty/`
- `AI-RL-Movie/Assets/ML-Agents/`
- `AI-RL-Movie/Library/`
- `AI-RL-Movie/Temp/`
- `AI-RL-Movie/Logs/`
- `AI-RL-Movie/UserSettings/`

## 正本の優先順位

1. `.agents/`
2. `AI-RL-Movie/Assets/_RLMovie/Environments/<Scenario>/Config/scenario_manifest.yaml`
3. シナリオ固有 C# / Scene / training YAML
4. `docs/`
