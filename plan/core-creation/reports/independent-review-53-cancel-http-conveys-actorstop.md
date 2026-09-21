# Independent review — [53 — Cancel HTTP conveys ActorStop](plan/core-creation/issues/53-cancel-http-conveys-actorstop.md)

Reviewer did not write the implementation. Axis files [Standards review: 53 — Cancel HTTP conveys ActorStop](code-review-standards-53-cancel-http-conveys-actorstop.md) and [Spec review: 53 — Cancel HTTP conveys ActorStop](code-review-spec-53-cancel-http-conveys-actorstop.md) are parallel opinions. This report is not approval. Ticket Status stays `coded`. Do not land onto staging or ready.

**Range:** `origin/staging...HEAD` at `0a9d294bc5a020e6352d8c1198554fcee8e0e696`. Base `origin/staging` `48c7a99ccbaf9b6bc6f78744ae817d27dc5c1838` (also merge-base). Command: `git diff origin/staging...HEAD`. Diff is non-empty (18 files, +361 / −56).

**Spec:** [53 — Cancel HTTP conveys ActorStop](plan/core-creation/issues/53-cancel-http-conveys-actorstop.md). Locked intent: Cancel HTTP success returns ActorStop Events; Client applies them immediately (like `CommandDone` for ActorStart) so `actorLiveFocusIds` / chrome clear without waiting solely on Poll. Server still cancels CTS.

**Commits** (`origin/staging..HEAD`):

- `0a9d294b` Mark 53 coded after Cancel Event proofs.
- `f67a5a53` Return ActorStop Events on Cancel HTTP success.
- `38d32734` File 53: Cancel HTTP should convey ActorStop Events.
- `8349a803` llm connection
- `a00dabb6` changed files plan
- `6e6a054f` Merge branch 'staging' into dev
- `6d8bb09d` Merge branch 'staging' into dev
- `53f1ffee` Merge branch 'staging' into dev
- `5c98c9ba` misc

## Standards

Standards axis only. Range as pinned. Scan command: `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging` (`python` is not on PATH).

### Mechanical scan

```
src/Client/App.fs  .agents/rules/fsharp-source.md  FILE 846->854  already over 400 or new file over 400; change increased it
src/Server/RouteRegistration.fs  .agents/rules/fsharp-source.md  FILE 417->418  already over 400 or new file over 400; change increased it
--- measure-fs-size ---
src/Client/UpdateCodec.fs::encodeCancelRequest: lines 22-27 (6 lines)
src/Server/Api.fs::latestEventId: lines 201-205 (5 lines)
src/Server/Api.fs::universalFromHandle: lines 206-220 (15 lines)
src/Shared/EventJson.fs::encodeCancelRequest: lines 73-77 (5 lines)
src/Shared/EventJson.fs::decodeCancelRequest: lines 78-83 (6 lines)
```

### Hard violations

- [App.fs](src/Client/App.fs) FILE 846→854. The file is already over 800. This change increased it. [fsharp-source](.agents/rules/fsharp-source.md) file size (800 / split under 400). Scan hit.
- [RouteRegistration.fs](src/Server/RouteRegistration.fs) FILE 417→418. Already over 400. This change increased it. Same rule. Scan hit.
- [appsettings.Development.json](src/Server/appsettings.Development.json): `AiKeys` `ApiKey` is non-empty on tip and empty on `origin/staging`. [environment](.agents/rules/environment.md) secrets / local config. Unrelated to the ticket. Do not land this value.

Measure-fs-size bindings (`encodeCancelRequest`, `latestEventId`, `universalFromHandle`, `decodeCancelRequest`) are 5–15 lines. Under the 40-line cap. Not fails. Surgical under-100-line preference is not a script fail ([core-agent-behavior](.agents/rules/core-agent-behavior.md)). Tests are exempt from 800/400 ([fsharp-source](.agents/rules/fsharp-source.md)).

### Documented standards (judgement)

- [UpdateCodec.fs](src/Client/UpdateCodec.fs) `encodeCancelRequest` now takes `focusId` and `eventId`. Shared already takes `CancelRequest` in [EventJson.fs](src/Shared/EventJson.fs) / [ApiResponses.fs](src/Shared/ApiResponses.fs). [fsharp-source](.agents/rules/fsharp-source.md): group related parameters into a reused type; do not lengthen the argument list.
- New links in [53 — Cancel HTTP conveys ActorStop](plan/core-creation/issues/53-cancel-http-conveys-actorstop.md), [map.md](plan/core-creation/map.md), and [arch.md](plan/core-creation/arch.md) use `../` or `issues/` paths. [markdown-writing](.agents/rules/markdown-writing.md): other local files use a path relative to the project root.

Ticket-shaped F# is surgical: `CancelRequest`, `universalFromHandle` shared by Command and Cancel, Browser `CommandDone`, mailbox skip of `actorStop` when Focus is gone. [core-api](.agents/rules/core-api.md): Browser sends an EventId cursor; it does not mint stored serials.

### Baseline smells (judgement)

- **Duplicated Code** — `decodeUniversal` is copied in [ApiPostCancelTests.fs](tests/Server.Tests/ApiPostCancelTests.fs) and [CancelByFocusTests.fs](tests/Server.Tests/CancelByFocusTests.fs).
- **Shotgun Surgery** — Cancel Events touch Browser, Server Adapter, Core mailbox, Shared JSON, and four test files. The wire contract needs those layers. Not a defect.

### Unrelated noise in the three-dot range

- [code-review skill](.agents/skills/code-review/SKILL.md): adds "Markdown lists."
- [changed-files-directional-links.md](plan/core-creation/reports/changed-files-directional-links.md)
- Merge commits `6e6a054f`, `6d8bb09d`, `53f1ffee`
- `8349a803` llm connection (with the ApiKey hunk)
- `a00dabb6` changed files plan; `5c98c9ba` misc

**Verdict (Standards):** Needs changes. Hard: 3. Judgement: 3. Worst issue: non-empty `AiKeys` `ApiKey` in [appsettings.Development.json](src/Server/appsettings.Development.json).

## Spec

Spec: ticket What to build plus locked Cancel HTTP ActorStop conveyance.

### (a) Missing or partial

None against What to build.

Quote: “Cancel HTTP success carries Events the same way Command carries `ActorStart`: at least the Cancelled `ActorStop` for that Focus, encoded as universal `{ nodes; events; latestId }`.” [Api.fs](src/Server/Api.fs) `postCancel` returns that envelope after `cancelByFocus`. [CancelByFocusTests](tests/Server.Tests/CancelByFocusTests.fs) Adapter cancel-while-live finds Cancelled `ActorStop` for the Focus. `nodes` is `[]`; Command still fills request graph nodes.

Quote: “Client applies those Events (`applyCommandEvents` / equivalent) so `actorLiveFocusIds` drops and chrome + Cancelled (or Error) result update without waiting on Poll.” [App.fs](src/Client/App.fs) `runSubmitCancel` dispatches `CommandDone`, same as Command. That path calls `applyCommandEvents`. [ActorLiveTests](tests/Shared.Tests/ActorLiveTests.fs) apply Cancelled `ActorStop` through `applyServerTail`: the Focus leaves `actorLiveFocusIds`, and `lastCmdResult` is Error `"Actor cancelled."`. [RowView.fs](src/Client/RowView.fs) adds `amb-actor-live` only from that set.

Quote: “Proof: Cancel while live → chrome off and Cancelled (or Error) result from the Cancel response Events. Server still cancels CTS / refuses later Actor output.” Adapter test keeps `waitFakeCancelled`. Shared apply covers chrome/result from those Events. Duplicate completion after cancel stays. Proof is split (HTTP Events, Shared apply, CTS), same shape as Command `ActorStart`.

Quote: “After cancel `finish` drops the live row, a later Actor-body `actorStop` does not emit `ActorStop` for `Graph.rootId` or any other Focus that was not live.” / “Proof: cancel then body stop → one Cancelled `ActorStop` for the Focus; no root stop.” [CoreMailboxBackend.fs](src/Server/Core/CoreMailboxBackend.fs) `dispatchActorStop` does not default `getFocusId` to `Graph.rootId`. New test: one Focus stop, zero root stops.

Non-goals hold: no Undo, no XML AI pack / Focus cssClass, no Poll protocol change.

### (b) Scope creep

Cancel JSON now requires `eventId` so the Server can return the tail. Quote: “the same way Command carries `ActorStart`.” That cursor is the Command pattern, not extra product behaviour.

The three-dot range also contains work [53 — Cancel HTTP conveys ActorStop](plan/core-creation/issues/53-cancel-http-conveys-actorstop.md) did not ask for:

- [.agents/skills/code-review/SKILL.md](.agents/skills/code-review/SKILL.md) wording
- [src/Server/appsettings.Development.json](src/Server/appsettings.Development.json) `AiKeys` `ApiKey` fill (value not quoted)
- [plan/core-creation/reports/changed-files-directional-links.md](plan/core-creation/reports/changed-files-directional-links.md)
- merge commits `6e6a054f`, `6d8bb09d`, `53f1ffee`
- `8349a803` llm connection

### (c) Implemented but wrong

None. `postCancel` still calls `cancelByFocus` (CTS `finish`) before it reads Events. Client apply is the Command `CommandDone` path.

**Verdict (Spec):** Approve with nits. a=0 missing, b=5 extra/noise, c=0 wrong. Worst within Spec: none. Worst in range: Development `ApiKey` fill (unrelated).

## Summary

Standards: 3 hard, 3 judgement; worst is the non-empty Development `AiKeys` `ApiKey`. Spec: 0 missing, 5 extra/noise, 0 wrong; worst in-axis is none.
