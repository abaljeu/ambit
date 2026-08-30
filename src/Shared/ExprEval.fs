namespace Gambol.Shared

[<RequireQualifiedAccess>]
module ExprEval =
    /// Delayed pull: one Answer plus a leftover cursor. Same shape as
    /// ViewModelSearch.takeResults (page, then leftover option).
    type Stream =
        private
        | Stream of (unit -> (ExprAnswer * Stream) option)

    type Predicate = ExprAnswer -> Stream

    let delay f = Stream f

    let pull (Stream f) = f ()

    let empty = Stream(fun () -> None)

    let cons (answer: ExprAnswer) (rest: Stream) =
        Stream(fun () -> Some(answer, rest))

    let singleton answer = cons answer empty

    let ofOption (answer: ExprAnswer option) =
        match answer with
        | None -> empty
        | Some a -> singleton a

    let rec ofList (answers: ExprAnswer list) =
        Stream(fun () ->
            match answers with
            | [] -> None
            | answer :: rest -> Some(answer, ofList rest))

    let toList stream =
        let rec loop acc remaining =
            match pull remaining with
            | None -> List.rev acc
            | Some(answer, rest) -> loop (answer :: acc) rest

        loop [] stream

    /// Page of `count` Answers and the unforced leftover, like takeResults.
    let take (count: int) (stream: Stream) : ExprAnswer list * Stream option =
        let rec loop n acc remaining =
            if n <= 0 then
                List.rev acc, Some remaining
            else
                match pull remaining with
                | None -> List.rev acc, None
                | Some(answer, rest) -> loop (n - 1) (answer :: acc) rest

        loop count [] stream

    let rec append (left: Stream) (right: Stream) : Stream =
        Stream(fun () ->
            match pull left with
            | Some(answer, rest) -> Some(answer, append rest right)
            | None -> pull right)

    let rec concatMap (f: ExprAnswer -> Stream) (stream: Stream) : Stream =
        Stream(fun () ->
            match pull stream with
            | None -> None
            | Some(answer, rest) -> pull (append (f answer) (concatMap f rest)))

    let bind (left: Predicate) (right: Predicate) : Predicate =
        fun input -> concatMap right (left input)

    let orEval (left: Predicate) (right: Predicate) : Predicate =
        fun input -> append (left input) (right input)

    /// Left Answers that also appear in the right sequence, both sides run on the same
    /// input. `once` drops repeats (AND); without it every match is yielded (IS).
    let private intersectEval (once: bool) (left: Predicate) (right: Predicate) : Predicate =
        fun input ->
            let rights = toList (right input)
            let isInRights answer =
                List.exists (ExprAnswer.equal answer) rights

            let rec filter seen remaining =
                Stream(fun () ->
                    match pull remaining with
                    | None -> None
                    | Some(answer, rest) ->
                        if isInRights answer
                           && not (List.exists (ExprAnswer.equal answer) seen) then
                            let seen = if once then answer :: seen else seen
                            Some(answer, filter seen rest)
                        else
                            pull (filter seen rest))

            filter [] (left input)

    let andEval (left: Predicate) (right: Predicate) : Predicate =
        intersectEval true left right

    let isEval (left: Predicate) (right: Predicate) : Predicate =
        intersectEval false left right

    let notEval (inner: Predicate) : Predicate =
        fun input ->
            Stream(fun () ->
                match pull (inner input) with
                | None -> Some(input, empty)
                | Some _ -> None)

    let ifEval (inner: Predicate) : Predicate =
        fun input ->
            Stream(fun () ->
                match pull (inner input) with
                | Some _ -> Some(input, empty)
                | None -> None)
