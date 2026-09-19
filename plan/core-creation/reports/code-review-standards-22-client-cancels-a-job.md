# Standards review — [22 — Client cancels a job](plan/core-creation/issues/22-client-cancels-a-job.md)

Range: `git diff origin/staging...HEAD` (`c4cbfd46`…`de0345fe`). Commit: `de0345fe` Add Browser cancel by Focus on live Actor chrome. 21 files, +313 / −18. Standards axis only.

Command used: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`

```
src/Client/App.fs  .agents/rules/fsharp-source.md  FILE 824->846  already over 400 or new file over 400; change increased it
src/Client/RowView.fs  .agents/rules/fsharp-source.md  FILE 417->440  already over 400 or new file over 400; change increased it
src/Server/RouteRegistration.fs  .agents/rules/fsharp-source.md  FILE 392->415  already over 400 or new file over 400; change increased it
--- measure-fs-size ---
src/Client/App.fs::runSubmitCancel: lines 400-420 (21 lines)
src/Client/Commands.fs::focusedSelectionId: lines 47-50 (4 lines)
src/Client/Commands.fs::cancelAvailable: lines 51-56 (6 lines)
src/Client/Commands.fs::execCancelOp: lines 57-61 (5 lines)
src/Client/RowView.fs::internal: lines 18-27 (10 lines)
src/Client/UpdateActorLive.fs::cancelFocusOp: lines 29-33 (5 lines)
src/Client/UpdateCodec.fs::encodeCancelRequest: lines 22-26 (5 lines)
src/Server/Api.fs::postCancel: lines 236-249 (14 lines)
src/Shared/ActorLive.fs::cancelControlClass: lines 30-31 (2 lines)
src/Shared/ActorLive.fs::offersCancel: lines 32-34 (3 lines)
src/Shared/ActorLive.fs::cancelEffect: lines 35-38 (4 lines)
src/Shared/EventJson.fs::encodeCancelRequest: lines 68-71 (4 lines)
src/Shared/EventJson.fs::decodeCancelRequest: lines 72-75 (4 lines)
```

## Hard violations

**File size** — [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) (800 lines or less per file; if a change increases an already-over-limit file, split later in a standalone commit). Scan hits:

- [src/Client/App.fs](src/Client/App.fs) 824→846 — already over 400 and over 800; `runSubmitCancel` made it larger.
- [src/Client/RowView.fs](src/Client/RowView.fs) 417→440 — already over 400; cancel button and `wireCancelControl` made it larger.
- [src/Server/RouteRegistration.fs](src/Server/RouteRegistration.fs) 392→415 — now over 400; `/ambit/cancel` MapPost made it larger.

**Refer by name** — [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md) (never refer by only the id). [plan/core-creation/project.md](plan/core-creation/project.md) new note: `Frontier is review of 22.` [plan/core-creation/issues/22-client-cancels-a-job.md](plan/core-creation/issues/22-client-cancels-a-job.md) coded comment: `reuse 21 Poll conveyance`.

No added line over 100 characters. No binding over 40 lines. Surgical under-100-line preference is not a script fail ([.agents/rules/core-agent-behavior.md](.agents/rules/core-agent-behavior.md)). Tests are exempt from file size. [postCancel](src/Server/Api.fs) injects `cancelByFocus` and does not mint EventId ([.agents/rules/core-api.md](.agents/rules/core-api.md)).

## Baseline smells (judgement)

**Duplicated Code** — [src/Server/RouteRegistration.fs](src/Server/RouteRegistration.fs) `/ambit/cancel` copies the `/ambit/command` cookie and admit block:

```
match BrowserRequestCreds.tryCookieCaller req with
| None -> return Results.Unauthorized()
| Some caller ->
    let! live = CoreMailbox.isAdmitted ...
```

This matches the current route style. Not a hard violation.

**Duplicated Code** — [src/Client/Commands.fs](src/Client/Commands.fs) `cancelAvailable` and `execCancelOp` both match `focusedSelectionId`:

```
match focusedSelectionId model with
| None -> ...
| Some focusId -> ...
```

**Middle Man** — [src/Shared/ActorLive.fs](src/Shared/ActorLive.fs) `offersCancel` is `Set.contains focusId live`. [src/Client/UpdateActorLive.fs](src/Client/UpdateActorLive.fs) `cancelFocusOp` only unwraps `cancelEffect`. Shared-first naming allows the Shared helpers.

**Shotgun Surgery** — one Cancel control edits Client, Shared, Server, CSS, and plan files. The module map needs that split.
