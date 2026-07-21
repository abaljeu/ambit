namespace Gambol.Shared

open System
open DiffPlex
open DiffPlex.Chunkers

/// Thin DiffPlex façade over a text sequence (match key = line text).
[<RequireQualifiedAccess>]
module OutlineLcs =

    let private walkBlocks
        (prevLen: int)
        (blocks: DiffPlex.Model.DiffBlock list)
        : OutlineDiffOp list =
        let rec loop i j bi acc =
            if bi >= blocks.Length then
                let acc =
                    List.fold
                        (fun a k -> Equal(i + k, j + k) :: a)
                        acc
                        [ 0 .. prevLen - i - 1 ]

                List.rev acc
            else
                let b = blocks.[bi]

                let acc =
                    List.fold
                        (fun a k -> Equal(i + k, j + k) :: a)
                        acc
                        [ 0 .. b.DeleteStartA - i - 1 ]

                let acc =
                    List.fold
                        (fun a k -> Delete(b.DeleteStartA + k) :: a)
                        acc
                        [ 0 .. b.DeleteCountA - 1 ]

                let acc =
                    List.fold
                        (fun a k -> Insert(b.InsertStartB + k) :: a)
                        acc
                        [ 0 .. b.InsertCountB - 1 ]

                loop
                    (b.DeleteStartA + b.DeleteCountA)
                    (b.InsertStartB + b.InsertCountB)
                    (bi + 1)
                    acc

        loop 0 0 0 []

    /// LCS-style diff on text keys. Empty lists are handled without DiffPlex.
    let diffTexts: OutlineDiffTexts =
        fun previous edited ->
            match previous, edited with
            | [], [] -> []
            | [], news -> news |> List.mapi (fun i _ -> Insert i)
            | olds, [] -> olds |> List.mapi (fun i _ -> Delete i)
            | olds, news ->
                let oldText = String.Join("\n", Array.ofList olds)
                let newText = String.Join("\n", Array.ofList news)

                let result =
                    Differ.Instance.CreateDiffs(
                        oldText,
                        newText,
                        false,
                        false,
                        LineChunker.Instance)

                walkBlocks olds.Length (result.DiffBlocks |> Seq.toList)
