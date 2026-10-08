# Code review — 11 Server evaluates — PR 215

Review of `git diff origin/staging...HEAD` for [11 — Server evaluates](../issues/11-server-evaluates.md). Node ids ride ActorStop as `ActorQuery`. The applied chip keeps them.

## Standards

No findings.

The scan's `item N` lines name the item. `let mutable found` is in a test. [F# source](../../../.agents/rules/fsharp-source.md) does not apply that rule to tests. New functions are under 40 lines.

No smells.

## Spec

No missing requirement inside this ticket. The Run race stays out of this diff. Alan assigned it to a follow-on ticket. That deferral is not acceptance.

[CmdLastResult](../../../src/Shared/ViewModel.fs) case `Query` was unnamed. Section 4 **Reply** now names `CmdLastResult.Query`. Find stays the `ActorSucceeded` chip.

Standards: no findings. Spec: no findings inside this ticket. The Run race is a follow-on gap, not accepted.
