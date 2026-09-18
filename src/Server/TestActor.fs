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

    /// Dispatch by command behavior. Always queues ActorStop.
    let private dispatch (input: ActorInput) (coreChanges: CoreChanges) =
        async {
            let! result =
                async {
                    try
                        match Map.tryFind input.commandId input.graph.nodes with
                        | None -> return ActorSucceeded
                        | Some commandNode ->
                            match
                                CommandRequest.behaviorFromText
                                    commandNode.text
                                with
                            | "hello" ->
                                do! hello input coreChanges
                                return ActorSucceeded
                            | "throw" ->
                                return failwith "test exception"
                            | _ -> return ActorSucceeded
                    with
                    | _ -> return ActorFailed
                }
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
