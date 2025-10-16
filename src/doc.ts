import { Id, Pool, Poolable } from './pool.js';

// DocLine object
// DocLine ID - uses 'Dxxxxxx' format
export class DocLineId extends Id<'DocLine'> {
    private constructor(value: string) { super(value); }

    public static deserialize(value: string): DocLineId {
        if (!/^D[0-9A-Z]{6}$/.test(value)) {
            return docLinePool.end.id;
        }
        return new DocLineId(value);
    }
}
export class DocLine extends Poolable<'DocLine'> {
    private _content: string;
    private static end = new DocLine('', DocLineId.deserialize('D000000'));
    // DocTree structure
    private readonly _children: DocLine[] = [];
    private _parent: DocLine  = DocLine.end;
    private _length: number = 1;
    private _indent: number = 0;
    
    public get indent(): number { return this._indent; }
    public get length(): number { return this._length; }
    public get parent(): DocLine { return this._parent; }
    public get children(): DocLine[] { return this._children; }

    public get content(): string { return this._content; }
    public constructor(content: string, id: Id<'DocLine'>) {
        super(id);
        const { stripped, indent } = this._stripIndent(content);
        this._content = stripped;
        this._indent = indent;
    }
    public setAsRoot(name: string): DocLine {
        this._indent = -1;
        this._content = name;
        return this;
    }
    public setContent(content: string): DocLine {
        const { stripped, indent } = this._stripIndent(content);
        this._content = stripped;
        this._indent = indent;
        return this;
    }
    private _stripIndent(content: string): { stripped: string; indent: number } {
        const match = content.match(/^(\t*)/);
        const indent = match ? match[1].length : 0;
        const stripped = content.substring(indent);
        return { stripped, indent };
    }
    
    public addChild(child: DocLine): void {
        child._parent = this;
        this._children.push(child);
        this._updateLength();
    }
    
    public removeChild(child: DocLine): boolean {
        const index = this.children.indexOf(child);
        if (index === -1) return false;
        
        child._parent = docLinePool.end;
        this._children.splice(index, 1);
        this._updateLength();
        return true;
    }
    
    private _updateLength(): void {
        this._length = 1 + this._children.reduce((sum, child) => sum + child._length, 0);
    }
    
    public getDepth(): number {
        return this.parent !== docLinePool.end ? this.parent.getDepth() + 1 : 0;
    }
    
    public isLeaf(): boolean {
        return this.children.length === 0;
    }
    
    public getSiblings(): DocLine[] {
        if (this.parent === docLinePool.end) return [this];
        return this.parent.children;
    }
    public isDocReference(): boolean { return false; }
}
const docLinePool = new Pool<DocLine, 'DocLine'>(
    'D', 
    (id: DocLineId) => new DocLine('', id)
);
const docArray: Doc[] = [];

export class Doc {
    private _lines: DocLine[] = [];
    private _root: DocLine = docLinePool.end;
    
    private constructor(public readonly name: string) {}
    
    public static create(content: string, name: string): Doc {
        const doc = new Doc(name);
        doc._setLines(content, name);
        doc.buildTree();
        return doc;
    }
    public updateContent(content: string): Doc {
        this._setLines(content, this.name);
        this.buildTree();
        return this;
    }
    public get lines(): DocLine[] { return this._lines; }

    public getRoot(): DocLine {
        return this._root;
    }
    public static get  end(): DocLine {
        return docLinePool.end;
    }
    public buildTree(): void {
        if (this._lines.length === 0) {
            this._root = docLinePool.end;
            return;
        }
        
        // First line (name) is always the root
        this._root = this._lines[0];
        const stack: DocLine[] = [this._root];
        
        // All other lines are children of the root, regardless of their indent
        for (let i = 1; i < this._lines.length; i++) {
            const line = this._lines[i];
            const indent = line.indent;
            
            // Find the appropriate parent based on indentation
            while (stack.length > indent + 1) {
                stack.pop();
            }
            
            const parent = stack[indent];
            if (parent) {
                parent.addChild(line);
            }
            
            // Add to stack for potential children
            if (stack.length <= indent) {
                stack.push(line);
            } else {
                stack[indent + 1] = line;
            }
        }
    }
    
    private _setLines(content: string, name: string): void {
        const lines = content.split("\n");
        
        // First line is always the document name
        this._lines = [
            docLinePool.create().setAsRoot(name),
            ...lines.map(line => docLinePool.create().setContent(line.replace(/\r/g, '')))
        ];
    }
}
export const noDoc = Doc.create('', '');