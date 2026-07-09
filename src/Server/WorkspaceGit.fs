namespace Gambol.Server

open System
open System.IO

[<RequireQualifiedAccess>]
module WorkspaceGit =

    let private workspaceRelative (label: string) =
        if label.StartsWith("@") then label else "@" + label

    let workspaceRepoDir (dataDir: string) (workspaceLabel: string) : string =
        Path.Combine(dataDir, workspaceRelative workspaceLabel)

    let ensureRepo (dataDir: string) (workspaceLabel: string) : Result<string, string> =
        let repoDir = workspaceRepoDir dataDir workspaceLabel

        try
            Directory.CreateDirectory(repoDir) |> ignore

            if GitSave.isRepo repoDir then
                Ok repoDir
            else
                match GitSave.runGit repoDir "init" with
                | Ok _ -> Ok repoDir
                | Error err -> Error err
        with ex ->
            Error ex.Message

    let status (dataDir: string) (workspaceLabel: string) : Result<string, string> =
        let repoDir = workspaceRepoDir dataDir workspaceLabel

        if not (GitSave.isRepo repoDir) then
            Ok "not a git repository"
        else
            GitSave.runGit repoDir "status --short"

    let commit (dataDir: string) (workspaceLabel: string) (message: string) : Result<string, string> =
        match ensureRepo dataDir workspaceLabel with
        | Error err -> Error err
        | Ok repoDir -> GitSave.commitAll repoDir message
