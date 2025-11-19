# Architecture

- **Frontend**: TypeScript compiled to ES6 modules
- **Backend**: PHP for authentication and file storage
- **Storage**: Plain text `.amb` files in `php/doc/`
- **URL format**: `ambit.php?doc=filename.amb`

## Design Principles
The principles here are aspirational.  The code presently may not adhere to them, but when writing new code, let's try to improve it towards these.

TypeScript code is modular. Each file defines a module as its primary export.
Functions must take clear roles: Either a function is a query and does not modify persistent data or its parameters; or a function is a command and it modifies what is implied by its name.

We aim for null-free coding.  How that is achieved can be a matter of discussion.

Code is based on strongly typed objects with clear ownership:
- Objects may have public members for read-access only.
- Only the owning object or module should modify internal state

## Dependency structure:
- Scene depends on Model (never the reverse)
- Editor depends on the DOM only, but also has temporary access to Scene.RowData
- Controller orchestrates and depends on Scene and Editor; Nothing depends on controller, except events.
- Network operations (Get/Post) reference Model only, not Controller/Scene/Editor
- Events flow: User → Editor/Controller → Scene → Model → Network → Controller/Editor
- CellBlock selection state is stored in Site (Model layer); Scene queries Site for current CellBlock; Scene passes selection state to Editor (via CellSelectionState array); Editor applies CSS classes based on state passed from Scene; Scene.updatedSelection() triggers Editor styling updates


## Model Concept

The server is a repository of text documents, 
Model
    History
    => organize by indentation
    [] Doc (received from server)
        name
        DocLine
            docLineId
            length
            children
            parent
            reference : DocLine?
    => take the tree, add folding annotations
    Site (composite of Doc fragments)
        Root : DocLine
        SiteNode

            length
            siteNodeid
            children
            parent
            folded = false
    => filter, flatten
    Scene
        SceneRow
            SceneCell
        reference SiteNode
        compute visible
        queries Site for CellBlock state
    => transform
    Editor
        DOM editor element
        row(index) : EditorRow
        EditorRow
            DOM Row Element
                siteid

            cells : Cell[]

        applies CellBlock CSS classes based on selection state passed from Scene
    Selection =
        CursorSelection
            cell
            start
            end
        | CellBlock (selection state)
            parentSiteRow
            child index range (vertical)
            cell index range (horizontal, -1 = all columns)
            active cell (SiteRow + cell index)


## History

[] Transaction
    [] DocChanges
        delete | insert | change
        doc
        owner : DocLineId
        lineOffset
        lineId
        oldtext?
        newtext?
[ ]
    [ ] SceneChange
        delete | insert | change
        owner : siteid
        rowOffset
        siteId
        foldToggled : bool
        oldtext?
        newtext?


### Document changes


