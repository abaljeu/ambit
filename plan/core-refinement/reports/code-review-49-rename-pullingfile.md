# Code review — Rename NoServerFile to PullingFile

- Range: `git diff HEAD`
- Spec: [49 — Rename NoServerFile to PullingFile](../issues/49-rename-noserverfile-to-pullingfile.md)
- Arch: [core-refinement architecture](../arch.md)

## 1. Standards

1. **Judgement — Duplicated Code** — [Serialization tests](../../../tests/Shared.Tests/SerializationTests.fs) ``PullingFile node and state op round-trip``. The node and op round-trip is also asserted in ``PullingFile round-trip``.
2. **Judgement — Duplicated Code** — [Workspace upload structure tests](../../../tests/Shared.Tests/WorkspaceUploadStructureTests.fs) ``new file stubs start PullingFile; directory parent stays Current``. It repeats the Current parent and PullingFile file asserts in ``markPullingFile sets PullingFile``.
3. **Note** — `DocumentPartition.isMemberOfPullingFile` is 9 lines. Under the 40-line function limit in [.agents/rules/fsharp-source.md](../../../.agents/rules/fsharp-source.md).
4. **Note** — `WorkspaceUploadStructure.markPullingFile` is 8 lines. Under the 40-line function limit in [.agents/rules/fsharp-source.md](../../../.agents/rules/fsharp-source.md).
5. **Note** — Checklist titles that said "flag" on [20 — Contract special-node state inside DocumentState](../issues/20-contract-special-node-state-inside-document-state.md) and [28 — Parsed when clear](../issues/28-parsed-when-clear.md) now say `PullingFile`.

## 2. Spec

1. No findings.

## 3. Arch

1. No findings.

## 4. Totals

- Standards: 2 findings. Worst: the two new acceptance tests overlap the renamed existing tests.
- Spec: 0 findings. Worst: none.
- Arch: 0 findings. Worst: none.
