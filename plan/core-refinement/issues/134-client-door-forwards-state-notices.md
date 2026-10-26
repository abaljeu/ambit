# 134 — The client door forwards state Notices

## 1. Type

coding

## 2. Blockers

None.

## 3. Problem

A client producer (upload or import) cannot post a Notice. `CoreEventDispatch.completeAction` rejects `EventBody.Notice` with "Notice is not a client event type". #401 relaxed that for every Notice, and the review flagged it.

## 4. Change

- `Api.postEvents` takes state Notices from the request's events. It posts the leading Notices through `CoreMailboxApi.postNotice` before `CoreDoor.postEvents`, and the trailing Notices after `Ok`. Placement is the rule in [135 — Op readers leave](135-op-readers-leave.md).
- It accepts only a Notice for which `Ev.documentStateFromNotice` is `Some`, and rejects any other Notice.
- `completeAction` is unchanged.
- No op line is deleted.

Source: `Api.fs`.

## 5. Tests

[ ] `NodeEventExpandTests`: a request with a lead `NodeParsed` and a trail `NodeUploading`; a non-state Notice is rejected

## 6. Open question (Alan)

1. Should `Api.postEvents` take state Notices from any logged-in caller for any node? Or only for a node named by that request's Change, or for a node that already exists?
2. When a request is only Notices, what does `Api.postEvents` reply? The current event id with no new events?

## 7. Done

[ ] Run `scripts/agent-test.sh` with no arguments.
[ ] Run `scripts/agent-test.sh --filter` for each test above that the default run missed.
[ ] All executed tests pass.
