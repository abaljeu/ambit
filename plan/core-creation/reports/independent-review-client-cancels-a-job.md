# Independent review — Client cancels a job

Reviewer did not write the implementation. Ticket [22 — Client cancels a job](plan/core-creation/issues/22-client-cancels-a-job.md) Status stays `coded`. This report is not approval.

**Range:** `origin/staging...HEAD` at `de0345fe2bf37f2d9ffff5aa52e5551104b06715`. Base `origin/staging` `c4cbfd4600751b2ecdd56a85192ec4465c7b3bbb`. Command: `git diff origin/staging...HEAD`. Diff is non-empty (21 files, +313 / −18).

**Spec:** [22 — Client cancels a job](plan/core-creation/issues/22-client-cancels-a-job.md) — cancel control on live [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) chrome, send cancel by Focus NodeId, after-cancel chrome/result via 21 Poll conveyance. Context only: [llm-connector architecture](plan/llm-connector/arch.md) Story path **Cancel by Focus**, Module **Browser Run / Cancel**, Seam **Browser cancel ↔ Core Cancelled**. Do not demand a reimplementation of 21. Cancel POST need not return Events when Poll is the intentional path.

**Commits** (`origin/staging..HEAD`):

- `de0345fe` Add Browser cancel by Focus on live Actor chrome.

Axis reports: [Standards](code-review-standards-22-client-cancels-a-job.md), [Spec](code-review-spec-22-client-cancels-a-job.md).

## Standards

Standards axis only. Range as pinned. Scan command: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` (`python` is not on PATH).

### Mechanical scan

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

### Hard violations

**File size** — [.agents/rules/fsharp-source.md](.agents/rules/fsharp-source.md) (800 lines or less per file; if a change increases an already-over-limit file, split later in a standalone commit). Scan hits:

- [src/Client/App.fs](src/Client/App.fs) 824→846 — already over 400 and over 800; `runSubmitCancel` made it larger.
- [src/Client/RowView.fs](src/Client/RowView.fs) 417→440 — already over 400; cancel button and `wireCancelControl` made it larger.
- [src/Server/RouteRegistration.fs](src/Server/RouteRegistration.fs) 392→415 — now over 400; `/ambit/cancel` MapPost made it larger.

**Refer by name** — [.agents/rules/refer-by-name.md](.agents/rules/refer-by-name.md) (never refer by only the id). [plan/core-creation/project.md](plan/core-creation/project.md) new note: `Frontier is review of 22.` [plan/core-creation/issues/22-client-cancels-a-job.md](plan/core-creation/issues/22-client-cancels-a-job.md) coded comment: `reuse 21 Poll conveyance`.

No added line over 100 characters. No binding over 40 lines. Surgical under-100-line preference is not a script fail ([.agents/rules/core-agent-behavior.md](.agents/rules/core-agent-behavior.md)). Tests are exempt from file size. [postCancel](src/Server/Api.fs) injects `cancelByFocus` and does not mint EventId ([.agents/rules/core-api.md](.agents/rules/core-api.md)).

### Baseline smells (judgement)

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

## Spec

Spec: ticket Sequence + What to build. Context only: [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) Poll conveyance; [plan/llm-connector/arch.md](plan/llm-connector/arch.md) Story path Cancel by Focus. This axis does not score Standards. Ticket Status stays `coded`.

### 1. Missing or partial

None. Live-row Cancel is shown only with [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md) `amb-actor-live` chrome. Browser POST sends Focus NodeId through the existing `CoreMailbox.cancelByFocus` door. Cancel POST acknowledges without Events; chrome off and Cancelled (or Error) result stay on 21 Poll. Non-goals hold: no Undo, no CoreMailbox cancel change, no DLL catalog, no `applyEvent` projection work.

### 2. Scope creep

1. **Palette and key Cancel** — Spec: “While a Focus is live (chrome from [21 — Client shows live Actor](plan/core-creation/issues/21-client-shows-lock-present.md)), the Browser offers cancel for that Focus.” [RowView.fs](src/Client/RowView.fs) Cancel on the live row meets that. The diff also adds `CommandId.Cancel`, palette filter `cancelAvailable`, and `Ctrl+Shift+Enter` in [CommandEntry.fs](src/Shared/CommandEntry.fs) and [Commands.fs](src/Client/Commands.fs). The spec does not name a palette item or a key.

Ticket Status, Time, [project.md](plan/core-creation/project.md) Notes, and arch checkbox edits are not product behaviour. Unchecked arch seam **Browser cancel ↔ Core Cancelled** is not a must-fix when What to build is met.

### 3. Looks implemented but wrong

None. Row click and palette/key both go through `ActorLive.cancelEffect` and POST `{ focusId }`. Success callback is empty, so `ActorStop` Cancelled, chrome off, and the result wait for 21 Poll. HTTP Adapter `/ambit/cancel` only exposes the existing Core door; it does not add Core cancel semantics.

## Summary

Standards: 5 hard (file size ×3 files, refer-by-name ×2 notes); 4 judgement smells; worst: [App.fs](src/Client/App.fs) 824→846 while already over 800. Spec: 0 missing, 1 scope creep, 0 wrong; worst: palette and `Ctrl+Shift+Enter` Cancel are extra surfaces beyond live-chrome cancel control. No must-fix.

**Verdict:** Good
