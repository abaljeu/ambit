# 06 — Compile preamble

**Status:** done
**Actual:** 1h
**Blocked by:** None

## Context

Server.Tests cannot see `Ev` and `Authority` in [[tests/Server.Tests/TestBackend.fs]] because `module Ev` shadows the type. Persist migrate needs those tests to compile.

## What to build

[[tests/Server.Tests/TestBackend.fs]] compiles. `Ev` the type and `Authority` the constructor are in scope. Leftover Change still compiles.

### 1. TestBackend Ev and Authority

- [x] 1.2.1 Compile preamble — TestBackend `Ev` type and `Authority` constructor in scope (`module Ev` must not shadow the type)

## Out of scope

1. Persist apply — [[07-persist-apply.md|07 — Persist apply]].
2. Contract deletes — [[12-contract-leftover-change-and-revision.md|12 — Contract leftover Change and Revision]].

## See also

[[../arch.md|Single event source architecture]], [[../map.md]]

## Comments

- 2026-09-16 — After [[05-expand-op-list-apply.md|05 — Expand Op-list apply]], that compile already succeeds. `open Gambol.Shared` puts bare `Ev` (the type) and `Authority` (the constructor) in scope. Leftover Change still compiles. `module Ev` did not produce a compile error, so [[src/Shared/History.fs]] was left unchanged.

## Time

- 2026-09-16 1h — Verified TestBackend `Ev` type and `Authority` constructor in scope after [[05-expand-op-list-apply.md|05 — Expand Op-list apply]]; no [[src/Shared/History.fs]] change (from chat)
