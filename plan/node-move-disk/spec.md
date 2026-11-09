# Node move and disk

Map: [[map.md]]

## 1. User stories

1. Derived-path comparison — As the Server, I want to compare each derived path before the Graph change with the derived path after the Graph change, for each change in Scope, so that the File or the Directory gets the result of that comparison.

    - The same derived path leaves that File or that Directory at that path.
    - A different derived path is the input to Relocate with git mv.
2. Relocate with git mv — As the Server, I want a different derived path to relocate that File or that Directory with `git mv`, so that the File or the Directory is at the derived path after the change.
3. File Node rename — As the Server, I want Derived-path comparison on a File Node rename, so that the File ends at its derived path after the change.
4. File Node under a different Node — As the Server, I want Derived-path comparison on an `Op.Replace` that places a File Node under a different Node, so that the File ends at its derived path after the change.
5. Directory Node move — As the Server, I want Derived-path comparison on a Directory Node move, so that the Directory ends at its derived path after the change.
6. Normal Node move — As the Server, I want Derived-path comparison on a Normal Node move that changes a derived path of an Owned File Node or an Owned Directory Node, including through other Normal Nodes, so that each of those Files and Directories ends at its derived path after the change.
7. Nested and overlapping paths — As the Server, I want one Graph change to apply Derived-path comparison to a Directory and to a File or Directory nested under that Directory, and to overlapping derived paths in that same change, so that each File and each Directory ends at its derived path after the change.
8. Never-parsed File Nodes — As the Server, I want Derived-path comparison when File Nodes that have never been parsed move under a new Directory Node, so that those Files end at their derived paths. This holds when the File Node lands as a direct child or deeper in the Body. The ticket is [01 — Moving never-parsed File Nodes under a new Directory Node leaves the files behind](plan/node-move-disk/issues/01-move-unparsed-files-under-new-directory.md).
9. Change conflict — As the Server, I want a change conflict on a node move or a disk operation to cause amendment through [ChangeAmendment](src/Shared/ChangeAmendment.fs) before the change lands, so that the change is disambiguated before it lands.

    A change conflict is one of these:

    - The destination path already exists.
    - The source and the destination disagree.

    The operation does not overwrite.

    The operation does not merge.
