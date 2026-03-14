---
name: scenario-ideation
description: Generate and refine video-worthy RL scenario ideas before specification. Use when the user wants new strong theme ideas, tonal variations, episode concepts, shortlist comparisons, or help choosing a scenario that is visually striking, emotionally memorable, and still feasible to build and train with Codex in this repository.
---

# Scenario Ideation

## Overview

Invent concepts that feel worth watching as a video, not just worth implementing as a benchmark.
Push for strong visual hooks and tonal variety, then compress ideas back into mechanics that are realistic for this repo.

## Workflow

1. Frame the brief quickly.
Infer missing constraints unless they change the answer materially. Useful constraints are: desired tone, target video vibe, appetite for imported assets, complexity budget, whether training should be quick or can be a stretch, and whether the user wants many wild ideas or a tighter shortlist.

2. Diverge before converging.
Generate ideas across different emotional lanes instead of producing five near-duplicates. Read `references/idea-lenses.md` when you want fresh angles, stronger contrast, or a better balance of funny / impressive / strange / tense / creepy / satisfying.

3. Filter for repo-feasible mechanics.
Prefer concepts that can be built from common Unity + ML-Agents patterns already suitable for this project:
- locomotion, steering, timing, jumping, pushing, carrying, balancing
- pickups, checkpoints, hazards, moving obstacles, triggers, score zones
- simple scripted NPCs, simple physics props, single-agent or lightly staged interactions
- visible resets, readable failure states, clear win conditions, camera-friendly spaces

4. Reject or simplify ideas that cross into research-heavy territory.
Avoid recommending concepts that depend on advanced robotics, dense multi-agent coordination, realistic language understanding, high-fidelity destruction, deformables, fluids, complex grasping, procedural animation breakthroughs, or expert-level custom optimization.
If the premise is strong but the implementation is too hard, salvage it with a simpler watchable core. Replace sophistication with better staging.

5. Package each idea like a pitch for both production and implementation.
For each candidate, usually include:
- `Title`: short and memorable
- `Viewer hook`: the one-sentence trailer
- `What we watch`: the most visible on-screen moments
- `RL core`: what the agent actually learns
- `Why it is learnable`: why this is trainable without exotic research
- `Build band`: `Easy`, `Moderate`, or `Stretch but feasible`
- `Comedy / awe / weird / fear factor`: the dominant emotional note

6. Recommend a direction, not just a pile of options.
If the user asks for ideas, end with one best-balance pick and one wild-card pick.
Explain why the best-balance pick is the safest path to a compelling video.
If the user chooses a concept, first create a short concept memo in `docs/ideas/` as a hand-off artifact, then load `scenario-spec` when the user wants to formalize it.

7. Create a concept memo once the idea is locked.
Write one compact Markdown note under `docs/ideas/` that captures the agreed direction before deeper specification.
Use it as a bridge between ideation and formal scenario design.
Include:
- `Concept`: what the scenario is
- `Why this idea`: why it is compelling for video and RL
- `Core fantasy`: the viewer-facing promise
- `Recommended first version`: the narrowest buildable V1
- `Emergence levers`: where surprising strategies may come from
- `Visual hooks`: what will read well on screen
- `Risks to avoid in V1`: what to keep out of the first implementation
- `Good next step`: usually `scenario-spec`
Keep the memo short, directional, and stable enough to revisit later.

## Creative Standard

- Optimize for concepts that read instantly on video.
- Favor visible cause-and-effect, escalating chaos, near misses, fragile objects, and clear outcomes.
- Prefer one strong gimmick over many weak mechanics.
- Make failure entertaining. A reset should still look funny, tense, awkward, or dramatic.
- Use contrast aggressively: tiny agent vs giant system, calm setup vs sudden disaster, neat task vs absurd environment.
- Default to ideas that can become a good thumbnail, title, and 10-second intro clip.

## Feasibility Guardrails

- Keep scenario scope narrow enough that one clear learning loop exists.
- Favor environments whose rules can be explained in a few sentences.
- Prefer short-to-medium horizon tasks over sprawling multi-stage missions.
- Use scripted set dressing to create the illusion of richness before adding real system complexity.
- Treat imported assets as optional polish, not a prerequisite for the core idea.
- When in doubt, simplify the agent behavior and enrich the spectacle through level design, props, camera angle, and reset choreography.

## Response Shape

- For broad ideation, usually return 4-8 ideas with deliberate tonal spread.
- For shortlist requests, return 3-5 stronger candidates with clearer tradeoffs.
- Keep each idea compact and concrete.
- Avoid vague genres like "survival" or "puzzle" without a specific visual setup.
- If the user asks for "more," pivot to a new emotional lane or spectacle lever instead of remixing the same concept.

## Hand-off

- Use `references/idea-lenses.md` when ideas are too similar, too safe, or too difficult.
- Once the user selects a concept, create a concept memo in `docs/ideas/` before moving to `scenario-spec`.
- If the concept depends heavily on new visuals, free assets, or third-party content, consider `unity-free-asset-research` or `asset-intake` after the concept is chosen.
