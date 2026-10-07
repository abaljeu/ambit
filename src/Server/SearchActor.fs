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

    /// The command Node on the server Graph is a query line.
    let isQueryRequest (graph: Graph) (request: ActorStart) =
        match Map.tryFind request.commandId graph.nodes with
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

    /// Query Actor body. ActorStop carries the Node ids. It does not use
    /// the Find walk.
    let functionStart
        (request: ActorStart)
        (getGraph: GraphCarrier)
        : FunctionStart =
        let lineId = request.commandId
        { actor =
            fun run changes ->
                async {
                    let result = ActorQuery (queryIds run.getGraph lineId)
                    let! _ =
                        changes
                            .asCaller(actorCaller run.secret)
                            .actorStop result
                    return ()
                }
          getGraph = getGraph
          focusId = request.focusId
          commandId = request.commandId }
