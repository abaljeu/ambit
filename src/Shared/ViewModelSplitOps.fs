namespace Gambol.Shared

module ViewModelSplitOps =

    /// Cursor and the two texts `splitContinueEditText` chooses between.
    type SplitEditText = {
        cursorPos: int
        currentText: string
        newNodeText: string
    }

    /// Split snapshot plus the node the edit continues on.
    type ContinueSplitEdit = {
        text: SplitEditText
        newNodeId: NodeId
        focusInstanceId: SiteId option
    }

    /// Edit-field snapshot after Enter-split.
    /// Offset 0 keeps the current node in edit with its full text.
    /// Offset > 0 edits the new node (`newNodeText`).
    let splitContinueEditText (edit: SplitEditText) =
        if edit.cursorPos <= 0 then edit.currentText else edit.newNodeText

    /// After split ops + site-map reconcile: keep Editing on the target
    /// instance with the continue-edit snapshot (not an empty box).
    let continueEditAfterSplit (edit: ContinueSplitEdit) (model: VM) : VM =
        let newSel =
            edit.focusInstanceId
            |> Option.bind (
                ViewModel.singleSelectionForInstance model.siteMap)
            |> Option.orElseWith (fun () ->
                ViewModel.singleSelection model.graph model.siteMap edit.newNodeId)
        { model with
            selectedNodes = newSel
            mode =
                Editing (
                    splitContinueEditText edit.text,
                    EditCaret.Utf16Index 0) }
        |> ViewModel.retargetEditingSelection
