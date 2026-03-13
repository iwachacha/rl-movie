# Unity Free Asset Evaluation Rubric

Use this file only when actively researching or comparing candidates.

## Minimum Inputs

Capture these before searching when they are available:

- Asset category: environment, characters, animation, UI, VFX, audio, tools, shaders, templates, etc.
- Intended use: prototype speed, final visuals, placeholder art, tooling, or learning/demo.
- Technical constraints: Unity version, Built-in/URP/HDRP, target platform, package size tolerance.
- Content constraints: art style, realism level, poly budget, animation rigging, licensing concerns.

## Hard Filters

Reject a candidate if any of these is true:

- Current price is not clearly `Free` on the live Unity Asset Store page.
- The page suggests the asset is only useful with a separate paid dependency.
- The asset clearly conflicts with required render pipeline, Unity version, or platform constraints.
- The listing is so sparse that fit or compatibility cannot be judged and better free options exist.

## Evidence To Record

For each serious candidate, capture:

- Asset name and URL.
- Current price.
- Publisher.
- Average rating and number of ratings/reviews.
- Favorites count if shown.
- Latest release date or most recent update date shown on the page.
- Original Unity version.
- Render pipeline compatibility.
- File size when setup weight matters.
- One-line note on what it is best at.
- One-line note on the biggest risk or caveat.

## Ranking Heuristic

Rank in this order:

1. Problem fit.
2. Technical compatibility and integration friction.
3. Social proof.
4. Maintenance health.
5. Nice-to-have polish.

### Problem Fit

Prefer assets that directly solve the requested job with minimal kitbashing.
Example: for "stylized low-poly city backgrounds," a strong city/environment pack beats a generic prop bundle even if the prop bundle is more popular.

### Technical Compatibility

Prefer assets that:

- Explicitly support the required render pipeline.
- Do not force a large conversion workflow.
- Match the user's Unity version or at least do not signal obvious version risk.
- Have a manageable package size for the likely use.

### Social Proof

Use all three together:

- Star rating.
- Rating/review count.
- Favorites count.

Interpretation rules:

- High rating with very low review count is weak evidence.
- Large favorites with modest reviews suggests broad interest but not necessarily easy adoption.
- A slightly lower star rating with much larger review volume can be the safer recommendation.

### Maintenance Health

Use update recency as a risk signal, not an automatic disqualifier.
Older assets can still be good if they fit well and have strong adoption, but call the age risk out explicitly.

## Secondary Sources

Use secondary sources only when the top candidates are close or the official page is thin.
Good tie-breakers:

- Unity Discussions threads about real-world usage.
- GitHub issues or repos showing active use.
- Reddit threads with practical integration feedback.
- Video reviews or walkthroughs from creators demonstrating the asset.

Treat these as anecdotal.
Do not let one enthusiastic opinion outweigh weak official evidence.

## Recommended Output

Use a compact structure:

1. Best overall pick for this request.
2. Shortlist with evidence and caveats.
3. Why the winner beats the runner-up.
4. Backup options or "no strong fit" statement.
5. Research date and any assumptions.
