# Lighting And Volume Recipes

The repo uses URP, but broad pipeline edits are still high-risk.
Prefer scene-local lights and Global Volume adjustments before touching shared renderer or pipeline assets.
Treat the numbers below as starting points, not fixed laws.

## Clean Arena Daylight

Use when:
- the scenario needs maximum readability
- the space is mostly open

Starting point:
- Directional Light: warm-neutral, intensity `1.2-1.5`
- Fill Light: cool-neutral, intensity `0.2-0.35`, no shadows
- Fog: off or extremely light
- Global Volume:
  - Tone Mapping: `ACES` or `Neutral`
  - Ambient Occlusion: subtle to medium
  - Bloom: `0-0.15`
  - Color Adjustments: slight contrast lift only

Best for:
- obstacle courses
- timing arenas
- traversal or pickup tasks

## Warm Industrial Hazard

Use when:
- hazards or machines should feel dramatic
- the scene needs heat, tension, or danger

Starting point:
- Main Light: warm key, intensity `0.8-1.2`
- Fill: cooler counter-light, intensity `0.2-0.3`
- Accent Lights: emissive hazard lights or spotlights near danger zones
- Fog: light, only if it improves depth
- Global Volume:
  - Tone Mapping: `ACES`
  - Ambient Occlusion: medium
  - Bloom: `0.15-0.35`
  - Saturation: slightly reduced unless the hook depends on vivid hazard color

Best for:
- reactor rooms
- factory floors
- lava, fire, electricity, or alarm-driven spaces

## Cool Lab Or Night Sim

Use when:
- the tone should feel controlled, eerie, or technical
- the space is cleaner and less chaotic

Starting point:
- Key Light: cool, intensity `0.7-1.1`
- Fill: neutral, intensity `0.15-0.25`
- Rim or back light: optional, only if the agent needs silhouette help
- Fog: low and clean, avoid murk
- Global Volume:
  - Tone Mapping: `ACES` or `Neutral`
  - Ambient Occlusion: medium
  - Bloom: `0.05-0.2`
  - Contrast: modest increase

Best for:
- lab rooms
- stealth-like spaces
- eerie competence or documentary-style observation

## Camera And Volume Sanity Checks

If volume changes do not appear to work, check:
- camera HDR
- camera post-processing enabled
- the intended volume is active and has priority
- the effect is helping the recording views, not only the Scene tab

## Anti-Patterns

- Do not solve a flat scene with bloom alone.
- Do not make the agent readable only through emissive glow.
- Do not use fog so thick that pathing or impacts become ambiguous.
- Do not tune one hero shot so hard that all other recording views break.
