# Data Equivalence

Overall this document proposes that our program takes text input files and runs them through a sequence of largely reversible transformations including filters until we arrive at our editor content.  The dependency stack will be Editor depends on VisibleScene, etc. and not the reverse, and no jumping down the line.  Most changes will be handled by constructing a replace specification such that the result is equivalent to the original with that replace operation.  And the original is equivalent to the result with the inverse operation.

## Server
- Collection of Repos
    - - stored in db
- Repo is
    - a coherent, versioned set of files
    - -stored in db 
    - Name
    - Path to Repo
    - Collection of Files
    - Sequence of Transactions on Ambit files.
- Transaction
    - Each Transaction is a collection of change records
    - TransactionId - string
    - Timestamp
    - Source
        - clientid
- Change
    - Each change applies to one ambit file.
    - Stores info for a reversible operation
        - Before + Change = After
        - After - Change = Before
    - Constraint: changes must be sequenced correctly;  The operation is not transitive.
    - A change is 
        - File
        - Line number
        - Old - array of text lines
        - New - array of text lines
- File
    - - stored in file system.  details we care about:
    - Relative Path from repo root.
    - Various types of files
        - Ambit files
            - have .amb extension.
            - text with \n delimiters
            - may embed references to other files in repo.
        - Other file types to be defined
            - in general by a pair of functions that 
                - target part of an amb file.
                - extract data from that file type into .amb
                    - in an idempotent fashion 
                    - i.e. if we extract twice the same data into an amb target, the second time is 
                - update that file from .amb
                    - in an idempotent fashion 
                    - i.e. f.apply(ambsource) == f.apply(ambsource).apply(ambsource)
                - the changes to the amb file are version tracked see below.
- Virtual version of a file
    - - conceptual; not stored, not calculated
    - the effect of applying a sequence of transactions to reverse changes to a file.

## Client

- Model
    - - not stored
    - Represents a repo.
    - Computed by loading Doc objects and applying 0 or more Transactions.
    - Version - a transaction id
    - Collection of SmartDoc at a particular version
    - Collection of Transactions
    - redoQueue Collection of tranactions
    - applyChange(Transaction) : success | fail
        - asks SmartDocs to apply Changes.
        - if any reject, undo other changes
        - if all succeed, record transaction
    - undo
        - reverse last transaction
        - put that transaction in redo stack
    - redo
        - apply top of redo stack, put into transaction list
- Doc
    - Represents one version of an ambit file.
    - - not stored
    - consists of
        - Name
        - VersionId
        - list of Line objects
    - On server, produced by
        -  reading a file
        - splitting into Line objects
        - applying a sequence of Changes
- Line
    - Represents one line of an ambit file.
    - simple Text
    - Doc?
    - Line Id?
- SmartDoc
    - has a Doc
    - has a collection of Interpreters
    - findInterpreterFor(LIne) : Interpreter
    - isHeading(Line)
- DocReferenceLine
    - is line that references another doc.
- Interpreter (multiple types)
    - Examines a portion of a document.  Validates content and changes.
    - Data Type
    - Doc
    - Target: Range of Lines or Cells
    - Status: Good / Bad / Warning
    - AllowChange(change)
    - UpdateForChange(change)
- WebClient
    - posts Model.version, Model.transactions
        - receives success, newVersion
            - updates model to transaction = [], version = newVersion
        - receive conflict
            - return 'conflicted'
    - merge()
        - until success or user cancels
            - get updates since Model.version. 
            - receive repo version, transactions
                - if no conflict, 
                    - apply transactions to model
                - if conflict
                    - compute transaction to merge with model.version
                    - apply to model
                    - call post
                    - if 'conflicted'
                        - undo and discard model merge.
                        - undo merge
        - return success or cancel
- Scene
    - - not stored
    - Represents display of a portion of a document, but including references to other documents
    - has 
        - list of SceneRow
        - list of SceneChange
    - processChangeRequest(change : SceneChange) : Transaction | Reject
        - does not apply, does not store.
    - applyChange(change : Transaction)
        - updates SceneRows
        - stores change
- SceneRow
    - - not stored
    - folded 
        - transienst property.
        - lines beneath folded line are not in Scene.
    - Represents display of a Line in a scene, as a row of a ragged table
    - has list of SceneCell 
        - construct(linetext) { split line into 1 or more celltext, and construct SceneCell }
        - SceneCell subdivision is postponed.
    - has Doc, Line
    - getCss, getDisplayContent
    - isHeaderRow
    - isDocRefRow
        - is scene row that references another doc.
        - may reference a heading of doc.
        - default folded
        - if expanded, content pulled from that other doc same an main scene.
- VisibleRows
    - SceneRows filtered to match the Editor's rows
    - maybe array of indices into SceneRows
- SceneChange
    - version
    - location : SceneRow
    - old : list of line text
        - plus fold status
    - new : list of line text
        - plus fold status
- SceneCell - postponed
    - - not stored
    - Represents display of a sub-line in a scene, as a cell of a table.
    - has
        - element type
        - attributes
        - classes
        - text content
    - construct(text)
- Editor
    - handles html id=editor element
- Row
    - handles html class="scenerow" elements
- Cell
    - handles html class="scenecell" elements
- Controller
    - handles interplay between client-side objects.
    - events from html come to controller
    - controller computes a natural editorChange object based on the event.  Then:

  ```js
  Controller.handleChange(editorChange) {
		Editor.handleChange(editorChange);
  }
  Editor.handleChange(editorChange) {
	visibleChange = Editor.proposeChange(editorChange);
	visibleChange = VisibleScene.handleChange(visibleChange);
	editorChange = computeChange(visibleChange);
	return applyChange(editorChange); // compute a new editorChange, 
  }
  VisibleScene.handleChange(visibleChange) {
	sceneChange = VisibleScene.proposeChange(visibleChange);
	sceneChange = Scene.handleChange(sceneChange);
	visbleChange = computeChange(sceneChange);
	return applyChange(visisbleChange);
  }
  Scene.handleChange(sceneChange) {
	modelChange = Scene.proposeChange(sceneChange);
	modelChange = SmartModel.handleChange(modelChange);
	sceneChange = computeChange(modelChange);
	return applyChange(sceneChange);
  }
  SmartModel.handleChange(modelChange) {
	result = send modelChange to Interpreters to allow or not.
	if (result.allow)
		return Model.applyChange(modelChange);
	else
		return new emptyModelChange(result.reason);
  }
  ```




## Response

Let me open the pseudocode in `doc/Data Equivalence.md` to review the updating scheme.

[1 tool called]

### Appraisal of the update pipeline (Editor → VisibleScene → Scene → SmartModel/Model)

Overall: The layered propose/delegate/compute/apply flow is directionally sound. It keeps ownership at the right layer, pushes validation to the model via interpreters, and lets upper layers translate/reflect the accepted change back to UI. It’s consistent with reversible, transaction-oriented changes and with the “owner applies” rule.

Strengths
- **Clear ownership**: Only Model applies model changes; Scene/VisibleScene/Editor apply their own representations. Good alignment with “whoever creates an object owns it.”
- **Validation boundary**: Interpreters gate model mutations, enabling domain constraints without leaking policy upward.
- **Reversibility-friendly**: The scheme naturally composes into transactions and supports undo/redo.
- **Top-down propose, bottom-up apply**: You only update the UI after knowing the model accepted (prevents ghost state).

Key risks and gaps
- **Ambiguous computeChange responsibilities**: “computeChange” is overloaded at each boundary. You need explicit directionality and purity guarantees, e.g. `toSceneChange`, `toVisibleChange`, `toEditorChange` as pure conversions.
- **Outcome typing**: You need a discriminated union result at each layer (Applied | Rejected | NoChange) to prevent accidental partial application when the model rejects.
- **Potential re-entrancy loops**: `applyChange` must not emit editor events that re-enter `handleChange` (or must be idempotent). Guard for reentrancy and make UI updates silent to the controller.
- **Atomicity vs. granularity**: A single user edit can map to multiple file-level model changes. The pipeline currently carries a single `modelChange`; you likely need a `Transaction` (collection of per-file changes) returned from Scene/SmartModel to preserve atomicity.
- **Addressing by line number is brittle**: Scene folding, references to other docs, and ragged tables mean you need stable identifiers (DocId, LineId, RowId, and spans) to translate changes reliably; do not rely solely on line numbers.
- **Version preconditions**: Include expected version in all change types (not just `SceneChange`) to support optimistic checks and deterministic validation.
- **Idempotency**: `applyChange` at every layer should be idempotent under the same precondition to handle redraws and guard against duplicate dispatch.
- **Naming/consistency**: Mixed variable names and typos aside, standardize interfaces and verb names per layer; keep the three function patterns strictly separated (pure propose/convert vs. apply).

Concrete recommendations
- **Type each change** using discriminated unions with stable IDs and spans:
  - `EditorChange`, `VisibleChange`, `SceneChange`, `ModelChange`, `Transaction`
  - `ChangeOutcome = { kind: 'Applied', change, version } | { kind: 'Rejected', reason } | { kind: 'NoChange' }`
- **Split conversions** for clarity and purity:
  - `toVisibleChange(editorChange)`, `toSceneChange(visibleChange)`, `toModelTransaction(sceneChange)`
  - `fromModelTransaction(txOutcome) -> SceneChangeOutcome`, etc.
- **Orchestration pattern** (only owners mutate):
  - Each layer exposes: `proposeX` (pure), `delegateX` (pure conversion down), `acceptX` (pure conversion up), `applyX` (mutates only its own state), `handleX` (orchestrates).
- **Use transactions at/under Scene**:
  - Model-level application should be `Model.applyTransaction(transaction): ChangeOutcome`.
- **Propagate rejection without partial apply**:
  - On any `Rejected`, upper layers must not apply; they return `Rejected` with a reason to the controller for UI feedback.
- **Enforce version preconditions** at each layer:
  - Each `Change` carries `expectedVersion`; `apply` verifies and rejects if mismatched.
- **Guard re-entrancy**:
  - Controller serializes `handleChange` calls; `apply*` does not trigger new external events.

Suggested tightened skeleton (illustrative)

```ts
type Outcome<T> =
  | { kind: 'Applied'; change: T; version: string }
  | { kind: 'Rejected'; reason: string }
  | { kind: 'NoChange' };

type EditorChange = { /* stable RowId/CellId spans, caret, version */ };
type VisibleChange = { /* maps to Scene rows via RowId */ };
type SceneChange = { /* DocId/LineId spans, folds, version */ };
type ModelChange = { /* per-file spans, old/new, version precond */ };
type Transaction = ReadonlyArray<ModelChange>;

interface Editor {
  proposeChange(e: EditorChange): VisibleChange; // pure
  acceptChange(v: VisibleChange): EditorChange;  // pure
  applyChange(e: EditorChange): Outcome<EditorChange>; // mutates Editor only
  handleChange(e: EditorChange): Outcome<EditorChange>;
}

interface VisibleScene {
  toSceneChange(v: VisibleChange): SceneChange; // pure
  fromSceneOutcome(o: Outcome<SceneChange>): Outcome<VisibleChange>; // pure
  applyChange(v: VisibleChange): Outcome<VisibleChange>; // mutates VisibleScene only
  handleChange(v: VisibleChange): Outcome<VisibleChange>;
}

interface Scene {
  toTransaction(s: SceneChange): Transaction; // pure
  fromTransactionOutcome(o: Outcome<Transaction>): Outcome<SceneChange>; // pure
  applyChange(s: SceneChange): Outcome<SceneChange>; // mutates Scene only
  handleChange(s: SceneChange): Outcome<SceneChange>;
}

interface SmartModel {
  validate(tx: Transaction): Outcome<Transaction>; // Interpreters gate here
}

interface Model {
  applyTransaction(tx: Transaction): Outcome<Transaction>; // mutates Model only
}

// Orchestration examples (no partial applies, pure conversions across boundaries)
function handleEditorChange(editor: Editor, visible: VisibleScene, scene: Scene, model: Model, smart: SmartModel, e: EditorChange): Outcome<EditorChange> {
  const v = editor.proposeChange(e);
  const vOutcome = visible.handleChange(v);
  if (vOutcome.kind !== 'Applied') return editor.acceptChangeOutcome?.(vOutcome) ?? { kind: vOutcome.kind, reason: vOutcome.reason };

  const s = visible.toSceneChange(vOutcome.change);
  const sOutcome = scene.handleChange(s);
  if (sOutcome.kind !== 'Applied') {
    const backV = visible.fromSceneOutcome(sOutcome);
    return editor.acceptChange(backV.kind === 'Applied' ? backV.change : e) as Outcome<EditorChange>;
  }

  const tx = scene.toTransaction(sOutcome.change);
  const gated = smart.validate(tx);
  if (gated.kind !== 'Applied') {
    const backS = scene.fromTransactionOutcome(gated);
    const backV = visible.fromSceneOutcome(backS);
    return editor.acceptChangeOutcome?.(backV) ?? { kind: 'Rejected', reason: gated.reason };
  }

  const txOutcome = model.applyTransaction(gated.change);
  if (txOutcome.kind !== 'Applied') {
    const backS = scene.fromTransactionOutcome(txOutcome);
    const backV = visible.fromSceneOutcome(backS);
    return editor.acceptChangeOutcome?.(backV) ?? { kind: 'Rejected', reason: (txOutcome as any).reason };
  }

  // Apply upward after model commit succeeds
  const sApplied = scene.applyChange(sOutcome.change);
  const vApplied = visible.applyChange(visible.fromSceneOutcome({ kind: 'Applied', change: sApplied.change, version: sApplied.version }).change);
  return editor.applyChange(editor.acceptChange(vApplied.change));
}
```

Implementation notes
- Keep `propose/convert` functions pure and total; avoid null.
- All exported types readonly; no setters; changes carry stable IDs and spans, not indices.
- `apply` verifies preconditions; if mismatched, return `Rejected`.
- Ensure idempotency: re-applying the same applied change is a no-op with the same version result.
- Maintain a single-threaded controller queue to avoid interleaving changes.

Status: I reviewed the pseudocode in `doc/Data Equivalence.md` and proposed a tighter, typed orchestration with concrete recommendations to eliminate ambiguity, prevent partial application, and align with your robustness style.

- Clarified change conversion responsibilities and outcome typing.
- Proposed transaction-carrying flow and stable ID usage.
- Outlined a re-entrancy-safe, all-or-nothing apply sequence from Model up to Editor.


## Notes

**Based on these ideas, i suppose change objects could also be a typed union, with the most common change being a nonspecific replace, but other times af more informative version could be passed?**

Yes—that’s a good fit. Use discriminated unions per layer with a canonical “replace” form, and let richer variants carry intent when available. Every specific variant must be reducible to a reversible Replace so correctness never depends on semantics.

Here’s a minimal shape you can adapt.

```ts
// Shared primitives
export type VersionId = string;

export type LineId = string; // stable, not an index
export type RowId = string;  // stable scene row id

export type Span = Readonly<{
  docId: string;
  start: LineId; // inclusive
  end: LineId;   // exclusive
}>;

export type Versioned = Readonly<{ version: VersionId }>;

//
// MODEL LAYER (file content)
//
export type ModelChange =
  | ReplaceSpan
  | InsertLines
  | DeleteLines
  | MoveBlock
  | RenameHeading
  | UpdateLink
  | NoOp;

export type ReplaceSpan = Readonly<{
  kind: 'ReplaceSpan';
  span: Span;
  old: readonly string[];
  neu: readonly string[];
}> & Versioned;

export type InsertLines = Readonly<{
  kind: 'InsertLines';
  at: Span; // zero-length span at insertion point
  lines: readonly string[];
}> & Versioned;

export type DeleteLines = Readonly<{
  kind: 'DeleteLines';
  span: Span;
  old: readonly string[];
}> & Versioned;

export type MoveBlock = Readonly<{
  kind: 'MoveBlock';
  from: Span;
  to: Span; // zero-length destination
  old: readonly string[];
}> & Versioned;

export type RenameHeading = Readonly<{
  kind: 'RenameHeading';
  target: Span;      // heading span
  oldTitle: string;
  newTitle: string;
}> & Versioned;

export type UpdateLink = Readonly<{
  kind: 'UpdateLink';
  target: Span;      // link span
  oldText: string;
  newText: string;
  oldHref: string;
  newHref: string;
}> & Versioned;

export type NoOp = Readonly<{ kind: 'NoOp' }> & Versioned;

// All specific variants must degrade to ReplaceSpan (pure, total).
export function toReplaceSpan(c: ModelChange): ReplaceSpan { /* pure mapping */ }

//
// SCENE LAYER (presentation of model, folds, inclusions)
//
export type SceneChange =
  | SceneReplace
  | ToggleFold
  | UpdateDocRef
  | NoSceneChange;

export type SceneReplace = Readonly<{
  kind: 'SceneReplace';
  // same data as ReplaceSpan but row-addressed
  rowSpan: Readonly<{ start: RowId; end: RowId }>;
  oldLines: readonly string[];
  newLines: readonly string[];
}> & Versioned;

export type ToggleFold = Readonly<{
  kind: 'ToggleFold';
  row: RowId;
  folded: boolean;
}> & Versioned;

export type UpdateDocRef = Readonly<{
  kind: 'UpdateDocRef';
  row: RowId;
  oldTarget: Readonly<{ docId: string; heading?: string }>;
  newTarget: Readonly<{ docId: string; heading?: string }>;
}> & Versioned;

export type NoSceneChange = Readonly<{ kind: 'NoSceneChange' }> & Versioned;

// Pure conversions must exist both ways (lossy allowed upward):
// - toTransaction(sceneChange) -> Transaction (array<ModelChange>)
// - fromTransactionOutcome(outcome) -> SceneChangeOutcome

//
// VISIBLE/EDITOR LAYERS (UI addressing)
//
export type VisibleChange =
  | VisibleReplace
  | CaretMove
  | SelectionSet
  | NoVisibleChange;

export type VisibleReplace = Readonly<{
  kind: 'VisibleReplace';
  rows: Readonly<{ start: RowId; end: RowId }>;
  oldText: readonly string[];
  newText: readonly string[];
}> & Versioned;

export type CaretMove = Readonly<{
  kind: 'CaretMove';
  row: RowId;
  column: number;
}> & Versioned;

export type SelectionSet = Readonly<{
  kind: 'SelectionSet';
  start: Readonly<{ row: RowId; col: number }>;
  end: Readonly<{ row: RowId; col: number }>;
}> & Versioned;

export type NoVisibleChange = Readonly<{ kind: 'NoVisibleChange' }> & Versioned;

//
// OUTCOMES
//
export type Outcome<T> =
  | Readonly<{ kind: 'Applied'; change: T; version: VersionId }>
  | Readonly<{ kind: 'Rejected'; reason: string }>
  | Readonly<{ kind: 'NoChange' }>;
```

Guidelines
- Default to Replace variants at every layer; use specific variants when they improve validation, merge, or UX.
- All specific variants must:
  - Provide a pure `toReplace` mapping (reversible with the provided payload).
  - Carry enough data for undo/redo (old/new or derivable old).
- Interpreters may prefer or require certain variants; if unknown, degrade via `toReplace` and proceed.
- Keep variants layer-appropriate: folds/doc refs in Scene; content edits in Model; caret/selection in Visible/Editor.
- Always include `version` preconditions; reject on mismatch to avoid partial applies.
- Use stable IDs (DocId, LineId, RowId) and spans; avoid indices.