import { Id, Pool, Poolable } from './pool.js';
import * as Change from './change.js';
import * as OrgViews from './orgviews.js';
// DocLine object
// DocLine ID - uses 'Dxxxxxx' format
export class DocLineId extends Id<'DocLine'> {
    public constructor(value: string) {
        if (!/^D[0-9A-Z]{6}$/.test(value)) {
            throw new Error('Invalid DocLineId');
        }
         super(value); 
    }
}

export class DocLine {
    public static end = new DocLine('', new DocLineId('D000000'));

    public readonly id: DocLineId;
    private _content: string;

    // DocTree structure
    private readonly _children: DocLine[] = [];
    private _parent: DocLine  = DocLine.end;
    private _subTreeLength: number = 1;

    // update infrastructure

    public constructor(content: string, id: DocLineId) {
        this.id = id;
        this._content = content;
    }
    public static makeRoot(id: DocLineId, name: string): DocLine {
        const root = new DocLine('', id);
        root._content = name;
        return root;
    }

    public get indent(): number { return this.parent !== DocLine.end ? this.parent.indent + 1 : -1; }
    public get subTreeLength(): number { return this._subTreeLength; }
    public get parent(): DocLine 
    { 
        if (this == DocLine.end)
            return DocLine.end;
        return this._parent; 
    }
    public setParent(parent: DocLine): void {
        if (this == DocLine.end)
            return;
        this._parent = parent;
    }
    public get children(): DocLine[] { return this._children; }
    public at(index : number) : DocLine { 
        if (index < 0 || index >= this.children.length)
            return DocLine.end;
        return this._children[index];
    }

    public get content(): string { return this._content; }
    public setContent(content: string): DocLine {
        this._content = content;
        return this;
    }
    
    public addChild(child: DocLine): DocLine {
        child._parent = this;
        this._children.push(child);
        this._updateLength();
        return child;
    }
    public addChildren(children: DocLine[]): DocLine {
        children.forEach(child => child._parent = this);
        this._children.push(...children);
        this._updateLength();
        return this;
    }
    public addBefore(lines : DocLine[]) : DocLine {
        const index = this.parent.indexOrLast(this);
        this.parent.insertChildrenAt(index, lines);
        return this;
    }
    
    public addAfter(newLine : DocLine) : DocLine {
        const index = this.parent.indexOrLast(this);
        this.parent.insertChildrenAt(index+1, [newLine]);
        return this;
    }

    private insertChildrenAt(index : number, children: DocLine[]): DocLine {
        children.forEach(child => child._parent = this);
        this._children.splice(index, 0, ...children);
        this._updateLength();
        return this;
    }
    private addChildAfter(newChild: DocLine, after: DocLine): void {
        const index = this.children.indexOf(after);
    }
    public indexOrLast(line: DocLine): number {
        return this._children.indexOf(line) !== -1 ? this._children.indexOf(line) : this.children.length;
    }
    private removeChild(child: DocLine): boolean {
        const index = this.children.indexOf(child);
        if (index === -1) return false;
        
        child._parent = DocLine.end;
        this._children.splice(index, 1);
        this._updateLength();
        return true;
    }
    public removeParent() : boolean {
        const index = this.parent.children.indexOf(this);
        if (index === -1) return false;
        
        this.parent._children.splice(index, 1);
        this.parent._updateLength();
        this._parent = DocLine.end;
        return true;
    }
    
    private _updateLength(): void {
        this._subTreeLength = 1 + this._children.reduce((sum, child) => sum + child._subTreeLength, 0);
        this.parent !== DocLine.end && this.parent._updateLength();
    }
    public isAncestorOf(descendant: DocLine): boolean {
        let current = descendant;
        while (current !== DocLine.end) {
            if (current === this) {
                return true;
            }
            current = current.parent;
        }
        return false;
    }
    public getDepth(): number {
        return this.parent !== DocLine.end ? this.parent.getDepth() + 1 : 0;
    }
    
    public isLeaf(): boolean {
        return this.children.length === 0;
    }
    
    public getSiblings(): DocLine[] {
        if (this.parent === DocLine.end) return [this];
        return this.parent.children;
    }
    public isDocReference(): boolean { return false; }

    public split(offset: number): void {
        // split the text at the offset, creating a new DocLine after this.
        // Defer ALL notifications during split to batch the operation
        const parent = this.parent;
        
        const newContent = this.content.substring(offset);
        const newDocLine = docLinePool.create((id) => new DocLine(newContent, id));
        
        this.setContent(this.content.substring(0, offset));
        this.addAfter(newDocLine)
        
       
        // Notify parent's subscribers ONCE - covers all changes
        parent.views.forEach(view => view.docLineSplitted(parent));
    }

    private _views: DocLineView[] = [];
    
    public addView(view: DocLineView): void {
        this._views.push(view);
    }
    
    public removeView(view: DocLineView): void {
        this._views = this.views.filter(s => s !== view);
    }
    public get views(): readonly DocLineView[] {
        return this._views;
    }
}
export abstract class DocLineView { 
    abstract get docLine(): DocLine;
    abstract docLineSplitted(docLine: DocLine): void;
    abstract docLinesInsertedBefore(lines: DocLine[]): void;
    abstract docLinesInsertedBelow(lines: DocLine[]): void;
    abstract docviewRoot(): DocLineView;
    abstract docLineMovingBefore(moving: DocLineView): void;
    abstract docLineMovingBelow(moving: DocLineView): void;

    abstract docLineRemoving(): void;
    abstract docLineTextChanged(line: DocLine): void;
}
export class DocLinePool extends Pool<DocLine, DocLineId> {
    protected override fromString(value: string): DocLineId {
        return new DocLineId(value);
    }
    public get end(): DocLine {
        return DocLine.end;
    }
    public readonly tag: string = 'D';

    public constructor() { super(); }
}
const docLinePool = new DocLinePool();

const docArray: Doc[] = [];

class StrippedContent {
    public readonly indent: number;
    public readonly text: string;
    
    private static _stripIndent(content: string): { stripped: string; indent: number } {
        const match = content.match(/^(\t*)/);
        const indent = match ? match[1].length : 0;
        const stripped = content.substring(indent);
        return { stripped, indent };
    }

    public constructor(content: string) {
        const a = StrippedContent._stripIndent(content);
        this.indent = a.indent;
        this.text = a.stripped;
    }
}
export class Doc {
    private _root: DocLine = DocLine.end;

    public static get  end(): DocLine {
        return DocLine.end;
    }

    public constructor(public readonly name: string) {
        this.constructRoot(name);
    }
    private constructRoot(name: string): void {
        this._root = Doc.createLine(name);
    }
    public static createLine(content: string): DocLine {
        return docLinePool.create((id) => new DocLine(content, id));
    }
    public updateContent(content: string): Doc {
        this.constructRoot(this.name);
        
        const lines = content.replace(/\r/g, '').split("\n");

        const strippedContent = lines.map(line => new StrippedContent(line));
        const {newChildren }  = Doc._buildSubTree(-1, strippedContent, 0);
        this._root.addChildren(newChildren);
        return this;
    }

    public get root(): DocLine {
        return this._root;
    }
    public get length(): number {
        return this._root.subTreeLength;
    }
    public findLine(id: DocLineId): DocLine {
        return docLinePool.find(id);
    }
    
    private static _buildSubTree(depth: number, content: StrippedContent[], contentOffset: number) 
        : {newChildren: DocLine[], newOffset: number}  {

        let newLines : DocLine[] = [];
        while (contentOffset < content.length) {
            let itemContent = content[contentOffset];
            if (itemContent.indent <= depth)
                break;
            let child = Doc.createLine(itemContent.text);
            newLines.push(child);
            const {newChildren,newOffset} = Doc._buildSubTree(itemContent.indent, content, contentOffset+1);
            contentOffset = newOffset;
            child.addChildren(newChildren);
        }
        return {newChildren: newLines, newOffset: contentOffset};
    }


    // Handler functions for each change type
    private static _handleInsertBefore(change: Change.InsertBefore) : void{
        change.before.addBefore(change.lines);
        change.before.views.forEach(view => 
            view.docLinesInsertedBefore(change.lines));
    }
    private static _handleInsertBelow(change: Change.InsertBelow) : void{
        for (const line of change.lines) {
            if (line.isAncestorOf(change.above)) {
                return;
            }
        }

        change.above.addChildren(change.lines);
        change.above.views.forEach(view => 
            view.docLinesInsertedBelow(change.lines));
    }
    // private static _handleInsert(change: Change.InsertText) : void{
    //     const strip = change.lines.map(line => new StrippedContent(line));

    //     const owner = change.owner;
    //     const {newChildren, newOffset} = Doc._buildSubTree(owner.indent, strip, 0);
    //     owner.insertChildAt(change.offset, newChildren);
    //     owner._subscribers.forEach(view => view.docLinesInserted(owner, change.offset, newChildren));
    // }

    private static _handleReinsert(change: Change.Reinsert) {
        console.log(`Reinserting ${change.lineIds.length} lines at offset ${change.offset}`);
    }

    private static _handleRemove(change: Change.Remove) {
        change.lines.forEach(line => line.views.forEach(view => 
            view.docLineRemoving()));
            change.lines.forEach(line => line.removeParent());
        }

    private static _handleTextChange(change: Change.TextChange) {
        change.line.setContent(change.newText);
        change.line.views.forEach(view => 
            view.docLineTextChanged(change.line));
        
    }
    private static _handleMoveBelow(change: Change.MoveBelow) : void{
        const orgViews = OrgViews.orgByViews(change.line.views, change.targetBelow.views);
        orgViews.forEach(view => this._moveBelow(change.line, change.targetBelow, view.a, view.b));
    }
    private static _handleMoveBefore(change: Change.MoveBefore) : void{
        const orgViews = OrgViews.orgByViews(change.line.views, change.targetBefore.views);
        orgViews.forEach(view => this._moveBefore(change.line, change.targetBefore, view.a, view.b));
    }

    private static _moveBefore(line: DocLine, targetBefore: DocLine,
            view: DocLineView|null, viewBefore: DocLineView|null) : void{
        if (view && !viewBefore) {
            view.docLineRemoving()
            line.removeParent();
            targetBefore.addBefore([line]);
        } else if (viewBefore && !view) {
            line.removeParent();
            targetBefore.addBefore([line]);
            viewBefore.docLinesInsertedBefore([line])
        } else if (view && viewBefore) {
            viewBefore.docLineMovingBefore(view);
            line.removeParent();
            targetBefore.addBefore([line]);
        }

    }

    private static _moveBelow(line: DocLine, targetBelow: DocLine,
            view: DocLineView|null, viewBelow: DocLineView|null) : void{
        if (line.isAncestorOf(targetBelow)) {
            return;
        }
        if (view && !viewBelow) {
            view.docLineRemoving()
            line.removeParent();
            targetBelow.addChildren([line]);
        } else if (viewBelow && !view) {
            line.removeParent();
            targetBelow.addChildren([line]);
            viewBelow.docLinesInsertedBelow([line])
        } else if (view && viewBelow) {
            viewBelow.docLineMovingBelow(view);
            line.removeParent();
            targetBelow.addChildren([line]);
        }
}
    public static processChange(change: Change.Change) {
        switch (change.type) {
            case Change.Type.NoOp:
                break;
            case Change.Type.InsertBefore:
                Doc._handleInsertBefore(change);
                break;
            case Change.Type.InsertBelow:
                Doc._handleInsertBelow(change);
                break;
            case Change.Type.Reinsert:
                Doc._handleReinsert(change);
                break;
            case Change.Type.Remove:
                Doc._handleRemove(change);
                break;
            case Change.Type.TextChange:
                Doc._handleTextChange(change);
                break;
            case Change.Type.MoveBefore:
                Doc._handleMoveBefore(change);
                break;
            case Change.Type.MoveBelow:
                Doc._handleMoveBelow(change);
                break;
        }
    }    


}
export const noDoc = new Doc('noDoc');