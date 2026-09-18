namespace Gambol.Shared

/// Browser Run Command request. One Node is Command, Zoom root, and Focus.
[<RequireQualifiedAccess>]
module CommandRequest =

    /// Literal `?` at the start of current Node text selects the Command path.
    let isCommandText (text: string) =
        text.StartsWith("?")

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
