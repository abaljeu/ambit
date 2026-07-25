module Gambol.Shared.Tests.SystemDirectoryPersistTests

open System
open Gambol.Shared
open Xunit

[<Fact>]
let ``allows SYSTEM amb marker`` () =
    Assert.Equal(Ok (), SystemDirectoryPersist.refuseWrite "SYSTEM/.amb")

[<Fact>]
let ``allows SYSTEM amb marker case insensitive`` () =
    Assert.Equal(Ok (), SystemDirectoryPersist.refuseWrite "system/.AMB")

[<Fact>]
let ``allows allowlisted user css`` () =
    Assert.Equal(Ok (), SystemDirectoryPersist.refuseWrite "SYSTEM/user.css")

[<Fact>]
let ``allows allowlisted user css case insensitive`` () =
    Assert.Equal(Ok (), SystemDirectoryPersist.refuseWrite "System/User.CSS")

[<Fact>]
let ``refuses other direct SYSTEM child`` () =
    match SystemDirectoryPersist.refuseWrite "SYSTEM/other.txt" with
    | Ok () -> failwith "expected refuse"
    | Error msg ->
        Assert.Contains(
            "system directory write refused",
            msg,
            StringComparison.OrdinalIgnoreCase)

[<Fact>]
let ``refuses nested SYSTEM path`` () =
    match SystemDirectoryPersist.refuseWrite "SYSTEM/foo/bar" with
    | Ok () -> failwith "expected refuse"
    | Error msg ->
        Assert.Contains(
            "system directory write refused",
            msg,
            StringComparison.OrdinalIgnoreCase)

[<Fact>]
let ``refuses SYSTEM directory itself`` () =
    match SystemDirectoryPersist.refuseWrite "SYSTEM/" with
    | Ok () -> failwith "expected refuse"
    | Error msg ->
        Assert.Contains(
            "system directory write refused",
            msg,
            StringComparison.OrdinalIgnoreCase)

[<Fact>]
let ``allows paths outside SYSTEM`` () =
    Assert.Equal(Ok (), SystemDirectoryPersist.refuseWrite "home/readme.txt")
    Assert.Equal(Ok (), SystemDirectoryPersist.refuseWrite ".amb")

[<Fact>]
let ``writeAllowlist starts with user css`` () =
    Assert.Equal<string list>(
        [ "user.css" ],
        SystemDirectoryPersist.writeAllowlist)
