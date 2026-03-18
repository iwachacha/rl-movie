# Shot Package

Use this file to design the camera package as an editorial tool.
Successful RL videos usually win with stable, readable wide shots first, then use a closer or follow cut only when it adds personality or precision.

## 1. Explain Shot

Purpose:
- explain hero, goal, hazard, and space immediately
- make the first episode readable without narration

Usually:
- higher 3/4 angle
- enough distance to see the core relationship
- static anchor, not a moving camera

Bad signs:
- looks cool but hides the target
- only makes sense after the agent has already moved
- failure source is off-screen

## 2. Wide A

Purpose:
- be the main readable gameplay shot
- show cause and effect from a different side than the explain shot

Usually:
- diagonal or side-biased anchor
- keeps the active lane and upcoming danger visible

Bad signs:
- duplicates the explain shot too closely
- compresses depth so hazards and goals overlap

## 3. Wide B

Purpose:
- give editorial variation without changing the information model
- reveal a different class of failure or spacing

Usually:
- mirrored diagonal, opposing side, or a more side-on cut

Bad signs:
- exists only because "three cameras feels better"
- rotates the viewer so much that they have to relearn the space

## 4. Follow Shot

Purpose:
- reveal body language, timing, comedy, near misses, or uncanny competence

Use only when:
- the agent's movement quality matters
- a static wide shot undersells the behavior
- context is still recoverable from other cuts

Do not use as:
- the only recording shot
- a replacement for readable wide coverage

## 5. Comparison Shot

Purpose:
- compare bad early behavior vs adopted run
- compare baseline vs new run

Use:
- a matching angle or nearly matching angle
- stable framing that makes the improvement obvious

Avoid:
- changing angle so much that the comparison becomes editorial instead of informative

## Default Package Rule

If unsure, start with:
- one explain shot
- two readable wides
- one follow shot only if justified

That pattern fits the repo's V2 scenario starter and matches the "clear first, expressive second" pattern seen in successful RL and simulation videos.
