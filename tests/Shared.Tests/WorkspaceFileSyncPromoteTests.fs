module WorkspaceFileSyncPromoteTests

open System
open System.IO
open Gambol.Shared
open Xunit

let private newTempDir (prefix: string) =
    let dir =
        Path.Combine(
            Path.GetTempPath(),
            $"{prefix}-{Guid.NewGuid()}")
    Directory.CreateDirectory(dir) |> ignore
    dir

let private bodyFile (rel: string) : WorkspaceSyncLimits.PlannedPath =
    { relative = rel
      isDirectory = false
      file = Some(WorkspaceSyncLimits.FilePlan.Body 1L) }

let private dirPath (rel: string) : WorkspaceSyncLimits.PlannedPath =
    { relative = rel
      isDirectory = true
      file = None }

let private stageNestedFile (stage: string) (rel: string) (text: string) =
    let full = Path.Combine(Array.append [| stage |] (rel.Split('/')))
    let parent = Path.GetDirectoryName full
    if not (String.IsNullOrEmpty parent) then
        Directory.CreateDirectory parent |> ignore
    File.WriteAllText(full, text)

[<Fact>]
let ``promotePlanned moves nested file without planned parent dir`` () =
    let mapped = newTempDir "gambol-promote-mapped"
    let stage = newTempDir "gambol-promote-stage"
    stageNestedFile stage "docs/a.txt" "from-server"
    let planned = [ bodyFile "docs/a.txt" ]

    match WorkspaceFileSync.promotePlanned mapped stage planned with
    | Error e -> Assert.Fail(e)
    | Ok () ->
        let dest = Path.Combine(mapped, "docs", "a.txt")
        Assert.True(File.Exists dest)
        Assert.Equal("from-server", File.ReadAllText dest)
        Assert.False(File.Exists(Path.Combine(stage, "docs", "a.txt")))

[<Fact>]
let ``promotePlanned still moves root file`` () =
    let mapped = newTempDir "gambol-promote-mapped"
    let stage = newTempDir "gambol-promote-stage"
    stageNestedFile stage "readme.md" "hi"
    let planned = [ bodyFile "readme.md" ]

    match WorkspaceFileSync.promotePlanned mapped stage planned with
    | Error e -> Assert.Fail(e)
    | Ok () ->
        let dest = Path.Combine(mapped, "readme.md")
        Assert.True(File.Exists dest)
        Assert.Equal("hi", File.ReadAllText dest)

[<Fact>]
let ``promotePlanned merges planned directory then skips already-moved files`` () =
    let mapped = newTempDir "gambol-promote-mapped"
    let stage = newTempDir "gambol-promote-stage"
    Directory.CreateDirectory(Path.Combine(mapped, "docs")) |> ignore
    stageNestedFile stage "docs/a.txt" "merged"
    let planned =
        [ dirPath "docs"
          bodyFile "docs/a.txt" ]

    match WorkspaceFileSync.promotePlanned mapped stage planned with
    | Error e -> Assert.Fail(e)
    | Ok () ->
        let dest = Path.Combine(mapped, "docs", "a.txt")
        Assert.True(File.Exists dest)
        Assert.Equal("merged", File.ReadAllText dest)
