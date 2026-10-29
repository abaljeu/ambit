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
- 2026-10-29 — Slicing revised. `ChildrenStatus` stays. The Server-sends-child-list ticket is 06 — Server sends each Node with its child list. `includeWorkspace` is one ticket, 07 — Delete includeWorkspace, blocked by 06. Ticket `Status` stays empty. The worker sets the stage when it starts that stage. Scope facts sit in each ticket. Count of ticket files plus reports is 30.

## Open questions

- spec.md story 13 still says remove Loaded and Unloaded, including `ChildrenStatus`, the wire values, the `residencyText` children line, and the glossary entries.
- arch.md path 6 still says this removes Loaded and Unloaded, and steps 6.8, 6.11, and 6.12 still delete the wire field and the type. Section 4 still proposes deleting the Loaded and Unloaded glossary entries.
- arch.md path 7, Poll for nearby Absent Nodes, is not ticketed. The file count is at 30, and that path would change tests that build Unloaded Nodes.
