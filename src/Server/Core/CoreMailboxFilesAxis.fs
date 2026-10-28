namespace Gambol.Server

open Gambol.Shared

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
/// NodePersisting changes no node state. NodeUnpersisted sets
/// Unpersisted and leaves parse state unchanged.
/// PersistFailed does not change persist state.
/// NodeUnparsed writes Unparsed only.
/// NodePersisted runs the diff-count hook, sets persistedVersion
/// to the written version, and applies updateTime. Persisted is
/// set when the node is already Persisted, or when it is
/// Unpersisted and the written version is at least changedVersion.
/// An older write stays Unpersisted and changedVersion stays.
/// On Ok it calls notePersisted, which requests the file diff.
[<RequireQualifiedAccess>]
module internal CoreMailboxFilesAxis =

    let private writeNotice
        (persist: PersistHandlers)
        (notice: Notice)
        : Result<unit, string> =
        CoreEventDispatch.writeAxis
            persist
            notice
            (NoticeGraph.writeGraph notice)

    /// Marks the node Unparsed, then pushes. The mark writes the axis
    /// directly and does not enqueue.
    let markUnparsedThenPush
        (persist: PersistHandlers)
        (rawPush: NodeId -> unit)
        (nodeId: NodeId)
        : Result<unit, string> =
        writeNotice persist (Notice.NodeUnparsed nodeId)
        |> Result.map (fun () -> rawPush nodeId)

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
        writeNotice persist (Notice.NodeParsed nodeId)
        |> Result.map (fun () ->
            pushParsedUnpersisted persist nodeId
            persist.noteParsed nodeId)

    let writePullingFileUnpersisted
        (persist: PersistHandlers)
        (nodeId: NodeId)
        : Result<unit, string> =
        writeNotice persist (Notice.NodeUploading nodeId)

    let writeReceived
        (persist: PersistHandlers)
        (nodeId: NodeId)
        : Result<unit, string> =
        writeNotice persist (Notice.NodeReceived nodeId)

    let writePersisted persist nodeId persistedVersion updateTime =
        writeNotice
            persist
            (Notice.NodePersisted(nodeId, persistedVersion, updateTime))
