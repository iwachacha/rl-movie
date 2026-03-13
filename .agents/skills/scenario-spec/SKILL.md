---
name: scenario-spec
description: Design or revise an RL scenario contract before implementation. Use when creating a new scenario or changing goals, success or failure conditions, observations, actions, rewards, randomization, difficulty, visuals, camera plans, or acceptance criteria in `scenario_manifest.yaml`.
---

# Scenario Spec

- 主モードは `Spec`
- 先に `../../references/manifest-contract.md` を読む
- 学習比較に関わる変更なら `../../references/experiment-rules.md` も読む
- 録画前提の設計を含むなら `../../references/video-standard.md` も読む
- `learning_goal`、成功条件、失敗条件、観測、行動、報酬、ランダム化、難易度、画作り、カメラ、受け入れ条件を実装前に固める
- `Config/scenario_manifest.yaml` をシナリオ契約の正本として扱う
- RL ロジックや学習条件を変えるなら、先に manifest を更新する
- 比較実験を行うなら変更軸を 1 つに絞り、`baseline_run` と採用基準を先に決める
- 学習比較は `1 run = 1 hypothesis` で扱う
- 実装へ進むときは `../scenario-build/SKILL.md` に引き継ぐ
