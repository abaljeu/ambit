# Standards review — [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md) slices 2–3

Range: `git diff origin/staging...HEAD` (commit `24ee25ab`). Baseline: [SMELLS.md](.agents/skills/code-review/SMELLS.md). Not approval. Status unchanged.

## Mechanical scan (documented-standard hits)

Each printed line is a hit with the rule path on that line.

1. [App.fs](src/Client/App.fs) `.agents/rules/fsharp-source.md` FILE 794→818 — already over 400; change increased it.
2. [RouteRegistration.fs](src/Server/RouteRegistration.fs) `.agents/rules/fsharp-source.md` FILE 382→406 — already over 400 or new file over 400; change increased it (crossed 400 from 382).
3. [ApiPostCommandTests.fs](tests/Server.Tests/ApiPostCommandTests.fs):210 `.agents/rules/fsharp-source.md` MUTABLE `let mutable found = false`.
4. [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) `.agents/rules/fsharp-source.md` FILE 548→591 — already over 400; change increased it.

measure-fs-size bindings are ≤40 lines (`postCommand` 33, `runSubmitCommand` 23). No added 100-char line.

## Hard violations

1. **File length** ([fsharp-source.md](.agents/rules/fsharp-source.md) — ≤400; do not grow a file already over): [App.fs](src/Client/App.fs) 794→818 via `runSubmitCommand`. [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) 548→591 via round-trips (new file would stay under, like [CommandRequestTests.fs](tests/Shared.Tests/CommandRequestTests.fs)). [RouteRegistration.fs](src/Server/RouteRegistration.fs) 382→406 by inlining POST `/ambit/command` into `registerStateRoutes` (lines 165–263, 99 lines vs ≤40).
2. **No mutable** ([fsharp-source.md](.agents/rules/fsharp-source.md)): `waitForHello` uses `let mutable found = false` then `found <-` while polling CoreMailbox State for an Owned `hello` child under Focus.
3. **Event/Ev naming** ([fsharp-source.md](.agents/rules/fsharp-source.md)): [Api.fs](src/Server/Api.fs) `List.map (fun e -> e.id)` binds Ev as `e`, not `event`/`ev`.
4. **Same-directory labeled link** ([markdown-writing.md](.agents/rules/markdown-writing.md)): [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md) Blocked-by uses a root path instead of `(34b-outside-core-lifecycle-proof.md)`. It did drop `[[path|label]]`.

[core-api.md](.agents/rules/core-api.md): production EventId uses encode/decode only. No `EventId.next`. [oneNodeStart](src/Shared/CommandRequest.fs) copies the Client EventId cursor; fixtures use `EventId.zero`. No unused bindings ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md)). Plan text names sections ([refer-by-name.md](.agents/rules/refer-by-name.md)). `nodesForRequest` walks `graphIds`, not full-graph `Map.toList`.

## Judgement calls ([SMELLS.md](.agents/skills/code-review/SMELLS.md))

- **Duplicated Code** — `/ambit/command` repeats cookie + admit instead of `withBrowserChanges`:

```
match BrowserRequestCreds.tryCookieCaller req with
| None -> return Results.Unauthorized()
| Some caller ->
    let! live =
        CoreMailbox.isAdmitted persistence.Core.host caller
        |> Async.StartAsTask
```

`withBrowserChanges` already does that walk; the new route also needs `startActor` with the same Caller.

- **Shotgun Surgery** — ActorStart HTTP plus Browser Run edits Shared, Server, Client, tests, and plan. Expected; still scattered.
- **Data Clumps** — `oneNodeStart` takes Graph, SiteMap, NodeId, EventId separately; Graph+SiteMap already travel into `IncludedDescendantIds.expand`.
- **Middle Man** — `encodeCommandRequest` wraps `EventJson.encodeStartRequest`. Matches [UpdateCodec.fs](src/Client/UpdateCodec.fs); suppress.
- **Speculative Generality** — `decodeUniversalResponse` lands before the Browser reads `{ nodes; events; latestId }` (`runSubmitCommand` only logs). Needed for Shared round-trips.

## Summary

4 mechanical hits (3 file-length, 1 mutable) plus Ev local `e` and a same-directory labeled path; worst: growing [App.fs](src/Client/App.fs), [RouteRegistration.fs](src/Server/RouteRegistration.fs), and [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) through the 400-line rule. Not approval.
