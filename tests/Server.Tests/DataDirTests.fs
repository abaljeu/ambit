module DataDirTests

open System.IO
open Gambol.Server
open Xunit

[<Fact>]
let ``normalize ensures trailing directory separator`` () =
    let withSep = Path.Combine("c:", "data") + string Path.DirectorySeparatorChar
    let withoutSep = Path.Combine("c:", "data")
    Assert.Equal(withSep, DataDir.normalize withoutSep)
    Assert.Equal(withSep, DataDir.normalize withSep)

[<Fact>]
let ``resolvePath azure default is home AppData`` () =
    let home = Path.Combine(Path.GetTempPath(), "gambol-azure-home")
    let expected = Path.Combine(home, "AppData") |> Path.GetFullPath
    Assert.Equal(expected, DataDir.resolvePath "/site/wwwroot" None true home)

[<Fact>]
let ``resolvePath azure honors configured relative under home`` () =
    let home = Path.Combine(Path.GetTempPath(), "gambol-azure-custom")
    let expected = Path.Combine(home, "my-data") |> Path.GetFullPath
    Assert.Equal(
        expected,
        DataDir.resolvePath "/site/wwwroot" (Some "my-data") true home)

[<Fact>]
let ``resolvePath azure honors configured absolute path`` () =
    let absolute = Path.Combine(Path.GetTempPath(), "gambol-absolute-data") |> Path.GetFullPath
    Assert.Equal(
        absolute,
        DataDir.resolvePath "/site/wwwroot" (Some absolute) true "/home")

[<Fact>]
let ``resolvePath local default is relative to content root`` () =
    let contentRoot = Path.Combine(Path.GetTempPath(), "gambol-content") |> Path.GetFullPath
    let expected = Path.Combine(contentRoot, "..", "..", "data") |> Path.GetFullPath
    Assert.Equal(expected, DataDir.resolvePath contentRoot None false "/home")

[<Fact>]
let ``normalize resolves relative path to full path with trailing separator`` () =
    let relative = Path.Combine("..", "..", "data")
    let expected =
        Path.GetFullPath relative
        |> fun full ->
            if full.EndsWith(string Path.DirectorySeparatorChar) then full
            else full + string Path.DirectorySeparatorChar
    Assert.Equal(expected, DataDir.normalize relative)
