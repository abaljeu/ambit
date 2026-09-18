# Spec review — [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md) slices 2–3

Range: `git diff origin/staging...HEAD` (commit `24ee25ab`). Spec: [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md) [§2 Browser Run](plan/core-creation/issues/35b-browser-run-hello.md) and [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md); [Core creation architecture](plan/core-creation/arch.md) Story path **Browser Run hello** items 1–2 and hop 10; Module **9. Browser Run**; Module **10. HTTP Adapter**. This report is not approval. Ticket Status is unchanged.

## (a) Missing or partial

1. **Browser Command POST does not use `{ nodes; events; latestId }`.** Spec: “encode universal `{ nodes; events; latestId }` for Command when that path is exercised” (Module **10. HTTP Adapter** Interface item 7); Story path **Browser Run hello** hop 10 “universal response when that path is exercised”; [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md) item 3 “return `{ nodes; events; latestId }` when this Command path runs.” [Api.fs](src/Server/Api.fs) `postCommand` encodes that object. [App.fs](src/Client/App.fs) `runSubmitCommand` on HTTP 200 logs `bodyLen` only; it does not decode `nodes`, `events`, or `latestId`. Headed [§7 Browser proof](plan/core-creation/issues/35b-browser-run-hello.md) is out of this axis. The unused body is a partial of hop 10 on the Run `?` send this slice added. [§2 Browser Run](plan/core-creation/issues/35b-browser-run-hello.md) items 1–4 and [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md) items 1–2 are present (`?` detect, one-Node ActorStart, Included `graphIds`, cookie Caller, [CoreMailbox.startActor](src/Server/Core/CoreMailbox.fs), AmbleRun otherwise).

## (b) Behaviour not asked for

1. **Adapter projects `nodes` from `graphIds`.** Spec [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md) item 3 names the keys only. [Api.fs](src/Server/Api.fs) `nodesForRequest` keeps getState Nodes whose ids are in request `graphIds`. Alan’s lock is that the server does not Zoom-expand or Fold-walk. This id filter is extra.

2. **Adapter recomputes `latestId`.** Spec names `latestId`. Code uses `EventId.max` of persist `getEventId` and Ev ids from `getEventsSince`.

## (c) Looks implemented but wrong

1. **Encode runs on `startActor` Ok, before ActorStop.** Story path **Browser Run hello** lists hop 10 after hop 9 “EventLog appends ActorStop Ev.” [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md) items 2–3 say submit through `startActor` then encode. [CoreMailbox.startActor](src/Server/Core/CoreMailbox.fs) does not wait for the Actor body. Returned `nodes` / `events` are the start snapshot (ActorStart Ev in tests), not the post-ActorStop Graph. Wrong vs hop order; aligned with the [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md) door sequence. [§4 CoreMailbox / CoreMsg / CoreActorPool](plan/core-creation/issues/35b-browser-run-hello.md) guts stay out of this axis.

## Summary

(a) 1 partial; (b) 2; (c) 1. Worst in this axis: Browser discards `{ nodes; events; latestId }` on the new Run `?` POST.
