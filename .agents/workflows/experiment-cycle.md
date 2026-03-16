---
description: workflow alias for the full RL Movie iteration loop across spec, build, instrumentation, training, evaluation, recording, and archival
---

# Workflow Alias: experiment-cycle

- Primary skills: `../skills/scenario-spec/SKILL.md`, `../skills/scenario-build/SKILL.md`, `../skills/rl-instrumentation/SKILL.md`, `../skills/scenario-train/SKILL.md`, `../skills/scenario-evaluate/SKILL.md`, `../skills/scenario-record/SKILL.md`, `../skills/rl-video-packaging/SKILL.md`, `../skills/rl-video-scriptwriting/SKILL.md`, `../skills/rl-video-quality-gate/SKILL.md`, `../skills/run-ingest-archive/SKILL.md`
- Use when the task spans multiple phases instead of a single isolated change.
- Minimum inputs: target scenario or scene, current phase, desired next decision, and if training is involved the latest `hypothesis` and `baseline_run`
- Phase gates: scene or config changes -> validate and `Heuristic`; training handoff -> `Build for Colab`; adoption or rejection -> compare runs and capture evidence; publishability questions -> run the quality gate; adopted runs -> archive artifacts and the decision
- Done when: the next phase, latest validation state, and keep-or-iterate decision are explicit
- Suggested loop: spec or revise the scenario -> build the changes -> add diagnostics if needed -> validate and run `Heuristic` -> build for Colab and train -> compare runs and decide adopt or iterate -> import and record if adopted -> package the best upload version -> write narration or text-card script if needed -> run a cross-stage quality gate when publishability is the question -> archive the run artifacts and decision
