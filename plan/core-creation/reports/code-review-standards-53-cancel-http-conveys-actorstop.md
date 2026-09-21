# Standards review: 53 — Cancel HTTP conveys ActorStop

Range: `git diff origin/staging...HEAD`. Tip `0a9d294b`. Base `origin/staging` `48c7a99c`. Ticket [53 — Cancel HTTP conveys ActorStop](plan/core-creation/issues/53-cancel-http-conveys-actorstop.md). Status stays `coded`.

## Documented standards (hard)

- [App.fs](src/Client/App.fs) FILE 846→854. The file is already over 800. This change increased it. [fsharp-source](.agents/rules/fsharp-source.md) file size (800 / split under 400). Scan hit.
- [RouteRegistration.fs](src/Server/RouteRegistration.fs) FILE 417→418. Already over 400. This change increased it. Same rule. Scan hit.
- [appsettings.Development.json](src/Server/appsettings.Development.json): `AiKeys` `ApiKey` is non-empty on tip and empty on `origin/staging`. [environment](.agents/rules/environment.md) secrets / local config. Unrelated to the ticket. Do not land this value.

Measure-fs-size bindings (`encodeCancelRequest`, `latestEventId`, `universalFromHandle`, `decodeCancelRequest`) are 5–15 lines. Under the 40-line cap. Not fails. Surgical under-100-line preference is not a script fail ([core-agent-behavior](.agents/rules/core-agent-behavior.md)). Tests are exempt from 800/400 ([fsharp-source](.agents/rules/fsharp-source.md)).

## Documented standards (judgement)

- [UpdateCodec.fs](src/Client/UpdateCodec.fs) `encodeCancelRequest` now takes `focusId` and `eventId`. Shared already takes `CancelRequest` in [EventJson.fs](src/Shared/EventJson.fs) / [ApiResponses.fs](src/Shared/ApiResponses.fs). [fsharp-source](.agents/rules/fsharp-source.md): group related parameters into a reused type; do not lengthen the argument list.
- New links in [53 — Cancel HTTP conveys ActorStop](plan/core-creation/issues/53-cancel-http-conveys-actorstop.md), [map.md](plan/core-creation/map.md), and [arch.md](plan/core-creation/arch.md) use `../` or `issues/` paths. [markdown-writing](.agents/rules/markdown-writing.md): other local files use a path relative to the project root.

Ticket-shaped F# is surgical: `CancelRequest`, `universalFromHandle` shared by Command and Cancel, Browser `CommandDone`, mailbox skip of `actorStop` when Focus is gone. [core-api](.agents/rules/core-api.md): Browser sends an EventId cursor; it does not mint stored serials.

## Baseline smells (judgement)

- **Duplicated Code** — `decodeUniversal` is copied in [ApiPostCancelTests.fs](tests/Server.Tests/ApiPostCancelTests.fs) and [CancelByFocusTests.fs](tests/Server.Tests/CancelByFocusTests.fs).
- **Shotgun Surgery** — Cancel Events touch Browser, Server Adapter, Core mailbox, Shared JSON, and four test files. The wire contract needs those layers. Not a defect.

## Unrelated noise in the three-dot range

- [code-review skill](.agents/skills/code-review/SKILL.md): adds "Markdown lists."
- [changed-files-directional-links.md](plan/core-creation/reports/changed-files-directional-links.md)
- Merge commits `6e6a054f`, `6d8bb09d`, `53f1ffee`
- `8349a803` llm connection (with the ApiKey hunk)
- `a00dabb6` changed files plan; `5c98c9ba` misc

**Verdict:** Needs changes. Hard: 3. Judgement: 3. Worst issue: non-empty `AiKeys` `ApiKey` in [appsettings.Development.json](src/Server/appsettings.Development.json).
