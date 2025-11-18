import { SiteRow } from './site.js';
import { SceneRow } from './scene.js';

export class CellBlock {
    private static _empty: CellBlock | null = null;
    
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
    public readonly activeSiteRow: SiteRow;
    public readonly activeCellIndex: number;

    public constructor(
        parentSiteRow: SiteRow,
        startChildIndex: number,
        endChildIndex: number,
        startCellIndex: number,
        endCellIndex: number, // -1 for all columns
        activeSiteRow: SiteRow,
        activeCellIndex: number
    ) {
        this.parentSiteRow = parentSiteRow;
        this.startChildIndex = startChildIndex;
        this.endChildIndex = endChildIndex;
        this._startCellIndex = startCellIndex;
        this._endCellIndex = endCellIndex;
        this.activeSiteRow = activeSiteRow;
        this.activeCellIndex = activeCellIndex;
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
        if (siteRow === this.activeSiteRow) {
            // Check if activeCellIndex matches the child index
            const childIndex = this.parentSiteRow.children.indexOf(siteRow);
            if (childIndex === this.activeCellIndex) {
                // activeCellIndex represents child index, so use cell 0
                return cellIndex === 0;
            }
            // Otherwise, activeCellIndex represents cell index
            return cellIndex === this.activeCellIndex;
        }
        return false;
    }
}

export class CellTextSelection {
    public readonly cellIndex: number;
    public constructor(public readonly row: SiteRow, _cellIndex: number, 
        public readonly focus: number, public readonly anchor: number) {
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
}

export type CellSelection = CellBlock | CellTextSelection;
