---
name: asset-intake
description: Evaluate or integrate external assets for RL Movie. Use when deciding whether to use a third-party asset, how to place it safely, or how to separate visual-only asset changes from learning logic changes.
---

# Asset Intake

- 主モードは通常 `Build`。必要性の整理だけなら `Spec` でもよい
- 先に `_RLMovie` 側で不足しているものを定義し、何のために asset が必要かを 1 文で決める
- `AI-RL-Movie/Assets/ThirdParty/` はユーザーの明示依頼がない限り読まない
- 学習ロジックに必要な追加か、録画品質向上だけの追加かを分けて扱う
- 採用する場合でも元 asset の直接改造は最小限にし、Prefab Variant やシナリオ側 Prefab へ寄せる
- 比較実験ではビジュアル変更を RL ロジック変更と混ぜない
- 人向けの在庫管理が必要なら `docs/ASSET_CATALOG.md` を更新する
- 配置先や責務に迷ったら `../../references/repo-map.md` を読む
