module Gambol.Shared.Tests.DocumentBinaryTests

open Xunit
open Gambol.Shared

[<Fact>]
let ``isBinaryExtension detects image and archive paths`` () =
    Assert.True(DocumentBinary.isBinaryExtension "photos/cat.JPG")
    Assert.True(DocumentBinary.isBinaryExtension "a/b/photo.jpeg")
    Assert.True(DocumentBinary.isBinaryExtension "x.png")
    Assert.True(DocumentBinary.isBinaryExtension "doc.PDF")
    Assert.True(DocumentBinary.isBinaryExtension "lib/foo.dll")
    Assert.False(DocumentBinary.isBinaryExtension "readme.txt")
    Assert.False(DocumentBinary.isBinaryExtension "notes.md")
    Assert.False(DocumentBinary.isBinaryExtension "outline.amb")

[<Fact>]
let ``looksLikeBinaryContent detects NUL in probe window`` () =
    Assert.False(DocumentBinary.looksLikeBinaryContent "hello\nworld")
    Assert.True(DocumentBinary.looksLikeBinaryContent ("abc" + string '\000' + "def"))
    Assert.False(DocumentBinary.looksLikeBinaryContent "")

[<Fact>]
let ``refuseParse rejects binary extension before content`` () =
    match DocumentBinary.refuseParse "cat.jpg" "not really image" with
    | Error err -> Assert.Equal(DocumentBinary.parseError, err)
    | Ok () -> failwith "expected refuse"

[<Fact>]
let ``refuseParse rejects NUL content on unknown extension`` () =
    let text = "hdr" + string '\000' + "tail"

    match DocumentBinary.refuseParse "mystery.bin" text with
    | Error err -> Assert.Equal(DocumentBinary.parseError, err)
    | Ok () -> failwith "expected refuse"

[<Fact>]
let ``refuseParse allows plain text`` () =
    match DocumentBinary.refuseParse "readme.txt" "line one\nline two" with
    | Ok () -> ()
    | Error err -> failwith err

[<Fact>]
let ``classifyCodec rejects binary extensions`` () =
    match DocumentFormat.classifyCodec "photos/cat.jpg" with
    | Error err -> Assert.Equal(DocumentBinary.parseError, err)
    | Ok _ -> failwith "expected classifyCodec error"
