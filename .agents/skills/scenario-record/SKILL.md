---
name: scenario-record
description: Prepare or capture recorded playback of trained scenarios. Use when checking `Inference Only`, deciding camera or UI policy, configuring `RecordingHelper`, or producing comparison-ready video output.
---

# Scenario Record

- 主モードは `Record`
- 先に `../../references/video-standard.md` と `../../references/validation-build-gates.md` を読む
- 学習済みモデルを取り込み、`Inference Only` で再生確認できるまでは録画しない
- `TrainingVisualizer` やデバッグ UI を見せるか隠すかを先に決める
- `RecordingHelper` 設定と `camera_plan` に沿ってカメラ構成を決める
- 導入、本編、必要なら比較カットの順でカット構成を決める
- 出力名は run を追跡できる形にする
- 録画用途だけのカメラ / UI 調整は RL 比較結果と混ぜて主張しない
