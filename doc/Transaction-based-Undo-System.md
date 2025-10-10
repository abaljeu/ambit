# Transaction-based Undo System

## Overview

Refactor the Model module to use transactions that record all document changes as reversible operations. This enables undo/redo and prepares for server synchronization.

## Key Design Decisions

- **Transaction scope**: Atomic operations (single insert/delete/update per transaction initially)
- **Recording format**: Each transaction records `{ location, oldLines, newLines }` for reversibility
- **Undo scope**: Per-document (each Doc maintains its own transaction history)
- **Server sync**: Transactions will be posted with baseline ID (future: merge handling)

## Implementation Steps

### 1. Create Operation Class in `src/operation.ts`

Add an `Operation` class that:
 
 { document : Model.Doc, lineIndex : number, oldLines : string[], newLines : string[] }

- Represents a single reversible change to a document
- Stores `location` (line ID or index), `oldLines` (array of Line objects), `newLines` (array of Line objects)
- Has methods: `reverse()` to create the inverse operation
- Simple data structure, no execution logic (Transaction handles execution)

### 2. Create Transaction Class in `src/transaction.ts`

Add a `Transaction` class that:

- Contains one or more `Operation` objects
- Has methods: `apply(doc: Doc)` to execute all operations, `reverse()` to create an undo transaction
- Tracks whether it has been applied/posted
- Initially will contain single operations, but designed to support multiple operations for future grouping

### 3. Add Transaction Management to Model

Modify `Model` class to:

- Add private `_transactions: Transaction[]` array to store history
- Add private `_undoneTransactions: Transaction[]` for redo support
- Add `createTransaction()` method that returns a new Transaction bound to this Doc.  Only Model will create transactions.
- Add `_applyTransaction(txn: Transaction)` private method
- Add public `undo()` and `redo()` methods
- Track baseline state ID for server sync (string field)

### 3. Refactor Doc Mutation Methods

Convert existing methods to use transactions:

- `insertBefore(lineId, content)` → receives a transaction, records old state (empty), new state (new line)
- `deleteLine(id)` → receives a transaction, records old line, new state (empty)
- `updateLineContent(lineId, content)` → receives a transaction, records old content, new content
- Each method should receive a transaction, do its work, and inform the transaction of the change.
- Remove direct `_lines` manipulation from these methods

### 4. Update Scene to Use Model.

Modify `src/scene.ts` to call Model methods, not Doc modifiers.  New model methods wrapping doc methods will constuct one transaction, do the operations.  Every main model operation should use a single transaction

- `updateRowData()` - already calls `Model.updateLineContent()`, should work as-is
- `splitRow()` - calls `updateRowData()` and `insertBefore()`.
- `insertBefore()` - calls `doc.insertBefore()`, should work as-is
- `deleteRow()` - calls `doc.deleteLine()`, should work as-is
- `indentRowAndChildren()` and `deindentRowAndChildren()` - call `Model.updateLineContent()`, should work as-is

### 5. Expose Undo/Redo in Controller

Add keyboard shortcuts in `src/controller.ts`:

- `Ctrl+Z` → call `doc.undo()`, refresh editor
- `Ctrl+Y` or `Ctrl+Shift+Z` → call `doc.redo()`, refresh editor
- Update `editorKeyDown()` switch statement

### 6. Add Transaction Export for Server Sync

Add methods to Doc:

- `getPendingTransactions()` → returns transactions since last baseline
- `markTransactionsPosted()` → marks transactions as posted to server
- `getBaselineId()` → returns current baseline ID
- These prepare for future server POST implementation

## Files to Modify

- `src/model.ts` - Core transaction implementation (~150 new lines)
- `src/scene.ts` - Minor updates to ensure transaction flow (minimal changes)
- `src/controller.ts` - Add undo/redo keyboard shortcuts (~20 lines)

## Testing Considerations

- Test basic undo/redo of insert, delete, update operations
- Test undo/redo with folded content
- Verify transaction history is maintained correctly
- Check that Scene and Editor stay in sync after undo/redo