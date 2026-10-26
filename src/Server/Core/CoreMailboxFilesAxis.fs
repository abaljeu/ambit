namespace Gambol.Server

open System
open Gambol.Shared

[<RequireQualifiedAccess>]
module internal CoreMailboxFilesAxis =

    let writeParseState
        (persist: PersistHandlers)
        (notice: Notice)
        (nodeId: NodeId)
        (parseState: ParseState)
        : Result<unit, string> =
        CoreEventDispatch.writeAxis persist notice (fun graph ->
            GraphMutate.setParseState nodeId parseState graph)

    /// Marks the node Unparsed, then pushes. The mark writes the axis
    /// directly and does not enqueue.
    let markUnparsedThenPush
        (persist: PersistHandlers)
        (rawPush: NodeId -> unit)
        (nodeId: NodeId)
        : Result<unit, string> =
        writeParseState
            persist
            (Notice.NodeUnparsed nodeId)
            nodeId
            ParseState.Unparsed
        |> Result.map (fun () -> rawPush nodeId)

    /// Core loop apply. NodeParsed writes Parsed only when pullingFile
    /// is clear, then pushes when the node is Parsed and Unpersisted.
    /// The push does not wait. noteParsed still runs. When pullingFile
    /// is set, that File Node stays Unparsed. It does not set Persisted.
    /// NodeParsed is the renamed ParseFinished.
    /// NodeUploading sets PullingFile in memory and Unpersisted. It does
    /// not insert the node. The column word stays Parsed or Unparsed.
    /// NodeReceived sets Unparsed and Persisted. withParseState clears
    /// PullingFile. The queue puller logs both messages.
    /// NodeParsing is logged by the queue puller. It does not set Unparsed,
    /// does not change parse or persist state, and does not push.
    /// NodePersisting sets Unpersisted and leaves parse state unchanged.
    /// PersistFailed does not change persist state.
    /// NodeUnparsed writes Unparsed only.
    /// NodePersisted runs the diff-count hook, sets persistedVersion
    /// to the written version, and applies updateTime. Persisted is
    /// set when the node is already Persisted, or when it is
    /// Unpersisted and the written version is at least changedVersion.
    /// An older write stays Unpersisted and changedVersion stays.
    /// On Ok it calls notePersisted, which requests the file diff.
    let private withPullingFile (nodeId: NodeId) (graph: Graph) =
        match Map.tryFind nodeId graph.nodes with
        | None -> Error "node not found"
        | Some node ->
            let updated = Node.withDocumentState PullingFile node
            Ok { graph with nodes = Map.add nodeId updated graph.nodes }

    let writeUnpersisted
        (nodeId: NodeId)
        (live: Graph)
        : Result<Graph, string> =
        GraphMutate.setPersistState
            nodeId
            PersistState.Unpersisted
            live

    let writePullingFileUnpersisted
        (persist: PersistHandlers)
        (nodeId: NodeId)
        : Result<unit, string> =
        CoreEventDispatch.writeAxis
            persist
            (Notice.NodeUploading nodeId)
            (fun live ->
                writeUnpersisted nodeId live
                |> Result.bind (withPullingFile nodeId))

    let writeReceived
        (persist: PersistHandlers)
        (nodeId: NodeId)
        : Result<unit, string> =
        CoreEventDispatch.writeAxis
            persist
            (Notice.NodeReceived nodeId)
            (fun live ->
            match GraphMutate.setParseState nodeId ParseState.Unparsed live with
            | Error err -> Error err
            | Ok graph ->
                GraphMutate.setPersistState
                    nodeId
                    PersistState.Persisted
                    graph)

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

    let private parsedAndUnpersisted (node: Node) =
        node.parseState = ParseState.Parsed
        && node.persistState = PersistState.Unpersisted

    /// Read the node after Parsed is written. Push only that case.
    let private pushParsedUnpersisted
        (persist: PersistHandlers)
        (nodeId: NodeId)
        =
        match persist.getState () with
        | Error _ -> ()
        | Ok state ->
            match Map.tryFind nodeId state.graph.nodes with
            | Some node when parsedAndUnpersisted node ->
                persist.pushPersist nodeId
            | _ -> ()

    let applyNodeParsed
        (persist: PersistHandlers)
        (nodeId: NodeId)
        =
        writeParseState
            persist
            (Notice.NodeParsed nodeId)
            nodeId
            ParseState.Parsed
        |> Result.map (fun () ->
            pushParsedUnpersisted persist nodeId
            persist.noteParsed nodeId)

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

    let writePersisted persist nodeId persistedVersion updateTime =
        CoreEventDispatch.writeAxis
            persist
            (Notice.NodePersisted(nodeId, persistedVersion, updateTime))
            (fun live ->
                match Map.tryFind nodeId live.nodes with
                | None -> Error "node not found"
                | Some node ->
                    let next =
                        DocumentState.persistStateForWritten
                            node.documentState
                            persistedVersion
                    GraphMutate.setPersistState nodeId next live
                    |> Result.bind (
                        withPersistedVersion nodeId persistedVersion)
                    |> Result.bind (stampUpdateTime nodeId updateTime))
