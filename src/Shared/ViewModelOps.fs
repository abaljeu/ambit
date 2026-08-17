namespace Gambol.Shared

// ---------------------------------------------------------------------------
// Stable public facade for pure view-model helpers
// ---------------------------------------------------------------------------

module ViewModel =

    type RowPatch = ViewModelDomPlan.RowPatch
    type RowMutation = ViewModelDomPlan.RowMutation

    let (|SetClassName|SetText|SetTextClasses|SetFoldArrow|SetNodeName|SetFileIndicator|) patch =
        match patch with
        | ViewModelDomPlan.SetClassName newClass -> SetClassName newClass
        | ViewModelDomPlan.SetText newText -> SetText newText
        | ViewModelDomPlan.SetTextClasses classes -> SetTextClasses classes
        | ViewModelDomPlan.SetFoldArrow arrow -> SetFoldArrow arrow
        | ViewModelDomPlan.SetNodeName name -> SetNodeName name
        | ViewModelDomPlan.SetFileIndicator (text, title) ->
            SetFileIndicator (text, title)

    let (|RemoveRow|CreateRow|RecreateRow|PatchRow|) mutation =
        match mutation with
        | ViewModelDomPlan.RemoveRow instId -> RemoveRow instId
        | ViewModelDomPlan.CreateRow instId -> CreateRow instId
        | ViewModelDomPlan.RecreateRow instId -> RecreateRow instId
        | ViewModelDomPlan.PatchRow (instId, patches) -> PatchRow (instId, patches)

    let buildParentInstanceIndex = ViewModelSiteMap.buildParentInstanceIndex
    let emptySiteMap = ViewModelSiteMap.emptySiteMap
    let buildSiteMapFrom = ViewModelSiteMap.buildSiteMapFrom
    let buildSiteMap = ViewModelSiteMap.buildSiteMap
    let firstChildSelection = ViewModelSiteMap.firstChildSelection
    let childSelectionAt = ViewModelSiteMap.childSelectionAt
    let reconcileSiteMapFrom = ViewModelSiteMap.reconcileSiteMapFrom
    let reconcileSiteMap = ViewModelSiteMap.reconcileSiteMap
    let toggleFold = ViewModelSiteMap.toggleFold
    let foldExpandedMembers = ViewModelSiteMap.foldExpandedMembers
    let expandEntry = ViewModelSiteMap.expandEntry
    let iterativeDeepenUnfold = ViewModelSiteMap.iterativeDeepenUnfold
    let parentSiblingTarget = ViewModelSiteMap.parentSiblingTarget
    let applyFoldSession = ViewModelSiteMap.applyFoldSession
    let buildOccurrenceIndex = ViewModelSiteMap.buildOccurrenceIndex
    let getVisibleRowIds = ViewModelSiteMap.getVisibleRowIds
    let getVisibleRowInstanceIds = ViewModelSiteMap.getVisibleRowInstanceIds
    let getVisibleInstanceIds = ViewModelSiteMap.getVisibleInstanceIds

    let getAllOccurrences = ViewModelOccurrence.getAllOccurrences
    let getOwnerOccurrence = ViewModelOccurrence.getOwnerOccurrence
    let tryReframeZoomAtOwnerParent = ViewModelOccurrence.tryReframeZoomAtOwnerParent
    let tryFocusNodeOccurrence = ViewModelOccurrence.tryFocusNodeOccurrence
    let focusNode = ViewModelOccurrence.focusNode
    let isOwnerUnderTrash = ViewModelOccurrence.isOwnerUnderTrash
    let occurrencesOutsideSelection = ViewModelOccurrence.occurrencesOutsideSelection
    let pushZoomIngress = ViewModelOccurrence.pushZoomIngress
    let ownerIngress = ViewModelOccurrence.ownerIngress
    let ownerPathIngress = ViewModelOccurrence.ownerPathIngress
    let trySiteMapParentOccurrence = ViewModelOccurrence.trySiteMapParentOccurrence
    let tryZoomInIngress = ViewModelOccurrence.tryZoomInIngress
    let resolveZoomOutParent = ViewModelOccurrence.resolveZoomOutParent
    let zoomIngressPathIds = ViewModelOccurrence.zoomIngressPathIds
    let tryZoomToIngressPathNode = ViewModelOccurrence.tryZoomToIngressPathNode

    let singleSelection = ViewModelSelection.singleSelection
    let singleSelectionForInstance = ViewModelSelection.singleSelectionForInstance
    let refreshSelection = ViewModelSelection.refreshSelection
    let firstSelectedNodeId = ViewModelSelection.firstSelectedNodeId
    let focusedNodeId = ViewModelSelection.focusedNodeId
    let tryFocusedNodeId = ViewModelSelection.tryFocusedNodeId
    let tryFindFocusedNode = ViewModelSelection.tryFindFocusedNode
    let focusedInstanceId = ViewModelSelection.focusedInstanceId
    let shiftArrow = ViewModelSelection.shiftArrow
    let collapseToFocus = ViewModelSelection.collapseToFocus
    let moveSelectionBy = ViewModelSelection.moveSelectionBy
    let applyMoveSelectionUp = ViewModelSelection.applyMoveSelectionUp
    let applyMoveSelectionDown = ViewModelSelection.applyMoveSelectionDown
    let cursorLevelStart = ViewModelSelection.cursorLevelStart
    let cursorLevelEnd = ViewModelSelection.cursorLevelEnd
    let shiftPgDown = ViewModelSelection.shiftPgDown
    let shiftPgUp = ViewModelSelection.shiftPgUp
    let cursorViewRootFirstChild = ViewModelSelection.cursorViewRootFirstChild
    let cursorViewRootLastChild = ViewModelSelection.cursorViewRootLastChild

    let isEntrySelected = ViewModelRowState.isEntrySelected
    let isEntryFocused = ViewModelRowState.isEntryFocused
    let isEditingEntry = ViewModelRowState.isEditingEntry
    let startEditInstanceAtPos = ViewModelRowState.startEditInstanceAtPos
    let isActiveEntry = ViewModelRowState.isActiveEntry
    let activeNodeId = ViewModelRowState.activeNodeId
    let tryFindFocusedPath = ViewModelRowState.tryFindFocusedPath
    let activeFileReference = ViewModelRowState.activeFileReference
    let refreshDesktopFileIndicator = ViewModelRowState.refreshDesktopFileIndicator
    let applyDesktopFileStatus = ViewModelRowState.applyDesktopFileStatus
    let desktopFileIndicatorText = ViewModelRowState.desktopFileIndicatorText
    let rowArtifactIndicatorState = ViewModelRowState.rowArtifactIndicatorState
    let outlineDisplayText = ViewModelRowState.outlineDisplayText
    let zoomIngressPathTexts = ViewModelRowState.zoomIngressPathTexts
    let rowNameDisplayText = ViewModelRowState.rowNameDisplayText
    let specialKindRowClass = ViewModelRowState.specialKindRowClass
    let specialKindSymbol = ViewModelRowState.specialKindSymbol
    let rowArtifactAbsentClassEligible = ViewModelRowState.rowArtifactAbsentClassEligible
    let rowFileIndicatorKindSymbol = ViewModelRowState.rowFileIndicatorKindSymbol
    let rowFileIndicator = ViewModelRowState.rowFileIndicator
    let rowFileIndicatorText = ViewModelRowState.rowFileIndicatorText
    let rowOwnershipClass = ViewModelRowState.rowOwnershipClass
    let rowFileUnparsedClassEligible = ViewModelRowState.rowFileUnparsedClassEligible
    let rowUnparsedObservationEligible = ViewModelRowState.rowUnparsedObservationEligible
    let rowChildrenIndicator = ViewModelChildrenIndicator.rowChildrenIndicator
    let bulletTip = ViewModelRowState.bulletTip

    let canCompareWorkspacePathSync = ViewModelRowState.canCompareWorkspacePathSync
    let rowWorkspacePathSyncStatus = ViewModelRowState.rowWorkspacePathSyncStatus
    let rowWorkspacePathSyncClass = ViewModelRowState.rowWorkspacePathSyncClass
    let applyWorkspacePathSyncSnapshot = ViewModelRowState.applyWorkspacePathSyncSnapshot

    let planPatchDOM = ViewModelDomPlan.planPatchDOM
    let needsDomOrderWalk = ViewModelDomPlan.needsDomOrderWalk
