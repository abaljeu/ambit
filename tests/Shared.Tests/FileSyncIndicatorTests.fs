module FileSyncIndicatorTests

open System
open Gambol.Shared
open Xunit

let private utc (y: int) (mo: int) (d: int) (h: int) (mi: int) (s: int) =
    DateTime(y, mo, d, h, mi, s, DateTimeKind.Utc)

[<Fact>]
let ``labelForExistingFile returns current when times match at db precision`` () =
    let t = utc 2024 6 1 12 0 0
    Assert.Equal("current", FileSyncIndicator.labelForExistingFile t t)

[<Fact>]
let ``labelForExistingFile returns old when node is older than source`` () =
    let node = utc 2024 6 1 10 0 0
    let source = utc 2024 6 1 12 0 0
    Assert.Equal("old", FileSyncIndicator.labelForExistingFile node source)

[<Fact>]
let ``labelForExistingFile returns edited when node is newer than source`` () =
    let node = utc 2024 6 1 14 0 0
    let source = utc 2024 6 1 12 0 0
    Assert.Equal("edited", FileSyncIndicator.labelForExistingFile node source)

[<Fact>]
let ``labelForExistingFile treats missing node time as old`` () =
    let source = utc 2024 6 1 12 0 0
    Assert.Equal("old", FileSyncIndicator.labelForExistingFile NodeUpdateTime.missing source)

[<Fact>]
let ``indicatorTextForStatus maps non-file statuses`` () =
    let t = utc 2024 6 1 12 0 0

    Assert.Equal("create", FileSyncIndicator.indicatorTextForStatus t CreateFile None)
    Assert.Equal("missing", FileSyncIndicator.indicatorTextForStatus t MissingArtifact None)
    Assert.Equal("invalid", FileSyncIndicator.indicatorTextForStatus t InvalidPath None)
    Assert.Equal("folder", FileSyncIndicator.indicatorTextForStatus t ExistingFolder None)
