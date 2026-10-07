namespace Gambol.Server

open Gambol.Shared

/// Shared Find and Move Search Actor. Query does not use this module's walk.
[<RequireQualifiedAccess>]
module SearchActor =

    type Walk =
        { text: string
          zoomRoot: NodeId
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
    /// The walk uses zoom and the graph. Search does not take a focus.
    let oneReply (walk: Walk) : SearchPicture.Reply =
        { ids = hitIds walk.text walk.zoomRoot walk.graph
          matchKey = walk.matchKey }

    /// Event-source start. `zoomId`, `focusId`, and `commandId` are the
    /// root. `graphIds` is the root. Search does not read a focus.
    /// `focusId` holds the root so ActorStop can pair on that id.
    let actorStart (graph: Graph) (eventId: EventId) : ActorStart =
        { zoomId = graph.root
          focusId = graph.root
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
              zoomRoot = graph.root
              graph = graph
              matchKey = SearchPicture.matchKeyOf request }
