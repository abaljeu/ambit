# 20 — SES smell-cleanup quality criteria

**Type:** research
**Status:** needs-info
Blocked by: none

## 1. Question

After the SES Spec path is okay (two EventId / overlay fixes landed; historical [11 — One serial event id](11-one-serial-event-id.md) and [12 — Contract leftover Change and Revision](12-contract-leftover-change-and-revision.md) remain `coded`), what **code-quality criteria** should govern a later smell-cleanup implement ticket?

Name the criteria so an implementer can accept or reject a change without re-litigating file/function length policy.

## 2. Starting sources

Read these reports first; extract naming and type-usage findings. Do not treat file length or function length as criteria except for functions a follow-on cleanup ticket actually edits.

1. [Code review — 11 One serial event id](../reports/code-review-11-one-serial-event-id.md)
2. [Code review — 11 repair](../reports/code-review-11-repair.md)
3. [Code review — 12 Contract leftover Change and Revision](../reports/code-review-12-contract-leftover-change-and-revision.md)
4. [Code review — 34b Outside Core lifecycle proof](../../core-creation/reports/code-review-34b-outside-core-lifecycle-proof.md) — in scope even though it lives under core-creation; Standards notes that touch SES remaps, EventId usage, and naming apply.

## 3. In scope

1. Naming — leftover Change / Revision names on Ev-typed values; Mysterious Name; locals and APIs that should say `event` / `events` / `EventId` after the SES remap.
2. Improper type usage — `EventId.fromJson` / `toJson` outside serialize or named wire/SQL peel; Primitive Obsession (`int` where `EventId` belongs); drafts that should be `EventId.zero` / `EventId.beforeAll`; authority or Caller stamps that claim the wrong kind then rely on overwrite.

## 4. Out of scope

1. File length — do not use ≤400 lines as a criterion for this research or for a follow-on cleanup ticket’s acceptance, except as ordinary surgical judgment on files that ticket edits.
2. Function length — do not use ≤40 lines as a criterion, except for functions a follow-on cleanup ticket actually touches (those may be brought in line when edited).
3. Spec gaps already closed by the landed EventId-zero / honest ServerRejected fixes — do not reopen Spec for 11/12 here.
4. Redo tickets [13 — Revision always 0 (diagnostic)](13-revision-always-zero.md)–[19 — Delete unused EventId.fs](19-delete-unused-eventid-fs.md) — leave their What to build alone; this research may note overlap but does not rewrite them.

## 5. Deliverable

1. Write the Answer under `## Answer` on this ticket.
2. Put the lasting checklist in [plan/single-event-source/reports/ses-smell-cleanup-quality-criteria.md](../reports/ses-smell-cleanup-quality-criteria.md) (create when research finishes).
3. Criteria must be testable: each item names what to look for and what “fixed” means.
4. When Alan accepts the Answer, set Status `done`. A later implement ticket (not this one) applies the criteria.

## Answer

_(empty until research finishes)_
