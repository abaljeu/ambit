module Gambol.Server.Tests.PersistRequestStackTests

open System.Diagnostics
open System.Threading
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared

let private request () : PersistRequest =
    { nodeId = NodeId.New() }

[<Fact>]
let ``pushBoundNode pushes a bound node and skips a missing stack`` () =
    let missing = ref None
    PersistRequestStack.pushBoundNode missing (NodeId.New())
    Assert.True(Option.isNone missing.Value)
    let holder = ref (Some(PersistRequestStack.create ()))
    let nodeId = NodeId.New()
    PersistRequestStack.pushBoundNode holder nodeId
    let got = PersistRequestStack.take holder.Value.Value
    Assert.Equal(nodeId, got.nodeId)

[<Fact>]
let ``persist stack takes the newest request`` () =
    let stack = PersistRequestStack.create ()
    let first = request ()
    let second = request ()
    PersistRequestStack.push stack first
    PersistRequestStack.push stack second
    let got = PersistRequestStack.take stack
    Assert.Equal(second.nodeId, got.nodeId)

[<Fact>]
let ``persist stack take waits for a push`` () =
    let stack = PersistRequestStack.create ()
    let pending = request ()
    let clock = Stopwatch.StartNew()
    let pusher =
        Task.Run(fun () ->
            Thread.Sleep(200)
            PersistRequestStack.push stack pending)
    let got = PersistRequestStack.take stack
    clock.Stop()
    pusher.Wait()
    Assert.True(clock.ElapsedMilliseconds >= 150L)
    Assert.Equal(pending.nodeId, got.nodeId)
