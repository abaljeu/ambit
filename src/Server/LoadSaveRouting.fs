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

    /// Workspace Load and Save enter their actors on Plain or Git.
    /// Non-Workspace Load and explicit Desk stay Desk.
    let resolvePath
        (state: GraphAtEvent)
        (request: LoadSaveCommandRequest)
        : Result<LoadSavePath, string> =
        PathPick.resolveCommand
            (LoadPathChoice.ofRequest request state.graph)
