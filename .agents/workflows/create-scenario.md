---
description: Legacy alias. Design with scenario-spec first, then implement with scenario-build, while following the shared common-backbone policy.
---

# Legacy Alias: create-scenario

- Main path: `../skills/scenario-spec/SKILL.md` -> `../skills/scenario-build/SKILL.md`
- Required reference: `../references/common-scenario-backbone.md`
- Required reference: `../references/manifest-contract.md`
- Required reference: `../references/validation-build-gates.md`
- Optional benchmark support: `../skills/rl-video-benchmarking/SKILL.md`
- Optional video framing support: `../references/video-standard.md`
- Experiment discipline: `../references/experiment-rules.md`

## Minimum Inputs

- A scenario idea or clear change request.
- A shared backbone plan naming which common systems will be reused.
- A manifest-ready viewer promise, visual hooks, thumbnail moment, and learning goal.

## Phase Gates

1. Spec the scenario around the shared backbone first.
2. Extend `_RLMovie/Common` only where the gap is reusable.
3. Keep scenario-local gameplay inside the scenario folder.
4. Validate before build and heuristic-check before train handoff.

## Done When

- The scenario contract is clear.
- The implementation reuses the shared backbone where appropriate.
- Validator passes.
- The train/build/record path still goes through the common tooling.
