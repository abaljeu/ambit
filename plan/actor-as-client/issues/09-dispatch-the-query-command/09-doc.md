# 09 — Start the query command

## 1. Current

[[doc/current/query-actor.md]]
[[doc/current/search-actor.md]]
[[doc/current/actors.md]]
[[doc/current/mailbox.md]]
[[doc/current/op.md]]
[[doc/current/http-contract.md]]
[[doc/current/api.md]]

## 2. User

[[doc/user/reference/commands/run.md]]

## 3. Found elsewhere

One suggested fix waits for confirmation. This ticket folder has no review report that accepts it.

[21 — Convert CoreMsg.ActorStop](plan/actor-as-client/issues/21-convert-coremsg-actorstop/21-convert-coremsg-actorstop-ticket.md) records that ``query start error posts no rows, fails the reply, and keeps the edit`` has left. ``posted gate start replies unknown actor`` replies `unknown actor`. `SearchActor.queryActorBody` returns the result. The pool posts the stop.
