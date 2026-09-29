namespace Gambol.Shared

module ViewModelSplitOps =

    /// Edit-field snapshot after Enter-split.
    /// Offset 0 keeps the current node in edit with its full text.
    /// Offset > 0 edits the new node (`newNodeText`).
    let splitContinueEditText
        (cursorPos: int) (currentText: string) (newNodeText: string) =
        if cursorPos <= 0 then currentText else newNodeText

    /// After split ops + site-map reconcile: keep Editing on the target
    /// instance with the continue-edit snapshot (not an empty box).
    let continueEditAfterSplit
        (cursorPos: int)
        (currentText: string)
        (newNodeText: string)
        (newNodeId: NodeId)
        (focusInstId: SiteId option)
        (model: VM)
        : VM =
        let newSel =
            focusInstId
            |> Option.bind (
                ViewModel.singleSelectionForInstance model.siteMap)
            |> Option.orElseWith (fun () ->
                ViewModel.singleSelection model.graph model.siteMap newNodeId)
        { model with
            selectedNodes = newSel
            mode =
                Editing (
                    splitContinueEditText cursorPos currentText newNodeText,
                    EditCaret.Utf16Index 0) }
        |> ViewModel.retargetEditingSelection
