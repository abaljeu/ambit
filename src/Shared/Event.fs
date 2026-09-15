namespace Gambol.Shared.Events

open System
open Gambol.Shared

type EventId = EventId of int

[<RequireQualifiedAccess>]
module EventId =
    let zero = EventId 0
    let next (EventId n) = EventId(n + 1)
    let max (EventId a) (EventId b) = EventId(Operators.max a b)

type Authority = Authority of string

type ActorResult =
    | ActorSucceeded
    | ActorFailed

type ActorStart =
    { zoomId: NodeId
      focusId: NodeId
      commandId: NodeId
      graphIds: NodeId list
      revision: EventId }

[<RequireQualifiedAccess>]
type EventBody =
    | Change of ops: Op list
    | Undo of target: EventId * ops: Op list
    | Redo of target: EventId * ops: Op list
    | ActorStart of ActorStart
    | ActorStop of focusId: NodeId * result: ActorResult

type Event =
    { id: EventId
      submissionId: Guid
      authority: Authority
      commandName: string
      body: EventBody }

[<RequireQualifiedAccess>]
module Event =
    let id (event: Event) : EventId = event.id

    let authority (event: Event) : Authority = event.authority

    let ops (event: Event) : Op list option =
        match event.body with
        | EventBody.Change ops
        | EventBody.Undo(_, ops)
        | EventBody.Redo(_, ops) -> Some ops
        | EventBody.ActorStart _
        | EventBody.ActorStop _ -> None

    let isAction (event: Event) : bool = ops event |> Option.isSome

    let target (event: Event) : EventId option =
        match event.body with
        | EventBody.Undo(target, _)
        | EventBody.Redo(target, _) -> Some target
        | EventBody.Change _
        | EventBody.ActorStart _
        | EventBody.ActorStop _ -> None

    let inverseOps (event: Event) : Op list option =
        match ops event with
        | None -> None
        | Some opList ->
            let source =
                { id = 0
                  changeId = event.submissionId
                  ops = opList }
            let inverse =
                Change.inverse Revision.Zero event.submissionId source
            Some inverse.ops

    let apply (event: Event) (state: State) : ApplyResult =
        match ops event with
        | None -> ApplyResult.Unchanged state
        | Some opList ->
            Change.apply
                { id = 0
                  changeId = event.submissionId
                  ops = opList }
                state
