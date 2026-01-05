# Spec

Creating a full stack web application in pure immutable F#.  Except the main containers might be mutable, though their elements shall remain immutable.

The application implements a Workflowy style app.

# Directory Structure

```
src/
  Server/
    Program.fs          # Entry point, web server setup
    Server.fsproj
    Models/
      Node.fs           # Node type definition
    Storage/
      Storage.fs        # File I/O, serialization
    Routes/
      Api.fs            # HTTP endpoints
  Client/
    App.fs              # Entry point, Solid.js app
    Client.fsproj
    Components/
      NodeView.fs       # Node rendering components
      Editor.fs         # Text editing
    Models/
      Site.fs           # SiteNode, selection state
  Shared/
    Types.fs            # Shared types between client/server
    Shared.fsproj
tests/
  Server.Tests/
    Server.Tests.fsproj
  Client.Tests/
    Client.Tests.fsproj
doc/
  spec.md
  plan.md
Oxpecker/               # Reference only, do not edit
  examples/TodoList/
```

# Framework

We will try the Oxpecker.Solid framework.  [[Oxpecker/Readme.md]].  The Oxpecker directory is for reference only, not for editing.
Oxpecker/examples/TodoList is an example application in that framework.


# Server

Persistent program or transient?
Read data from file, build graph, send graph to client.  
Receive edits from client.  Update store.  Confirm to client.

## Model
type node
        - uid 
        - name
        - children : [uid]
        - text
        
noderoot : node

## Storage

Propose something.
- serialize
    - A) just guid content to disk
    - B) lowlevel update ops
    - C) tree + links
    - D) database

## low level ops
    - create node
    - set text old new
    - replace (establishes parent-child relations)
        - node
        - index
        - [ old guids
        - [ new guid ]
    - undo
    - redo

# Client

HTML view

## Model 
same as above

## Low Level Ops
same ops as above

## Site (compoSite)
type sitenode
    - node
    - occurence : scope
    - opened (include children)
    - children : [nodeview]
root : sitenode

selected : block
    - nodeview
    - span

## High level ops
- replace node childspan replacement_ids
    algorithm
    - with node
        - for each occurence
            - if include_children
                - remove children from index to old guids length
                - create occurence children 
                - add those at index
- [[ link ]]
    - find a node id'd link, and replace the current node with that.
- or copy, paste links
- undo
- redo
- edit link id
    - replace all uses.
- select node, select range

## View
- viewroot
    - nodeview
    - trace
- lines
    - editable for cursor
    - capture all keys
    - recursively add sitenodes, stopping at folded
- updates
    - replace site node -> replace view line
    - remove site node -> remove view lines, including all children.
        - find node; find nextnode; remove lines between these indexes
    - insert site node
        - recursively build from site node
        - insert into array

