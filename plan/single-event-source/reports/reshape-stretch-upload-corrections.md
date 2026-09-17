# Corrections — reshape stretch workspace upload

Date: 2026-09-17. Implements the major findings in [code-review-reshape-stretch-upload.md](code-review-reshape-stretch-upload.md). Tip is this corrections branch, based on the implement tip that the review named.

## Major finding → fix

1. **AuthToken layering.** [AuthToken.fs](src/Shared/dotnet/AuthToken.fs) lives in Shared.dotnet as `namespace Gambol.Shared`. Cookie derive/parse, Desktop `proxyCookieHeader`, and Server `UploadCapability` are one published Shared.dotnet API. Server and Desktop consume that API; they do not compile a `Gambol.Server` type out of Shared.DotNet.dll. [AmbitSession.fs](src/Shared/dotnet/AmbitSession.fs) and [LocalProxy.fs](src/Desktop/LocalProxy.fs) call the same `AuthToken` helpers.
2. **Desktop non-UI reuse.** [WorkspaceCloudUpload.fs](src/Shared/dotnet/WorkspaceCloudUpload.fs) is the Desktop non-UI create / inventory / stub / push / mark path. [WorkspaceSyncEndpoints.fs](src/Desktop/WorkspaceSyncEndpoints.fs) `handlePush` calls `WorkspaceCloudUpload.push` (same six arguments as `/_desktop/workspace-push`). `handleInventory` calls `WorkspaceCloudUpload.listForUpload`. Stretch [stretch-workspace-upload.fsx](scripts/stretch-workspace-upload.fsx) calls `WorkspaceCloudUpload.run`, which uses those same helpers plus `createWorkspace` / `postStubs` / `markBodiesPresent`. Client [UpdateWorkspaceSync.fs](src/Client/UpdateWorkspaceSync.fs) stays on the Fable `applyAndPostSync` path.
3. **Stretch CLI out of Shared.** `parseArgs` defaults and `clientHint = "stretch-workspace-upload"` sit in the `.fsx`. `WorkspaceCloudUploadArgs.clientHint` is a caller-supplied field.
4. **pushAndMark proof state.** After mark, the harness GETs `/state?scope=full` again. Printed `graph-eventId` and nodes are that post-mark state.

## Standards (cheap)

- PASS lines use `EventId.display` at the print edge. `postStubs` no longer peels `EventId.value` into a string.
- `AmbitSession.createHttpClient` sets `Timeout` in the constructor. `runWithClient` does not mutate a caller-owned client.

## Proof

`scripts/stretch-workspace-upload.sh` against local `:5215` PASSed create + upload. Label `stretch-corr-6269`. `graph-eventId=3` is the post-mark GET (`create=1`, `stubs posted`, `mark` → event 3). Nodes include `workspace:stretch-corr-6269` and `file:hello.md`.
