---
name: rl-movie-core
description: Common repository guardrails for RL Movie. Use when a request is repo-specific and requires reading files, searching code, planning changes, or editing anything under `c:\\rl-movie`.
---

# RL Movie Core

- 最初の作業更新で主モードを `Spec` / `Build` / `Train` / `Record` / `Fix` のどれか 1 つとして明示する
- repo 作業では `.agents/` を AI 向け正本として扱う
- シナリオ契約の正本は `Config/scenario_manifest.yaml` として扱う
- `docs/` は人向け補助資料であり、必要なときだけ読む
- 調査は `AI-RL-Movie/Assets/_RLMovie/` から始める
- 主な作業対象は `AI-RL-Movie/Assets/_RLMovie/`、`Notebooks/`、`.agents/`、必要な docs
- `AI-RL-Movie/Assets/ThirdParty/` はユーザーの明示依頼がない限り読まない・検索しない・編集しない
- `AI-RL-Movie/Library/`、`Temp/`、`Logs/`、`UserSettings/`、`Assets/ML-Agents/` は触らない
- `.meta` を手動作成・手動編集・手動削除しない
- `.unity`、`.prefab`、`.mat`、`.anim`、`.controller`、`.asset` をテキストエディタで直接編集しない
- シーン・Prefab・Material の変更は Unity Editor または UnityMCP で行う
- Play モード中は編集しない
- 既存 GameObject の削除や `ProjectSettings/` の変更は事前確認を取る
- UnityMCP で変更したら対象シーンを確認し、`Save` と `Console` を必ず確認する
- `RLMovie > Validate Current Scenario` と `RLMovie > Build for Colab (Current Scene)` の前にはアクティブシーンを保存する
- `AI-RL-Movie/ColabBuilds/` と `AI-RL-Movie/Assets/StreamingAssets/*.onnx` は生成物 / 取込物として扱い、手編集しない
- Common コード変更時は全シナリオ影響を意識する
- パスや責務が曖昧なら `../references/repo-map.md` を読む
- `.agents` 自体を触るときは `../skills/agents-maintenance/SKILL.md` を読む
- それ以外の repo タスクでは目的に合う skill を `../skills/` から 1 つ以上読む
