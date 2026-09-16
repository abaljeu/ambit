namespace Gambol.Shared

open System
open Gambol.Shared

type EventId = EventId of int

[<RequireQualifiedAccess>]
module EventId =
    let zero = EventId 0
    let next (EventId n) = EventId(n + 1)
    let max (EventId a) (EventId b) = EventId(Operators.max a b)

