# RL Diagnostics Patterns

Use this file when choosing what instrumentation to add.

## Reward Buckets

Use when the total reward moves but the cause is unclear.

Patterns:

- Track per-episode totals for each reward source.
- Keep a final episode summary string or log line.
- Distinguish dense shaping rewards from sparse terminal rewards.

Questions answered:

- Which reward term dominates?
- Is the agent farming shaping reward?
- Is success reward too weak to matter?

## Termination Reasons

Use when many episodes end but the reason is unclear.

Patterns:

- Count success, timeout, collision, out-of-bounds, and invalid-state endings separately.
- Store the last termination reason in a field visible to debug UI.
- Emit one summary per episode, not per frame.

Questions answered:

- Is the agent failing for one dominant reason?
- Are "fails" actually timeouts?
- Is reset logic ending episodes prematurely?

## Observation Sanity

Use when the agent appears blind, unstable, or inconsistent.

Patterns:

- Assert or log when observations become NaN or Infinity.
- Sample representative values at low frequency.
- Compare intended normalization range with actual values seen in play.

Questions answered:

- Are observations finite?
- Are values wildly off-scale?
- Is a field stuck at zero or a constant?

## Reset-State Checks

Use when early episode behavior looks random or impossible.

Patterns:

- Log or expose spawn positions, target positions, and randomization knobs at episode start.
- Confirm rigidbody velocity is cleared on reset.
- Verify randomization is inside the intended bounds.

Questions answered:

- Are resets actually happening?
- Is randomization too hard or too easy?
- Is the agent inheriting stale state?

## Minimalism Rule

Start with the smallest pattern that answers the current question.
If a model is simply not learning, reward buckets and termination reasons usually pay off first.
