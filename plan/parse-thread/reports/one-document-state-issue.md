# One document state issue

No existing issue implements this bug. Filed [16 — One document state](../issues/16-one-document-state.md).

First heading: `# 16 — One document state`

## Survey

[Parse thread](../project.md) issues `01` through `15` were read by title and by the bodies that mention parse, Load, or document state.

1. **01 — Directory body home** — Grilling. Directory body home. Done.
2. **02 — Structure-match Directory Load** — Grilling. Structure match. Done.
3. **03 — Workspace Load after incoming files** — Grilling. Workspace Load order. Done.
4. **04 — Browser want priority versus directory reconcile** — Grilling. Want priority. Defined.
5. **05 — Directory reconcile** — Coding. Reconcile members. Coded.
6. **06 — Setting Unparsed, recursive update** — Coding. A disk-newer File Node is set Unparsed by InMsg. `Op.SetDocumentState` is named as not the writer. It does not collapse the two fields. Coded.
7. **07 — Directory Unparsed during reconcile** — Coding. Child Directory Unparsed through the same InMsg. Coded.
8. **08 — Re-enable recursive update: enqueue on MarkUnparsed** — Coding. Turn requeue back on. Defined.
9. **09 — Grill reconcile discovery depth** — Grilling. Walk depth. Done.
10. **10 — Add discoverImmediateMembers** — Coding. Immediate members. Coded.
11. **11 — Load reconciles walk one level** — Coding. One-level walk. Coded.
12. **12 — Git pull marks each received file Unparsed** — Coding. Pull marks files. Coded.
13. **13 — Git pull marks containing Directories Unparsed** — Coding. Pull marks containing Directory Nodes. Coded.
14. **14 — Directory reconcile matches disk members** — Coding. Member add and delete, and `SetDocumentState` from Unparsed to Current when the Directory Node is Unparsed. It posts Current. It does not remove `parseState`. Coded.
15. **15 — Load marks its subject Unparsed at the entry point** — Coding. Load sets its subject Unparsed and then pushes. It does not store one parse fact, and it does not fix the slashed hollow circle against `alreadyParsed`. Coded.

The project notes also name three core-refinement issues and one github-transport issue.

1. **02 — Git Load: Unparsed then Parse stack** — Load order. It is not this bug.
2. **03 — One Parse thread stack** — One stack. It is not this bug.
3. **04 — Parsed/Unparsed and Persisted/Unpersisted** — Done grilling. It locks two axes and says Current is the old name of Parsed. It does not correct two stored fields for one parse fact.
4. **20 — State axes on special nodes** — Done coding. It dual-writes the new axes and the old `DocumentState`. That ticket created the two sources. It is not the correction.

## Filed

[16 — One document state](../issues/16-one-document-state.md). Status `defined`. Type `bug-fixing`. Number 16.

[Parse thread map](../map.md) has no new Decisions-so-far line. [Parse thread](../project.md) Stage is unchanged.
