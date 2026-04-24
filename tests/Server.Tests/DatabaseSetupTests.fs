module Gambol.Server.Tests.DatabaseSetupTests

open System
open Xunit
open Gambol.Server
open Gambol.Shared

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

[<Fact>]
let ``documentStatesMatch treats two outline reads as same when revision matches`` () =
    let outline = "\tSame text each load\n"
    let g1 = Snapshot.read outline
    let g2 = Snapshot.read outline
    Assert.False(GraphProjection.graphEquals g1 g2)

    let st1 =
        { graph = g1
          history = History.empty
          revision = Revision 0 }

    let st2 =
        { graph = g2
          history = History.empty
          revision = Revision 0 }

    Assert.True(DatabaseSetup.documentStatesMatch st1 st2)

[<Fact>]
let ``resolveConnectionString prefers TEST_DB_CONNECTION_STRING`` () =
    let priorTest = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING")
    let priorMain = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")

    try
        Environment.SetEnvironmentVariable("TEST_DB_CONNECTION_STRING", "test-conn")
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", "main-conn")

        let result = DatabaseSetup.resolveConnectionString ()
        Assert.Equal("test-conn", result)
    finally
        Environment.SetEnvironmentVariable("TEST_DB_CONNECTION_STRING", priorTest)
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", priorMain)

[<Fact>]
let ``resolveConnectionString falls back to DB_CONNECTION_STRING`` () =
    let priorTest = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING")
    let priorMain = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")

    try
        Environment.SetEnvironmentVariable("TEST_DB_CONNECTION_STRING", null)
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", "main-conn")

        let result = DatabaseSetup.resolveConnectionString ()
        Assert.Equal("main-conn", result)
    finally
        Environment.SetEnvironmentVariable("TEST_DB_CONNECTION_STRING", priorTest)
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", priorMain)

[<Fact>]
let ``resolveConnectionString returns empty when neither env var is set`` () =
    let priorTest = Environment.GetEnvironmentVariable("TEST_DB_CONNECTION_STRING")
    let priorMain = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")

    try
        Environment.SetEnvironmentVariable("TEST_DB_CONNECTION_STRING", null)
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", null)

        let result = DatabaseSetup.resolveConnectionString ()
        Assert.Equal("", result)
    finally
        Environment.SetEnvironmentVariable("TEST_DB_CONNECTION_STRING", priorTest)
        Environment.SetEnvironmentVariable("DB_CONNECTION_STRING", priorMain)
