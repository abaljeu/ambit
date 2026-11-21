module FileTextTests

open System.Text
open Gambol.Shared
open Xunit

let private sample (bytes: byte[]) (truncated: bool) =
    FileText.sampleFromBytes bytes truncated

let private utf8 (text: string) =
    sample (Encoding.UTF8.GetBytes text) false

[<Fact>]
let ``empty sample is text`` () =
    Assert.Equal(FileText.Kind.Text, FileText.classifySample (sample [||] false))

[<Fact>]
let ``ascii source is text`` () =
    let got = FileText.classifySample (utf8 "let x = 1\n")
    Assert.Equal(FileText.Kind.Text, got)

[<Fact>]
let ``utf-8 accent and emoji are text`` () =
    let got = FileText.classifySample (utf8 "café 😀")
    Assert.Equal(FileText.Kind.Text, got)

[<Fact>]
let ``utf-8 bom is text`` () =
    let bytes = Array.append [| 0xEFuy; 0xBBuy; 0xBFuy |] (Encoding.UTF8.GetBytes "hi")
    Assert.Equal(FileText.Kind.Text, FileText.classifySample (sample bytes false))

[<Fact>]
let ``nul byte is binary`` () =
    let bytes = Encoding.UTF8.GetBytes "ab"
    let withNul = Array.append bytes [| 0uy |]
    Assert.Equal(FileText.Kind.Binary, FileText.classifySample (sample withNul false))

[<Fact>]
let ``invalid utf-8 is binary`` () =
    Assert.Equal(FileText.Kind.Binary, FileText.classifySample (sample [| 0xFFuy |] false))
    Assert.Equal(
        FileText.Kind.Binary,
        FileText.classifySample (sample [| 0xC0uy; 0x80uy |] false))
    Assert.Equal(
        FileText.Kind.Binary,
        FileText.classifySample (sample [| 0xEDuy; 0xA0uy; 0x80uy |] false))
    Assert.Equal(
        FileText.Kind.Binary,
        FileText.classifySample (sample [| 0xF0uy; 0x8Fuy; 0x80uy; 0x80uy |] false))

[<Fact>]
let ``truncated multibyte tail is text`` () =
    let lead = [| byte 'c'; 0xC3uy |]
    Assert.Equal(FileText.Kind.Text, FileText.classifySample (sample lead true))

[<Fact>]
let ``incomplete multibyte at end of file is binary`` () =
    let lead = [| byte 'c'; 0xC3uy |]
    Assert.Equal(FileText.Kind.Binary, FileText.classifySample (sample lead false))
