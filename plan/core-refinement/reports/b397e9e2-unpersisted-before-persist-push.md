# b397e9e2 — Set a new special node Unpersisted before its persist push

## Commit

- Full hash: `b397e9e205964ff8b138b3ebdbd73c0694143d58`
- Author: abaljeu \<alanbaljeu@yahoo.com\>
- Committer: GitHub \<noreply@github.com\>
- Date: 2026-10-21 19:05:58 -0400
- Parent: `e433ababb7be8eda0a08ea0fd64d4408c959a02a` (one parent)
- Subject: Set a new special node Unpersisted before its persist push (#337)
- Place: tip of `dev`, `staging`, and `origin/staging`. No later commit on those branches.
- Kind: one ordinary commit from pull request 337. It is not a merge commit and not a revert.

## Ticket

[71 — Persist thread takes from the stack](../issues/71-persist-thread-takes-from-the-stack.md) is `coded`.

The ticket asks for three steps, in order:

1. `DocumentPersistChange.persistNode` writes one node's document file.
2. `PersistRequest` holds `nodeId` only. The persist thread reads live state through `currentState`.
3. `FileAgent` and `DbAgent` push one request per node. The thread takes that id, skips a node that is already Persisted, calls `persistNode`, and posts `Notice.NodePersisted` or `Notice.PersistFailed`. The collector queue leaves.

Those three steps are checked. The time log on the ticket records them earlier on 2026-10-21, in commits `eeceaa28` (step 1, #326), `1fa7b81c` (step 2, #328), and `f32f7d61` (step 3, #330). The open item is **Still working**: upload, import, parse, Load, undo, and persist still complete, and the server starts no extra thread.

## What b397e9e2 did

Files:

- [FileAgent.fs](../../../src/Server/Core/FileAgent.fs)
- [FileAgentFailureTests.fs](../../../tests/Server.Tests/FileAgentFailureTests.fs)

`persistCollected` already found the owning special nodes and called `openForWrite` before `PersistRequestStack.push`. The parent commit already set a Persisted node to Unpersisted in that helper, with a private map update, so the thread's "skip if Persisted" rule would still write a new special node. A new special node starts Persisted (`Node.Create` defaults `persistState` to Persisted).

This commit moves that mark:

- `persistedNode` reads `node.persistState`.
- `markUnpersisted` calls `GraphMutate.setPersistState` with `PersistState.Unpersisted`.
- `setPersistState` accepts an artifact node. It refuses a missing node, the Workspaces node, a normal node, and a directory file. On that error, `markUnpersisted` keeps the previous graph.
- `openForWrite` writes the graph back onto `loaded.state` on every call.
- The unused `replaceGraph` parameter is renamed to `_notice`.

The new test `new special node writes its file without NodePersisting` creates a workspace, waits for `Notice.NodePersisted`, asserts that `Notice.NodePersisting` is absent for that id, and asserts that the artifact file exists. The in-memory mark does not post `Notice.NodePersisting`. The thread still posts `Notice.NodePersisted` after the write.

## Verdict

This commit relates to the ticket. It does not implement the ticket.

The three build steps stay in the earlier commits. This commit does not change `PersistThread`, `PersistRequest`, or `persistNode`. It adjusts the FileAgent mark that runs immediately before the stack push from step 3, and it locks that mark with one test. It does not close **Still working**.
