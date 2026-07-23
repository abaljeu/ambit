module WorkspacePathSyncStatusTests

open System
open Gambol.Shared
open Xunit

let private utc y m d h =
    DateTime(y, m, d, h, 0, 0, DateTimeKind.Utc)

let private fact presence localM serverM =
    { relative = "a.txt"
      isDirectory = false
      presence = presence
      localMtimeUtc = localM
      serverMtimeUtc = serverM }

[<Fact>]
let ``presence strings round-trip ledger literals`` () =
    Assert.Equal(
        Some WorkspacePathPresence.Both,
        WorkspacePathPresence.ofLedgerString "both")
    Assert.Equal(
        Some WorkspacePathPresence.LocalOnly,
        WorkspacePathPresence.ofLedgerString "localOnly")
    Assert.Equal(
        Some WorkspacePathPresence.ServerOnly,
        WorkspacePathPresence.ofLedgerString "serverOnly")
    Assert.Equal(None, WorkspacePathPresence.ofLedgerString "other")

[<Fact>]
let ``classifyComparison covers presence and mtime cases`` () =
    Assert.Equal(
        WorkspacePathSyncStatus.OnlyOnDesktop,
        WorkspacePathSyncStatus.classifyComparison
            WorkspacePathPresence.LocalOnly None None)
    Assert.Equal(
        WorkspacePathSyncStatus.OnlyOnServer,
        WorkspacePathSyncStatus.classifyComparison
            WorkspacePathPresence.ServerOnly None None)
    Assert.Equal(
        WorkspacePathSyncStatus.Synced,
        WorkspacePathSyncStatus.classifyComparison
            WorkspacePathPresence.Both
            (Some(utc 2026 1 1 0))
            (Some(utc 2026 1 1 0)))
    Assert.Equal(
        WorkspacePathSyncStatus.NewerOnDesktop,
        WorkspacePathSyncStatus.classifyComparison
            WorkspacePathPresence.Both
            (Some(utc 2026 1 2 0))
            (Some(utc 2026 1 1 0)))
    Assert.Equal(
        WorkspacePathSyncStatus.NewerOnServer,
        WorkspacePathSyncStatus.classifyComparison
            WorkspacePathPresence.Both
            (Some(utc 2026 1 1 0))
            (Some(utc 2026 1 2 0)))
    Assert.Equal(
        WorkspacePathSyncStatus.Synced,
        WorkspacePathSyncStatus.classifyComparison
            WorkspacePathPresence.Both None None)

[<Fact>]
let ``withUnparsed only overlays Synced`` () =
    Assert.Equal(
        WorkspacePathSyncStatus.Unparsed,
        WorkspacePathSyncStatus.withUnparsed
            true WorkspacePathSyncStatus.Synced)
    Assert.Equal(
        WorkspacePathSyncStatus.NewerOnDesktop,
        WorkspacePathSyncStatus.withUnparsed
            true WorkspacePathSyncStatus.NewerOnDesktop)

[<Fact>]
let ``resolve without compare allows Unparsed only`` () =
    let f =
        fact WorkspacePathPresence.LocalOnly None None
    Assert.Equal(
        Some WorkspacePathSyncStatus.Unparsed,
        WorkspacePathSyncStatus.resolve false (Some f) true)
    Assert.Equal(
        None,
        WorkspacePathSyncStatus.resolve false (Some f) false)

[<Fact>]
let ``resolve with compare uses ledger and Unparsed overlay`` () =
    let synced =
        fact
            WorkspacePathPresence.Both
            (Some(utc 2026 1 1 0))
            (Some(utc 2026 1 1 0))
    Assert.Equal(
        Some WorkspacePathSyncStatus.Unparsed,
        WorkspacePathSyncStatus.resolve true (Some synced) true)
    Assert.Equal(
        Some WorkspacePathSyncStatus.Synced,
        WorkspacePathSyncStatus.resolve true (Some synced) false)
    Assert.Equal(
        Some WorkspacePathSyncStatus.OnlyOnDesktop,
        WorkspacePathSyncStatus.resolve
            true
            (Some(fact WorkspacePathPresence.LocalOnly None None))
            false)
    Assert.Equal(
        Some WorkspacePathSyncStatus.Unparsed,
        WorkspacePathSyncStatus.resolve true None true)
    Assert.Equal(
        None,
        WorkspacePathSyncStatus.resolve true None false)

[<Fact>]
let ``effectiveServerMtime prefers node stamp over ledger`` () =
    let nodeStamp = utc 2026 1 3 0
    let ledger = Some(utc 2026 1 1 0)
    Assert.Equal(
        Some(NodeUpdateTime.toDbPrecision nodeStamp),
        WorkspacePathSyncStatus.effectiveServerMtime
            nodeStamp
            ledger)
    Assert.Equal(
        ledger,
        WorkspacePathSyncStatus.effectiveServerMtime
            NodeUpdateTime.missing
            ledger)
    Assert.Equal(
        None,
        WorkspacePathSyncStatus.effectiveServerMtime
            NodeUpdateTime.missing
            None)

[<Fact>]
let ``resolveWithNodeStamp ignores node stamp when Unparsed`` () =
    let local = utc 2026 1 1 0
    let synced =
        fact
            WorkspacePathPresence.Both
            (Some local)
            (Some local)
    let nodeNewer = utc 2026 1 3 0
    Assert.Equal(
        Some WorkspacePathSyncStatus.Unparsed,
        WorkspacePathSyncStatus.resolveWithNodeStamp
            true (Some synced) nodeNewer true)
    Assert.Equal(
        Some WorkspacePathSyncStatus.NewerOnServer,
        WorkspacePathSyncStatus.resolveWithNodeStamp
            true (Some synced) nodeNewer false)

[<Fact>]
let ``glyph and shortLabel cover every sync status`` () =
    Assert.Equal("synced", WorkspacePathSyncStatus.shortLabel WorkspacePathSyncStatus.Synced)
    Assert.Equal("\u2713", WorkspacePathSyncStatus.glyph WorkspacePathSyncStatus.Synced)
    Assert.Equal("srv only", WorkspacePathSyncStatus.shortLabel WorkspacePathSyncStatus.OnlyOnServer)
    Assert.Equal("\u2601", WorkspacePathSyncStatus.glyph WorkspacePathSyncStatus.OnlyOnServer)
    Assert.Equal("srv new", WorkspacePathSyncStatus.shortLabel WorkspacePathSyncStatus.NewerOnServer)
    Assert.Equal("\u2193", WorkspacePathSyncStatus.glyph WorkspacePathSyncStatus.NewerOnServer)
    Assert.Equal("desk only", WorkspacePathSyncStatus.shortLabel WorkspacePathSyncStatus.OnlyOnDesktop)
    Assert.Equal("\u25A2", WorkspacePathSyncStatus.glyph WorkspacePathSyncStatus.OnlyOnDesktop)
    Assert.Equal("desk new", WorkspacePathSyncStatus.shortLabel WorkspacePathSyncStatus.NewerOnDesktop)
    Assert.Equal("\u2191", WorkspacePathSyncStatus.glyph WorkspacePathSyncStatus.NewerOnDesktop)
    Assert.Equal("unparsed", WorkspacePathSyncStatus.shortLabel WorkspacePathSyncStatus.Unparsed)
    Assert.Equal("\u2026", WorkspacePathSyncStatus.glyph WorkspacePathSyncStatus.Unparsed)
