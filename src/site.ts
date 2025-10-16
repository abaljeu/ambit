import { Doc, noDoc } from './doc.js';
import { DocTree } from './doctree.js';
import { Id, Pool, Poolable } from './pool.js';

// SiteRow ID - uses 'Sxxxxxx' format  
export class SiteRowId extends Id<'SiteRow'> {
    private constructor(value: string) { super(value); }

    public static deserialize(value: string): SiteRowId {
        if (!/^S[0-9A-Z]{6}$/.test(value)) {
            throw new Error('SiteRowId must be S followed by 6 chars, 0-9A-Z');
        }
        return new SiteRowId(value);
    }
}

export class SiteRow extends Poolable<'SiteRow'> {
    private _folded : boolean = false;

    // tree structure
    public readonly children: SiteRow[] = [];
    public parent: SiteRow = siteRowPool.end;
    public length: number = 1;
    // reference to the DocTree
    public readonly doctree: DocTree;
    
    public constructor(reference: DocTree, id: Id<'SiteRow'>) {
        super(id);
        this.doctree = reference;
    }
    public get folded(): boolean { return this._folded; }
    public addChild(child: SiteRow): void {
        child.parent = this;
        this.children.push(child);
        this._updateLength();
    }
    
    public removeChild(child: SiteRow): boolean {
        const index = this.children.indexOf(child);
        if (index === -1) return false;
        
        child.parent = siteRowPool.end;
        this.children.splice(index, 1);
        this._updateLength();
        return true;
    }
    
    private _updateLength(): void {
        this.length = 1 + this.children.reduce((sum, child) => sum + child.length, 0);
    }
    
    public getDepth(): number {
        return this.parent !== siteRowPool.end ? this.parent.getDepth() + 1 : 0;
    }
    
    public isLeaf(): boolean {
        return this.children.length === 0;
    }
    
    public getSiblings(): SiteRow[] {
        if (this.parent === siteRowPool.end) return [this];
        return this.parent.children;
    }
}
export const siteRowPool = new Pool<SiteRow, 'SiteRow'>(
    'S', 
    (id: Id<'SiteRow'>) => new SiteRow(DocTree.create(Doc.end), id)
);

export class Site {
    private _doc: Doc = noDoc; // The root doc
    private siteRows: SiteRow[] = [];
    private _root: SiteRow = siteRowPool.end;
    
    public setDoc(doc: Doc): void {
        this._doc = doc;
        this.siteRows = new Array(doc.lines.length);
        this.buildTree(DocTree.fromDoc(doc));
    }
    
    private buildTree(tree: DocTree | null): void {
        if (!tree) {
            this._root = siteRowPool.end;
            return;
        }
        
        // Create SiteRow for each DocLine, matching the tree structure
        this._root = this._buildTreeRecursive(tree.line);
    }
    
    private _buildTreeRecursive(docLine: any): SiteRow {
        // Create DocTree for this specific DocLine
        const docTree = DocTree.create(docLine);
        
        // Create SiteRow with reference to this DocTree
        const siteRow = new SiteRow(docTree, siteRowPool.create().id);
        
        // Recursively create children
        for (const child of docLine.children) {
            const childSiteRow = this._buildTreeRecursive(child);
            siteRow.addChild(childSiteRow);
        }
        
        return siteRow;
    }
    
    public getRoot(): SiteRow {
        return this._root;
    }
    
    public get doc(): Doc { return this._doc; }
    
    constructor() {}
}

