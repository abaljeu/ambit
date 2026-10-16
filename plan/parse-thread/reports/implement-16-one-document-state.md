# Implement 16 — One document state

Date: 2026-10-14
Ticket: [16 — One document state](../issues/16-one-document-state.md)
Workplace: `dev`

## 1. Outcome

1. **Stop** — Coding did not start. The ticket contradicts binding architecture. The rule and the ticket must agree before code changes.
2. **Status** — Ticket status stays `defined`. This run does not set `coded`.
3. **Code** — No production file and no test file changed.
4. **Tests** — No tests ran. There is no diff to compile.
5. **Review** — Code review did not run. There is no change to review.

## 2. Binding architecture read

1. **Parse thread architecture** — [Parse thread architecture](../arch.md) is this Project's binding note.
2. **Core refinement architecture** — [core-refinement architecture](../../core-refinement/arch.md) binds the Core seam: axes, the core loop, and `InMsg`. The ticket names [Core mailbox load](../../../src/Server/Core/CoreMailboxLoad.fs), parse finish, and the parse stack.
3. **Browser residency architecture** — [browser-residency architecture](../../browser-residency/arch.md) §11 **Slashed Unparsed Bullet** names `documentState`. That note describes the slashed hollow circle. It is not the stop.

## 3. Conflicts

1. **Contract removes DocumentState** — [core-refinement architecture](../../core-refinement/arch.md) §3 step 1 **State axes on special nodes**, item 3 **Contract**, and §9 story path 1 **State axes on special nodes**, item 3 **Contract**: remove `DocumentState` once no caller remains. `ParseState` and `PersistState` stay. The ticket stores one `documentState` record and removes `parseState` and `persistState`.
2. **SetDocumentState is not the writer** — [core-refinement architecture](../../core-refinement/arch.md) §9 story path 7 **InMsg**, item 3 **Contract**; §10 **Core loop**, Interface item 9 **Private axes** and item 10 **Mark unparsed**. The same rule is [Parse thread architecture](../arch.md) story path 22 **Add InMsg**, items 3 **Core loop**, 4 **Not an Op**, and 6 **MarkUnparsed**. `Op.SetDocumentState` is not the writer of the parsed axis. The field write is `GraphMutate.setParseState`. The ticket says each received `SetDocumentState` sets `parse`, Load queues `SetDocumentState` Unparsed, and a finished parse queues `SetDocumentState` Parsed.
3. **Parse finish field write** — [core-refinement architecture](../../core-refinement/arch.md) §5 item 6 **Directory Parse done** and §10 **Core loop**, Interface item 4 **Parse finished**. [Parse thread architecture](../arch.md) §2 **Directory reconcile**, Interface item 7 **ParseFinished** and Uses item 6 **ParseFinished**. Parse finish adds `InMsg` `ParseFinished`. The core loop sets Parsed through `GraphMutate.setParseState`. Persist stays unchanged. The ticket says the finish queues `SetDocumentState` Parsed, including a finish that posts no outline ops.
4. **Current remains the reconcile value** — [Parse thread architecture](../arch.md) §1 story path 1 **Unparsed becomes Graph**, item 3 **Current**, and §2 **Directory reconcile**, Interface item 3 **Current**. When the Directory Node or Workspace Node is Unparsed, the ops start with `SetDocumentState` from Unparsed to Current. The ticket says the name Current goes away and a stored Current reads as Parsed.
5. **New special node starts Unparsed** — [core-refinement architecture](../../core-refinement/arch.md) §5 item 7 **Create special**. A new special node starts Unparsed and Persisted. The ticket says a new File stub is `NoServerFile` and Persisted.

## 4. Rules that match the ticket

1. **Skip Parsed** — [core-refinement architecture](../../core-refinement/arch.md) §5 item 11 **Parse stack pop**. A popped node whose parse is Parsed is skipped. The ticket says the same.
2. **Edit marks Unpersisted** — [core-refinement architecture](../../core-refinement/arch.md) §1 item 2 **Persisted | Unpersisted** and §5 item 2 **Graph edit**. A graph edit marks the owning node Unpersisted. The ticket says the same, and applying Parse leaves persist unchanged. That persist rule matches **Parse finished**.

## 5. Next step

1. **Agree the writer** — Update [core-refinement architecture](../../core-refinement/arch.md) and [Parse thread architecture](../arch.md), or change [16 — One document state](../issues/16-one-document-state.md), so one statement owns who writes `parse` and whether `DocumentState` remains.
2. **Then implement** — After that agreement, implement can start. This run left the tree's code as it was.
