---
name: scenario-fix
description: Debug and minimally fix scenario issues in this repository. Use when isolating defects, reproducing bugs, checking validator failures, fixing broken scene references, or deciding whether a change requires manifest updates or retraining.
---

# Scenario Fix

- 最初は主モードを `Fix` にする
- `Spec`、`Build`、`Train` に切り替えるときは理由を短く共有する
- 先に `../../references/validation-build-gates.md` を読む
- 契約不整合の可能性があるなら `../../references/manifest-contract.md` も読む
- 再学習判断が絡むなら `../../references/experiment-rules.md` も読む
- 変更前に再現手順、期待結果、実際結果、原因仮説を整理する
- 変更は最小差分に留め、確認方法を先に決める
- UnityMCP 変更後は保存し、Validator や Heuristic を必要範囲で再実行する
- 観測、行動、報酬、ランダム化、物理挙動を変えたら再学習を検討し、必要なら `scenario_manifest.yaml` と `spec_version` を更新する
- カメラや UI だけの修正は通常再学習しない
- 終了時は「何が直ったか」「何で確認したか」「残リスクは何か」を明示する
