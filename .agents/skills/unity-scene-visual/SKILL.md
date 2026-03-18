---
name: unity-scene-visual
description: Improve Unity scene readability and polish for RL Movie through composition, lighting, dressing, materials, and local URP volume choices. Use when a scene built or edited in Unity or UnityMCP looks flat, empty, confusing, or low-fidelity; when the user asks to make a scene prettier, cleaner, more cinematic, more readable, or more thumbnail-worthy; or when planning visual-only passes before recording.
---

# Unity Scene Visual

## Overview

Raise visual quality without sacrificing RL readability.
Prefer focal hierarchy, silhouette separation, depth, and camera-aware dressing over prop spam or generic "cinematic" effects.
Keep changes scene-local by default; treat shared render-pipeline assets and project-wide graphics settings as high-risk.

## Workflow

1. Frame the pass.
Classify the request as one of:
- `Clarity pass`: the scene is readable but ugly or flat.
- `Mood pass`: the hook needs stronger atmosphere or style.
- `Final polish`: the scene already works and needs a last visual lift before recording.

2. Load the right references.
Read `../../references/project-direction.md` for the repo's readability and watchability bias.
Read `../../references/video-standard.md` when the pass affects camera readability, viewer comprehension, or recording.
Read `references/style-lanes.md` to choose a visual lane.
Read `references/visual-pass-order.md` to structure the edit sequence.
Read `references/lighting-recipes.md` only when adjusting lights, fog, materials, or local volume settings.
If the main lever is camera framing, recording coverage, or shot design rather than dressing or lighting, load `../unity-rl-camera/SKILL.md`.

3. Pick one visual lane.
Default to one of:
- `Broadcast Arena`: clean, contrasty, easy to read, best for most RL V1s.
- `Stylized Setpiece`: richer atmosphere and spectacle without losing play clarity.
- `Simulation Documentary`: calm, elegant, system-focused visuals for emergent behavior.
Use one lane on purpose instead of mixing unrelated moods.

4. Lock the viewer read before adding detail.
Identify:
- hero agent
- target or reward object
- main hazard or failure source
- playable lane or arena center
- one hero shot that could plausibly become the thumbnail
When `ScenarioGoldenSpine` exists, compose for the bound camera roles (e.g. `default_camera`, `explain`, `wide_a`), not only the Scene view.

5. Apply the visual pass in order.
Use this order unless a narrower request clearly changes it:
- silhouette and color separation
- ground plane, scale cues, and contact points
- lighting hierarchy
- dressing clusters and background breakup
- local volume or post polish
- camera recheck from recording views

6. Keep the pass RL-safe.
- Prefer visual-only scene changes over logic, collider, reward, or timing changes.
- Do not make the play space harder to read in exchange for atmosphere.
- If the scene needs outside visuals, use `unity-free-asset-research` first and `asset-intake` when import or usage risk matters.
- When editing through MCP, also use `unity-mcp-safe-ops`.

7. Escalate before broad render changes.
- Do not edit `ProjectSettings/`, shared URP pipeline assets, shared renderer assets, or global quality tiers unless the user explicitly asks.
- Prefer scene-local lights, materials, decals, sky/fog choices, camera anchors, and Global Volume tuning.

## Guardrails

- Do not fill empty space just because it exists.
- Do not let background detail compete with the agent, goal, or hazard.
- Do not rely on darkness, bloom, or fog to fake quality.
- Do not overuse VFX where they obscure motion, collisions, or resets.
- Prefer one accent color and one dominant material family per scene.
- Keep the first episode view understandable even with debug UI hidden.

## Response Shape

- State the chosen lane.
- State the hero shot.
- State the planned pass order or edit batches.
- State what you will not touch.
- End with the camera or validation view you will use to confirm the result.
