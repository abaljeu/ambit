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

let private readFirstArrayString (root: JsonElement) (arrayName: string) (prop: string) =
    match root.TryGetProperty arrayName with
    | true, el when el.ValueKind = JsonValueKind.Array && el.GetArrayLength() > 0 ->
        let first = el.[0]
        match first.TryGetProperty prop with
        | true, v when v.ValueKind = JsonValueKind.String -> nonEmpty (Some(v.GetString()))
        | _ -> None
    | _ -> None

let private fromElement (root: JsonElement) : FileSettings =
    let apiFromAiKeys = readFirstArrayString root "AiKeys" "ApiKey"
    let repoFromAiRepos = readFirstArrayString root "AiRepos" "Url"
    let nameFromAiRepos = readFirstArrayString root "AiRepos" "Name"
    let refFromAiRepos = readFirstArrayString root "AiRepos" "StartingRef"
    { ApiKey = readString root "ApiKey" |> Option.orElse apiFromAiKeys
      Model = readString root "Model"
      Repo = readString root "Repo" |> Option.orElse repoFromAiRepos
      Ref = readString root "Ref" |> Option.orElse refFromAiRepos
      Name = readString root "Name" |> Option.orElse nameFromAiRepos }

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
    let env = environmentName()

    // Walk upward from a starting directory looking for either appsettings.json or appsettings.{env}.json
    let filenames = [ sprintf "appsettings.%s.json" env; "appsettings.json" ]
    let rec findUp (dir: string) : string option =
        let found = filenames |> List.exists (fun f -> File.Exists(Path.Combine(dir, f)))
        if found then Some dir
        else
            let di = DirectoryInfo(dir)
            if isNull di.Parent then None else findUp di.Parent.FullName

    match findUp cwd with
    | Some d -> d
    | None ->
        match findUp exe with
        | Some d -> d
        | None -> cwd

let loadFiles () =
    let dir = settingsDirectory ()
    let env = environmentName ()
    let basePath = Path.Combine(dir, "appsettings.json")
    let envPath = Path.Combine(dir, $"appsettings.{env}.json")
    Console.WriteLine(envPath)
    let baseFile =tryLoad basePath
    let envFile = tryLoad envPath
    match baseFile, envFile with
    | Some baseFile, Some envFile -> 
        Console.WriteLine("1here")

        overlay baseFile envFile
    | Some baseFile, None -> 
        Console.WriteLine("2here")
        baseFile
    | None, Some envFile -> 
        Console.WriteLine("here")
        envFile
    | None, None -> 
        Console.WriteLine("not here")
        emptyFile

let resolve cli file envApiKey =
    { Prompt = nonEmpty cli.Prompt
      ApiKey = pick cli.ApiKey file.ApiKey envApiKey
      Model = pick cli.Model file.Model None
      Repo = pick cli.Repo file.Repo None
      Ref = pick cli.Ref file.Ref None
      Name = pick cli.Name file.Name None }
