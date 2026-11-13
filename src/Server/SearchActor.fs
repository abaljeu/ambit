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

    /// Event-source start. `zoomId`, `focusId`, and `runDefinitionId` are the
    /// root. `graphIds` is the root. Search does not read a focus.
    /// `focusId` holds the root so ActorStop can pair on that id.
    let actorStart (graph: Graph) (eventId: EventId) : ActorStart =
        { zoomId = graph.root
          focusId = graph.root
          runDefinitionId = graph.root
          graphIds = [ graph.root ]
          eventId = eventId }

    /// The carrier supplies the full server graph.
    /// The walk starts at `request.startId`, the zoom root.
    let reply
        (getGraph: unit -> Graph)
        (request: SearchPicture.Request)
        : SearchPicture.Reply =
        let graph = getGraph ()
        oneReply
            { text = request.text
              zoomRoot = request.startId
              graph = graph
              matchKey = SearchPicture.matchKeyOf request }

    /// The command Node on the server Graph is a query line.
    let isQueryRequest (graph: Graph) (request: ActorStart) =
        match Map.tryFind request.runDefinitionId graph.nodes with
        | None -> false
        | Some node -> CommandRequest.isQueryText node.text

    /// One eval on the carrier Graph. `graphIds` do not select that Graph.
    let private queryIds (getGraph: unit -> Graph) (lineId: NodeId) =
        let graph = getGraph ()
        match Map.tryFind lineId graph.nodes with
        | None -> []
        | Some node -> ExprRun.answerNodeIds lineId graph node.text

    /// One query eval. The result is the ActorQuery of that line.
    let private queryActorBody
        (getGraph: unit -> Graph)
        (lineId: NodeId)
        : Async<ActorResult> =
        async.Return (ActorQuery (queryIds getGraph lineId))

    /// Query Actor body. ActorStop carries the Node ids. It does not use
    /// the Find walk.
    let functionStart
        (request: ActorStart)
        (getGraph: GraphCarrier)
        : FunctionStart =
        let lineId = request.runDefinitionId
        { actor =
            fun run _ ->
                queryActorBody run.getGraph lineId
          getGraph = getGraph
          actorNodeId = request.focusId }

    /// Query command. `runDefinitionId` is the equals line.
    let functionActorAt
        (runDefinitionId: NodeId)
        (_focusId: NodeId)
        : ActorFunction =
        fun run _ ->
            queryActorBody run.getGraph runDefinitionId

    /// Find and Move actor. The walk stays in the search reply.
    let searchActorAt : ActorFunction =
        fun _run _changes ->
            async.Return ActorSucceeded
