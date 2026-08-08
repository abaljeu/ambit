# Node-marker tip — grill notes (round 2)

Depends on: [[grill-notes.md]], [[open-questions.md]] (round 1). User has not answered. Nothing locked.

## Round-1 self-contradictions to force

1. **Job vs vehicle**: Round 1 leans debug/identity (Guid for logs) *and* native `title`. Native `title` is not selectable / not paste-friendly. Those two recommendations fight. Either the job is "glance stamps" (title OK) or the job is "copy Guid into a log" (needs click-to-copy or custom overlay).

2. **Ban vs desktop block**: Round 1 bans duplicating `.amb-file-indicator` shortLabel, then proposes putting a status label on the tip. Those are *different* status families (see below) — but the user must still pick one home for sync-ish text so the tip does not become a second chrome strip.

3. **"Desktop when available" understatement**: Round 1 framed stamps as App-only. Code also fetches **server** file-status for `//label/...` workspace refs (`RequestServerFileStatus` → `/{file}/file-status`), and that response can carry `sourceModifiedUtc` (DataDir mtime). "Available" is not App-host ⊕ matching active node — it is whichever status path last filled `VM.desktopFileIndicator`.

## Three clocks (do not collapse)

| Clock | Source | After artifact persist |
|-------|--------|------------------------|
| **Update Time** | `Node.updateTime` | Often **Server DataDir mtime** via `withStamp` |
| **Source modified** | `sourceModifiedUtc` on status response | App disk **or** Server DataDir depending on which endpoint ran |
| **Derived sync word** | `FileSyncIndicator` current / old / edited | Comparison of the two above; **computed in Shared, not rendered in DOM** |

Saying one UI word "TimeStamp" for any two of these is a lie. After persist, Update Time and Server `sourceModifiedUtc` can be the *same kind* of clock (DataDir), which makes a dual display either redundant or a subtle drift detector — user must choose which.

## Two status UIs already latent

| Surface | What it is today | Visible? |
|---------|------------------|----------|
| `.amb-file-indicator` + native `title` | `WorkspacePathSyncStatus` glyph + `shortLabel` | Yes |
| `VM.desktopFileIndicator` → `desktopFileIndicatorText` | file/folder/create/**current/old/edited**/… | Text computed, **not in DOM** |

Putting "desktop file info" on the Node marker tip without deciding the orphaned `desktopFileIndicatorText` either **adopts that orphan** or **bypasses it** with a raw UTC dump. That is a product fork, not a formatting detail.

## Hollow ambiguity (debug tip stress test)

`HollowCircle` = Unloaded **or** Unparsed ([[src/Shared/ViewModelChildrenIndicator.fs]]). Guid + Update Time alone do not disambiguate why the circle is hollow. If the tip's job is debug/identity for "why does this look empty?", v1 floor fails that job. If the tip's job is only Guid+stamp for logs, hollow ambiguity is out of scope — say so explicitly.

## Naming stress

- Glossary: Node ≠ bullet ([[CONTEXT.md]]).
- Code: `leafBullet` → proposed `nodeBullet`; CSS still `amb-leaf-*`.
- Class lie: `.amb-node-guid` shows Filename, not Guid — tooltip Guid makes the lie louder unless scoped as separate debt.

## Evidence deltas vs round 1

- Server file-status can supply `sourceModifiedUtc` ([[src/Server/DocumentPersistence.fs]], [[doc/current/desktop-local-files.md]] status section is App-shaped; workspace path uses server path in Browser code).
- `updateTime` meaning after persist is DataDir mtime ([[src/Shared/Model.fs]] `NodeUpdateTime.withStamp` comment) — same family as server source stamp.
- No custom outline tooltip component; native `title` only ([[grill-notes.md]]).
- `doc/arch.md` Node section still omits `updateTime` / residency fields relative to Model.
