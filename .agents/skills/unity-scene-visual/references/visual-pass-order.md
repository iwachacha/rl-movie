# Visual Pass Order

Use this file to structure a scene polish pass.
Work from readability to atmosphere, not the other way around.

## 1. Silhouette And Readability

Check first:
- Can the hero agent be picked out instantly from the floor and walls?
- Is the target or reward object visibly distinct?
- Is the main hazard recognizable at a glance?

Improve by:
- separating colors or value ranges between agent, floor, and hazards
- simplifying noisy materials around the play lane
- using stronger edge contrast around the active area

## 2. Ground Plane, Scale, And Contact

Check first:
- Does the arena feel grounded rather than floating or template-like?
- Are object sizes and spacing readable?

Improve by:
- adding clear floor borders, trims, decals, or hazard markings
- placing a few scale cues at the arena perimeter
- fixing floating props and improving contact shadows or grounding cues

## 3. Lighting Hierarchy

Goal:
- brightest emphasis near the hero area, target, or most important motion
- darker or quieter treatment at the periphery

Improve by:
- one clear key light direction
- a softer fill to keep shadows readable
- optional rim or accent light when the agent blends into the background

## 4. Dressing And Background Breakup

Add detail only after the action reads.

Improve by:
- clustering props instead of spacing them evenly
- leaving deliberate negative space in the play lane
- putting richer detail at edges, corners, walls, or far background planes

## 5. Local Volume And Post Polish

Use sparingly.

Usually worthwhile:
- tone mapping
- subtle ambient occlusion
- light color adjustments
- light fog only when it improves depth

Usually risky:
- heavy bloom
- strong vignette
- dense fog
- depth of field during gameplay

## 6. Camera Review

Review from:
- the default scene view the player or editor will use
- the widest recording shot
- any follow or side shot used by `RecordingHelper`

Confirm:
- the episode start reads clearly
- failure moments are readable
- success moments are visible without explanation
- the scene still reads with debug UI hidden

## Stop Conditions

Stop and simplify if:
- detail keeps getting added but the action reads worse
- the scene looks cinematic only from one angle
- the goal, threat, and floor start sharing the same value or color family
- atmosphere hides collisions, pathing, or reset behavior
