namespace Gambol.Shared

open Gambol.Shared.Events

/// Shared Poll/events/load protocol marker. Bump on incompatible wire or semantics.
[<RequireQualifiedAccess>]
module ApiVersion =
    let current = 1

/// Bootstrap graph scope for GET /state. Production clients use RootClosure.
/// Tests may request FullGraph via `?scope=full` on `/ambit/state`.
type BootstrapScope =
    | RootClosure
    | FullGraph

/// Response from GET /{file}/state.
type StateResponse =
    { graph: Graph
      revision: Gambol.Shared.Events.EventId
      isReady: bool }

/// Complete success response from POST /events and GET /poll.
type ChangeSuccessResponse =
    { revision: Gambol.Shared.Events.EventId
      buildEpochSec: int
      pageBuildEpochSec: int
      apiVersion: int
      isReady: bool
      externalChanges: bool
      events: Gambol.Shared.Events.Event list
      /// File-write status when graph change succeeded but artifact save had issues.
      message: string option
      /// Optional ROOT-closure fingerprint; omitted by old Servers.
      bootstrapHash: string option }

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
      events: Gambol.Shared.Events.Event list
      /// Complete Workspace subgraph Nodes at the response Revision (wire: packages).
      packages: Node list }

/// Authoritative Sync install: ordered Change tail plus optional resident packages.
type SyncResponse =
    { events: Gambol.Shared.Events.Event list
      /// Complete Workspace / child-list snapshots at the response revision.
      packages: Node list }
