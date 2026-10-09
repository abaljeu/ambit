# Code review — Server evaluates

Review of the working tree for [Server evaluates](../issues/11-server-evaluates.md).

## 1. Standards

The scan printed `let mutable found` in [QueryActorTests](../../../tests/Server.Tests/QueryActorTests.fs). [F# source](../../../.agents/rules/fsharp-source.md) does not apply that rule to tests. Function sizes in the diff are under 40 lines.

No findings.

## 2. Spec

The first pass found that [ExprRun.answerNodeIds](../../../src/Shared/ExprRun.fs) used [ExprCompile.eval](../../../src/Shared/ExprCompile.fs) without [ExprCompile.inferType](../../../src/Shared/ExprCompile.fs). A type error could still return Node ids. The tree now returns no ids when inferType fails. [ExprRunTests](../../../tests/Shared.Tests/ExprRunTests.fs) asserts that empty list for `= root text child`.

No open findings.

Standards: 0 findings. Spec: 0 open findings. Worst on Standards: none. Worst on Spec: none.
