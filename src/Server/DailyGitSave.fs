namespace Gambol.Server

open System
open System.Globalization
open System.IO
open System.Threading.Tasks
open Microsoft.Extensions.Hosting

[<RequireQualifiedAccess>]
module DailyGitSave =

    [<Literal>]
    let commitMessage = "gambol: daily autosave"

    [<Literal>]
    let stampFileName = "gambol.git-save-day"

    let stampPath (dataDir: string) =
        Path.Combine(Bookkeeping.systemDir dataDir, stampFileName)

    let formatUtcDay (utc: DateTime) =
        utc.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)

    let shouldRunToday (stampText: string option) (utcDay: string) =
        match stampText with
        | Some text when text.Trim() = utcDay -> false
        | _ -> true

    let repoRoots
        (dataDirIsRepo: bool)
        (dataDir: string)
        (childRepos: string list)
        : string list =
        let head = if dataDirIsRepo then [ dataDir ] else []
        head @ childRepos

    let tryReadStamp (dataDir: string) : string option =
        let path = stampPath dataDir
        try
            if File.Exists path then Some(File.ReadAllText path) else None
        with _ ->
            None

    let writeStamp (dataDir: string) (utcDay: string) : Result<unit, string> =
        let path = stampPath dataDir
        try
            Directory.CreateDirectory(Bookkeeping.systemDir dataDir) |> ignore
            let tmpPath = path + ".tmp"
            File.WriteAllText(tmpPath, utcDay)
            File.Move(tmpPath, path, true)
            Ok ()
        with ex ->
            Error ex.Message

    let private listImmediateDirs (dataDir: string) : Result<string list, string> =
        try
            Directory.GetDirectories dataDir |> Array.toList |> Ok
        with ex ->
            Error ex.Message

    let discoverRepoRoots (dataDir: string) : Result<string list, string> =
        match listImmediateDirs dataDir with
        | Error err -> Error err
        | Ok children ->
            let childRepos = children |> List.filter WorkspaceGit.isRepo
            Ok(repoRoots (GitSave.isRepo dataDir) dataDir childRepos)

    let private isDataDirRoot (dataDir: string) (root: string) =
        let dataFull = Path.GetFullPath dataDir
        let rootFull = Path.GetFullPath root
        String.Equals(dataFull, rootFull, StringComparison.Ordinal)

    let private commitRoot (dataDir: string) (root: string) =
        if isDataDirRoot dataDir root then
            GitSave.commitAll root commitMessage
        else
            WorkspaceGit.commitAll root commitMessage None

    let walk (dataDir: string) : Result<unit, string> =
        match discoverRepoRoots dataDir with
        | Error err -> Error err
        | Ok roots ->
            let rec go remaining =
                match remaining with
                | [] -> Ok ()
                | root :: rest ->
                    match commitRoot dataDir root with
                    | Error err -> Error err
                    | Ok _ -> go rest
            go roots

    let tryRun (dataDir: string) (utcNow: DateTime) : Result<bool, string> =
        let day = formatUtcDay utcNow
        if not (shouldRunToday (tryReadStamp dataDir) day) then
            Ok false
        else
            match walk dataDir with
            | Error err -> Error err
            | Ok () ->
                match writeStamp dataDir day with
                | Error err -> Error err
                | Ok () -> Ok true

    let start (dataDir: string) (whenReady: Task) (utcNow: DateTime) : Task =
        task {
            let day = formatUtcDay utcNow
            if not (shouldRunToday (tryReadStamp dataDir) day) then
                ()
            else
                do! whenReady
                match tryRun dataDir utcNow with
                | Ok _ -> ()
                | Error err -> eprintfn "[DailyGitSave] %s" err
        }

    let register
        (lifetime: IHostApplicationLifetime)
        (dataDir: string)
        (whenReady: Task)
        =
        lifetime.ApplicationStarted.Register(fun () ->
            Task.Run(fun () -> start dataDir whenReady DateTime.UtcNow)
            |> ignore)
        |> ignore
