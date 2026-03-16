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
- 必要な skill / reference / file だけを読み、広い探索や全文読込は詰まったときだけ行う
- 大きいファイルは全文より `rg`、行番号付き検索、部分抜粋を優先する
- 最短で動画映えする学習シミュレーションを優先し、独自制作を増やす前に既存の無料 Asset Store asset 活用を検討する
- 有料 asset は採用しない。外部 asset が必要なら無料の Unity Asset Store 候補だけを扱う
- 非専門視聴者にも目的・危険・結果が数秒で伝わる視認性を重視する
- 既定のユーザー向け出力は短くし、結果・検証・ブロッカー・判断待ちを優先する
- コード説明、実装の逐次実況、長い背景説明は、依頼された場合かリスク判断に必要な場合だけ出す
- ユーザー入力は repo や現在状態から安全に確定できない重要判断だけに絞り、推定可能なら前進する
- 省入力・省出力を優先しても、精度に必要な検証、保存確認、`Console` 確認は省略しない
- 複数フェーズにまたがる task では、早い段階で次の検証チェックポイントを明示する
- 繰り返し起きやすい不具合、レビュー指摘、手戻りが絡む task では `../skills/lessons-maintenance/SKILL.md` を読む
- 新しい学びを残すときは、グローバルルールを増やす前に `../skills/lessons-maintenance/SKILL.md` を使って置き場所と統合方針を先に決める
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
- 方針判断が絡む task では `../references/project-direction.md` を読む
- パスや責務が曖昧なら `../references/repo-map.md` を読む
- `.agents` 自体を触るときは `../skills/agents-maintenance/SKILL.md` を読む
- それ以外の repo タスクでは目的に合う skill を `../skills/` から 1 つ以上読む
