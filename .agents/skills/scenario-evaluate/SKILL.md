---
name: scenario-evaluate
description: Compare RL training runs for this repository and recommend whether to adopt, reject, or continue iterating on a model. Use when reviewing `run_summary.json`, evaluation notes, videos, imported `.onnx` candidates, baseline comparisons, or any request about which run is best, whether a model is good enough, or why a run should or should not be promoted.
---

# Scenario Evaluate

## Overview

Turn scattered training artifacts into a concrete adoption decision.
Judge runs against the intended hypothesis and baseline instead of choosing the highest-looking reward by instinct.

## Workflow

1. Gather the comparable artifacts first.
Look for `run_summary.json`, `run_id`, `spec_version`, `baseline_run`, training config, evaluation video, and the candidate `.onnx` if one exists.
If key artifacts are missing, say what is missing before making a strong recommendation.

2. Verify that the runs are actually comparable.
Do not compare runs as if they are equal when `spec_version`, reward rules, action space, observation contract, max steps, or success criteria changed.
If the contract changed materially, frame the result as "informative but not apples-to-apples."

3. Evaluate from outcome backward.
Start with the question that matters: does the agent reliably do the intended thing in a way that would look acceptable on video and survive import as `Inference Only`.
Use metrics to support that judgment, not replace it.

4. Use the rubric in `references/adoption-rubric.md`.
Check success behavior, failure modes, stability, baseline lift, training efficiency, and import readiness.
Prefer a slightly worse peak metric with clearer stable behavior over a flashy but brittle run.

5. Return a decision, not just a summary.
End with one of: `Adopt`, `Keep as baseline`, `Needs another run`, `Reject`.
State the main reason in one sentence and the next action to take.

## Guardrails

- Treat `1 run = 1 hypothesis` as a hard framing rule.
- Do not recommend adoption if the only evidence is reward movement without behavioral confirmation.
- Flag confounders such as seed changes, reward edits, curriculum changes, or mismatched baselines.
- Separate "learned faster" from "learned better."
- Call out when a model is good enough for recording even if it is not the best research result.

## Response Shape

- Open with the decision and whether the comparison is trustworthy.
- Then give a compact comparison of the top runs.
- Include the winner's strengths, the biggest remaining risk, and why the runner-up lost.
- End with the next action: import, retrain, record, or redesign the scenario.

## Handoffs

If the result is adoptable, hand off to `scenario-train` for import steps, `scenario-record` for final capture, or `rl-video-packaging` when the user wants a concrete upload package.
If the result is adoptable but the user wants to know whether the artifact chain is actually strong enough as content, hand off to `rl-video-quality-gate`.
If the result is not adoptable, hand off to `scenario-fix`, `scenario-spec`, or `rl-instrumentation` depending on whether the issue is implementation, design, or observability.
