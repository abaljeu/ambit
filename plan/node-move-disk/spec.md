# Node move and disk

Map: [[map.md]]

## 1. User stories

1. Derived-path comparison — As a person, I want each change in Scope to compare the derived path of each Owned File Node and each Owned Directory Node before the Graph change with the derived path after the Graph change, so that the file tree matches the graph outline and I can share those files through git push and git pull.

    - The same derived path leaves that File or that Directory at that path.
    - A different derived path that stays under the same Workspace Node is the input to Relocate with git mv.
    - A different derived path under a different Workspace Node is the input to Move into a different Workspace.
2. Relocate with git mv — As a person, I want a different derived path that stays under the same Workspace Node to move that File or that Directory with `git mv`, so that git tracks the move and the file tree matches the graph outline for git push and git pull.
3. File Node rename — As a person, I want Derived-path comparison on a rename of an Owned File Node in the graph outline, so that the File ends at its derived path and the file tree matches the graph outline for git push and git pull.
4. File Node under a different Node — As a person, I want Derived-path comparison on an `Op.Replace` that places a File Node as an Owned child under a different Node in the graph outline, so that the File ends at its derived path and the file tree matches the graph outline for git push and git pull.
5. Directory Node move — As a person, I want Derived-path comparison on a move of an Owned Directory Node in the graph outline, so that the Directory ends at its derived path and the file tree matches the graph outline for git push and git pull.
6. Normal Node move — As a person, I want Derived-path comparison on a move of a Normal Node in the graph outline that changes a derived path of an Owned File Node or an Owned Directory Node, including through other Normal Nodes, so that each of those Files and Directories ends at its derived path and the file tree matches the graph outline for git push and git pull.
7. Nested and overlapping paths — As a person, I want one Graph change to apply Derived-path comparison to an Owned Directory Node and to an Owned File Node or Owned Directory Node nested under that Directory Node, and to overlapping derived paths in that same change, so that each of those Files and Directories ends at its derived path and the file tree matches the graph outline for git push and git pull.
8. Never-parsed File Nodes — As a person, I want Derived-path comparison when Owned File Nodes that have never been parsed move under a new Directory Node in the graph outline, so that those Files end at their derived paths and the file tree matches the graph outline for git push and git pull. This holds when the Owned File Node lands as a direct Owned child or deeper in the Body. The ticket is [01 — Moving never-parsed File Nodes under a new Directory Node leaves the files behind](plan/node-move-disk/issues/01-move-unparsed-files-under-new-directory.md).
9. Change conflict — As a person, I want a change conflict on a node move or a disk operation to cause amendment through [ChangeAmendment](src/Shared/ChangeAmendment.fs) before the change lands, so that the change is disambiguated before it lands and the file tree of the Owned nodes matches the graph outline for git push and git pull.

    A change conflict is one of these:

    - The destination path already exists.
    - The source and the destination disagree.

    The operation does not overwrite.

    The operation does not merge.
10. Move into a different Workspace — As a person, I want a move of an Owned File Node or an Owned Directory Node into a different Workspace Node in the graph outline to delete that File or that Directory from the source Workspace repository and add it to the destination Workspace repository, so that the file tree matches the graph outline and I can share those files through git push and git pull.
