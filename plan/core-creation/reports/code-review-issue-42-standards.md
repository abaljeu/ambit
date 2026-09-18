# Standards review — [42 — Migrate PersistHandlers restore and getEventsSince](../issues/42-migrate-persisthandlers-restore-and-geteventssince.md) (uncommitted vs HEAD)

## Mechanical scan

```
warning: in the working copy of 'src/Server/Database.fs', LF will be replaced by CRLF the next time Git touches it
plan/core-creation/reports/code-review-issue-42-spec.md:1  .agents/rules/refer-by-name.md  BARE_ID  issue 42
plan/core-creation/reports/code-review-issue-42-spec.md:7  .agents/rules/refer-by-name.md  BARE_ID  item 11
plan/core-creation/reports/code-review-issue-42-spec.md:13  .agents/rules/refer-by-name.md  BARE_ID  item 10
plan/core-creation/reports/code-review-issue-42-spec.md:15  .agents/rules/refer-by-name.md  BARE_ID  issue 42
plan/core-creation/reports/code-review-issue-42-spec.md:15  .agents/rules/refer-by-name.md  BARE_ID  items 2-4
plan/core-creation/reports/code-review-issue-42-spec.md:19  .agents/rules/refer-by-name.md  BARE_ID  item 6
plan/core-creation/reports/implement-issue-42.md:7  .agents/rules/refer-by-name.md  BARE_ID  ticket 43
plan/core-creation/reports/issue-42-persist-seam-map.md:28  .agents/rules/refer-by-name.md  BARE_ID  ticket 43
plan/core-creation/reports/issue-42-persist-seam-map.md:41  .agents/rules/refer-by-name.md  BARE_ID  Issue 40
plan/core-creation/reports/issue-42-persist-seam-map.md:52  .agents/rules/refer-by-name.md  BARE_ID  ticket 41
plan/core-creation/reports/issue-42-persist-seam-map.md:69  .agents/rules/refer-by-name.md  BARE_ID  issue 40
plan/core-creation/reports/issue-42-persist-seam-map.md:77  .agents/rules/refer-by-name.md  BARE_ID  ticket 41
plan/core-creation/reports/issue-42-persist-seam-map.md:87  .agents/rules/refer-by-name.md  BARE_ID  Ticket 42
plan/core-creation/reports/issue-42-persist-seam-map.md:124  .agents/rules/refer-by-name.md  BARE_ID  ticket 42
plan/core-creation/reports/issue-42-persist-seam-map.md:126  .agents/rules/refer-by-name.md  BARE_ID  ticket 43
plan/core-creation/reports/issue-42-persist-seam-map.md:146  .agents/rules/refer-by-name.md  BARE_ID  ticket 43
plan/core-creation/reports/issue-42-persist-seam-map.md:162  .agents/rules/refer-by-name.md  BARE_ID  ticket 43
plan/core-creation/reports/issue-42-persist-seam-map.md:163  .agents/rules/refer-by-name.md  BARE_ID  ticket 44
plan/core-creation/reports/issue-42-persist-seam-map.md:164  .agents/rules/refer-by-name.md  BARE_ID  ticket 45
plan/core-creation/reports/issue-42-persist-seam-map.md:168  .agents/rules/refer-by-name.md  BARE_ID  Issue 40
plan/core-creation/reports/issue-42-persist-seam-map.md:169  .agents/rules/refer-by-name.md  BARE_ID  ticket 42
src/Server/Core/DbAgent.fs  .agents/rules/fsharp-source.md  FILE 529->564  already over 400 or new file over 400; change increased it
src/Server/Database.fs  .agents/rules/fsharp-source.md  FILE 476->529  already over 400 or new file over 400; change increased it
--- measure-fs-size ---
src/Server/Core/CoreEventDispatch.fs::actorStart: lines 37-44 (8 lines)
src/Server/Core/CoreEventDispatch.fs::actorStop: lines 45-60 (16 lines)
src/Server/Core/CoreMailbox.fs::getEventsSince: lines 65-74 (10 lines)
src/Server/Core/CoreMailboxBackend.fs::eventDispatchContext: lines 134-138 (5 lines)
src/Server/Core/CoreMailboxBackend.fs::seedEventLog: lines 276-296 (21 lines)
src/Server/Core/DbAgent.fs::eventsSince: lines 357-369 (13 lines)
src/Server/Core/DbAgent.fs::appendPersistedEvent: lines 370-389 (20 lines)
src/Server/Database.fs::appendEvent: lines 257-277 (21 lines)
src/Server/Database.fs::getEventsAfter: lines 278-295 (18 lines)
```

## Standards

### Hard violations

1. **File length** ([[.agents/rules/fsharp-source.md]] — 400 lines; do not increase files already over limit): scan lines on `DbAgent.fs` (529→564) and `Database.fs` (476→529). New Event persist/read helpers grow two already-over-limit modules instead of a new cohesive module.

2. **Refer by name** ([[.agents/rules/refer-by-name.md]]): mechanical scan BARE_ID hits on [[issue-42-persist-seam-map.md]], [[implement-issue-42.md]], and [[code-review-issue-42-spec.md]] — bare “ticket/issue 40–45”, “item N”, “Migrate item N” without linked titles. Issue ticket [[42-migrate-persisthandlers-restore-and-geteventssince.md]] and modified [[arch.md]] / [[project.md]] use named wikilinks correctly.

3. **Ticket structure** ([[.agents/rules/markdown-writing.md]] — one blank line between blocks): [[42-migrate-persisthandlers-restore-and-geteventssince.md]] has two `## Time` sections (lines 47–55) and a stray `Actual: 1h30m` in the header block; duplicate headings break the usual one-section shape.

### Judgement calls (SMELLS.md)

- **Shotgun Surgery** — one persist seam edits CoreEventDispatch, CoreMailboxBackend, FileAgent, DbAgent, Database, EventLogFile, fsproj, and tests; expected for the migration but scattered.
- **Duplicated Code** — `DbAgent.eventsSince` / `appendPersistedEvent` mirror the existing `changesSince` / postChange persist shape; reasonable symmetry, possible extract later.
- **Middle Man** — `CoreMailbox.getEventsSince` only forwards through `GetEventsSince`; matches existing door pattern ([[.agents/rules/core-api.md]]), not a refactor target here.

New functions in the scan are under 40 lines and 100 chars; `EventLogFile.fs` (59 lines) is within file limit. Embedded SQL in `Database.fs` continues that module’s established pattern ([[.agents/rules/fsharp-source.md]] tiny-SQL rule — judgement: acceptable here). Plan edits to [[41-migrate-core-mailbox-coremsg-and-pool-onto-event.md]] status and broad [[arch.md]] checkboxes extend beyond issue 42 lines touched ([[.agents/rules/core-agent-behavior.md]] surgical scope — judgement: plan hygiene vs minimal diff).

## Summary

8 hard findings (2 file-length, 19 refer-by-name scan lines, 1 ticket markdown shape); worst: growing `DbAgent.fs` / `Database.fs` past the 400-line rule while already over limit.

## Follow-up (2026-09-15)

Plan markdown: merged duplicate `## Time` on the ticket; refer-by-name fixes on [[implement-issue-42.md]], [[code-review-issue-42-spec.md]], [[issue-42-persist-seam-map.md]]. **Open:** `DbAgent.fs` / `Database.fs` file-length rule still violated by the F# diff.
