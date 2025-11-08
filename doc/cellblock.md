# CellBlock - Multi-Cell Selection

## Overview
CellBlock provides Excel-like multi-cell selection functionality. It allows selecting a rectangular region of cells across multiple rows and columns.

## Naming Convention
- **Text selection** (within a single cell): `selection`, `getSelectionRange()`, `setSelection()`
- **Multi-cell selection**: `cellBlock`, `getCellBlock()`, `setCellBlock()`, `CellBlock` type

This naming avoids confusion with existing text selection terminology.

## Architecture Context

### Site vs Scene
- **Site**: Persistent collection of the model data. Tree-structured.
- **Scene**: Filtered linear view of Site. Represents visible rows in the editor.
- Selection happens in the **Scene**, but may implicitly include rows not currently in the Scene.

### Tree Structure Implications
- Site is a tree structure
- Any CellBlock effectively represents a contiguous array of children of one tree node
- The block **implicitly contains all descendants** of those children
- Scene is a linear (flattened) representation of a subset of that tree

### Cell Structure
- Each tree node (SiteRow) converts to a SceneRow
- Each SceneRow's content is split into multiple SceneCell objects (cells)
- Cells are organized horizontally within a row

## CellBlock Definition

A CellBlock is defined by two axes:

### Vertical Axis (Rows)
- Contiguous array of children of one tree node
- Includes all descendants of those children (implicitly)
- Represented as SceneRows in the Scene

### Horizontal Axis (Columns)
- Range of cells: `(startCellIndex, endCellIndex)`
- Inclusive range across all selected rows
- Cell indices refer to positions within each row's cell array

## Implementation

### Marking Cells
1. **Mark every cell** in Scene that matches the selected CellBlock criteria
2. **Mark one special cell** as "active" (the primary focus cell)

### Visual Representation
- In the Editor, every marked SceneCell produces a CellElement with:
  - CSS class for highlight color (standard selected cells)
  - Different CSS class for the active cell (different highlight color)

### State Management
- CellBlock state is maintained at the **Site level** (persistent)
- Scene queries Site to determine which cells are selected
- Editor layer applies visual styling based on Scene state
- Active cell is tracked separately from the block selection

## Proposed Types and Properties

### CellBlock Type (new file: `src/cellblock.ts`)

```typescript
export class CellBlock {
    // Parent SiteRow whose children define the vertical axis
    public readonly parentSiteRow: SiteRow;
    
    // Vertical axis: contiguous range of child indices
    // Inclusive: [startChildIndex, endChildIndex]

    public readonly startChildIndex: number;
    public readonly endChildIndex: number;
    
    // Horizontal axis: column index range within each row
    // Inclusive: [startColumnIndex, endColumnIndex]
    // Note: Cell index 0 is the first cell (typically an indentation cell).
    // Cell indices are non-negative, starting from 0.

    private readonly _startCellIndex: number;
    private readonly _endCellIndex: number; // if -1, acts as infinity: includes all columns
    // in each row, and if any row has more columns, those are included too.
    
    // Getters for column range
    public get startColumnIndex(): number;
    public get endColumnIndex(): number; // -1 means "all columns" (infinity - adapts to max columns across rows)
    
    // Active cell: the focused cell within the block
    // Specified by SiteRow (which row) and cell index
    public readonly activeSiteRow: SiteRow;
    public readonly activeCellIndex: number;
    
    public constructor(
        parentSiteRow: SiteRow,
        startChildIndex: number,
        endChildIndex: number,
        startColumnIndex: number,
        endColumnIndex: number, // -1 for all columns
        activeSiteRow: SiteRow,
        activeCellIndex: number
    ) {
        // Validation and assignment
    }
    
    // Check if a SiteRow is part of this block (including descendants)
    public includesSiteRow(siteRow: SiteRow): boolean;
    
    // Check if a specific cell (row + cell index) is in this block
    public includesCell(siteRow: SiteRow, cellIndex: number): boolean;
    
    // Check if a cell is the active cell
    public isActiveCell(siteRow: SiteRow, cellIndex: number): boolean;
}
```

### Site Properties (add to `src/site.ts`)
```typescript
export class Site {
    // ... existing properties ...
    
    // Current CellBlock selection (null if no selection)
    // Stored at Site level for persistence
    private _cellBlock: CellBlock | null = null;
    
    public get cellBlock(): CellBlock | null {
        return this._cellBlock;
    }
    
    // Set the CellBlock selection
    public setCellBlock(block: CellBlock | null): void;
    
    // Clear the CellBlock selection
    public clearCellBlock(): void {
        this._cellBlock = null;
    }
}
```

### Scene Properties (add to `src/scene.ts`)

```typescript
export class Scene {
    // Find all SceneRows that are part of the current CellBlock
    public getSelectedSceneRows(): readonly SceneRow[];
    public updatedSelection() {
        // called by controller after updating site selection. calls editor updates.
    }
}
```

### SceneRow Properties (add to `src/scene.ts`)

```typescript
export class SceneRow {
    // ... existing properties ...
    
    // Check if this SceneRow is part of the current CellBlock
    public isInCellBlock(): boolean;
    
    // Check if a specific cell index in this row is selected
    public isCellSelected(cellIndex: number): boolean;
    
    // Check if a specific cell index is the active cell
    public isCellActive(cellIndex: number): boolean;
    
    // Get the cell selection state for this row
    // Returns: { selected: boolean, active: boolean } for each cell index
    public getCellSelectionStates(): readonly CellSelectionState[];
}

export type CellSelectionState = {
    cellIndex: number;
    selected: boolean;
    active: boolean;
};
```

### Editor/Row/Cell Properties (add to `src/editor.ts`)

```typescript
// CSS class constants
const CellBlockSelectedClass: string = 'cellBlock-selected';
const CellBlockActiveClass: string = 'cellBlock-active';

export class Cell {
    // ... existing properties ...
    
    // Check if this cell is part of the current CellBlock
    public isInCellBlock(): boolean;
    
    // Check if this cell is the active cell
    public isActiveCell(): boolean;
    
    // Update CSS classes based on CellBlock state
    public updateCellBlockStyling(): void;
}

export class Row {
    // ... existing properties ...
    
    // Update CSS classes for all cells in this row based on Scene CellBlock state
    public updateCellBlockStyling(): void;
    
    // Get cell index for a given Cell element
    public getCellIndex(cell: Cell): number;
}
```

### Controller Integration (add to `src/controller.ts`)

```typescript
// Methods to manage CellBlock selection
export function initCellBlockToRow(initialRow: SiteRow ): void;
// modifier functions to be defined later.
export function getCellBlock(): CellBlock | null;
export function clearCellBlock(): void;
```

## Implementation Notes

### CellBlock Creation
- CellBlock is created from:
  - Parent SiteRow (tree node)
  - Child index range (vertical selection)
  - Cell index range (horizontal selection)
  - Active cell position (SiteRow + cell index)

### Scene Integration
- Scene queries Site to get the current CellBlock
- SceneRow methods check if they're part of the block (via Site)
- `updatedSelection()` is called by controller after Site changes to update Editor
- Includes all descendants of selected children (implicitly)

### Editor Integration
- Editor applies CSS classes based on Scene state
- `Scene.updatedSelection()` triggers Editor updates when:
  - CellBlock changes in Site
  - Scene rows are added/removed
  - Rows are updated (cell count changes)

### CSS Classes
- `.cellBlock-selected` - applied to all selected cells
- `.cellBlock-active` - applied to the active cell (in addition to selected)

## Notes
- CellBlock selection is independent of text selection within cells
- A cell can be part of a CellBlock while also having text selected within it
- The active cell is the one that receives keyboard input focus
- CellBlock may span rows that are not currently visible in Scene (folded descendants)
- Cell indexing: index 0 is the first cell (typically an indentation cell); indices are non-negative starting from 0
- Column range of -1 means "all columns" (acts as infinity - includes all columns in each row, and if any row has more columns, those are included too)
- CellBlock state persists in Site (model layer), Scene queries Site for current state

