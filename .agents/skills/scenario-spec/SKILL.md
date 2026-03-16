---
name: scenario-spec
description: Design or revise an RL scenario contract before implementation. Use when creating a new scenario or changing goals, success or failure conditions, observations, actions, rewards, randomization, difficulty, visuals, camera plans, or acceptance criteria in `scenario_manifest.yaml`.
---

# Scenario Spec

- 主モードは `Spec`
- 先に `../../references/project-direction.md` と `../../references/manifest-contract.md` を読む
- 学習比較に関わる変更なら `../../references/experiment-rules.md` も読む
- 録画前提の設計を含むなら `../../references/video-standard.md` も読む
- `camera_plan` の中身や shot package まで設計するなら `../unity-rl-camera/SKILL.md` を先に読む
- まず「視聴者が何を見て面白がるか」を 1 文で固定し、そのあとに RL の学習課題へ落とす
- V1 は 1 つの読みやすい学習ループに絞り、目的・危険・成功失敗が数秒で伝わる構成を優先する
- 高品質化は複雑な agent 挙動より、環境、障害物、prop、camera、reset 演出で稼ぐ方を優先する
- 外部 visual を使うなら無料 Unity Asset Store のみを前提にし、必要なら `../unity-free-asset-research/SKILL.md` で候補を固めてから仕様を閉じる
- `learning_goal`、成功条件、失敗条件、観測、行動、報酬、ランダム化、難易度、画作り、カメラ、受け入れ条件を実装前に固める
- `Config/scenario_manifest.yaml` をシナリオ契約の正本として扱う
- RL ロジックや学習条件を変えるなら、先に manifest を更新する
- 比較実験を行うなら変更軸を 1 つに絞り、`baseline_run` と採用基準を先に決める
- 学習比較は `1 run = 1 hypothesis` で扱う
- asset の選定や導入方針まで論点になるなら `../asset-intake/SKILL.md` に引き継ぐ
- 実装へ進むときは `../scenario-build/SKILL.md` に引き継ぐ
