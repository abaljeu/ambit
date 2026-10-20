# Code review — 64 Merge PostEvent and PostGraphOnly into Change

- Range: `git diff HEAD`
- Spec: [64 — Merge PostEvent and PostGraphOnly into Change](../issues/64-merge-postevent-and-postgraphonly-into-change.md)
- Arch: [core-refinement architecture](../arch.md)

## 1. Standards

1. **Hard** — [fsharp-source.md](../../../.agents/rules/fsharp-source.md): "When adding a parameter that belongs with existing ones, extend that type instead of lengthening the argument list." `changeFromAssigned` in `src/Server/Api.fs` takes eight positional arguments. `isReady`, `assignedId`, `events`, `externalChanges`, and `message` are already fields of the change response.
2. **Hard** — [fsharp-source.md](../../../.agents/rules/fsharp-source.md): "Group related function parameters into a named, reused type (record or DU)." `placeChange` in `src/Server/Core/CoreMailboxApi.fs`, `postChange` in `src/Server/Core/CoreEventDispatch.fs`, and `dispatchPlaceOrder` in `src/Server/Core/CoreMailboxLoad.fs` each take six positional arguments. `orderId`, `commandName`, and the change payload already travel together on `Mailbox.PlaceOrder`.
3. **Hard** — [core-api.md](../../../.agents/rules/core-api.md): "EventLog assigns stored serials." `nextStoredId` and `appendUnpersisted` in `src/Server/Core/CoreMailboxLoad.fs` mint the notice id with `EventId.next` of `persist.getEventId`. `rememberUnpersisted` then `EventLog.restore`s that numbered event.
4. **Hard** — [core-agent-behavior.md](../../../.agents/rules/core-agent-behavior.md): "Don't replicate code — put shared logic in a reusable place and call it." `postForReplyList` in `src/Server/Core/MailboxHost.fs` repeats the gate lock, `TaskCompletionSource`, and `Async.Parallel` shape of `postForReplies`.
5. **Judgement — Argument Count** — `responseForAssigned` in `src/Server/Api.fs`. Six positional arguments. `buildEpochSec` with `pageBuildEpochSec`, and `beforeId` with `assignedId`, travel as pairs.
6. **Judgement — Overcomplication** — `storedById` in `src/Server/Core/CoreMailboxApi.fs`. `postEvent` of a Change drops the id from `placeOne` and reads the full log to find that id.
7. Scan note — function-size lines are all under 40. No file, line, or phrase limit break. `src/Shared/History.fs` is 800 lines.

## 2. Spec

1. **Wrong** — HTTP change ack. `sideEffects` in `src/Server/Api.fs` keeps non-request events with `id` greater than `beforeId`, except `NodeUnpersisted`. The ack can include an `ActorStop` stored while the Change was handled. Amendment 3 **HTTP ack** records that filter.
2. **Extra** — The mailbox stores a persist status string by the assigned `EventId`, and `Api.postEvents` reads it for the HTTP `message`. Amendment 4 **Persist message** records that store. The `PlaceOrder` reply stays `Result<EventId, string>`.
3. **Unnamed** — `EventBody.NodeUnpersisted` on shared `EventBody`. What to build names `Notice.NodeUnpersisted` as the appended `Ev` body. Amendment 1 **Logged notice** names the history case.
4. **Unnamed** — `Query.PersistMessage` of `EventId`. Amendment 4 **Persist message** names that query.

## 3. Arch

1. **Blocker** — [arch.md](../arch.md) §5 item 13 **One mailbox queue** and §11 seam 1 **Private add**. `CoreEventDispatch.postUnpersisted` posts `Notice.NodeUnpersisted` through `enqueueNotice` (`CoreMailbox.fs` `processor.Post`), not `CoreMailboxApi.postNotice`. Amendment 10 **Notice post** records the gate skip.
2. **Blocker** — [arch.md](../arch.md) §1 **Document-state events**. `Api.sideEffects` drops `EventBody.NodeUnpersisted` from the change response. Poll `c` still includes that event. The section says the client receives every log message and there is no filter.

## 4. Totals

- Standards: 6 findings. Worst: grouped parameters and the notice id minted outside `EventLog.append`.
- Spec: 4 findings. Worst: the change ack adds later non-request events and omits `NodeUnpersisted`.
- Arch: 2 findings. Worst: `Notice.NodeUnpersisted` is posted beside `CoreMailboxApi.postNotice`.
