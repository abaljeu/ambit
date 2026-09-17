# Code review — reshape stretch workspace upload

Independent review. Not approval. No ticket Status change.

Range: `origin/staging...HEAD` (three-dot). Tip `b3065176`. `origin/staging` `f94924ad`. Merge-base `0179d8d6`. Non-empty. Five commits: `57a58ffa` fat CLI harness; `caa4c1c3` Newtonsoft `/state`; `529a6c8e` first stretch proof; `d1cd955f` reshape onto Shared.dotnet; `b3065176` reshape proof. Net 12 files (+524 / −14). Fat [Program.fs](scripts/stretch-workspace-upload/Program.fs) is absent on HEAD (deleted in the reshape commit).

Mechanical scan (`python3 .agents/skills/code-review/scripts/standards-scan.py --diff origin/staging`): no FILE>400 growth; no added LONG>100; no refer-by-name or markdown bare-id hits. All new bindings under 40 lines ([AmbitSession.fs](src/Shared/dotnet/AmbitSession.fs) 155; [WorkspaceCloudUpload.fs](src/Shared/dotnet/WorkspaceCloudUpload.fs) 187; [WorkspaceCloudUploadTests.fs](tests/Shared.Tests/WorkspaceCloudUploadTests.fs) 52; [stretch-workspace-upload.fsx](scripts/stretch-workspace-upload.fsx) 49).

Spec source: Alan’s reshape ask (thin `.fsx` / FSI that reuses Desktop non-UI; no replication versus the old fat Program.fs; same `WorkspaceFileSync.post` as `/_desktop/workspace-push`; no WPF / folder-picker / WebView2; create named workspace + upload local folder still works) plus [stretch-workspace-upload-browser.md](stretch-workspace-upload-browser.md). No issue ticket.

Alan locks applied: labeled links `[label](path)`; Change→Ev / EventId peels where relevant. No GitHub PR for this report. Disposable `cursor/*` only; no staging push.

## Standards

Range `origin/staging...HEAD` is not empty (12 files). Tip `b3065176`. Mechanical-scan lines are clean; remaining hits are documented standards the scanner does not enforce.

**Hard documented violations**

EventId peel ([core-api.md](.agents/rules/core-api.md) EventId serial; Alan lock: carry EventId in-process; peel only at wire/SQL). [WorkspaceCloudUpload.fs](src/Shared/dotnet/WorkspaceCloudUpload.fs) `postStubs` stores `Ok("stub eventId=" + string (EventId.value ack.eventId))`. [stretch-workspace-upload.fsx](scripts/stretch-workspace-upload.fsx) PASS lines peel `EventId.value proof.created.eventId` and `EventId.value proof.state.eventId`. Neither is wire or SQL. `ClientHistory.mintChange` local `event` with `EventBody.Change` is KEEP.

Mutable ([fsharp-source.md](.agents/rules/fsharp-source.md): “Don’t use mutable.”). [WorkspaceCloudUpload.fs](src/Shared/dotnet/WorkspaceCloudUpload.fs) `runWithClient`: `client.Timeout <- TimeSpan.FromMinutes 2.0` mutates a caller-owned `HttpClient`.

**Dismissed**

[AmbitSession.fs](src/Shared/dotnet/AmbitSession.fs) `send` / `loginByGet` `try/with ex -> Error` matches existing Shared.dotnet IO ([ProcessExec.fs](src/Shared/dotnet/ProcessExec.fs), [GitRun.fs](src/Shared/dotnet/GitRun.fs), [WorkspaceDavClient.fs](src/Shared/dotnet/WorkspaceDavClient.fs)); style of that layer wins over a literal Exceptions ban. Report links are `[label](path)` or allowed `[[path]]`; no consecutive blanks ([markdown-writing.md](.agents/rules/markdown-writing.md), [refer-by-name.md](.agents/rules/refer-by-name.md)).

**Judgement smells (not hard)**

Mysterious Name / inverted layer: [AuthToken.fs](src/Server/AuthToken.fs) stays `namespace Gambol.Server` and compiles only via Shared.dotnet `Link` in [Gambol.Shared.DotNet.fsproj](src/Shared/dotnet/Gambol.Shared.DotNet.fsproj). [AmbitSession.fs](src/Shared/dotnet/AmbitSession.fs) is the only Shared file that `open Gambol.Server`. [Gambol.Server.fsproj](src/Server/Gambol.Server.fsproj) and [Gambol.Desktop.fsproj](src/Desktop/Gambol.Desktop.fsproj) dropped their `Compile`. Server now consumes `Gambol.Server.AuthToken` through Shared.DotNet.dll.

Divergent Change / Speculative Generality: [WorkspaceCloudUpload.fs](src/Shared/dotnet/WorkspaceCloudUpload.fs) public `parseArgs` defaults `http://127.0.0.1:5215/ambit`, `/tmp/ambit-stretch-upload`, `stretch`; `let clientHint = "stretch-workspace-upload"`. Stretch CLI and identity live in the Shared upload module. Desktop never calls `WorkspaceCloudUpload` ([core-agent-behavior.md](.agents/rules/core-agent-behavior.md): no extra flexibility).

Middle Man: [LocalProxy.fs](src/Desktop/LocalProxy.fs) `let private createHttpClient () = AmbitSession.createHttpClient ()`. [WorkspaceSyncEndpoints.fs](src/Desktop/WorkspaceSyncEndpoints.fs) `cookieHeader` is only `AmbitSession.cookieHeader creds`. `LocalProxy.start` already calls `AmbitSession.cookieHeader` direct.

Feature Envy: same Desktop hunks still call `AuthToken.proxyCookieHeader` / `AuthToken.applySetCookieHeaders` while cookie/http helpers moved to AmbitSession. Cookie work stays split.

## Spec

Range `origin/staging...HEAD` is non-empty (12 files, tip `b3065176`). Spec is Alan’s reshape ask plus [stretch-workspace-upload-browser.md](stretch-workspace-upload-browser.md). Live harness not run.

**(a) Missing or partial**

- “Reshape stretch harness as thin `.fsx` / FSI that **reuses Desktop non-UI code**” / report: “Desktop non-UI and the stretch script call the same modules.” Desktop never calls [WorkspaceCloudUpload](src/Shared/dotnet/WorkspaceCloudUpload.fs). [WorkspaceSyncEndpoints](src/Desktop/WorkspaceSyncEndpoints.fs) and [LocalProxy](src/Desktop/LocalProxy.fs) only use `AmbitSession.cookieHeader` / `createHttpClient`. Create / stub / mark live in Shared.dotnet plus Browser [UpdateWorkspaceSync](src/Client/UpdateWorkspaceSync.fs) (`planCreateWorkspace`, `planStubOps`, `planServerFilePresentOps` + `applyAndPostSync`), not Desktop non-UI.
- “**No replication** between Desktop and the old fat `scripts/stretch-workspace-upload/Program.fs`.” Fat Program.fs is gone, but its create / inventory / stub / login / `/state` / `/changes` body was lifted into Shared, not replaced by a Desktop call. WebDAV is the shared piece.
- Report: “[stretch-workspace-upload.fsx](scripts/stretch-workspace-upload.fsx) only parses argv and prints PASS lines.” The `.fsx` still walks `graph.nodes` and peels `EventId.value`. `parseArgs` and `clientHint = "stretch-workspace-upload"` sit in [WorkspaceCloudUpload](src/Shared/dotnet/WorkspaceCloudUpload.fs).

**(b) Scope creep**

- Ask is reuse Desktop non-UI, not AuthToken inversion. `AuthToken.fs` Compile left Server+Desktop; [Gambol.Shared.DotNet.fsproj](src/Shared/dotnet/Gambol.Shared.DotNet.fsproj) now compiles it. Server gets `Gambol.Server.AuthToken` from Shared.DotNet.dll. Not required.
- Harness identity in the “shared” module. Stretch defaults (`/tmp/ambit-stretch-upload`, label `stretch`) and `clientHint` belong in the `.fsx`. Tests lock those defaults in [WorkspaceCloudUploadTests](tests/Shared.Tests/WorkspaceCloudUploadTests.fs).

**(c) Looks implemented, looks wrong**

- “Same upload path as `/_desktop/workspace-push` (`WorkspaceFileSync.post`).” `pushMapped` does call `WorkspaceFileSync.post` with the same six args as `handlePush` (`client`, `ambitBase`, `mappedRoot`, `scope`, cookie, `clientHint`). That part matches.
- Module comment: “Same create / inventory / stub / push / mark path Desktop non-UI uses.” Desktop non-UI does not run create / stub / mark. Client does, via a different post path (`applyAndPostSync` vs `AmbitSession.postOps`).
- Report: “After mark, `GET /state?scope=full` `eventId=3`.” `pushAndMark` GETs after push, then marks, then returns that pre-mark `state2`. Printed `graph-eventId` / nodes are not a post-mark GET.
- No WPF / picker / WebView2 in the harness: holds. Live PASS not judged.

## Summary

Standards: 3 hard findings (2 EventId.value peels, 1 mutable `HttpClient.Timeout`), 4 judgement smells (AuthToken inverted layer, stretch CLI in Shared, Desktop Middle Man wrappers, split cookie Feature Envy); worst is EventId.value peel in [WorkspaceCloudUpload.fs](src/Shared/dotnet/WorkspaceCloudUpload.fs) `postStubs`.

Spec: 7 findings (3 missing/partial, 2 creep, 2 looks-wrong) plus live harness not run; worst is create+stub+mark not Desktop-shared, with AuthToken compile inversion as the major layering correction.
