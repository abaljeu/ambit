module WorkspaceSyncLedgerTests

open System
open System.IO
open Gambol.Shared
open Xunit

let private utc y m d h =
    DateTime(y, m, d, h, 0, 0, DateTimeKind.Utc)

let private row rel isDir localM serverM presence lastOp =
    { relative = rel
      isDirectory = isDir
      localMtimeUtc = localM
      serverMtimeUtc = serverM
      lastServerHead = None
      presence = presence
      lastOp = lastOp }

[<Fact>]
let ``shouldSkipUpload skips when server is newer`` () =
    let local = utc 2026 1 2 12
    let server = utc 2026 1 3 12
    Assert.True(WorkspaceSyncLedger.shouldSkipUpload local (Some server))

[<Fact>]
let ``shouldSkipUpload skips when server equals local`` () =
    let t = utc 2026 1 2 12
    Assert.True(WorkspaceSyncLedger.shouldSkipUpload t (Some t))

[<Fact>]
let ``shouldSkipUpload transfers when server is older`` () =
    let local = utc 2026 1 3 12
    let server = utc 2026 1 2 12
    Assert.False(WorkspaceSyncLedger.shouldSkipUpload local (Some server))

[<Fact>]
let ``shouldSkipUpload transfers when server mtime unknown`` () =
    Assert.False(WorkspaceSyncLedger.shouldSkipUpload (utc 2026 1 1 0) None)

[<Fact>]
let ``shouldSkipDownload skips when local is newer`` () =
    let server = utc 2026 1 2 12
    let local = utc 2026 1 3 12
    Assert.True(WorkspaceSyncLedger.shouldSkipDownload server (Some local))

[<Fact>]
let ``shouldSkipDownload skips when local equals server`` () =
    let t = utc 2026 1 2 12
    Assert.True(WorkspaceSyncLedger.shouldSkipDownload t (Some t))

[<Fact>]
let ``shouldSkipDownload transfers when local is older`` () =
    let server = utc 2026 1 3 12
    let local = utc 2026 1 2 12
    Assert.False(WorkspaceSyncLedger.shouldSkipDownload server (Some local))

[<Fact>]
let ``shouldSkipDownload transfers when local file missing`` () =
    Assert.False(WorkspaceSyncLedger.shouldSkipDownload (utc 2026 1 1 0) None)

[<Fact>]
let ``directory scope skips upload when server is newer`` () =
    let local = utc 2026 1 2 12
    let server = utc 2026 1 3 12
    Assert.True(
        WorkspaceSyncLedger.shouldSkipUploadScoped
            SyncScopeKind.Directory
            local
            (Some server))

[<Fact>]
let ``workspace scope skips download when local equals server`` () =
    let t = utc 2026 1 2 12
    Assert.True(
        WorkspaceSyncLedger.shouldSkipDownloadScoped
            SyncScopeKind.Workspace
            t
            (Some t))

[<Fact>]
let ``file scope never skips upload for mtime`` () =
    let local = utc 2026 1 2 12
    let server = utc 2026 1 3 12
    Assert.False(
        WorkspaceSyncLedger.shouldSkipUploadScoped
            SyncScopeKind.File
            local
            (Some server))

[<Fact>]
let ``file scope never skips download for mtime`` () =
    let server = utc 2026 1 2 12
    let local = utc 2026 1 3 12
    Assert.False(
        WorkspaceSyncLedger.shouldSkipDownloadScoped
            SyncScopeKind.File
            server
            (Some local))

[<Fact>]
let ``transferDatestampsMatch requires identical client server node`` () =
    let t = utc 2026 4 1 9
    Assert.True(WorkspaceSyncLedger.transferDatestampsMatch t t t)
    Assert.False(
        WorkspaceSyncLedger.transferDatestampsMatch
            t
            t
            (utc 2026 4 1 10))

[<Fact>]
let ``recordUpload aligns ledger local and server mtimes`` () =
    let t = utc 2026 3 1 10
    let ledger =
        { label = "home"
          rows = [ row "f.txt" false None None "localOnly" None ] }
    let next =
        WorkspaceSyncLedger.recordUpload ledger "f.txt" false t t None
    let r = next.rows |> List.find (fun x -> x.relative = "f.txt")
    Assert.True(WorkspaceSyncLedger.ledgerRowDatestampsAligned r)
    Assert.True(
        WorkspaceSyncLedger.transferDatestampsMatch
            r.localMtimeUtc.Value
            r.serverMtimeUtc.Value
            t)

[<Fact>]
let ``recordDownload aligns ledger local and server mtimes`` () =
    let t = utc 2026 5 1 8
    let ledger =
        { label = "home"
          rows = [ row "f.txt" false None None "serverOnly" None ] }
    let next = WorkspaceSyncLedger.recordDownload ledger "f.txt" t t
    let r = next.rows |> List.find (fun x -> x.relative = "f.txt")
    Assert.True(WorkspaceSyncLedger.ledgerRowDatestampsAligned r)

[<Fact>]
let ``encode round-trips through decode`` () =
    let original =
        { label = "home"
          rows =
            [ row "a.txt" false (Some(utc 2026 1 1 0)) (Some(utc 2026 1 1 1)) "both" (Some "seed") ] }
    let decoded =
        match WorkspaceSyncLedger.decode (WorkspaceSyncLedger.encode original) with
        | Ok l -> l
        | Error e -> failwith e
    Assert.Equal("home", decoded.label)
    Assert.Equal(1, decoded.rows.Length)
    Assert.Equal("a.txt", decoded.rows.[0].relative)

[<Fact>]
let ``loadFromFile missing file returns empty ledger`` () =
    let path =
        Path.Combine(Path.GetTempPath(), $"gambol-ledger-missing-{Guid.NewGuid()}.json")
    match WorkspaceSyncLedger.loadFromFile path "home" with
    | Error e -> Assert.Fail(e)
    | Ok ledger ->
        Assert.Equal("home", ledger.label)
        Assert.Empty(ledger.rows)

[<Fact>]
let ``save and load round-trip`` () =
    let path =
        Path.Combine(Path.GetTempPath(), $"gambol-ledger-{Guid.NewGuid()}.json")
    let original =
        { label = "docs"
          rows =
            [ row "x/y.txt" false (Some(utc 2026 2 1 8)) None "localOnly" (Some "seed") ] }
    match WorkspaceSyncLedger.saveToFile path original with
    | Error e -> Assert.Fail(e)
    | Ok () ->
        match WorkspaceSyncLedger.loadFromFile path "docs" with
        | Error e -> Assert.Fail(e)
        | Ok loaded ->
            Assert.Equal(1, loaded.rows.Length)
            Assert.Equal("localOnly", loaded.rows.[0].presence)

[<Fact>]
let ``seed merges server and local inventory`` () =
    let server =
        [ { relative = "a.txt"
            isCollection = false
            lastModifiedUtc = Some(utc 2026 1 1 0)
            contentLength = 3L }
          { relative = "only-server"
            isCollection = false
            lastModifiedUtc = Some(utc 2026 1 2 0)
            contentLength = 1L } ]
    let local =
        [ { relative = "a.txt"; isDirectory = false }
          { relative = "only-local"; isDirectory = false } ]
    let mapped = Path.GetTempPath()
    let ledger = WorkspaceSyncLedger.seed "home" mapped server local
    Assert.Equal(3, ledger.rows.Length)
    let byRel = ledger.rows |> List.map (fun r -> r.relative) |> Set.ofList
    Assert.True(Set.contains "a.txt" byRel)
    Assert.True(Set.contains "only-server" byRel)
    Assert.True(Set.contains "only-local" byRel)
    let a =
        ledger.rows |> List.find (fun r -> r.relative = "a.txt")
    Assert.Equal("both", a.presence)
    Assert.Equal(Some "seed", a.lastOp)

[<Fact>]
let ``needsSeed is true for empty rows`` () =
    Assert.True(
        WorkspaceSyncLedger.needsSeed
            { label = "home"; rows = [] })

[<Fact>]
let ``needsSeed is false when rows exist`` () =
    Assert.False(
        WorkspaceSyncLedger.needsSeed
            { label = "home"
              rows = [ row "a.txt" false None None "both" None ] })

[<Fact>]
let ``recordUpload updates row mtimes and head`` () =
    let t = utc 2026 3 1 10
    let ledger =
        { label = "home"
          rows = [ row "f.txt" false None None "localOnly" None ] }
    let next =
        WorkspaceSyncLedger.recordUpload ledger "f.txt" false t t (Some "abc")
    let row = next.rows |> List.find (fun r -> r.relative = "f.txt")
    Assert.Equal(Some t, row.localMtimeUtc)
    Assert.Equal(Some t, row.serverMtimeUtc)
    Assert.Equal(Some "abc", row.lastServerHead)
    Assert.Equal("both", row.presence)
    Assert.Equal(Some "upload", row.lastOp)

[<Fact>]
let ``liveStatusRows includes workspace root and file mtimes without writing ledger`` () =
    let root =
        Path.Combine(
            Path.GetTempPath(),
            $"gambol-live-status-{Guid.NewGuid()}")
    Directory.CreateDirectory(root) |> ignore
    let filePath = Path.Combine(root, "note.md")
    File.WriteAllText(filePath, "hi")
    let fileMtime = File.GetLastWriteTimeUtc filePath
    let ledger =
        { label = "home"
          rows =
            [ row "gone.txt" false None (Some(utc 2026 1 1 0)) "serverOnly" None ] }
    let rows = WorkspaceSyncLedger.liveStatusRows root ledger
    let byRel = rows |> List.map (fun r -> r.relative, r) |> Map.ofList
    Assert.True(Map.containsKey "" byRel)
    Assert.Equal("both", byRel.[""].presence)
    Assert.True(byRel.[""].localMtimeUtc.IsSome)
    Assert.True(Map.containsKey "note.md" byRel)
    Assert.Equal("both", byRel.["note.md"].presence)
    Assert.Equal(Some fileMtime, byRel.["note.md"].localMtimeUtc)
    Assert.True(Map.containsKey "gone.txt" byRel)
    Assert.Equal("serverOnly", byRel.["gone.txt"].presence)
    try Directory.Delete(root, true) with _ -> ()
