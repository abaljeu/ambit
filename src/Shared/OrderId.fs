namespace Gambol.Shared

open System

[<Struct>]
type OrderId =
    | OrderId of Guid

    member this.Value =
        let (OrderId value) = this
        value

    static member New() = OrderId(Guid.NewGuid())
