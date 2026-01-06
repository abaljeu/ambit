module HistoryTests

open Gambol.Shared
open Xunit

[<Fact>]
let ``CreateState12 has empty history`` () =
    let state = ModelBuilder.createState12 ()
    Assert.Empty(state.history.past)
    Assert.Empty(state.history.future)

[<Fact>]
let ``NewChange uses next id and has no ops`` () =
    let history = History.empty
    let change: Change = History.newChange history
    Assert.Equal(0, change.id)
    Assert.Empty(change.ops)

[<Fact>]
let ``AddOp appends to change`` () =
    let history = History.empty
    let change0: Change = History.newChange history
    let op1 = Op.SetText(NodeId.New(), "", "x")
    let op2 = Op.SetText(NodeId.New(), "", "y")
    let change1 = Change.addOp op1 change0
    let change2 = Change.addOp op2 change1
    Assert.Equal<Op>([ op1; op2 ], change2.ops)

[<Fact>]
let ``AddChange pushes to past and clears future`` () =
    let history0 =
        { History.empty with
            future = [ { id = 99; ops = [] } ] }

    let change: Change = History.newChange history0
    let history1 = History.addChange change history0
    Assert.Equal(1, history1.past.Length)
    Assert.Empty(history1.future)
