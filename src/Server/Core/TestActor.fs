namespace Gambol.Server

open System
open Gambol.Shared

/// TestActor interprets command Node and produces output.
[<RequireQualifiedAccess>]
module TestActor =

    /// ActorFn for Actor name "test".
    /// Interprets command Node text and dispatches to behavior.
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
                    let! result =
                        coreChanges.asCaller(caller).postChange [ change ]
                    match result with
                    | Ok _ -> return ()
                    | Error _ -> return ()
                | _ -> return ()
        }

    /// Create ActorFn from TestActor.run.
    let actorFn: ActorFn = run
