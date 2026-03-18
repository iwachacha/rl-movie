---
name: scenario-spec
description: Design or revise an RL scenario contract before implementation. Use when creating a new scenario or changing goals, success or failure conditions, observations, actions, rewards, randomization, difficulty, visuals, camera plans, or acceptance criteria in `scenario_manifest.yaml`.
---

# Scenario Spec

- Primary mode: `Spec`
- Read `../../references/project-direction.md`, `../../references/manifest-contract.md`, and `../../references/common-scenario-backbone.md`.
- If the spec changes camera logic in a meaningful way, also read `../unity-rl-camera/SKILL.md`.
- If visuals depend on external free assets, use `../unity-free-asset-research/SKILL.md`.

## Common-First Rule

- For a brand new learning scene, do not spec a one-off architecture first.
- Start from the shared backbone and only introduce scenario-local systems where the common layer is clearly insufficient.
- New starter kinds are downstream of shared-backbone maturity, not the default first step.

## Required Output

Every new scenario spec should explicitly include a short shared backbone plan:

1. Which common systems will be reused as-is.
2. Which common systems need extension or configuration.
3. Which logic stays scenario-local.
4. Which missing reusable gap should be solved in `Common` before or during build.

## Spec Priorities

- Keep `viewer_promise`, `visual_hooks`, `thumbnail_moment`, and `learning_goal` strong and specific.
- Keep `1 run = 1 hypothesis`.
- Keep the manifest as the viewer-facing contract, not as a dumping ground for implementation details.
- Prefer stable role/team/camera concepts that fit the shared blueprint and recording flow.
- When a requirement sounds reusable across future scenes, phrase it as a common-layer capability instead of a one-scene hack.

## Handoff To Build

- The spec handoff should make it obvious which shared components the builder must preserve.
- If the scenario cannot fit the current common backbone, call that out directly before implementation.
- Route implementation next through `../scenario-build/SKILL.md`.
