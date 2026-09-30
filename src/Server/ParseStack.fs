namespace Gambol.Server

open System.Threading
open Gambol.Shared

/// LIFO work for the long-lived Parse stack. Core pushes; one consumer waits.
[<RequireQualifiedAccess>]
module ParseStack =

    /// Closes private cells; returns push and the blocking consumer.
    let create () : (NodeId -> unit) * (unit -> NodeId) =
        let gate = obj ()
        let items: NodeId list ref = ref []
        let push (fileId: NodeId) =
            lock gate (fun () ->
                items.Value <- fileId :: items.Value
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
