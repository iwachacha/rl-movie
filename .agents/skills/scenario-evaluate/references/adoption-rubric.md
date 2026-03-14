# Scenario Adoption Rubric

Use this file when choosing between runs or deciding whether a model is good enough to import.

## Required Comparison Inputs

- `run_id`
- `spec_version`
- `baseline_run`
- hypothesis summary
- training config differences
- any `run_summary.json`
- evaluation video or behavioral notes
- candidate `.onnx` if one exists

## Decision Order

Rank in this order:

1. Behavioral success.
2. Comparison integrity.
3. Stability.
4. Efficiency.
5. Presentation readiness.

## Behavioral Success

Ask:

- Does the agent consistently complete the intended task?
- Does it fail in an obvious or embarrassing way?
- Would this behavior be acceptable in an imported `Inference Only` scene?
- Would this look convincing enough to record?

Reward curves do not override obviously bad behavior.

## Comparison Integrity

Downgrade confidence when any of these changed between runs:

- `spec_version`
- reward rules
- observations
- actions
- curriculum or randomization
- max steps
- seed policy

If core task rules changed, compare directionally rather than declaring a strict winner.

## Stability

Prefer runs that:

- hold performance late in training
- do not show frequent collapse or oscillation
- behave similarly across repeated checks
- fail gracefully rather than randomly

## Efficiency

Use efficiency as a tie-breaker:

- learns faster to a usable level
- needs fewer steps for similar quality
- avoids unnecessary environment complexity

Do not overvalue a tiny training-speed gain if behavior quality drops.

## Presentation Readiness

A run can be "recording-ready" even if it is not "research-best."
Call this out explicitly when it matters.

Checks:

- imports cleanly
- works in `Inference Only`
- produces understandable visuals
- does not rely on debug-only scaffolding

## Output Recommendation

End with one status:

- `Adopt`
- `Keep as baseline`
- `Needs another run`
- `Reject`
