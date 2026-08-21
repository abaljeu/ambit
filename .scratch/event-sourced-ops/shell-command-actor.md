# Shell command as a later Actor

Prospective. Not a product spec. Not locked. No Gambol shell-job API exists (only host `ProcessExec` / git).

Same kind as Parse File ([[parse-file-actor.md]]): pool `Task` **off** the file mailbox, conclude with `Change`(s), **post** inner apply, Browsers **Poll** + rewind+replay ([[job-launch-apply-cancel.md]], [[in-process-apply.md]]).

Parse is the pathfinder (Merge/ACK realignment). Shell is a **new** instance.

**New vs Parse:** N concurrent jobs; Client keeps **job ids**; **cancel** (`CTS` before `Post*`). Soft-lock: same accepted meaning — advisory checkout of the job's subtree ([[soft-lock.md]]). Stdout/result as node text vs parse Ops — unspec'd.

## WORK.md

Add this file to the Active [[project.md]] related list. Stage `charting`.
