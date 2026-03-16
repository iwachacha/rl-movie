---
name: lessons-maintenance
description: Curate reusable project lessons without letting repo memory sprawl. Use when capturing a repeated gotcha, consolidating duplicated guidance, promoting a lesson into a task-specific skill or reference, or pruning stale repo memory.
---

# Lessons Maintenance

## Overview

Keep repo memory small, useful, and evidence-backed.
Treat `../../references/lessons-learned.md` as the compact cross-task memory for RL Movie.
Prefer placing durable guidance in the narrowest canonical location instead of growing one giant global note.

## Workflow

1. Start from evidence, not vibes.
Read `../../references/lessons-learned.md`.
Then read only the artifact that produced the lesson, such as the relevant skill, reference, validator output, review note, run summary, or handoff memo.
Do not write memory updates from an unverified hunch.

2. Classify the lesson before editing.
Choose the smallest correct home:
- `RunArchive/...` or task notes for run-specific evidence, adoption rationale, or one-off outcomes
- the matching skill or task reference for guidance that belongs to one phase, subsystem, or workflow
- `../../references/lessons-learned.md` only for short cross-task lessons that should be remembered broadly
- `../../core/SKILL.md` only for stable repo-wide guardrails that should apply on most tasks

3. Merge before you append.
Search for overlap before adding anything new.
If a lesson mostly duplicates an existing one, strengthen or rewrite the existing entry instead of adding a sibling bullet.
Prefer one sharper lesson over three near-duplicates.

4. Promote and prune deliberately.
Promote a lesson out of `lessons-learned.md` when it has become clearly task-shaped and belongs in a skill or reference.
Delete or shrink a lesson when it duplicates a stronger canonical source or no longer matches repo reality.
When a lesson becomes a rule, remove the redundant global phrasing.

5. Keep the global memory compact.
Treat `lessons-learned.md` as an index, not a diary.
Use a soft cap:
- about 3-6 sections
- about 2-4 bullets per section
If the file grows beyond that, consolidate or promote items instead of extending it.

6. Explain the placement decision.
When you update repo memory, say:
- what evidence triggered the change
- where the knowledge now lives
- whether you merged, promoted, or pruned anything

## Guardrails

- Do not create new memory files when an existing canonical location already fits.
- Do not store long examples, logs, or narrative postmortems in `lessons-learned.md`.
- Do not turn temporary preferences into permanent rules.
- Do not add abstract advice that is not grounded in RL Movie repo behavior.
- Do not let the global lessons file become a second copy of task-specific checklists.

## Response Shape

- `Evidence`: what proved the lesson
- `Placement`: why the knowledge belongs in that file
- `Changes`: what was added, merged, promoted, or removed
- `Residual risk`: only if something still feels provisional
