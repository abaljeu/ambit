Check: pass

A posted start whose name has no function start replies `Error "unknown actor"`. `test`, `ai`, and a query still queue. The admitted start stores `Order.StartActor` under the posted submission id, and a second post of that submission returns that stored event.

- `tests/Server.Tests` passed 778, failed 0, skipped 1.
- `tests/Shared.Tests` passed 1940, failed 0, skipped 3.
- `scripts/agent-test.sh` ran the mapped Server filter and passed 132, failed 0, skipped 0.
- `scripts/agent-test.sh` ran the mapped Shared filter and passed 126, failed 0, skipped 0.
- The named Server filter passed 9, failed 0, skipped 0.
- `dispatchStartActor` and `EventBody.ActorStart` stay in `src/Server/Core/CoreMailboxActors.fs`.
