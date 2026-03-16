---
name: rl-video-packaging
description: Turn recorded or candidate RL footage into a concrete upload package for RL Movie. Use when the user wants title or thumbnail options, a cold open, beat order, clip selection, comparison structure, on-screen text, or a short edit plan after a scenario concept or recorded run already exists.
---

# RL Video Packaging

## Overview

Turn watchable behavior into a publishable video package.
Optimize for one clear viewer promise, one readable story arc, and one memorable payoff instead of dumping every interesting clip into the cut.
Treat title, thumbnail, cold open, and clip order as one package decision.

## Workflow

1. Start from the strongest available artifact.
Read `../../references/project-direction.md` and `../../references/video-standard.md`.
Read `../rl-video-benchmarking/references/youtube-patterns.md`.
Read `references/episode-beats.md` and `references/title-thumbnail.md`.
If the task is selecting among raw recordings or comparisons, also read `references/footage-selection.md`.

2. Lock the viewer promise in one sentence.
Frame it as what the viewer gets to watch, not what algorithm was used.
Good shapes are:
- `Watch the AI try to <verb>`
- `Can this tiny idiot survive / escape / carry / dodge <threat>?`
- `The AI keeps failing until it discovers <twist>`

3. Pick the hero arc.
Choose one main story:
- chaotic failure to competence
- ridiculous exploit discovery
- tense repeated near-misses before a clean solve
- eerie competence in a readable environment
If two arcs compete, pick one and demote the other to a comparison beat or future upload.

4. Build a short beat stack.
Use `references/episode-beats.md` and usually keep:
- `Cold open`
- `Rule / setup`
- `Chaos or early failure`
- `Breakthrough or exploit`
- `Payoff`
- optional `Comparison` or `button`

5. Select footage like an editor.
Prefer clips that are:
- self-explanatory without debug UI
- behaviorally distinct from each other
- short enough to preserve momentum
- emotionally different enough to feel like escalation
Avoid stacking three clips that all show "sort of getting better" in the same way.

6. Design title and thumbnail together.
Use `references/title-thumbnail.md`.
The title should promise the verb, danger, or twist.
The thumbnail should show the protagonist, the problem, and the contrast.
If the title needs heavy RL jargon to make sense, rework the package.

7. Write only the minimum viewer-facing copy.
Keep on-screen text, intro narration, or description lines short and concrete.
Prefer rules, stakes, and the strange outcome over implementation detail.
If the user wants a full voiceover, beat-aligned narration, or TTS-ready script, hand off to `../rl-video-scriptwriting/SKILL.md` instead of stretching this skill past package design.

## Guardrails

- Do not lead with training graphs or config talk.
- Do not explain the whole scenario before showing something interesting happen.
- Do not use more than one main twist in the same package.
- Do not choose clips only because they are technically impressive.
- Do not let comparison footage dominate the main story unless comparison is the story.

## Response Shape

- Open with the package verdict: `Strong package`, `Promising but muddy`, or `Weak package`.
- State the one-sentence viewer promise.
- List the selected beat stack.
- Give 3-5 title options and 1-3 thumbnail concepts.
- End with the must-have clips or missing assets.

## Handoffs

If the package is locked and the user now needs spoken narration, text-card copy, or a TTS-friendly script, hand off to `rl-video-scriptwriting`.
If the user wants a broader cross-stage publishability review instead of another package iteration, hand off to `rl-video-quality-gate`.
