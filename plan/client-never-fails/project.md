# Client never fails on server data

Stage: spec
Summary: The Browser takes in every Event and answer that the Server sends, and never asks the user to reload because of server data.
Updated: 2026-10-29
Started: 2026-10-29

## Notes

- 2026-10-29 — Opened by Alan. Source statement: [140 — Client never fails on server data](../core-refinement/issues/140-client-never-fails-on-server-data.md). That ticket stays in core-refinement as the original statement; this Project does its work.
- 2026-10-29 — Alan agreed the points in [[plan/client-never-fails/map.md]] one at a time in chat. Alan added Notices as the first case: a Notice that would otherwise trip the Browser is ignored. Evidence: [[plan/client-never-fails/reports/notice-trip-findings.md]].

## Open questions

- 1 — Placeholder text: what text the placeholder row shows before its Node arrives.
- 2 — Merge in progress: which handling the Browser uses when a post reply carries different changes and more changes are pending. Ignore the reply, post the rest, and wait for a clean merge is one example.
- 3 — Typing pause: how long a pause in typing is before the Browser posts the edit.
- 4 — Edited Node removed: what the Browser does when a Poll removes the Node in edit mode. Alan named "idle poll may override edit mode".
- 5 — Server write order: the Server writes the Event first, or writes the Event and the Graph in one transaction.
- 6 — Loaded definition: [[GLOSSARY.md]] says a Loaded Node has its Children present. Alan's definition adds that each child Node is Resident. Does the glossary change?
- 7 — Ignored Notice: "ignored" means the Browser skips the Notice and takes its event id, or applies the Notice without the block. A skipped Notice leaves the parse or persist status stale until the next Notice for that Node.
- 8 — Event id ahead with no Events: after the server write order fix, does the Browser still need its own handling when the Server reports an event id with no Events behind it?
