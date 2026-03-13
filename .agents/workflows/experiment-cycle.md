---
description: workflow alias for the full RL Movie iteration loop across spec, build, instrumentation, training, evaluation, recording, and archival
---

# Workflow Alias: experiment-cycle

- Primary skills: `../skills/scenario-spec/SKILL.md`, `../skills/scenario-build/SKILL.md`, `../skills/rl-instrumentation/SKILL.md`, `../skills/scenario-train/SKILL.md`, `../skills/scenario-evaluate/SKILL.md`, `../skills/scenario-record/SKILL.md`, `../skills/run-ingest-archive/SKILL.md`
- Use when the task spans multiple phases instead of a single isolated change.
- Suggested loop: spec or revise the scenario -> build the changes -> add diagnostics if needed -> validate and run `Heuristic` -> build for Colab and train -> compare runs and decide adopt or iterate -> import and record if adopted -> archive the run artifacts and decision
