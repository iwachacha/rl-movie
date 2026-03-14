---
name: unity-free-asset-research
description: Research the best currently-free Unity Asset Store assets for a specific game, scene, prototype, UI, VFX, environment, character, or tooling idea. Use when the user asks which free Unity assets fit a concept, wants high-quality free options only, or needs a ranked shortlist based on live store data such as ratings, review volume, favorites, update recency, render pipeline compatibility, and setup risk.
---

# Unity Free Asset Research

## Overview

Find strong Unity Asset Store candidates for a concrete build idea without suggesting paid assets.
Use live web research, keep "free only" as a hard rule, and rank candidates by idea fit before popularity.

## Workflow

1. Translate the request into asset-search criteria.
Ask only for missing constraints that materially change the answer: asset type, style, platform, render pipeline, Unity version, animation needs, poly budget, and whether the asset is visual-only or affects gameplay/tooling.
If the user gives only a rough idea, infer a sensible first-pass search scope and state the assumption after researching.

2. Research the current Unity Asset Store before recommending anything.
Browse live pages on `assetstore.unity.com`; do not rely on memory for price, ratings, review count, compatibility, or update status.
Treat the official Asset Store page as the primary source.
Use secondary sources only when official signals are too thin and the comparison is still ambiguous.

3. Apply hard filters before ranking.
Require the current listed price to be `Free` or equivalent zero-cost pricing on the live store page.
Reject paid assets, bundles that require a paid dependency to be useful, or listings whose current price cannot be confirmed.
Reject candidates that clearly miss required render pipeline, Unity version, or platform constraints.

4. Compare candidates with the rubric in `references/evaluation-rubric.md`.
Capture the specific evidence that matters: title, link, current price, publisher, rating, rating count, favorites, latest release date, original Unity version, render pipeline compatibility, package size when relevant, and notable caveats.
Prioritize fit to the requested idea over raw popularity.

5. Recommend a small, opinionated shortlist.
Usually return 3 primary picks and 1-3 backups.
Explain why the top pick wins for this exact use case, not just why it is popular overall.
If nothing is a strong fit, say that plainly and offer the closest free compromises.

## Guardrails

- Keep "free only" absolute.
- Prefer assets with stronger social proof when fit is similar, but do not over-trust a high star rating with very few reviews.
- Treat favorites and popularity as supporting signals, not replacements for compatibility and maintenance checks.
- Call out stale packages, old original Unity versions, unclear pipeline support, or large setup overhead as risks.
- Clearly label any inference or unknown.
- Include the date of research in the response because store data changes.

## Response Shape

- Start with the best recommendation in one sentence.
- Follow with a compact comparison table or tight bullet list for the shortlist.
- For each candidate, include why it fits, what evidence supports it, and the main caveat.
- End with a practical next step.
- If the user decides to import or evaluate installation risk inside this repo, load `asset-intake` next.

## Secondary Sources

Use outside sources only to break close ties or validate community sentiment.
Prefer Unity-owned or community-primary sources such as Unity Discussions, GitHub, Reddit, or creator videos/posts that discuss real usage.
Summarize them briefly and distinguish anecdotal sentiment from official store facts.
