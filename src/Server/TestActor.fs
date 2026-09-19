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
