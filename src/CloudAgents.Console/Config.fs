module ConsoleConfig

open System
open System.IO
open System.Text.Json
open Gambol.CloudAgents

type CliArgs =
    { Prompt: string option
      ApiKey: string option
      Model: string option
      Params: ModelParam list
      Repo: string option
      Ref: string option
      Name: string option }

type FileSettings =
    { Model: string option
      ModelParams: ModelParam list
      Repo: string option
      Ref: string option
      Name: string option }

let emptyCli =
    { Prompt = None
      ApiKey = None
      Model = None
      Params = []
      Repo = None
      Ref = None
      Name = None }

let emptyFile =
    { Model = None
      ModelParams = []
      Repo = None
      Ref = None
      Name = None }

let private parseParamPair (text: string) =
    let parts = text.Split([| '=' |], 2)
    if parts.Length = 2 && parts.[0] <> "" then
        Some { ModelParam.Id = parts.[0]; Value = parts.[1] }
    else
        None

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
    | "--param" :: pv :: rest ->
        match parseParamPair pv with
        | Some p ->
            parseArgs rest
                { acc with Params = acc.Params @ [ p ] }
        | None -> parseArgs rest acc
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

let pickParams (cli: ModelParam list) (file: ModelParam list) =
    if not (List.isEmpty cli) then cli else file

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

let private readJsonString (el: JsonElement) (name: string) =
    match el.TryGetProperty name with
    | true, v when v.ValueKind = JsonValueKind.String ->
        Some(v.GetString())
    | _ -> None

let private readParamField el primary secondary =
    readJsonString el primary
    |> Option.orElse (readJsonString el secondary)

let private readParam (el: JsonElement) =
    match readParamField el "Id" "id", readParamField el "Value" "value" with
    | Some i, Some v when not (String.IsNullOrWhiteSpace i) ->
        Some { ModelParam.Id = i; Value = v }
    | _ -> None

let private readModelParams (root: JsonElement) =
    match root.TryGetProperty "ModelParams" with
    | true, el when el.ValueKind = JsonValueKind.Array ->
        el.EnumerateArray()
        |> Seq.choose readParam
        |> Seq.toList
    | _ -> []

let private readFirstArrayString
    (root: JsonElement)
    (arrayName: string)
    (prop: string)
    =
    match root.TryGetProperty arrayName with
    | true, el when
        el.ValueKind = JsonValueKind.Array
        && el.GetArrayLength() > 0 ->
        match el.EnumerateArray() |> Seq.tryHead with
        | Some first -> readString first prop
        | None -> None
    | _ -> None

let private fromElement (root: JsonElement) : FileSettings =
    let repoFromAiRepos = readFirstArrayString root "AiRepos" "Url"
    let nameFromAiRepos = readFirstArrayString root "AiRepos" "Name"
    let refFromAiRepos = readFirstArrayString root "AiRepos" "StartingRef"
    { Model = readString root "Model"
      ModelParams = readModelParams root
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
    { Model = pick over.Model baseSettings.Model None
      ModelParams =
          pickParams over.ModelParams baseSettings.ModelParams
      Repo = pick over.Repo baseSettings.Repo None
      Ref = pick over.Ref baseSettings.Ref None
      Name = pick over.Name baseSettings.Name None }

let private settingsFilenames env =
    [ sprintf "appsettings.%s.json" env; "appsettings.json" ]

let private findSettingsDir
    (filenames: string list)
    (start: string)
    : string option =
    let rec findUp (dir: string) =
        let found =
            filenames
            |> List.exists (fun f ->
                File.Exists(Path.Combine(dir, f)))
        if found then Some dir
        else
            let di = DirectoryInfo(dir)
            if isNull di.Parent then None
            else findUp di.Parent.FullName
    findUp start

let settingsDirectory () =
    let cwd = Directory.GetCurrentDirectory()
    let exe = AppContext.BaseDirectory
    let env = environmentName()
    let filenames = settingsFilenames env
    match findSettingsDir filenames cwd with
    | Some d -> d
    | None ->
        match findSettingsDir filenames exe with
        | Some d -> d
        | None -> cwd

let loadFiles () =
    let dir = settingsDirectory ()
    let env = environmentName ()
    let basePath = Path.Combine(dir, "appsettings.json")
    let envPath = Path.Combine(dir, $"appsettings.{env}.json")
    let baseFile = tryLoad basePath
    let envFile = tryLoad envPath
    match baseFile, envFile with
    | Some baseFile, Some envFile -> overlay baseFile envFile
    | Some baseFile, None -> baseFile
    | None, Some envFile -> envFile
    | None, None -> emptyFile

type ResolvedSettings =
    { Prompt: string option
      ApiKey: string option
      Model: string option
      ModelParams: ModelParam list
      Repo: string option
      Ref: string option
      Name: string option }

/// DefaultAiKey selects AiKeys:cursor. A missing name or empty value is None.
let apiKeyFromSecrets
    (defaultName: string option)
    (lookup: string -> string option)
    =
    match nonEmpty defaultName with
    | None -> None
    | Some name -> nonEmpty (lookup name)

let resolve
    (cli: CliArgs)
    (file: FileSettings)
    secretApiKey
    envApiKey
    : ResolvedSettings =
    { Prompt = nonEmpty cli.Prompt
      ApiKey = pick cli.ApiKey secretApiKey envApiKey
      Model = pick cli.Model file.Model None
      ModelParams = pickParams cli.Params file.ModelParams
      Repo = pick cli.Repo file.Repo None
      Ref = pick cli.Ref file.Ref None
      Name = pick cli.Name file.Name None }
