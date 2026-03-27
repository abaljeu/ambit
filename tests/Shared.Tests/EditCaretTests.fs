module Gambol.Tests.EditCaretTests

open Gambol.Shared
open Xunit

[<Fact>]
let ``utf16ClampedToLength clamps high cursor to text length`` () =
    Assert.Equal(EditCaret.Utf16Index 5, EditCaret.utf16ClampedToLength 99 5)

[<Fact>]
let ``utf16ClampedToLength clamps negative to zero`` () =
    Assert.Equal(EditCaret.Utf16Index 0, EditCaret.utf16ClampedToLength -3 10)

[<Fact>]
let ``utf16ClampedToLength preserves in-range index`` () =
    Assert.Equal(EditCaret.Utf16Index 3, EditCaret.utf16ClampedToLength 3 10)
