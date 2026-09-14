namespace Gambol.Server

open System
open Gambol.Shared

/// TestActor interprets command Node and produces output.
[<RequireQualifiedAccess>]
module TestActor =

    /// Behavior for "hello" command.
    /// Posts one Owned child text "hello" under Focus.
    let private hello (input: ActorInput) (coreChanges: CoreChanges) : Async<unit> =
        async {
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
            return ()
        }

    /// Generic Actor dispatcher.
    /// Catches exceptions, dispatches to behavior by command, and always enqueues ActorStop.
    let private dispatch (input: ActorInput) (coreChanges: CoreChanges) : Async<unit> =
        async {
            try
                match Map.tryFind input.commandId input.graph.nodes with
                | None -> ()
                | Some commandNode ->
                    let commandText = commandNode.text.Trim().ToLowerInvariant()
                    match commandText with
                    | "hello" ->
                        do! hello input coreChanges
                    | _ -> ()
            with
            | ex -> ()
            
            let caller =
                { authority = Authority "Actor"
                  secret = input.secret }
            let! stopResult =
                coreChanges.asCaller(caller).actorStop ActorSucceeded
            return ()
        }

    /// ActorFn for Actor name "test".
    /// Generic dispatcher wrapping hello behavior.
    /// Actor selection (choosing TestActor) happens at CoreActorPool via CSS class "actor-test" or node text.
    let actorFn: ActorFn = dispatch
