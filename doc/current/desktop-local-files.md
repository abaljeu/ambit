# Desktop local files

Category: Capability

See Also
[Workspace local mapping](workspace-local-mapping.md)
Label-to-path bindings.
[Workspace graph](graph.md)
[Workspace file sync](workspace-file-sync.md)
Scoped WebDAV Upload and Download.
[Gambol.Desktop](gambol-desktop.md)
[Gambol.Client](gambol-client.md)
[Gambol.Shared](gambol-shared.md)
[Workspace WebDAV](doc/roadmap/workspace-webdav.md)

Server DAV leftover.

The desktop host is WPF WebView2 plus a local HTTP proxy in front of the cloud API.

## Job

[x] The cloud server remains authoritative for the graph.
[x] The desktop adds loopback-only filesystem access.
[x] UI: Gambol.Desktop in [Desktop.fs](src/Desktop/Desktop.fs). WebView2 loads the local proxy URL.
[x] The proxy in [LocalProxy.fs](src/Desktop/LocalProxy.fs) forwards `/ambit/*` and static assets to the configured cloud base URL.
[x] The proxy handles `/_desktop/*` on the local host.
[x] `AuthStore.fs` stores the cloud session cookie so the proxied app is authenticated.
[x] Target URL resolution uses `--local`, `--cloud`, `--target <url>`, or `GAMBOL_TARGET_URL`.

## Capabilities

[x] `GET /_desktop/capabilities` returns JSON for capability discovery. [DesktopCapabilities.fs](src/Shared/DesktopCapabilities.fs) decodes that JSON.
[x] The enabled desktop shape is `file.open` false, `file.import` true, `file.export` true, `file.status` true, `file.workspacePaths` true, and `git.git` true.
[x] `open`: launch a file with the default application.
[x] `import`: read a local file into the graph through the import command.
[x] `export`: write owned children to a local file.
[x] `status`: query path status for the file-reference indicator.
[x] `workspacePaths`: resolve a `//label/relative` path through local workspace mapping.
[x] `git.git`: the host has the ignore-filter binary on PATH. Upload ignore uses `check-ignore`. Pack transport does not use this flag.
[x] On the web client, the capabilities request fails. The client treats every flag as disabled.
[x] The host does not open a file or a workspace root in the system explorer.

## Endpoints

[x] `POST /_desktop/workspace-inventory` returns a local scoped inventory.
[x] Clients use `/_desktop/file`.

## File status

[x] `POST /_desktop/file-status` takes `{ "path": "..." }`.
[x] Response: `{ "path": "...", "status": "invalid" | "create" | "file" | "folder" }`.
[x] The response may include `sourceModifiedUtc` when the path exists on disk.
[x] The client requests status for the active-row indicator when the active row has a valid `[[path]]` or a workspace path reference, and the `status` capability is enabled.
[x] Indicator text: `...`, `invalid`, `create`, `file`, or `folder`.

## File read

[x] `GET /_desktop/file?path=<url-encoded-path>` reads a local file or a directory listing for import.
[x] A file returns `DesktopImportPackage` JSON. The client applies that package through normal cloud sync.
[x] A directory returns a synthetic listing, one `[[name]]` line per entry, as an import package with `isDirectory: true`.

## File write

[x] `POST /_desktop/file` takes `{ "path": "...", "content": "..." }`.
[x] The write sends tab-indented child text to a local file.
[x] The write rejects a directory.
[x] Response: `{ "path": "..." }`.

## Path forms

[x] [LocalProxy.fs](src/Desktop/LocalProxy.fs) resolves a path with the process current directory and the workspace mapping.
[x] A wikilink relative path has a form such as `note.txt` from `[[note.txt]]`.
[x] An absolute path has a form such as `D:\projects\doc.md`.
[x] A workspace-relative path has a form such as `//home/src/lib.fs`.

## Commands

[x] The command palette registers the commands in [Commands.fs](src/Client/Commands.fs).
[x] Import reads the local file at the focus row's first file reference and replaces that node's children. The command uses `UpdateImport.fs` and `GET /_desktop/file`.
[x] Export serializes the owned children of the focus row to the local file at its file reference. The command uses `UpdateExport.fs` and `POST /_desktop/file`.
[x] Ignore filtering requires the `git.git` capability.
[x] Results appear in `#cmd-last-result`.
[x] The palette has no standalone Map, Connect, Clone, pack Push, or Status command.
[x] Import and Export require the matching capabilities `import` and `export`.
[x] Import and Export are blocked during the command palette, the search dialog, and the CSS-class prompt.

## Config

[x] WebView2 user data lives in `%LocalAppData%/Gambol/WebView2`.
