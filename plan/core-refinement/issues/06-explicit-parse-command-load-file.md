# 06 — Explicit parse command on a File (Load)

**Type:** coding
**Status:** coded
**Blocked by:** None — can start immediately
Actual: 3.5h

## Context

A person issues an explicit parse command on a File. Core accepts that command as mailbox Load. The subject is a File node. The parse thread parses that file. Parse is disk → graph. When parse finishes, the parse thread adds InMsg ParseFinished through the private function. The queue puller hands that InMsg to the InMsg handler. The core loop sets that node Parsed.

The 2026-09-29 code does not match that finish. `ParseThread.parseOne` calls `DocumentPersistWrite.planParseFile`. `ImportDocument.planParseFile` emits `Op.SetDocumentState`. `ParseThread.postParseOps` mints a Change with `GraphOnlyChangePost.mint` and posts it through `CoreMailbox.postGraphOnly`. `History.apply` applies that op through `Graph.setDocumentState`, which calls `GraphMutate.setDocumentState`. That write sets the parsed axis. The parse thread does not add InMsg. The mailbox queue element is still `CoreMsg`.

## What to build

This ticket keeps mailbox Load of a File node and the parse thread loop. The required finish is InMsg on the one mailbox queue, [[../arch.md]] §10 Core loop. Claim homes: [Mailbox](../../doc/current/mailbox.md) and [Parse and persist](../../doc/current/parse-persist.md).

### 1. Mailbox Load of a File node

- [x] Load on the mailbox queue — Load arrives on the mailbox queue. The door is `CoreMailboxBackend.dispatchLoad`.
- [x] File node subject — The subject is a File node.
- [x] Push for parse — Load pushes that File node onto the Parse stack (`parsePush`).

### 2. Parse thread

- [x] One stack — The request is a push onto the Parse stack.
- [x] Parse thread — The consumer is the parse thread in `src/Server/ParseThread.fs`. The parse thread is a thread.
- [x] Loop runs parse — The parse thread pulls that File node and runs `DocumentPersistWrite.planParseFile`.
- [x] Keep old paths — Existing old parse callers stay. This ticket does not remove them.

### 3. Parse finish

- [x] InMsg — The message type is InMsg. It is not an Op. It carries the completion and the node. Cases: ParseFinished, SnapshotDone. Home: [[../arch.md]] §10 Core loop.
- [x] One queue — The mailbox has one queue. A private function adds InMsg. A public function adds CoreMsg. The queue puller hands the item to its handler. There is no second queue. Home: [[../arch.md]] §10 Core loop.
- [x] Parse thread adds — When this File parse finishes, the parse thread adds InMsg ParseFinished for that node through the private function. The parse thread does not edit the graph axes. The parse thread does not hold the mailbox queue.
- [x] Core loop sets Parsed — The InMsg handler applies ParseFinished. It sets that node Parsed only through `GraphMutate.setParseState`. PersistState stays unchanged.
- [x] Public post stays clear — `CoreMailbox.postEvents` and `CoreMailbox.postGraphOnly` do not carry InMsg.
- [x] Axis writers — `Op.SetDocumentState` is not the writer of the parsed axis. `Op.SetPersistState` is not a writer.

## Out of scope

1. **Contract old parse paths** — This ticket does not remove other parse callers.
2. **Persist thread** — This ticket does not build the persist thread. SnapshotDone is the persist-side case of InMsg. It replaces PersistFinished. The persist thread add stays [[../arch.md]] §10 Persist thread.
3. **Workspace lock** — This ticket does not build the workspace lock. That lock stays [[../arch.md]] §6.
4. **Later sequence steps** — DataDir path control and git requests stay on later [[../arch.md]] §3 steps.

## See also

[[../arch.md]] §10 Core loop; [Mailbox](../../doc/current/mailbox.md); [Parse and persist](../../doc/current/parse-persist.md); [03 — One Parse thread stack](03-one-parse-thread-stack.md); [05 — Selection-scoped Parse after whole-tree git Load](05-selection-scoped-parse-after-whole-tree-git-load.md)

## Comments

- 2026-09-29: Specced from Alan’s first use case (explicit parse command on a file). Expand only; Status `defined`.
- 2026-09-29: Alan — background parse loop needs a separate thread, so the loop is an Actor function.
- 2026-09-29: Implemented expand path (mailbox Load, ParseStack, ParseThread loop). Status `coded`. Review: [code-review-06](../reports/code-review-06-explicit-parse-command-load-file.md).
- 2026-09-30: Alan — Prior “Actor function” framing withdrawn ([[../arch.md]] §3 step 2).
- 2026-10-01: Revisit. The coded finish does not match [[../arch.md]] §10 Core loop. When parse finishes, the parse thread adds InMsg ParseFinished through the private function. The core loop sets Parsed through `GraphMutate.setParseState`. Status `defined`. The 2026-09-29 `coded` mark was the Change and `Op.SetDocumentState` path.
- 2026-10-01: Alan locked InMsg on the one mailbox queue. This ticket follows that name. Status stays `defined`.
- 2026-10-01: InMsg is on the one mailbox queue. File parse finish adds ParseFinished. The db agent adds SnapshotDone. CoreMsg has no SnapshotDone. `Op.SetDocumentState` still dual-writes the parse axis for other callers. Status `coded`.

## Time

- 2026-09-29 1.5h — implement mailbox Load, ParseStack, ParseThread loop, tests `(from chat)`
- 2026-10-01 2h — InMsg queue, ParseFinished, SnapshotDone off CoreMsg `(from chat)`
