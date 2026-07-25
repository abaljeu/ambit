module WorkspaceSyncLimitsTests

open Gambol.Shared
open Xunit

let private file rel bytes : WorkspaceSyncLimits.SizedItem =
    { relative = rel
      isDirectory = false
      byteSize = bytes }

let private dir rel : WorkspaceSyncLimits.SizedItem =
    { relative = rel
      isDirectory = true
      byteSize = 0L }

let private miB (n: int64) = n * 1024L * 1024L

[<Fact>]
let ``bulk upload includes full structure when eligible files fit caps`` () =
    let items =
        [ dir "docs"
          file "docs/a.txt" (miB 1L)
          file "docs/oversized.bin" (miB 8L)
          file "root.txt" 10L ]
    let mode, planned =
        WorkspaceSyncLimits.planUpload SyncScopeKind.Workspace "" items
    Assert.Equal(WorkspaceSyncLimits.Mode.Full, mode)
    Assert.Equal(4, planned.Length)
    let bodyPaths =
        WorkspaceSyncLimits.bodyTransfers planned
        |> List.map (fun p -> p.relative)
        |> Set.ofList
    Assert.Equal<Set<string>>(set [ "root.txt"; "docs/a.txt" ], bodyPaths)

[<Fact>]
let ``bulk upload eligible cap excludes oversized files`` () =
    let oversized =
        [ 1..1600 ]
        |> List.map (fun i -> file $"large{i}.bin" (miB 2L))
    let eligible =
        [ 1..1500 ]
        |> List.map (fun i -> file $"small{i}.txt" 1L)
    let mode, planned =
        WorkspaceSyncLimits.planUpload
            SyncScopeKind.Workspace
            ""
            (oversized @ eligible)
    Assert.Equal(WorkspaceSyncLimits.Mode.Full, mode)
    Assert.Equal(3100, planned.Length)
    Assert.Equal(1500, WorkspaceSyncLimits.bodyTransfers planned |> List.length)

[<Fact>]
let ``bulk upload falls back to eligible immediate files over either cap`` () =
    let top =
        [ 1..1501 ]
        |> List.map (fun i -> file $"top{i}.txt" 1L)
    let items =
        dir "docs"
        :: file "docs/nested.txt" 1L
        :: file "huge.bin" (miB 2L)
        :: top
    let mode, planned =
        WorkspaceSyncLimits.planUpload SyncScopeKind.Workspace "" items
    Assert.Equal(WorkspaceSyncLimits.Mode.TopLevel, mode)
    let rels = planned |> List.map (fun p -> p.relative) |> Set.ofList
    Assert.True(Set.contains "docs" rels)
    Assert.True(Set.contains "top1501.txt" rels)
    Assert.False(Set.contains "docs/nested.txt" rels)
    Assert.False(
        WorkspaceSyncLimits.bodyTransfers planned
        |> List.exists (fun p -> p.relative = "huge.bin"))
    Assert.Equal(1501, WorkspaceSyncLimits.bodyTransfers planned |> List.length)

[<Fact>]
let ``bulk upload byte cap boundary is inclusive`` () =
    let atCap =
        [ 1..16 ]
        |> List.map (fun i -> file $"f{i}.bin" (miB 1L))
    let overCap = file "extra.txt" 1L :: atCap
    let modeAt, _ =
        WorkspaceSyncLimits.planUpload SyncScopeKind.Directory "d" atCap
    let modeOver, _ =
        WorkspaceSyncLimits.planUpload SyncScopeKind.Directory "d" overCap
    Assert.Equal(WorkspaceSyncLimits.Mode.Full, modeAt)
    Assert.Equal(WorkspaceSyncLimits.Mode.TopLevel, modeOver)

[<Fact>]
let ``direct file upload keeps four MiB limit`` () =
    let mode, planned =
        WorkspaceSyncLimits.planUpload
            SyncScopeKind.File
            "a.bin"
            [ file "a.bin" (miB 4L) ]
    Assert.Equal(WorkspaceSyncLimits.Mode.Full, mode)
    Assert.Single(WorkspaceSyncLimits.bodyTransfers planned) |> ignore

    let _, oversized =
        WorkspaceSyncLimits.planUpload
            SyncScopeKind.File
            "a.bin"
            [ file "a.bin" (miB 4L + 1L) ]
    Assert.Empty(WorkspaceSyncLimits.bodyTransfers oversized)

[<Fact>]
let ``download plans file over four MiB as a body`` () =
    let mode, planned =
        WorkspaceSyncLimits.planDownload [ file "large.bin" (miB 8L) ]
    Assert.Equal(WorkspaceSyncLimits.Mode.Full, mode)
    Assert.Equal(1, planned.Length)
    Assert.Equal(
        Some(WorkspaceSyncLimits.FilePlan.Body(miB 8L)),
        planned.Head.file)

[<Fact>]
let ``download plans every path over fifteen hundred without truncation`` () =
    let items =
        dir "nested"
        :: ([ 1..1501 ]
            |> List.map (fun i -> file $"nested/f{i}.txt" 1L))
    let mode, planned = WorkspaceSyncLimits.planDownload items
    Assert.Equal(WorkspaceSyncLimits.Mode.Full, mode)
    Assert.Equal(items.Length, planned.Length)
    Assert.Equal(1501, WorkspaceSyncLimits.bodyTransfers planned |> List.length)

[<Fact>]
let ``download plans every body when inventory exceeds sixteen MiB`` () =
    let items =
        [ file "a.bin" (miB 8L)
          file "b.bin" (miB 8L)
          file "c.bin" (miB 8L) ]
    let mode, planned = WorkspaceSyncLimits.planDownload items
    Assert.Equal(WorkspaceSyncLimits.Mode.Full, mode)
    Assert.Equal(3, WorkspaceSyncLimits.bodyTransfers planned |> List.length)
    Assert.DoesNotContain(
        planned,
        fun path ->
            path.file = Some WorkspaceSyncLimits.FilePlan.StubOnly)

[<Fact>]
let ``nameCount counts every file and directory name`` () =
    let items =
        [ dir "a"
          file "a/x.txt" 10L
          dir "b"
          file "c.txt" 1L ]
    Assert.Equal(4, WorkspaceSyncLimits.nameCount items)

[<Fact>]
let ``transferByteSum includes only bodies Full would send`` () =
    let items =
        [ dir "d"
          file "ok.txt" 100L
          file "big.bin" (miB 5L)
          file "empty.dat" 0L ]
    Assert.Equal(100L, WorkspaceSyncLimits.transferByteSum items)

[<Fact>]
let ``transferByteSum excludes TreeStructure-scale soft overflow bodies over 16 MiB`` () =
    // Five ≤4 MiB files whose bodies sum past the soft transfer cap.
    let items =
        [ file "a.bin" (miB 4L)
          file "b.bin" (miB 4L)
          file "c.bin" (miB 4L)
          file "d.bin" (miB 4L)
          file "e.bin" (miB 4L) ]
    Assert.Equal(miB 20L, WorkspaceSyncLimits.transferByteSum items)

[<Fact>]
let ``classify returns Full under 200 names and 16 MiB transfer`` () =
    let items =
        [ dir "docs"
          file "docs/a.txt" 50L
          file "readme.md" 10L ]
    Assert.Equal(WorkspaceSyncLimits.Mode.Full, WorkspaceSyncLimits.classify items)

[<Fact>]
let ``classify returns TreeStructure when name count exceeds 200`` () =
    let items =
        [ 1..201 ]
        |> List.map (fun i -> file $"f{i}.txt" 1L)
    Assert.Equal(
        WorkspaceSyncLimits.Mode.TreeStructure,
        WorkspaceSyncLimits.classify items)

[<Fact>]
let ``classify returns TreeStructure when transfer bytes exceed 16 MiB`` () =
    let items =
        [ file "a.bin" (miB 4L)
          file "b.bin" (miB 4L)
          file "c.bin" (miB 4L)
          file "d.bin" (miB 4L)
          file "e.bin" (miB 4L) ]
    Assert.Equal(
        WorkspaceSyncLimits.Mode.TreeStructure,
        WorkspaceSyncLimits.classify items)

[<Fact>]
let ``classify returns TopLevel when structure paths exceed 1500`` () =
    let items =
        [ 1..1501 ]
        |> List.map (fun i -> file $"f{i}.txt" 1L)
    Assert.Equal(
        WorkspaceSyncLimits.Mode.TopLevel,
        WorkspaceSyncLimits.classify items)

[<Fact>]
let ``classify at exactly 1500 names stays TreeStructure when over soft name cap`` () =
    let items =
        [ 1..1500 ]
        |> List.map (fun i -> file $"f{i}.txt" 1L)
    Assert.Equal(
        WorkspaceSyncLimits.Mode.TreeStructure,
        WorkspaceSyncLimits.classify items)

[<Fact>]
let ``Full plan keeps sibling files and marks oversized as stub-only`` () =
    let items =
        [ dir "d"
          file "d/small.txt" 10L
          file "d/big.bin" (miB 5L)
          file "d/other.txt" 20L ]
    let mode, planned = WorkspaceSyncLimits.plan "" items
    Assert.Equal(WorkspaceSyncLimits.Mode.Full, mode)
    let byRel =
        planned
        |> List.map (fun p -> p.relative, p)
        |> Map.ofList
    match byRel.["d/small.txt"].file with
    | Some(WorkspaceSyncLimits.FilePlan.Body 10L) -> ()
    | other -> Assert.Fail($"expected Body 10, got {other}")
    match byRel.["d/big.bin"].file with
    | Some WorkspaceSyncLimits.FilePlan.StubOnly -> ()
    | other -> Assert.Fail($"expected StubOnly, got {other}")
    match byRel.["d/other.txt"].file with
    | Some(WorkspaceSyncLimits.FilePlan.Body 20L) -> ()
    | other -> Assert.Fail($"expected Body 20, got {other}")
    Assert.True(byRel.["d"].isDirectory)
    let bodies = WorkspaceSyncLimits.bodyTransfers planned
    Assert.Equal(2, List.length bodies)
    Assert.False(
        bodies |> List.exists (fun p -> p.relative = "d/big.bin"))

[<Fact>]
let ``TreeStructure plan is stub-only with no body transfers`` () =
    let items =
        [ 1..201 ]
        |> List.map (fun i -> file $"f{i}.txt" 50L)
    let mode, planned = WorkspaceSyncLimits.plan "" items
    Assert.Equal(WorkspaceSyncLimits.Mode.TreeStructure, mode)
    Assert.All(
        planned,
        fun p ->
            Assert.False(p.isDirectory)
            Assert.Equal(
                Some WorkspaceSyncLimits.FilePlan.StubOnly,
                p.file))
    Assert.Empty(WorkspaceSyncLimits.bodyTransfers planned)

[<Fact>]
let ``TopLevel plan only immediate children under scope`` () =
    let nested =
        [ dir "docs"
          file "docs/a.txt" 1L
          file "docs/b.txt" 1L
          dir "docs/sub"
          file "docs/sub/deep.txt" 1L
          file "root.txt" 1L ]
    let pad =
        [ 1..1500 ]
        |> List.map (fun i -> file $"pad{i}.txt" 1L)
    let items = nested @ pad
    let mode, planned = WorkspaceSyncLimits.plan "" items
    Assert.Equal(WorkspaceSyncLimits.Mode.TopLevel, mode)
    let rels = planned |> List.map (fun p -> p.relative) |> Set.ofList
    Assert.True(Set.contains "docs" rels)
    Assert.True(Set.contains "root.txt" rels)
    Assert.True(Set.contains "pad1.txt" rels)
    Assert.False(Set.contains "docs/a.txt" rels)
    Assert.False(Set.contains "docs/sub" rels)
    Assert.False(Set.contains "docs/sub/deep.txt" rels)

[<Fact>]
let ``selectForVolume TopLevel matches plan path set`` () =
    let nested =
        [ dir "docs"
          file "docs/a.txt" 1L
          dir "docs/sub"
          file "root.txt" 1L ]
    let pad =
        [ 1..1500 ]
        |> List.map (fun i -> file $"pad{i}.txt" 1L)
    let items = nested @ pad
    let mode, selected = WorkspaceSyncLimits.selectForVolume "" items
    Assert.Equal(WorkspaceSyncLimits.Mode.TopLevel, mode)
    let selectedRels =
        selected |> List.map (fun i -> i.relative) |> Set.ofList
    let _, planned = WorkspaceSyncLimits.plan "" items
    let plannedRels =
        planned |> List.map (fun p -> p.relative) |> Set.ofList
    Assert.True((plannedRels = selectedRels))

[<Fact>]
let ``selectForVolume TreeStructure keeps full path set for stubs`` () =
    let pad =
        [ 1..250 ]
        |> List.map (fun i -> file $"pad{i}.txt" 1L)
    let items = dir "docs" :: file "docs/a.txt" 1L :: pad
    let mode, selected = WorkspaceSyncLimits.selectForVolume "" items
    Assert.Equal(WorkspaceSyncLimits.Mode.TreeStructure, mode)
    let rels =
        selected |> List.map (fun i -> i.relative) |> Set.ofList
    Assert.True(Set.contains "docs" rels)
    Assert.True(Set.contains "docs/a.txt" rels)

[<Fact>]
let ``TopLevel top-level file over 4 MiB is stub-only`` () =
    let pad =
        [ 1..1500 ]
        |> List.map (fun i -> file $"pad{i}.txt" 1L)
    let items = file "huge.bin" (miB 5L) :: pad
    let mode, planned = WorkspaceSyncLimits.plan "" items
    Assert.Equal(WorkspaceSyncLimits.Mode.TopLevel, mode)
    let huge = planned |> List.find (fun p -> p.relative = "huge.bin")
    Assert.Equal(
        Some WorkspaceSyncLimits.FilePlan.StubOnly,
        huge.file)
    Assert.False(
        WorkspaceSyncLimits.bodyTransfers planned
        |> List.exists (fun p -> p.relative = "huge.bin"))

[<Fact>]
let ``plan is directory-complete for Full sibling sets`` () =
    let items =
        [ dir "d"
          file "d/a.txt" 1L
          file "d/b.txt" 2L
          file "d/c.txt" 3L ]
    let _, planned = WorkspaceSyncLimits.plan "" items
    Assert.True(
        WorkspaceSyncLimits.isEveryDirectoryComplete items planned)
    let byParent = WorkspaceSyncLimits.filesByParent planned
    Assert.Equal<Set<string>>(
        set [ "d/a.txt"; "d/b.txt"; "d/c.txt" ],
        byParent.["d"])

[<Fact>]
let ``empty directory is a complete shell with no file children`` () =
    let items =
        [ dir "empty"
          file "x.txt" 1L ]
    let _, planned = WorkspaceSyncLimits.plan "" items
    Assert.True(
        WorkspaceSyncLimits.isEveryDirectoryComplete items planned)
    let empties = WorkspaceSyncLimits.emptyDirectories planned |> Set.ofList
    Assert.True(Set.contains "empty" empties)
    Assert.False(Map.containsKey "empty" (WorkspaceSyncLimits.filesByParent planned))

[<Fact>]
let ``partial sibling set is not directory-complete`` () =
    let intended =
        [ dir "d"
          file "d/a.txt" 1L
          file "d/b.txt" 2L ]
    let partialPlan : WorkspaceSyncLimits.PlannedPath list =
        [ { relative = "d"; isDirectory = true; file = None }
          { relative = "d/a.txt"
            isDirectory = false
            file = Some(WorkspaceSyncLimits.FilePlan.Body 1L) } ]
    Assert.False(
        WorkspaceSyncLimits.isEveryDirectoryComplete intended partialPlan)
