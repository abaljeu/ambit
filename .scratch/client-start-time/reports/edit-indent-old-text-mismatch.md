# Edit + indent — `old text does not match`

Date: 2026-08-28
Branch: `w/client-start-time`
Prior: [[stale-view-possibilities.md]], [[free-tier-cold-start-sync.md]]
No implement. No red-capable auto loop yet (DOM + Poll race); HITL below is the Phase 1 loop.

## Verdict

**Distinct edit+indent CAS bug**, often **triggered by** the same Poll/Load apply path as stale-sync, not “idle tab never Polls.” Local reject needs `graph.text ≠ Editing.originalText` and `live ≠ graph` at Tab. Dirty top-level Editing blocks auto-sync ([[src/Client/UpdateHelpers.fs]] `isAutoSyncBlocked`), so a pure remote SetText while the user is typing should not land. The open seam is **Graph advances under an Editing snapshot that never refreshes**, then Tab’s `tryTextCommitOps` emits `SetText(staleOriginal, live)` ([[src/Client/UpdateHelpers.fs]] L241–246; [[src/Client/UpdateMove.fs]] L132–153).

## Ranked mechanisms

1. **Overlay returnTo Editing (most likely).** Palette / search / class / rename wrap `model.mode` and keep Editing as `returnTo` ([[src/Client/CommandPalette.fs]] L12–13). Top-level mode is not `Editing`, so `isAutoSyncBlocked` is false and `adjustModeAfterServerApply` is a no-op ([[src/Client/UpdateHelpers.fs]] L205–237). Poll/Load can change the node’s text; close overlay restores stale `originalText`; user types or RecreateRow seeds `#edit-input` from that snapshot ([[src/Client/RowView.fs]] L204–208) → Tab CAS fails while mode is Editing.
2. **Poll/Load apply + `withSiteMap` focus relocate, adjustMode checks the wrong node.** `adjustMode` uses post-refresh `focusedNodeId` ([[src/Client/UpdateHelpers.fs]] L225–237; [[src/Client/UpdateHelpers.fs]] `withSiteMap` L361–374). If focus moves off the node whose text changed, mode stays Editing; RecreateRow on the new focus initializes from the old `originalText` → same CAS shape. Needs a clean edit field (or nested mode) so apply is not blocked.
3. **contentEditable whitespace alone — weak.** Diverges live vs graph only; CAS old is still `originalText` which matched graph at enter. Needs a graph move (1–2) as well.
4. **Indent plan order — weak.** `planIndentSelection` may expand the previous sibling; `tryTextCommitOps` still runs on current focus before Replace ([[src/Shared/ViewModelMoveOps.fs]] L29–57; [[src/Client/UpdateMove.fs]] L139–152).
5. **Multi-select / SiteId / Ref focus ≠ `#edit-input` — weak while Editing.** Keyboard and pointer paths commit or re-snapshot before focus moves ([[src/Client/UpdateOps.fs]] L137–178, L542–554).

## Sequence (mechanism 1)

Enter edit (original=A) → type (optional) → open palette → Poll applies A→A′ (or structural+text) → close → Editing(A) + graph A′ → Tab → `SetText(A, live)` → `Graph.setText` `"old text does not match"`.

## What would refute

- Fail with **no** overlay and **no** Poll/Load/Submit apply between enter-edit and Tab, and client `graph.text` still equals enter-edit snapshot → look elsewhere (instrument `originalText` vs `graph.text` at Tab).
- Fail when `originalText = graph.text` at apply → not this CAS shape (would be Replace / other).

## HITL check

1. Multi-select, edit focus, type one char, open command palette, wait one Poll (5 s), close, Tab.
2. Log or watch: mode Editing; `#cmd-last-result` `old text does not match`; optional console: `originalText`, `graph.nodes[id].text`, `readEditInputValue()` at Indent.
