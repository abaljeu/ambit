module Gambol.Shared.Tests.FocusXmlStreamTests

open Xunit
open Gambol.Shared

let private newId () = NodeId.New()

let private pushApply draft chunk =
    let tokens, draft = FocusXmlStream.push chunk draft
    FocusXmlStream.apply newId tokens draft

[<Fact>]
let ``partial tag stays off-graph until greater-than`` () =
    let focusId = NodeId.New()
    let draft = FocusXmlStream.start focusId
    let step = pushApply draft "<di"
    Assert.Empty(step.adds)
    Assert.Equal("<di", step.draft.hold)
    let step = pushApply step.draft "v>Hi</div>"
    Assert.Equal(1, step.adds.Length)
    Assert.Equal("Hi", step.adds.Head.text)
    Assert.Equal(focusId, step.adds.Head.parentId)

[<Fact>]
let ``text deltas append to pending and commit once at End`` () =
    let focusId = NodeId.New()
    let draft = FocusXmlStream.start focusId
    let step = pushApply draft "<n>Te"
    Assert.Empty(step.adds)
    match step.draft.pending with
    | None -> Assert.Fail("pending missing")
    | Some pending ->
        Assert.Equal("n", pending.name)
        Assert.Equal("Te", pending.text)
    let step = pushApply step.draft "xt</n>"
    Assert.Equal(1, step.adds.Length)
    Assert.Equal("Text", step.adds.Head.text)
    Assert.Equal(focusId, step.adds.Head.parentId)
    Assert.True(step.draft.pending.IsNone)

[<Fact>]
let ``Start of child commits parent with one addChild`` () =
    let focusId = NodeId.New()
    let draft = FocusXmlStream.start focusId
    let step =
        pushApply draft "<p>hello<c>world</c></p>"
    Assert.Equal(2, step.adds.Length)
    let parentAdd = step.adds.Head
    let childAdd = step.adds.[1]
    Assert.Equal(focusId, parentAdd.parentId)
    Assert.Equal("hello", parentAdd.text)
    Assert.Equal(parentAdd.childId, childAdd.parentId)
    Assert.Equal("world", childAdd.text)

[<Fact>]
let ``Empty element is one addChild with empty text`` () =
    let focusId = NodeId.New()
    let draft = FocusXmlStream.start focusId
    let step = pushApply draft "<leaf/>"
    Assert.Equal(1, step.adds.Length)
    Assert.Equal("", step.adds.Head.text)
    Assert.Equal(focusId, step.adds.Head.parentId)

[<Fact>]
let ``fragment wrappers are not nodes`` () =
    let focusId = NodeId.New()
    let draft = FocusXmlStream.start focusId
    let step =
        pushApply draft "<><n>one</n><n>two</n></>"
    Assert.Equal(2, step.adds.Length)
    Assert.Equal("one", step.adds.Head.text)
    Assert.Equal("two", step.adds.[1].text)
    Assert.Equal(focusId, step.adds.Head.parentId)
    Assert.Equal(focusId, step.adds.[1].parentId)

[<Fact>]
let ``leading angle of next tag commits pending text`` () =
    let focusId = NodeId.New()
    let draft = FocusXmlStream.start focusId
    let step = pushApply draft "<tag>Text<"
    Assert.Equal(1, step.adds.Length)
    Assert.Equal("Text", step.adds.Head.text)
    Assert.Equal(focusId, step.adds.Head.parentId)
    Assert.True(step.draft.pending.IsNone)
    Assert.Equal("<", step.draft.hold)

[<Fact>]
let ``incomplete next tag after commit stays in hold`` () =
    let focusId = NodeId.New()
    let draft = FocusXmlStream.start focusId
    let step = pushApply draft "<tag>Text<"
    Assert.Equal(1, step.adds.Length)
    let step = pushApply step.draft "di"
    Assert.Empty(step.adds)
    Assert.Equal("<di", step.draft.hold)
    Assert.True(step.draft.pending.IsNone)

[<Fact>]
let ``more text before next open stays pending`` () =
    let focusId = NodeId.New()
    let draft = FocusXmlStream.start focusId
    let step = pushApply draft "<n>Te"
    Assert.Empty(step.adds)
    let step = pushApply step.draft "xt"
    Assert.Empty(step.adds)
    match step.draft.pending with
    | None -> Assert.Fail("pending missing")
    | Some pending -> Assert.Equal("Text", pending.text)
    let step = pushApply step.draft "<"
    Assert.Equal(1, step.adds.Length)
    Assert.Equal("Text", step.adds.Head.text)
    Assert.True(step.draft.pending.IsNone)
    Assert.Equal("<", step.draft.hold)

[<Fact>]
let ``End after open-commit does not add again`` () =
    let focusId = NodeId.New()
    let draft = FocusXmlStream.start focusId
    let step = pushApply draft "<tag>Text<"
    Assert.Equal(1, step.adds.Length)
    let childId = step.adds.Head.childId
    let step = pushApply step.draft "/tag>"
    Assert.Empty(step.adds)
    Assert.True(step.draft.pending.IsNone)
    Assert.Equal("", step.draft.hold)
    Assert.Equal(focusId, step.draft.parents.Head.id)
    Assert.NotEqual(childId, step.draft.parents.Head.id)

[<Fact>]
let ``flush commits pending without a later text edit`` () =
    let focusId = NodeId.New()
    let draft = FocusXmlStream.start focusId
    let step = pushApply draft "<n>partial"
    Assert.Empty(step.adds)
    let flushed = FocusXmlStream.flush newId step.draft
    Assert.Equal(1, flushed.adds.Length)
    Assert.Equal("partial", flushed.adds.Head.text)
    Assert.True(flushed.draft.pending.IsNone)
