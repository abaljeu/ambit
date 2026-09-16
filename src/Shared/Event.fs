namespace Gambol.Shared

[<RequireQualifiedAccess>]
module Ev =
    let id (event: Ev) : EventId = event.id

    let authority (event: Ev) : Authority = event.authority

    let ops (event: Ev) : Op list option =
        match event.body with
        | EventBody.Change ops
        | EventBody.Undo(_, ops)
        | EventBody.Redo(_, ops) -> Some ops
        | EventBody.ActorStart _
        | EventBody.ActorStop _ -> None

    let isAction (event: Ev) : bool = ops event |> Option.isSome

    let target (event: Ev) : EventId option =
        match event.body with
        | EventBody.Undo(target, _)
        | EventBody.Redo(target, _) -> Some target
        | EventBody.Change _
        | EventBody.ActorStart _
        | EventBody.ActorStop _ -> None

    let inverseOps (event: Ev) : Op list option =
        match ops event with
        | None -> None
        | Some opList ->
            let source =
                { id = 0
                  submissionId = event.submissionId
                  ops = opList }
            let inverse =
                Change.inverse Revision.Zero event.submissionId source
            Some inverse.ops

    let asChange (event: Ev) : Change =
        { id = event.id.Value
          submissionId = event.submissionId
          ops = ops event |> Option.defaultValue [] }

    let ofChange (commandName: string) (change: Change) : Ev =
        { id = EventId change.id
          submissionId = change.submissionId
          authority = Authority "Browser"
          commandName = commandName
          body = EventBody.Change change.ops }

    let apply (event: Ev) (state: State) : ApplyResult =
        match ops event with
        | None -> ApplyResult.Unchanged state
        | Some opList ->
            Change.apply
                { id = 0
                  submissionId = event.submissionId
                  ops = opList }
                state
