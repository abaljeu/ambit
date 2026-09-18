# 35b slices 2–3 — HTTP Adapter and Browser Run

Ticket: [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md). Scope is [§2 Browser Run](plan/core-creation/issues/35b-browser-run-hello.md) and [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md) only. Status stays `ready-for-agent`.

## 1. What landed

### 1.1 HTTP Adapter

POST `/ambit/command` decodes cookie Caller the same way Change posts do, then decodes [ActorStart](src/Shared/History.fs) named ids `zoomId`, `focusId`, `commandId`, `graphIds`, and `eventId`. The route calls [CoreMailbox.startActor](src/Server/Core/CoreMailbox.fs). On Ok it returns `{ nodes; events; latestId }`. `nodes` are the `graphIds` extract from getState. `events` are getEventsSince the request EventId. `latestId` is EventId.max of the persist Graph cursor and those Event ids, because ActorStart does not advance Graph `eventId`.

### 1.2 Browser Run

[execRunOp](src/Client/Commands.fs) reads the current Node after commit. Literal `?` at the start of Node text takes the Command path. [CommandRequest.oneNodeStart](src/Shared/CommandRequest.fs) sets Command, Zoom root, and Focus to that one NodeId and fills `graphIds` from [IncludedDescendantIds.expand](src/Shared/IncludedDescendantIds.fs). The Client posts the request with `credentials:'same-origin'` (cookie Caller). Text that does not start with `?` still uses [AmbleRun](src/Shared/AmbleRun.fs).

### 1.3 TestActor at boot

Production [CreateBoot](src/Server/RouteRegistration.fs) still has `Actors = []`. [§5.1 Register TestActor](plan/core-creation/issues/35b-browser-run-hello.md) was not injected. Mailbox tests register Actor name `test` on the test host only.

## 2. Verification

### 2.1 Shared

[CommandRequestTests](tests/Shared.Tests/CommandRequestTests.fs) cover `?` detect, one-Node ids, and `graphIds` from IncludedDescendantIds. [SerializationTests](tests/Shared.Tests/SerializationTests.fs) round-trip the start request and [UniversalResponse](src/Shared/ApiResponses.fs). [AmbleRunTests](tests/Shared.Tests/AmbleRunTests.fs) stay green (25 passed).

### 2.2 Server HTTP

[ApiPostCommandTests](tests/Server.Tests/ApiPostCommandTests.fs) (7 passed): decode named ids and call startActor; invalid JSON does not start; startActor Error is not a universal response; POST without cookie is 401; POST with cookie reaches startActor; CoreMailbox start encodes ActorStart and advances latestId; after startActor, TestActor can post one Owned child text `hello` under Focus (light check, not headed [§7 Browser proof](plan/core-creation/issues/35b-browser-run-hello.md)).

### 2.3 Client compile

`dotnet fable src/Client --outDir src/Server/wwwroot --sourceMaps` succeeded. `npm run bundle` succeeded.

## 3. Out of scope

[§4 CoreMailbox / CoreMsg / CoreActorPool](plan/core-creation/issues/35b-browser-run-hello.md) and [§5 TestActor / History / CoreRuntime](plan/core-creation/issues/35b-browser-run-hello.md) guts stay as landed by [34b — Outside Core lifecycle proof](plan/core-creation/issues/34b-outside-core-lifecycle-proof.md). [§6 History durability](plan/core-creation/issues/35b-browser-run-hello.md) was skipped. Headed Browser proof of `?test hello` was skipped. Actor select from raw text `?test hello` still needs CSS class `actor-test` or a later Core name parse; this slice only wires the doors.

## 4. Files

1. [CommandRequest.fs](src/Shared/CommandRequest.fs) — detect `?` and one-Node ActorStart.
2. [EventJson.fs](src/Shared/EventJson.fs) — `encodeStartRequest` / `decodeStartRequest`.
3. [ApiResponses.fs](src/Shared/ApiResponses.fs) / [ApiResponseSerialization.fs](src/Shared/ApiResponseSerialization.fs) — UniversalResponse.
4. [Api.fs](src/Server/Api.fs) / [RouteRegistration.fs](src/Server/RouteRegistration.fs) — POST `/ambit/command`.
5. [Commands.fs](src/Client/Commands.fs) / [App.fs](src/Client/App.fs) — Run `?` path and SubmitCommand POST.
