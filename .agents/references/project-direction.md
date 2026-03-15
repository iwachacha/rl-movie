# RL Movie Project Direction

## Primary Goal

- Build RL simulations that are worth watching as short videos, not just technically valid experiments.
- Optimize for the shortest route to a high-quality result with Unity, UnityMCP, ML-Agents, and Codex.
- Make each scenario understandable and interesting even to viewers with no RL background.

## Content Priorities

1. A clear viewer hook.
2. One readable learning loop with visible cause and effect.
3. Strong visual leverage from existing scene dressing and free Asset Store assets.
4. Fast, low-risk implementation.
5. Research novelty only when it materially improves the video.

## Asset Policy

- External assets are allowed and encouraged when they shorten the route to a better-looking result.
- Only currently-free Unity Asset Store assets are eligible. Do not recommend, plan around, or import paid assets.
- Prefer assets that improve readability, scale, spectacle, or atmosphere without entangling core RL logic.
- Use current repo inventory first when it is already good enough; otherwise research free Asset Store options; then isolate intake risk with `asset-intake`.

## Scenario Bias

- Prefer scenarios whose goal, danger, and outcome are readable in seconds.
- Prefer spectacle from environment, hazards, props, camera framing, and resets over complex hidden cognition.
- Prefer a narrow V1 with one memorable gimmick over a wide feature set.
- Avoid concepts whose appeal depends on subtle reward shaping that is invisible on video.
- If two concepts are similarly learnable, choose the one with better thumbnail and first-10-second potential.

## Recording Bias

- A viewer should understand what is happening without debug UI or ML vocabulary.
- The first recorded cut should establish the fantasy, goal, and failure risk quickly.
- Keep the action legible with strong silhouettes, landmarks, contrast, and camera angles that show cause and effect.

## Decision Shortcuts

- Choose "looks great and trains simply" over "clever but visually flat."
- Choose "free asset plus simple mechanic" over "custom build plus marginally better control."
- Choose "clear spectacle" over "ambiguous sophistication."
