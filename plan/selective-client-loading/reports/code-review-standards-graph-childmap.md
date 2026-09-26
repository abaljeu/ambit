# Standards review of Graph.childMap

Range: `git diff origin/staging...HEAD`. Commit c76009ee Move Node children onto Graph.childMap. Scan: no function over 40 lines. No [fsharp-source.md](.agents/rules/fsharp-source.md) file-limit hit ([App.fs](src/Client/App.fs) stays 854). No markdown in the range, so no [markdown-writing.md](.agents/rules/markdown-writing.md) or [refer-by-name.md](.agents/rules/refer-by-name.md) hit. Node.children and Node.childrenStatus are removed. Wire NodeWire keeps those fields for legacy decode. That is not unused leftover.

## 1. Line length in Api.fs (hard)

[fsharp-source.md](.agents/rules/fsharp-source.md) limits each source line to 100 characters. [Api.fs](src/Server/Api.fs) line 68 has 113 characters:

```
        : Async<Result<Result<Node list * Map<NodeId, ChildNode list>, ResidentProjection.LoadRefuse>, string>> =
```

## 2. Line length in SerializationTests.fs (hard)

The same line-length rule applies to test source. The 800-line file exemption does not apply. [SerializationTests.fs](tests/Shared.Tests/SerializationTests.fs) adds line 66 (229 characters) and line 78 (169 characters):

```
        $"""{{"root":"{rootId}","nodes":[{{"id":"{rootId}","text":"ROOT","children":[],"kind":{{"type":"special","kind":"workspace"}}}},{{"id":"{nodeId.Value}","text":"legacy","children":[],"cssClasses":[],"kind":"normal"}}]}}"""
```

```
        $"""[{{"id":"{nodeId.Value}","text":"bad","children":[{{"ref":"owner","id":"{childId.Value}"}}],"childrenStatus":"unloaded","cssClasses":[],"kind":"normal"}}]"""
```

## 3. Dual names for Children lookup (smell: Mysterious Name)

[SMELLS.md](.agents/skills/code-review/SMELLS.md) Mysterious Name is a judgement call. [GraphChildren](src/Shared/Model.fs) uses `get` / `tryGet` / `status`. [GraphOps](src/Shared/GraphOps.fs) adds `Graph.children` / `Graph.tryGetChildren` / `Graph.childrenStatus`. `get` returns `[]` when the parent is Unloaded, so Unloaded and Loaded-empty look the same. `tryGet` returns `None` for Unloaded. Client, Paste, and some Server files call `Graph.children`. Other Shared files call `GraphChildren.get`.

```
    let get (graph: Graph) (id: NodeId) : ChildNode list =
        Map.tryFind id graph.childMap |> Option.defaultValue []
```

```
        static member children graph id = GraphBuild.getChildren graph id
        static member tryGetChildren graph id = GraphBuild.tryGetChildren graph id
```

## 4. GraphBuild lookup wrappers (smell: Middle Man)

[SMELLS.md](.agents/skills/code-review/SMELLS.md) Middle Man is a judgement call. [GraphBuild.fs](src/Shared/GraphBuild.fs) adds four functions that only call [GraphChildren](src/Shared/Model.fs):

```
    let tryGetChildren (graph: Graph) (id: NodeId) =
        GraphChildren.tryGet graph id

    let getChildren (graph: Graph) (id: NodeId) = GraphChildren.get graph id
```

`isLoaded` and `childrenStatus` do the same.
