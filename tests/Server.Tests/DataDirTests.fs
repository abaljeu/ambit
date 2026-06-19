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
let ``normalize resolves relative path to full path with trailing separator`` () =
    let relative = Path.Combine("..", "..", "data")
    let expected =
        Path.GetFullPath relative
        |> fun full ->
            if full.EndsWith(string Path.DirectorySeparatorChar) then full
            else full + string Path.DirectorySeparatorChar
    Assert.Equal(expected, DataDir.normalize relative)
