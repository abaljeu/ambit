# 35b slices 4+5+7 — Core TestActor and Browser proof

Ticket: [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md). This unit is [§4 CoreMailbox / CoreMsg / CoreActorPool](plan/core-creation/issues/35b-browser-run-hello.md), [§5 TestActor / History / CoreRuntime](plan/core-creation/issues/35b-browser-run-hello.md), and [§7 Browser proof](plan/core-creation/issues/35b-browser-run-hello.md). [§6 History durability](plan/core-creation/issues/35b-browser-run-hello.md) stays on [46 — Mailbox History durability](plan/core-creation/issues/46-mailbox-history-durability.md).

## 1. What landed

### 1.1 CoreMailbox / CoreMsg / CoreActorPool

Start, admit, and finish were already on the mailbox from [34b — Outside Core lifecycle proof](plan/core-creation/issues/34b-outside-core-lifecycle-proof.md). This unit fills two Browser gaps on [CoreActorPool](src/Server/Core/CoreActorPool.fs):

1. Actor select — [CommandRequest.actorNameFromText](src/Shared/CommandRequest.fs) reads Actor name `test` from Command text `?test hello`. CSS `actor-*` and whole-text fallback stay for the 34b harness.
2. graphIds extract — `Graph.fromExtracted` at `zoomId` builds the Actor Graph. `Graph.fromNodes` threw when Browser `graphIds` started at the Command Node and omitted document ROOT.

### 1.2 TestActor / History / CoreRuntime

[TestActor](src/Server/TestActor.fs) is an injected Actor, not a Core module. [CommandRequest.behaviorFromText](src/Shared/CommandRequest.fs) reads `hello` from `?test hello` (or from bare `hello`). The body posts one Owned child text `hello` under Focus, then queues `ActorStop ActorSucceeded`. [CreateBoot](src/Server/RouteRegistration.fs) injects `(ActorName "test", TestActor.actorFn)` before [CoreRuntime.create](src/Server/Core/CoreRuntime.fs) starts the mailbox. CoreRuntime still does not hardcode TestActor.

### 1.3 Browser proof

Production WebApplicationFactory POST `/ambit/changes` seeds `?test hello` with no `actor-test` CSS. POST `/ambit/command` uses one-Node `graphIds`. getState then shows one Owned child text `hello` under Focus. No Agent service. Headed `:5215` Run is recorded in [§2.4 Headed Browser](#24-headed-browser).

## 2. Verification

### 2.1 Shared

[CommandRequestTests](tests/Shared.Tests/CommandRequestTests.fs) — 7 passed: `?` detect, one-Node ids, `graphIds`, Actor select `test` from `?test hello`, behavior `hello` from `?test hello` and from `hello`.

### 2.2 Server mailbox and HTTP

Focused Server filter on TestActorHello, ApiPostCommand, CoreMailboxDoor, CoreActorPool, ActorCoreChangesDoor, CoreMsgActorCases — 58 passed. New facts: mailbox `?test hello` without CSS posts one Owned `hello` under Focus; production boot HTTP Run of `?test hello` posts the same child.

### 2.3 Client compile

`dotnet fable src/Client --outDir src/Server/wwwroot --sourceMaps` succeeded. `npm run bundle` succeeded.

### 2.4 Headed Browser

Live `http://127.0.0.1:5215` with injected Actor `test`: GET `/ambit` issues the development cookie; POST `/ambit/changes` seeds Node text `?test hello`; POST `/ambit/command` with one-Node `graphIds` returns 200; getState then has one Owned child text `hello` under that Focus. Headed Chrome on the same server at `/ambit?debug=1` shows Zoom `?test hello` and one child `hello`. No Agent service. The computerUse subagent did not start (model quota), so there is no recorded Ctrl+Enter click-path; the Client Run door is the same POST `/ambit/command` used by [execRunOp](src/Client/Commands.fs).

## 3. Out of scope

[46 — Mailbox History durability](plan/core-creation/issues/46-mailbox-history-durability.md) is not in this unit. [§1 Unfolded Included context id list](plan/core-creation/issues/35b-browser-run-hello.md) through [§3 HTTP Adapter](plan/core-creation/issues/35b-browser-run-hello.md) were not reopened except the Shared Command text parse used by Core and TestActor.

## 4. Files

1. [CommandRequest.fs](src/Shared/CommandRequest.fs) — Actor name and behavior from Command text.
2. [CoreActorPool.fs](src/Server/Core/CoreActorPool.fs) — select `test`; extract via `fromExtracted`.
3. [TestActor.fs](src/Server/TestActor.fs) — injected Actor `test`.
4. [RouteRegistration.fs](src/Server/RouteRegistration.fs) — inject TestActor at production boot.
5. [Gambol.Server.fsproj](src/Server/Gambol.Server.fsproj) — compile TestActor outside Core.
6. [CommandRequestTests.fs](tests/Shared.Tests/CommandRequestTests.fs) — parse facts.
7. [TestActorHelloTests.fs](tests/Server.Tests/TestActorHelloTests.fs) — `?test hello` lifecycle.
8. [ApiPostCommandTests.fs](tests/Server.Tests/ApiPostCommandTests.fs) — production boot HTTP proof.
9. [Gambol.Server.Tests.fsproj](tests/Server.Tests/Gambol.Server.Tests.fsproj) — drop test-only TestActor compile.
