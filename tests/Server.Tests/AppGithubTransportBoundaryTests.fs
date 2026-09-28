module Gambol.Server.Tests.AppGithubTransportBoundaryTests

open System
open System.IO
open System.Text.RegularExpressions
open System.Xml.Linq
open Xunit

let rec private findRepositoryRoot (directory: DirectoryInfo) =
    if File.Exists(Path.Combine(directory.FullName, "gambol.sln")) then
        Some directory.FullName
    elif isNull directory.Parent then
        None
    else
        findRepositoryRoot directory.Parent

let private repositoryRoot =
    match findRepositoryRoot (DirectoryInfo AppContext.BaseDirectory) with
    | Some root -> root
    | None ->
        Assert.Fail("Could not find repository root")
        ""

let private repositoryPath (relative: string) =
    Path.GetFullPath(Path.Combine(repositoryRoot, relative))

let private appProjects =
    [ "src/Client/Gambol.Client.fsproj"
      "src/Desktop/Gambol.Desktop.fsproj" ]
    |> List.map repositoryPath

let private projectItems (itemName: string) (projectPath: string) =
    let projectDirectory = Path.GetDirectoryName projectPath
    XDocument.Load(projectPath).Descendants()
    |> Seq.filter (fun element -> element.Name.LocalName = itemName)
    |> Seq.choose (fun element ->
        element.Attribute(XName.Get "Include") |> Option.ofObj)
    |> Seq.map (fun attribute ->
        let includePath =
            attribute.Value.Replace(
                '\\',
                Path.DirectorySeparatorChar)
        Path.GetFullPath(Path.Combine(projectDirectory, includePath)))
    |> Seq.toList

let private appSourceFiles () =
    appProjects
    |> List.collect (projectItems "Compile")

let rec private projectClosure pending visited =
    match pending with
    | [] -> visited
    | projectPath :: rest when Set.contains projectPath visited ->
        projectClosure rest visited
    | projectPath :: rest ->
        let references = projectItems "ProjectReference" projectPath
        projectClosure (references @ rest) (Set.add projectPath visited)

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
let ``App compile items include every declared source file`` () =
    appSourceFiles ()
    |> List.iter (fun path -> Assert.True(File.Exists path, path))

let private expectedProjectClosure =
    [ "src/Client/Gambol.Client.fsproj"
      "src/Desktop/Gambol.Desktop.fsproj"
      "src/Shared/Gambol.Shared.fsproj"
      "src/Shared/documents/Gambol.Shared.Documents.fsproj"
      "src/Shared/dotnet/Gambol.Shared.DotNet.fsproj" ]
    |> List.map repositoryPath
    |> Set.ofList

[<Fact>]
let ``App project dependency closure stays inside approved projects`` () =
    let actual = projectClosure appProjects Set.empty
    Assert.Equal<string>(
        expectedProjectClosure |> Set.toList |> List.sort,
        actual |> Set.toList |> List.sort)

[<Fact>]
let ``App source does not host or invoke GitHub transport`` () =
    assertSourceOmits
        [ "GithubTransportActor"
          "CoreActorPool"
          "registerPeer"
          "GitRun"
          "WorkspaceGit" ]

let private recordFieldPattern =
    Regex(
        @"(?m)^\s*(?:{\s*)?([A-Za-z][A-Za-z0-9_']*)\s*:\s*",
        RegexOptions.Compiled)

let private appRecordFieldNames () =
    appSourceFiles ()
    |> List.collect (fun path ->
        recordFieldPattern.Matches(File.ReadAllText path)
        |> Seq.map (fun item -> item.Groups.[1].Value)
        |> Seq.toList)

[<Fact>]
let ``App source contains no GitHub remote branch or credential state`` () =
    let forbiddenStateName (name: string) =
        let lower = name.ToLowerInvariant()
        lower.Contains("github")
        || lower.Contains("remote")
        || lower.Contains("branch")
    Assert.DoesNotContain(appRecordFieldNames (), forbiddenStateName)
    assertSourceOmits
        [ "github"
          "credential.helper"
          "git remote"
          "git pull"
          "git push" ]
