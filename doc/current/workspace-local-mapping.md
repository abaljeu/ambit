# Workspace local mapping

Category: Desktop
See also: [[doc/current/desktop-local-files.md]], [[doc/current/workspace-graph.md]]

Implemented baseline for desktop-local workspace label → filesystem root bindings. Shared module;
the desktop layer supplies the config file path and Get/Put / folder-picker HTTP endpoints.

## Purpose

A workspace label such as `home` is shared graph identity (`//home`). Each desktop may map that
label to an absolute local directory root. Mapping is local-only and does not alter the cloud graph.

## Config file

Path (desktop): `%LocalAppData%/Gambol/config.json`

Loaded at `LocalProxy` startup via `WorkspaceLocalMapping.loadFromFile`. Missing file → empty
mapping set.

## JSON format

```json
{
  "workspaceMappings": [
    { "label": "home", "path": "D:\\dev\\myproject" },
    { "label": "docs", "path": "C:\\Users\\me\\Documents\\gambol" }
  ]
}
```

- `workspaceMappings` — optional array; omitted or empty → no mappings.
- `label` — non-empty, trimmed; case-insensitive uniqueness enforced at decode.
- `path` — non-empty, **fully qualified** absolute path.

Decode errors: `malformed_json`, `duplicate_workspace`, `invalid_workspace`, `invalid_path`,
`mapping_read_failed`.

## Label validation

Rejected when label is empty or contains:

- Invalid filename characters (`Path.GetInvalidFileNameChars()`)
- `/` or `\`

## Root path validation

Rejected when path is empty or not `Path.IsPathFullyQualified`.

## Relative path resolution

`WorkspaceLocalMapping.resolvePath` maps a workspace label plus relative path to an absolute path under the mapped
root. Callers obtain the label and relative path from `NodeDesktopPath.tryParseWorkspacePath` on a `//label/relative` reference.

Rules:

- Unknown label → `invalid_workspace`
- Empty relative path → workspace root
- Forward-slash separators only in relative path
- No `..`, no empty segments
- No `:`, `#`, `^`, or other invalid filename chars in segments
- Resolved path must stay under mapped root (`path_escape` if not)

Plain paths (not `//label/...` workspace form) are resolved relative to `Environment.CurrentDirectory` in
`LocalProxy` — see [[doc/current/desktop-local-files.md]].

## Runtime use

`LocalProxy` holds the decoded map in memory for the process lifetime (mutable; updated on Put). Used by:

- `POST /_desktop/file-status`
- `GET /_desktop/file` (import)
- `POST /_desktop/file` (export)
- `GET` / `PUT /_desktop/workspace-mappings`
- Desktop git endpoints that resolve a mapped label

Requires `workspacePaths` capability when using `//label/...` form.

## API (G6)

| Method | Path | Body / notes |
| --- | --- | --- |
| `GET` | `/_desktop/workspace-mappings` | Returns `{ "workspaceMappings": [ { "label", "path" }, … ] }` |
| `PUT` | `/_desktop/workspace-mappings` | Upsert one `{ "label", "path" }`, **or** full replace with the same document shape as the config file. Persists to disk and updates the in-memory map. |
| `POST` | `/_desktop/pick-folder` | Optional `{ "requireGit": true }`. OS folder dialog. `{ "cancelled": true }` or `{ "cancelled": false, "path", "gitRoot" }` (`gitRoot` may be `null` unless `requireGit`). |
| `POST` | `/_desktop/detect-git` | `{ "path" }` → `{ "gitRoot" }` or error `not_a_git_repo` / `invalid_path`. |

## Tests

`tests/Shared.Tests/WorkspaceLocalMappingTests.fs` — decode, duplicate labels, path escape, segment validation, happy-path resolution, encode round-trip, upsert, `tryGitRoot`.

## Not implemented

- Startup sync of local labels to cloud workspace nodes (§4b in stage plan).
- Automatic initial pull after Connect (user runs Download).
- Persistent status chrome beyond `#cmd-last-result` / console.
