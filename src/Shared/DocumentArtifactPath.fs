namespace Gambol.Shared

open System

[<RequireQualifiedAccess>]
module DocumentArtifactPath =

    let private parts (relativePath: string) =
        relativePath.Replace('\\', '/').Split(
            '/',
            StringSplitOptions.RemoveEmptyEntries)
        |> Array.toList

    /// Exact `.amb` basename maps to its containing document root.
    let tryDirectoryFileOwnerParts (relativePath: string) : string list option =
        match parts relativePath |> List.rev with
        | name :: ownerReversed when Filename.isDirectoryFileBasename name ->
            Some(List.rev ownerReversed)
        | _ -> None

    let isDirectoryFile (relativePath: string) =
        tryDirectoryFileOwnerParts relativePath |> Option.isSome
