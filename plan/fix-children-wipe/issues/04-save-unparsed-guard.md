# 04 — Guard Save on an Unparsed Node

**Status**: coded
[01 — Run the event-log queries](01-run-event-log-queries.md) confirmed this ticket's cause; see the Findings on [[../project.md]].

## Scope

`GithubTransportActor.focusCanBeParsed` lets Save flip an Unparsed File or Directory focus to Parsed without reading its disk content first, then persist writes empty content over it. Commit `3a1a0aa8` emptied [07 — Document the command-line actor](../../command-line-actor/issues/07-document-the-command-line-actor.md) this way. Save on an Unparsed focus must parse that focus from disk first, or refuse the Save.

## Done

[x] Save on an Unparsed File or Directory focus parses it from disk before any persist write.
[x] A focus whose disk read fails, or that is gone from disk, refuses the Save instead of persisting empty content.
[x] A test Saves an Unparsed focus with real disk content and asserts the persisted content is not empty.
[x] A test Saves an Unparsed focus whose disk read fails and asserts the Save is refused, not silently emptied.

## Stop

[ ] Stop before ticket 01's results confirm the issue-07 loss matches this path; this ticket does not start before that confirmation.
[ ] Stop if the fix changes Save on a focus that is already Parsed.
[ ] Stop if the change reaches outside `GithubTransportActor.fs` plus its direct callers and tests.
