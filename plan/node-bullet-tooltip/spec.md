# Bullet tip — spec

Status: ready-for-agent

Synthesis of [[plan/node-bullet-tooltip/grill-notes-round-3.md]] (locked answers R3 + parent
frontier P-Q1…P-Q4). Time model beyond Update Time is deferred to the parked
[[plan/bullet-tip-times/map.md]].

## Problem Statement

A Node occurrence shows a **Bullet** (the left-edge glyph: fold chevron, solid circle, or hollow
circle). Several facts that identify a Node and explain its glyph are not visible anywhere on the
row: the Node's Guid, why a circle is hollow (Unloaded vs Unparsed), where the file lives in the
workspace, when the Node last changed, and which CSS classes it carries. Today a user diagnosing an
outline ("which Node is this in the logs?", "why is this empty?", "where does this map on disk?")
has no in-app way to see them.

## Solution

Hovering the Bullet reveals an always-on inspector tip listing exactly the applicable non-obvious
facts for that Node, one per line, in a fixed order. Each line appears only when its fact applies;
absent facts are omitted entirely (no label, no `N/A`). The tip is identical across chevron, solid,
and hollow Bullets, is passive (it changes no click behavior), and reads times in the viewer's local
timezone. It is delivered as a native HTML `title`, so it is a plain glance aid, not a copyable or
interactive surface.

## User Stories

1. As an outliner, I want to hover any Bullet and see the Node's short Guid tail, so that I can
   correlate the row with a Guid that appears in a log message.
2. As an outliner, I want the hover tip on a hollow Bullet to spell out the Node's residency
   (documentState and childrenStatus), so that I can tell whether the circle is hollow because it is
   Unloaded or because it is Unparsed.
3. As an outliner, I want the same residency lines on a solid Bullet, so that I can confirm a leaf is
   Loaded and Parsed without guessing from the glyph.
4. As an outliner, I want the hover tip on a Bullet whose Node maps to a workspace file to show the
   `//label/relative` workspace path, so that I know where the file lives without opening anything.
5. As an outliner, I want the hover tip to show the Node's Update Time in my local timezone, so that
   I can see when the Node last changed without mental UTC conversion.
6. As an outliner, I want the hover tip to list the Node's own CSS classes last, so that I can see
   which styling classes are applied when debugging appearance.
7. As an outliner, I want a Bullet with no applicable extra facts to still show at least the Guid tail
   and Update Time, so that every Node is at minimum identifiable.
8. As an outliner in a browser-only or unmapped session, I want the tip to omit the workspace path
   line gracefully, so that I am never shown a broken or empty path.
9. As an outliner, I want the tip content to be identical whether the Bullet is a chevron, a solid
   circle, or a hollow circle, so that hovering behaves predictably regardless of the glyph.
10. As an outliner, I want hovering the Bullet to never change what a click, double-click, fold, or
    zoom does, so that the tip stays a passive inspector.
11. As an outliner, I want the tip lines in a stable order (Guid tail → residency → workspace path →
    Update Time → CSS classes), so that I always look in the same place for a fact.
12. As an outliner, I want the tip to omit a fact rather than repeat something already obvious on the
    row, so that it stays a compact list of the non-obvious.
13. As a developer reading the code, I want the Bullet binding named `nodeBullet` (not `leafBullet`),
    so that the identifier is honest for chevron cases too.
14. As a developer reading the code, I want the Bullet's CSS classes renamed from `amb-leaf-dot` /
    `amb-leaf-hollow` to `amb-bullet-dot` / `amb-bullet-hollow`, so that the styling hooks match the
    Bullet vocabulary and no longer imply a "leaf".


## Implementation Decisions

- **Single Shared seam.** Add one pure formatter in `ViewModelRowState` (Shared) that assembles the
  tip string from the model and Node. Signature shape:

  ```fsharp
  // Shared. `formatLocal` renders a UTC DateTime in the viewer's local zone.
  // Returns "" when no lines apply (caller omits the title attribute).
  let bulletTip (formatLocal: System.DateTime -> string) (model: VM) (node: Node) : string
  ```

  The `formatLocal` function is injected because local-timezone rendering needs the browser
  (`Intl.DateTimeFormat`), which is not available in pure Shared. Tests inject a deterministic stub;
  the Client injects the real Intl-based formatter.

- **Line assembly and gating.** `bulletTip` produces a `\n`-joined string. Each line is produced by
  a self-gating step; a step that has no fact contributes nothing. Fixed order:
  1. **Guid tail** — `NodeId.GuidTail8 node.id.Value` (always present).
  2. **Residency** — text from `node.documentState` (`Current` / `Unparsed` / `NoServerFile`) and
     `node.childrenStatus` (`Loaded` / `Unloaded`). Rendered as text so a hollow Bullet is
     disambiguated. Present whenever the Node is loaded (i.e. always for a rendered row).
  3. **Workspace path** — `NodeDesktopPath.pathForNodeId model.graph node.id` when it resolves to a
     `//label/relative` path; omitted otherwise (browser-only / unmapped / non-file Nodes).
  4. **Local path** (post-spec addition) — the desktop mapping's local root joined with the relative
     tail (e.g. `//life/note.md` → `d:\life\note.md`), shown *in addition to* the workspace-path line
     when the Node's workspace label has a local root mapping; omitted when there is no mapping
     (browser-only / unmapped). Sourced from a new `VM.workspaceRoots` (lowercased label → root),
     populated from `/_desktop/workspace-mappings` alongside `workspaceMappedLabels`. Display-only
     string join — never `WorkspaceLocalMapping.resolvePath` (that is desktop-only, `#if !FABLE`).
  5. **Update Time** — `formatLocal node.updateTime`, always present. This is the only clock in v1.
  6. **CSS classes** — `CssClass.toList node.cssClasses` joined; omitted when the Node has no classes.
     The Node's own classes only, never the assembled row `className`.

- **Client attachment.** In `RowView.Layout.buildRowElement`, rename the local binding `leafBullet`
  to `nodeBullet` (all three glyph branches, the downstream returns, and `Behavior.wireRow` /
  `wireSelectingActivate` parameters). Set `nodeBullet.setAttribute("title", tip)` for every glyph
  variant when `tip` is non-empty; do not set the attribute when it is empty. No change to any
  listener wiring — the tip is presentation only.

- **Local-time formatter.** Add a Client helper (Fable `Intl.DateTimeFormat`, browser default zone)
  that renders a UTC `DateTime`. This is net-new; no client-local formatter exists today
  (`JsInterop.fs` has only a hardcoded ET/epoch helper). Format precision is date + time to the
  minute; adjust only if a resolved bullet-tip-times decision supersedes it.

- **Naming.** "Bullet" is the glyph element (already promoted to [[CONTEXT.md]]); a Node is not a
  Bullet. Spoken/doc name for the disclosure is **Bullet tip**. The binding rename is code-only.

- **CSS class rename.** Rename `amb-leaf-dot` → `amb-bullet-dot` and `amb-leaf-hollow` →
  `amb-bullet-hollow` at both the class-adding call sites in `RowView.Layout.buildRowElement` and the
  rule definitions in `src/Server/wwwroot/style.css` (lines ~229–242), together in one change so no
  rule is orphaned. Purely cosmetic naming; no styling behavior changes.

- **Known deferred debt (do not fix here).** The misleading `.amb-node-guid` span (which shows the
  Filename, not a Guid) stays as-is. Implementers MUST NOT "fix" the class lie by stuffing the Guid
  into the name span; the Guid lives only in the tip.

## Testing Decisions

- **What makes a good test.** Assert the external contract of `bulletTip`: given a `VM` + `Node`
  (and a stub `formatLocal`), the returned string contains exactly the expected lines, in order, with
  absent facts omitted. Do not assert DOM wiring or private helpers.
- **Module under test.** `ViewModelRowState.bulletTip` in `tests/Shared.Tests`.
- **Cases to cover.**
  - Minimal Node (no workspace path, no CSS classes): lines = Guid tail, residency, Update Time only.
  - Hollow-by-Unloaded vs hollow-by-Unparsed: residency text differs and disambiguates.
  - Loaded + Parsed leaf: residency reads as Loaded/Current.
  - Node with a resolvable `//label/relative` path: path line present between residency and Update
    Time.
  - Node with CSS classes: class line present and last.
  - Line order stable across a chevron (has children) vs a leaf Node.
  - `formatLocal` stub is invoked with `node.updateTime` and its output appears verbatim.
- **Prior art.** `tests/Shared.Tests/ViewModelRowStateTests.fs` and `ViewModelTests.fs` already test
  Shared view-model projections; follow their fixture and Node-construction style. `HistoryTests.fs`
  demonstrates `NodeId.GuidTail8` assertions.

## Out of Scope

- **The full time model.** Workspace file time, Server file time, and Last sync (ledger-derived) are
  parked in [[plan/bullet-tip-times/map.md]] (T-Q1…T-Q4: de-dup tolerance, tz precision/format,
  whether a real last-sync timestamp is wanted, multi-clock ordering). v1 shows Update Time only.
- **Active file-status clock.** `VM.desktopFileIndicator.sourceModifiedUtc` and the orphaned
  `desktopFileIndicatorText` (current/old/edited) are not shown on the tip.
- **Copy / interactive tip.** Native `title` only; no click-to-copy, no custom overlay, no selectable
  text. If copy is later needed it is a separate effort.
- **Fetch on hover.** No new fetch, endpoint, or refetch policy; the tip reads only facts already in
  the model.
- **Full Guid.** Excluded. (The desktop mapping's local path — e.g. `d:\life\...` — is now **in
  scope** as a second path line per Implementation Decisions #4; the never-shown item is the full
  `NodeId` Guid and any resolved `%LocalAppData%` server-side mapping.)
- **`.amb-node-guid` class-lie cleanup** (the name span shows the Filename, not a Guid) — later
  ticket, tracked separately.
- **Stale `doc/arch.md` Node section** (lines ~186–207): hand-lists `Node` fields and omits
  `updateTime` / `childrenStatus` / `documentState`. Remedy (later ticket): stop enumerating fields —
  name the `Node` type and point to it in `src/Shared/Model.fs` as the source of truth, so the doc
  cannot drift.
- The `amb-leaf-*` → `amb-bullet-*` rename is now in scope — see story 14.

## Further Notes

- The tip is an always-on inspector for everyone; the workspace path is not treated as a privacy
  concern (R3-Q1). Precise timestamps remain screenshot/screen-share visible — accepted, noted.
- This content choice is reversible and is intentionally **not** a Committed Decision under
  [[doc/Decisions/]].
- The hollow Bullet's Load-on-click behavior is separate work
  ([[plan/selective-client-loading/issues/28-make-hollow-circle-clicks-invoke-load.md]]); the tip
  must not interfere with it.
