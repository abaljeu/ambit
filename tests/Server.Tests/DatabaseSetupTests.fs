module Gambol.Server.Tests.DatabaseSetupTests

open Xunit
open Gambol.Server

[<Fact>]
let ``statusFromMatches returns Ok when initial state matches`` () =
    let status = DatabaseSetup.statusFromMatches true false
    Assert.Equal(DatabaseSetup.DbStatus.Ok, status)

[<Fact>]
let ``statusFromMatches returns Mismatch1 when rebuild fixes mismatch`` () =
    let status = DatabaseSetup.statusFromMatches false true
    Assert.Equal(DatabaseSetup.DbStatus.Mismatch1, status)

[<Fact>]
let ``statusFromMatches returns Mismatch2 when rebuild does not fix mismatch`` () =
    let status = DatabaseSetup.statusFromMatches false false
    Assert.Equal(DatabaseSetup.DbStatus.Mismatch2, status)
