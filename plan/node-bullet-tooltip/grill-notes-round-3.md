# Node-marker tip — grill notes (round 3)

Depends on: [[grill-notes-round-2.md]]. Facts: [[tmp/node-marker-tip-facts.md]].
User answered R2: **1=A+C** (glance + diagnostic, "info at the moment it's pointed at"),
**2=** four clocks (Node.UpdateTime, Server file time, App workspace file time, sync time), client-local tz,
**3=** also include cssClass + workspace local path, **4=** instant/local/pointed-node only,
**5=A** native `title`, **6=** rename + promote "bullet" + show only applicable info (no label, no N/A).

## The reframe (why R2-Q2/Q4 don't hold as stated)

- The active-only `VM.desktopFileIndicator.sourceModifiedUtc` is ONE slot, filled by whichever
  endpoint (server `/ambit/file-status` OR app `/_desktop/file-status`) ran last, **active node only**,
  needs a fetch. It **cannot** serve a non-active pointed node instantly, and cannot hold server-time
  AND app-time at once. So it is the wrong source for "instant / local / any pointed node."
- The honest instant-local source is the **sync ledger** mirrored as `VM.workspaceSyncFacts`
  (`WorkspaceSyncPathFact { localMtimeUtc; serverMtimeUtc; presence; isDirectory }`), cached per node
  for every mapped-label node in a desktop session after the snapshot ran. Lookup:
  `tryWorkspaceSyncFact model nodeId`.
- Mapping user clocks → real fields:
  - Update Time  = `Node.updateTime` (always; after persist == server DataDir mtime).
  - Workspace file time = `fact.localMtimeUtc` (App local disk).
  - Server file time    = `fact.serverMtimeUtc` (server DataDir) — often == Update Time; and
    `effectiveServerMtime` already prefers `node.updateTime` over it.
  - "Sync time"   = **no distinct timestamp exists**. Ledger has `lastOp` (seed/upload/download),
    a word, not a clock.

## Path correction (R3-Q1)

Only `//label/relative` is derivable client-side (`NodeDesktopPath.pathForNodeId`). The absolute
`%LocalAppData%` path is NOT on the client (desktop-proxy only) → showing it breaks "instant/local".

## Timezone

No client-local formatter exists today (only hardcoded America/Toronto epoch-seconds helper).
Local-tz rendering is net-new (Fable `Intl.DateTimeFormat`, browser default zone).

## Degrade edges

Ledger facts require desktop (WebView2) + mapped label + prior snapshot. Browser-only or unmapped
nodes → only Update Time is available locally.

## Locked (round 3 user answers)

- **R3-Q1 = a**: the tip is an always-on inspector for everyone. User does **not** consider
  "where the file lives on my computer" a privacy concern, so the workspace path is fine to show.
  The content choice is **not** a long-term Committed Decision (reversible) — no record under
  [[doc/Decisions/]].
- **R3-Q4 = a**, sharpened: **Bullet** = the glyph element every Node view shows (chevron / solid /
  hollow circle); a Node is not a Bullet; "leaf" was the false name. Promoted to [[CONTEXT.md]].
  Rename binding `leafBullet` → `nodeBullet`; CSS `amb-leaf-*` and `.amb-node-guid` are later debt.
- **R3-Q5 = yes**: one self-gating template; each line renders only if its fact applies; identical
  across chevron / solid / hollow.
- **R3-Q2 / R3-Q3 (time model)**: tabled to its own project
  [[plan/bullet-tip-times/map.md]] + [[plan/bullet-tip-times/time-requirements.md]].

## Locked (parent frontier P-Q1…P-Q4)

- **P-Q1 = GuidTail8**: show the short Guid tail (`NodeId.GuidTail8`), not the full Guid.
- **P-Q2 = `node.cssClasses` only**: the Node's own CSS classes, not the assembled `row.className`.
- **P-Q3 = yes**: show residency as text — `documentState` and `childrenStatus` — to disambiguate
  the hollow Bullet; self-gated like every line.
- **P-Q4 = line order**: GuidTail8 → residency → workspace `//label/relative` path → times block
  (order per [[plan/bullet-tip-times/time-requirements.md]] T-Q4) → `node.cssClasses` last.
