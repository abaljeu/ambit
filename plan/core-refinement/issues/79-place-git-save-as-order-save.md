# 79 — Place git Save as Order.Save

**Status:** `defined`
**Type:** coding
**Blocked by:** [67 — Log Load as Order.Load](67-log-load-as-order-load.md)
**Trackers:** architecture

## Context

`GithubTransportActor.runOperation` runs git Load and git Save. For `GithubTransportOperation.Save` it calls `saveTracked`, which commits the Workspace and pushes, and returns `Ok ()` or the push `Error`. git Save appends no `Ev`. `CoreDoor.loadNode` calls `CoreMailboxApi.mbLoadNode`, which places `Order.Load` through `Mailbox.PlaceOrder`. `Order` is in `src/Shared/MailboxLog.fs`. `EventBody` is in `src/Shared/History.fs`. `Ev.ops` and `Ev.target` skip the bodies that carry no ops.

This ticket is decision 2 **Load is logged** and decision 19 **One case each** in [Message shape](../cqrs-messages.md). Load and Save have the same shape. git Save is an order: the mailbox assigns and logs its `EventId`, then the push runs. Save never Loads or parses.

## What to build

Do these steps in order. Finish each step before starting the next.

1. [ ] **Stored body** — Add `Order.Save of nodeId: NodeId` and `EventBody.Save of nodeId: NodeId`. `EventJson` writes and reads `EventBody.Save`. `MailboxLog.ofEv` and `MailboxLog.toEv` map `EventBody.Save` and `Order.Save` both ways. `Ev.ops` and `Ev.target` skip `EventBody.Save` beside `EventBody.Load`, so Undo skips it. Every exhaustive `EventBody` match names `Save` beside `Load`: `PersistStamp`, `ActorLive`, `CoreEventDispatch.completeAction`, `CoreMailboxEvents.withAppliedOps`, and `MailboxHistoryDurabilityTests.bodyKind`. Add `Save keeps its node through EventJson and MailboxLog`. Files: `src/Shared/History.fs` (where `EventBody` is defined, and the Undo skip), `src/Shared/EventJson.fs`, `src/Shared/MailboxLog.fs` (where `Order` is defined), `src/Shared/PersistStamp.fs`, `src/Shared/ActorLive.fs`, `src/Server/Core/CoreEventDispatch.fs`, `src/Server/Core/CoreMailboxEvents.fs`, `tests/Shared.Tests/MailboxLogTests.fs`, `tests/Server.Tests/MailboxHistoryDurabilityTests.fs`. Done when it compiles and `Save keeps its node through EventJson and MailboxLog` passes.
2. [ ] **Place and handle** — Add `CoreMailboxApi.mbSaveNode`, which places `Order.Save` of the subject through `Mailbox.PlaceOrder` and returns `Result<EventId, string>`. Add `CoreDoor.saveNode` beside `CoreDoor.loadNode`. The `PlaceOrder` arm of `CoreMailboxLoad.dispatchMailbox` for `Order.Save` appends one `Ev` of `EventBody.Save` of the subject and replies `Ok` of that `EventId`. The handler does not Load, parse, or push. Add acceptance test 1. Files: `src/Server/Core/CoreMailboxApi.fs`, `src/Server/Core/CoreMailboxLoad.fs`, `src/Server/Core/CoreDoor.fs`, `tests/Server.Tests/MailboxExpandTests.fs`. Done when `saveNode logs Order.Save` passes.
3. [ ] **git Save** — `GithubTransportActor.runOperation` for `GithubTransportOperation.Save` calls `CoreDoor.saveNode` of the Workspace first. On `Ok` of the `EventId` it runs `saveTracked` and returns `Ok` of that `EventId`. A push error returns `Error`. An `Error` from `saveNode` returns that `Error` and runs no push. `runOperation` keeps its name and returns `Result<EventId, string>` for both Load and Save; Load returns the `EventId` that `CoreDoor.loadNode` replies. `runBody` maps `Ok` to `ActorSucceeded`. Add acceptance tests 2 and 3. Files: `src/Server/GithubTransportActor.fs`, `tests/Server.Tests/GithubTransportActorTests.fs`. Done when `git Save logs Order.Save before the push` and `git Save push error returns Error` pass.

### Acceptance Criteria

1. [ ] **Logged** — `saveNode logs Order.Save`. `saveNode` of a Workspace replies `Ok` of a new `EventId`, and the log at that id is `Save` of that Workspace. The parse stack gains no node.
2. [ ] **git Save** — `git Save logs Order.Save before the push`. git Save returns `Ok` of the `EventId`, and the log holds `EventBody.Save` of the Workspace at that id. The push runs after that entry is logged.
3. [ ] **Push error** — `git Save push error returns Error`. A failed push returns `Error` of the push message.
4. [ ] **Round trip** — `Save keeps its node through EventJson and MailboxLog`.
5. [ ] **Still working** — `upload, import, parse, Load, undo, and persist complete`.

## See also

- [Message shape](../cqrs-messages.md)
- [core-refinement](../project.md)
- [67 — Log Load as Order.Load](67-log-load-as-order-load.md)
- [80 — Place Actor orders through PlaceOrder](80-place-actor-orders-through-placeorder.md)
- [81 — Log Actor and search stops as Notices](81-log-actor-and-search-stops-as-notices.md)
- [GLOSSARY](../../../GLOSSARY.md)
