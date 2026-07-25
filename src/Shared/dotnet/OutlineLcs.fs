namespace Gambol.Shared

open System
open DiffPlex

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

    /// A delimiter absent from every item in `texts`, so joining/splitting on
    /// it round-trips exactly, regardless of embedded `\r`/`\n` in any item.
    let private pickDelimiter (texts: string list) : string =
        let rec pick (candidate: string) =
            if texts |> List.exists (fun t -> t.Contains(candidate: string)) then
                pick (candidate + Guid.NewGuid().ToString("N"))
            else
                candidate

        pick "\u0000GAMBOL_LCS_SPLIT\u0000"

    /// Splits on an exact literal delimiter (no line semantics), so an item's
    /// embedded `\r`/`\n` never produces extra chunks beyond the item count.
    let private sentinelChunker (delimiter: string) : IChunker =
        { new IChunker with
            member _.Chunk(text: string) =
                text.Split([| delimiter |], StringSplitOptions.None) }

    /// LCS-style diff on text keys. Empty lists are handled without DiffPlex.
    let diffTexts: OutlineDiffTexts =
        fun previous edited ->
            match previous, edited with
            | [], [] -> []
            | [], news -> news |> List.mapi (fun i _ -> Insert i)
            | olds, [] -> olds |> List.mapi (fun i _ -> Delete i)
            | olds, news ->
                let delimiter = pickDelimiter (olds @ news)
                let oldText = String.Join(delimiter, Array.ofList olds)
                let newText = String.Join(delimiter, Array.ofList news)

                let result =
                    Differ.Instance.CreateDiffs(
                        oldText,
                        newText,
                        false,
                        false,
                        sentinelChunker delimiter)

                walkBlocks olds.Length (result.DiffBlocks |> Seq.toList)
