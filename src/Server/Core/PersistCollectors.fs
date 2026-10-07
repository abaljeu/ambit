namespace Gambol.Server

open System.Threading
open Gambol.Shared

/// Ops live-save, or a full-graph snapshot write.
[<RequireQualifiedAccess>]
type PersistKind =
    | Ops
    | Change

/// One persist job. The persist thread is the caller of the write body.
type PersistSubmit = {
    nodeIds: NodeId list
    dataDir: string option
    preGraph: Graph
    postGraph: Graph
    ops: Op list
    kind: PersistKind
    notify: bool
    wait: bool
}

/// Result of a collector call. A thrown write becomes Failed.
[<RequireQualifiedAccess>]
type PersistOutcome =
    | Wrote of PersistGraphOk
    | Blocked
    | Failed of string
    | Queued

type internal PersistWork = {
    submit: PersistSubmit
    reply: (PersistOutcome -> unit) option
}

/// Stack of persist jobs. Not a MailboxProcessor.
[<RequireQualifiedAccess>]
module PersistCollectors =

    let internal opAnchor (op: Op) : NodeId option =
        match op with
        | Op.NewNode(id, _)
        | Op.SetText(id, _, _)
        | Op.SetClasses(id, _, _)
        | Op.NewSpecialNode(id, _, _)
        | Op.SetName(id, _, _)
        | Op.SetDocumentState(id, _, _)
        | Op.Replace(id, _, _) -> Some id
        | Op.SetUpdateTime _ -> None

    let private owner (graph: Graph) (op: Op) : NodeId option =
        opAnchor op
        |> Option.bind (
            GraphQuery.enclosing graph Node.carriesStateAxes)

    /// True when that node is Unparsed on both axes.
    /// Old parse sets DocumentState Current and leaves parseState Unparsed.
    /// That pair stays open until the axis migrate lands.
    let unparsedNode (graph: Graph) (nodeId: NodeId) : bool =
        match Map.tryFind nodeId graph.nodes with
        | Some node when
            node.parseState = ParseState.Unparsed
            && node.documentState <> Current ->
            true
        | _ -> false

    /// True when that node may be persisted: not Unparsed, and Unpersisted.
    let openNode (graph: Graph) (nodeId: NodeId) : bool =
        match Map.tryFind nodeId graph.nodes with
        | Some node when
            node.persistState = PersistState.Unpersisted
            && not (unparsedNode graph nodeId) ->
            true
        | _ -> false

    /// Owning special nodes for these ops, nearest axis carrier first.
    let owningSpecials (graph: Graph) (ops: Op list) : NodeId list =
        ops
        |> List.choose (owner graph)
        |> List.distinct

    /// Drop ops whose owning special is Unparsed. Those files are not open.
    let opsOpenForPersist (graph: Graph) (ops: Op list) : Op list =
        ops
        |> List.filter (fun op ->
            match owner graph op with
            | Some id when unparsedNode graph id -> false
            | _ -> true)

    let private waitReply
        (push: PersistWork -> unit)
        (submit: PersistSubmit)
        : PersistOutcome =
        let cell = ref None
        let gate = obj ()
        let reply outcome =
            lock gate (fun () ->
                cell.Value <- Some outcome
                Monitor.Pulse gate)
        push { submit = submit; reply = Some reply }
        lock gate (fun () ->
            while cell.Value.IsNone do
                Monitor.Wait gate |> ignore
            match cell.Value with
            | Some outcome -> outcome
            | None -> PersistOutcome.Failed "persist collector lost the reply")

    /// Closes the stack. Returns collect and the blocking consumer.
    let internal create ()
        : (PersistSubmit -> PersistOutcome) * (unit -> PersistWork) =
        let gate = obj ()
        let items: PersistWork list ref = ref []
        let push (work: PersistWork) =
            lock gate (fun () ->
                items.Value <- work :: items.Value
                Monitor.Pulse gate)
        let rec waitHead () =
            match items.Value with
            | [] ->
                Monitor.Wait gate |> ignore
                waitHead ()
            | head :: rest ->
                items.Value <- rest
                head
        let consumer () =
            lock gate (fun () -> waitHead ())
        let collect (submit: PersistSubmit) =
            if submit.wait then
                waitReply push submit
            else
                push { submit = submit; reply = None }
                PersistOutcome.Queued
        collect, consumer
