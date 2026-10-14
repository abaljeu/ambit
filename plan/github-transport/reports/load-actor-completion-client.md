# Load actor completion client

Diagnosis of the report: a Load command on a git-bound Workspace starts an Actor, the Actor finishes, and the Cancel control stays because the client never applies ActorStop.

## 1. Red loop

Command:

`dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~git Load tells the client the actor completed"`

The test holds git Load inside pull, applies the event list from that moment with `SyncLogic.applyServerTail` (the CommandDone path), waits until the server log has `ActorSucceeded`, posts one later event, applies the next `getEventsSince` list the same way, and asserts Cancel is gone.

First red run, after the later event was a `SetUpdateTime` the server accepts on an Unparsed Workspace:

```
Failed Gambol.Server.Tests.GithubTransportActorTests.git Load tells the client the actor completed
Cancel stays: client was not told the actor completed
```

The command response had already shown Cancel, and the server log had `ActorSucceeded`, before that assertion.

## 2. Minimised repro

Removing the later event makes the same test pass. The later event is load-bearing. The held pull is load-bearing: the command response must apply ActorStart before ActorStop is in the list. The git Load Peer Actor is the path that records that ActorStart and ActorStop. The smallest later event that the mailbox accepts here is `SetUpdateTime`.

What remains:

1. git Load starts, and the client applies only the events visible while pull is still inside the Actor.
2. The Actor then records ActorStop.
3. One newer event is appended.
4. The client applies the next server event list in the order `getEventsSince` returns it.

Cancel stays.

## 3. Hypotheses

Ranked before the checks below. User was away, so this order stood.

1. **Newest event hides ActorStop.** `getEventsSince` returns a newest-head list. `SyncLogic.applyServerTail` applies the first event and sets the client event id to that id. It then skips ActorStop because that id is older. If the same poll list is applied oldest-id first, Cancel goes away.
2. **ActorStop is missing from the poll.** If ActorStop is not in the poll list, applying that list oldest-id first still leaves Cancel.
3. **The command response already passed ActorStop.** If the command-response event id is already at or past ActorStop, the poll list does not contain ActorStop.
4. **Poll apply fails closed.** If `applyServerTail` returns Error, the test fails with the apply label, not with the Cancel sentence.
5. **ActorStop names a different Focus.** If ActorStart and ActorStop do not share the Load Focus, applying the poll list oldest-id first still leaves Cancel on that Focus.

## 4. Checks

Hypothesis 1 held. The same red test, with the poll list sorted by event id before apply, passed. Cancel went away. That result falsifies 2, 3, and 5: ActorStop was in the list, the command response had not passed it, and the Focus matched. Hypothesis 4 was already out: the red failure text was the Cancel sentence, so apply returned Ok.

On a real git Load, the Parse thread can append a Change after ActorStop. Load pushes the Workspace, then the Actor records ActorStop. Parse runs on another thread and posts later. The next poll is a newest-head list. The newest Change is applied. ActorStop is skipped. Cancel stays. The repro uses `SetUpdateTime` for that newer event because a content edit is refused while the Workspace is Unparsed.

## 5. Fix

`SyncLogic.foldProjectedEvents` now applies events from the older id upward. CommandDone and Poll both use that fold. EventLog storage stays newest-head.

The regression seam is `applyServerTail newest-head tail still removes a live Focus` in [SyncLogicTests](tests/Shared.Tests/SyncLogicTests.fs). It failed with `Assert.False() Failure` before the sort, then passed. The original server loop then passed too.

Commands that passed:

`dotnet test tests/Shared.Tests -c Debug --filter "FullyQualifiedName~applyServerTail"`

`dotnet test tests/Server.Tests -c Debug --filter "FullyQualifiedName~git Load tells the client the actor completed"`

`scripts/client.sh build`

No debug instrumentation was added.

## 6. Post-mortem

A test that applies one ActorStop, alone, cannot see this. The cursor skip only drops ActorStop when a newer event shares the list. The missing test is a newest-head tail: a later event, then ActorStop, after ActorStart is already applied, asserting the Load Focus is no longer live. That test is now in Shared. Returning oldest-first from `getEventsSince` would also hide the bug from this client, and it would surprise every reader of the newest-head log. Apply-by-id keeps the log order and still delivers ActorStop.
