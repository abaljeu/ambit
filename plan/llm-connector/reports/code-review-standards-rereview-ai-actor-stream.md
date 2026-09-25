# Standards re-review — AI Actor stream

Pin: `46f6edb4...3e1e2f5f` (`3e1e2f5f`). Scan lines are under the 40-line and 800-line limits in [fsharp-source](.agents/rules/fsharp-source.md); none of those printed bindings are findings.

## Hard

### 1. One-off `(Draft * PlannedAdd list)` next to `Step`

[fsharp-source](.agents/rules/fsharp-source.md): group related values into a named reused type; reuse a type that already exists; do not invent a one-off tuple.

[`FocusXmlStream`](src/Shared/documents/FocusXmlStream.fs) already has `Step = { draft; adds }`. `stepToken` still takes a tuple, and `commitPending` / `addLeaf` / `apply` / `flush` still return or fold that tuple:

```
    let private stepToken
        (newId: unit -> NodeId)
        (draft: Draft, adds: PlannedAdd list)
        token
```

```
        let draft, adds =
            List.fold
                (fun acc token -> stepToken newId acc token)
                (draft, [])
                tokens
        { draft = draft
          adds = List.rev adds }
```

### 2. Test member is 49 lines

[fsharp-source](.agents/rules/fsharp-source.md): 40 lines or less per function. That limit still applies to tests.

[`AgentRunnerFakeTests`](tests/CloudAgents.Tests/AgentRunnerFakeTests.fs) member `partial fake stream waits until cancel` is lines 195–243 (49 lines).

## Smell (judgement call)

### Possible Mysterious Name — `Draft.hold`

[SMELLS](.agents/skills/code-review/SMELLS.md): a name that does not reveal what the field holds.

```
    type Draft =
        { hold: string
          pending: Pending option
          parents: Frame list }
```

`hold` is the unparsed remainder (partial tag bytes until `>`). The name does not say that.
