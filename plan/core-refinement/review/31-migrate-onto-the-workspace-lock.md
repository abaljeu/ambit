# 31 — Migrate onto the workspace lock

Ticket: [[../issues/31-migrate-onto-the-workspace-lock.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- **Hard** — [[.agents/rules/fsharp-source.md]] groups related parameters into a named type instead of lengthening the argument list. `commitInLock`, `pullTracked`, and `commitAndPull` in [Github transport actor](../../../src/Server/GithubTransportActor.fs) add `graph` and `workspaceId` beside `root`.
- **Judgement** — Duplicated Code. The same `HolderHoldsLock` flatten appears in `DocumentPersistWrite.underLock`, `ParseThread.reconcileDirectory`, and `GithubTransportActor.commitInLock`. Source: [[../../context/SMELLS.md]]

```fsharp
match NodeLockPool.withLock graph nodeId action with
| Error NodeLockRejection.HolderHoldsLock ->
    Error "holder already holds a lock"
| Ok result -> result
```

- `python3 scripts/standards-scan.py --diff origin/staging` printed the new functions with their line counts. Each count is under the 40-line limit. No GLOSSARY.md PHRASE line.

## 2. Spec

- **Extra** — `persistGraphChangeWith` in [Document persist change](../../../src/Server/DocumentPersistChange.fs) no longer holds a lock across path moves. [09 — Relocate under the node lock](../../node-move-disk/issues/09-relocate-under-the-node-enclosure-lock.md) is the move lock.
- **Extra** — A same-thread second `withLock` on Persist, parse, or git returns the string `holder already holds a lock`. The pool rejection is `HolderHoldsLock`.
- No Missing, Partial, or Wrong requirement. Persist, parse reads, Directory reconcile, and git Load and git Save call `NodeLockPool.withLock`. The git Save commit is inside that lock. Push stays outside it. `withWorkTreeGate` and the `GitRun` private gate are gone. Fetch-first pull stays on [32 — Workspace lock handoff](../issues/32-workspace-lock-handoff.md).

## 3. Arch

- No findings.

## 4. Tests

- ``Persist write waits while the Workspace lock is held`` in [Workspace git tests](../../../tests/Server.Tests/WorkspaceGitTests.fs) shows `writeDocument` waits on the Workspace node.
- ``parse reads wait while the Workspace lock is held`` shows `planParseFile` and Directory reconcile wait on that lock.
- ``git Load and git Save wait while the Workspace lock is held`` in [GitHub transport load pull tests](../../../tests/Server.Tests/GithubTransportLoadPullTests.fs) shows the Load commit and the Save commit wait.
- ``Persist and git paths have no work tree gate`` and ``gitExec has no private work tree gate`` show `withWorkTreeGate` absent.
- `bash scripts/check-test.sh 8040` prints `DONE` and `/tmp/ambit-full-suite-20261110-183104.log`.
- Server.Tests passed 762, skipped 1, total 763.
- Shared.Tests passed 1937, skipped 3, total 1940.

## 5. Miss

- No findings.

## 6. Found elsewhere

- **Problem.** [Parse thread architecture](../../parse-thread/arch.md) item 9 **Locks** is ticked, and the line still names [09 — Relocate under the node lock](../../node-move-disk/issues/09-relocate-under-the-node-enclosure-lock.md).
- **Fix.** Point that line at [31 — Migrate onto the workspace lock](../issues/31-migrate-onto-the-workspace-lock.md).
- **Decision.** Reject until Alan confirms. 09 still owns relocate. The checkbox records the call 31 added.
