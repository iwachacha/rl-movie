# Unity MCP Edit Checklist

Use this file during Unity MCP scene work.

## Before Editing

- Confirm the active scene and target scenario.
- Read the nearby hierarchy and naming pattern.
- Identify every serialized reference that the change touches.
- Decide the smallest safe edit batch.

## After Creating Objects

- Verify the object landed in the intended parent.
- Verify transform defaults make sense.
- Verify expected components exist.
- Save before doing another large batch.

## After Rewiring References

- Re-open or inspect the relevant component fields.
- Confirm no required field stayed null.
- If an array or list changed, confirm count and ordering.

## After Camera or Recording Edits

- Confirm `RecordingHelper` still exists.
- If camera switching is enabled, ensure at least two camera positions remain assigned.
- Confirm the main camera still gives a sane view.

## After Agent-Related Edits

- Confirm `BehaviorParameters` and `DecisionRequester` remain attached.
- Confirm `Behavior Name` still matches the agent class and training YAML.
- Confirm any new `SerializeField` object references are assigned.
- Confirm `TrainingVisualizer.targetAgent` still points to a live agent.

## Validation Loop

1. Save scene.
2. Inspect Console.
3. Run `RLMovie > Validate Current Scenario`.
4. Run `Heuristic` if behavior could have changed.
5. Only then continue to the next batch.
