---
name: asset-intake
description: Evaluate or integrate external assets for RL Movie. Use when deciding whether to use a third-party asset, how to place it safely, or how to separate visual-only asset changes from learning logic changes.
---

# Asset Intake

- 主モードは通常 `Build`。必要性の整理だけなら `Spec` でもよい
- 先に `../../references/project-direction.md` を読み、asset が動画価値をどこで上げるかを明確にする
- 先に `_RLMovie` 側で不足しているものを定義し、何のために asset が必要かを 1 文で決める
- 新規候補の選定から必要なら `../unity-free-asset-research/SKILL.md` を先に使う
- `AI-RL-Movie/Assets/ThirdParty/` はユーザーの明示依頼がない限り読まない
- 既存 asset の棚卸し、用途整理、組み合わせ検討が先なら `asset-inventory-planning` を併用する
- 外部 asset は無料 Unity Asset Store のみを採用対象とし、有料 asset や paid 依存前提 package は却下する
- 映像的な効果が大きく、導入コストが低く、RL ロジックと分離しやすい asset を優先する
- 学習ロジックに必要な追加か、録画品質向上だけの追加かを分けて扱う
- 採用する場合でも元 asset の直接改造は最小限にし、Prefab Variant やシナリオ側 Prefab へ寄せる
- 比較実験ではビジュアル変更を RL ロジック変更と混ぜない
- 人向けの在庫管理が必要なら `docs/ASSET_CATALOG.md` を更新する
- 配置先や責務に迷ったら `../../references/repo-map.md` を読む
