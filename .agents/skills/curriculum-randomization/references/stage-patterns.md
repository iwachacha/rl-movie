# Curriculum and Randomization Patterns

Use this file when deciding how to introduce difficulty or variation.

## Stage Design Rule

Each stage should introduce one main challenge.
Examples:

- increase distance
- add moving obstacles
- narrow the success tolerance
- shorten the available time

If two stages differ in many ways, the learning signal becomes harder to interpret.

## Common Stage Progressions

### Navigation Tasks

1. Fixed start and fixed goal.
2. Fixed start and randomized goal.
3. Randomized start and goal.
4. Add obstacles or tighter spaces.
5. Add time pressure or distractors.

### Precision Tasks

1. Large target and relaxed timing.
2. Medium target.
3. Smaller target or stricter alignment.
4. Add disturbance or noise.

### Avoidance Tasks

1. Slow hazard and predictable path.
2. Faster hazard.
3. Multiple hazards.
4. Randomized hazard timing or placement.

## Randomization Knobs

Good early knobs:

- spawn position
- target position
- obstacle count within a narrow range

Later knobs:

- material or lighting variation
- camera angle variation
- distractor objects
- speed or timing noise

## Anti-Patterns

- adding broad randomization before the base task is learnable
- changing reward rules and randomization in the same run without a clear reason
- making the easiest stage too trivial to teach the real task
