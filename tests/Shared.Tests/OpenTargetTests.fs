module OpenTargetTests

open Gambol.Shared
open Xunit

[<Fact>]
let ``tryFindOpenableUri bare https`` () =
    let got = OpenTarget.tryFindOpenableUri "https://example.com/path"
    Assert.Equal(Some "https://example.com/path", got)

[<Fact>]
let ``tryFindOpenableUri embedded in sentence`` () =
    let got = OpenTarget.tryFindOpenableUri "see http://a.org/x for info"
    Assert.Equal(Some "http://a.org/x", got)

[<Fact>]
let ``tryFindOpenableUri www becomes https`` () =
    let got = OpenTarget.tryFindOpenableUri "at www.foo.bar/baz end"
    Assert.Equal(Some "https://www.foo.bar/baz", got)

[<Fact>]
let ``tryFindOpenableUri scheme-less host becomes https`` () =
    let got = OpenTarget.tryFindOpenableUri "see example.com/path for details"
    Assert.Equal(Some "https://example.com/path", got)

[<Fact>]
let ``tryFindOpenableUri explicit scheme wins over later bare host`` () =
    let got =
        OpenTarget.tryFindOpenableUri "use https://a.com/ not b.org please"

    Assert.Equal(Some "https://a.com/", got)

[<Fact>]
let ``tryFindOpenableUri mailto`` () =
    let got = OpenTarget.tryFindOpenableUri "mail me mailto:user@example.com thanks"
    Assert.True(got.IsSome)
    Assert.Contains("mailto:user@example.com", got.Value)

[<Fact>]
let ``tryFindOpenableUri rejects javascript`` () =
    Assert.Equal(None, OpenTarget.tryFindOpenableUri "javascript:alert(1)")

[<Fact>]
let ``tryFindOpenableUri trims trailing punctuation`` () =
    Assert.Equal(Some "https://a.com/x", OpenTarget.tryFindOpenableUri "(https://a.com/x).")
    Assert.Equal(Some "https://b.com/", OpenTarget.tryFindOpenableUri "https://b.com)")

[<Fact>]
let ``tryFindOpenableUri empty`` () =
    Assert.Equal(None, OpenTarget.tryFindOpenableUri "")
    Assert.Equal(None, OpenTarget.tryFindOpenableUri "   ")

[<Fact>]
let ``tryFindOpenableUri first url wins`` () =
    let got =
        OpenTarget.tryFindOpenableUri "a https://first.com x https://second.com y"

    Assert.Equal(Some "https://first.com/", got)

[<Fact>]
let ``tryFindOpenableUriWithFirstChildFallback uses child when parent empty of link`` () =
    let got =
        OpenTarget.tryFindOpenableUriWithFirstChildFallback "no link here" (Some "https://kid.com")

    Assert.Equal(Some "https://kid.com/", got)

[<Fact>]
let ``tryFindOpenableUriWithFirstChildFallback parent wins`` () =
    let got =
        OpenTarget.tryFindOpenableUriWithFirstChildFallback "https://parent.com" (Some "https://child.com")

    Assert.Equal(Some "https://parent.com/", got)

[<Fact>]
let ``tryFindOpenableUriWithFirstChildFallback no child skips second lookup`` () =
    let got = OpenTarget.tryFindOpenableUriWithFirstChildFallback "plain" None
    Assert.Equal(None, got)
