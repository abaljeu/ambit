namespace Gambol.Server

open System
open Gambol.Shared

/// TestActor interprets command Node and produces output.
[<RequireQualifiedAccess>]
module TestActor =

    /// ActorFn for Actor name "test".
    /// Interprets command Node text to determine which behavior to run.
    /// Actor selection (choosing TestActor) happens at CoreActorPool via CSS class "actor-test" or node text.
    /// This function reads node text for command interpretation only ("hello", etc.).
    let run (input: ActorInput) (coreChanges: CoreChanges) : Async<unit> =
        async {
            match Map.tryFind input.commandId input.graph.nodes with
            | None -> return ()
            | Some commandNode ->
                let commandText = commandNode.text.Trim().ToLowerInvariant()
                match commandText with
                | "hello" ->
                    let helloNodeId = NodeId.New()
                    let change =
                        { id = 0
                          changeId = Guid.NewGuid()
                          ops =
                            [ Op.NewNode(helloNodeId, "hello")
                              Op.Replace(
                                  input.focusId,
                                  [],
                                  [ ChildNode.owner helloNodeId ]) ] }
                    let caller =
                        { authority = Authority "Actor"
                          secret = input.secret }
                    let! postResult =
                        coreChanges.asCaller(caller).postChange [ change ]
                    match postResult with
                    | Ok _ ->
                        let! stopResult =
                            coreChanges.asCaller(caller).actorStop ActorSucceeded
                        return ()
                    | Error _ -> return ()
                | _ -> return ()
        }

    /// Create ActorFn from TestActor.run.
    let actorFn: ActorFn = run
