namespace Gambol.Shared

/// Bootstrap graph scope for GET /state. Production clients use RootClosure.
/// Tests may request FullGraph via `?scope=full` on `/ambit/state`.
type BootstrapScope =
    | RootClosure
    | FullGraph

/// Response from GET /{file}/state.
type StateResponse =
    { graph: Graph
      revision: Revision
      isReady: bool }

/// Response from GET /{file}/poll.
type PollResponse =
    { revision: int
      buildEpochSec: int
      pageBuildEpochSec: int
      isReady: bool
      changes: Change list }

/// Request body for POST /ambit/load (Fetch + Poll for one Focus target).
type LoadRequest =
    { revision: int
      targetId: NodeId
      includeWorkspace: bool }

/// Response from POST /ambit/load: Poll stamp envelope plus optional Workspace subgraphs.
type LoadResponse =
    { revision: int
      buildEpochSec: int
      pageBuildEpochSec: int
      isReady: bool
      changes: Change list
      /// Complete Workspace subgraph Nodes at the response Revision (wire: packages).
      packages: Node list }

/// Authoritative Sync install: ordered Change tail plus optional resident packages.
type SyncResponse =
    { changes: Change list
      /// Complete Workspace / child-list snapshots at the response revision.
      packages: Node list }
