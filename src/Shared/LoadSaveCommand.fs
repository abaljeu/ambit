namespace Gambol.Shared

[<RequireQualifiedAccess>]
type LoadSaveOperation =
    | Load
    | Save

type LoadSaveCommandRequest =
    { operation: LoadSaveOperation
      prePick: LoadSavePrePick
      start: ActorStart }

type LoadSaveCommandResponse =
    { path: LoadSavePath
      command: UniversalResponse option }
