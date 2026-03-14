---
name: curriculum-randomization
description: Design or revise curriculum stages and environment randomization for RL scenarios in this repository. Use when training is brittle, overfits to a narrow setup, stalls because the task starts too hard, or needs better difficulty progression, spawn variation, or domain randomization without losing the scenario contract.
---

# Curriculum Randomization

## Overview

Improve generalization and learning stability by shaping how difficulty and variation enter the environment.
Treat curriculum and randomization as explicit design tools, not ad hoc tweaks after a bad run.

## Workflow

1. Identify the actual failure pattern.
Decide whether the agent is failing because the task starts too hard, because it overfits a narrow spawn/layout distribution, or because variance is already too high.
Do not add randomization blindly to a scenario that is not yet learnable.

2. Keep the scenario contract stable unless the task truly changed.
Prefer modifying `difficulty_stages`, `randomization_knobs`, spawn ranges, obstacle patterns, time pressure, or reward pacing before rewriting the whole scenario.
If the task meaning changes, route through `scenario-spec`.

3. Design from easy-to-useful, not easy-to-hard in the abstract.
The first stage should teach the core behavior with minimal confounders.
Each later stage should add one meaningful source of difficulty or variation at a time.

4. Use the patterns in `references/stage-patterns.md`.
Choose only the curriculum and randomization knobs that match the failure mode.
Keep the first pass small enough that a later run can attribute success or failure to the change.

5. Tie the design back to experiment discipline.
Document the new hypothesis, baseline, and affected knobs in the manifest or training handoff.
Treat the resulting run as a new hypothesis, not a quiet continuation of the old one.

## Guardrails

- Do not mix many new randomization knobs into one experiment unless the user explicitly wants a broad reset.
- Do not increase difficulty and variance at the same time if interpretability matters.
- Avoid randomization that destroys visual clarity for recording unless the goal is robustness testing.
- Prefer scenario-local knobs over hidden magic constants.
- If a change requires manifest updates, make them explicit.

## Typical Moves

- Curriculum stages: expand target distance, obstacle density, timer pressure, or required precision over time.
- Spawn randomization: widen the start/goal distribution after core navigation is learned.
- Domain randomization: vary visuals, lighting, scale, distractors, or positions only after core control works.
- Evaluation robustness: keep a harder validation setup separate from the easiest learning stage.

## Response Shape

- Name the failure pattern you are addressing.
- Show the minimal curriculum or randomization change.
- State what should stay fixed for the next run.
- End with the new hypothesis and the validation expectation.
