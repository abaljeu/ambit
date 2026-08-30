namespace Gambol.Server

open Gambol.Shared

/// Load canonical document state from disk (same semantics as `FileAgent` startup).
[<RequireQualifiedAccess>]
module DocumentLoader =

    let private loadGraphFromDisk (dataDir: string) : Result<Graph, string> =
        DocumentPersistence.readAllDocuments dataDir

    let private stateFromGraph (dataDir: string) (graph: Graph) : State =
        { graph = graph
          history = History.empty
          revision = Bookkeeping.readRevision dataDir }

    /// Read `.amb` network from disk (empty graph when no artifacts exist).
    let tryLoadState (dataDir: string) : Result<State, string> =
        loadGraphFromDisk dataDir
        |> Result.map (stateFromGraph dataDir)

    /// Fail-fast wrapper around `tryLoadState`.
    let loadState (dataDir: string) : State =
        match tryLoadState dataDir with
        | Ok state -> state
        | Error msg -> failwith msg
