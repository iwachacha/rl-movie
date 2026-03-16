---
name: rl-video-quality-gate
description: Apply a cross-stage quality gate to RL Movie concepts, scenes, recordings, packages, or scripts. Use when the user wants a go/no-go review, wants to know whether something is video-ready, wants the top blockers before spending more implementation or editing effort, or wants to judge project output against winning RL and AI-simulation YouTube patterns.
---

# RL Video Quality Gate

## Overview

Judge whether the current artifact is actually strong enough as video, not just technically complete.
Use successful RL and AI-simulation channels as a benchmark source, but score the repo's output against transferable patterns rather than imitation.
Prefer a decision with clear blockers over a vague "pretty good."

## Workflow

1. Classify the review scope.
Pick one scope first:
- `Concept`: idea, hook, premise, or scenario contract
- `Scene`: readability, staging, lighting, dressing
- `Footage`: camera coverage, behavioral legibility, clip quality
- `Package`: title, thumbnail, cold open, beat order
- `Script`: narration, text cards, TTS readiness
- `End-to-end`: the whole chain from idea to publishable package

2. Load the minimum useful context.
Always read `../../references/project-direction.md` and `../../references/video-standard.md`.
Always read `../rl-video-benchmarking/references/youtube-patterns.md`.
Read `references/quality-rubric.md` and `references/common-failures.md`.
Then load only the matching stage references:
- `Package`: `../rl-video-packaging/references/episode-beats.md`, `../rl-video-packaging/references/title-thumbnail.md`
- `Script`: `../rl-video-scriptwriting/references/script-beats.md`, `../rl-video-scriptwriting/references/voice-friendly-copy.md`
- `Footage`: `../unity-rl-camera/references/shot-package.md`
- `Scene`: `../unity-scene-visual/references/visual-pass-order.md`

3. Judge viewer value before craft detail.
Answer these first:
- Is the actor, goal, and danger readable in seconds?
- Is there at least one failure mode or tension pattern worth watching?
- Is there a visible payoff, exploit, or behavioral leap?
- Is there one frame, shot, or moment that could sell the video?
If these are weak, do not hide the problem behind polish notes.

4. Score the artifact against the rubric.
Use `references/quality-rubric.md`.
Prioritize:
- hook clarity
- failure entertainment
- progress or payoff visibility
- visual and camera readability
- package strength
- script restraint and fit
- effort-to-video-value ratio
Not every category applies to every scope; score only what the current artifact can actually prove.

5. Call out the real blockers.
Focus on the smallest set of problems that most limits publishability.
Good blocker shapes are:
- the concept needs a cleaner viewer promise
- the scene is legible but not thumbnailable
- the footage lacks a readable breakthrough clip
- the package explains too much and promises too little
- the script talks over the footage instead of sharpening it

6. Convert the verdict into the next repo move.
Recommend the narrowest useful next skill:
- `rl-video-benchmarking` when the core concept or hook is weak
- `scenario-ideation` or `scenario-spec` when the scenario itself needs reframing
- `unity-scene-visual` or `unity-rl-camera` when watchability is a staging problem
- `scenario-record` when coverage is missing
- `rl-video-packaging` when the footage is good but the upload package is weak
- `rl-video-scriptwriting` when the package is solid but spoken copy is not

## Guardrails

- Do not pass something just because the model performs well.
- Do not overvalue visual polish when the viewer promise is muddy.
- Do not overvalue metrics, reward curves, or implementation cleverness.
- Do not recommend broad rebuilds when one blocker dominates.
- Do not force all scopes through the same checklist; use only the categories the artifact can support.
- If the result depends on footage or assets that do not exist yet, say that confidence is limited.

## Response Shape

- Open with `Ready`, `Promising but blocked`, or `Not video-ready yet`.
- State the review scope and confidence.
- List the top 1-3 blockers in severity order.
- Give a compact rubric readout.
- End with the next skill or next concrete step.

## Handoffs

If the user wants external precedent first, hand off to `rl-video-benchmarking`.
If the user wants fixes rather than review, hand off to the narrowest stage skill that matches the main blocker.
