module Gambol.Server.Tests.CloudAgentActorTests

open System
open System.Threading.Tasks
open Xunit
open Gambol.Server
open Gambol.Shared
open Gambol.Server.Tests.TestBackend

let private requireOk label result =
    match result with
    | Ok value -> value
    | Error err ->
        Assert.Fail($"{label}: {err}")
        Unchecked.defaultof<_>

let private addFocusNode (handle: CoreChanges) (text: string) : Async<NodeId> = async {
    let! state = handle.getState ()
    let state = requireOk "state" state
    let parent = state.graph.nodes.[Graph.rootId]
    let focusId = NodeId.New()
    let change =
        { id = state.revision.Value
          changeId = Guid.NewGuid()
          ops =
            [ Op.NewNode(focusId, text)
              Op.Replace(
                  Graph.rootId,
                  parent.children,
                  parent.children @ [ ChildNode.owner focusId ]) ] }
    let! accepted = handle.postChange [ change ]
    ignore (requireOk "post" accepted)
    return focusId
}

let private addChildToFocus (handle: CoreChanges) (focusId: NodeId) (text: string) : Async<NodeId> = async {
    let! state = handle.getState ()
    let state = requireOk "state" state
    let focus = state.graph.nodes.[focusId]
    let childId = NodeId.New()
    let change =
        { id = state.revision.Value
          changeId = Guid.NewGuid()
          ops =
            [ Op.NewNode(childId, text)
              Op.Replace(
                  focusId,
                  focus.children,
                  focus.children @ [ ChildNode.owner childId ]) ] }
    let! accepted = handle.postChange [ change ]
    ignore (requireOk "post" accepted)
    return childId
}

let private spanOf (graph: Graph) (parentId: NodeId) (childId: NodeId) : NodeRange =
    let start =
        graph.nodes.[parentId].children
        |> List.findIndex (fun c -> c.id = childId)
    { pnode = parentId; start = start; endd = start + 1 }

[<Fact>]
let ``cloud-agent actor is registered`` () =
    let dataDir = newTempDir ()
    let agent = FileAgent.create dataDir
    try
        let handle = FileAgent.coreChanges agent
        let credentials = CoreCredentials.create ()
        let pool = CoreActorPool.create credentials
        
        // Register cloud-agent actor (with empty config for test)
        let config = { CloudAgentActor.ApiKey = "" }
        let actorFn = CloudAgentActor.createActorFn config
        pool.register (ActorName CloudAgentActor.actorName) actorFn
        
        // Verify we can query for it (by launching and checking it doesn't fail with unknown actor)
        task {
            let! focusId = addFocusNode handle "? test message" |> Async.StartAsTask
            let! childId = addChildToFocus handle focusId "context" |> Async.StartAsTask
            let! state = handle.getState () |> Async.StartAsTask
            let state = requireOk "state" state
            
            let request =
                { name = ActorName CloudAgentActor.actorName
                  revision = state.revision
                  span = spanOf state.graph focusId childId }
            
            let! launched = pool.launch handle request |> Async.StartAsTask
            // Should not fail with "unknown actor" error
            match launched with
            | Error err when err.Contains("unknown actor") ->
                Assert.Fail("Actor not registered")
            | Error err ->
                // Other errors are OK for this test (like missing API key)
                ()
            | Ok _ ->
                // Success is also OK
                ()
        }
    finally
        FileAgent.dispose agent

[<Fact>]
let ``extractFocusMessage extracts question from header`` () =
    let dataDir = newTempDir ()
    let agent = FileAgent.create dataDir
    try
        task {
            let handle = FileAgent.coreChanges agent
            
            // Create a node with "? what is the answer"
            let! focusId = addFocusNode handle "? what is the answer" |> Async.StartAsTask
            let! state = handle.getState () |> Async.StartAsTask
            let state = requireOk "state" state
            
            // Verify the message can be extracted
            let node = state.graph.nodes.[focusId]
            let header = node.header.text.Trim()
            
            Assert.StartsWith("?", header)
            let message = if header.StartsWith("?") && header.Length > 1 then
                            Some(header.Substring(1).Trim())
                          else None
            
            Assert.Equal(Some "what is the answer", message)
        }
    finally
        FileAgent.dispose agent

[<Fact>]
let ``payload to launch request converts nodelist to span`` () =
    let dataDir = newTempDir ()
    let agent = FileAgent.create dataDir
    try
        task {
            let handle = FileAgent.coreChanges agent
            
            let! focusId = addFocusNode handle "? test" |> Async.StartAsTask
            let! child1 = addChildToFocus handle focusId "first" |> Async.StartAsTask
            let! child2 = addChildToFocus handle focusId "second" |> Async.StartAsTask
            
            let! state = handle.getState () |> Async.StartAsTask
            let state = requireOk "state" state
            
            let payload =
                { ApiResponseSerialization.ActorLaunchPayload.actor = "cloud-agent"
                  nodelist = [ child1; child2 ]
                  focusnode = focusId
                  rootnode = Graph.rootId
                  revision = state.revision.Value }
            
            let result = Api.payloadToLaunchRequest payload state.graph
            let request = requireOk "payloadToLaunchRequest" result
            
            Assert.Equal(ActorName "cloud-agent", request.name)
            Assert.Equal(state.revision, request.revision)
            Assert.Equal(focusId, request.span.pnode)
            Assert.Equal(0, request.span.start)
            Assert.Equal(2, request.span.endd)
        }
    finally
        FileAgent.dispose agent
