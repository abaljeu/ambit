# Transaction-based Undo System

## Overview

Refactor the Model module to use transactions that record all document changes as reversible operations. This enables undo/redo and prepares for server synchronization.

Regarding server communication, all info and UI shown to the user should reside on the main webpage, never a popup window.

## Key Design Decisions

- **Transaction scope**: Atomic operations (single insert/delete/update per transaction initially)
- **Recording format**: Each transaction records `{ location, oldLines, newLines }` for reversibility
- **Undo scope**: Per-document (each Doc maintains its own transaction history)
- **Server sync**: Transactions will be posted with baseline ID (future: merge handling)

## Implementation Steps

### 1. Create Operation Class in `src/operation.ts`

class Operation { document : Model.Doc; lineIndex : number, oldLines : string[]; newLines : string[];  }

- Represents a single reversible change to a document
- Stores `location` (line ID or index), `oldLines` (array of text from Line objects), `newLines` 
- Has methods: `reverse()` to create the inverse operation
- Simple data structure, no execution logic (Transaction handles execution)

### 2. Create Transaction Class in `src/transaction.ts`

Add a `Transaction` class that:

- Contains one or more `Operation` objects
- currentLine: number; currentOffset:  number; state after a transaction.
- will not have fold state or selection info.

- Has methods: `apply()` to execute all operations, `reverse()` to create an undo transaction
- Tracks whether it has been applied/posted
- Initially will contain single operations, but designed to support multiple operations for future grouping

### 3. Add Transaction Management to Model

Modify the `Model` module to have
- currentLine: number; currentOffset:  number;
- will not have fold state or selection info.

- major side point: whenever controller sets editor currentRow/offset, it should inform Scene which should update model, which should put the new value into the current transaction.
- if the row/offset is implicitly set by the editor, the controller's handler should fetch the new value and inform the scene.

- Add internal `_transactions: Transaction[]` array to store history
- Add internal `_undoneTransactions: Transaction[]` for redo support
- Add internal `baselineVersion : string` ID for server sync
- Add `createTransaction()` function that returns a new Transaction bound to this Doc.  Only Model will create transactions.
- Add `_applyTransaction(tract: Transaction)` function // yes tract.
- Add public `undo()` and `redo()` methods

### 4. Refactor Doc Mutation Methods

Convert existing methods to use transactions:

- `insertBefore(lineId, content)` → receives a transaction, records old state (empty), new state (new line)
- `deleteLine(id)` → receives a transaction, records old line, new state (empty)
- `updateLineContent(lineId, content)` → receives a transaction, records old content, new content
- Each method should receive a transaction, do its work, and inform the transaction of the change.
- Remove direct `_lines` manipulation from these methods

### 5. Update Scene to Use Model functions, not Doc, when modifying

Modify `src/scene.ts` to call Model funs, not Doc modifiers.  New Model funs wrapping doc methods will construct one transaction, do the operations.  Every main model operation should use a single transaction

- `updateRowData()` - already calls `Model.updateLineContent()`, should work as-is
- `splitRow()` - calls `updateRowData()` and `insertBefore()`.
- `insertBefore()` - calls `doc.insertBefore()`, should work as-is
- `deleteRow()` - calls `doc.deleteLine()`, should work as-is
- `indentRowAndChildren()` and `deindentRowAndChildren()` - call `Model.updateLineContent()`, should work as-is

### 6. Controller

#### a. Expose Undo/Redo
Add keyboard shortcuts in `src/controller.ts`:

- `Ctrl+Z` → call `Model.undo()`, obtain the line replacements and update the editor.
- `Ctrl+Y` or `Ctrl+Shift+Z` → call `Model.redo()`, obtain the line replacements an update the editor.
- Update `editorKeyDown()` switch statement

#### b. Handler for default edits
Many edit events have default handling.
Before any bound keypress, we push the current row to the scene.
If a mouse click occurs, beforehand push the current row to the scene.
If a keypress does not result in e.preventDefault();, after 5 seconds of idle time, push the current row.  (the timer restarts if more keys happen, and stops after the row is pushed.)

This will ensure no data is forgotten in the editor; within 5 seconds the row change is in a transaction.

#### 7. Notes
Next step: [[client-server-updating]], which will get/post transactions.

Model calls create transactions to record changes to the model.  Undo/redo will apply the changes.
Error handling: transaction failure scenarios are for the next step.
Memory management: transaction history limits are for the next step.
Refresh mechanism details: What exactly "refresh editor" entails
Idle timer implementation: Certain keyboard operations start the timer, which fires at 5s.  The timer also has stop and reset.  Other event handlers may call these methods.  If unsure, do nothing.
Importing: Model imports Transaction; Transactiont imports Operation.  No other changes.
Baseline ID generation: For now, version id is const "v0".
Transaction ownership: Transactions are private to the model.
Undo/redo state management: Undo sets the cursor to the previous transaction's position, if any.  Redo to the next one's.

