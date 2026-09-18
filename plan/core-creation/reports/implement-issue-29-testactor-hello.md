# Implement issue 29 — TestActor hello

Date: 2026-09-11. Branch `dev`. No commit (parent commits after review).

## What shipped

Public Core Command launch and Poll now return `{ nodes; events; latestId }` on the FileAgent apply mailbox. Command Node text `?test hello` launches TestActor hello. Core validates public Authority plus secret on launch and on the Actor post. Launch registers the live Actor (public identity, secret, cancellation handle, Focus NodeId), appends ActorStarted, then starts the Actor off the mailbox. TestActor receives Authority and secret in ActorStart and presents that pair on `core.post`. It posts one Owned hello child under Focus and queues Succeeded. The first Succeeded appends ActorFinished, drops registry and secret together, and cancels the token without waiting.

TestActor is registered through normal CoreRuntime composition. One fact uses that composed body with no wrap. Another wrap captures the secret only so the outer fact can refuse a later post. TestActor does not assert. Browser HTTP still uses `CoreChangesAccepted`. The discarded `CoreActorPool` mailbox is not wrap-patched.

The F# Event type is `CoreEvent` in Server. Shared cannot use the name Event; that name is the Browser DOM Event in the Client.

## Files changed

- [[src/Server/Core/CoreEvents.fs]] — PublicActorId, ActorStarted, ActorFinished, CoreEvent
- [[src/Server/Core/CoreCommand.fs]] — CoreResponse, CommandLaunchRequest, ActorStart, ActorCore, ActorBody
- [[src/Server/Core/CoreActorMailbox.fs]] — FileAgentMsg Actor cases, registry, launch, admit-and-post, Succeeded, Poll
- [[src/Server/Core/CoreCredentials.fs]] — Authority
- [[src/Server/Core/CoreRuntime.fs]] — launch, poll, post, registerActor, browserAuthority; registers TestActor
- [[src/Server/TestActor.fs]] — hello body
- [[src/Server/FileAgent.fs]] — Actor messages on the apply mailbox; event ids on accepted Changes
- [[src/Server/FileAgentCommand.fs]] — seed, register, launch, poll, post
- [[src/Server/DbAgent.fs]] — exhaustive Actor match; does not run hello
- [[src/Server/Gambol.Server.fsproj]]
- [[tests/Server.Tests/TestActorHelloTests.fs]]
- [[tests/Server.Tests/Gambol.Server.Tests.fsproj]]
- [[plan/core-creation/issues/29-prove-testactor-hello.md]] — Status `done`; hello checklist
- [[plan/core-creation/project.md]] — Actual 20h10m; hello wikilink
- The echo filename is gone. The hello filename is the one home.

## Tests

Filter `FullyQualifiedName~TestActorHelloTests` after `dotnet build tests/Server.Tests -c Debug`.

- `launch TestActor hello through Command text returns ActorStarted before hello child` — passed
- `composed TestActor hello yields hello child and one ActorFinished` — passed
- `hello Change and Succeeded yield hello Graph, one ActorFinished, and revoked secret` — passed

Related Core filters (CoreRuntimeTests, CoreChangesTests, CoreActorPoolTests, CoreCredentialsTests): 25 passed.

`./scripts/client.sh build` succeeded after CoreEvent moved out of Shared.

`./scripts/test.sh all` passed: 371 Server, 1566 Shared (1 skipped).

## Remaining gaps

The issue 29 checklist is proven on FileAgent through the public Core members. These gaps remain outside that checklist:

- DbAgent does not apply Actor launch, post, Succeeded, or Poll. Hello tests use File mode.
- ActorStarted and ActorFinished live in FileAgent mailbox memory. They are not ChangeLog or restart durable. Interrupted restart is out of this increment.
- The discarded CoreActorPool and CoreCredentials mailboxes remain for Browser HTTP and old pool tests.
- Cancel, fail, live query, host-stop, post-twice, and duplicate terminal are not in this increment.
- [[plan/llm-connector/issues/07-lock-run-agent-architecture.md]] still names the first increment through the 29 hello ticket; it does not restate `?test hello` in every sentence.

## Could not prove

- Hello on the Db apply mailbox
- Lifecycle Events after process restart
