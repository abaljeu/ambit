import { Doc, DocLine, noDoc, DocLineView } from './doc.js';
import * as Change from './change.js';
import { Id, Pool } from './pool.js';
import { CellBlock, CellSelection, CellTextSelection, NoSelection } from './cellblock.js';
import { SceneCell, SceneRowCells } from './sitecells.js';
import { PureCellSelection, PureCellKind, PureSelection, PureTextSelection, PureCell, PureRow } from './web/pureData.js';
import * as Editor from './editor.js';
import { model } from './model.js';
// SiteRow ID - uses 'Sxxxxxx' format  
export class RowCell {
    public constructor(public readonly row: SiteRow, public readonly cell: SceneCell) { }
    public get cellIndex(): number { return this.row.cells.indexOf(this.cell); }
}
export class SiteRowId extends Id<'SiteRow'> {
    public constructor(value: string) {
        if (!/^S[0-9A-Z]{6}$/.test(value)) {
            throw new Error('Invalid SiteRowId');
        }
         super(value); 
    }
}
// Not used.  but should be in this file.
export abstract class SiteRowSubscriber {
    abstract siteRowsInsertedBefore(siteRows: SiteRow[]): void;
    abstract siteRowsInsertedBelow(siteRows: SiteRow[]): void;
    abstract siteRowRemoving(): void;
    abstract siteRowTextChanged(siteRow: SiteRow): void;
    abstract siteRowCellTextChanged(siteRow: SiteRow,cellIndex: number): void;
    abstract siteRowFolded(): void;
    abstract siteRowUnfolded(): void;
}

// a doc reference is like [[Path/To/DocName]] or [[DocName#Heading]]
// but this is simply text in a DocLine.
// DocLineView is a view of a single line of a document.
// a DocRef is the element that describes the target of the reference.
// if we reference a name, it will be that file's name.
// if we reference a heading, it will be the heading's text.
export enum SiteRowType {
    DocRef = 'docref',
    DocLineView = 'doclineview',
}

export class SiteRow extends DocLineView {
    public readonly id: SiteRowId;
    private _folded : boolean = false;
    private readonly _docLine: DocLine;
    private _cells: SceneRowCells | undefined;
    public get cells(): SceneRowCells {
        if (this._cells === undefined) {
            this._cells = new SceneRowCells(this.content, this.indent);
        }
        return this._cells;
    }
    public get content(): string { return this.docLine.content; }

    public get docLine(): DocLine { return this._docLine; }
    
    public static end = new SiteRow(DocLine.end, new SiteRowId('S000000'), SiteRowType.DocRef);
    // tree structure
    public readonly children: SiteRow[] = [];
    private _parent: SiteRow = SiteRow.end; // SiteRow.end.parent gets null
    public setParent(parent: SiteRow): void {
        this._parent = parent;
        this._cells = undefined;
        this.children.forEach(child => child.setParent(this));
    }
    public get parent(): SiteRow { return this._parent ?? SiteRow.end; }

    public get indent(): number { return this.parent !== SiteRow.end ? this.parent.indent + 1 : -1; }
    public get treeLength(): number {
        return 1 + this.children.reduce((sum, child) => sum + child.treeLength, 0);
    }
    public print() : void {
        console.log(`${this.id.toString()} ${this._folded? '+': '-'}`
        + `(${this.docLine.indent}) ${this.docLine.content}`);
        for (const child of this.children) {
            child.print();
        }
    }
        
        // Check if a specific cell index in this row is selected
        public isCellSelected(cellIndex: number): boolean {
            const cellSelection = model.site.cellSelection;
            if (cellSelection instanceof CellBlock) {
                return cellSelection.includesCell(this, cellIndex);
            }
            return false;
        }
        
    public getCellSelectionStates(): readonly PureSelection[] {
        const sceneSelection : CellSelection = cellSelection();
        const states: PureSelection[] = [];
        const cellCount = this.cells.count;
            
        if (sceneSelection instanceof CellBlock) {
            for (let i = 0; i < cellCount; i++) {
                const _selected = sceneSelection.includesCell(this, i);
                const active = sceneSelection.isActiveCell(this, i);
                states.push(new PureCellSelection(this.id, i, _selected, active));
            }
        } else if (sceneSelection instanceof CellTextSelection) {
            for (let i = 0; i < cellCount; i++) {
                const thisOne : boolean = 
                    (sceneSelection.row === this)
                    && (sceneSelection.cellIndex === i);
                if (thisOne) {
                    states.push(new PureTextSelection(this.id, sceneSelection.cellIndex, sceneSelection.focus, sceneSelection.anchor));
                } else 
                    states.push(new PureCellSelection(this.id, i, false, false));
            }
        } else if (sceneSelection instanceof NoSelection) {
            for (let i = 0; i < cellCount; i++) {
                states.push(new PureCellSelection(this.id, i, false, false));
            }
        }
        return states;
    }
    
    public constructor(DocLine: DocLine, id: SiteRowId, 
            public readonly type: SiteRowType = SiteRowType.DocLineView) {
        super();
        this.id = id;
        this._docLine = DocLine;
        this.docLine.addView(this);
        this.setParent(SiteRow.end);
    }
    public get hasChildren(): boolean { return this.children.length > 0; }

    public docviewRoot() : DocLineView {
        if (this.type === SiteRowType.DocRef) return this;
        return this.parent.docviewRoot();
    }
    public toggleFold(): void {
        this._folded = !this._folded;
        if (this._folded) {
        this.siteRowFolded();
        } else {
            this.siteRowUnfolded();
        }
    }
    public siteRowFolded(): void {
        const next = this.next;
        const index = model.scene.indexOf(this);
        let nextIndex = index;
        if (next.valid) {
            nextIndex = model.scene.indexOf(next);
        } else {
            nextIndex = model.scene.indexOf(SiteRow.end);
        }
        model.scene.deleteRows(index, nextIndex-index);
    }
    public siteRowUnfolded(): void {
        const next = this.next;
        const index = model.scene.indexOf(this);
        let nextIndex = index;
        if (next.valid) {
            nextIndex = model.scene.indexOf(next);
        } else {
            nextIndex = model.scene.indexOf(SiteRow.end);
        }
        if (nextIndex == index+1) {
            model.scene.addRows(index+1, this.children);        
        }
    }
    public get previous(): SiteRow {
        const index = this.parent.children.indexOf(this);
        if (index <= 0) { 
             return SiteRow.end;
        }
        return this.parent.children[index - 1];
    }
    public get next(): SiteRow {
        const index = this.parent.children.indexOf(this);
        if (index >= this.parent.children.length - 1) {
             return SiteRow.end;
        }
        return this.parent.children[index + 1];
    }
    public docLineTextChanged(line: DocLine): void {
        if (line !== this.docLine) return;
        const oldCells = this.cells;
        this._cells = undefined;
        const newCells = this.cells;

        this.siteRowTextChanged();
    }
    public siteRowTextChanged(): void {
        const r : Editor.Row = Editor.findRow(this.id.value);

        // todo: transfer cell selection to new cells.
        // for  range (a,b) and old length L, new length N
        // new range (c,d) will have L-b - N-d and b-a = d-c.
        // (then if c<0 then c=0.)
         if (r !== Editor.endRow) {
            // Convert SceneRow to PureRow
            const cells = this.cells.toArray.map(cell => 
                new PureCell(cell.type, cell.text, cell.width)
            );
            const pureRow = new PureRow(this.id, this.indent, cells);
            r.setContent(pureRow);
         }
    }
    public docLineCellTextChanged(line: DocLine, cellIndex: number, newText: string): void {
        if (line !== this.docLine)
            return;
        this._cells = undefined;       
        this.siteRowCellTextChanged(cellIndex);
    }
    public siteRowCellTextChanged(cellIndex: number): void {
        const sceneCell = this.cells.at(cellIndex);
        if (sceneCell === undefined) return;

        const editorRow : Editor.Row = Editor.findRow(this.id.value);
        if (editorRow !== Editor.endRow) {
            const editorCell : Editor.Cell = editorRow.cellAt(cellIndex);
            const pureCell = new PureCell(sceneCell.type, sceneCell.text, sceneCell.width);
            editorCell.updateFromPureCell(pureCell);
        }
    }

    public indexOrLast(row : SiteRow): number {
        return this.children.indexOf(row) !== -1 ? this.children.indexOf(row) : this.children.length;
    }
    public addBefore(lines : DocLine[]): SiteRow[] {
        const index = this.parent.indexOrLast(this);
        const newRows = lines.map(line => siteRowPool.create((id) => new SiteRow(line, id)));
        this.parent.children.splice(index, 0, ...newRows);
        newRows.forEach(row => row.setParent(this.parent));
        return newRows;
    }
    // these lines could pre-exist or be trees
    public docLinesInsertedBefore(lines: DocLine[]): void {
        const newRows = this.addBefore(lines);
    
        const sceneParent = model.scene.search(row => row === this.parent)
        if (sceneParent === SiteRow.end) return;
        
        const selfIndex = model.scene.indexOf(this);
        model.scene.addRows(selfIndex, newRows);
    }
    public docLinesInsertedBelow(lines: DocLine[]): void {
        const newRows = lines.map(line => siteRowPool.create((id) => new SiteRow(line, id)));
        this.addChildren(newRows);

        if (this.folded) return;
        if (this  === SiteRow.end) return;
        
        const index = model.scene.indexOf(this);
        model.scene.addRows(index+this.treeLength, newRows);
    }

    public docLineMovingBefore(moving: DocLineView): void {
        // moving is a SiteRow.  need its SceneRow
        moving.docLineRemoving(); 
        (moving as SiteRow).insertBefore(this);
    }
    public insertBefore(afterRow: SiteRow): void {
        const index = afterRow.parent.indexOrLast(afterRow);
        afterRow.parent.children.splice(index, 0, this);
        this.setParent(afterRow.parent);

        const sceneParent = model.scene.search(row => row === this.parent)
        if (sceneParent === SiteRow.end) return;  
        const selfIndex = model.scene.indexOf(this);
        model.scene.addRows(selfIndex, [this]);
    }
    public insertBelow(parentRow: SiteRow): void {
        parentRow.children.push(this);
        this.setParent(parentRow);

        if (this.folded) return;
        if (this  === SiteRow.end) return;
        
        const selfIndex = model.scene.indexOf(this);
        model.scene.addRows(selfIndex+this.treeLength, [this]);
    }
    public docLineMovingBelow(moving: DocLineView): void {
        // moving is a SiteRow.  need its SceneRow
        moving.docLineRemoving(); 
        (moving as SiteRow).insertBelow(this);
    }
    public docLineRemoving(): void {
        // delete self and children from scene.
        model.scene.deleteRows(model.scene.indexOf(this), this.treeLength);
        this.parent.removeChild(this);
    }
    public docLineSplitted(docLine: DocLine): void {
        // Verify this is the DocLine we're tracking
        if (docLine !== this.docLine) return;
       
        // Update length propagates up the tree
    }
    
    public cellTextPosition(c: SceneCell): number {
        let position = 0;
        for (const cell of this.cells.toArray) {
            if (cell === c)
                return position;
            position += cell.text.length + 1;
        }
        return position;
    }        
    public get valid(): boolean { return this !== SiteRow.end; }
    public get folded(): boolean { return this._folded; }
    public addChild(child: SiteRow): void {
        child.setParent(this);
        this.children.push(child);
    }
    public addChildren(children: SiteRow[]): SiteRow[] {
        this.children.push(...children);
        children.forEach(row => row.setParent(this));
        return children;
    }
    
    public removeChild(child: SiteRow): boolean {
        const index = this.children.indexOf(child);
        if (index === -1) return false;
        
        child.setParent(SiteRow.end);
        this.children.splice(index, 1);
        return true;
    }
    
    public _subscribers: SiteRowSubscriber[] = [];
    
    public subscribe(subscriber: SiteRowSubscriber): void {
        this._subscribers.push(subscriber);
    }
    public unsubscribe(subscriber: SiteRowSubscriber): void {
        this._subscribers = this._subscribers.filter(s => s !== subscriber);
    }
    public static _buildTreeRecursive(docLine: DocLine): SiteRow {
        // Create SiteRow with reference to this DocLine
        const siteRow = siteRowPool.create((id) => new SiteRow(docLine, id));
        // Recursively create children
        for (const child of docLine.children) {
            const childSiteRow = this._buildTreeRecursive(child);
            childSiteRow.setParent(siteRow);
            siteRow.children.push(childSiteRow);
        }
        return siteRow;
    }

    public getDepth(): number {
        return this.parent !== SiteRow.end ? this.parent.getDepth() + 1 : 0;
    }
    
    public isLeaf(): boolean {
        return this.children.length === 0;
    }
    
    public getSiblings(): SiteRow[] {
        if (this.parent === SiteRow.end) return [this];
        return this.parent.children;
    }
}
export class SiteRowPool extends Pool<SiteRow, SiteRowId> {
    protected override fromString(value: string): SiteRowId {
        return new SiteRowId(value);
    }
    public get end(): SiteRow {
        return SiteRow.end;
    }
    public readonly tag: string = 'S';
}
const siteRowPool = new SiteRowPool();
    // private _doc: Doc = noDoc; // The root doc
let _root: SiteRow = SiteRow.end;
let _cellSelection: CellSelection = new NoSelection();
export function cellSelection(): CellSelection {
    return _cellSelection;
}

export class Site {


    // public get doc(): Doc { return this._doc; }
    public setDoc(doc: Doc): void {
        // this._doc = doc;
        this.buildTree(doc.root);
    }
    public get cellSelection(): CellSelection {
        return _cellSelection;
    }
    public get activeCell() : RowCell | null {
        if (this.cellSelection instanceof CellBlock) {
            return this.cellSelection.activeRowCell;
        } else if (this.cellSelection instanceof CellTextSelection) {
            return this.cellSelection.activeRowCell;
        }
        return null;
    }
    public findRow(id: SiteRowId): SiteRow {
        return siteRowPool.find(id);
    }
    public setCellBlock(block: CellBlock): void {
        _cellSelection = block;
    }
    public setSelection(selection: CellSelection): void {
        _cellSelection = selection;
    }
    public clearCellBlock(): void {
        if (_cellSelection instanceof CellBlock) {
            const row= _cellSelection.activeSiteRow;
            const cellIndex = _cellSelection.activeCellIndex;
            _cellSelection = new CellTextSelection(row, cellIndex, 0, 0);
        }
    }
    public get root(): SiteRow { return _root; }
    private buildTree(line: DocLine ): void {
        // Create SiteRow for each DocLine, matching the tree structure
        _root = SiteRow._buildTreeRecursive(line);
    }
    public testFindRowByDocLine(docLine: DocLine): SiteRow {
        return siteRowPool.search((row: SiteRow) => row.docLine === docLine);
    }
    public getRoot(): SiteRow {
        return _root;
    }
    
    constructor() {}
}

