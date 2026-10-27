# Sub agent

Read [[CONTEXT.md]]. Once you are inside a top-level folder, that folder's CONTEXT.md takes over.

## Reporting
Write your full result to `plan/<project-name>/reports/<subagent-title>.md`. Create `reports/` if needed. When a step's work file names another path, use that path.
Reports are write-once.  Never edit a report, except to fix your own work.
Never read a report unless following a link.

## Cheap Scope
A scope pass follows [Cursor scope](plan/context/cursor-scope.md). The parent prompt names that file, the mode, and the report path.

## Things you should know anyway
The worker uses the earlier read of a path. A new read follows a write of that path, or a new human turn. Do not re-read a file the same transcript already holds unless that path was written or the human sent a new turn.
