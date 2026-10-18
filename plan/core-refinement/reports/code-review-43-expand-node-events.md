# Code review — 43 Expand Node events beside the Set output

- Range: `git diff HEAD` plus untracked `src/Server/Core/NodeEventBeside.fs` and `tests/Server.Tests/NodeEventExpandTests.fs`
- Spec: [43 — Expand Node events beside the Set output](../issues/43-expand-file-events-beside-the-set-output.md)
- Arch: [core-refinement architecture](../arch.md), [Parse thread architecture](../../parse-thread/arch.md)

## 1. Standards

1. No findings.
2. Scan: [Parse thread architecture](../../parse-thread/arch.md) Uses **NodeParsed** names [core-refinement architecture](../arch.md) §5 **Directory Parse done** on the same line, so refer-by-name holds.
3. Scan: `acceptedChangeResult` is 22 lines. `MailboxHost.create` is 11 lines. Both are within 40 lines.
4. The three acceptance tests that were over 40 lines were split with helpers. Each test function is 40 lines or fewer.

## 2. Spec

1. **Extra** — A retried `submissionId` returns Ok without applying `Op.SetDocumentState` again, and `Api.postEvents` still posts the Node message from the request body. The ticket posts beside the write.
2. **Partial** — Beside posts run when `CoreDoor.postEvents` returns Ok. If an earlier event in that request already applied and a later event fails, the client sees an error and those applied Changes get no `InMsg`.

## 3. Arch

1. No findings.

## 4. Totals

- Standards: 0 findings. The scan notes are named above.
- Spec: 2 findings. Worst: a retried submission logs the Node message again.
- Arch: 0 findings. Worst: none.
