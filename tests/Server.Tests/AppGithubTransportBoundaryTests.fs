module Gambol.Server.Tests.AppGithubTransportBoundaryTests

open System
open System.IO
open Xunit

let rec private findRepositoryRoot (directory: DirectoryInfo) =
    if File.Exists(Path.Combine(directory.FullName, "gambol.sln")) then
        directory.FullName
    elif isNull directory.Parent then
        failwith "Could not find repository root"
    else
        findRepositoryRoot directory.Parent

let private repositoryRoot =
    findRepositoryRoot (DirectoryInfo AppContext.BaseDirectory)

let private appSourceFiles () =
    [ "src/Client"; "src/Desktop" ]
    |> List.collect (fun relativePath ->
        Directory.GetFiles(
            Path.Combine(repositoryRoot, relativePath),
            "*.fs",
            SearchOption.TopDirectoryOnly)
        |> Array.toList)

let private assertSourceOmits forbidden =
    appSourceFiles ()
    |> List.iter (fun path ->
        let source = File.ReadAllText path
        forbidden
        |> List.iter (fun term ->
            Assert.DoesNotContain(
                term,
                source,
                StringComparison.OrdinalIgnoreCase)))

[<Fact>]
let ``App projects do not reference the Server project`` () =
    [ "src/Client/Gambol.Client.fsproj"
      "src/Desktop/Gambol.Desktop.fsproj" ]
    |> List.iter (fun relativePath ->
        let project =
            File.ReadAllText(Path.Combine(repositoryRoot, relativePath))
        Assert.DoesNotContain(
            "Gambol.Server.fsproj",
            project,
            StringComparison.OrdinalIgnoreCase))

[<Fact>]
let ``App source does not host or invoke GitHub transport`` () =
    assertSourceOmits
        [ "GithubTransportActor"
          "CoreActorPool"
          "registerPeer"
          "GitRun"
          "WorkspaceGit"
          "System.Diagnostics.Process" ]

[<Fact>]
let ``App source contains no GitHub remote branch or credential state`` () =
    assertSourceOmits
        [ "githubToken"
          "githubCredential"
          "remoteMap"
          "trackedBranch"
          "credential.helper"
          "git remote"
          "git pull"
          "git push" ]
