# Workspace git

Updated: 2026-09-23

## 1. Problem Statement

1. **No Actor owns the protocol** — A person in the Browser cannot view or edit a remote for the Workspace. No Actor receives Git Remote. DataDir/{label} gets no remote from the Browser. WorkspaceGit already owns DataDir/{label}.
2. **No push or pull of the current Node** — The person cannot push or pull the current Node and its Owned descendants. No Actor receives Git Push or Git Pull.
3. **Pack services are a different door** — WorkspacePush is git-receive-pack. WorkspacePull is git-upload-pack. Those services send and receive packs for DataDir/{label}. They are not an Actor that receives Git Push or Git Pull for the current Node toward a remote the person sets.
4. **The App holds no remote** — The App map holds no remote and runs no git. Desktop push into the Workspace stays WebDAV. A person who works in the Browser still cannot send DataDir/{label} to an external remote.
5. **No conflict rule for that remote** — The person cannot reject a push when both sides changed the same path since the merge-base, and cannot accept non-overlapping edits when the histories diverge. The person cannot keep Persist and the local commit when that push is rejected.
6. **No Pull that keeps File identity** — The person cannot overwrite the pulled files and then accept upstream on every conflicting file through the warm read. The warm read is DocumentWarm. It overlays and keeps File identity. The cold read refuses a text, name, or kind mismatch.
7. **HEAD moves that are not Git Pull** — After a server HEAD move that is not Git Pull, Lazy Load marks the File Node Unparsed and leaves it for a later planParseFile warm read of that one File Node. The person has no Git Pull that overwrites the pulled files and then runs the warm read.
8. **Projection and Parse stay as they are** — persistGraphOps writes the Graph projection. PostgreSQL stays the authority ([[doc/current/persistence-model.md]]). Parse reconciles one file with the Nodes under that File Node. When the Graph is not current, Nodes are marked Unparsed the same way as now. The person still needs Pull and Persist to stay apart, and Persist to run only when the Graph is current with the file.

## 2. Solution

1. **WorkspaceGit is the Actor** — WorkspaceGit is the Actor that owns this protocol. The Actor receives Git Remote, Git Push, and Git Pull.
2. **Git Remote** — On Git Remote, the Actor views and edits the remote for DataDir/{label}. That command is how DataDir/{label} gets its remote. Git Remote has an option, default exclude, for Directory File / Ambit note paths named `.amb`. That skip is the same hard skip class as `.git/`.
3. **Git Push** — On Git Push, the Actor pushes the current Node and its Owned descendants. Git Push fails when no remote is set.
4. **Git Pull** — On Git Pull, the Actor pulls the current Node and its Owned descendants. Git Pull fails when no remote is set.
5. **DataDir starts the transfer** — Git Push and Git Pull are the push and pull that DataDir starts toward the remote Git Remote sets. WorkspaceGit owns DataDir/{label}. WorkspacePush stays git-receive-pack. WorkspacePull stays git-upload-pack.
6. **Pull overwrite** — On Pull, the Actor overwrites the local files it is pulling. Those files are the current Node and its Owned descendants that are being updated.
7. **Upstream on conflict** — On a rejected push, the follow-up Pull discards persisted bytes on conflicting files and accepts upstream for every conflicting file. That overwrite is how Pull accepts upstream on the conflicting files. Non-conflicting local files stay.
8. **Warm read after Pull** — Then Pull invokes the warm read. The warm read is DocumentWarm. The warm read overlays and keeps File identity. Pull does not take the cold read. The cold read refuses a text, name, or kind mismatch.
9. **Push conflict rule** — A conflicting file is a path both sides changed since the merge-base. On Git Push, the Actor rejects conflicting files. The Actor accepts non-overlapping edits. That accept is not a fast-forward. Histories can diverge with no shared path.
10. **Reject keeps local work** — Persist and the local commit remain when the push is rejected.
11. **Pull and Persist** — Pull and Persist do not run at the same time. Persist runs only when the Graph is current with the file. When the Graph is not current, Nodes are marked Unparsed the same way as now.
12. **Other HEAD moves** — After a server HEAD move that is not Git Pull, Lazy Load marks the File Node Unparsed and leaves it for a later planParseFile warm read of that one File Node.

## 3. User Stories

1. **View and edit the remote** — As a person, I want the WorkspaceGit Actor to view and edit the remote when it receives Git Remote, so that DataDir/{label} gets its remote from the Browser.
2. **Exclude `.amb` by default** — As a person, I want the WorkspaceGit Actor, on Git Remote, to offer an option, default exclude, for Directory File / Ambit note paths named `.amb`, in the same hard skip class as `.git/`, so that I can keep those paths off the remote.
3. **Push the current Node** — As a person, I want the WorkspaceGit Actor to push the current Node and its Owned descendants when it receives Git Push, so that I send that Node and its Owned descendants to the remote.
4. **Fail push with no remote** — As a person, I want the WorkspaceGit Actor to fail Git Push when no remote is set, so that the push does not start without a remote.
5. **Pull the current Node** — As a person, I want the WorkspaceGit Actor to pull the current Node and its Owned descendants when it receives Git Pull, so that I bring that Node and its Owned descendants from the remote.
6. **Fail pull with no remote** — As a person, I want the WorkspaceGit Actor to fail Git Pull when no remote is set, so that the pull does not start without a remote.
7. **DataDir moves toward the remote** — As a person, I want the WorkspaceGit Actor to make Git Push and Git Pull the push and pull that DataDir starts toward the remote Git Remote sets, so that the files under DataDir/{label} are what the remote receives and sends.
8. **Overwrite pulled files** — As a person, I want the WorkspaceGit Actor, on Pull, to overwrite the local files it is pulling, so that the current Node and its Owned descendants that are being updated match the pull.
9. **Accept upstream after reject** — As a person, I want the WorkspaceGit Actor, on the follow-up Pull after a rejected push, to discard persisted bytes on conflicting files and accept upstream for every conflicting file, so that the overwrite is how I accept upstream on those files.
10. **Keep non-conflicting files** — As a person, I want non-conflicting local files to stay on that follow-up Pull, so that files without a conflict keep their local bytes.
11. **Warm read keeps File identity** — As a person, I want the WorkspaceGit Actor, on Pull, to invoke the warm read after the overwrite, so that DocumentWarm overlays the file and keeps File identity.
12. **Leave the cold read** — As a person, I want the WorkspaceGit Actor to leave the cold read unused on Pull, so that a text, name, or kind mismatch does not refuse the Pull.
13. **Name a conflicting file** — As a person, I want a conflicting file to be a path both sides changed since the merge-base, so that I know which paths block the push.
14. **Reject a conflicting push** — As a person, I want the WorkspaceGit Actor to reject conflicting files on Git Push, so that the remote does not take a path both sides changed.
15. **Accept non-overlapping edits** — As a person, I want the WorkspaceGit Actor to accept non-overlapping edits on Git Push when histories diverge and share no path, so that the accept is not a fast-forward.
16. **Keep Persist and the local commit** — As a person, I want Persist and the local commit to remain when the push is rejected, so that the reject leaves my local work in place.
17. **Separate Pull and Persist** — As a person, I want the WorkspaceGit Actor to keep Pull apart from Persist, so that they do not run at the same time.
18. **Persist only when current** — As a person, I want Persist to run only when the Graph is current with the file, so that the projection matches the file.
19. **Mark Unparsed when not current** — As a person, I want Nodes marked Unparsed the same way as now when the Graph is not current, so that I still see that those Nodes are Unparsed.
20. **Unparsed after another HEAD move** — As a person, I want a server HEAD move that is not Git Pull to mark the File Node Unparsed through Lazy Load, so that a later planParseFile warm read of that one File Node can Parse it.
21. **Parse one file** — As a person, I want Parse to reconcile one file with the Nodes under that File Node, so that the warm read after Git Pull and the later planParseFile read stay that same Parse.

## 4. Out of Scope

1. **Pack service replacement** — This spec does not replace WorkspacePush or WorkspacePull. WorkspacePush stays git-receive-pack. WorkspacePull stays git-upload-pack.
2. **Desktop git** — This spec does not put a remote on the App map, run git in the App, or change Desktop push into the Workspace away from WebDAV.
3. **Graph authority** — This spec does not change persistGraphOps and does not move authority off PostgreSQL. persistGraphOps writes the Graph projection.
4. **Parse redesign** — This spec does not change what Parse reconciles. Parse reconciles one file with the Nodes under that File Node.
5. **Unparsed marking change** — This spec does not change how Nodes are marked Unparsed when the Graph is not current.
6. **Cold read for Pull** — This spec does not use the cold read for Git Pull.
7. **Fast-forward-only accept** — This spec does not limit an accepted non-overlapping push to a fast-forward.
8. **Per-file conflict choice** — This spec does not offer a choice to keep local bytes on a conflicting file. The follow-up Pull accepts upstream for every conflicting file.
9. **Directory File transport** — This spec does not change WebDAV Upload or Download of Directory Files, and does not change Server DataDir tracking of `.amb`.
10. **Ambit note backup** — A new offsite path for Ambit notes is out of scope for Workspace git. [[plan/roadmap/epics/chapters/ambit-keeps-consistency-with-desktop-repo-for-agentic-work.md]] already places that backup on Server DataDir through WebDAV Upload and Download and Server git.
11. **Automatic Upload and Download** — Automatic Upload and Download are out of scope for Workspace git. [[plan/auto-download-persisted-files/project.md]] and the documents Chapter own that work.

## 5. Further Notes

1. **Ambit notes and most remotes** — Most remotes must not receive Ambit notes. On Git Remote, the Actor has an option, default exclude, for Directory File / Ambit note paths named `.amb`, the same hard skip class as `.git/`.
