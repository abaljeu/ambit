# Contain Core Change Authority

Fix for the hard finding in [[review-standards-initial-core-changes.md]]: Core change authority was not contained. The agent post functions were public, so any Server code could publish a Change without Core.

## Step 1 — the handle contract moved down

[[CoreChanges.fs]] now holds the contract. The record moved out of the old Core handle file and is named CoreChanges. The getChangesSince field takes a Revision instead of an integer. The HTTP adapter in [[Api.fs]] makes the Revision from the decoded integer before it enters Core, at both the Poll and the Load call sites. No other call site used getChangesSince.

One shared builder, CoreChanges.accepted, sits next to the type. It takes the revision and the readiness flag, then the confirmed Changes, the external-changes flag, and the message. The two near-identical private builders in [[FileAgent.fs]] and [[DbAgent.fs]] are now two-line calls to it.

## Step 2 — the agent post functions are private

FileAgent.postChange and FileAgent.postGraphOnlyChange are private. DbAgent.postChange and DbAgent.postGraphOnlyChange are private. Each agent has one new public constructor, FileAgent.coreChanges and DbAgent.coreChanges, that makes a CoreChanges value from the agent closures. The old GraphAgentHandle.ofFile and GraphAgentHandle.ofDb adapters are deleted, because the agents now supply the value.

The non-Change surface stays: FileAgent.create, createWithDependencies, defaultDependencies, runBounded, ChangeProcessingTimeoutMs, flushSnapshot, dispose, and the read functions. FileAgent.initialState is new, because the record field is no longer reachable from outside the file. DbAgent keeps create, createWithDataDir, createForTest, createForTestWithDependencies, isReady, and the read functions.

## Step 3 — the mailbox back door

The containment reached: **both agent record representations are private**. FileAgent.fs declares type FileAgent = private { ... } and DbAgent.fs declares type DbAgent = private { ... }. In F# this scopes the fields to the declaring file, so no code outside FileAgent.fs or DbAgent.fs can read agent.mailbox. The compiler enforces this, not a convention. The build proves it: no call site outside those two files broke on the mailbox field.

The FileAgentMsg DU cases stay public. Private cases are not possible here, because DbAgent.fs and DbAgentStartup.fs both match on the same DU from other files. This is acceptable, because a message value is inert with no mailbox to receive it.

Residual leaks, all inside the Server assembly and all needed by the current design:

- Code inside FileAgent.fs or DbAgent.fs can post PostChange directly. This is the declaring file, so it is by design.
- DbAgentStartup.run receives the live MailboxProcessor as its inbox parameter. It only reads and replies today, but it holds a mailbox that accepts PostChange. To close this, the startup loop would need a narrower message type, which is part of the deferred deeper refactor.

DatabaseSetup.getOrCreateDbAgent now returns a CoreChanges value. The cache still holds the raw DbAgent behind the lock and the same dataDir key, so the single-shared-instance behaviour does not change. The startup warm call at the end of resolveDbConnection still ignores the result, so no caller needs the raw agent any more.

## Step 4 — Core keeps only what Core owns

The old Core handle file no longer matched its content, so it is renamed to [[CoreRuntime.fs]] and registered under that name in [[Gambol.Server.fsproj]]. It holds the persistence-mode selection (CoreRuntime.create, formerly createRuntime), the read-only rejection (CoreRuntime.readOnly), and the DB mirror (CoreRuntime.ofFileWithDbMirror). The runtime record is renamed CoreRuntime. The mirror now takes two CoreChanges values, so it no longer touches an agent type. The two mirror failure messages use the [Core] prefix instead of the false [Api] prefix.

## Tests

All the direct post call sites now go through the CoreChanges handle: [[IgnoredDestinationValidationTests.fs]], [[DbAgentFailureTests.fs]], [[DbAgentTests.fs]], [[FileAgentFailureTests.fs]], [[GraphOnlyChangePostTests.fs]], [[LazyLoadReconciliationServerTests.fs]], and [[DatabaseProjectionContractTests.fs]]. The handle type annotations in [[ApiGetStateTests.fs]], [[ApiPostLoadTests.fs]], [[CoreChangesTests.fs]], and [[GraphOnlyChangePostTests.fs]] follow the rename. The FileAgent.createWithDependencies injection path in [[FileAgentFailureTests.fs]] is unchanged and still works. Two lines there read the checkpoint state through FileAgent.initialState instead of the record field.

The requireOk helper in [[CoreChangesTests.fs]] no longer calls failwith. It reports the Error through Assert.Fail, so a failure is an xUnit assertion failure.

## Verification

Build: dotnet build on [[Gambol.Server.fsproj]] and on the Server test project both succeed with zero errors and zero F# warnings.

Containment grep: no FileAgent.postChange, FileAgent.postGraphOnlyChange, DbAgent.postChange, or DbAgent.postGraphOnlyChange reference exists outside the declaring file, in src or in tests. The private modifier makes this a compiler guarantee.

Focused tests, all passed:

| Test set | Result |
| --- | --- |
| CoreChanges, GraphOnlyChangePost, FileAgentFailure, IgnoredDestinationValidation, LazyLoadReconciliationServer | 40 passed, 0 failed, 0 skipped |
| DbAgent, DbAgentFailure, DatabaseProjectionContract | 25 passed, 0 failed, 0 skipped |

PostgreSQL was available. TEST_DB_CONNECTION_STRING is not set in the environment, but TestBackend resolves the connection string from the test config beside the test binary, so the database tests ran instead of skipping. Nothing was left unrun.

The Client compile gate does not apply. The Client project references only Gambol.Shared and Gambol.Shared.Documents, and this work touches neither. Only src/Server and tests/Server.Tests changed.

## Deviations

Two.

First, the agent read functions stay public. FileAgent.getState, getRevision, getChangesSince, tryGetState, and the DbAgent equivalents are still callable from tests. The plan listed the getState and getRevision test call sites for migration too. Every read call site would then need an unwrap at the call site, which is about forty more changed test lines with no gain for the finding: a read is not Change authority. The Change path, which is the authority, is fully migrated. Say the word if you want the reads migrated as well.

Second, [[DbAgent.fs]] grows from 540 to 545 lines. The shared accepted builder saves three lines, and the new coreChanges constructor costs eight. There is no way to add the constructor without the file growing, short of the deferred deeper refactor that would move the apply and log work out of the agent.

The deeper refactor, where Core owns apply, amend, and log and the agents become storage ports, is untouched.
