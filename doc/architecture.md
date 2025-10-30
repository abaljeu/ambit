# Architecture

Reworking [[old-architecture]].  In concept our scheme will be a series of transformations


## Model Concept

The concept is not literally

The server is a repository of text documents, 
Model
    History
    [] Doc 
        name
        []
            DocLine
                docLineId
    => organize by indentation
    DocTree 
        DocNode
            length
            children
            parent
            reference DocLine
    => take the tree, add folding annotations
    Site(Doc)
        SiteNode
            length
            siteNodeid
            children
            parent
            reference DocNode
            folded = false
    => filter, flatten
    Scene
        SceneRow
        reference SiteNode
        compute visible
    => transform
    Editor
        DOM editor element
        row(index) : EditorRow
        EditorRow
            DOM Row Element
                siteid

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


