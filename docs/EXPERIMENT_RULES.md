# RL Movie — Experiment Rules

## 基本原則

- `1 run = 1 hypothesis`
- 比較対象の `baseline_run` を明示する
- RL ロジック変更とビジュアル変更を同じ比較実験に混ぜない
- 採用理由を説明できない run は採用しない

## run_id 規約

```text
<scenario_slug>__v<spec_version>__<tag>__<yyyymmdd_hhmm>
```

例:

```text
roller_ball__v1__baseline__20260314_1200
roller_ball__v2__reward-shaping__20260314_1845
```

## 変更時のルール

- 観測、行動、報酬、ランダム化、物理挙動を変えたら `spec_version` を上げる
- `scenario_manifest.yaml` を先に更新する
- `HYPOTHESIS` は 1 文で書く
- `SEED` を記録する

## 比較観点

- 成功率
- 平均報酬
- 収束速度
- 挙動の自然さ
- 録画したときの見栄え

## 必須記録

- `run_id`
- `spec_version`
- `hypothesis`
- `seed`
- `baseline_run`
- 採用した `.onnx`
- `run_summary.json`
