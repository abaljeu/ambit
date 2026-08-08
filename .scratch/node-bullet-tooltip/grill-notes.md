# Node-bullet tooltip — grill notes (round 0/1 facts)

Round 2 contradictions and clock analysis: [[grill-notes-round-2.md]]. Interview frontier: [[open-questions.md]] (R2-Q1…R2-Q6).

Feature proposal (not locked): rename `leafBullet` → `nodeBullet`; hover tooltip on that control showing Node facts not otherwise visible on the row; must include Node Guid and TimeStamp; desktop file info when available (`sourceModifiedUtc`).

No decisions locked. Layout-behavior work is out of scope for this session.

## Domain language collisions

From [[CONTEXT.md]]:

- **Node** — avoid: item, bullet, line, row, entry.
- **Browser** / **App** — avoid saying Client / Desktop in speech.
- **Loaded** / **Unloaded** — hollow is presentation, not a glossary synonym for Unloaded.
- **updateTime** is not in the glossary; code field is `Node.updateTime`. User said "TimeStamp" — unresolved synonym.

Spoken "nodeBullet" fights the glossary's ban on calling a Node a bullet. Code identifier `nodeBullet` may still be fine if spoken name differs (e.g. children indicator / node marker).

## What a row already shows

From [[src/Client/RowView.fs]] + [[src/Shared/ViewModelRowState.fs]]:

| Visible | Source |
|---------|--------|
| Outline text | `outlineDisplayText` → `.amb-text` |
| Filename / name token | `rowNameDisplayText node.name` → `.amb-node-guid` (**class name lies**: not a Guid) |
| Workspace path-sync glyph | `rowFileIndicator` → `.amb-file-indicator` text |
| Sync short label | native `title` on `.amb-file-indicator` (`WorkspacePathSyncStatus.shortLabel`) |
| Children shape | `RowChildrenIndicator`: FoldChevron / SolidCircle / HollowCircle |
| Owned vs Ref | row CSS `amb-row-owned` / `amb-row-ref` |
| Kind chrome | special row classes / text border |

## What is not on the row today

| Fact | Where it lives | Notes |
|------|----------------|-------|
| Full `NodeId` Guid | `Node.id` in Shared Graph | Never rendered. `NodeId.GuidTail8` exists for messages. |
| `Node.updateTime` | Shared `Node` | Mutation stamp; after persist for artifacts, DataDir mtime ([[src/Shared/Model.fs]]). |
| Raw `sourceModifiedUtc` | App `POST /_desktop/file-status` → `DesktopFileStatusResponse` → `FileStatusIndicator` on VM | Only after status fetch; see [[doc/current/desktop-local-files.md]]. |
| Derived current/old/edited | `FileSyncIndicator.indicatorTextForStatus` via `desktopFileIndicatorText` | **Computed + tested, not rendered in Browser DOM.** |
| Absolute / resolved local path | App status path + `NodeDesktopPath` | Not on the left control; may appear elsewhere indirectly. |
| `documentState` | Shared Node | Hollow already encodes Unloaded **or** Unparsed; not spelled out as text. |
| `childrenStatus` | Shared Node | Same. |
| Owner parent `NodeId` | Shared Node.owner | Not shown. |
| Instance / SiteId | SiteMap | Not shown. |

## Children-indicator variants

[[src/Shared/ViewModelChildrenIndicator.fs]]:

- **FoldChevron** — resident children present; fold + deferred Zoom handlers on the control.
- **SolidCircle** — Loaded + Parsed leaf.
- **HollowCircle** — Unloaded **or** Unparsed leaf (distinct facts, shared glyph; Load click still pending issue 28).

Binding today: circle variants also get row activate / double-click when `not hasChildren`. Chevron keeps fold/zoom. Tooltip attachment must not break those handlers.

Local binding name today: `leafBullet` even when the case is FoldChevron — rename to `nodeBullet` is honest for the binding, but "leaf" CSS (`amb-leaf-dot`, `amb-leaf-hollow`) remains.

## Desktop file truth path

1. App exposes `status: true` in capabilities ([[doc/current/desktop-local-files.md]]).
2. Browser requests status for the **active** file reference path.
3. Response may include `sourceModifiedUtc` when the path exists.
4. Stored on `VM.desktopFileIndicator` as `FileStatusIndicator (…, sourceModifiedUtc)`.
5. Comparison to `node.updateTime` yields current / old / edited — **only meaningful when both stamps exist**.

Browser-only Sessions never get `sourceModifiedUtc`. Non-active rows never hold that indicator on the VM (active-only). Hovering a non-active File Node therefore cannot honestly show live disk mtime without a new fetch policy.

## Existing tooltip pattern

Native HTML `title` already used on:

- `.amb-file-indicator` (sync short label)
- command dock buttons
- db-status / cmd-last-result

No custom multi-line tooltip component in outline rows.

## Privacy / disclosure surface

Tooltip content is screenshot- and screen-share-visible. Candidates that raise stakes: full Guids, absolute local paths, precise UTC stamps, workspace labels, Resolved disk paths under `%LocalAppData%` mappings.

## Spec / doc gaps

- No existing `.scratch/node-bullet-tooltip/` spec before this grill.
- No Committed Decision for UI disclosure of Node identity.
- [[doc/arch.md]] Node section omits `updateTime`, `childrenStatus`, `documentState` (stale relative to Model.fs).
