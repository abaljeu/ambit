namespace Gambol.Shared

[<RequireQualifiedAccess>]
module CssClass =
    let add cls existing = existing + " " + cls
    let addIf cond cls existing = if cond then add cls existing else existing
