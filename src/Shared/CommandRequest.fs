namespace Gambol.Shared

/// Browser Run Command request. One Node is Command, Zoom root, and Focus.
[<RequireQualifiedAccess>]
module CommandRequest =

    /// Literal `?` at the start of current Node text selects the Command path.
    let isCommandText (text: string) =
        text.StartsWith("?")

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
