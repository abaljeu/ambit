namespace Gambol.Shared

[<RequireQualifiedAccess>]
module ExprEval =
    type Predicate = ExprAnswer -> ExprAnswer list

    let bind (left: Predicate) (right: Predicate) : Predicate =
        fun input -> left input |> List.collect right

    let orEval (left: Predicate) (right: Predicate) : Predicate =
        fun input -> left input @ right input

    let andEval (left: Predicate) (right: Predicate) : Predicate =
        fun input ->
            let rights = right input
            let isInRights answer = List.exists (ExprAnswer.equal answer) rights
            let rec loop seen acc remaining =
                match remaining with
                | [] -> List.rev acc
                | answer :: rest ->
                    if isInRights answer && not (List.exists (ExprAnswer.equal answer) seen) then
                        loop (answer :: seen) (answer :: acc) rest
                    else
                        loop seen acc rest
            loop [] [] (left input)

    let notEval (inner: Predicate) : Predicate =
        fun input ->
            match inner input with
            | [] -> [ input ]
            | _ -> []
