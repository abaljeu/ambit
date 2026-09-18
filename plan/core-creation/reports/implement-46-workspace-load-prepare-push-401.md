# Implement 46 — Workspace Load prepare-push 401

Ticket: [46 — Workspace Load prepare-push 401](../issues/46-workspace-run-prepare-push-401.md). Project Stage stayed `build`. Ticket Status is `coded`. This agent did not commit.

## 1. Verdict

Desktop Load on a mapped Workspace Node now presents the same request cookie family as LocalProxy Graph traffic: server-issued `gambol_auth` wins; else stored user/pass; else development `deriveToken("", "")`. prepare-push no longer goes out with `None` when AuthStore is empty. A missing cookie still refuses at the Adapter. This change does not restore a closed-over server secret.

## 2. Assumptions

1. Alan confirmed the user command was Load, not Run or Ctrl+Enter.
2. [Core creation architecture](../arch.md) does not name this Desktop cookie seam. The work follows the ticket and Alan's host-call inventory, not a Core Actor hop.
3. `AmbitSession.cookieHeader` stays creds-only (`None` when stored login is absent). Workspace-push, workspace-pull, and download do not use that function.
4. Git smart HTTP to `/ambit/git/...` is not a `/_desktop/*` handler. It uses LocalProxy `forward`, which already attaches the same cookie family.

## 3. What changed

1. [AmbitSession.fs](../../../src/Shared/dotnet/AmbitSession.fs) — `requestCookieHeader` maps stored creds plus issued value through `AuthToken.proxyCookieHeader`.
2. [WorkspaceSyncEndpoints.fs](../../../src/Desktop/WorkspaceSyncEndpoints.fs) — workspace-push and workspace-pull pass `Some(requestCookieHeader creds issued)` at request time.
3. [WorkspaceDownloadManager.fs](../../../src/Desktop/WorkspaceDownloadManager.fs) — `getCookieHeader` runs at job time. The cookie is not captured once at Desktop start.
4. [LocalProxy.fs](../../../src/Desktop/LocalProxy.fs) — Graph `forward` and download `liveCookie` call the same helper. `issuedCookie` is passed into Desktop sync handlers.
5. [WorkspaceCloudUploadTests.fs](../../../tests/Shared.Tests/WorkspaceCloudUploadTests.fs) — three Desktop host cookie cases: absent creds, stored creds, server-issued wins.

## 4. Desktop host-call inventory

Every `/_desktop/*` handler and every other Desktop path that can call the Ambit host.

### 4.1 Paths that call the Ambit host

1. **LocalProxy `forward` (not `/_desktop`)** — Browser Graph, login, logout, `/ambit/git/...`, and every other non-desktop proxy target. Uses, provides, and passes `AmbitSession.requestCookieHeader` on each forward. Already correct. DRY change only: `addAuthCookie` now calls the Shared helper instead of building `ProxyCookieInput` inline.
2. **`POST /_desktop/workspace-push`** — Host call: `WorkspaceCloudUpload.push` → `WorkspaceDavClient.preparePush`. Was: `AmbitSession.cookieHeader creds` (`None` with no stored login). Fixed: request-time `requestCookieHeader` with issued cookie.
3. **`POST /_desktop/workspace-pull`** — Host call: `WorkspaceFileSync.get`. Same gap as push. Fixed the same way.
4. **`POST /_desktop/workspace-download`** — Host call is deferred: `WorkspaceDownloadManager.runJob` → `WorkspaceFileSync.getStaged`. Was: cookie captured once at `LocalProxy.start` from `AmbitSession.cookieHeader session.Value`. Fixed: `liveCookie` reads current session and issued cookie when the job runs.

### 4.2 Paths that do not call the Ambit host (left unchanged)

These handlers stay local. They do not use, provide, or pass a host cookie. No omit/freeze gap.

1. **`GET /_desktop/capabilities`** — Local `DesktopGit.isAvailable` JSON only.
2. **`POST /_desktop/file-status`** — Local disk status.
3. **`GET /_desktop/file` and `POST /_desktop/file`** — Local import and export.
4. **`GET /_desktop/workspace-mappings` and `PUT /_desktop/workspace-mappings`** — Local `config.json` map.
5. **`POST /_desktop/pick-folder`** — OS folder dialog.
6. **`POST /_desktop/workspace-inventory`** — Local walk of the mapped folder.
7. **`POST /_desktop/workspace-sync-ledger`** — Local ledger file plus disk mtimes.
8. **`GET /_desktop/workspace-download`** — Local job status. The host call is on the POST/job path in 4.1.4.

### 4.3 Git

1. **DesktopGit** — Local CLI probe (`git` available). Used in capabilities JSON and `.gitignore` filter. No HTTP to the host.
2. **`/ambit/git/{label}.git`** — Host git. Browser/WebView traffic goes through LocalProxy `forward` (4.1.1). There is no `/_desktop/git` handler.

## 5. Tests and compile

1. Red: `requestCookieHeader` missing. Then the Shared function and Desktop wiring.
2. `dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~WorkspaceCloudUploadTests"` — Passed 7.
3. `dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~AuthTokenTests"` — Passed 13.
4. `dotnet build src/Desktop -c Debug` — succeeded, 0 warnings.
5. Client compile gate not run. This ticket did not edit [src/Shared](../../../src/Shared) Fable sources or [src/Client](../../../src/Client). [Gambol.Client.fsproj](../../../src/Client/Gambol.Client.fsproj) does not reference Shared.DotNet.
6. Full suite `./scripts/test.sh all` started in background after coding.

## 6. Standards

Scan: `python .agents/skills/code-review/scripts/standards-scan.py` against `HEAD`. After a line-count shrink, this ticket's files did not trip the over-400 increase rule. `proxyCookieInput` is 8 lines. `requestCookieHeader` is 6 lines.

1. **Hard: none on this ticket's files.** Scan still prints [Database.fs](../../../src/Server/Database.fs) `tryLoadGraphFromProjection` at 62 lines. That file is unrelated dirty tree. This ticket did not edit it.
2. **Judgement: Data Clump** — stored creds plus issued value already live in `AuthToken.ProxyCookieInput`. The new helper is the Credentials-shaped door to that clump. It is not a second cookie algorithm.
3. **Judgement: Middle Man** — `requestCookieHeader` delegates to `proxyCookieHeader`. That is the Shared seam so Graph, push, pull, and download cannot drift.
4. **Smell skipped:** [LocalProxy.fs](../../../src/Desktop/LocalProxy.fs) and [WorkspaceSyncEndpoints.fs](../../../src/Desktop/WorkspaceSyncEndpoints.fs) were already over 400 lines. The ticket did not split them. Net line count on the sync endpoints went down.

## 7. Spec

Spec source: [46 — Workspace Load prepare-push 401](../issues/46-workspace-run-prepare-push-401.md), Alan's Load confirmation, and the Desktop host-call inventory.

1. **Present:** Load wording on the ticket. Same cookie family as LocalProxy. Push, pull, and download fixed. No closed-over server secret. Tests for absent creds, stored creds, and server-issued. Inventory recorded here.
2. **Missing: none** for the coded target. Manual Desktop Load against a live host is still the QA proof.
3. **Scope creep: none.** Local-only `/_desktop/*` handlers were not given cookies. `AmbitSession.cookieHeader` was not widened.

## 8. Next step

Alan reviews [46 — Workspace Load prepare-push 401](../issues/46-workspace-run-prepare-push-401.md). Do not set `done` until that review. Do not commit until Alan asks. Confirm Load on a mapped Workspace Node in Development (no login) and after login.

## 9. Summary counts

1. **Standards:** 0 hard findings on ticket files. Worst judgement: leftover over-400 Desktop files, not split.
2. **Spec:** 0 missing coded requirements. Worst residual: live Desktop Load still needs a human pass.
