# 04 — Use a placeholder

Ticket: [[plan/client-never-fails/issues/04-use-placeholder/04-use-placeholder-ticket.md]]

Spec: [[plan/client-never-fails/spec.md]]

Arch: [[plan/client-never-fails/arch.md]]

Review: [[04-review.md]]

## Stage

Stage is refactor in [04 — Use a placeholder](../CONTEXT.md).

Ticket status is `coded`.

## 1. Selection ids

`rangeNodeIds` in `src/Client/UpdateOps.fs` is 4 lines (508–511). It is the id list that `selectedNodeIds`, `copyOp`, and `onCopy` each built.

- **rangeNodeIds.** 4 lines (508–511).
- **copyOp.** `copyOp` in `src/Client/Commands.fs` is 13 lines (303–315). It was 15 lines.
- **onCopy.** `onCopy` in `src/Client/Controller.fs` is 9 lines (134–142). It was 10 lines.
- **openCssClassPromptOp.** `openCssClassPromptOp` is 10 lines (513–522).
- **submitCssClassPromptOp.** `submitCssClassPromptOp` is 37 lines (560–596).

## 2. Class list

`tryInitialUserClasses` in `src/Shared/CssClassToggle.fs` is 20 lines (29–48). It was 16 lines. The inner `read` is 7 lines (33–39). Each node's user classes are one group. The groups reverse once, then `List.concat` and `List.distinct` keep the first-seen order.

The missing-Header `Map.tryFind` stays in `tryInitialUserClasses`. `src/Shared/CommandRequest.fs` compiles after `src/Shared/CssClassToggle.fs`.

## 3. Left in place

- `execRunOp` in `src/Client/Commands.fs` stays 40 lines (124–163).
- Arch section 1, story path 4, step 3 still says Copy, Edit classes, and Exec stay unchanged. The review Miss item stands.
- Cut still reads the node map by index.

## 4. Fact

``tryInitialUserClasses keeps first-seen user class order`` expects `Some "a b c"` for user classes `a b` then `b c`, and `None` when the second id has no Header.

## 5. Suite

- **Command.** `bash scripts/check-test.sh 10828` printed `DONE` and `/tmp/ambit-full-suite-20261101-124557.log`. The log ends `full suite finished`.
- **Server.** `tests/Server.Tests` passed 733, skipped 1, failed 0.
- **Shared.** `tests/Shared.Tests` passed 1924, skipped 3, failed 0.
- **Scan.** `python3 scripts/standards-scan.py --diff HEAD` printed `rangeNodeIds` at 4 lines. The scan printed no limit hit.
