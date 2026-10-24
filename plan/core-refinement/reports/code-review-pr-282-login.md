# Code review — PR 282 login and logout

- Range: `git diff origin/staging...HEAD` at `a98bf9ee` on [Move Session off the Core mailbox](https://github.com/abaljeu/ambit/pull/282)
- Spec: [61 — Move Session off the core mailbox](../issues/61-move-session-off-the-core-mailbox.md)
- Decision: [Message shape](../cqrs-messages.md) decision 8 **Session leaves**

## Login and logout

The two-phase logout is the right shape. `Session.stopAdmitting` closes admission under the session lock, and `CoreMailboxApi.logout` queues `CoreQueue.DropAuthority` with that generation. The startup prelude still serves `Ask` and leaves the drop behind orders already queued. One later login stores the closed generation as `prior` and opens a new `live` generation. `Session.removeAuthority` closes only the generation on that drop. A test shows the new generation's order still runs, and a later logout still rejects `PlaceOrder` with `Error "authority dropped"`.

That slot rotation does not make logout stick to the route that already passed admission.

`SessionRoute` stores `generation`. `Mailbox.PlaceOrder` and `CoreMsg` store `Caller` only. `authorityHeld` asks `Session.wasDropped`, and that reads `slot.live.closed`. After a login during a queued drop, `live` is the new open generation. A late order from the old route is held and applied. Acceptance criterion 3 of [61 — Move Session off the core mailbox](../issues/61-move-session-off-the-core-mailbox.md) says an order from that open route that queues after `DropAuthority` is rejected. The test `login during a pending logout runs and a dropped generation rejects a late order` posts the rejected `PlaceOrder` only after a second logout.

The map keeps one `prior`. This sequence drops a generation that is still in use:

1. A route begins on generation A.
2. Logout queues `DropAuthority` for A.
3. Login opens B and keeps A as `prior`.
4. Logout queues `DropAuthority` for B.
5. Login opens C and keeps B as `prior`. A is gone.

`finishAdmittedRoute` searches `live` and `prior` only. The route from A finishes as a no-op. `waitForAdmittedRoutes` on A's stop waits on a `TaskCompletionSource` that nothing sets. `removeAuthority` for A finds neither slot and returns. The logout HTTP path does not call that wait, so the browser reply still returns. The public wait does not complete when those routes finish.

A sound check rejects the generation that admitted the order. The drop message already carries that generation. The order does not. `Caller` is one Browser slot (`name = ""`), so a new login and an old in-flight route are the same key at Receive.

## Standards

Mechanical scan: no `emit()` hits. Measured bindings are under 40 lines. No added line over 100 characters.

### Hard violations

[Session](../../../src/Server/Core/Session.fs) violates [F# source](../../../.agents/rules/fsharp-source.md): "Use pure functional F#. Don't use mutable." The host cell is a `ref`, and every public call takes `lock` and assigns `.Value`. The same idiom already exists on `MailboxHost` and `PersistCollectors`. The mutable rule gives those cells no exception.

No other documented-standard hits. [Session](../../../src/Server/Core/Session.fs) is `[<RequireQualifiedAccess>]`. Ticket names keep their titles. EventId serial is untouched.

### Judgement

**Duplicated Code.** `withAdmittedCaller` and `withBrowserCaller` repeat the cookie, session, and 401 path. `admissionIsOpen`, `hasAuthority`, and `wasDropped` repeat the same lock and `Map.tryFind`.

**Overcomplication.** `Session.login` is `Result<unit, string>` and returns only `Ok ()`. `generation` and `routeId` are bare `Guid` values.

**Speculative Generality.** `waitForAdmittedRoutes` has no `src/` caller. Amendment 7 keeps it. The smell is suppressed.

## Spec

Login and logout match the named sequence. `CoreMailboxApi.login` calls `Session.login`, returns `Ok ()`, and posts nothing. `logout` stops admission, posts `CoreQueue.DropAuthority`, and returns. The cookie is set only after `Ok`. Orders already queued run before the drop. With no newer login, `PlaceOrder` answers `Error "authority dropped"`.

### Missing or partial

Receive is not generation-scoped. Acceptance criterion 3: "An order from that open route that queues after `DropAuthority` is rejected at Receive." Amendment 9: "the queued drop closes only the generation logout queued, and the new generation's orders run." `authorityHeld` reads `slot.live.closed`. After login during a queued drop, a late order from the old route is still held.

### Scope creep

`/ambit/state`, `/ambit/poll`, `/ambit/load`, and `/ambit/workspace-remotes` moved from `withBrowserChanges` to check-only `withAdmittedChanges`. Amendment 4 requires the hold "through the Core handoff" for parse, save, reconciliation, and the other order routes. Those order routes still hold it. The read routes no longer count as open routes.

### Implemented but wrong

Same Receive hole. [Mailbox](../../../doc/current/mailbox.md) and decision 8: "rejects an order whose current generation `DropAuthority` closed." Closing `prior` while `live` is a new login does not reject. "A `CoreMsg` order uses the same reason and skips apply and the log" is true when `live.closed` is set. It is false for an old-generation `CoreMsg` after re-login.

Actor orders skip this check (`Authority "Actor" -> true`). `admitCaller` still checks `pool.isLive` at actor start. Staging `PostEvent` did not call `admitCaller`. That bypass matches "An Actor order stays held by the live pool secret."

## Totals

Standards: 1 hard finding. Worst: the new `authorities` ref in [Session](../../../src/Server/Core/Session.fs). Spec: 2 findings. Worst: Receive allows an old route's order once a newer generation is open.
