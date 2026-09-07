# Initial Core Changes implementation

## Goal

Implement the resolved issues 03–06 as the bounded initial Core Changes increment. A typed `GraphAgentHandle` must be the only production capability that can accept Changes and publish the resulting Server State, Revision, and History tail. Browser POST uses Normal Core Changes. Parse uses Graph-only Core Changes directly after it produces Changes. The typed Normal seam enables later work on [[plan/core-creation/issues/01-generalized-server-actor-produce-path.md]].

## Scope

- Implement only the resolved contracts in [[plan/core-creation/issues/03-define-typed-core-changes-contract.md]], [[plan/core-creation/issues/04-separate-http-adapter-from-core-changes.md]], [[plan/core-creation/issues/05-place-core-changes-in-existing-projects.md]], and [[plan/core-creation/issues/06-ready-the-initial-core-changes-increment.md]].
- Add typed Normal and Graph-only operations. Each operation accepts a `Change list` and returns accepted Core facts or the current text Reject.
- Put the Server Core seam under [[src/Server/Core/]] in the existing Server project. Keep Shared apply and amendment in [[src/Shared/History.fs]] and [[src/Shared/ChangeAmendment.fs]].
- Keep `FileAgent` and `DbAgent` mailboxes as the sequencing and publication owners. Put their production construction and selection behind `GraphAgentHandle`.
- Keep `Api.postChange` as the HTTP Adapter. It decodes the Browser body, calls Normal Core Changes, adds HTTP response fields, and encodes the current acknowledgement.
- Make Parse, lazy-load reconciliation, and git reconciliation call typed Graph-only Core Changes without internal JSON.

## Non-goals

- Do not implement Core Files, general Query, Command, the Actor pool, Actor cancellation, Actor completion, or Actor shutdown. These belong to issues 07–12.
- Do not implement issue 01. Do not add a production Server Actor or another production Server-producer path. This increment only supplies the typed Normal seam and direct test evidence that issue 01 can start later.
- Do not implement issue 13. Do not delete the mirror, alter persistence-mode selection, add mirror tests, or redesign the obsolete mirror path.
- Do not make Parse an Actor and do not change Parse planning.
- Do not redesign ACID apply, initialization, repair, database/file reconciliation, Graph-to-file, or file-to-Graph protocols.
- Do not add Graph/file functions or move Shared source modules.

## Settled constraints

- Normal and Graph-only have the same typed input and accepted result. Empty lists and no-effect Changes remain Rejects.
- The accepted result contains the final `Revision`, acknowledged Changes in input order, `externalChanges`, the persistence message, and readiness. A repeated `changeId` returns its stored accepted Change through this result and does not advance Revision.
- Normal retains current amendment, validation, persistence, stamp, batch, and timeout behavior. Graph-only skips document validation and document persistence as it does now.
- The HTTP Adapter retains auth, client hint, body read, JSON decode and encode, protocol fields, and HTTP status mapping. Core has no JSON, `HttpRequest`, `IResult`, or HTTP status concerns.
- Graph, Node, and State values are immutable and can be shared for Query and planning. A Graph value does not grant Change authority. `GraphAgentHandle` is the mutation-ready capability.
- After mutation-ready initialization, Core is the only runtime capability that accepts, publishes, and persists Changes. Existing initialization, repair, and reconciliation protocols remain named exceptions.
- The supported production direction uses Database persistence when it is available and rejects Changes on the read-only Graph/file fallback when it is unavailable. This increment does not change the current selector or mirror.
- Keep the current eight-second Change-processing timeout. Do not add late-completion guarantees for abandoned tasks.

## Current evidence

- [[src/Server/Api.fs]] defines `AgentHandle` and passes JSON strings to the agents. `Api.postChange` decodes the agent acknowledgement instead of the request.
- [[src/Server/FileAgent.fs]] and [[src/Server/DbAgent.fs]] decode `ChangeBatch`, apply and amend typed Changes, encode accepted facts, and reply with JSON strings. Their mailbox loops already serialize apply, persist, publish, and read operations.
- [[src/Server/GraphOnlyChangePost.fs]] and `Api.postParseFile` encode internal Change JSON before Graph-only posts.
- [[src/Server/Gambol.Server.fsproj]] compiles `FileAgent` before `Database` and `DbAgent`. A Core accepted-result type must compile before both agents. A handle that wraps both agents must compile after `DbAgent` and before `Api`.
- [[tests/Server.Tests/StateEndpointTests.fs]] already covers HTTP acknowledgement, Reject, batch atomicity, amendment, deduplication, and Poll. [[tests/Server.Tests/FileAgentFailureTests.fs]], [[tests/Server.Tests/DatabaseProjectionContractTests.fs]], and [[tests/Server.Tests/DbAgentTests.fs]] cover persistence, stamps, readiness, and timeout. [[tests/Server.Tests/GraphOnlyChangePostTests.fs]] and [[tests/Server.Tests/LazyLoadReconciliationServerTests.fs]] cover Graph-only reconciliation.

## Ordered implementation

### 1. Define typed Core Changes facts and convert agent messages

Files: add [[src/Server/Core/CoreChanges.fs]]; edit [[src/Server/Gambol.Server.fsproj]], [[src/Server/FileAgent.fs]], and [[src/Server/DbAgent.fs]]; adapt only directly affected agent tests in [[tests/Server.Tests/FileAgentFailureTests.fs]], [[tests/Server.Tests/DatabaseProjectionContractTests.fs]], [[tests/Server.Tests/DbAgentTests.fs]], [[tests/Server.Tests/DbAgentFailureTests.fs]], and [[tests/Server.Tests/IgnoredDestinationValidationTests.fs]].

Add one Server-only accepted record with `revision`, `changes`, `externalChanges`, `message`, and `isReady`. Compile it after `DocumentLoader.fs` and before `FileAgent.fs`. Change `PostChange` and `PostGraphOnlyChange` mailbox messages and public agent functions from JSON strings to `Change list` input and the typed accepted result. Remove request decode and accepted-result encode from both agents. Keep each existing apply, validation, persistence, timeout, and publication sequence in place. Return domain `State` and `Revision` at the typed read seam; expose readiness separately.

Focused verification:

```sh
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~FileAgentFailureTests|FullyQualifiedName~DatabaseProjectionContractTests|FullyQualifiedName~DbAgentTests|FullyQualifiedName~DbAgentFailureTests|FullyQualifiedName~IgnoredDestinationValidationTests"
```

### 2. Put the complete typed handle and selector in Core

Files: add [[src/Server/Core/GraphAgentHandle.fs]]; edit [[src/Server/Gambol.Server.fsproj]], [[src/Server/Api.fs]], [[src/Server/RouteRegistration.fs]], [[src/Server/SavePrep.fs]], [[tests/Server.Tests/ApiGetStateTests.fs]], [[tests/Server.Tests/ApiPostLoadTests.fs]], and [[tests/Server.Tests/SavePrepTests.fs]].

Compile `GraphAgentHandle.fs` after `SavePrep.fs` and before `Api.fs`. Move the current agent wrappers, lazy FileAgent creation, and production agent selection behind this Core file. Replace `AgentHandle`; do not retain a second handle type. The handle exposes typed current State, Revision, `getChangesSince`, readiness, Normal Changes, and Graph-only Changes. Route composition can keep existing DataDir, status, and save-preparation facts, but it must not receive raw `FileAgent` or `DbAgent` values. Give `SavePrep` only the existing typed read and flush callbacks that it needs.

The FileAgent, DbAgent, and handle signature changes must leave the current source compiling. If the obsolete mirror wrapper still exists, adapt only its input and result signatures. Preserve its current call order and result behavior. Do not move callers, add tests, delete it, or change mode selection. This is compile compatibility for issues 03–06, not issue 13 work.

Focused verification:

```sh
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~ApiGetStateTests|FullyQualifiedName~ApiPostLoadTests|FullyQualifiedName~SavePrepTests"
```

### 3. Make HTTP and Parse use the typed operations

Files: edit [[src/Server/Api.fs]], [[src/Server/RouteRegistration.fs]], [[src/Server/GraphOnlyChangePost.fs]], [[src/Server/LazyLoadReconciliationServer.fs]], [[tests/Server.Tests/StateEndpointTests.fs]], [[tests/Server.Tests/ChangeEndpointResilienceTests.fs]], [[tests/Server.Tests/GraphOnlyChangePostTests.fs]], and [[tests/Server.Tests/LazyLoadReconciliationServerTests.fs]].

In `Api.postChange`, decode `ChangeBatch`, reject malformed or empty input with the current HTTP behavior, call Normal Core Changes with `batch.changes`, map a typed Reject through the current error mapper, and encode the accepted facts with current build and protocol fields. Keep RouteRegistration auth, client hint, body read, and endpoint mapping unchanged. Make `postParseFile` construct one typed Change and call Graph-only directly. Make `GraphOnlyChangePost.postChunks` pass one-item typed Change lists and use the accepted Revision for the next chunk. Keep Parse and reconciliation planners unchanged.

Focused verification:

```sh
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~StateEndpointTests|FullyQualifiedName~ChangeEndpointResilienceTests|FullyQualifiedName~GraphOnlyChangePostTests|FullyQualifiedName~LazyLoadReconciliationServerTests"
```

### 4. Add only the typed seam evidence

Files: add [[tests/Server.Tests/CoreChangesTests.fs]] and register it in [[tests/Server.Tests/Gambol.Server.Tests.fsproj]].

Add two tests. First, use a test-only caller that represents the future Server Actor boundary: create the selected test Core handle, call Normal with typed Changes and no HTTP submit, then call the existing Poll Adapter and prove that Poll returns the accepted Change and Revision. This harness is not a production Server Actor or Server-producer path. Second, give `Api.postChange` a recording handle and prove that valid JSON reaches Normal as typed Changes while malformed JSON does not call Core. Update the existing Graph-only post test to record `Change list` values instead of JSON; do not add a duplicate Graph-only behavior test.

Focused verification:

```sh
dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~CoreChangesTests|FullyQualifiedName~GraphOnlyChangePostTests"
```

## Acceptance gate

- `GraphAgentHandle` is the only production Graph Change capability. Its input and result are typed and contain no transport types.
- A test-only non-HTTP caller invokes Normal and the accepted Change is Poll-visible. This evidence enables issue 01; it does not deliver it.
- Browser POST keeps current auth, wire response, Reject, acknowledgement, amendment, deduplication, and Poll behavior.
- Parse and reconciliation use typed Graph-only Changes and keep current document behavior.
- The current eight-second timeout remains in effect.
- No Files, Query, Command, Actor-pool, ACID, mirror-removal, or new Graph/file behavior enters this increment.

Run the four focused test commands above. Then run this compile check:

```sh
dotnet build src/Server/Gambol.Server.fsproj -c Debug
```

No Shared or Browser source file is in the intended edit set, so the Browser compile gate is not required. If implementation changes a Shared or Browser dependency, run `./scripts/client.sh build` as required by the local workflow.
