# 38 — Mailbox parse-state writer logs the Event

**Status:** `cancelled`
**Type:** coding
**Blocked by:** None — can start immediately
**Trackers:** architecture

## Context

[Graph change expand](plan/core-refinement/arch.md#graph-change-expand) stands the mailbox parse-state writer and the logged Event before any caller moves. `InMsg.SetDocumentState` carries Parsed, Unparsed, or NoServerFile. Parsed means the Graph has the file's data. NoServerFile means a file is pending from the client. Those three values are one parse state in the column `document_state`. The client displays document state from `Op.SetDocumentState` in an Event. The Op stays. Callers stay on their current path.

## What to build

The mailbox has one parse-state writer. It runs only on Core loop. It applies `InMsg` and logs `Op.SetDocumentState` as an Event. Callers stay on their current path. The public post still accepts the Op.

### Writer

1. [ ] **Core loop** — The writer runs on the mailbox processor. It is not returned to a caller outside that loop.
2. [ ] **Three cases** — The writer applies `InMsg.ParseFinished`, `InMsg.MarkUnparsed`, and `InMsg.SetDocumentState`. `ParseFinished` sets Parsed. `MarkUnparsed` sets Unparsed. `SetDocumentState` sets Parsed, Unparsed, or NoServerFile.
3. [ ] **Logged Event** — When the stored value changes, the writer appends one Event. The body is `EventBody.Change` of one `Op.SetDocumentState`. The Op carries the prior value and the new value. The client receives that Event.
4. [ ] **One Event** — The writer does not append a second Event when this post already stored the same transition.
5. [ ] **Callers stay** — Upload, import, undo, stored replay, file parse, directory reconcile, lazy load, and `markUnparsedThenPush` stay on their current path.
6. [ ] **Public post** — `CoreMailbox.postEvents` and `CoreMailbox.postGraphOnly` still accept `Op.SetDocumentState`. The Op stays in [History](src/Shared/History.fs) and in the JSON codec.
7. [ ] **Persist state** — The logged Event does not change persist state. Persisted stays a separate field.

### Obligations

1. [ ] **Log test** — `ParseFinished and MarkUnparsed log SetDocumentState` in [Core loop InMsg tests](tests/Server.Tests/CoreLoopInMsgTests.fs). The Event carries the prior value and the new value. The node matches that new value.
2. [ ] **NoServerFile test** — `SetDocumentState InMsg sets NoServerFile and logs the Event` in the same file. The node is NoServerFile. The Event's new value is NoServerFile.
3. [ ] **Door test** — `public post of SetDocumentState is accepted` in the same file.
4. [ ] **Four functions** — Upload, import, undo, and stored replay succeed. The client still receives the parse-state Event. `planServerFilePresentOps transitions only matching absent files`, `build import marks unparsed file current before tree operations`, `valid parse batch can replay undo and redo`, and `File recover applies Ops Ev ahead of Graph checkpoint` stay green.
5. [ ] **Mailbox claim** — [Mailbox](doc/current/mailbox.md) says the mailbox writer applies `InMsg.SetDocumentState` for Parsed, Unparsed, and NoServerFile, and appends `Op.SetDocumentState` when that state changes.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, adding what's missing (but skipping minutia, and avoiding repetition within the documentation)), deleting things no longer true and checking off things newly achieved.

## See also

- [Graph change expand](plan/core-refinement/arch.md#graph-change-expand)
- [core-refinement](plan/core-refinement/project.md)
- [39 — Upload, import, undo, and stored replay use the mailbox writer](39-upload-import-undo-replay-use-the-mailbox-writer.md)
- [Mailbox](doc/current/mailbox.md)
- [GLOSSARY](GLOSSARY.md)

## Comments

- 2026-10-17 — Superseded by the File event model. Status `cancelled`. Successor: [43 — Expand File events beside the Set output](43-expand-file-events-beside-the-set-output.md).
