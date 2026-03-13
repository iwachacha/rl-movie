# RL Movie — Operating Model

## 目的

このプロジェクトは `1人 + AI + UnityMCP` で、強化学習シナリオを再利用しやすい形で量産することを目指す。

## AI instruction の運用

- 軽い非 repo 質問では `.agents` を追加読込しない
- repo を調べる / 変更する依頼では `AGENTS.md` から `.agents/core/SKILL.md` と必要な task skill へ進む
- 詳細な契約やチェックリストは `.agents/references/` を必要時だけ読む
- `docs/` は人向け補助資料であり、自動必読ではない

## 5 つのモード

- `Spec`: シナリオ契約を固める
- `Build`: Scene、C#、YAML、manifest を実装する
- `Train`: build、Colab 学習、比較、model import を行う
- `Record`: 学習済みモデルの再生確認と録画を行う
- `Fix`: 不具合切り分けと最小差分修正を行う

## 標準パイプライン

1. `Spec` で `scenario_manifest.yaml` を固める
2. `Build` で実装する
3. `RLMovie > Validate Current Scenario`
4. `Heuristic` 確認
5. `RLMovie > Build for Colab (Current Scene)`
6. `Notebooks/rl_movie_training.ipynb`
7. `RLMovie > Import Trained Model`
8. `Inference Only` 確認
9. `Record`

## 品質ゲート

### 学習前

- アクティブシーンが保存されている
- Validator が通る
- `Heuristic` が通る
- `Behavior Name`、training YAML、manifest が一致している
- `DecisionRequester`、`TrainingVisualizer`、`RecordingHelper` が必要構成としてそろっている

### 録画前

- 学習済みモデルを import 済み
- `Inference Only` が通る
- UI / camera / cut 構成が決まっている
- 出力名で run を追跡できる
