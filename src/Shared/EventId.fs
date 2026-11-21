namespace Gambol.Shared

[<RequireQualifiedAccess>]
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
    let zero = EventId.Zero
    let next (id: EventId) =
        match id with
        | EventId.Int n -> EventId.Int(n + 1)
        | EventId.Zero -> EventId.Zero
    let max (a: EventId) (b: EventId) =
        if a.Value >= b.Value then a else b
    let value (id: EventId) = id.Value
    let display (id: EventId) = string id.Value
    let isAccepted id =
        id <> EventId.Zero
    let fromJson n =
        if n > 0 then EventId.Int n
        else EventId.Zero
    let toJson (id: EventId) = id.Value
