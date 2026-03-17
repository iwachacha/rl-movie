---
name: scenario-build
description: Implement or modify RL scenarios in this repository. Use when creating a new scenario or changing scene setup, C# scripts, training YAML, or `scenario_manifest.yaml` under `AI-RL-Movie/Assets/_RLMovie`.
---

# Scenario Build

- 主モードは `Build`
- 先に `../../references/manifest-contract.md` と `../../references/validation-build-gates.md` を読む
- 学習比較まで見据える変更なら `../../references/experiment-rules.md` も読む
- 新規シナリオは `../scenario-spec/SKILL.md` で `viewer_promise`、`visual_hooks`、`thumbnail_moment` を manifest に固めてから build に入る
- 新規シナリオは `AI-RL-Movie/Assets/_RLMovie/Environments/<PascalCase>/` に作る
- 必須フォルダは `Scenes/`、`Scripts/`、`Prefabs/`、`Config/`
- 新規作成は Scene、Agent、training YAML、`scenario_manifest.yaml` の 4 点セットで始める
- 新規作成はまず `RLMovie/Create Golden Scenario Starter Files` を使い、その後に生成された scene builder を実行する
- `_Template` は generator の正本テンプレートであり、手コピー先として直接複製しない
- 新しい Agent は `BaseRLAgent` を継承し、namespace は `RLMovie.Environments.<Scenario>` にする
- `Behavior Name = Agent クラス名 = training YAML の behaviors キー = manifest の behavior_name` を保つ
- `scenario_name = scene_name = Unity Scene 名 = シナリオフォルダ名` を保つ
- 既存シナリオ改修では、RL 挙動を変える前に manifest と必要なら `spec_version` を更新する
- RL ロジック変更と録画専用の見た目変更を同じ比較実験に混ぜない
- シーン・Prefab・Material の変更は Unity Editor または UnityMCP で行う
- UnityMCP 変更後は保存し、`Console` を確認する
- `RLMovie > Validate Current Scenario` の前にはアクティブシーンを保存する
- `Heuristic` の最低限確認が通るまでは学習へ進まない
- 不具合切り分け起点なら `../scenario-fix/SKILL.md` も読む
