namespace Gambol.Shared

open System.Text.RegularExpressions

/// An ordered set of CSS class names assigned to a node.
type CssClasses = Classes of string list

[<RequireQualifiedAccess>]
module CssClass =

    // ---- Collection type ----

    let empty : CssClasses = Classes []

    let ofList (classes: string list) : CssClasses = Classes classes

    let toList (Classes classes) : string list = classes

    let contains (name: string) (Classes classes) : bool =
        List.contains name classes

    /// Encode the collection as a metadata token string, e.g. ".h1 .blue".
    let toMetaString (Classes classes) : string =
        classes |> List.map (fun c -> "." + c) |> String.concat " "

    /// Toggle a class name in or out of the collection.
    let toggle (name: string) (Classes classes) : CssClasses =
        if List.contains name classes
        then Classes (List.filter ((<>) name) classes)
        else Classes (classes @ [name])

    // ---- DOM className string building (for row-level system classes) ----

    let add cls existing = existing + " " + cls
    let addIf cond cls existing = if cond then add cls existing else existing

    // ---- Validation ----

    /// True when name is a legal CSS identifier and does not start with the reserved "amb-" prefix.
    let isValidUserClass (name: string) : bool =
        if System.String.IsNullOrEmpty(name) then false
        elif name.StartsWith("amb-") then false
        else Regex.IsMatch(name, @"^[a-zA-Z_][a-zA-Z0-9_-]*$")
