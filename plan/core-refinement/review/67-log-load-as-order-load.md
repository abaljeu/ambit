# 67 — Log Load as Order.Load

Ticket: [[../issues/67-log-load-as-order-load.md]]
Spec: [[../spec.md]]
Arch: [[../arch.md]]

## 1. Standards

- 1. **Argument count** — Judgement. Smell Argument count. `replyLoad` and `placeOrder` in `src/Server/Core/CoreMailboxLoad.fs` each take five parameters. One record type holds that clump. `.agents/rules/fsharp-source.md` groups related parameters into a reused type.

```fsharp
let private replyLoad
    (context: MailboxContext)
    (caller: Caller)
    (orderId: Guid)
    (subject: NodeId)
    (reply: AsyncReplyChannel<Result<EventId, string>>)
```

```fsharp
let private placeOrder context caller orderId order reply =
```

- 2. **Standards scan** — `python3 scripts/standards-scan.py --diff origin/staging` exited 0. `python` is absent on this machine, so the scan ran under `python3`. The printed sizes are `replyLoad` at 20 lines and `placeOrder` at 12 lines, both under the 40-line function limit in `.agents/rules/fsharp-source.md`.

## 2. Spec

- No findings.

## 3. Arch

- No findings.

## 4. Tests

- 1. **Full suite** — `scripts/agent-test.sh --full` wrote `/tmp/ambit-full-suite-20261023-012036.log` and printed `full suite finished`. Server.Tests passed 740, failed 0, skipped 0. Shared.Tests passed 1928, failed 0, skipped 1 (`WorkspaceLocalInventoryTests.listForPush without git still returns walk`).
- 2. **Round trip** — `Load keeps its node through EventJson and MailboxLog` in `tests/Shared.Tests/MailboxLogTests.fs` keeps the node id through `EventJson` and `MailboxLog`, and Undo of that history targets the prior edit.
- 3. **Logged** — `loadNode logs Order.Load` in `tests/Server.Tests/MailboxExpandTests.fs` checks `Ok` of an `EventId` whose stored body is `EventBody.Load` of that file.
- 4. **Push** — `loadNode pushes the subject` checks that file on the parse stack after the reply, and parsing it reaches `Notice.NodeParsed`.
- 5. **Rejected** — `loadNode of a plain Node replies Error` checks that error and an unchanged log.

## 5. Miss

- No findings.
