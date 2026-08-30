module WorkspaceFileSyncUploadWavesTests

open Gambol.Shared
open Xunit

let private sizedBodyFile (rel: string) (bytes: int64) : WorkspaceSyncLimits.PlannedPath =
    { relative = rel
      isDirectory = false
      file = Some(WorkspaceSyncLimits.FilePlan.Body bytes) }

let private bodyFile rel = sizedBodyFile rel 1L

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
let ``partitionUploadWaves orders file bodies smallest-first then path`` () =
    let planned =
        [ sizedBodyFile "large.txt" 100L
          sizedBodyFile "z-small.txt" 1L
          sizedBodyFile "a-small.txt" 1L ]
    let waves = WorkspaceFileSync.partitionUploadWaves planned
    Assert.Equal<string list>(
        [ "a-small.txt"; "z-small.txt"; "large.txt" ],
        waveRels waves.[0])

[<Fact>]
let ``partitionUploadWaves dirs only has no file wave`` () =
    let planned = [ dirPath "a/b"; dirPath "a" ]
    let waves = WorkspaceFileSync.partitionUploadWaves planned

    Assert.Equal(2, waves.Length)
    Assert.Equal<string list>([ "a" ], waveRels waves.[0])
    Assert.Equal<string list>([ "a/b" ], waveRels waves.[1])

[<Fact>]
let ``partitionUploadBatchResults keeps successes when one fails`` () =
    let plannedA = bodyFile "a.txt"
    let plannedB = bodyFile "b.txt"
    let plannedC = bodyFile "c.txt"
    let uploaded, errors =
        WorkspaceFileSync.partitionUploadBatchResults
            [ Ok plannedA
              Error "b.txt: direct upload HTTP 500"
              Ok plannedC ]

    Assert.Equal<string list>([ "a.txt"; "c.txt" ], waveRels uploaded)
    Assert.Equal<string list>(
        [ "b.txt: direct upload HTTP 500" ],
        errors)

[<Fact>]
let ``partitionUploadBatchResults all ok has empty errors`` () =
    let uploaded, errors =
        WorkspaceFileSync.partitionUploadBatchResults
            [ Ok(bodyFile "x.txt") ]

    Assert.Equal(1, uploaded.Length)
    Assert.Empty(errors)

[<Fact>]
let ``accumulateUploadWaveBatches keeps later batches after a failed batch`` () =
    let batch1 =
        WorkspaceFileSync.partitionUploadBatchResults
            [ Ok(bodyFile "a.txt"); Ok(bodyFile "b.txt") ]
    let batch2 =
        WorkspaceFileSync.partitionUploadBatchResults
            [ Error "c.txt: direct upload HTTP 500" ]
    let batch3 =
        WorkspaceFileSync.partitionUploadBatchResults
            [ Ok(bodyFile "d.txt"); Error "e.txt: timeout" ]
    let uploaded, errors =
        WorkspaceFileSync.accumulateUploadWaveBatches
            [ batch1; batch2; batch3 ]

    Assert.Equal<string list>(
        [ "a.txt"; "b.txt"; "d.txt" ],
        waveRels uploaded)
    Assert.Equal<string list>(
        [ "c.txt: direct upload HTTP 500"; "e.txt: timeout" ],
        errors)

[<Fact>]
let ``accumulateUploadWaveBatches all-fail still folds every batch`` () =
    let batches =
        [ WorkspaceFileSync.partitionUploadBatchResults
              [ Error "a.txt: 500" ]
          WorkspaceFileSync.partitionUploadBatchResults
              [ Error "b.txt: 500"; Error "c.txt: 500" ] ]
    let uploaded, errors =
        WorkspaceFileSync.accumulateUploadWaveBatches batches

    Assert.Empty(uploaded)
    Assert.Equal(3, errors.Length)
    Assert.True(WorkspaceFileSync.uploadWaveAllFailed uploaded errors)

[<Fact>]
let ``uploadWaveAllFailed is false when any upload succeeded`` () =
    Assert.False(
        WorkspaceFileSync.uploadWaveAllFailed
            [ bodyFile "a.txt" ]
            [ "b.txt: failed" ])

[<Fact>]
let ``uploadWaveAllFailed is false when there were no errors`` () =
    Assert.False(WorkspaceFileSync.uploadWaveAllFailed [] [])
