# Train Handoff Checklist

## 到達条件

- `RLMovie > Validate Current Scenario` が通っている
- `Heuristic` 合格の確認が取れている
- `spec_version`、`baseline_run`、`hypothesis` が確定している
- `RLMovie > Build for Colab (Current Scene)` が完了している
- ZIP 名と Colab 入力値を短く共有できる

## Colab 入力値

- `SCENARIO_NAME`
- `RUN_ID`
- `SPEC_VERSION`
- `HYPOTHESIS`
- `SEED`
- `BASELINE_RUN`
- `MAX_STEPS`

## 最小共有フォーマット

- `Scenario`
- `Run ID`
- `Spec version`
- `Baseline`
- `Hypothesis`
- `Max steps`
- `Next action: Notebooks/rl_movie_training.ipynb を開いて Run all`
