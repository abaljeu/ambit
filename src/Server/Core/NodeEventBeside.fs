namespace Gambol.Server

open Gambol.Shared

/// Posts document-state InMsg beside an existing Op.SetDocumentState.
/// The post does not replace that write.
[<RequireQualifiedAccess>]
module internal NodeEventBeside =

    let private changeOps (event: Ev) =
        match event.body with
        | EventBody.Change ops -> ops
        | _ -> []

    let private stateOps (ops: Op list) =
        ops
        |> List.choose (function
            | Op.SetDocumentState(id, oldState, newState) ->
                Some(id, oldState, newState)
            | _ -> None)

    let uploading (ops: Op list) =
        stateOps ops
        |> List.choose (function
            | id, _, NoServerFile -> Some(InMsg.NodeUploading id)
            | _ -> None)

    let receivedAbsent (ops: Op list) =
        stateOps ops
        |> List.choose (function
            | id, NoServerFile, Unparsed -> Some(InMsg.NodeReceived id)
            | _ -> None)

    /// Import Change: Parsed mark, and no upload or arrival write.
    let importParsed (ops: Op list) =
        let states = stateOps ops
        let blocksImport =
            states
            |> List.exists (function
                | _, _, NoServerFile
                | _, _, Unparsed -> true
                | _ -> false)
        if blocksImport then
            []
        else
            states
            |> List.choose (function
                | id, Unparsed, Current -> Some(InMsg.NodeParsed id)
                | _ -> None)

    let parsedMarks (ops: Op list) =
        stateOps ops
        |> List.choose (function
            | id, Unparsed, Current -> Some(InMsg.NodeParsed id)
            | _ -> None)

    let receivedExistingFiles (graph: Graph) (ops: Op list) =
        stateOps ops
        |> List.choose (function
            | id, _, Unparsed ->
                match Map.tryFind id graph.nodes with
                | Some { kind = Special File } ->
                    Some(InMsg.NodeReceived id)
                | _ -> None
            | _ -> None)

    let private besideOne (event: Ev) =
        let ops = changeOps event
        uploading ops @ receivedAbsent ops @ importParsed ops

    let forPostedEvents (events: Ev list) =
        events |> List.collect besideOne

    let post (host: MailboxHost) (msgs: InMsg list) =
        msgs |> List.iter (CoreMailboxApi.addInMsg host)
