---
description: 互換用 alias。新規シナリオ作成は scenario-spec で動画向け契約を固めてから scenario-build へ進む。
---

# Legacy Alias: create-scenario

- 正本: `../skills/scenario-spec/SKILL.md` -> `../skills/scenario-build/SKILL.md`
- フックや動画価値が曖昧なら先に `../skills/rl-video-benchmarking/SKILL.md`
- 追加参照: `../references/manifest-contract.md`
- 追加参照: `../references/validation-build-gates.md`
- 録画前提の読みやすさまで固めるなら `../references/video-standard.md`
- 比較計画まで含むなら `../references/experiment-rules.md`
- 到達点: `viewer_promise`、`visual_hooks`、`thumbnail_moment` を含む manifest 契約が固まり、その後に Scene、Agent、training YAML、manifest を作って Validator と Heuristic に進めること
