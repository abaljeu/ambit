# Spec re-review — [51 — Browser Run: Focus reply parent, Command is runnable ancestor](plan/core-creation/issues/51-browser-run-focus-vs-command.md)

Independent Spec axis vs the locked ticket and architecture. Not approval. Ticket Status stays `coded`. Range: `git diff origin/staging...HEAD` (non-empty). Tip `1186d617`. `origin/staging` `b484d99e`. Locked after the first review: a `=` scan-stop is Amble Run, not ActorStart. The locked ticket/arch text is the spec.

Focused tests green: [CommandRequestTests](tests/Shared.Tests/CommandRequestTests.fs) 18 passed; [TestActorHelloTests](tests/Server.Tests/TestActorHelloTests.fs) 9 passed.

## 1. Missing or partial

None. [51 — Browser Run: Focus reply parent, Command is runnable ancestor](plan/core-creation/issues/51-browser-run-focus-vs-command.md) What to build (encode + proof) is in the tip.

Scan: ticket Intent “Owner-scan Focus → zoom stops at the first node whose text starts with `?` **or** contains `=`.” [CommandRequest](src/Shared/CommandRequest.fs) `isScanStopText` / `scanStopOnOwnerPath`. Test `scan stops at first equals and does not skip to ?`.

`?` ActorStart: ticket “A `?` stop is ActorStart.” Same Intent: “Actor Command = nearest `?` owner-ancestor”; “reply becomes a Child of Focus.” `tryStart` is Ok; [Commands.fs](src/Client/Commands.fs) `SubmitCommand`. Distinct `focusId` / `commandId` / `zoomId`. Server hello Owned child under the question Focus; question text remains. One-Node hello when Focus is the Command.

`=` Amble: ticket encode 4 “If the scan-stop is a `=` line (not a `?` Actor Command), Amble Run only — do not ActorStart / SubmitCommand.” [core-creation arch](plan/core-creation/arch.md) **Browser Run** item 3: “If that node contains `=` and is not a `?` Actor Command, Amble Run only — do not ActorStart.” [llm-connector arch](plan/llm-connector/arch.md) Locked item 8 **Focus vs Command on Run**: “A `=` stop is Amble Run only; it is not ActorStart.” `isAmbleScanStop` → `execAmbleRunOp`; `tryStart` Error; no `SubmitCommand`. Proof `count=1+2` with Focus and under Focus.

Plan/arch/ticket updated. Status `coded`.

## 2. Scope creep

None in product code. `execAmbleRunOp` is the prior Amble body. `oneNodeStart` stays for hello. Non-goals (Amb write algorithm, Actor live label TitleCase, Core door widen) are not in the code diff.

## 3. Looks implemented but wrong

None. `commandOnOwnerPath` returns the scan-stop only when text starts with `?`. A `=` stop is not Actor Command. Text that starts with `?` and also contains `=` stays Actor Command (arch item 3: “If that node starts with `?`, it is the Actor Command”).

## 4. Summary

(a) 0, (b) 0, (c) 0. Worst in Spec: none.

Spec-axis verdict: Approve
