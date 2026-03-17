# RL Movie — Operating Model

## 目的

このプロジェクトは `1人 + AI + UnityMCP` で、強化学習シナリオを「実験として正しい」だけでなく「動画として見たくなる」形で量産することを目指す。

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

1. 必要なら ideation / benchmark を行い、`docs/ideas/` に concept memo を置く
2. `Spec` で `scenario_manifest.yaml` を固める
3. この段階で `viewer_promise`、`visual_hooks`、`thumbnail_moment` を明示する
4. `Build` で実装する
5. `RLMovie > Validate Current Scenario`
6. `Heuristic` 確認
7. `RLMovie > Build for Colab (Current Scene)`
8. `Notebooks/rl_movie_training.ipynb`
9. `scenario-evaluate` で採用判断を行う
10. `RLMovie > Import Trained Model`
11. `Inference Only` 確認
12. `Record`
13. `rl-video-packaging`
14. 必要なら `rl-video-scriptwriting`
15. publishability が論点なら `rl-video-quality-gate`
16. 採用 run と判断を `RunArchive/` に残す

## 品質ゲート

### 学習前

- アクティブシーンが保存されている
- Validator が通る
- `Heuristic` が通る
- `Behavior Name`、training YAML、manifest が一致している
- `DecisionRequester`、`TrainingVisualizer`、`RecordingHelper` が必要構成としてそろっている
- manifest に `viewer_promise`、`visual_hooks`、`thumbnail_moment` が入っている

### 録画前

- 学習済みモデルを import 済み
- `Inference Only` が通る
- UI / camera / cut 構成が決まっている
- 出力名で run を追跡できる

### 公開前

- title / thumbnail / cold open が同じ viewer promise を支えている
- 最初の 10 秒で目的・危険・結果が伝わる
- package や script が footage の弱さを無理に説明で埋めていない
- 採用判断と使用 run が `RunArchive/` で追跡できる
