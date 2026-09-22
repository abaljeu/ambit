module ConsoleConfig

open System
open System.IO
open System.Text.Json

type CliArgs =
    { Prompt: string option
      ApiKey: string option
      Model: string option
      Repo: string option
      Ref: string option
      Name: string option }

type FileSettings =
    { ApiKey: string option
      Model: string option
      Repo: string option
      Ref: string option
      Name: string option }

let emptyCli =
    { Prompt = None
      ApiKey = None
      Model = None
      Repo = None
      Ref = None
      Name = None }

let emptyFile =
    { ApiKey = None
      Model = None
      Repo = None
      Ref = None
      Name = None }

let rec parseArgs (args: string list) (acc: CliArgs) =
    match args with
    | [] -> acc
    | "--repo" :: url :: rest ->
        parseArgs rest { acc with Repo = Some url }
    | "--ref" :: r :: rest ->
        parseArgs rest { acc with Ref = Some r }
    | "--name" :: n :: rest ->
        parseArgs rest { acc with Name = Some n }
    | "--model" :: m :: rest ->
        parseArgs rest { acc with Model = Some m }
    | "--api-key" :: k :: rest ->
        parseArgs rest { acc with ApiKey = Some k }
    | text :: rest ->
        match acc.Prompt with
        | None -> parseArgs rest { acc with Prompt = Some text }
        | Some _ -> parseArgs rest acc

let private nonEmpty (value: string option) =
    match value with
    | Some v when not (String.IsNullOrWhiteSpace v) -> Some v
    | _ -> None

let pick cli file fallback =
    nonEmpty cli
    |> Option.orElse (nonEmpty file)
    |> Option.orElse (nonEmpty fallback)

let environmentName () =
    let asp =
        Environment.GetEnvironmentVariable "ASPNETCORE_ENVIRONMENT"
    let dot =
        Environment.GetEnvironmentVariable "DOTNET_ENVIRONMENT"
    match nonEmpty (Option.ofObj asp), nonEmpty (Option.ofObj dot) with
    | Some name, _ -> name
    | None, Some name -> name
    | None, None -> "Development"

let private readString (root: JsonElement) (name: string) =
    match root.TryGetProperty name with
    | true, el when el.ValueKind = JsonValueKind.String ->
        nonEmpty (Some(el.GetString()))
    | _ -> None

let private fromElement (root: JsonElement) : FileSettings =
    { ApiKey = readString root "ApiKey"
      Model = readString root "Model"
      Repo = readString root "Repo"
      Ref = readString root "Ref"
      Name = readString root "Name" }

let tryLoad path =
    if File.Exists path then
        use stream = File.OpenRead path
        let doc = JsonDocument.Parse stream
        Some(fromElement doc.RootElement)
    else
        None

let overlay (baseSettings: FileSettings) (over: FileSettings) =
    { ApiKey = pick over.ApiKey baseSettings.ApiKey None
      Model = pick over.Model baseSettings.Model None
      Repo = pick over.Repo baseSettings.Repo None
      Ref = pick over.Ref baseSettings.Ref None
      Name = pick over.Name baseSettings.Name None }

let settingsDirectory () =
    let cwd = Directory.GetCurrentDirectory()
    let exe = AppContext.BaseDirectory
    let hasBase dir =
        File.Exists(Path.Combine(dir, "appsettings.json"))
    if hasBase cwd then cwd
    elif hasBase exe then exe
    else cwd

let loadFiles () =
    let dir = settingsDirectory ()
    let env = environmentName ()
    let basePath = Path.Combine(dir, "appsettings.json")
    let envPath = Path.Combine(dir, $"appsettings.{env}.json")
    match tryLoad basePath, tryLoad envPath with
    | Some baseFile, Some envFile -> overlay baseFile envFile
    | Some baseFile, None -> baseFile
    | None, Some envFile -> envFile
    | None, None -> emptyFile

let resolve cli file envApiKey =
    { Prompt = nonEmpty cli.Prompt
      ApiKey = pick cli.ApiKey file.ApiKey envApiKey
      Model = pick cli.Model file.Model None
      Repo = pick cli.Repo file.Repo None
      Ref = pick cli.Ref file.Ref None
      Name = pick cli.Name file.Name None }
