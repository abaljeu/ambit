namespace Gambol.Shared

/// Browser Run Command request. Product Focus / Command / Zoom may differ.
[<RequireQualifiedAccess>]
module CommandRequest =

    /// Literal `?` at the start of text selects an Actor by name.
    let isCommandText (text: string) =
        text.StartsWith("?")

    /// Owner-scan stop: `?` Actor Command or an Amble `=` line.
    let isScanStopText (text: string) =
        isCommandText text || text.Contains("=")

    let private afterQuestion (text: string) =
        if text.Length <= 1 then ""
        else text.Substring(1).Trim()

    let private firstToken (rest: string) =
        let space = rest.IndexOf(' ')
        if space < 0 then rest
        else rest.Substring(0, space)

    let private afterFirstToken (rest: string) =
        let space = rest.IndexOf(' ')
        if space < 0 then ""
        else rest.Substring(space + 1).Trim()

    /// Actor select from `?test hello` → `test`. Not the hello behavior.
    let actorNameFromText (text: string) =
        if not (isCommandText text) then None
        else
            let name = firstToken (afterQuestion text)
            if name = "" then None
            else Some (name.ToLowerInvariant())

    /// Behavior from `?test hello` → `hello`, or the whole trimmed text.
    let behaviorFromText (text: string) =
        if not (isCommandText text) then
            text.Trim().ToLowerInvariant()
        else
            afterFirstToken (afterQuestion text)
            |> fun rest -> rest.ToLowerInvariant()

    let private noRunnableCommand =
        "no runnable Command on Focus to Zoom path"

    let private ownerPathToZoom
        (graph: Graph)
        (focusId: NodeId)
        (zoomId: NodeId)
        : NodeId list option =
        let rec collect acc current visited =
            if Set.contains current visited then
                None
            elif current = zoomId then
                Some (List.rev (current :: acc))
            else
                match Map.tryFind current graph.ownerParentByChild with
                | None -> None
                | Some parentId ->
                    collect (current :: acc) parentId (Set.add current visited)

        collect [] focusId Set.empty

    let private firstScanStop (graph: Graph) (path: NodeId list) =
        path
        |> List.tryFind (fun id ->
            match Map.tryFind id graph.nodes with
            | Some node -> isScanStopText node.text
            | None -> false)

    let private nodeText (graph: Graph) (id: NodeId) =
        Map.tryFind id graph.nodes |> Option.map (fun node -> node.text)

    /// First `?` or `=` owner from Focus toward Zoom, inclusive of both.
    let scanStopOnOwnerPath
        (graph: Graph)
        (focusId: NodeId)
        (zoomId: NodeId)
        : NodeId option =
        ownerPathToZoom graph focusId zoomId
        |> Option.bind (firstScanStop graph)

    /// Actor Command id: scan-stop whose text starts with `?`. `=` is not one.
    let commandOnOwnerPath
        (graph: Graph)
        (focusId: NodeId)
        (zoomId: NodeId)
        : NodeId option =
        match scanStopOnOwnerPath graph focusId zoomId with
        | Some id ->
            match nodeText graph id with
            | Some text when isCommandText text -> Some id
            | _ -> None
        | None -> None

    /// Scan-stop is an Amble `=` line, not a `?` Actor Command.
    let isAmbleScanStop
        (graph: Graph)
        (focusId: NodeId)
        (zoomId: NodeId)
        : bool =
        Option.isSome (scanStopOnOwnerPath graph focusId zoomId)
        && Option.isNone (commandOnOwnerPath graph focusId zoomId)

    /// Product ActorStart. Zoom is the Included extract root. `=` is not Actor.
    let tryStart
        (graph: Graph)
        (siteMap: SiteMap)
        (zoomId: NodeId)
        (focusId: NodeId)
        (eventId: EventId)
        : Result<ActorStart, string> =
        match commandOnOwnerPath graph focusId zoomId with
        | None -> Error noRunnableCommand
        | Some commandId ->
            Ok
                { zoomId = zoomId
                  focusId = focusId
                  commandId = commandId
                  graphIds = IncludedDescendantIds.expand graph siteMap zoomId
                  eventId = eventId }

    /// One-Node ActorStart. Client supplies unfolded Included `graphIds`.
    let oneNodeStart
        (graph: Graph)
        (siteMap: SiteMap)
        (nodeId: NodeId)
        (eventId: EventId)
        : ActorStart =
        { zoomId = nodeId
          focusId = nodeId
          commandId = nodeId
          graphIds = IncludedDescendantIds.expand graph siteMap nodeId
          eventId = eventId }

    /// Editing commit wrote a new Error: do not ActorStart or Amble.
    let mayLaunchAfterEditCommit
        (wasEditing: bool)
        (before: CmdLastResult option)
        (after: CmdLastResult option)
        : bool =
        match wasEditing, after with
        | true, Some (CmdLastResult.Error _) when after <> before ->
            false
        | _ -> true

    /// SubmitCommand only when launch after edit commit is allowed.
    let commandSubmitEffects
        (mayLaunch: bool)
        (request: Result<ActorStart, string>)
        : Effect list =
        match mayLaunch, request with
        | true, Ok req -> [ SubmitCommand req ]
        | _ -> []
