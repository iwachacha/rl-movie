---
name: unity-rl-camera
description: Design and tune Unity camera packages for RL Movie recording and viewer readability. Use when defining `camera_plan`, placing or revising `defaultCameraView` or `recordingCameraViews`, configuring `RecordingHelper`, choosing wide vs follow cuts, or making a scenario more watchable through camera framing in Unity or UnityMCP.
---

# Unity RL Camera

## Overview

Design a shot package, not just one good angle.
Prioritize viewer comprehension, readable cause and effect, and personality-revealing cuts over filmic motion for its own sake.
Match the repo's actual camera backbone: `ScenarioGoldenSpine`, `RecordingHelper`, and manifest `camera_plan`.

## Workflow

1. Start from the viewer question.
Read `../../references/project-direction.md` and `../../references/video-standard.md`.
If the request changes `camera_plan`, also read `../../references/manifest-contract.md`.
Read `references/shot-package.md` first.
Read `references/anchor-patterns.md` when choosing anchor geometry for the scene type.
Read `references/recording-helper-patterns.md` when changing `RecordingHelper`.

2. Choose the minimum shot package that tells the story.
Default to a package rather than a single angle:
- `Explain shot`: the default view that immediately explains hero, goal, and hazard.
- `Wide A`: one readable recording angle.
- `Wide B`: a complementary angle with different failure visibility.
- `Follow`: only if motion, personality, or precision becomes clearer from a dynamic cut.
Add a comparison shot only when the before/after story matters.

3. Choose anchor patterns by scenario geometry.
Use `references/anchor-patterns.md` to pick a baseline pattern such as arena, course, room, field, or swarm.
Treat the V2 starter anchors as the default package to improve, not as disposable boilerplate.

4. Give every shot one job.
- `Explain shot`: establish the rule, spatial relationship, and risk.
- `Wide shots`: show cause and effect, recoverable mistakes, and the shape of the arena.
- `Follow shot`: reveal body language, precision, near misses, or comedy.
If two shots do the same job, cut one.

5. Keep the package repo-aligned.
- When `ScenarioGoldenSpine` exists, ensure camera anchors are properly bound through the `ScenarioBackboneContext` or inspector.
- Keep `followCameraIndex` pointed at the intended dynamic cut.
- Prefer `hideUIWhenRecording = true` unless debug UI is deliberately part of the content.
- Tune `cameraSwitchInterval` only when the cuts are meaningfully different.

6. Validate the package like a viewer, not just an editor.
Preview:
- episode start
- typical mid-episode behavior
- one representative failure
- one success or near-success
- one reset or transition if relevant
Check that the viewer can still answer "who wants what, and what can go wrong?" within a few seconds.

7. Hand off cleanly.
- Use `scenario-spec` when the contract-level `camera_plan` changes.
- Use `scenario-record` when deciding capture order or UI policy.
- Use `unity-mcp-safe-ops` when making the scene edits through MCP.

## Guardrails

- Do not let a stylish follow shot replace the explain shot.
- Do not switch cameras before the viewer can reorient.
- Do not use FOV extremes unless the premise truly benefits from them.
- Do not center every subject symmetrically by default.
- Do not create anchors that only look good after the episode is already underway.
- Do not add motion when fixed anchors already tell the story better.

## Response Shape

- State the planned shot package.
- State the job of each shot.
- State the anchor pattern you are using.
- State which `RecordingHelper` settings matter.
- End with the validation sequence you will preview.
