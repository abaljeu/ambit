# Code review — 07 Directory Unparsed during reconcile, Spec focus

Range: `staging...HEAD`. Ticket: [07 — Directory Unparsed during reconcile](../issues/07-directory-unparsed-during-reconcile.md). Architecture: [Parse thread architecture](../arch.md) §2 Module map, item 1 **Directory reconcile**, **Directory needs reparse** and **Directory Unparsed**.

## 1. Standards

No findings. The mechanical scan lists new bindings, and each is under the 40-line limit in [F# source](../../../.agents/rules/fsharp-source.md). No added line exceeds 100 characters. [History](../../../src/Shared/History.fs) stays under 800 lines.

## 2. Spec

1. **Suffix detach beside a Normal child** — [07 — Directory Unparsed during reconcile](../issues/07-directory-unparsed-during-reconcile.md) asks Directory reconcile to append a missing Directory Node. [Unparsed shell edit](../../../src/Shared/UnparsedShellEdit.fs) `isStubEdit` also treats the swapped lists as a stub edit. `Op.invert` emits that swap, and undo applies those inverse ops through `Op.applyAllowing`, so the reverse is the undo of the append while the shell is still Unparsed. The predicate does not limit the suffix to nodes this reconcile just created. A Replace can also drop a pre-existing trailing File Node or Directory Node under an Unparsed Directory or Workspace that still has a Normal child. That wider detach is not in the ticket.

## 3. Match-check labels

These two labels were `missed`. Neither is a Spec gap.

1. **Push** — False positive. Directory reconcile names the Directory Node on the same `push` list a File Node already uses: disk-newer File ids, then disk-newer Directory ids, then ids of Directory Nodes just created. [Parse thread](../../../src/Server/ParseThread.fs) `afterPost` is not in this diff. It still calls `deps.push` for that list when the parse thread holds the Parse stack. That is the File Node rule on [06 — Setting Unparsed, recursive update](../issues/06-setting-unparsed-recursive-update.md).

2. **No axis edit** — False positive. This diff does not change the parse thread. Reconcile ops are `NewSpecialNode` and a child-list append. The disk-newer Directory test asserts those ops contain no `Op.SetDocumentState`, and the Directory Node stays Current and Parsed. The ids are only on `push`. The existing parse thread posts `InMsg.MarkUnparsed`. The core loop applies that InMsg. The parse thread does not edit the graph axes.

## 4. Totals

Standards: 0 findings. Spec: 1 finding. Worst spec issue: Suffix detach beside a Normal child.
