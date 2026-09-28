namespace Gambol.Shared

module ViewModelSplitOps =

    /// Edit-field snapshot after Enter-split.
    /// Offset 0 keeps the current node in edit with its full text.
    /// Offset > 0 edits the new node (`newNodeText`).
    let splitContinueEditText
        (cursorPos: int) (currentText: string) (newNodeText: string) =
        if cursorPos <= 0 then currentText else newNodeText
