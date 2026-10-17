# 38 — Mailbox parse-state writer logs the Event

**Status:** `coded`
**Actual:** 1h
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

[Graph change expand](plan/core-refinement/arch.md#graph-change-expand) stands the mailbox parse-state writer and the logged Event before any caller moves. `InMsg.SetDocumentState` carries Parsed, Unparsed, or NoServerFile. Parsed means the Graph has the file's data. NoServerFile means a file is pending from the client. Those three values are one parse state in the column `document_state`. The client displays document state from `Op.SetDocumentState` in an Event. The Op stays. Callers stay on their current path.

## What to build

The mailbox has one parse-state writer. It runs only on Core loop. It applies `InMsg` and logs `Op.SetDocumentState` as an Event. Callers stay on their current path. The public post still accepts the Op.

### Writer

1. [x] **Core loop** — The writer runs on the mailbox processor. It is not returned to a caller outside that loop.
2. [x] **Three cases** — The writer applies `InMsg.ParseFinished`, `InMsg.MarkUnparsed`, and `InMsg.SetDocumentState`. `ParseFinished` sets Parsed. `MarkUnparsed` sets Unparsed. `SetDocumentState` sets Parsed, Unparsed, or NoServerFile.
3. [x] **Logged Event** — When the stored value changes, the writer appends one Event. The body is `EventBody.Change` of one `Op.SetDocumentState`. The Op carries the prior value and the new value. The client receives that Event.
4. [x] **One Event** — The writer does not append a second Event when this post already stored the same transition.
5. [x] **Callers stay** — Upload, import, undo, stored replay, file parse, directory reconcile, lazy load, and `markUnparsedThenPush` stay on their current path.
6. [x] **Public post** — `CoreMailbox.postEvents` and `CoreMailbox.postGraphOnly` still accept `Op.SetDocumentState`. The Op stays in [History](src/Shared/History.fs) and in the JSON codec.
7. [x] **Persist state** — The logged Event does not change persist state. Persisted stays a separate field.

### Obligations

1. [x] **Log test** — `ParseFinished and MarkUnparsed log SetDocumentState` in [Core loop InMsg tests](tests/Server.Tests/CoreLoopInMsgTests.fs). The Event carries the prior value and the new value. The node matches that new value.
2. [x] **NoServerFile test** — `SetDocumentState InMsg sets NoServerFile and logs the Event` in the same file. The node is NoServerFile. The Event's new value is NoServerFile.
3. [x] **Door test** — `public post of SetDocumentState is accepted` in the same file.
4. [x] **Four functions** — Upload, import, undo, and stored replay succeed. The client still receives the parse-state Event. `planServerFilePresentOps transitions only matching absent files`, `build import marks unparsed file current before tree operations`, `valid parse batch can replay undo and redo`, and `File recover applies Ops Ev ahead of Graph checkpoint` stay green.
5. [x] **Mailbox claim** — [Mailbox](doc/current/mailbox.md) says the mailbox writer applies `InMsg.SetDocumentState` for Parsed, Unparsed, and NoServerFile, and appends `Op.SetDocumentState` when that state changes.

## Finish-up

[x] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[x] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[x] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## Amendments

1. **None** — The spec did not change.

## Side effects

1. **Parse finish logs** — `InMsg.ParseFinished` and `InMsg.MarkUnparsed` append `Op.SetDocumentState` when the stored value changes. File parse and directory reconcile already enqueue those cases. They were not migrated. A client can see that Event.
2. **markUnparsedThenPush stays silent** — That function still writes through `writeParseState` and does not append an Event. `InMsg.MarkUnparsed` does.
3. **Append after the graph write** — The writer writes the node, then appends the Event. If `appendEvent` or `getEventLog` fails, the node has changed and no Event is stored. `ParseFinished` then skips `noteParsed`.
4. **Newest Event guard** — The writer skips a second Event only when the newest Event already contains the same transition. An older Event with that transition does not suppress a later change.
5. **NoServerFile and MarkUnparsed** — `setParseState` still clears `noServerFile` on `MarkUnparsed`. That change now logs `NoServerFile` to Unparsed. `ParseFinished` still leaves a `noServerFile` File Unparsed and logs nothing. `InMsg.SetDocumentState` of Parsed clears `noServerFile`.
6. **Mailbox event log** — The writer appends through `persist.appendEvent`. Client poll uses that log. The mailbox in-memory event log (`GetEventHistory`, `EventsSince`) does not receive the Event until the host reseeds. A later public post advances its next id from the persist log. Undo of the writer Event looks in the in-memory log and does not find it until that reseed.

## Time

- 2026-10-17 1h — mailbox parse-state writer, tests, and claims (from chat)

## See also

- [Graph change expand](plan/core-refinement/arch.md#graph-change-expand)
- [core-refinement](plan/core-refinement/project.md)
- [39 — Upload, import, undo, and stored replay use the mailbox writer](39-upload-import-undo-replay-use-the-mailbox-writer.md)
- [Mailbox](doc/current/mailbox.md)
- [GLOSSARY](GLOSSARY.md)
