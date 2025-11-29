import { RowCell, SiteRow } from './site.js';
import { SceneCell } from './sitecells.js';

export class CellSpec {
    public constructor(public readonly row: SiteRow, public readonly cellIndex: number) {
    }
}
export class CellBlock {
    private static _empty: CellBlock | null = null;
    public rows(): readonly SiteRow[] {
        return this.parentSiteRow.children.slice(this.startRowIndex, this.endRowIndex + 1);
    }
    public get activeRowCell() : RowCell {
        return new RowCell(this.focusRow, this.focusRow.cells.at(this.focusCellIndex));
    }
    public static get empty(): CellBlock {
        if (CellBlock._empty === null) {
            CellBlock._empty = CellBlock.create(SiteRow.end, 0, -1);
        }
        return CellBlock._empty;
    }
    
    public get focusRow() : SiteRow {
        return this.parentSiteRow.children.at(this.focusRowIndex) ?? SiteRow.end;
    }
    public get anchorRow() : SiteRow {
        return this.parentSiteRow.children.at(this.anchorRowIndex) ?? SiteRow.end;
    }
    public get focusCell() : RowCell {
        return new RowCell(this.focusRow, this.focusRow.cells.at(this.focusCellIndex));
    }
    public get anchorCell() : RowCell {
        return new RowCell(this.anchorRow, this.anchorRow.cells.at(this.anchorCellIndex));
    }

    public get startRowIndex() : number {
        return this.focusRowIndex < this.anchorRowIndex ? this.focusRowIndex : this.anchorRowIndex;
    }
    public get endRowIndex() : number {
        return this.focusRowIndex > this.anchorRowIndex ? this.focusRowIndex : this.anchorRowIndex;
    }
    public get startCellIndex() : number {
        if (this.anchorCellIndex === -1) {
            return this.focusCellIndex;
        }
        if (this.focusCellIndex === -1) {
            return this.anchorCellIndex;
        }
        return Math.min(this.focusCellIndex, this.anchorCellIndex);
    }
    public get endCellIndex() : number {
        if (this.focusCellIndex === -1) {
            return this.focusCellIndex;
        }
        if (this.anchorCellIndex === -1) {
            return this.anchorCellIndex;
        }
        return Math.max(this.focusCellIndex, this.anchorCellIndex);
    }
    public constructor(
        public readonly parentSiteRow: SiteRow,
        public readonly focusRowIndex: number,
        public readonly anchorRowIndex: number,
        public readonly focusCellIndex: number,
        public readonly anchorCellIndex: number) {
    }
    public static create(
        parentSiteRow: SiteRow,
        focusRowIndex: number,
        anchorRowIndex: number,
    ): CellBlock {
        return new CellBlock(parentSiteRow, focusRowIndex, anchorRowIndex, 0, -1);
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
                if (childIndex >= this.startRowIndex && childIndex <= this.endRowIndex) {
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
        if (cellIndex < this.startCellIndex) {
            return false;
        }
        if (this.endCellIndex === -1) {
            return true;
        }

        return cellIndex <= this.endCellIndex;
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
        if (siteRow === this.focusRow) {
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
