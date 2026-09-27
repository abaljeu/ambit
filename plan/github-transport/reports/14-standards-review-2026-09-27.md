# 14 — Standards review

1. **Repeated Load/Save command construction — hard violation and judgment call.** Hard violation: `.agents/rules/core-agent-behavior.md`, Simplicity First: “Don't replicate code — put shared logic in a reusable place and call it.” Judgment call: possible **Duplicated Code** (`.agents/skills/code-review/SMELLS.md`). `loadOpFor` and `saveOpFor` repeat the same focus lookup and `ActorStart` construction; only the final `operation` differs:

```fsharp
let focusId =
    match model.selectedNodes with
    | Some selection ->
        focusedNodeId model.graph selection
    | None -> viewRootNodeId model
let start =
    CommandRequest.actorStart
        model.graph
        model.siteMap
        model.zoomRoot
        focusId
        focusId
        model.eventId
```

Extract this construction to one shared Browser helper and call it from both command updaters.
