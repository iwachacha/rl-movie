---
name: rl-video-scriptwriting
description: Write beat-aligned narration, TTS-friendly voiceover, and text-card copy for RL Movie videos. Use when the user wants a spoken cold open, a full short-form script, tone variants, line-by-line narration matched to recorded footage, or voiceover text that will later be generated with ElevenLabs or another TTS tool.
---

# RL Video Scriptwriting

## Overview

Turn a strong RL video package into spoken copy that makes the footage easier to follow and more fun to watch.
Treat narration as support for the edit, not as a second story that competes with the visuals.
Write for TTS by default: short sentences, clean rhythm, easy pronunciation, and clear beat transitions.

## Workflow

1. Start from a locked package.
Read `../../references/project-direction.md` and `../../references/video-standard.md`.
Read `../rl-video-benchmarking/references/youtube-patterns.md`.
Read `../rl-video-packaging/references/episode-beats.md`.
Read `references/script-beats.md`, `references/tone-lanes.md`, and `references/voice-friendly-copy.md`.
If the user has raw footage but no clear beat order, title promise, or hero arc yet, load `../rl-video-packaging/SKILL.md` first.

2. Decide what the narration must do.
Choose one primary job:
- `Decode`: explain the rule, stake, or twist quickly.
- `Stitch`: bridge time jumps and clip transitions.
- `Punch up`: sharpen comedy, tension, or weirdness.
- `Document`: frame the behavior like a tiny simulation documentary.
Keep one primary job and at most one secondary job.

3. Choose one tone lane.
Use `references/tone-lanes.md`.
Default lanes are:
- `Chaotic comedy`
- `Tense challenge`
- `Curious documentary`
- `Uncanny competence`
Match the tone to the footage instead of forcing jokes onto every clip.

4. Build the beat map before writing lines.
For each beat, note:
- what the viewer sees
- what the viewer needs to understand
- whether narration helps or silence is better
- the maximum line count for that beat
Use the default beat stack unless the package clearly needs another order.

5. Write TTS-first copy.
Use `references/voice-friendly-copy.md`.
Default rules:
- one thought per sentence
- concrete verbs over abstract explanation
- short lines that survive neutral delivery
- natural wording for numbers, abbreviations, and hard-to-read tokens
- RL jargon only when it materially helps the viewer

6. Let the footage prove the claim.
Narration may set expectation, ask a question, label a failure pattern, or frame a breakthrough.
Do not script claims the viewer cannot see on screen.
If the line explains the whole joke before the action lands, cut or delay it.

7. Produce a recording-ready script package.
Usually include:
- `Viewer promise`
- `Tone lane`
- `Beat sheet`
- `Voiceover script`
- `Text cards or captions`
- optional `Alt openings` or `pickup lines`
If missing footage would force awkward narration, call out the exact missing clip.

## Guardrails

- Do not narrate every visible action.
- Do not default to tutorial voice unless the user asks for explainer style.
- Do not front-load reward shaping, hyperparameters, or config names.
- Do not overstuff jokes into every beat.
- Do not write long, clause-heavy sentences that sound robotic in TTS.
- Prefer silence over filler.
- Keep the cold open especially short and sharp.

## Response Shape

- Open with `Strong script fit`, `Needs tighter package first`, or `Good footage but wrong tone`.
- State the viewer promise and chosen tone lane.
- Give a compact beat map.
- Provide the final voiceover script.
- End with text-card copy and any missing shots or pickups.

## Handoffs

If the story arc, title promise, or beat order is still muddy, hand off to `rl-video-packaging`.
If the script exposes missing coverage, hand off to `scenario-record`.
If the narration problem is really a concept problem, hand off to `rl-video-benchmarking` or `scenario-ideation`.
If the user wants a final go/no-go review on the video's overall strength, hand off to `rl-video-quality-gate`.
