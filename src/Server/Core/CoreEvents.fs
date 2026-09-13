namespace Gambol.Server

open Gambol.Shared

type ActorResult = | ActorSucceeded

type ActorStarted =
    { eventId: int
      actor: Authority
      focus: NodeId }

type ActorFinished =
    { eventId: int
      actor: Authority }

type CoreEvent =
    | ActorStarted of ActorStarted
    | ActorFinished of ActorFinished

type EventLog =
    { appendStarted: Authority -> NodeId -> ActorStarted
      appendFinished: Authority -> ActorFinished
      all: unit -> CoreEvent list }

[<RequireQualifiedAccess>]
module EventLog =

    let create () : EventLog =
        let lockObj = obj ()
        let mutable next = 1
        let mutable acc: CoreEvent list = []
        { appendStarted =
            fun actor focus ->
                lock lockObj (fun () ->
                    let started =
                        { eventId = next
                          actor = actor
                          focus = focus }
                    next <- next + 1
                    acc <- CoreEvent.ActorStarted started :: acc
                    started)
          appendFinished =
            fun actor ->
                lock lockObj (fun () ->
                    let finished =
                        { eventId = next
                          actor = actor }
                    next <- next + 1
                    acc <- CoreEvent.ActorFinished finished :: acc
                    finished)
          all =
            fun () ->
                lock lockObj (fun () -> List.rev acc) }
