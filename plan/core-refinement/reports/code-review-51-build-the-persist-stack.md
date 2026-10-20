# Code review — 51 Build the persist stack

- Range: `git diff HEAD`
- Spec: [51 — Build the persist stack](../issues/51-build-the-persist-stack.md)
- Arch: [core-refinement architecture](../arch.md)

## 1. Standards

1. **Judgement — hot path** — [Persist thread](../../../src/Server/Core/PersistThread.fs) `pullOne` calls `persistGraphChange` with the live graph. That existing write walks every node. No production caller pushes a node, so this loop does not run on the edit path.
2. **Note — scan** — `measure-fs-size` listed `startPersistThreads` (22), `notify` (3), `runOne` (8), `start` (5), and `loopNodes` (4). Each is under the 40-line limit in [F# source](../../../.agents/rules/fsharp-source.md). No added line exceeds 100 characters. No banned phrase.

## 2. Spec

1. **Partial — node id and the write** — `pullOne` calls `DocumentPersistChange.persistGraphChange` with the current graph as both sides. The node id is the `Notice.NodePersisted` target. It is not an input that selects the file. No production caller pushes a node.
2. **Unnamed — PersistWrite** — Internal write-dependency record. The ticket names the stack, the thread, and `Notice.NodePersisted`. It does not name this record.
3. **Unnamed — PersistNodePull** — Internal pull record (`take`, `dataDir`, `graph`, `write`). The ticket does not name it.
4. **Unnamed — persistPush** — Private cell on the db agent and the file agent. It holds the stack push so route registration does not. The ticket does not name the cell.
5. **Note — failure hook** — A failed node pull uses an empty `changeFailed`. It does not clear the db diff batch.

## 3. Arch

1. **No findings.** — [core-refinement architecture](../arch.md) Graph change persist stack holds: the stack is node ids inside Core, it is not a `MailboxProcessor`, route registration does not construct it, the thread calls `DocumentPersistChange` and does not edit the graph, and a successful write posts `Notice.NodePersisted`.

## 4. Totals

- Standards: 1 finding. Worst: the node pull can call a whole-graph write once a caller pushes.
- Spec: 4 findings. Worst: the pulled node id does not select the file write.
- Arch: 0 findings. Worst: none.
