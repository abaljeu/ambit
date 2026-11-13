namespace Gambol.Server

open Gambol.Shared

/// Shared Find and Move Search Actor. Query does not use this module's walk.
[<RequireQualifiedAccess>]
module SearchActor =

    type Walk =
        { text: string
          zoomRoot: NodeId
          graph: Graph
          matchKey: SearchPicture.Match
          count: int }

    let private hitIds (walk: Walk) =
        match ViewModelSearch.startSearch walk.text walk.zoomRoot walk.graph with
        | None -> []
        | Some cursor ->
            let capped = ViewModelSearch.withResultCap walk.count cursor
            ViewModelSearch.takeResults System.Int32.MaxValue capped
            |> fst
            |> List.map (fun hit -> hit.nodeId)

    /// One reply from the client search algorithm. No continuation cursor.
    /// The walk uses zoom and the graph. Search does not take a focus.
    let oneReply (walk: Walk) : SearchPicture.Reply =
        { ids = hitIds walk
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
              matchKey = SearchPicture.matchKeyOf request
              count = request.count }

    /// The command Node on the server Graph is a query line.
    let isQueryRequest (graph: Graph) (request: ActorStart) =
        match Map.tryFind request.runDefinitionId graph.nodes with
        | None -> false
        | Some node -> CommandRequest.isQueryText node.text

    let private actorCaller (secret: Credential) : Caller =
        { authority = Authority "Actor"
          name = ""
          secret = secret }

    /// One eval on the carrier Graph. `graphIds` do not select that Graph.
    let private queryIds (getGraph: unit -> Graph) (lineId: NodeId) =
        let graph = getGraph ()
        match Map.tryFind lineId graph.nodes with
        | None -> []
        | Some node -> ExprRun.answerNodeIds lineId graph node.text

    /// One query eval. The caller supplies the stop door.
    let private queryActorBody
        (getGraph: unit -> Graph)
        (lineId: NodeId)
        (stop: ActorResult -> Async<Result<unit, string>>)
        =
        async {
            let result = ActorQuery (queryIds getGraph lineId)
            let! _ = stop result
            return ()
        }

    /// Query Actor body. ActorStop carries the Node ids. It does not use
    /// the Find walk.
    let functionStart
        (request: ActorStart)
        (getGraph: GraphCarrier)
        : FunctionStart =
        let lineId = request.runDefinitionId
        { actor =
            fun run changes ->
                let door =
                    CoreDoor.forCaller (actorCaller run.secret) changes
                queryActorBody
                    run.getGraph
                    lineId
                    (CoreDoor.actorStop door)
          getGraph = getGraph
          actorNodeId = request.focusId }

    /// Query command. `runDefinitionId` is the equals line.
    let functionActorAt
        (runDefinitionId: NodeId)
        (_focusId: NodeId)
        : ActorFunction =
        fun run changes ->
            queryActorBody
                run.getGraph
                runDefinitionId
                changes.actorStop

    /// Find and Move actor. The walk stays in the search reply.
    let searchActorAt : ActorFunction =
        fun _run changes ->
            async {
                let! _ = changes.actorStop ActorSucceeded
                return ()
            }
