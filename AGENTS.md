# Codex Workspace Instructions

Use `.agents` only for repository-specific work in `c:\rl-movie`.

## Load Policy
- For casual chat, general advice, translation, or repo-independent light questions, do not load additional `.agents` files.
- For repo inspection, code search, planning, or file changes, read `c:\rl-movie\.agents\core\SKILL.md` first.
- Then load only the matching skill under `c:\rl-movie\.agents\skills\`.
- `c:\rl-movie\.agents\RULES.md` is a legacy shim, not the canonical rule source.
- `c:\rl-movie\docs` is human-facing optional reference; do not auto-load it.
- Keep context small: load only the skills, references, and file excerpts needed for the current task.

## Working Style
- Default to compact output: report outcomes, validation, blockers, and required decisions first.
- Skip code walkthroughs and implementation narration unless the user asks for them or they affect risk.
- Ask for user input only when a material decision cannot be resolved safely from repo context.
- For new learning scenes, default to a common-first architecture: reuse and strengthen `_RLMovie/Common` before creating new starter kinds, archetype scaffolds, or one-off scene infrastructure.

## Local Skills
- `agents-maintenance`: edit `AGENTS.md`, `.agents/`, skill layout, or instruction architecture.
- `lessons-maintenance`: capture, merge, promote, and prune reusable repo lessons so knowledge stays useful without sprawling.
- `asset-inventory-planning`: summarize which third-party assets are already imported, what they can do, which combinations are promising, and whether a scenario can be built from current inventory before researching more.
- `rl-video-benchmarking`: benchmark scenario ideas, hooks, titles/thumbnails, and first-10-second plans against successful RL and AI-simulation YouTube patterns before locking direction.
- `rl-video-quality-gate`: review a concept, scene, footage, package, script, or whole episode against the project's watchability bar and winning RL/AI YouTube patterns, then identify the top blockers and next step.
- `rl-video-packaging`: turn recorded or candidate RL footage into a concrete upload package with cold open, beat order, title/thumbnail options, clip picks, and viewer-facing copy.
- `rl-video-scriptwriting`: write beat-aligned narration, TTS-friendly voiceover, and text-card copy once a video's package and footage are mostly locked.
- `scenario-ideation`: invent video-worthy RL scenario themes and shortlist ideas that stay feasible to implement in this repo. Pair with `unity-free-asset-research` when free Asset Store visuals should influence the idea or be captured in the final hand-off memo.
- `scenario-spec`: define or revise scenario contracts before implementation.
- `scenario-build`: create or change scene/C#/YAML/manifest under `_RLMovie`.
- `scenario-train`: prepare builds, Colab training, comparisons, model import, or train handoff.
- `scenario-evaluate`: compare RL runs, judge adoption quality, and decide whether a model is ready to import, keep as baseline, or reject.
- `scenario-record`: verify `Inference Only`, camera/UI policy, or recording flow.
- `scenario-fix`: debug defects, isolate causes, plan minimal fixes, or decide retraining.
- `rl-instrumentation`: add lightweight reward, termination, observation, and reset diagnostics for ML-Agents debugging.
- `unity-scene-visual`: improve scene readability and polish for RL video through composition, lighting, dressing, materials, and safe local URP volume decisions, especially when UnityMCP output looks flat or low-fidelity.
- `unity-rl-camera`: design RL-video camera packages, anchor layouts, and `RecordingHelper` behavior so scenarios read clearly and record well instead of relying on one generic angle.
- `unity-mcp-safe-ops`: perform Unity scene or asset edits through MCP in small validated batches that preserve RL setup integrity.
- `curriculum-randomization`: design or revise curriculum stages and domain randomization for learnability and generalization.
- `run-ingest-archive`: organize returned training artifacts, adoption decisions, and run metadata by `run_id`.
- `unity-free-asset-research`: research the best currently-free Unity Asset Store assets for a specific implementation idea, ranking by fit, reviews, popularity, compatibility, and update health. Pair with `scenario-ideation` when asset choices should shape concept selection or be written into `docs/ideas/`.
- `asset-intake`: evaluate or integrate external assets or `ThirdParty` usage.
