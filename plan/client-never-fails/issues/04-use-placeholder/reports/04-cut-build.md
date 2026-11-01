# 04 — Use a placeholder — Cut

Ticket: [04 — Use a placeholder](plan/client-never-fails/issues/04-use-placeholder/04-use-placeholder-ticket.md)

Spec: [Client never fails on server data](plan/client-never-fails/spec.md)

## Before

[ ] `collectSubtree` in `src/Shared/Paste.fs` read `graph.nodes.[nodeId]` and `graph.nodes.[topId]`.
[ ] A selected child id with no Header threw `KeyNotFoundException` from that read.
[ ] `onCut` in `src/Client/Controller.fs` called `onCopyOrCut`, which dispatched `cutSelectionOp`.
[ ] `cutSelection` in `src/Client/UpdatePaste.fs` stored a clipboard and posted a remove.

## After

[ ] `readClipboardNode` reads the Header with `Map.tryFind`. A missing Header is `None`.
[ ] `collectSubtree` skips that id. The clipboard lists, node map, and child map stay empty for that id.
[ ] `onCut` returns before clipboard and dispatch when `anyNodeHeaderIsAbsent` is true.
[ ] `cutSelection` returns the same model and no effects when a selected Header is absent.
[ ] Spec story 4 and the ticket One job list Cut with Copy, Edit classes, and Exec.

## Fact

[ ] ``Cut does nothing when the selected row Header is absent`` in `tests/Shared.Tests/PasteTests.fs` builds a child id with no Header, calls `collectSubtree`, and expects empty clipboard lists.

## Suite

[ ] `scripts/agent-test.sh --filter` for ``Cut does nothing when the selected row Header is absent`` failed first with `KeyNotFoundException` at `Paste.fs` line 98, then passed.
[ ] `scripts/agent-test.sh --filter FullyQualifiedName~PasteTests` passed 16.
[ ] `scripts/agent-test.sh --full` log `/tmp/ambit-full-suite-20261101-132523.log` ends `full suite finished`.
[ ] `tests/Server.Tests` passed 733, skipped 1, failed 0.
[ ] `tests/Shared.Tests` passed 1925, skipped 3, failed 0.
