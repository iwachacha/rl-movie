# Recording Helper Patterns

Use this file when changing `RecordingHelper`.
The repo already supports more than static anchor cycling, so configure it intentionally.

## Baseline Package In This Repo

The V2 scenario starter creates:
- `DefaultView`
- `RecordWideLeft`
- `RecordWideRight`
- `RecordFollowRear`

And wires:
- `RecordingHelper.cameraPositions` to the three recording views
- `followTarget` to the primary agent
- `followCameraIndex` to the rear follow cut
- `hideUIWhenRecording` to `true`

Treat that as the default package to refine, not random starter noise.

## Settings That Usually Matter

- `cameraPositions`
  - order them like an editorial sequence, not just a list of transforms

- `cameraSwitchInterval`
  - start around `6-10` seconds
  - shorten only if the cuts are very distinct and still easy to reorient around

- `followCameraIndex`
  - reserve for the one cut that truly benefits from dynamic tracking

- `followTarget`
  - usually the primary agent
  - change only when another object is the true protagonist of the shot

- `followCameraProfiles`
  - use when multiple follow-style cuts need different offsets or sharpness

- `enableOccluderGhosting`
  - useful indoors or in prop-dense scenes where the agent gets blocked

- `enableTemporaryBlackoutCuts`
  - useful when reset transitions should feel cleaner on video

- `hideUIWhenRecording`
  - usually keep `true`
  - only show UI when the UI itself explains the story

## Validation Notes From Repo Behavior

- If camera switching is on and there are fewer than 2 `cameraPositions`, validator warns.
- If `followCameraIndex` points to a cut but `followTarget` is missing, validator warns.
- Camera anchors should be registered to `ScenarioGoldenSpine` using `CameraRoleBinding` (e.g. `default_camera`, `explain`, `wide_a`).
- Camera anchors should live under the scenario environment root.

## Anti-Patterns

- switching often because "movement feels dynamic"
- making the follow cut the only interesting angle
- forgetting that the default view is part of the viewer experience too
- using per-camera follow logic when a static wide would explain the scene better
