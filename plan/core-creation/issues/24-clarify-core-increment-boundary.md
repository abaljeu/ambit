# 24 — Clarify Core increment boundary

**Status:** done
**Blocked by:** None — can start immediately.
**Estimate:** 45m
**Actual:** 40m

## Context

[[plan/core-creation/reports/core-api-boundary-review.md]] found a **partial** Core API seam. The next worker on this Project still lacks one agent-facing boundary: what belongs in Core vs Adapter vs Client, that this increment does not add Browser lock UI, and that callers must use typed Core functions rather than primitives. [[23-close-core-object-seam.md|23 (Close Core object seam)]] corrects the code. This ticket adds the instruction so that work does not rediscover the gaps or start [[21-client-shows-lock-present.md|21 (Client shows lock-present)]].

Do not implement 23 here. Do not rewrite application source.

## What to build

Write clarifying instruction in the most specific homes. Placement follows [[.cursor/skills/prepare-agent-instruction-change/SKILL.md]]: one canonical text, links elsewhere, no copies in bridges.

- [x] Canonical text lives in [[plan/core-creation/project.md]] as a short **Agent instruction** section (after Map / Committed Decisions). It states: Core vs Adapter vs Client; this increment does not add Browser lock UI (21 belongs with [[plan/event-sourced-ops/project.md]]); callers hold a Core object and use typed Core API functions (`Credential`, `Revision`, `Change`, Command types) and do not unpack `CoreRuntime` or pass cookie strings / `dataDir` into Core; Files stay [[07-define-core-files-contract.md|07]]; general Query stays [[08-define-core-query-contract.md|08]]; Parse algorithms stay out and call typed Graph-only Post.
- [x] [[plan/core-creation/map.md]] Notes gains one sentence that points at that project section. Do not copy the review table into Decisions so far. Out of scope already names Browser UI; keep that.
- [x] If Server Core / Adapter editors will miss `project.md`, add a scoped rule `.cursor/rules/core-api.mdc` with globs limited to Core and Adapter sources (for example `src/Server/Core/**/*.fs`, [[src/Server/Api.fs]], [[src/Server/RouteRegistration.fs]], [[src/Server/GraphOnlyChangePost.fs]]). The rule body is pointers only: this project's Agent instruction, [[doc/Decisions/0003-core-is-a-container-of-subobjects.md]], [[CONTEXT.md]] Core API. No duplicated table. Update [[.cursor/rules/gambol.mdc]] only if that rule file is added.
- [x] Do not edit `AGENTS.md`, `.cursor/copilot-instructions.md`, or `.cursor/codex-context.md`. Do not rewrite [[CONTEXT.md]] (Core API is already defined). Do not start 21. Do not treat 07 or 08 as closed.

Success: the next worker on this Project can tell Core vs Adapter vs Client, will not put lock UI in this increment, and will not call Core with primitives.

## See also

[[plan/core-creation/reports/core-api-boundary-review.md]], [[23-close-core-object-seam.md|23 (Close Core object seam)]], [[21-client-shows-lock-present.md|21 (Client shows lock-present)]], [[.cursor/skills/prepare-agent-instruction-change/SKILL.md]]

## Answer

Canonical Agent instruction is in [[plan/core-creation/project.md]]; map Notes and [[.cursor/rules/core-api.mdc]] only point there.

## Time

- 2026-09-06 40m — Agent instruction in project.md, map Notes pointer, scoped core-api rule (from chat)

## Comments

- 2026-09-06 — Filed from the Core API boundary review (partial). Instruction only; code seam is 23.
- 2026-09-06 — Locked plan [[plan/core-creation/mitigations.md]]: this ticket first, then [[23-close-core-object-seam.md|23 (Close Core object seam)]]. Sentence-level claims and file list are in that plan.
