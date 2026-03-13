---
name: rl-instrumentation
description: Add or refine lightweight diagnostics for ML-Agents scenarios in this repository. Use when rewards are hard to interpret, failures are opaque, observations or actions may be wrong, training looks unstable, or a scenario needs better counters, debug output, or visualizer support without changing the core task more than necessary.
---

# RL Instrumentation

## Overview

Make RL behavior observable before changing learning logic blindly.
Instrument reward flow, episode endings, and sanity checks in a way that helps debugging while keeping the training contract stable.

## Workflow

1. Identify the hidden question first.
Decide whether the real unknown is reward composition, termination reason, observation correctness, action application, reset logic, or environment randomness.
Instrument the smallest thing that answers that question.

2. Prefer additive diagnostics over behavioral changes.
Start with counters, labels, debug summaries, visualizer fields, and explicit helper methods.
Do not quietly change reward magnitude, termination timing, or observation structure unless the request is actually about fixing those.

3. Anchor instrumentation around `BaseRLAgent`.
Use existing hooks such as `OnEpisodeBegin`, `CollectObservations`, `OnActionReceived`, `Success`, `Fail`, and `AddTrackedReward`.
When possible, expose information through narrowly-scoped fields or helper methods rather than spraying logs everywhere.

4. Use the patterns in `references/diagnostics-patterns.md`.
Pick one or two patterns that match the bug class: reward buckets, termination reasons, observation sanity dumps, or reset-state checks.
Keep the first pass minimal and easy to remove or evolve.

5. Verify the instrumentation path.
After edits, ensure the scene still validates, runs in `Heuristic`, and surfaces the new signal in Console, inspector, or `TrainingVisualizer` as intended.

## Guardrails

- Favor structured counters over noisy per-frame logs.
- Prefer episode-level summaries to action-level spam.
- Keep debug-only fields clearly named and easy to disable.
- Do not add instrumentation that changes observation count or action semantics unless explicitly intended.
- If a diagnostic change would affect training behavior, say so clearly.

## Typical Targets

- Reward decomposition: show which sub-events actually drive return.
- Failure analysis: count timeout, collision, out-of-bounds, invalid state, and success endings separately.
- Observation sanity: confirm values are finite, normalized, and aligned with the intended contract.
- Reset integrity: confirm episode start state is deterministic where expected and randomized where intended.
- Presentation checks: expose just enough state for `TrainingVisualizer` or short-term console review.

## Response Shape

- State what you are instrumenting and why.
- List the exact signals being added.
- Note whether the change is training-neutral or behavior-affecting.
- End with how to verify the new signal locally.
