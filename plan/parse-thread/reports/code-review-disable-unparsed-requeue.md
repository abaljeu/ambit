# Code review — disable Unparsed requeue

Range: the tip diff against staging. No ticket. The spec is Alan's temporary-remedy request. Binding architecture: [Parse thread architecture](../arch.md), [core-refinement architecture](../../core-refinement/arch.md), [github-transport architecture](../../github-transport/arch.md).

## 1. Standards

1. **Function length** — Hard. [F# source](../../../.agents/rules/fsharp-source.md) says 40 lines or less per function. ``disk-newer file stays Unparsed and is not requeued`` is 83 lines in [Parse thread load tests](../../../tests/Server.Tests/ParseThreadLoadTests.fs). On the base it was already 61 lines. This diff made it longer. The tests exemption in that rule is the 800-line file limit.

2. **Duplicated code** — Judgement. [Smell baseline](../../../.agents/skills/code-review/SMELLS.md) Duplicated Code. The same hold rationale is copied above `requeueOnUnparsed`, inside `enqueueIfRequeue`, inside `afterPost` in [Parse thread](../../../src/Server/ParseThread.fs), and above the renamed fact. The request asked for that reason at each disabled site and in the test.

3. **Speculative generality** — Judgement. The same smell baseline. `requeueOnUnparsed` is constant false, and `enqueueIfRequeue` does not run its body. The request asked for one flag so re-enable is a one-line change.

The mechanical scan listed `requeueOnUnparsed` (2 lines) and `enqueueIfRequeue` (10 lines before the helper comment was filled in). Both stay under the 40-line limit. No added line exceeds 100 characters.

## 2. Spec

No findings.

## 3. Arch

1. **Push when the stack exists** — Blocker. [Parse thread architecture](../arch.md) §1 Story paths, item 16 **Push when the stack exists**. Directory reconcile pushes that Unparsed File Node when the Parse stack exists. In [Parse thread](../../../src/Server/ParseThread.fs), `requeueOnUnparsed` is false. `afterPost` still calls `deps.markUnparsed`, then `enqueueIfRequeue` skips `deps.push`.

2. **Directory into Graph** — Blocker. Same file, §1 item 23 Shared segments, item 2 **Directory into Graph**, and §2 Module map, item 1 **Directory reconcile**, Uses item 2 **Parse stack**. Same `afterPost` site. The stack push does not run.

3. **Directory reconcile push** — Blocker. [core-refinement architecture](../../core-refinement/arch.md) §3 step 2 **Parse stack**, Migrate item 4 **Directory reconcile**; §5 Axis-write mechanics, item 10 **Directory reconcile**; §9 Story paths, item 2 **Parse stack**, Migrate item 4 **Directory reconcile**. Each checked rule says a disk-newer File Node is marked Unparsed through `InMsg` `MarkUnparsed` and is pushed. The mark still runs. The push does not. [Parse thread load tests](../../../tests/Server.Tests/ParseThreadLoadTests.fs) ``disk-newer file stays Unparsed and is not requeued`` asserts the disk text never lands.

4. **Parse thread runs when Unparsed** — Blocker. Same file, §1 Special-node Parse and Persist axes, item 1, and §6 Core locking model, item 3 **Parse thread**. The parse thread runs when a node is Unparsed. Those ids stay Unparsed and are not queued, so the consumer does not run them.

[github-transport architecture](../../github-transport/arch.md) holds. The Load inform-Core handoff is outside this diff. §1 story path 17 item 4 leaves the Parse stack to core-refinement.

## 4. Totals

Standards: 3 findings, worst is function length on the renamed fact. Spec: 0 findings. Arch: 4 findings, worst is the skipped Parse-stack push after Unparsed.
