# Client never fails on server data

Stage: spec
Summary: The Browser takes in every Event and answer that the Server sends, and never asks the user to reload because of server data.
Updated: 2026-10-29
Started: 2026-10-29

## Notes

- 2026-10-29 — Opened by Alan. Source statement: [140 — Client never fails on server data](../core-refinement/issues/140-client-never-fails-on-server-data.md). That ticket stays in core-refinement as the original statement; this Project does its work.
- 2026-10-29 — Alan agreed the points in [[plan/client-never-fails/map.md]] one at a time in chat. Alan added Notices as the first case: with no pending changes, the Browser accepts each Notice and never blocks on it. Evidence: [[plan/client-never-fails/reports/notice-trip-findings.md]].
- 2026-10-29 — Alan answered the open questions on the draft PR. The map and the spec hold the answers. A post reply applies Notices. The placeholder is drawn in color #444444. A merge in progress ignores the reply, posts the rest, and waits for a clean merge (rule for now). An edit posts after a one-second typing pause. A Poll that removes the edited Node ends edit mode. The Server writes the Event first, then the Graph, so the Browser needs no handling for an event id with no Events. Loaded becomes Resident (Node and child list on the Browser) and Unloaded becomes Absent.

## Open questions

- 1 — Glossary fit: Alan chose Resident and Absent and judges they match the earlier usage. [[GLOSSARY.md]] says Resident is "A Node whose Header is present in this Graph. Children may still be Loaded or Unloaded." with antonym Absent. In this Project, Resident is a Node with its Header and child list, and Absent is a Node with its Header and no child list. The glossary calls that second Node Resident and Unloaded, and its Absent is the placeholder case: a NodeId in a child list with no Node.
