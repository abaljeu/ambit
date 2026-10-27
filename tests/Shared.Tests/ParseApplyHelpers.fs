module ParseApplyHelpers

open System
open Xunit
open Gambol.Shared

/// Graph-only parse apply. An empty list leaves the graph unchanged.
let applyGraphOnly (graph: Graph) (ops: Op list) : Graph =
    if List.isEmpty ops then
        graph
    else
        let state = { graph = graph; eventId = EventId.zero }
        match
            ChangeAmendment.applyForGraphOnly EventId.zero "Parse" ops state
        with
        | ApplyResult.Changed next, _, _
        | ApplyResult.Unchanged next, _, _ -> next.graph
        | ApplyResult.Invalid(_, msg), _, _ -> failwith msg

let applyNodeParsed (graph: Graph) (nodeId: NodeId) : Graph =
    let event =
        { id = EventId.zero
          submissionId = Guid.NewGuid()
          authority = Authority "Parse"
          commandName = ""
          body = EventBody.Notice(Notice.NodeParsed nodeId) }
    match Ev.apply event { graph = graph; eventId = EventId.zero } with
    | ApplyResult.Changed next
    | ApplyResult.Unchanged next -> next.graph
    | ApplyResult.Invalid(_, msg) -> failwith msg

let assertNoSetDocumentState (ops: Op list) =
    Assert.False(
        ops
        |> List.exists (function
            | Op.SetDocumentState _ -> true
            | _ -> false))

/// Ops stay free of SetDocumentState. The file stays Unparsed until NodeParsed.
let applyParseThenCurrent (graph: Graph) (fileId: NodeId) (ops: Op list) =
    assertNoSetDocumentState ops
    let during = applyGraphOnly graph ops
    Assert.Equal(Unparsed, during.nodes.[fileId].documentState.AsEnum)
    let after = applyNodeParsed during fileId
    Assert.Equal(Current, after.nodes.[fileId].documentState.AsEnum)
    during, after
