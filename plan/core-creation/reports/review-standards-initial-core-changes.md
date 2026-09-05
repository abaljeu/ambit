# Standards review: initial Core Changes

Standards does not pass. I found six findings: two hard violations and four judgement calls.

## Findings

1. **Hard — Core change authority is not contained.** [[CONTEXT.md]] says Core owns persistent state and that persistence goes through the Core API. However, [[src/Server/FileAgent.fs]] and [[src/Server/DbAgent.fs]] still expose direct public mutation functions: `let postChange (agent: FileAgent) (changes: Change list)` and the matching DbAgent function. [[src/Server/Core/GraphAgentHandle.fs]] adds another route instead of making these implementation details inaccessible. Any Server code can bypass Core and publish a Change through a raw agent.

2. **Hard — the new test uses an exception for result handling.** [[.cursor/rules/fsharp-source.mdc]] says “Don't use Exceptions. Use Error types.” [[tests/Server.Tests/CoreChangesTests.fs]] adds `| Error err -> failwith $"{label}: {err}"`. Use an assertion/result-aware test helper.

3. **Judgement — Middle Man.** Both adapters in [[src/Server/Core/GraphAgentHandle.fs]] are field-for-field forwarding: `postChange = fun changes -> FileAgent.postChange agent changes`. Core owns no operation here; FileAgent and DbAgent still apply, persist, and publish. The new Core module is a selector/pass-through, not a deep module.

4. **Judgement — Duplicated Code.** [[src/Server/FileAgent.fs]] and [[src/Server/DbAgent.fs]] add near-identical `accepted` builders for `CoreChangesAccepted`. This also conflicts with [[.cursor/rules/core-agent-behavior.mdc]]: “Don't replicate code.” Put the shared accepted-result construction with its type.

5. **Judgement — Primitive Obsession.** [[src/Server/Core/GraphAgentHandle.fs]] declares `getChangesSince: int -> Async<Change list>` although the same interface returns `Revision`. The HTTP Adapter can decode an integer, then construct a Revision before entering Core.

6. **Judgement — Mysterious Name.** Mirror failures emitted from [[src/Server/Core/GraphAgentHandle.fs]] are still labelled `"[Api] Secondary DB write failed..."`. The source name is now false and can misdirect diagnosis.

## Checks without findings

The old AgentHandle type is removed. Core contains no JSON, HttpRequest, IResult, or HTTP status types. The change adds no mutable binding. The supplied size measurements pass; added source lines are at most 100 characters, changed bindings are at most 40 lines, and changed files satisfy the 400-line growth rule.

Worst issue: direct FileAgent and DbAgent mutation APIs leave Core bypassable.
