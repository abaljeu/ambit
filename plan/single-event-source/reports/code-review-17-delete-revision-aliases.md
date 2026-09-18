# Code review — 17 Delete Revision aliases

Range: uncommitted vs `HEAD`. Spec: [17 — Delete Revision aliases](../issues/17-delete-revision-aliases.md). Scan: none.

`type Revision` and `EventId.ofRevision` / `toRevision` are already gone on `HEAD` from the historical [12 — Contract leftover Change and Revision](../issues/12-contract-leftover-change-and-revision.md) land. This peel is Status/`Actual`/comment/Time bookkeeping.

## Standards

Mechanical scan: `scan: none`. **No hard documented-standard hits.**

Plan-only hunks keep one blank line between blocks, no intra-paragraph wraps, and labeled links as `[name](file.md)` with number **and** name ([markdown-writing.md](.agents/rules/markdown-writing.md): no consecutive blanks; unlimited line length; labeled links; same-directory file name. [refer-by-name.md](.agents/rules/refer-by-name.md): never id-only). New [21 — SES smell-cleanup](../issues/21-ses-smell-cleanup.md) link matches the issue title. Status/Actual/Time/Comments edits stay on those tickets and the map list ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md) Surgical Changes).

**Judgement — Mysterious Name.** Time line on [17 — Delete Revision aliases](../issues/17-delete-revision-aliases.md) originally said “no production peel.” That line now reads “no F# type or alias to delete.”

## Spec

**(a) Missing or partial**

None for the product requirement. Spec: “Delete `type Revision` and `EventId.ofRevision` / `toRevision`.” Green bar: “no Revision type left.” This range has no `src/` or `tests/` hunks. HEAD already has none of those symbols (grep). The ticket comment records that as the historical [12 — Contract leftover Change and Revision](../issues/12-contract-leftover-change-and-revision.md) land. Out of scope leftover Change wrapping, unused EventId.fs, and [core-creation arch.md](../../core-creation/arch.md) were not touched.

**(b) Scope creep**

[16 — Approve / merge stamp + beforeAll](../issues/16-approve-merge-stamp-beforeall.md) `**Status:** coded` → `done` and the matching Status line on [project.md](../project.md) are not in 17’s What to build. Ticket 17’s own Status/Actual/Time/comment and project Actual +20m / 17 Status `coded` are tracker fields for this land, not product behaviour.

**(c) Implemented but wrong**

None. Green bar is “no Revision type left,” not leftover Revision *names* (`decodeRevision`, `responseRevision`, test titles). The comment correctly defers those to [21 — SES smell-cleanup](../issues/21-ses-smell-cleanup.md). Status `coded` is implement-complete, not review-approved `done`.

## Summary

Standards: 0 hard; 1 judgement smell (Time-line wording, already rephrased). Spec: 0 missing product work; worst extra is [16 — Approve / merge stamp + beforeAll](../issues/16-approve-merge-stamp-beforeall.md) Status `done` in the same range.
