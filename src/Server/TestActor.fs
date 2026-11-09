namespace Gambol.Server

open Gambol.Shared

/// Injected proof Actor for name `test`. Not a Core module.
[<RequireQualifiedAccess>]
module TestActor =

    /// Posts one Owned child text "hello" under Focus.
    let private hello (input: ActorInput) (coreChanges: CoreChangesDoor) =
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
            let caller = CoreDoor.actorCaller input.secret
            let! _ =
                CoreDoor.postEvents
                    (CoreDoor.forCaller caller coreChanges)
                    [ event ]
            return ()
        }

    /// Hello-only. Any other command text is ActorFailed. No exception path.
    let private dispatch (input: ActorInput) (coreChanges: CoreChangesDoor) =
        async {
            let! result =
                match Map.tryFind input.runDefinitionId input.graph.nodes with
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
            let caller = CoreDoor.actorCaller input.secret
            let! _ =
                CoreDoor.actorStop
                    (CoreDoor.forCaller caller coreChanges)
                    result
            return ()
        }

    /// ActorFn for Actor name "test". Selection is CoreActorPool's job.
    let actorFn: ActorFn = dispatch

    let private childEvent (parentId: NodeId) (text: string) : Ev =
        let childId = NodeId.New()
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

    let private postChild
        (parentId: NodeId)
        (text: string)
        (post: Ev list -> Async<Result<unit, string>>)
        =
        async {
            let! _ = post [ childEvent parentId text ]
            return ()
        }

    let private commandText (graph: Graph) (nodeId: NodeId) =
        match Map.tryFind nodeId graph.nodes with
        | None -> ""
        | Some node -> CommandRequest.behaviorFromText node.text

    let private runBehavior
        (behavior: string)
        (parentId: NodeId)
        (changes: ActorChanges)
        =
        async {
            let! result =
                match behavior with
                | "hello" ->
                    async {
                        do! postChild parentId "hello" changes.postChanges
                        return ActorSucceeded
                    }
                | "ping" ->
                    async {
                        do! postChild parentId "pong" changes.postChanges
                        return ActorSucceeded
                    }
                | _ -> async.Return (ActorFailed "")
            let! _ = changes.actorStop result
            return ()
        }

    /// Function-shaped TestActor. Calls getGraph; pool supplies no Graph.
    let functionActor: Gambol.Server.ActorFunction =
        fun run door ->
            let graph = run.getGraph ()
            let parentId =
                graph.focus |> Option.defaultValue graph.root
            let behavior =
                match graph.focus with
                | None -> ""
                | Some nodeId -> commandText graph nodeId
            let changes =
                CoreDoor.actorChanges
                    (CoreDoor.forCaller
                        (CoreDoor.actorCaller run.secret)
                        door)
            runBehavior behavior parentId changes

    /// Test command after the actor node id is applied.
    /// Hello and ping land under focusId.
    let functionActorAt
        (runDefinitionId: NodeId)
        (focusId: NodeId)
        : Gambol.Shared.ActorFunction =
        fun run changes ->
            let graph = run.getGraph ()
            runBehavior
                (commandText graph runDefinitionId)
                focusId
                changes
