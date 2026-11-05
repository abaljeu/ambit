pass

## Check

Ticket: [07 — Start the test command](../issues/07-dispatch-the-test-command/07-dispatch-the-test-command-ticket.md)

[x] `scripts/agent-test.sh` with no arguments passed 17 tests.

[x] `scripts/agent-test.sh --filter FullyQualifiedName~TestActorHelloTests server` passed 9 tests.

[x] `scripts/agent-test.sh --filter FullyQualifiedName~TestActorCommandErrorTests server` passed 8 tests.

## Result

[x] A Run whose command text selects `test` queues `Order.ActorOrder` from `postActorStart` through `MailboxHost.postActorOrder`.

[x] The order function is `TestActor.functionActorAt`. It closes over the Run actor node id and the Run focus id.

[x] Boot registration in `RouteRegistration` lists the `ai` actor.

[x] `TestActor.functionActor` stays the pool `ActorFunction`. `CoreActorPoolFunctionStartTests` still calls that value.

[x] `poolActor` builds `ActorChanges` with the credential `startFunction` minted.
