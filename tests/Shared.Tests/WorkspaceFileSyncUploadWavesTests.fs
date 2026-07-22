module WorkspaceFileSyncUploadWavesTests

open Gambol.Shared
open Xunit

let private bodyFile (rel: string) : WorkspaceSyncLimits.PlannedPath =
    { relative = rel
      isDirectory = false
      file = Some(WorkspaceSyncLimits.FilePlan.Body 1L) }

let private dirPath (rel: string) : WorkspaceSyncLimits.PlannedPath =
    { relative = rel
      isDirectory = true
      file = None }

let private waveRels (wave: WorkspaceSyncLimits.PlannedPath list) =
    wave |> List.map (fun p -> p.relative)

[<Fact>]
let ``partitionUploadWaves groups dirs by depth then files`` () =
    let planned =
        [ bodyFile "a/x.txt"
          dirPath "b"
          dirPath "a/nested"
          bodyFile "b/y.txt"
          dirPath "a"
          bodyFile "z.txt" ]

    let waves = WorkspaceFileSync.partitionUploadWaves planned

    Assert.Equal(3, waves.Length)
    Assert.Equal<string list>([ "a"; "b" ], waveRels waves.[0])
    Assert.Equal<string list>([ "a/nested" ], waveRels waves.[1])
    Assert.Equal<string list>(
        [ "a/x.txt"; "b/y.txt"; "z.txt" ],
        waveRels waves.[2])

[<Fact>]
let ``partitionUploadWaves empty plan is empty`` () =
    Assert.Empty(WorkspaceFileSync.partitionUploadWaves [])

[<Fact>]
let ``partitionUploadWaves files only is one wave`` () =
    let planned = [ bodyFile "b.txt"; bodyFile "a.txt" ]
    let waves = WorkspaceFileSync.partitionUploadWaves planned

    Assert.Equal(1, waves.Length)
    Assert.Equal<string list>([ "a.txt"; "b.txt" ], waveRels waves.[0])

[<Fact>]
let ``partitionUploadWaves dirs only has no file wave`` () =
    let planned = [ dirPath "a/b"; dirPath "a" ]
    let waves = WorkspaceFileSync.partitionUploadWaves planned

    Assert.Equal(2, waves.Length)
    Assert.Equal<string list>([ "a" ], waveRels waves.[0])
    Assert.Equal<string list>([ "a/b" ], waveRels waves.[1])
