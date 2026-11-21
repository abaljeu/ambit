# Code review — Rename SnapshotDone to NodePersisted

- Range: `git diff HEAD`
- Spec: [77 — Rename SnapshotDone to NodePersisted, and Notices start no catch-up](../issues/77-rename-snapshotdone-and-notices-start-no-catch-up.md)
- Arch: [core-refinement architecture](../arch.md)

## 1. Standards

1. No findings.
2. Scan: `countsAsExternal`, `isNoticeEvent`, and `dropNotices` are under the 40-line function limit. No long line, file-growth, tab, mutable, bare-id, or banned-phrase hit.
3. **Fixed before commit** — `Api.postPoll` called `countsAsExternal` before that binding. The helper now sits above `postPoll`.

## 2. Spec

1. No findings.
2. **Fixed before commit** — [Message shape](../cqrs-messages.md) §1 named `SnapshotDone`. It now names `Notice.NodePersisted` of `nodeId`. [core-refinement architecture](../arch.md) §5 item 12 no longer lists `NodePersisted` as both a today's case and a later case.

## 3. Arch

1. No findings.

## 4. Totals

- Standards: 0 findings. The ordering bug was fixed before commit.
- Spec: 0 findings. The basis sentences were updated before commit.
- Arch: 0 findings.
