# 22 — Run sends = lines to Amble

**Type:** bug-fixing
**Status:** defined
**Blocked by:** None

## Context

Run chooses an Actor or Amble through a chain in [execRunOp](src/Client/Commands.fs). The chain is [submitQuery](src/Client/Commands.fs), then [isAmbleScanStop](src/Shared/CommandRequest.fs), then [tryStart](src/Shared/CommandRequest.fs), then [shouldExec](src/Shared/AmbleRun.fs). This ticket replaces that chain with one rule. Work is not started.

## Current behavior

[isCommandText](src/Shared/CommandRequest.fs) is `text.StartsWith("?")`. [isQueryText](src/Shared/CommandRequest.fs) is `text.Contains("=") && not (isCommandText text)`. [tryQueryStart](src/Shared/CommandRequest.fs) returns an ActorStart when the focused line is `isQueryText`. [submitQuery](src/Client/Commands.fs) queues that start, so [execAmbleRunOp](src/Client/Commands.fs) is never reached for a line that contains `=` and does not start with `?`.

[classify](src/Shared/ExprRun.fs) accepts a trimmed line that starts with `=`, or a name token then `=`. [isRunStatement](src/Shared/ExprRun.fs) is that result. [shouldExec](src/Shared/AmbleRun.fs) is true when the focused line is not a special Focus and the line is a run statement or the trimmed line starts with `>`.

## Target

[ ] A focused line that starts with `?` starts that Actor.
[ ] Else a focused line that contains `=` runs Amble on that line.
[ ] Else Run scans owners upward and applies the same two tests to the first owner that matches. A `?` line starts that Actor. A line that contains `=` runs Amble on that owner.
[ ] When no line matches up to the root, Run does nothing.

There is no query-by-`=` path. [11 — Server evaluates](11-server-evaluates.md) starts a query Actor when the line contains `=`. This ticket removes that start from the Run door.

## Owner path today

This scan differs from the code. [scanStopOnOwnerPath](src/Shared/CommandRequest.fs) walks owners from the Focus through the zoom root, and the Focus is included. It stops at the first `?` or `=` line. A `?` stop starts that Actor and keeps the focused Node as Focus. A `=` stop calls [execAmbleRunOp](src/Client/Commands.fs) on the focused Node, not on the matching owner. [shouldExec](src/Shared/AmbleRun.fs) then runs Amble only when that focused line is a run statement or starts with `>`. When nothing matches, Run stores the error `No command found`.

Remove that owner-path Amble call on the focused Node. The target runs Amble on the matching owner, scans to the root rather than the zoom root, and does nothing when nothing matches.

## Future consideration

`>` lines are out of scope. The implementer does not remove or change `>` handling.

## Finish-up

[ ] Amendments - changes during implementation are written on this ticket, or the ticket records that none changed.
[ ] Basis and reference docs — the ticket's basis, and the reference docs for each tracker named on Trackers, match those changes.
[ ] Documentation - files in [[doc/current]] and [[doc/user]] reflect what has been implemented by this ticket, deleting things no longer true and checking off things newly achieved.

## See also

[21 — Run consumer: `=` and `Name=` statements](plan/expression-language/issues/21-run-consumer-equals-and-name-equals-statements.md)
[33 — Recognize `?` as a Run statement](plan/expression-language/issues/33-recognize-ask-run-statement.md) treats `?` as an Amble statement. This ticket starts an Actor instead.
