namespace Gambol.Shared

open Gambol.Shared.ViewModel

/// Queue a Run or Cancel on the Browser pending list.
[<RequireQualifiedAccess>]
module RunLaunch =

    let private posted (commandName: string) (body: EventBody) : Ev =
        { id = EventId.zero
          submissionId = System.Guid.NewGuid()
          authority = Authority "Browser"
          commandName = commandName
          body = body }

    let queueStart
        (request: ActorStart)
        (eventId: EventId)
        (syncInfo: SyncInfo)
        (effects: Effect list)
        : SyncInfo * Effect list =
        SyncPlanner.queueWithOpenBatch
            (posted "Exec" (EventBody.ActorStart request))
            eventId
            syncInfo
            effects

    let queueCancel
        (focusId: NodeId)
        (eventId: EventId)
        (syncInfo: SyncInfo)
        (effects: Effect list)
        : SyncInfo * Effect list =
        SyncPlanner.queueWithOpenBatch
            (posted "Cancel" (EventBody.Cancel focusId))
            eventId
            syncInfo
            effects
