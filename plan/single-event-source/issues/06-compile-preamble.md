# 06 — Compile preamble

**Status:** blocked
**Blocked by:** [05 — Expand Op-list apply](05-expand-op-list-apply.md)

## Context

Server.Tests cannot see `Ev` and `Authority` in [[tests/Server.Tests/TestBackend.fs]] because `module Ev` shadows the type. Persist migrate needs those tests to compile.

## What to build

[[tests/Server.Tests/TestBackend.fs]] compiles. `Ev` the type and `Authority` the constructor are in scope. Leftover Change still compiles.

### 1. TestBackend Ev and Authority

- [ ] 1.2.1 Compile preamble — TestBackend `Ev` type and `Authority` constructor in scope (`module Ev` must not shadow the type)

## Out of scope

1. Persist apply — [[07-persist-apply.md|07 — Persist apply]].
2. Contract deletes — [[12-contract-leftover-change-and-revision.md|12 — Contract leftover Change and Revision]].

## See also

[[../arch.md|Single event source architecture]], [[../map.md]]
