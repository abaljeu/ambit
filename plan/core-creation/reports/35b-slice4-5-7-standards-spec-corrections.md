# 35b slices 4+5+7 — standards and Spec corrections

Ticket: [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md). This unit answers Alan’s Origin Spec review of [§4 CoreMailbox / CoreMsg / CoreActorPool](plan/core-creation/issues/35b-browser-run-hello.md), [§5 TestActor / History / CoreRuntime](plan/core-creation/issues/35b-browser-run-hello.md), and [§7 Browser proof](plan/core-creation/issues/35b-browser-run-hello.md). [§6 History durability](plan/core-creation/issues/35b-browser-run-hello.md) stays on [46 — Mailbox History durability](plan/core-creation/issues/46-mailbox-history-durability.md). Status stays `coded`. Staging was not published.

## 1. Spec

### 1.1 Unregistered Actor name fails start

First token after `?` that is not a registered Actor name fails start/Command. [CommandRequest.actorNameFromText](src/Shared/CommandRequest.fs) extracts any first token. [CoreActorPool.startActor](src/Server/Core/CoreActorPool.fs) looks that name up in the registered set and returns Error when it is absent. No live row. No ActorStart Ev. Browser Run already sends every `?` text on the Command path, so this is not AmbleRun. Implementation is registered-set membership. Tests use `unknown` and `nope` as examples.

### 1.2 Non-hello command fails on Actor `test`

After Actor `test` is selected, any command text that is not `hello` is `ActorFailed`. No Owned child. No empty `ActorSucceeded`. [TestActor](src/Server/TestActor.fs) matches [CommandRequest.behaviorFromText](src/Shared/CommandRequest.fs) to `hello` only. Tests use `unknown` and `nope` as examples.

### 1.3 Hello still succeeds

`?test hello` still posts one Owned child text `hello` and stops as `ActorSucceeded`.

## 2. Standards

1. Production [TestActor](src/Server/TestActor.fs) has no `failwith`, no `try-with`, and no `throw` command. The 34b exception harness is gone.
2. New fixtures use `EventId.zero`. [TestActorCommandErrorTests](tests/Server.Tests/TestActorCommandErrorTests.fs) and the `?test hello` fixture this branch added on [TestActorHelloTests](tests/Server.Tests/TestActorHelloTests.fs) use it. `sampleRequest` in that file now uses `EventId.zero`.
3. New poll wait is recursive `waitUntil`. No mutable flag in the new test file. Pre-existing mutable waits in [TestActorHelloTests](tests/Server.Tests/TestActorHelloTests.fs) were not rewritten.
4. Error facts live in [TestActorCommandErrorTests](tests/Server.Tests/TestActorCommandErrorTests.fs) so [TestActorHelloTests](tests/Server.Tests/TestActorHelloTests.fs) does not grow. The old `throw` fact is removed. The CSS `unknown` fact now asserts `ActorFailed` and no hello child.
5. Same-directory labeled links on [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md) use the file name. Arch [TestActor](src/Server/TestActor.fs) path and the `ActorFailed` sentence in Alternative 5 were required: exceptions are no longer the fail path.

Origin review report files `code-review-35b-slice4-5-*.md` were not in this workspace. They were not copied.

## 3. Verification

Focused Shared `CommandRequestTests` — 9 passed, including extract of any first token (`unknown`, `nope`) and any command text after the name. Server `TestActorCommandErrorTests` plus `TestActorHelloTests` — 15 passed: unregistered start fail for `?unknown` and `?nope`; production Command of those names is HTTP BadRequest; `?test unknown` and `?test nope` are `ActorFailed` with no Owned child; `?test hello` still posts Owned `hello` and `ActorSucceeded`. Client was not edited; no Client compile gate.

## 4. Files

1. [TestActor.fs](src/Server/TestActor.fs) — hello-only; explicit `ActorFailed`; no exception path.
2. [TestActorCommandErrorTests.fs](tests/Server.Tests/TestActorCommandErrorTests.fs) — unregistered start fail; non-hello `ActorFailed`; `?test hello` success.
3. [TestActorHelloTests.fs](tests/Server.Tests/TestActorHelloTests.fs) — drop `throw`; CSS `unknown` is `ActorFailed`; `EventId.zero` on this branch’s fixtures.
4. [CommandRequestTests.fs](tests/Shared.Tests/CommandRequestTests.fs) — parse any first token and any command text after the name.
5. [Gambol.Server.Tests.fsproj](tests/Server.Tests/Gambol.Server.Tests.fsproj) — register the new test file.
6. [35b — Browser Run hello](plan/core-creation/issues/35b-browser-run-hello.md) — checklist items and comment.
7. [Core creation architecture](plan/core-creation/arch.md) — TestActor path; non-hello `ActorFailed`; no exception path.
