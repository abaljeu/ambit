# 04 — Guard Save on an Unparsed Node

**Status**: coded
[01 — Run the event-log queries](01-run-event-log-queries.md) confirmed this ticket's cause; see the Findings on [[../project.md]].

## Scope

`GithubTransportActor.focusCanBeParsed` let Save flip an Unparsed File or Directory focus to Parsed without reading it, then persist wrote empty content over it. Commit `3a1a0aa8` emptied [07 — Document the command-line actor](../../command-line-actor/issues/07-document-the-command-line-actor.md) this way. The only Save rule: Save on an Unparsed focus (File, Directory, or Workspace) is refused with an Error before any persist write, commit, pull, or push. A file or folder missing from disk is not an Unparsed case; Save on a Parsed focus proceeds as it did before this ticket even when its file or folder is missing from disk.

## Done

[x] Save on an Unparsed File, Directory, or Workspace focus is refused with an Error before any persist write, commit, pull, or push.
[x] Save on a Parsed focus proceeds unchanged, including when its file or folder is missing from disk.
[x] A test Saves an Unparsed File focus and asserts the Save is refused, the file on disk is unchanged, and no commit runs.
[x] A test Saves an Unparsed Directory focus and asserts the same.
[x] A test Saves a Parsed Directory focus whose folder is missing from disk and asserts the Save is not refused.

## Stop

[ ] Stop before ticket 01's results confirm the issue-07 loss matches this path; this ticket does not start before that confirmation.
[ ] Stop if the fix changes Save on a focus that is already Parsed.
[ ] Stop if the change reaches outside `GithubTransportActor.fs` plus its direct callers and tests.
