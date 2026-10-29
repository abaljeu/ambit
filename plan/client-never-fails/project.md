# Client never fails on server data

Stage: slicing
Summary: The Browser takes in every Event and answer that the Server sends, and never asks the user to reload because of server data.
Updated: 2026-10-29
Started: 2026-10-29

## Notes

- 2026-10-29 — Opened by Alan. Source statement: [140 — Client never fails on server data](../core-refinement/issues/140-client-never-fails-on-server-data.md). That ticket stays in core-refinement as the original statement; this Project does its work.
- 2026-10-29 — Alan agreed the points in [[plan/client-never-fails/map.md]] one at a time in chat. Alan added Notices as the first case: with no pending changes, the Browser accepts each Notice and never blocks on it. Evidence: [[plan/client-never-fails/reports/notice-trip-findings.md]].
- 2026-10-29 — Alan answered the open questions on the draft PR. The map and the spec hold the answers. A post reply applies Notices. The placeholder is drawn in color #444444. A merge in progress ignores the reply, posts the rest, and waits for a clean merge (rule for now). An edit posts after a one-second typing pause. A Poll that removes the edited Node ends edit mode. The Server writes the Event first, then the Graph, so the Browser needs no handling for an event id with no Events.
- 2026-10-29 — Alan: remove Loaded and Unloaded. Each state return for a NodeId includes both its Header and its child list. Resident and Absent keep their glossary meaning; an Absent child is the placeholder. Findings: [[plan/client-never-fails/reports/remove-loaded-findings.md]].
- 2026-10-29 — Alan: cost accepted. Poll and child-id lists have no width limit. Parse holds the safety valve for size. The Load refusal `LoadRefuse.MultiWorkspace` stays; it is not residency Poll.
- 2026-10-29 — Arch written: [[plan/client-never-fails/arch.md]]. Story paths and the slicing strategy follow Alan's seven steps, from Notices through the reload prompt. Direction: `tracer-cut`.
- 2026-10-29 — Alan: the placeholder comes before the child-list step. Order: Notices, Event then Graph, placeholder, child list on each Node, Absent fetch, protocol then the one-second typing pause, reload prompt last.
- 2026-10-29 — Slicing. Ticket folders are under [[plan/client-never-fails/issues/]], in that order. The size limit splits the Graph model test updates into 16 — Retarget model status tests through 25 — Delete ChildrenStatus, and the whole-Workspace flag into 27 — Retarget Load capture Workspace flag through 30 — Delete includeWorkspace. Each ticket `Status` stays at the empty template value. The worker sets the stage when it starts that stage. That note is in each ticket `CONTEXT.md`.

## Open questions

- 08 — Apply Replace when parent Header is present also drops the `parent children not loaded` gate in `GraphMutate.replace`. The architecture names `applyOpAllowing` for that step. The gate is in this ticket so 25 — Delete ChildrenStatus has no Replace caller left.
- The Graph model contract is 16 — Retarget model status tests through 25 — Delete ChildrenStatus because the status assertions are test functions and the limit is 4 tests. The whole-Workspace flag uses the same limit, so it is 27 — Retarget Load capture Workspace flag through 30 — Delete includeWorkspace.
- 37 — Reload prompt has no existing test. The sentences live in the two view modules.
