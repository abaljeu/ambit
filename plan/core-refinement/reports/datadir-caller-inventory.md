# DataDir caller inventory

Updated: 2026-09-29

Write-once snapshot of Server DataDir use for [[arch.md]] §3 step 4 Path control. Living rule stays on that step. Do not edit this report after write; revise living plan text instead.

## 1. How DataDir is born

There is no F# const. Config key `"DataDir"` in [[src/Server/appsettings.json]] is `"../../data"`. `DataDir.resolve` in [[src/Server/DataDir.fs]] makes one absolute trailing-sep path at boot via `Server.resolveDataDir`, stored on `AmbitApp.DataDir`, `CoreBoot.DataDir`, `PersistenceContext.DataDir`.

Counts (research grain, not a migrate batch list): 98 Server `(dataDir: string)` bindings; 26 Server files; Shared/Client comments only. Tests inject a temp dir via the same config key (177 `let dataDir = newTempDir ()` fixtures) — not separate product call sites.

## 2. Absolute builders

Symbols that build absolute paths under DataDir:

1. **DocumentPersistPath** — `resolveUnderDataDir`, `workspaceRootFor`, `resolveArtifactPath` (main combine + escape check)
2. **Bookkeeping**, **EventLogFile**, **HttpResponseLog**, **DailyGitSave** — `SYSTEM/…`
3. **GitGateway.resolveWorkspaceRoot** — `DataDir/{label}`
4. **WorkspaceWebDav**, **LazyLoadReconciliationServer**, **IgnoredDestination**, **GitSave** — workspace root, work tree, or `.git`
5. **RouteAppShell.serveUserCss** — `SYSTEM/user.css`

## 3. Pass-through of the absolute root

Holders that receive and forward the absolute DataDir root (relative identity stays in the graph; absolute appears when Server adds `dataDir`):

1. **RouteRegistration**, **CoreRuntime**, **FileAgent**, **DbAgent**, **CoreMailbox**, **Api**
2. **LoadSaveRouting**, **DocumentPersistWrite**, **DocumentPersistChange**, **DocumentLoader**, **DatabaseSetup**, **GithubTransportActor**

## 4. Unclassified

1. **SavePrep.syncDataDir** — takes `dataDir` and does not use it
2. **GitSave** `dataDir` parameter — often a workspace root (`DataDir/{label}`), not the data-directory root
3. **DbAgent.liveSaveDataDir** — sometimes only an error-string label

## 5. Absolute roots that are not this DataDir

List apart; do not mix into DataDir migrate classification:

1. Content root and `wwwroot`
2. Temp paths
3. Desktop ledger `LocalApplicationData/Gambol` (`gambolAppDataDir`)

Workspace git dirs are under Server DataDir.
