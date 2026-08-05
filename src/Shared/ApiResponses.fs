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
