---
name: unity-mcp-safe-ops
description: Safely perform Unity scene and asset editing through MCP for this repository. Use when creating or modifying GameObjects, components, references, cameras, prefabs, materials, or scene wiring through Unity MCP and the change could break validator assumptions, serialized references, training setup, or recording flow.
---

# Unity MCP Safe Ops

## Overview

Reduce Unity editing mistakes when working through MCP instead of a fully interactive editor workflow.
Apply small, validated edit loops that preserve scene integrity, serialized references, and RL-specific requirements.

## Safe Edit Loop

1. Read the target scene state before editing.
Inspect the current GameObjects, required references, and nearby naming patterns before creating or rewiring anything.
For RL scenarios, always keep the validator expectations in mind: agent, behavior settings, visualizer, recording helper, config files, and manifest alignment.

2. Make the smallest coherent batch of edits.
Prefer one logical batch such as "create camera anchors and wire `RecordingHelper`" or "add one serialized field and assign it" over a long chain of unrelated scene mutations.
If the task is large, split it into batches and validate between them.

3. Save and inspect the immediate fallout.
After an edit batch, save the scene and inspect Console output before stacking on more changes.
Resolve obvious serialization or missing-reference issues immediately.

4. Run the RL gates at the right points.
Use `RLMovie > Validate Current Scenario` after any batch that affects components, references, or config alignment.
Run `Heuristic` confirmation before considering the change done.
If the edit affects training/export readiness, treat `Build for Colab` as the final gate.

5. Use the checklist in `references/mcp-edit-checklist.md`.
Follow the task-specific checks for creation, rewiring, camera edits, and prefab-related work.
When uncertain, favor safety over speed.

## Guardrails

- Do not make broad scene edits without first reading the current state.
- Do not assume serialized fields auto-populate.
- Do not trust a successful object creation step as proof the scenario is valid.
- Prefer keeping RL logic edits in code and visual/layout edits in Unity; do not blur them unless necessary.
- Stop and re-evaluate when a single edit batch causes multiple new console errors.

## Common Risk Areas

- Missing serialized references on `BaseRLAgent` children.
- `TrainingVisualizer.targetAgent` not assigned.
- `RecordingHelper` camera positions fewer than expected.
- `Behavior Name`, manifest values, and training YAML drifting apart.
- Prefab or scene hierarchy names diverging from established scenario patterns.

## Response Shape

- Say which batch of edits you are making.
- State the validation checkpoint that follows it.
- Report any console or validator fallout before proceeding to another batch.
