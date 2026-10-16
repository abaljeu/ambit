# 16 — One document state

**Status:** `defined`
Actual: 45m
**Type:** bug-fixing
**Blocked by:** None — can start immediately

## Context

A Node in [Model](../../../src/Shared/Model.fs) stores `documentState` (`Current` | `Unparsed` | `NoServerFile`) and `parseState` (`Parsed` | `Unparsed`). Those two fields hold one parse fact. `withDocumentState` writes `documentState` only. `withParseState` writes both fields. A later `SetDocumentState` to Unparsed leaves `parseState` Parsed.

The slashed hollow circle in [Children indicator](../../../src/Shared/ViewModelChildrenIndicator.fs) reads `documentState`. [Parse thread](../../../src/Server/ParseThread.fs) `alreadyParsed` returns at once when `parseState` is Parsed, so the outline stays and the mark stays Unparsed.

A new database starts both fields Unparsed, so the parse event includes `SetDocumentState` to Current and the mark updates.

[Serialization](../../../src/Shared/Serialization.fs) stores the two fields as independent JSON values. [History](../../../src/Shared/History.fs) stores `SetDocumentState` only. SQL stores `document_state` only. [Graph projection](../../../src/Shared/GraphProjection.fs) builds the node from that column, and `Node.Create` derives `parseState` from `documentState`.

`NoServerFile` is a third parse case. Unpersisted means a graph edit still needs a disk write. A new File stub is `NoServerFile` and Persisted.

## What to build

A Node stores one `documentState`.

```fsharp
type DocumentParse = Parsed | Unparsed | NoServerFile
type DocumentPersist = Persisted | Unpersisted
type DocumentState = { parse: DocumentParse; persist: DocumentPersist }
```

The name Current goes away. A stored Current reads as Parsed. The parse value comes from the old `documentState`. A stored `parseState` is dropped on read. JSON `persistState` becomes `persist`.

The client sends no `SetDocumentState`. The client applies `parse` from each `SetDocumentState` it receives on the event queue, by post or by poll. `persist` is absent from that op.

Load writes `parse` = Unparsed on the server node, queues `SetDocumentState` Unparsed, then pushes the parse stack. The stack processor reads that node and sees Unparsed. The work list is a stack (last in, first out). A popped entry whose `parse` is Parsed is skipped, so Load sets Unparsed before the push.

When parse finishes, including when the outline already matches and there are no outline ops, the finish queues `SetDocumentState` Parsed. A parse error leaves Unparsed and leaves the outline.

An edit marks the owning node Unpersisted. Applying Parse leaves `persist` unchanged. History stores no `DocumentPersist`. The SQL column `document_persist` is a bool. `true` means Persisted. An older row loads as `true`.

Load, the parse stack, and history keep their operations aside from these types and these posts.

1. [ ] **One record** — `Node.documentState` is `{ parse: DocumentParse; persist: DocumentPersist }`. The node has no `parseState` field and no `persistState` field.
2. [ ] **Read old stores** — A stored Current reads as Parsed. The parse value comes from the old `documentState`. A stored `parseState` is dropped on read. JSON `persistState` reads as `persist`.
3. [ ] **Client apply** — The client sends no `SetDocumentState`. Each received `SetDocumentState` sets `parse`. `persist` is absent from that op.
4. [ ] **Load posts Unparsed** — [Core mailbox load](../../../src/Server/Core/CoreMailboxLoad.fs) writes `parse` = Unparsed, queues `SetDocumentState` Unparsed, then pushes that node. The stack processor sees Unparsed.
5. [ ] **Skip Parsed** — A popped stack entry whose `parse` is Parsed is skipped.
6. [ ] **Parse finish posts Parsed** — A finished parse queues `SetDocumentState` Parsed, including a finish that posts no outline ops. A parse error leaves Unparsed and leaves the outline.
7. [ ] **Persist axis** — An edit marks the owning node Unpersisted. Applying Parse leaves `persist` unchanged. History stores no `DocumentPersist`. [Database](../../../src/Server/Database.fs) column `document_persist` is a bool, and `true` means Persisted. An older row loads as `true`.
8. [ ] **NoServerFile** — `NoServerFile` is a `DocumentParse` case. A new File stub is `NoServerFile` and Persisted.

### Acceptance Criteria

1. [ ] **One parse fact** — `SetDocumentState` to Unparsed sets `parse` to Unparsed. No second field remains Parsed.
2. [ ] **Mark follows the event** — After a parse that finishes with no outline ops, the received `SetDocumentState` Parsed clears the slashed hollow circle.
3. [ ] **Same path for a new database** — A new database updates the mark through that same `SetDocumentState` Parsed.
4. [ ] **File stub** — A new File stub is `NoServerFile` and Persisted.
5. [ ] **Edit and parse** — A graph edit marks the owning node Unpersisted. Applying Parse leaves `persist` unchanged.

## See also

- [Model](../../../src/Shared/Model.fs)
- [Graph mutate](../../../src/Shared/GraphMutate.fs)
- [Parse thread](../../../src/Server/ParseThread.fs)
- [Children indicator](../../../src/Shared/ViewModelChildrenIndicator.fs)
- [Serialization](../../../src/Shared/Serialization.fs)
- [Graph projection](../../../src/Shared/GraphProjection.fs)
- [History](../../../src/Shared/History.fs)
- [Database](../../../src/Server/Database.fs)
- [15 — Load marks its subject Unparsed at the entry point](15-load-marks-its-subject-unparsed-at-the-entry-point.md)

## Comments

- 2026-10-14 — Filed. Two fields hold one parse fact. The correction is one `documentState` record and the `SetDocumentState` posts above.
- 2026-10-14 — Implement stopped before code. Binding architecture contradicts this ticket. Report: [Implement 16 — One document state](../reports/implement-16-one-document-state.md). Status stays `defined`.

## Time

- 2026-10-14 45m — read binding architecture and stopped on the writer conflict (from chat)
