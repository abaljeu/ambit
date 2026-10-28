namespace Gambol.Shared

open System

/// Pure Notice graph writes shared by the server axis and the browser.
/// A missing node is an error. Callers that keep an absent node absent
/// decide that before calling.
[<RequireQualifiedAccess>]
module NoticeGraph =

    let private withPullingFile (nodeId: NodeId) (graph: Graph) =
        match Map.tryFind nodeId graph.nodes with
        | None -> Error "node not found"
        | Some node ->
            let updated = Node.withDocumentState PullingFile node
            Ok { graph with nodes = Map.add nodeId updated graph.nodes }

    let private writeUnpersisted (nodeId: NodeId) (graph: Graph) =
        GraphMutate.setPersistState
            nodeId
            PersistState.Unpersisted
            graph

    let private writePullingFileUnpersisted
        (nodeId: NodeId)
        (graph: Graph)
        =
        writeUnpersisted nodeId graph
        |> Result.bind (withPullingFile nodeId)

    let private writeReceived (nodeId: NodeId) (graph: Graph) =
        GraphMutate.setParseState nodeId ParseState.Unparsed graph
        |> Result.bind (
            GraphMutate.setPersistState nodeId PersistState.Persisted)

    let private stampUpdateTime
        (nodeId: NodeId)
        (updateTime: DateTime)
        (graph: Graph)
        : Result<Graph, string> =
        match Map.tryFind nodeId graph.nodes with
        | None -> Error "node not found"
        | Some node ->
            let stamped = NodeUpdateTime.withStamp updateTime node
            Ok { graph with nodes = Map.add nodeId stamped graph.nodes }

    let private withPersistedVersion
        (nodeId: NodeId)
        (persistedVersion: EventId)
        (graph: Graph)
        : Result<Graph, string> =
        match Map.tryFind nodeId graph.nodes with
        | None -> Error "node not found"
        | Some node ->
            let updated =
                { node with
                    documentState =
                        { node.documentState with
                            persistedVersion = persistedVersion } }
            Ok { graph with nodes = Map.add nodeId updated graph.nodes }

    /// setPersistState rejects a non-artifact. Version and file time follow.
    let private writePersisted
        (nodeId: NodeId)
        (persistedVersion: EventId)
        (updateTime: DateTime)
        (graph: Graph)
        =
        match Map.tryFind nodeId graph.nodes with
        | None -> Error "node not found"
        | Some node ->
            let next =
                DocumentState.persistStateForWritten
                    node.documentState
                    persistedVersion
            GraphMutate.setPersistState nodeId next graph
            |> Result.bind (withPersistedVersion nodeId persistedVersion)
            |> Result.bind (stampUpdateTime nodeId updateTime)

    /// Graph write for one Notice. Failure and in-progress notices
    /// leave the graph unchanged.
    let writeGraph (notice: Notice) (graph: Graph) : Result<Graph, string> =
        match notice with
        | Notice.NodeParsed nodeId ->
            GraphMutate.setParseState nodeId ParseState.Parsed graph
        | Notice.NodeUploading nodeId ->
            writePullingFileUnpersisted nodeId graph
        | Notice.NodeReceived nodeId ->
            writeReceived nodeId graph
        | Notice.NodeParsing _
        | Notice.ParseFailed _
        | Notice.NodePersisting _
        | Notice.PersistFailed _ ->
            Ok graph
        | Notice.NodeUnpersisted nodeId ->
            writeUnpersisted nodeId graph
        | Notice.NodeUnparsed nodeId ->
            GraphMutate.setParseState nodeId ParseState.Unparsed graph
        | Notice.NodePersisted (nodeId, written, time) ->
            writePersisted nodeId written time graph
