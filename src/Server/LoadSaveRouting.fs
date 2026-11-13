namespace Gambol.Server

open Gambol.Shared

type LoadSaveCommandRouter =
    { resolvePath:
        GraphAtEvent -> LoadSaveCommandRequest -> Result<LoadSavePath, string>
      startCommand:
        LoadSavePath ->
        LoadSaveCommandRequest ->
        Async<Result<unit, string>> }

[<RequireQualifiedAccess>]
module LoadSaveRouting =

    /// Plain Save enters the Save actor. Desk is an explicit pre-pick,
    /// including the client's local-mapping choice.
    let resolvePath
        (state: GraphAtEvent)
        (request: LoadSaveCommandRequest)
        : Result<LoadSavePath, string> =
        PathPick.resolveCommand
            (LoadPathChoice.ofRequest request state.graph)
