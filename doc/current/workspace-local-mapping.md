# Workspace local mapping

Category: Capability
See Also
[Desktop local files](desktop-local-files.md)
[Workspace graph](workspace-graph.md)
[Workspace file sync](workspace-file-sync.md)
Tree sync is WebDAV.
[Gambol.Desktop](gambol-desktop.md)
[Gambol.Shared](gambol-shared.md)

Workspace local mapping binds a workspace label to an absolute local directory root on one desktop.

## Job

[x] A workspace label such as `home`: shared graph identity (`//home`).
[x] Each desktop may map that label to an absolute local directory root.
[x] The mapping is local-only. The mapping does not change the cloud graph.
[x] The mapped folder does not need to be a git clone.
[x] The shared module decodes and resolves mappings. The desktop layer supplies the config file path and the Get, Put, and folder-picker HTTP endpoints.
[x] Startup does not sync local labels to cloud workspace nodes.
[x] The first map does not start a Download. The user runs Download.

## Config

[x] Desktop config path: `%LocalAppData%/Gambol/config.json`.
[x] `LocalProxy` loads the file at startup through `WorkspaceLocalMapping.loadFromFile`.
[x] A missing file yields an empty mapping set.

## JSON

[x] The document has an optional `workspaceMappings` array of objects with `label` and `path`.
[x] An omitted or empty `workspaceMappings` array means no mappings.
[x] `label` is non-empty and trimmed. Decode enforces case-insensitive uniqueness.
[x] `path`: a non-empty fully qualified absolute path.
[x] Decode errors: `malformed_json`, `duplicate_workspace`, `invalid_workspace`, `invalid_path`, and `mapping_read_failed`.

## Label

[x] Decode rejects an empty label.
[x] Decode rejects a label that contains an invalid filename character from `Path.GetInvalidFileNameChars()`, or that contains `/` or `\`.

## Root path

[x] Decode rejects an empty path.
[x] Decode rejects a path when `Path.IsPathFullyQualified` is false.

## Resolve

[x] `WorkspaceLocalMapping.resolvePath` maps a workspace label plus a relative path to an absolute path under the mapped root.
[x] The caller gets the label and the relative path from `NodeDesktopPath.tryParseWorkspacePath` on a `//label/relative` reference.
[x] An unknown label returns `invalid_workspace`.
[x] An empty relative path returns the workspace root.
[x] The relative path uses forward-slash separators only.
[x] The relative path has no `..` segment and no empty segment.
[x] A segment has no `:`, `#`, `^`, or other invalid filename character.
[x] The resolved path stays under the mapped root. Otherwise the result is `path_escape`.
[x] `LocalProxy` resolves a plain path, not a `//label/...` path, relative to `Environment.CurrentDirectory`.

## Runtime

[x] `LocalProxy` holds the decoded map in memory for the process lifetime. The map is mutable. Put updates the map.
[x] The map serves `POST /_desktop/file-status`, `GET /_desktop/file` for import, `POST /_desktop/file` for export, `GET` and `PUT /_desktop/workspace-mappings`, and WebDAV Upload and Download inventory and write-back under the mapped root.
[x] A `//label/...` path requires the `workspacePaths` capability.

## API

[x] `GET /_desktop/workspace-mappings` returns `{ "workspaceMappings": [ { "label", "path" }, … ] }`.
[x] `PUT /_desktop/workspace-mappings` upserts one `{ "label", "path" }`, or replaces the document with the same shape as the config file. The Put writes the file and updates the in-memory map.
[x] `POST /_desktop/pick-folder` opens the OS folder dialog. The result is `{ "cancelled": true }` or `{ "cancelled": false, "path": "..." }`.

## Tests

[x] [WorkspaceLocalMappingTests.fs](tests/Shared.Tests/WorkspaceLocalMappingTests.fs) covers decode, duplicate labels, path escape, segment validation, happy-path resolution, encode round-trip, upsert, and `tryGitRoot`.
