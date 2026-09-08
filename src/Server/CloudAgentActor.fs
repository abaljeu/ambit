namespace Gambol.Server

open System
open Gambol.Shared
open Gambol.CloudAgents

[<RequireQualifiedAccess>]
module CloudAgentActor =

    [<Literal>]
    let actorName = "cloud-agent"

    [<Literal>]
    let systemPrompt = "You are a helpful AI assistant working with structured outline documents."

    type ActorConfig =
        { ApiKey: string }

    let private extractFocusMessage (graph: Graph) (focus: NodeId) : string option =
        match Map.tryFind focus graph.nodes with
        | None -> None
        | Some node ->
            let header = node.header.text.Trim()
            if header.StartsWith("?") && header.Length > 1 then
                Some(header.Substring(1).Trim())
            else
                None

    let private packToMarkdown (graph: Graph) : string =
        // Use the root of the subgraph as the document root
        match MdDocument.writeArtifact graph graph.root None with
        | Ok text -> text
        | Error _ -> ""

    let private markdownToGraph (markdown: string) (focusId: NodeId) : Result<Node list * MdComplement, string> =
        // Create a minimal context graph for parsing
        let emptyGraph = Graph.create ()
        let tempDocId = NodeId.New()
        
        match MdDocument.read markdown tempDocId emptyGraph with
        | Error err -> Error $"Failed to parse markdown: {err}"
        | Ok readResult ->
            // Extract the parsed nodes as a list, excluding the temp doc root
            let docNode = 
                match Map.tryFind readResult.documentRootId readResult.nodes with
                | None -> Node.empty
                | Some node -> node
            
            // Get all the children nodes (not just IDs)
            let childNodes =
                docNode.children
                |> List.choose (fun child ->
                    Map.tryFind child.id readResult.nodes)
            
            if List.isEmpty childNodes then
                Error "No content in agent response"
            else
                Ok (childNodes, readResult.complement)

    let private buildPrompt (focusMessage: string) (pack: string) : string =
        sprintf "%s\n\nUser question: %s\n\nContext:\n%s" systemPrompt focusMessage pack

    let private runAgent (config: ActorConfig) (prompt: string) : Async<Result<string, string>> =
        async {
            let runnerConfig = { ApiKey = config.ApiKey }
            let options = { DisplayName = Some "Gambol Run Agent"; ModelHint = None }
            
            match AgentRunner.start runnerConfig prompt None options with
            | Error err ->
                let errMsg =
                    match err with
                    | AgentError.AuthenticationFailed msg -> $"Authentication failed: {msg}"
                    | AgentError.NetworkError msg -> $"Network error: {msg}"
                    | AgentError.ApiError (code, msg) -> $"API error ({code}): {msg}"
                    | AgentError.InvalidResponse msg -> $"Invalid response: {msg}"
                    | AgentError.Timeout -> "Request timeout"
                return Error errMsg
            | Ok (agentId, runId) ->
                match AgentRunner.waitUntilComplete runnerConfig agentId runId 2000 (Some 300000) with
                | Error err ->
                    let errMsg =
                        match err with
                        | AgentError.AuthenticationFailed msg -> $"Authentication failed: {msg}"
                        | AgentError.NetworkError msg -> $"Network error: {msg}"
                        | AgentError.ApiError (code, msg) -> $"API error ({code}): {msg}"
                        | AgentError.InvalidResponse msg -> $"Invalid response: {msg}"
                        | AgentError.Timeout -> "Agent timed out"
                    return Error errMsg
                | Ok result ->
                    return Ok result.Text
        }

    let private addChildrenToFocus
        (handle: CoreChanges)
        (focusId: NodeId)
        (newNodes: Node list)
        (complement: MdComplement)
        : Async<Result<unit, string>> =
        async {
            let! stateResult = handle.getState ()
            match stateResult with
            | Error err -> return Error err
            | Ok state ->
                match Map.tryFind focusId state.graph.nodes with
                | None -> return Error "Focus node not found"
                | Some focusNode ->
                    // Build ops to add new nodes and update focus children
                    let newNodeOps =
                        newNodes
                        |> List.collect (fun node ->
                            let nodeOp = Op.NewNode(node.id, node.header.text)
                            let cssOp =
                                match Map.tryFind node.id complement.cssClassesByNodeId with
                                | Some classes when not (List.isEmpty (CssClass.toList classes)) ->
                                    [ Op.SetNodeCssClasses(node.id, classes) ]
                                | _ -> []
                            nodeOp :: cssOp)
                    
                    let newChildRefs =
                        newNodes |> List.map (fun n -> ChildNode.owner n.id)
                    
                    let replaceOp =
                        Op.Replace(
                            focusId,
                            focusNode.children,
                            focusNode.children @ newChildRefs)
                    
                    let change =
                        { id = state.revision.Value
                          changeId = Guid.NewGuid()
                          ops = newNodeOps @ [ replaceOp ] }
                    
                    let! postResult = handle.postChange [ change ]
                    match postResult with
                    | Ok _ -> return Ok ()
                    | Error err -> return Error err
        }

    let createActorFn (config: ActorConfig) : ActorFn =
        fun (subgraph: Graph) (credential: Credential) (handle: CoreChanges) -> async {
            // Extract focus (parent of subgraph)
            let focusId = subgraph.root
            
            match extractFocusMessage subgraph focusId with
            | None ->
                eprintfn "[CloudAgentActor] No message found on focus node"
                return ()
            | Some message ->
                let pack = packToMarkdown subgraph
                let prompt = buildPrompt message pack
                
                let! agentResult = runAgent config prompt
                
                match agentResult with
                | Error err ->
                    eprintfn "[CloudAgentActor] Agent failed: %s" err
                    return ()
                | Ok responseText ->
                    match markdownToGraph responseText focusId with
                    | Error err ->
                        eprintfn "[CloudAgentActor] Failed to convert response to graph: %s" err
                        return ()
                    | Ok (nodes, complement) ->
                        let! addResult = addChildrenToFocus handle focusId nodes complement
                        match addResult with
                        | Ok () ->
                            eprintfn "[CloudAgentActor] Successfully added %d children" nodes.Length
                        | Error err ->
                            eprintfn "[CloudAgentActor] Failed to add children: %s" err
        }
