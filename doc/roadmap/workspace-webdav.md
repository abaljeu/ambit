# Server WebDAV (workspace file sync transport)

Category: Sync
Status: Partial
See also: [[workspace-file-sync]], [[workspaces-checklist]], [[doc/arch]], [[doc/current/workspace-local-mapping]], [[src/Server/IgnoredDestination.fs]], [[src/Server/WorkspaceGit.fs]], [[src/Server/GitSave.fs]]

Server-side WebDAV Class 1 that maps `/ambit/dav/{label}/…` onto `DataDir/{label}/`. This is the **only** Upload / Download transport. Product flow, desktop push/pull functions, and command surface live in [[workspace-file-sync]]; this doc owns the server HTTP surface, listing properties, and **server-side Download inventory filtering**.

## What it gives you

- Authenticated tree browse and transfer under a workspace label without git remotes or smart-HTTP pack transport.
- `PROPFIND` as the **server-side inventory** for Pull: path list under scope, then **DataDir `.gitignore` / check-ignore** reduces that list before the client `GET`s.
- `PROPFIND` listings that expose **datestamps** (not only paths/sizes) so Pull / freshness can compare local and server mtime.
- `GET` / `PUT` / `MKCOL` for scoped Push and Pull.
- Rejected ignored PUTs via `git check-ignore` ([[src/Server/IgnoredDestination.fs]]).
- Finish-commit after a Push batch so server WorkspaceGit advances `HEAD` for Lazy Load reconcile.

## Inventory role on the server

Per [[workspace-file-sync]]:

- **Download inventory source** = this mount’s `PROPFIND` under scope.
- **Download ignore SoT** = server `DataDir/{label}/` ignore rules. Prefer applying check-ignore **when building/filtering `PROPFIND` results** so the multistatus is already the download candidate list.
- **Upload ignore SoT** remains the desktop mapped tree (local walk → check-ignore → `PUT` / `MKCOL`); server still rejects ignored PUT as belt-and-suspenders.

Never expose `.git/` under the DAV mount. Omit ignored paths from `PROPFIND`; reject `PUT` to an ignored destination (same exception for `.gitignore` files themselves as IgnoredDestination).

An exact `.amb` file remains a DAV file so Upload and Download preserve its body. Graph consumers interpret it as the persistence/proxy artifact of the containing Directory, or the Workspace at mount root; DAV inventory must never cause a child File node named `.amb`. Names such as `notes.amb` remain ordinary files.

## What it avoids for now

- WebDAV Class 2, locks, DeltaV, COPY/MOVE, Windows drive mapping.
- `DELETE` (no mirror-delete in v1).
- Client pack transport (`/ambit/git/…` remotes) as the Upload / Download path.
- Serving `.git/` or gitignored paths in listings or downloads.
- A separate auth scheme; use the same `/ambit` session auth as other app routes.
- Redefining Pull ignore from the client’s mapped tree (desktop may optionally re-filter; DataDir rules remain authoritative for Pull).

## URL and Class 1 subset

| Piece | Decision |
| --- | --- |
| Mount | `/ambit/dav/{label}/…` → `DataDir/{label}/…` (verbatim label) |
| Class | WebDAV Class 1 only |
| `PROPFIND` | List collection (Depth `0`, `1`, or `infinity`) — **ignore-filtered inventory** for Pull |
| `GET` | Download file bytes for remaining Pull candidates |
| `PUT` | Upload / overwrite file (create parent dirs as needed, or require `MKCOL` first — pick one in implementation and keep consistent) |
| `MKCOL` | Create directory |
| `DELETE` | Deferred |
| `LOCK` / DeltaV | Out of scope |

## PROPFIND properties (required for clients)

Every `PROPFIND` multistatus response (and any equivalent list response the server may add) **must** expose datestamps for files and collections — not path/size alone. Clients need this for Download inventory and later freshness UI. Entries that fail DataDir check-ignore (or live under `.git/`) must not appear.

| Property | Required | Meaning |
| --- | --- | --- |
| `href` / relative path | **Yes** | Resource path under `/ambit/dav/{label}/…` (or workspace-relative path derived from it) |
| Collection vs file | **Yes** | `DAV:resourcetype` with `DAV:collection` for directories; absent/empty for files |
| `DAV:getlastmodified` | **Yes** | HTTP-date last-modified / filesystem **mtime** of the resource |
| `DAV:getcontentlength` | Optional | Byte length for files; omit or `0` for collections |
| `DAV:getcontenttype` | Optional | MIME if cheap; not required for v1 |

**Datestamp rule:** `getlastmodified` is the authoritative listing timestamp. Map it from the on-disk last-write time of the file or directory. Do not invent a second custom timestamp property unless `getlastmodified` cannot be emitted; if a custom field is added for Fable/desktop convenience, it must equal the same mtime instant.

Depth behavior:

- Depth `0` — properties of the named resource only (still include `getlastmodified`; still subject to ignore / `.git` policy).
- Depth `1` — resource + immediate children (typical scoped Pull).
- Depth `infinity` — full subtree under the scope root (allowed; keep DataDir ignore filtering).

## Finish-commit

WebDAV alone does not commit. After a Push batch, the desktop (or client via desktop proxy) calls an explicit finish endpoint so the server runs WorkspaceGit add/commit ([[src/Server/WorkspaceGit.fs]] / [[src/Server/GitSave.fs]]). Lazy Load then reconciles from the new `HEAD`. Push pipeline and sequence: [[workspace-file-sync]] (local scope → check-ignore → PUT/MKCOL → finish-commit).

## Libraries

Nothing WebDAV-related is in the repo today.

| Option | Plan |
| --- | --- |
| NWebDav | Spike first under `/ambit/dav/{label}/…` |
| Hand-roll Class 1 | Fallback if the library fights ignore filtering or property control |

Either path must emit the required `PROPFIND` properties above and apply DataDir check-ignore when building listings.

## Implementation steps

1. Route mount + label → `DataDir/{label}` resolution; auth same as `/ambit`.
2. `PROPFIND` with required properties + **DataDir check-ignore** / `.git` filtering; Server.Tests assert `getlastmodified` present and ignored paths omitted.
3. `GET` / `PUT` / `MKCOL`; reject ignored PUT; optional `getcontentlength` on listings.
4. Finish-commit endpoint wiring after Push batch.
5. Proxy `/ambit/dav/…` through desktop LocalProxy like other `/ambit` routes.

## Tests

- `PROPFIND` returns `href`, collection vs file, and `getlastmodified` for each entry.
- Ignored paths and `.git` omitted from listings (DataDir check-ignore); ignored PUT rejected.
- `GET` after `PUT` round-trips bytes; `MKCOL` creates a collection visible to `PROPFIND`.
- Finish-commit advances `HEAD` after a Push-shaped batch.

## Success criteria

- Desktop Pull can treat scoped `PROPFIND` as the ignore-reduced inventory (path, type, mtime) and `GET` from that list alone.
- Upload / Download never require `/ambit/git/…` pack transport.
- Server listings never leak `.git` or gitignored trees.
