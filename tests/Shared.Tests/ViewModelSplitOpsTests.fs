module ViewModelSplitOpsTests

open Gambol.Shared.ViewModelSplitOps
open Xunit

[<Fact>]
let ``splitContinueEditText at offset 0 keeps the current node text`` () =
    Assert.Equal("keep me", splitContinueEditText 0 "keep me" "")

[<Fact>]
let ``splitContinueEditText in the middle uses the new node text`` () =
    Assert.Equal("lo", splitContinueEditText 3 "hello" "lo")

[<Fact>]
let ``splitContinueEditText at the end uses the new node text`` () =
    Assert.Equal("", splitContinueEditText 5 "hello" "")
