# Code review — 07 Directory Unparsed during reconcile, Spec focus

Range: `staging...HEAD`. Ticket: [07 — Directory Unparsed during reconcile](../issues/07-directory-unparsed-during-reconcile.md). Architecture: [Parse thread architecture](../arch.md) §2 Module map, item 1 **Directory reconcile**, **Directory needs reparse** and **Directory Unparsed**.

## 1. Standards

No findings. The mechanical scan lists new bindings, and each is under the 40-line limit in [F# source](../../../.agents/rules/fsharp-source.md). No added line exceeds 100 characters. [History](../../../src/Shared/History.fs) stays under 800 lines.

## 2. Spec

No findings. The prior finding Suffix detach beside a Normal child is closed. [History](../../../src/Shared/History.fs) allows a stub attach on the normal-change door only when every owned child on both lists is a document root. A Replace that drops a trailing child stays blocked on that door. Mailbox `postGraphOnly` uses [Change amendment](../../../src/Shared/ChangeAmendment.fs) `applyForGraphOnly`. That apply may append a suffix of owned document-root children on an Unparsed shell. [File agent](../../../src/Server/Core/FileAgent.fs) and [Db agent](../../../src/Server/Core/DbAgent.fs) call it for graph-only posts.

## 3. Match-check labels

These two labels were `missed`. Neither is a Spec gap.

1. **Push** — False positive. Directory reconcile names the Directory Node on the same `push` list a File Node already uses: disk-newer File ids, then disk-newer Directory ids, then ids of Directory Nodes just created. [Parse thread](../../../src/Server/ParseThread.fs) `afterPost` is not in this diff. It still calls `deps.push` for that list when the parse thread holds the Parse stack. That is the File Node rule on [06 — Setting Unparsed, recursive update](../issues/06-setting-unparsed-recursive-update.md).

2. **No axis edit** — False positive. This diff does not change the parse thread. Reconcile ops are `NewSpecialNode` and a child-list append. The disk-newer Directory test asserts those ops contain no `Op.SetDocumentState`, and the Directory Node stays Current and Parsed. The ids are only on `push`. The existing parse thread posts `InMsg.MarkUnparsed`. The core loop applies that InMsg. The parse thread does not edit the graph axes.

## 4. Totals

Standards: 0 findings. Spec: 0 findings.
