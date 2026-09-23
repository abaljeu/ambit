namespace Gambol.CloudAgents.Internal

open System
open System.IO
open System.Reflection

module CursorModelsFile =

    let private fileName = "cursor-models.json"

    let private candidatePaths () =
        let baseDir = AppContext.BaseDirectory
        let asmDir =
            match Assembly.GetExecutingAssembly().Location with
            | null
            | "" -> None
            | loc ->
                match Path.GetDirectoryName loc with
                | null
                | "" -> None
                | d -> Some d
        [
            Path.Combine(baseDir, fileName)
            match asmDir with
            | Some d when d <> baseDir -> Path.Combine(d, fileName)
            | _ -> ()
        ]

    let tryFindPath () =
        candidatePaths ()
        |> List.tryFind File.Exists

    let loadFromFile ()
        : Result<CursorTypes.CursorModel list, string> =
        match tryFindPath () with
        | None ->
            Error $"Missing {fileName} beside the assembly"
        | Some path ->
            try
                File.ReadAllText path
                |> CursorHttp.parseModelCatalog
            with ex ->
                Error $"Could not read {fileName}: {ex.Message}"

    /// File catalog when present; otherwise live GET /v1/models.
    let loadStartCatalog
        (apiKey: string)
        : Result<CursorTypes.CursorModel list, string> =
        match tryFindPath () with
        | Some _ -> loadFromFile ()
        | None -> CursorHttp.listModels apiKey
