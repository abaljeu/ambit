namespace Gambol.Server

open System.Threading
open Gambol.Shared

/// LIFO node ids for the persist thread. Core pushes; one consumer waits.
/// Not a MailboxProcessor.
[<RequireQualifiedAccess>]
module PersistStack =

    /// Closes private cells; returns push and the blocking consumer.
    let create () : (NodeId -> unit) * (unit -> NodeId) =
        let gate = obj ()
        let items: NodeId list ref = ref []
        let push (nodeId: NodeId) =
            lock gate (fun () ->
                items.Value <- nodeId :: items.Value
                Monitor.Pulse gate)
        let rec waitForHead () =
            match items.Value with
            | [] ->
                Monitor.Wait gate |> ignore
                waitForHead ()
            | head :: rest ->
                items.Value <- rest
                head
        let consumer () =
            lock gate (fun () -> waitForHead ())
        push, consumer
