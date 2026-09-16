namespace Gambol.Shared

open Gambol.Shared

/// Shared Poll/events/load protocol marker. Bump on incompatible wire or semantics.
/// Wire: integer (major*10 + minor); current is 11 for API version 1.1.
[<RequireQualifiedAccess>]
module ApiVersion =
    let current = 11

/// Bootstrap graph scope for GET /state. Production clients use RootClosure.
/// Tests may request FullGraph via `?scope=full` on `/ambit/state`.
type BootstrapScope =
    | RootClosure
    | FullGraph

/// Response from GET /{file}/state.
type StateResponse =
    { graph: Graph
      revision: Gambol.Shared.EventId
      isReady: bool }

/// Complete success response from POST /changes (or /events alias) and GET /poll.
type ChangeSuccessResponse =
    { revision: Gambol.Shared.EventId
      buildEpochSec: int
      pageBuildEpochSec: int
      apiVersion: int
      isReady: bool
      externalChanges: bool
      events: Gambol.Shared.Ev list
      /// File-write status when graph change succeeded but artifact save had issues.
      message: string option
      /// Optional ROOT-closure fingerprint; omitted by old Servers.
      bootstrapHash: string option }

    member this.changes = this.events

/// One selected Load target and whether its owning Workspace package is needed.
type LoadTarget =
    { targetId: NodeId
      includeWorkspace: bool }

/// Request body for POST /ambit/load (Fetch + Poll for the full selection).
type LoadRequest =
    { revision: EventId
      targets: LoadTarget list }

/// Response from POST /ambit/load: Poll stamp envelope plus optional Workspace subgraphs.
type LoadResponse =
    { revision: EventId
      buildEpochSec: int
      pageBuildEpochSec: int
      apiVersion: int
      isReady: bool
      events: Gambol.Shared.Ev list
      /// Complete Workspace subgraph Nodes at the response Revision (wire: packages).
      packages: Node list }

    member this.changes = this.events

/// Authoritative Sync install: ordered Change tail plus optional resident packages.
type SyncResponse =
    { events: Gambol.Shared.Ev list
      /// Complete Workspace / child-list snapshots at the response revision.
      packages: Node list }

    member this.changes = this.events
