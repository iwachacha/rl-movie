# Video Standard

## 録画前チェック

- `Inference Only` で再生確認済み
- `TrainingVisualizer` を出すか隠すか決めた
- `BaseRLAgent.showDebugInfo` の表示方針を決めた
- `RecordingHelper` の設定を確認した
- `camera_plan` と撮る順番を決めた

## 最低限の画づくり

- 主役と目標物がひと目でわかる
- 背景よりプレイ領域が目立つ
- 成功と失敗の瞬間が視覚的にわかる
- UI を隠しても状況が読み取れる

## 録画構成

1. 導入カット
2. 学習成果が見える本編カット
3. 必要なら比較カット

## 出力名

```text
<scenario_slug>__<run_id>__takeXX
<scenario_slug>__<run_id>__comparison
```

## 録画後確認

- コマ落ちや破綻がない
- カメラ切替が不自然でない
- 採用テイクが run と結び付く
