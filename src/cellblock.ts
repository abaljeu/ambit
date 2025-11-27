import { RowCell, SiteRow } from './site.js';
import { SceneCell } from './sitecells.js';

export class CellSpec {
    public constructor(public readonly row: SiteRow, public readonly cellIndex: number) {
    }
}
export class CellBlock {
    private static _empty: CellBlock | null = null;
    public get activeRowCell() : RowCell {
        return new RowCell(this.focusSiteRow, this.focusSiteRow.cells.at(this.focusCellIndex));
    }
    public static get empty(): CellBlock {
        if (CellBlock._empty === null) {
            CellBlock._empty = new CellBlock(
                SiteRow.end,
                0,
                -1,
                0,
                -1,
                SiteRow.end,
                0
            );
        }
        return CellBlock._empty;
    }
    
    public readonly parentSiteRow: SiteRow;
    public readonly startChildIndex: number;
    public readonly endChildIndex: number;
    private readonly _startCellIndex: number;
    private readonly _endCellIndex: number; // if -1, acts as infinity
    public readonly focusSiteRow: SiteRow;
    public readonly focusCellIndex: number;

    public constructor(
        parentSiteRow: SiteRow,
        startRowIndex: number,
        endRowIndex: number,
        startCellIndex: number,
        endCellIndex: number, // -1 for all columns
        activeSiteRow: SiteRow,
        activeCellIndex: number
    ) {
        this.parentSiteRow = parentSiteRow;
        this.startChildIndex = startRowIndex;
        this.endChildIndex = endRowIndex;
        this._startCellIndex = startCellIndex;
        this._endCellIndex = endCellIndex;
        this.focusSiteRow = activeSiteRow;
        this.focusCellIndex = activeCellIndex;
    }
    public static create(
        parentSiteRow: SiteRow,
        focusRowIndex: number,
        anchorRowIndex: number,
    ): CellBlock {
        const startRowIndex = focusRowIndex < anchorRowIndex ? focusRowIndex : anchorRowIndex;
        const endRowIndex = focusRowIndex > anchorRowIndex ? focusRowIndex : anchorRowIndex;
        const activeRow = (focusRowIndex >= 0 && focusRowIndex < parentSiteRow.children.length)
            ? parentSiteRow.children[focusRowIndex]
            : SiteRow.end;
        return new CellBlock(parentSiteRow, startRowIndex, endRowIndex, 0, -1, activeRow, 0);
    }

    public get startColumnIndex(): number {
        return this._startCellIndex;
    }

    public get endColumnIndex(): number {
        return this._endCellIndex; // -1 means "all columns" (infinity)
    }

    // Check if a SiteRow is part of this block (including descendants)
    public includesSiteRow(siteRow: SiteRow): boolean {
        // Empty state: parentSiteRow is SiteRow.end
        if (this.parentSiteRow === SiteRow.end) {
            return false;
        }
        
        // Walk up the parent chain to find a direct child of parentSiteRow
        let current = siteRow;
        while (current !== SiteRow.end) {
            const parent = current.parent;
            
            // Check if we found a direct child of parentSiteRow
            if (parent === this.parentSiteRow) {
                const childIndex = this.parentSiteRow.children.indexOf(current);
                if (childIndex === -1) {
                    return false;
                }
                
                // Check if childIndex is in the range
                if (childIndex >= this.startChildIndex && childIndex <= this.endChildIndex) {
                    return true;
                }
                return false;
            }
            
            // Move up to the parent
            current = parent;
        }
        
        return false;
    }

    // Check if a specific cell (row + cell index) is in this block
    public includesCell(siteRow: SiteRow, cellIndex: number): boolean {
        if (this.parentSiteRow === SiteRow.end) {
            return false;
        }
        if (!this.includesSiteRow(siteRow)) {
            return false;
        }
        if (cellIndex < this._startCellIndex) {
            return false;
        }
        if (this._endCellIndex === -1) {
            return true;
        }

        return cellIndex <= this._endCellIndex;
    }

    // Check if a cell is the active cell
    public isActiveCell(siteRow: SiteRow, cellIndex: number): boolean {
        // Empty state: parentSiteRow is SiteRow.end
        if (this.parentSiteRow === SiteRow.end) {
            return false;
        }
        
        // If this is the active row, check if cellIndex matches activeCellIndex
        // activeCellIndex can represent either the cell index or child index
        // If it matches the child index, we use cell 0 as the active cell
        if (siteRow === this.focusSiteRow) {
            // Check if activeCellIndex matches the child index
            const childIndex = this.parentSiteRow.children.indexOf(siteRow);
            if (childIndex === this.focusCellIndex) {
                // activeCellIndex represents child index, so use cell 0
                return cellIndex === 0;
            }
            // Otherwise, activeCellIndex represents cell index
            return cellIndex === this.focusCellIndex;
        }
        return false;
    }
}

export class CellTextSelection {
    public readonly cellIndex: number;
    public constructor(public readonly row: SiteRow, _cellIndex: number, 
        public readonly focus: number, public readonly anchor: number) {
            if (row === SiteRow.end) {
                throw new Error('row cannot be SiteRow.end');
            }
            if (_cellIndex < 0) {
                this.cellIndex = 0;
            } else {
                this.cellIndex = _cellIndex;
            }
            if (focus < 0) {
                throw new Error('focus must be non-negative');
            }
            if (anchor < 0) {
                throw new Error('anchor must be non-negative');
            }
        }
    public get activeRowCell() : RowCell {
        return new RowCell(this.row, this.row.cells.at(this.cellIndex));
    }
}
export class NoSelection {}
export type CellSelection = CellBlock | CellTextSelection | NoSelection
