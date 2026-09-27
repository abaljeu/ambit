namespace Gambol.Server

open Gambol.Shared

/// Injected proof Actor for name `test`. Not a Core module.
[<RequireQualifiedAccess>]
module TestActor =

    /// Posts one Owned child text "hello" under Focus.
    let private hello (input: ActorInput) (coreChanges: CoreChanges) =
        async {
            let helloNodeId = NodeId.New()
            let event =
                { id = EventId.zero
                  submissionId = System.Guid.NewGuid()
                  authority = Authority "Browser"
                  commandName = ""
                  body =
                    EventBody.Change
                        [ Op.NewNode(helloNodeId, "hello")
                          Op.Replace(
                              input.focusId,
                              [],
                              [ ChildNode.owner helloNodeId ]) ] }
            let caller =
                { authority = Authority "Actor"
                  name = ""
                  secret = input.secret }
            let! _ =
                coreChanges.asCaller(caller).postEvents [ event ]
            return ()
        }

    /// Hello-only. Any other command text is ActorFailed. No exception path.
    let private dispatch (input: ActorInput) (coreChanges: CoreChanges) =
        async {
            let! result =
                match Map.tryFind input.commandId input.graph.nodes with
                | None -> async.Return (ActorFailed "")
                | Some commandNode ->
                    match
                        CommandRequest.behaviorFromText
                            commandNode.text
                        with
                    | "hello" ->
                        async {
                            do! hello input coreChanges
                            return ActorSucceeded
                        }
                    | _ -> async.Return (ActorFailed "")
            let caller =
                { authority = Authority "Actor"
                  name = ""
                  secret = input.secret }
            let! _ =
                coreChanges.asCaller(caller).actorStop result
            return ()
        }

    /// ActorFn for Actor name "test". Selection is CoreActorPool's job.
    let actorFn: ActorFn = dispatch

    let private actorCaller (secret: Credential) : Caller =
        { authority = Authority "Actor"
          name = ""
          secret = secret }

    let private postOwnedChild
        (parentId: NodeId)
        (text: string)
        (secret: Credential)
        (coreChanges: CoreChanges)
        =
        async {
            let childId = NodeId.New()
            let event =
                { id = EventId.zero
                  submissionId = System.Guid.NewGuid()
                  authority = Authority "Browser"
                  commandName = ""
                  body =
                    EventBody.Change
                        [ Op.NewNode(childId, text)
                          Op.Replace(
                              parentId,
                              [],
                              [ ChildNode.owner childId ]) ] }
            let! _ =
                coreChanges
                    .asCaller(actorCaller secret)
                    .postEvents [ event ]
            return ()
        }

    let private operationFrom (graph: Graph) =
        match graph.focus with
        | None -> None
        | Some nodeId ->
            Map.tryFind nodeId graph.nodes
            |> Option.map (fun node ->
                CommandRequest.behaviorFromText node.text)

    /// Function-shaped TestActor. Calls getGraph; pool supplies no Graph.
    let functionActor: FunctionActor =
        fun run coreChanges ->
            async {
                let graph = run.getGraph ()
                let parentId =
                    graph.focus
                    |> Option.defaultValue graph.root
                let! result =
                    match operationFrom graph with
                    | Some "hello" ->
                        async {
                            do! postOwnedChild
                                    parentId
                                    "hello"
                                    run.secret
                                    coreChanges
                            return ActorSucceeded
                        }
                    | Some "ping" ->
                        async {
                            do! postOwnedChild
                                    parentId
                                    "pong"
                                    run.secret
                                    coreChanges
                            return ActorSucceeded
                        }
                    | _ -> async.Return (ActorFailed "")
                let! _ =
                    coreChanges
                        .asCaller(actorCaller run.secret)
                        .actorStop result
                return ()
            }
