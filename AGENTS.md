# Codex Workspace Instructions

Use `.agents` only for repository-specific work in `c:\rl-movie`.

## Load Policy
- For casual chat, general advice, translation, or repo-independent light questions, do not load additional `.agents` files.
- For repo inspection, code search, planning, or file changes, read `c:\rl-movie\.agents\core\SKILL.md` first.
- Then load only the matching skill under `c:\rl-movie\.agents\skills\`.
- `c:\rl-movie\.agents\RULES.md` is a legacy shim, not the canonical rule source.
- `c:\rl-movie\docs` is human-facing optional reference; do not auto-load it.

## Local Skills
- `agents-maintenance`: edit `AGENTS.md`, `.agents/`, skill layout, or instruction architecture.
- `scenario-ideation`: invent video-worthy RL scenario themes and shortlist ideas that stay feasible to implement in this repo. Pair with `unity-free-asset-research` when free Asset Store visuals should influence the idea or be captured in the final hand-off memo.
- `scenario-spec`: define or revise scenario contracts before implementation.
- `scenario-build`: create or change scene/C#/YAML/manifest under `_RLMovie`.
- `scenario-train`: prepare builds, Colab training, comparisons, model import, or train handoff.
- `scenario-evaluate`: compare RL runs, judge adoption quality, and decide whether a model is ready to import, keep as baseline, or reject.
- `scenario-record`: verify `Inference Only`, camera/UI policy, or recording flow.
- `scenario-fix`: debug defects, isolate causes, plan minimal fixes, or decide retraining.
- `rl-instrumentation`: add lightweight reward, termination, observation, and reset diagnostics for ML-Agents debugging.
- `unity-mcp-safe-ops`: perform Unity scene or asset edits through MCP in small validated batches that preserve RL setup integrity.
- `curriculum-randomization`: design or revise curriculum stages and domain randomization for learnability and generalization.
- `run-ingest-archive`: organize returned training artifacts, adoption decisions, and run metadata by `run_id`.
- `unity-free-asset-research`: research the best currently-free Unity Asset Store assets for a specific implementation idea, ranking by fit, reviews, popularity, compatibility, and update health. Pair with `scenario-ideation` when asset choices should shape concept selection or be written into `docs/ideas/`.
- `asset-intake`: evaluate or integrate external assets or `ThirdParty` usage.
