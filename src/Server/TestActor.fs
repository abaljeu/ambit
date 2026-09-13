namespace Gambol.Server

open System
open Gambol.Shared

[<RequireQualifiedAccess>]
module TestActor =

    let private postHello
        (focus: NodeId)
        (graph: Graph)
        (core: CoreChanges)
        : Async<unit> =
        async {
            match Map.tryFind focus graph.nodes with
            | None -> return ()
            | Some parent ->
                let childId = NodeId.New()
                let! rev = core.getRevision ()
                let helloChild = [ ChildNode.owner childId ]
                let change =
                    { id = rev.Value
                      changeId = Guid.NewGuid()
                      ops =
                        [ Op.NewNode(childId, "hello")
                          Op.Replace(
                              focus,
                              parent.children,
                              parent.children @ helloChild) ] }
                let! _ = core.postChange [ change ]
                return ()
        }

    let run (case: string) : ActorFn =
        fun focus graph _cred core ->
            match case with
            | "hello" -> postHello focus graph core
            | _ -> async.Return ()
