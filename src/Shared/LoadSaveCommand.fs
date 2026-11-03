namespace Gambol.Shared

type LoadSaveCommandResponse =
    { path: LoadSavePath
      command: UniversalResponse option }

[<RequireQualifiedAccess>]
[<CompilationRepresentation(CompilationRepresentationFlags.ModuleSuffix)>]
module LoadPathChoice =

    let ofRequest
        (request: LoadSaveCommandRequest)
        (graph: Graph)
        : LoadPathChoice =
        { operation = request.operation
          prePick = request.prePick
          subject = PathPick.subjectOf graph request.start.focusId }
