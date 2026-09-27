# Independent code review recheck — [15 — Keep the App outside Peer Actor hosting](../issues/15-keep-app-outside-peer-actor-hosting.md)

Range: follow-up commit `498c2b38` after the failed review at `0a532cb1` on PR 142. This recheck inspected the branch and PR body, but did not accept the implementer's test log or self-pass as evidence. Ticket Status stays `coded`.

## 1. Landing decision — Pass

**Pass for landing.** Both prior Must-fixes are closed. The Server test matrix drives Load and Save with explicit Git and Plain resolved to Git through `Api.postLoadSaveCommand`, `CoreMailbox.startLoadSaveCommand`, a registered `GithubTransportActor`, and the Actor pool. The App/Browser boundary tests resolve every current declared compile item, including linked or nested paths, and verify the exact transitive project-reference closure. Independent MSBuild evaluation confirmed that these declarations are the complete current App and Browser compile surfaces.

## 2. Must-fix — None

No Must-fix remains for landing.

## 3. Should-fix

### 3.1. Compare against evaluated MSBuild items

[AppGithubTransportBoundaryTests.fs](../../../tests/Server.Tests/AppGithubTransportBoundaryTests.fs) reads direct XML descendants from each project file. This resolves the current literal `Compile` and `ProjectReference` includes, including linked and nested paths, and independent `dotnet msbuild -getItem` output confirms that it covers the current build. It would not discover a future item supplied by an imported props or targets file, or correctly expand every MSBuild property, condition, or wildcard. Querying evaluated MSBuild items would make the regression guard match its compile-surface name under future project-file changes. This limitation does not leave the current boundary unproved.

### 3.2. Remove the remaining exception-based test branches

[LoadSaveCommandTests.fs](../../../tests/Server.Tests/LoadSaveCommandTests.fs) still uses `failwith` in `decodePoll`, and [LoadSaveCommandClientTests.fs](../../../tests/Server.Tests/LoadSaveCommandClientTests.fs) still uses `failwith` in `applyOps`, `onlyUpdater`, and continuation mismatch branches. The follow-up removes the exception paths involved in operation and pre-pick decoding, but these remaining test branches still conflict with the F# no-exception rule. They do not weaken either closed Must-fix.

### 3.3. Keep the state scan classified as a structural guard

The record-field and source-term checks catch the current GitHub, remote, branch, and credential vocabulary, but they cannot semantically classify every future renamed state representation. The PR body now states this limitation accurately. The exact dependency closure and host/invocation checks provide the durable boundary proof; the state-name scan remains supplementary.

## 4. Good

### 4.1. The complete Git route matrix crosses the real Server door

[LoadSaveCommandTests.fs](../../../tests/Server.Tests/LoadSaveCommandTests.fs) contains four cases: Load with Git, Load with Plain resolved to Git, Save with Git, and Save with Plain resolved to Git. Each case posts encoded JSON to `Api.postLoadSaveCommand`, resolves Git, calls `CoreMailbox.startLoadSaveCommand`, and targets the operation-specific registered `GithubTransportActor`.

### 4.2. The matrix proves that the Peer Actor starts

The test does not stop at an accepted response. Its operation-specific Actor dependencies complete a `TaskCompletionSource` only after Load reaches `pullTracked` or Save reaches `commitAll`, and the test also requires the matching `ActorStart` Event in EventLog. This closes the former local-`post`-stub gap.

### 4.3. The current App and Browser compile surfaces are covered

[AppGithubTransportBoundaryTests.fs](../../../tests/Server.Tests/AppGithubTransportBoundaryTests.fs) combines both App and Browser projects, resolves each `Compile Include` relative to its project, and scans the resulting files. Independent MSBuild evaluation returned the same 43 Browser and 7 App compile items, with no generated or imported compile item outside the scan.

### 4.4. The project dependency boundary is exact

The recursive project-reference check requires the complete closure to equal the approved five projects: Browser, App, Shared, Document, and Shared dotnet. A direct or transitive Server or other host dependency changes the set and fails the test; the check is no longer limited to one exact forbidden Server project reference.

### 4.5. Independent tests pass

The focused boundary and routing filter passed 19/19 tests with zero skips. The complete Server suite then passed 572/572 tests with zero skips in 2m05s. The mechanical standards scan for `0a532cb1...498c2b38` produced no size, line-length, or naming findings.

### 4.6. Product code and ticket Status remain unchanged

The follow-up changes tests only. [15 — Keep the App outside Peer Actor hosting](../issues/15-keep-app-outside-peer-actor-hosting.md) remains `coded`, as required.

## 5. Review-axis summary

Standards has no finding in the follow-up diff. The older exception-based test branches and the compile-scan hardening opportunity remain non-blocking Should-fixes. Spec has no remaining Must-fix. Landing decision: **Pass**.
