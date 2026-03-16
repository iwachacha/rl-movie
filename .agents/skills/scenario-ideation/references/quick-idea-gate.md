# Quick Idea Gate

Use this file after broad ideation and before deeper benchmarking or concept lock.
It is a fast repo-native filter for choosing which ideas deserve more attention.
Do not use it to replace `rl-video-benchmarking`; use it to avoid benchmarking weak concepts.

## When To Use It

- Narrowing a long list into a shortlist
- Choosing one recommendation and one wild-card pick
- Deciding whether an idea is strong enough to formalize in `docs/ideas/`
- Checking whether a concept is watchable before investing in asset research or packaging work

For very broad brainstorming, keep this gate mostly implicit.
Use explicit scoring only when comparison or prioritization matters.

## The Five Axes

Score each axis from `1` to `3`.
The goal is not precision. The goal is to catch weak concepts early.

### 1. Emotional Spike

Does the idea naturally create at least one strong viewer reaction?

- `1`: emotionally flat, mostly technical, or only interesting after explanation
- `2`: some surprise, humor, tension, awe, or weirdness, but not consistently
- `3`: obvious surprise, laughter, eerie competence, exploit potential, or strong rooting interest

### 2. Instant Legibility

Can a non-RL viewer understand the actor, goal, and danger within a few seconds?

- `1`: needs narration, debug UI, or reward explanation to make sense
- `2`: understandable after a short setup, but not instantly
- `3`: the rule reads straight from the footage

### 3. Entertaining Failure

If the policy is bad, is that still worth watching?

- `1`: failed runs are static, repetitive, or visually ambiguous
- `2`: some bad runs are watchable, but the reset or failure beat is weak
- `3`: crashes, near misses, awkward retries, or collapses are assets, not dead time

### 4. Build Leverage

Can V1 be built and trained in this repo with ordinary Unity and ML-Agents patterns plus existing or free Asset Store visuals?

- `1`: likely research-heavy, asset-fragile, or too broad for a narrow V1
- `2`: feasible with simplification, stronger staging, or careful scope control
- `3`: strong fit for the repo's current build and training lane

### 5. Packaging Potential

Is there already a simple title promise, thumbnail contrast, or first-10-second shot hiding in the premise?

- `1`: the best part is invisible, hard to title, or hard to thumbnail
- `2`: packaging is possible, but the hook needs reframing
- `3`: the idea already implies a strong verb, contrast, or reveal

## Reading The Result

- `12-15`: strong shortlist candidate
- `9-11`: promising, but simplify, restage, or sharpen the hook first
- `5-8`: weak fit for the main video lane unless the brief explicitly wants niche or experimental work

When two ideas land close together:

- prefer the one with better `Instant Legibility` and `Packaging Potential`
- if those are equal, prefer the one with better `Build Leverage`

## Fast Red Flags

Be skeptical if the idea:

- only sounds good when explained in ML terms
- looks almost the same in success and failure
- depends on dense multi-agent coordination to become interesting
- needs a huge content build before the first watchable run exists
- has no obvious cold-open shot, contrast, or memorable exploit

If the premise is exciting but fails this gate, simplify the mechanic and improve the staging instead of discarding the fantasy immediately.
