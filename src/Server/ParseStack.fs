namespace Gambol.Server

open System.Threading
open Gambol.Shared

/// LIFO work for the long-lived Parse stack. Core pushes; one consumer waits.
[<RequireQualifiedAccess>]
module ParseStack =

    /// Closes private cells; returns push and the blocking consumer.
    /// None means the stack was stopped and drained.
    let create () : (NodeId -> unit) * (unit -> NodeId option) =
        let gate = obj ()
        let stopped = ref false
        let items: NodeId list ref = ref []
        let push (fileId: NodeId) =
            lock gate (fun () ->
                items.Value <- fileId :: items.Value
                Monitor.Pulse gate)
        let rec waitForHead () =
            match items.Value with
            | head :: rest ->
                items.Value <- rest
                Some head
            | [] when stopped.Value -> None
            | [] ->
                Monitor.Wait gate |> ignore
                waitForHead ()
        let requestStop () =
            lock gate (fun () ->
                stopped.Value <- true
                Monitor.PulseAll gate)
        ServerRunRegistry.registerParseUnblock requestStop
        let consumer () =
            lock gate (fun () -> waitForHead ())
        push, consumer
