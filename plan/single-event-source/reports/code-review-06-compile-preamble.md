# Code review — 06 — Compile preamble

Independent review of the implementer change for [[plan/single-event-source/issues/06-compile-preamble.md|06 — Compile preamble]]. Content commit first line: `Mark 06 compile preamble coded; TestBackend Ev already in scope`. Merge first line: `Merge pull request #15 from abaljeu/cursor/compile-preamble-e0a4`. Files: [[plan/single-event-source/arch.md]], [[plan/single-event-source/issues/06-compile-preamble.md]], [[plan/single-event-source/project.md]]. No `*.fs` / `*.fsi`. F# size measurement skipped. [[src/Shared/History.fs]] is not in the range. Mechanical Standards scan: none (empty stdout, exit 0).

This report is not approval. Ticket **Status:** stays `coded`.

## Standards

Mechanical scan: none. No `*.fs` / `*.fsi`; F# size skipped. [[src/Shared/History.fs]] is not in the range (Alan lock: no hit).

### Hard violations

[[plan/single-event-source/issues/06-compile-preamble.md|06 — Compile preamble]] `**Blocked by:**`

```
**Blocked by:** None — [[05-expand-op-list-apply.md|05 — Expand Op-list apply]] is coded on staging
```

- [[.agents/rules/planning-docs.md]] — Plan text records what is implemented by ticket, section, or Point, not git branch names. Do not discuss `dev`, `ready`, `master`, or other branches as delivery status. `on staging` is branch-as-status.
- [[doc/agents/issue-tracker.md]] Blocking — form is `Blocked by: NN, NN`; a ticket is unblocked when every listed ticket is `done`. [[plan/single-event-source/issues/05-expand-op-list-apply.md|05 — Expand Op-list apply]] is `coded`, not `done`, yet Blocked by is `None` plus narrative.

Same file, `## Time`:

```
- 2026-09-16 1h — Verified TestBackend `Ev` type and `Authority` constructor in scope after 05; no History.fs change (from chat)
```

- [[.agents/rules/refer-by-name.md]] — never refer by only the id; always include the name. `after 05` drops **Expand Op-list apply**.
- [[.agents/rules/markdown-writing.md]] — file paths use `[[wikilinks]]`. Bare `History.fs` (Context already has `[[src/Shared/History.fs]]`).

### Judgement calls

- [[.agents/rules/planning-docs.md]] one concern per section: Context now mixes the original `module Ev` shadow problem with a post-05 outcome. That second paragraph belongs under Comments or Time.
- Time / `**Actual:** 1h` / `(from chat)` match [[doc/agents/issue-tracker.md]]. Project `Actual: 2h30m` matches 05 `1h30m` + 06 `1h`. `**Status:** coded` is a valid [[doc/agents/triage-labels.md]] value. [[plan/single-event-source/arch.md]] `[ ]` → `[x]` is a surgical plan edit. No consecutive blank lines; no hard wrap.

### Baseline smells

None that survive repo override. Repeating `coded` on the issue, the project list, and the arch checkbox is the documented plan pattern, not Shotgun Surgery or Duplicated Code.

## Spec

Range is the content commit plus merge. Files: [[plan/single-event-source/arch.md]], [[plan/single-event-source/issues/06-compile-preamble.md|06 — Compile preamble]], [[plan/single-event-source/project.md]]. No `*.fs`. [[src/Shared/History.fs]] unchanged (locked; not a problem).

Checked against **What to build:** “[[tests/Server.Tests/TestBackend.fs]] compiles. `Ev` the type and `Authority` the constructor are in scope. Leftover Change still compiles.” Checklist: “TestBackend `Ev` type and `Authority` constructor in scope (`module Ev` must not shadow the type).”

**Evidence (not the change):** `open Gambol.Shared`; bare `Authority "Test"`; `: Ev` on `eventFromChange`; leftover `Gambol.Shared.Change` still used. `module Ev` remains a `[<RequireQualifiedAccess>]` companion of `type Ev` and does not hide the type. `dotnet build tests/Server.Tests/Gambol.Server.Tests.fsproj` succeeded, 0 errors.

### (a) Missing or partial

Nothing. The compile-scope outcome is already true; marking **1.2.1** and **Status:** `coded` matches What to build. No [[src/Shared/History.fs]] edit was required for that outcome (and would have been out of scope).

### (b) Scope creep

Nothing. Diff is ticket close-out only: Sequence **1.2.1** `[x]`, ticket status/Actual/Blocked by/Context/Time, project issue line and Actual. No persist-apply or leftover-Change contract work (those stay out of scope).

### (c) Looks implemented but wrong

Nothing. Checking “`module Ev` must not shadow the type” is correct in the compiler sense: the type is in scope; the old Context line “cannot see `Ev` and `Authority` … because `module Ev` shadows the type” is a stale diagnosis, and the added Context states that 05 already made compile succeed. That matches the build.

## Summary

Standards: 5 findings (4 hard, 1 judgement); worst: `Blocked by` names a branch as delivery status and clears the blocker while [[plan/single-event-source/issues/05-expand-op-list-apply.md|05 — Expand Op-list apply]] is `coded`, not `done`. Spec: 0 findings; no worst Spec issue.
