import { SiteRow } from './site.js';
import { SceneRow } from './scene.js';

export class CellBlock {
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
        // Check if siteRow is a descendant of one of the selected children
        const parent = siteRow.parent;
        if (parent !== this.parentSiteRow) {
            return false;
        }

        const childIndex = this.parentSiteRow.children.indexOf(siteRow);
        if (childIndex === -1) {
            return false;
        }

        // Check if childIndex is in the range
        if (childIndex < this.startChildIndex || childIndex > this.endChildIndex) {
            return false;
        }

        // This row is a direct child in the range, or we need to check if it's a descendant
        // Since blocks implicitly include all descendants, any row that is a descendant
        // of a selected child is included
        return true;
    }

    // Check if a specific cell (row + cell index) is in this block
    public includesCell(siteRow: SiteRow, cellIndex: number): boolean {
        // First check if the row is included
        if (!this.includesSiteRow(siteRow)) {
            return false;
        }

        // Check horizontal range
        if (cellIndex < this._startCellIndex) {
            return false;
        }

        // If endCellIndex is -1 (infinity), always include if >= start
        if (this._endCellIndex === -1) {
            return true;
        }

        return cellIndex <= this._endCellIndex;
    }

    // Check if a cell is the active cell
    public isActiveCell(siteRow: SiteRow, cellIndex: number): boolean {
        return siteRow === this.activeSiteRow && cellIndex === this.activeCellIndex;
    }

}

