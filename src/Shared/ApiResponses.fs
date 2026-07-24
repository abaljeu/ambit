namespace Gambol.Shared

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
