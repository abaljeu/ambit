# Workspaces checklist

Category: Workspace scale
See also: [[workspaces]], [[doc/current/workspace-graph]], [[doc/current/workspace-local-mapping]]

Living checklist for implementing workspaces.  Mark done an item when it is done.

- Workspace = graph node type; 
- 1:1 with `DataDir/{workspaceName}`; 
- that directory is an independent git repo.
- Desktop holds a partial map `workspaceName` → local path (also a git repo); the server is the push target.

## Graph model

Workspace as a first-class graph node type and structural rules around it.

- [ ] Files and Directories are only allowed to be in Directories or Workspaces. (Root is a workspace.)

## Server DataDir
One workspace folder per name under DataDir; ownership and on-disk layout.

## Git identity
Each workspace directory is its own git repository (server and mapped local roots).

## Desktop mapping
Partial `workspaceName` → absolute local path bindings on the desktop.

## Sync / push
Move changes between local workspace repos and the server as push target.

## References (`//name`)
Cross-workspace and path references using `//name` form.

## Client UX
How users create, open, navigate, and work in workspaces in the UI.

## Import
Bringing existing trees or repos into the workspace model.
