module Gambol.Client.UpdateEdit

open Gambol.Client.UpdateHelpers
open Gambol.Shared
open Gambol.Shared.ViewModel


let moveEdit (delta: int) (cursorPos: int) (model: VM) : VM * Effect list =
    moveEditImpl delta (MoveEditUtf16 cursorPos) model

let moveEditUpAtClientX (clientX: float) (model: VM) : VM * Effect list =
    moveEditImpl -1 (MoveEditPrevLastLineX clientX) model

let moveEditDownAtClientX (clientX: float) (model: VM) : VM * Effect list =
    moveEditImpl 1 (MoveEditNextFirstLineX clientX) model

/// Join the currently-edited node with the previous visible (inorder) node.
/// 1. If current has no children: append current's text to prev, delete current.
/// 2. If current and prev both have children: abort.
/// 3. If current has children but prev does not: move current's children to prev, then do 1.
/// Cursor lands at the join point (end of prevText) in prev.
let joinWithNext (currentText: string) (model: VM) : VM * Effect list =
    match model.mode, model.selectedNodes with
    | Editing _, Some sel ->
        let currentId = focusedNodeId model.graph sel
        let rows = getVisibleRowInstanceIds model.siteMap
        match focusedInstanceId sel with
        | None -> model, []
        | Some focusInstId ->
            match rows |> List.tryFindIndex ((=) focusInstId) with
            | None -> model, []
            | Some currentIndex ->
                if currentIndex >= rows.Length - 1 then model, []
                else
                    let nextInstId = rows.[currentIndex + 1]
                    let nextEntry = model.siteMap.entries.[nextInstId]
                    let nextId = nextEntry.nodeId
                    let nextNode = model.graph.nodes.[nextId]
                    let currentNode = model.graph.nodes.[currentId]
                    if not currentNode.children.IsEmpty then
                        let pos = readEditInputCursor ()
                        match model.mode with
                        | Editing (t, _) ->
                            { model with mode = Editing (t, EditCaret.Utf16Index pos) }, []
                        | _ -> model, []
                    else
                        match Graph.tryFindParentAndIndex nextId model.graph with
                        | None -> model, []
                        | Some (_parentId, _indexInParent) ->
                            if System.String.IsNullOrWhiteSpace currentText then
                                match Graph.tryFindParentAndIndex currentId model.graph with
                                | None -> model, []
                                | Some (currParentId, currIndexInParent) ->
                                    let ops =
                                        [ Op.Replace
                                            (currParentId, currIndexInParent, ownedChildren [currentId], []) ]
                                    let change =
                                        { id = model.revision.Value
                                          changeId = System.Guid.NewGuid()
                                          ops = ops }
                                    match applyAndPost change model with
                                    | None, _ -> model, []
                                    | Some m, effects ->
                                        let result = withSiteMap m
                                        { result with
                                            mode = Editing (nextNode.text, EditCaret.Utf16Index 0)
                                            selectedNodes =
                                                singleSelection result.graph result.siteMap nextId },
                                        effects
                            else
                                let joinedText = currentText + nextNode.text
                                let cursorPos = currentText.Length
                                match Graph.tryFindParentAndIndex currentId model.graph with
                                | None -> model, []
                                | Some (currParentId, currIndexInParent) ->
                                    let ops =
                                        [ if joinedText <> nextNode.text then
                                              yield Op.SetText(nextId, nextNode.text, joinedText)
                                          yield Op.Replace
                                              (currParentId, currIndexInParent, ownedChildren [currentId], [])
                                          ]
                                    let change =
                                        { id = model.revision.Value
                                          changeId = System.Guid.NewGuid()
                                          ops = ops }
                                    match applyAndPost change model with
                                    | None, _ -> model, []
                                    | Some m, effects ->
                                        let result = withSiteMap m
                                        { result with
                                            mode = Editing (joinedText, EditCaret.Utf16Index cursorPos)
                                            selectedNodes =
                                                singleSelection result.graph result.siteMap nextId },
                                        effects
    | _ -> model, []

let joinWithPrevious (currentText: string) (model: VM) : VM * Effect list =
    match model.mode, model.selectedNodes with
    | Editing _, Some sel ->
        let currentId = focusedNodeId model.graph sel
        let rows = getVisibleRowInstanceIds model.siteMap
        match focusedInstanceId sel with
        | None -> model, []
        | Some focusInstId ->
            match rows |> List.tryFindIndex ((=) focusInstId) with
            | None | Some 0 -> model, []
            | Some currentIndex ->
                let prevInstId = rows.[currentIndex - 1]
                let prevEntry = model.siteMap.entries.[prevInstId]
                let prevId = prevEntry.nodeId
                let prevNode = model.graph.nodes.[prevId]
                let currentNode = model.graph.nodes.[currentId]
                if not currentNode.children.IsEmpty && not prevNode.children.IsEmpty then
                    model, []
                else
                    match Graph.tryFindParentAndIndex currentId model.graph with
                    | None -> model, []
                    | Some (parentId, indexInParent) ->
                        let joinedText = prevNode.text + currentText
                        let cursorPos = prevNode.text.Length
                        let ops =
                            [ if joinedText <> prevNode.text then
                                  yield Op.SetText(prevId, prevNode.text, joinedText)
                              if not currentNode.children.IsEmpty then
                                  yield Op.Replace
                                      (prevId, prevNode.children.Length, [], currentNode.children)
                              yield Op.Replace
                                  (parentId, indexInParent, ownedChildren [currentId], []) ]
                        let change =
                            { id = model.revision.Value
                              changeId = System.Guid.NewGuid()
                              ops = ops }
                        match applyAndPost change model with
                        | None, _ -> model, []
                        | Some m, effects ->
                            let result = withSiteMap m
                            { result with
                                mode = Editing (joinedText, EditCaret.Utf16Index cursorPos)
                                selectedNodes =
                                    singleSelection result.graph result.siteMap prevId },
                            effects
    | _ -> model, []
