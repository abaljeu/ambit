namespace Gambol.Shared

type EventId =
    private
    | Zero // the Ev is a draft
    | Int of int // The Ev is an event; accepted, uniquely numbered, 
        // and has been stored, and applied in the system

    member this.Value =
        match this with
        | Zero -> 0
        | Int n -> n

[<RequireQualifiedAccess>]
module EventId =
    let zero = Zero
    let next (id: EventId) =
        match id with
        | Int n -> Int(n + 1)
        | Zero -> Zero
    let max (a: EventId) (b: EventId) =
        if a.Value >= b.Value then a else b
    let value (id: EventId) = id.Value
    let display (id: EventId) = string id.Value
    let isAccepted id =
        id <> EventId.Zero
    let fromJson n =
        if n > 0 then Int n
        else Zero
    let toJson (id: EventId) = id.Value
