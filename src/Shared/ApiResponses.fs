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

/// Complete success response from POST /changes and GET /poll.
type ChangeSuccessResponse =
    { revision: Revision
      buildEpochSec: int
      pageBuildEpochSec: int
      isReady: bool
      externalChanges: bool
      changes: Change list
      /// File-write status when graph change succeeded but artifact save had issues.
      message: string option }

/// One selected Load target and whether its owning Workspace package is needed.
type LoadTarget =
    { targetId: NodeId
      includeWorkspace: bool }

/// Request body for POST /ambit/load (Fetch + Poll for the full selection).
type LoadRequest =
    { revision: int
      targets: LoadTarget list }

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
