# Code review — [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md) slices 2–3

Independent review. Not approval. Ticket Status left `ready-for-agent`.
**Pin:** three-dot `origin/staging...HEAD`. Tip `24ee25ab` (`origin/cursor/35b-slice2-3-http-browser-run-d461`, same as this report branch parent). Base current `origin/staging` `e2ef8960`. Merge-base `6a50f582`. Diff non-empty: 20 files, +651/−42. One commit: `24ee25ab Implement 35b slices 2–3 HTTP Command and Browser Run`.
**Spec:** [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md) [§2 Browser Run](plan/core-creation/issues/35b-browser-run-hello.md) and [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md) only. Arch Story path **Browser Run hello** items 1–2 and hop 10; Module **9. Browser Run**; Module **10. HTTP Adapter**. Out of this Spec axis: [§6 History durability](plan/core-creation/issues/35b-browser-run-hello.md) (moved to 46), headed [§7 Browser proof](plan/core-creation/issues/35b-browser-run-hello.md), full TestActor production inject unless the diff claims it.
**Mechanical scan:** `python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`. Axis reports: [Standards](code-review-35b-slice2-3-standards.md), [Spec](code-review-35b-slice2-3-spec.md).
**Focused tests:** Shared `CommandRequestTests` + `SerializationTests` — 44 passed, 0 failed. Server `ApiPostCommandTests` — 7 passed, 0 failed. Client compile gate `./scripts/client.sh build` — Fable + bundle succeeded.

## Standards

Range: `git diff origin/staging...HEAD` (commit `24ee25ab`). Baseline: [SMELLS.md](.agents/skills/code-review/SMELLS.md). Not approval. Status unchanged.

### Mechanical scan (documented-standard hits)

Each printed line is a hit with the rule path on that line.

1. [App.fs](src/Client/App.fs) `.agents/rules/fsharp-source.md` FILE 794→818 — already over 400; change increased it.
2. [RouteRegistration.fs](src/Server/RouteRegistration.fs) `.agents/rules/fsharp-source.md` FILE 382→406 — already over 400 or new file over 400; change increased it (crossed 400 from 382).
3. [ApiPostCommandTests.fs](tests/Server.Tests/ApiPostCommandTests.fs):210 `.agents/rules/fsharp-source.md` MUTABLE `let mutable found = false`.
4. [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) `.agents/rules/fsharp-source.md` FILE 548→591 — already over 400; change increased it.

measure-fs-size bindings are ≤40 lines (`postCommand` 33, `runSubmitCommand` 23). No added 100-char line.

### Hard violations

1. **File length** ([fsharp-source.md](.agents/rules/fsharp-source.md) — ≤400; do not grow a file already over): [App.fs](src/Client/App.fs) 794→818 via `runSubmitCommand`. [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) 548→591 via round-trips (new file would stay under, like [CommandRequestTests.fs](tests/Shared.Tests/CommandRequestTests.fs)). [RouteRegistration.fs](src/Server/RouteRegistration.fs) 382→406 by inlining POST `/ambit/command` into `registerStateRoutes` (lines 165–263, 99 lines vs ≤40).
2. **No mutable** ([fsharp-source.md](.agents/rules/fsharp-source.md)): `waitForHello` uses `let mutable found = false` then `found <-` while polling CoreMailbox State for an Owned `hello` child under Focus.
3. **Event/Ev naming** ([fsharp-source.md](.agents/rules/fsharp-source.md)): [Api.fs](src/Server/Api.fs) `List.map (fun e -> e.id)` binds Ev as `e`, not `event`/`ev`.
4. **Same-directory labeled link** ([markdown-writing.md](.agents/rules/markdown-writing.md)): [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md) Blocked-by uses a root path instead of `(34b-outside-core-lifecycle-proof.md)`. It did drop `[[path|label]]`.
5. **EventId.fromJson in fixtures** ([core-api.md](.agents/rules/core-api.md) — drafts use `EventId.zero`; `fromJson`/`toJson` only when serializing): [ApiPostCommandTests.fs](tests/Server.Tests/ApiPostCommandTests.fs) `startedEvent` uses `EventId.fromJson 1`; [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) uses `EventId.fromJson 4` and `EventId.fromJson 2`. Production encode/decode paths use EventId serialize helpers only. No `EventId.next`. [oneNodeStart](src/Shared/CommandRequest.fs) copies the Client EventId cursor.

[core-api.md](.agents/rules/core-api.md): production EventId uses encode/decode only. No unused bindings ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md)). Plan text names sections ([refer-by-name.md](.agents/rules/refer-by-name.md)). `nodesForRequest` walks `graphIds`, not full-graph `Map.toList`.

### Judgement calls ([SMELLS.md](.agents/skills/code-review/SMELLS.md))

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

## Spec

Range: `git diff origin/staging...HEAD` (commit `24ee25ab`). Spec: [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md) [§2 Browser Run](plan/core-creation/issues/35b-browser-run-hello.md) and [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md); [Core creation architecture](plan/core-creation/arch.md) Story path **Browser Run hello** items 1–2 and hop 10; Module **9. Browser Run**; Module **10. HTTP Adapter**. This report is not approval. Ticket Status is unchanged.

### (a) Missing or partial

1. **Browser Command POST does not use `{ nodes; events; latestId }`.** Spec: “encode universal `{ nodes; events; latestId }` for Command when that path is exercised” (Module **10. HTTP Adapter** Interface item 7); Story path **Browser Run hello** hop 10 “universal response when that path is exercised”; [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md) item 3 “return `{ nodes; events; latestId }` when this Command path runs.” [Api.fs](src/Server/Api.fs) `postCommand` encodes that object. [App.fs](src/Client/App.fs) `runSubmitCommand` on HTTP 200 logs `bodyLen` only; it does not decode `nodes`, `events`, or `latestId`. Headed [§7 Browser proof](plan/core-creation/issues/35b-browser-run-hello.md) is out of this axis. The unused body is a partial of hop 10 on the Run `?` send this slice added. [§2 Browser Run](plan/core-creation/issues/35b-browser-run-hello.md) items 1–4 and [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md) items 1–2 are present (`?` detect, one-Node ActorStart, Included `graphIds`, cookie Caller, [CoreMailbox.startActor](src/Server/Core/CoreMailbox.fs), AmbleRun otherwise).

### (b) Behaviour not asked for

1. **Adapter projects `nodes` from `graphIds`.** Spec [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md) item 3 names the keys only. [Api.fs](src/Server/Api.fs) `nodesForRequest` keeps getState Nodes whose ids are in request `graphIds`. Alan’s lock is that the server does not Zoom-expand or Fold-walk. This id filter is extra.
2. **Adapter recomputes `latestId`.** Spec names `latestId`. Code uses `EventId.max` of persist `getEventId` and Ev ids from `getEventsSince`.

### (c) Looks implemented but wrong

1. **Encode runs on `startActor` Ok, before ActorStop.** Story path **Browser Run hello** lists hop 10 after hop 9 “EventLog appends ActorStop Ev.” [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md) items 2–3 say submit through `startActor` then encode. [CoreMailbox.startActor](src/Server/Core/CoreMailbox.fs) does not wait for the Actor body. Returned `nodes` / `events` are the start snapshot (ActorStart Ev in tests), not the post-ActorStop Graph. Wrong vs hop order; aligned with the [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md) door sequence. [§4 CoreMailbox / CoreMsg / CoreActorPool](plan/core-creation/issues/35b-browser-run-hello.md) guts stay out of this axis.

## Summary

Standards: 5 hard (3 file-length, 1 mutable, 1 Ev local `e`, plus EventId.fromJson fixtures and a same-directory labeled path), 5 judgement. Worst in-axis: growing [App.fs](src/Client/App.fs), [RouteRegistration.fs](src/Server/RouteRegistration.fs) (crosses 400; `registerStateRoutes` 99 lines), and [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) through the 400-line rule.
Spec: (a) 1 partial; (b) 2; (c) 1. Worst in-axis: Browser discards `{ nodes; events; latestId }` on the new Run `?` POST. [§2 Browser Run](plan/core-creation/issues/35b-browser-run-hello.md) items 1–4 and [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md) items 1–3 (Adapter encode) hold. Unused Client body and start-snapshot encode are residuals for headed [§7 Browser proof](plan/core-creation/issues/35b-browser-run-hello.md), not missing §2 send or §3 door. Production `Actors = []` was not claimed.

**approve this slice land**
