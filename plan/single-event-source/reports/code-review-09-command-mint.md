# Code review — 09 Command mint

Independent review. Not approval. Ticket [09 — Command mint](plan/single-event-source/issues/09-command-mint.md) stays **Status:** `coded`.

Range: `origin/cursor/08-core-doors-eaa1...origin/cursor/09-command-mint-efa8` (three-dot). Tip `c704acd7`. Base `03605b25`. Non-empty. 27 files. One commit: `c704acd7 Mint Ev at Browser and Parse command sites.` [History.fs](src/Shared/History.fs) is not in the range (no incidental edit; no [11 — One serial event id](plan/single-event-source/issues/11-one-serial-event-id.md) EventId type work).

Mechanical scan (`python3 .agents/skills/code-review-fsharp/scripts/measure-fs-size.py --diff 'origin/cursor/08-core-doors-eaa1...origin/cursor/09-command-mint-efa8'`): new lets [mint](src/Server/GraphOnlyChangePost.fs) 7 lines; [mintChange](src/Shared/ClientHistory.fs) 7 lines; [yieldMinted](src/Shared/ClientHistory.fs) 11 lines. None over 40. No added LONG lines. Pre-existing [splitNode](src/Client/UpdateHelpers.fs) is 71 lines (this hunk shortened it). Pre-existing LONG in [submitCssClassPromptOp](src/Client/UpdateOps.fs) line 587 was not added.

Alan locks applied: Ev locals `event`/`events`; labeled-link path form waived (flag only `[[path|label]]`); History.fs is ticket 11; Status `defined`/`coded` not `blocked`; no GitHub PR for this report.

## Standards

Range `origin/cursor/08-core-doors-eaa1...origin/cursor/09-command-mint-efa8` (`c704acd7`). Mechanical scan clean: no added LONG/TAB/`mutable`/BARE_ID/blank-blank; no file grown over 400. New lets are 1–11 lines. [History.fs](src/Shared/History.fs) is not in the range. No `[[path|label]]`. Ticket/project Status is `coded`, not `blocked`. New Ev locals use `event`/`posted`/`inverse`/`produced`/`stored`, not `change`/`changes`.

**Hard documented-standard findings:** none.

**Judgement calls**

**Duplicated Code** — same Ev mint, two modules. [ClientHistory.fs](src/Shared/ClientHistory.fs) `mintChange` and [GraphOnlyChangePost.fs](src/Server/GraphOnlyChangePost.fs) `mint` differ only by `Authority`. Ticket names both sites, so a shared helper would fight surgical / no-speculative rules in [core-agent-behavior.md](.agents/rules/core-agent-behavior.md).

```
{ id = EventId.zero
  submissionId = ...
  authority = Authority "Browser" | "Parse"
  commandName = commandName
  body = EventBody.Change ops }
```

**Duplicated Code** — mint then apply, three Client sites. [UpdateHelpers.fs](src/Client/UpdateHelpers.fs) `applyAndPost` and [UpdateWorkspaceSync.fs](src/Client/UpdateWorkspaceSync.fs) `applyAndPostSync` / `applyStructureLocally`:

```
let event = ClientHistory.mintChange commandName ops
match SyncLogic.applyLocalChange event (clientSyncState model) with
```

**Mysterious Name** — new `mintChange` returns `Ev`, not leftover Change ([fsharp-source.md](.agents/rules/fsharp-source.md) Change→Ev rename is for locals; this is the function name). `applyLocalChange` now takes `event: Ev` and keeps the old name; that rename was not required.

**One-word public** — `GraphOnlyChangePost.mint` is one word. Module is `[<RequireQualifiedAccess>]`, which is the “needs context” exception in [fsharp-source.md](.agents/rules/fsharp-source.md). Not treated as hard.

Pre-existing `splitNode` (71) and `submitCssClassPromptOp` LONG 587: 09 shortened / did not add the long line. Not 09 breaches.

## Spec

Range `origin/cursor/08-core-doors-eaa1...origin/cursor/09-command-mint-efa8` (`c704acd7`). No [History.fs](src/Shared/History.fs) edits.

**(a) Missing / partial** — none.

Browser builders go through `ClientHistory.mintChange` (`EventId.zero`, `commandName`, `EventBody.Change`) via `applyAndPost` / `applyAndPostSync`. Parse uses `GraphOnlyChangePost.mint` / `postChunks` onto `postGraphOnly` with `"Parse"`. Exec uses `displayName Exec` (`"Run"`) as a Change Event. Leftover `Change` / `Ev.ofChange` / `Ev.asChange` remain (ticket 12). [ImportText.fs](src/Shared/ImportText.fs) still returns leftover `Change`; that is Shared leftover, not a Browser/Parse builder.

**(b) Scope creep** — none that is extra product.

`ClientHistory.record` / `undo` / `redo` now take or yield `Ev` (arch ClientHistory checkbox). `SyncLogic.applyLocalChange` takes `Ev` so builders reach `postEvents`. `Api.fs` compile-order move is so Parse can call `mint`. Plan/arch `coded` / 1.2.4 checkboxes match Alan’s lock. Locals `Change`→`Ev` are a rename, not new scope. Boot / EventId-private / contract deletes are untouched.

**(c) Implemented but wrong** — none.

Mint sites set `id = EventId.zero` and keep `commandName`. Undo/redo still `EventBody.Undo`/`Redo` from `invertAs`; posted copies are re-zeroed. `postChunks` no longer threads `accepted.revision`; every chunk is `EventId.zero`, which is what the spec asked.

Spec lines covered: “Browser command builders and Parse mint Ev (`EventId.zero`, `commandName`)”; “Run is ActorStart or a Change Event with that Run command in `commandName`”; “Leftover Change still compiles until contract.”

## Summary

Standards: 0 hard findings, 3 judgement smells (Duplicated Code ×2, Mysterious Name); worst is Mysterious Name — `mintChange` returns Ev and `applyLocalChange` still says Change.

Spec: 0 findings; [History.fs](src/Shared/History.fs) untouched; worst none.
