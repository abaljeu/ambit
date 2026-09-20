# Standards re-review — Browser Run: Focus reply parent, Command is runnable ancestor

Range: `git diff origin/staging...HEAD` (tip `1186d617`, `origin/staging` `b484d99`), including the Amble lock. No prior review reports. [51 — Browser Run: Focus reply parent, Command is runnable ancestor](plan/core-creation/issues/51-browser-run-focus-vs-command.md) Status stays `coded`.

## Mechanical scan (documented hits)

Treat each printed scan line as a hit. Cite the rule on that line.

- [code-review-spec-51-focus-vs-command](plan/core-creation/reports/code-review-spec-51-focus-vs-command.md) lines 3, 5, 19: [refer-by-name](.agents/rules/refer-by-name.md) BARE_ID (`#81`, `item 3`, `item 8`, `item 4`).
- [code-review-standards-51-focus-vs-command](plan/core-creation/reports/code-review-standards-51-focus-vs-command.md) lines 8–10, 24: [refer-by-name](.agents/rules/refer-by-name.md) BARE_ID (`Ticket 51`).
- [independent-review-51-focus-vs-command](plan/core-creation/reports/independent-review-51-focus-vs-command.md) lines 25–27, 41, 88, 96: [refer-by-name](.agents/rules/refer-by-name.md) BARE_ID (`Ticket 51`, `item 8`, `item 4`).
- [plan-or-doc-change-51-focus-vs-command](plan/core-creation/reports/plan-or-doc-change-51-focus-vs-command.md) lines 30, 44, 48: [refer-by-name](.agents/rules/refer-by-name.md) BARE_ID (`Ticket 51` / `ticket 51`).
- [TestActorHelloTests](tests/Server.Tests/TestActorHelloTests.fs) FILE 481→538: [fsharp-source](.agents/rules/fsharp-source.md) 800/400 split. Tests are exempt. Not a fail.
- measure-fs-size: listed bindings under 40 lines. Pass. Surgical under-100-line preference is not a script fail ([core-agent-behavior](.agents/rules/core-agent-behavior.md)).

## Additional hard (documented)

F# [CommandRequest](src/Shared/CommandRequest.fs) and [Commands](src/Client/Commands.fs): no mutable, no exceptions, lines ≤100. [core-api](.agents/rules/core-api.md) unused. [core-creation](plan/core-creation/project.md) Stage stays `build`. [no-retrofit](.agents/rules/no-retrofit.md) holds.

- [llm-connector project](plan/llm-connector/project.md) Note `[51](plan/core-creation/issues/51-browser-run-focus-vs-command.md)` is number-only. [refer-by-name](.agents/rules/refer-by-name.md): the name must wrap the link.
- [plan-or-doc-change-51](plan/core-creation/reports/plan-or-doc-change-51-focus-vs-command.md) Scope ticket line: same number-only `[51](...)`.
- [core-creation project](plan/core-creation/project.md) Filed note uses Obsidian `[[path|label]]`. [markdown-writing](.agents/rules/markdown-writing.md) requires `[label](path)`.
- Ticket See also: `[llm-connector 06]` and `[llm-connector 08]` omit issue names. [refer-by-name](.agents/rules/refer-by-name.md).

## Smells (judgement only)

**Feature Envy / custom walk.** [fsharp-source](.agents/rules/fsharp-source.md): check [GraphQuery](src/Shared/GraphQuery.fs) `enclosing` first. `enclosing` has no Zoom bound, so not a hard fail.

```
let private ownerPathToZoom
    (graph: Graph)
    (focusId: NodeId)
    (zoomId: NodeId)
    : NodeId list option =
    let rec collect acc current visited =
```

**Duplicated Code.** `isAmbleScanStop` walks the owner path twice:

```
Option.isSome (scanStopOnOwnerPath graph focusId zoomId)
&& Option.isNone (commandOnOwnerPath graph focusId zoomId)
```

**Parameter Explosion.** `tryStart` takes five arguments. Nearby grouping rule; `ActorStart` is the output. Nit.

**Mysterious Name.** Public `tryStart` is one word. [fsharp-source](.agents/rules/fsharp-source.md) wants more than one word, or explicit context. `CommandRequest.tryStart` supplies context. Nit.

## Verdict

Approve with nits
