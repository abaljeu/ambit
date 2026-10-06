namespace Gambol.Server

open Gambol.Shared

/// Shared Find and Move Search Actor. Query does not use this module's walk.
[<RequireQualifiedAccess>]
module SearchActor =

    type Walk =
        { text: string
          zoomRoot: NodeId
          focusId: NodeId option
          graph: Graph
          matchKey: SearchPicture.Match }

    let private hitIds (text: string) (zoomRoot: NodeId) (graph: Graph) =
        match ViewModelSearch.startSearch text zoomRoot graph with
        | None -> []
        | Some cursor ->
            ViewModelSearch.takeResults System.Int32.MaxValue cursor
            |> fst
            |> List.map (fun hit -> hit.nodeId)

    /// One reply from the client search algorithm. No continuation cursor.
    /// Focus is supplied. The walk uses zoom and the graph.
    let oneReply (walk: Walk) : SearchPicture.Reply =
        { ids = hitIds walk.text walk.zoomRoot walk.graph
          matchKey = walk.matchKey }

    /// Event-source start. `zoomId` is root. `focusId` is focus, or root
    /// when the graph has no focus. `graphIds` is the root. The walk
    /// reads the full Graph. This record does not copy every Node id.
    let actorStart (graph: Graph) (eventId: EventId) : ActorStart =
        let focusId =
            match graph.focus with
            | Some id -> id
            | None -> graph.root
        { zoomId = graph.root
          focusId = focusId
          commandId = graph.root
          graphIds = [ graph.root ]
          eventId = eventId }

    /// The carrier supplies the full server graph. Root is that graph's root.
    let reply
        (getGraph: unit -> Graph)
        (request: SearchPicture.Request)
        : SearchPicture.Reply =
        let graph = getGraph ()
        oneReply
            { text = request.text
              zoomRoot = request.startId
              focusId = graph.focus
              graph = graph
              matchKey = SearchPicture.matchKeyOf request }
