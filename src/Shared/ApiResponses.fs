namespace Gambol.Shared

open Gambol.Shared

/// Shared Poll/events/load protocol marker. Bump on incompatible wire or semantics.
/// Wire: integer (major*10 + minor); current is 13 for API version 1.3
/// (Want + edges/Nodes on Poll and post-Event).
[<RequireQualifiedAccess>]
module ApiVersion =
    let current = 13

/// Bootstrap graph scope for GET /state. Production clients use RootClosure.
/// Tests may request FullGraph via `?scope=full` on `/ambit/state`.
type BootstrapScope =
    | RootClosure
    | FullGraph

/// Response from GET /{file}/state.
type StateResponse =
    { graph: Graph
      eventId: Gambol.Shared.EventId
      isReady: bool
      /// GetState lockPresent overlay (mailbox live table). Not a Graph field.
      seedLiveFocusIds: Set<NodeId> }

/// Complete success response from POST /changes (or /events alias) and GET /poll.
type ChangeSuccessResponse =
    { eventId: Gambol.Shared.EventId
      buildEpochSec: int
      pageBuildEpochSec: int
      apiVersion: int
      isReady: bool
      externalChanges: bool
      events: Ev list
      /// File-write status when graph change succeeded but artifact save had issues.
      message: string option
      /// Optional ROOT-closure fingerprint; omitted by old Servers.
      bootstrapHash: string option
      /// Required Want-answer Nodes. Missing field fails decode.
      nodes: Node list
      /// Required Want-answer edges. Absent key stays Unloaded; [] is a Loaded leaf.
      childMap: Map<NodeId, ChildNode list> }

/// Want list on Poll and post-Event. Always send; empty compose is [].
type SyncWant =
    { want: NodeId list }

/// Poll body so Want shares the post-Event field (not a query string).
type PollRequest =
    { eventId: EventId
      want: NodeId list }

/// post-Event body with Want beside EventBatch.
type ChangeRequest =
    { events: Ev list
      want: NodeId list }

/// One selected Load target and whether its owning Workspace package is needed.
type LoadTarget =
    { targetId: NodeId
      includeWorkspace: bool }

/// Request body for POST /ambit/load (Fetch + Poll for the full selection).
type LoadRequest =
    { eventId: EventId
      targets: LoadTarget list }

/// Response from POST /ambit/load: Poll stamp envelope plus an edges-and-Nodes answer.
type LoadResponse =
    { eventId: EventId
      buildEpochSec: int
      pageBuildEpochSec: int
      apiVersion: int
      isReady: bool
      events: Ev list
      nodes: Node list
      childMap: Map<NodeId, ChildNode list> }

    member this.changes = this.events

/// Universal Command response. Either collection may be empty.
type UniversalResponse =
    { nodes: Node list
      events: Ev list
      latestId: EventId }

/// Cancel request: Focus to cancel and Client EventId cursor for the Event tail.
type CancelRequest =
    { focusId: NodeId
      eventId: EventId }

/// Authoritative Sync install: Event tail then edges-plus-Nodes answer.
type SyncResponse =
    { events: Ev list
      /// Want-answer Nodes. Load Fetch maps here too.
      nodes: Node list
      /// Want-answer edges. Absent key stays Unloaded; [] is a Loaded leaf.
      childMap: Map<NodeId, ChildNode list> }
