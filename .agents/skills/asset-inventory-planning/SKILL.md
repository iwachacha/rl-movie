---
name: asset-inventory-planning
description: Catalog and reason about third-party assets that are already imported in this repository. Use when the user asks what assets are available now, what each asset can do, which assets combine well for a scenario, whether an idea can be built from the current asset inventory before researching more, or when asset inventory docs under `docs/` and `Assets/ThirdParty/` need to be refreshed.
---

# Asset Inventory Planning

## Overview

Answer asset-library questions from the repo's current contents, not from memory of store listings.
Focus on `what we already have`, `what it enables`, and `what combinations are practical` before suggesting more imports.

## Workflow

1. Start from the human-facing inventory.
Read `docs/ASSET_CATALOG.md`, `docs/ASSET_USAGE_GUIDE.md`, and `AI-RL-Movie/Assets/ThirdParty/README.md` first.
Use them as the initial map of asset names, paths, and intended roles.

2. Inspect `Assets/ThirdParty/` only when the request is explicitly about the current asset library or the docs may be stale.
The repo-wide default is to avoid reading `ThirdParty/` casually, but this skill is the place where that inspection is justified.
Prefer shallow inspection first: folder names, major subfolders, prefab/model/animation counts, demo scenes, and obvious pipeline helpers.

3. Normalize naming.
Map `Asset Store name`, `folder name`, and `how the team refers to it` when they differ.
Prefer the imported folder name when discussing exact paths, but mention the store-facing name when it helps recognition.

4. Separate capabilities by role.
Classify assets into roles such as:
- environment / set dressing
- characters / creatures
- animation
- VFX / feedback
- props / weapons / pickups
- tools / systems / sample-project references

5. Answer in three layers when possible.
- `Inventory snapshot`: what exists now
- `Capability map`: what each asset can realistically contribute
- `Combination map`: which bundles create strong scenario directions with low additional work

6. Distinguish what is visual-only from what can affect gameplay or authoring.
Examples:
- skyboxes, props, and particles are usually visual-only
- animation packs can affect recording quality and perceived intelligence
- sample projects and tween libraries may change implementation options, not just visuals

7. Keep combinations grounded in this repo.
Favor combinations that support the existing ML-Agents workflow and avoid bundles that imply major new systems unless the user explicitly wants that jump.
If an answer starts drifting into "we should import more assets", hand off to `unity-free-asset-research`.

## Coordination

- Pair with `scenario-ideation` when the user wants ideas constrained by assets already in the repo.
- Pair with `asset-intake` when the user wants to move, integrate, prune, or safely adopt assets in scenes.
- Pair with `unity-free-asset-research` when the current inventory is insufficient and the user wants additional free assets.

## Doc Updates

When inventory knowledge changes or new assets are imported, keep the human-facing docs aligned:

- `docs/ASSET_CATALOG.md`
  Update category, source, license, purpose, path, and registration date.
- `docs/ASSET_USAGE_GUIDE.md`
  Update what the asset is good at, what kinds of prompts should mention it, and what asset pairings are promising.
- `AI-RL-Movie/Assets/ThirdParty/README.md`
  Keep the short registry in sync with the actual folders.

Keep these docs descriptive rather than exhaustive dump lists.
Summarize reusable capability, not every single prefab.

## Output Shape

For asset-planning requests, prefer a compact structure like:

- `Have now`: the most relevant current assets
- `Can do`: what those assets enable
- `Best combinations`: 2-4 strong bundles for likely scenario directions
- `Gaps / caveats`: what is still missing, risky, or stale in docs
- `Next move`: ideation, intake, or external research

When making inferences from folder contents, say so plainly.
