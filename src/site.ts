import { Doc, DocLine, noDoc, DocLineView } from './doc.js';
import * as Change from './change.js';
import { Id, Pool } from './pool.js';

// SiteRow ID - uses 'Sxxxxxx' format  
export class SiteRowId extends Id<'SiteRow'> {
    public constructor(value: string) {
        if (!/^S[0-9A-Z]{6}$/.test(value)) {
            throw new Error('Invalid DocLineId');
        }
         super(value); 
    }
}

export abstract class SiteRowSubscriber {
    abstract siteRowsInsertedBefore(siteRows: SiteRow[]): void;
    abstract siteRowsInsertedBelow(siteRows: SiteRow[]): void;
    abstract siteRowRemoving(): void;
    abstract siteRowTextChanged(siteRow: SiteRow): void;
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
    public get docLine(): DocLine { return this._docLine; }
    
    public static end = new SiteRow(DocLine.end, new SiteRowId('S000000'), SiteRowType.DocRef);
    // tree structure
    public readonly children: SiteRow[] = [];
    private _parent: SiteRow = SiteRow.end; // SiteRow.end.parent gets null
    public setParent(parent: SiteRow): void {
        this._parent = parent;
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
        this._subscribers.forEach(subscriber => subscriber.siteRowFolded());
        } else {
            this._subscribers.forEach(subscriber => subscriber.siteRowUnfolded());
        }
    }
    public get previous(): SiteRow {
        const index = this.parent.children.indexOf(this);
        if (index <= 0) { // can expand in future
             return SiteRow.end;
        }
        return this.parent.children[index - 1];
    }
    public docLineTextChanged(line: DocLine): void {
        if (line !== this.docLine) return;
        this._subscribers.forEach(subscriber => subscriber.siteRowTextChanged(this));
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
        this._subscribers.forEach(subscriber => subscriber.siteRowsInsertedBefore( newRows));
    }
    public docLinesInsertedBelow(lines: DocLine[]): void {
        const newRows = lines.map(line => siteRowPool.create((id) => new SiteRow(line, id)));
        this.addChildren(newRows);
        this._subscribers.forEach(subscriber => subscriber.siteRowsInsertedBelow( newRows));
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
        afterRow._subscribers.forEach(subscriber => subscriber.siteRowsInsertedBefore([this]));
    }
    public insertBelow(parentRow: SiteRow): void {
        parentRow.children.push(this);
        this.setParent(parentRow);
        parentRow._subscribers.forEach(subscriber => subscriber.siteRowsInsertedBelow([this]));
    }
    public docLineMovingBelow(moving: DocLineView): void {
        // moving is a SiteRow.  need its SceneRow
        moving.docLineRemoving(); 
        (moving as SiteRow).insertBelow(this);
    }
    public docLineRemoving(): void {
        this._subscribers.forEach(subscriber => subscriber.siteRowRemoving());
        this.parent.removeChild(this);
    }
    public docLineSplitted(docLine: DocLine): void {
        // Verify this is the DocLine we're tracking
        if (docLine !== this.docLine) return;
       
        // Update length propagates up the tree
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
export class Site {
    // private _doc: Doc = noDoc; // The root doc
    private _root: SiteRow = SiteRow.end;
    
    public setDoc(doc: Doc): void {
        // this._doc = doc;
        this.buildTree(doc.root);
    }
    
    private buildTree(line: DocLine ): void {
        // Create SiteRow for each DocLine, matching the tree structure
        this._root = SiteRow._buildTreeRecursive(line);
    }
    public testFindRowByDocLine(docLine: DocLine): SiteRow {
        return siteRowPool.search((row: SiteRow) => row.docLine === docLine);
    }
    public getRoot(): SiteRow {
        return this._root;
    }
    
    // public get doc(): Doc { return this._doc; }
    
    constructor() {}
}

