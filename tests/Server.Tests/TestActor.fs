[<RequireQualifiedAccess>]
module Gambol.Server.Tests.TestActor

open Gambol.Shared
open Gambol.Server

/// Behavior for "hello" command.
/// Posts one Owned child text "hello" under Focus.
let private hello (input: ActorInput) (coreChanges: CoreChanges) : Async<unit> =
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

/// Generic Actor dispatcher.
/// Catches exceptions, dispatches to behavior by command, and always enqueues ActorStop.
let private dispatch (input: ActorInput) (coreChanges: CoreChanges) : Async<unit> =
    async {
        let mutable result = ActorSucceeded
        try
            match Map.tryFind input.commandId input.graph.nodes with
            | None -> ()
            | Some commandNode ->
                let commandText = commandNode.text.Trim().ToLowerInvariant()
                match commandText with
                | "hello" ->
                    do! hello input coreChanges
                | "throw" ->
                    failwith "test exception"
                | _ -> ()
        with
        | _ -> result <- ActorFailed
        let caller =
            { authority = Authority "Actor"
              name = ""
              secret = input.secret }
        let! _ =
            coreChanges.asCaller(caller).actorStop result
        return ()
    }

/// ActorFn for Actor name "test".
/// Generic dispatcher wrapping hello behavior.
/// Actor selection happens at CoreActorPool via CSS class "actor-test" or node text.
let actorFn: ActorFn = dispatch
