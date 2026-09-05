# Spec review: initial Core Changes

## Result

Spec passes. I found no Spec-axis findings in git diff HEAD plus the untracked [[src/Server/Core/CoreChanges.fs]], [[src/Server/Core/GraphAgentHandle.fs]], and [[tests/Server.Tests/CoreChangesTests.fs]].

## (a) Missing or partial requirements

None.

## (b) Behavior not asked for

None.

## (c) Implemented requirements that appear wrong

None.

## Key requirements checked

- Normal and Graph-only accept typed Change lists and return the same typed accepted facts or text Reject. Core transport types are absent.
- GraphAgentHandle replaces AgentHandle, owns production construction and selection, and is the only production Change capability used outside the agent modules.
- The HTTP Adapter retains authentication, client hint, body read, JSON decode and encode, protocol fields, and HTTP status mapping.
- Parse, lazy-load reconciliation, and git reconciliation reach typed Graph-only calls without internal Change JSON.
- Existing agent logic still provides acknowledgement, Reject, amendment, deduplication, persistence, Poll publication, readiness, and the eight-second timeout.
- The mirror has only typed signature adaptation. No issue 01 Server Actor, issue 13 selector change, Files, Query, Command, Actor pool, ACID, startup, repair, or new Graph/file behavior appears.
- The two new seam tests cover direct non-HTTP Normal then Poll and the recording HTTP Adapter. The existing Graph-only test records typed Changes.

## Verification

The four specified focused test groups passed: 40, 15, 91, and 3 tests. The Server build succeeded with 0 warnings and 0 errors. git diff --check passed.

Finding count: 0. Worst issue: none.
