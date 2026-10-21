namespace Gambol.Server

open System.Threading
open Gambol.Shared

/// Ops live-save, or a follow-up file diff.
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
}

type internal PersistWork = {
    submit: PersistSubmit
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

    /// True when that node is Unparsed.
    let unparsedNode (graph: Graph) (nodeId: NodeId) : bool =
        match Map.tryFind nodeId graph.nodes with
        | Some node when
            node.documentState.parseState = ParseState.Unparsed ->
            true
        | _ -> false

    /// True when that node may be persisted: not Unparsed, and Unpersisted.
    let openNode (graph: Graph) (nodeId: NodeId) : bool =
        match Map.tryFind nodeId graph.nodes with
        | Some node when
            node.documentState.persistState = PersistState.Unpersisted
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

    /// Closes the stack. None from the consumer means stop and drain finished.
    let internal create ()
        : (PersistSubmit -> unit)
          * (unit -> PersistWork option)
          * (unit -> unit) =
        let gate = obj ()
        let stopped = ref false
        let items: PersistWork list ref = ref []
        let push (work: PersistWork) =
            lock gate (fun () ->
                items.Value <- work :: items.Value
                Monitor.Pulse gate)
        let rec waitHead () =
            match items.Value with
            | head :: rest ->
                items.Value <- rest
                Some head
            | [] when stopped.Value -> None
            | [] ->
                Monitor.Wait gate |> ignore
                waitHead ()
        let requestStop () =
            lock gate (fun () ->
                stopped.Value <- true
                Monitor.PulseAll gate)
        ServerRunRegistry.registerPersistUnblock requestStop
        let consumer () =
            lock gate (fun () -> waitHead ())
        let collect (submit: PersistSubmit) =
            push { submit = submit }
        collect, consumer, requestStop
