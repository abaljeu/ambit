# 72 — Push parsed nodes to the persist stack

Ticket: [[../issues/72-push-parsed-nodes-to-the-persist-stack.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- 1. **Duplicated push** — Judgement. Smell Duplicated Code. `FileAgent.pushPersist` and `DbAgent.pushPersist` match the bound stack and push `{ nodeId = nodeId }`.

```fsharp
let private pushPersist (loaded: LoadedFile) (nodeId: NodeId) =
    match loaded.stack.Value with
    | None -> ()
    | Some stack ->
        PersistRequestStack.push stack { nodeId = nodeId }
```

```fsharp
let private pushPersist (loaded: LoadedPersist) (nodeId: NodeId) =
    match loaded.stack.Value with
    | None -> ()
    | Some stack ->
        PersistRequestStack.push stack { nodeId = nodeId }
```

- 2. **Duplicated check** — Judgement. Smell Duplicated Code. `CoreMailboxLoad.parsedAndUnpersisted` and the `when` guard in `DbAgent.noteParsed` both require Parsed and Unpersisted. The mailbox uses that check for the push. `DbAgent.noteParsed` uses it for the file-diff request the ticket keeps.

```fsharp
let private parsedAndUnpersisted (node: Node) =
    node.parseState = ParseState.Parsed
    && node.persistState = PersistState.Unpersisted
```

```fsharp
| Some node when
    node.documentState.parseState = ParseState.Parsed
    && node.documentState.persistState =
        PersistState.Unpersisted ->
```

- 3. **Standards scan** — `python3 scripts/standards-scan.py --diff origin/staging` exited 0. `python` is absent on this machine, so the scan ran under `python3`. The printed sizes are `parsedAndUnpersisted` at 4 lines, `pushParsedUnpersisted` at 12 lines, `applyNodeParsed` at 13 lines, and both `pushPersist` functions at 6 lines. Each sits under the 40-line function limit in `.agents/rules/fsharp-source.md`.

## 2. Spec

- No findings.

## 3. Arch

- No findings.

## 4. Tests

- 1. **Full suite** — `scripts/agent-test.sh --full` first wrote `/tmp/ambit-full-suite-20261023-014318.log`. Server.Tests passed 741, failed 1 (`TestActor hello drops live row after successful stop`: the live row was absent right after `startActor` in `tests/Server.Tests/TestActorHelloTests.fs`). The script stopped before Shared.Tests. That test passed on its own next run. A second `--full` wrote `/tmp/ambit-full-suite-20261023-014956.log` and printed `full suite finished`. Server.Tests passed 742, failed 0, skipped 0. Shared.Tests passed 1928, failed 0, skipped 1 (`WorkspaceLocalInventoryTests.listForPush without git still returns walk`).
- 2. **Push** — `NodeParsed pushes a persist request` in `tests/Server.Tests/CoreLoopNoticeTests.fs` records the node `Notice.NodeParsed` leaves Parsed and Unpersisted, including a node that was already Parsed. The Persisted node, the PullingFile node, and the missing node stay off that list.
- 3. **Persisted** — `NodeParsed leaves the written node Persisted` in `tests/Server.Tests/PersistThreadTests.fs` applies `Notice.NodeParsed`, the persist thread writes the file, and the node is Persisted and Parsed.
- 4. **Removed names** — `scheduleCatchUp` and `catchUpParsed` are absent under `src/` and `tests/`.

## 5. Miss

- No findings.
