---
name: scenario-train
description: Train, evaluate, hand off, or import RL models for this repository. Use when preparing `Build for Colab`, running or comparing Colab training, deciding adoption, importing `.onnx`, or setting up a train handoff.
---

# Scenario Train

- 主モードは `Train`
- 先に `../../references/validation-build-gates.md` を読む
- 学習比較、run 設計、採用判断が絡むなら `../../references/experiment-rules.md` も読む
- train handoff や Colab 引き継ぎが目的なら `../../references/train-handoff-checklist.md` も読む
- `RLMovie > Validate Current Scenario` と `Heuristic` が通るまでは `Build for Colab` や学習を始めない
- 学習前に manifest と training YAML の整合、`spec_version`、`baseline_run`、`hypothesis` を確定する
- 学習ビルドは `RLMovie > Build for Colab (Current Scene)` を正規手順として使う
- `AI-RL-Movie/ColabBuilds/` の ZIP は生成物として扱い、手編集しない
- Colab 入力は `SCENARIO_NAME`、`RUN_ID`、`SPEC_VERSION`、`HYPOTHESIS`、`SEED`、`BASELINE_RUN`、`MAX_STEPS`
- `RUN_ID` は `<scenario_slug>__v<spec_version>__<tag>__<yyyymmdd_hhmm>` を守る
- 比較では成功率、平均報酬、収束速度、挙動の自然さを確認する
- 採用した run は `run_summary.json`、採用理由、使用 `.onnx` と結び付けて残す
- 学習済みモデル取込は `RLMovie > Import Trained Model` を正規手順として使う
- 取込後は `Behavior Type = Inference Only` と正しい `.onnx` 割当を確認する
- train handoff だけが目的なら、最低限 checklist を満たして次アクションまで整理して止めてよい
