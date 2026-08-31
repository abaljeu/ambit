# Provisional domain language — node marker tip

Not locked. Do not merge into [[CONTEXT.md]] until the user confirms.
Round-2 revisions marked below.

**Round-3 update**: "Node marker" is REJECTED in favour of **Bullet** (the glyph element every Node
view shows). Bullet is now in [[CONTEXT.md]]. Time vocabulary (Update Time / Workspace file time /
Server file time / Last sync) moved to [[plan/bullet-tip-times/time-requirements.md]].

## Candidate terms

**Node marker**:
The left-edge outline control that presents FoldChevron, SolidCircle, or HollowCircle for a Node occurrence.
_Avoid (spoken)_: bullet, leafBullet, nodeBullet, fold toggle (when the control is a circle)

**Children indicator**:
Already in code (`RowChildrenIndicator`); the classification of what the Node marker shows.
_Avoid_: leaf state, residency icon (as synonyms)

**Node identity tip** *(provisional; name depends on R2-Q1)*:
Hover disclosure on the Node marker. Round-2 default recommendation: glance facts not shown as row text — Node Guid and Update Time; optional Source modified when status matches.
_Avoid_: tooltip (as a domain noun), node info, inspector, sync tip (unless R2-Q1 = C)

**Update Time**:
The Node's `updateTime` stamp (mutation time; for artifacts after server persist, often DataDir mtime).
_Avoid_: TimeStamp, timestamp (unqualified), modified time (ambiguous with disk), Source modified

**Source modified**:
Optional filesystem (or DataDir) mtime from the file-status payload's `sourceModifiedUtc` — App disk **or** Server DataDir depending on which status endpoint filled `VM.desktopFileIndicator`.
_Avoid_: updateTime, desktop time, file time (unqualified), "the TimeStamp"

**File status word** *(not tip vocabulary unless R2-Q1 = C)*:
Labels from `DesktopFileStatus` / `FileSyncIndicator` (file, folder, create, current, old, edited, …). Today computed as `desktopFileIndicatorText`, not shown in DOM. Distinct from WorkspacePathSync `shortLabel` on `.amb-file-indicator`.

## Open renames (code vs speech)

- Code binding `leafBullet` → `nodeBullet` may proceed without promoting "bullet" into the glossary.
- CSS `amb-leaf-*` and class `amb-node-guid` (Filename display) are separate naming debt — louder once Guid appears only on the tip.

## Round-2 language risks (still unresolved)

- If R2-Q1 = B or C, "Node identity tip" is the wrong noun — rename when the job locks.
- Do not write Source modified into CONTEXT as "desktop-only"; evidence shows server file-status can supply the same field.
