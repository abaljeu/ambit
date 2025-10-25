import { ArraySpan } from './arrayspan.js';
import { Doc, DocLine, DocLineId } from './doc.js';
export enum Type {
    Insert = 'insert',
    Reinsert = 'reinsert',
    Delete = 'delete',
    Text = 'text',
    Move = 'move',
    NoOp = 'noop',
}
export class NoOp {
    readonly type = Type.NoOp;
}
export class Insert {
    readonly type = Type.Insert;

    constructor(public readonly owner: DocLine, 
        public readonly offset: number, 
        public readonly lines: string[]) { }
}
export function makeInsertBefore(owner: DocLine, before: DocLine, 
        lines: string[]): Insert {
    const offset = owner.indexOrLast(before);
    
    return new Insert(owner, offset, lines);
}

export class Reinsert {
    readonly type = Type.Reinsert;

    constructor(public readonly owner: DocLineId, 
        public readonly offset: number, 
        public readonly lineIds: DocLineId[]) { }
}
export function makeReinsert(doc:Doc, ownerId: DocLineId, beforeId: DocLineId, 
        lineIds: DocLineId[]): Reinsert {
    const owner = doc.findLine(ownerId);
    const before = doc.findLine(beforeId);
    
    const offset = owner.indexOrLast(before);
    return new Reinsert(ownerId, offset, lineIds);
}

export class Delete {
    readonly type = Type.Delete;
    constructor(public readonly owner: DocLineId, 
        public readonly offset: number, 
        public readonly lines: DocLineId[]) { }
}
export function makeDelete(doc:Doc, id: DocLineId, count: number): Delete {
    const line = doc.findLine(id);
    const owner = line.parent;
    const offset = owner.indexOf(line);

    const lines = owner.children.slice(offset, offset + count);
    const lineIds = lines.map(line => line.id);
    return new Delete(owner.id, offset, lineIds);
}
export class Text {
    readonly type = Type.Text;
    
    constructor(public readonly owner: DocLineId, 
        public readonly offset: number, 
        public readonly oldText: string[],
        public readonly newText: string[]) { }
}
export function makeText(doc:Doc, id: DocLineId, newText: string[]): Text {
    const line = doc.findLine(id);
    const owner = line.parent;
    const offset = owner.indexOf(line);
    if (offset + newText.length > owner.children.length) {
        throw new RangeError(`Text change out of range`);
    }
    const lines = owner.children.slice(offset, offset + newText.length);
    const oldText:string[] = lines.map(line => line.content);
    return new Text(owner.id, offset, oldText, newText);
}
export class Move {
    readonly type = Type.Move;
    constructor(public readonly oldowner: DocLine, 
        public readonly newowner: DocLine, 
        public readonly oldoffset: number, 
        public readonly newoffset: number, 
        public readonly lines: DocLine[]) { }
}
export function makeMoveBefore(line: DocLine, count : number
        , targetOwner : DocLine
        , targetBefore : DocLine) : Move | NoOp { // ie. Change

    // const line = doc.findLine(lineId);
    // const targetParent = targetOwner.parent;
    const oldowner = line.parent;
    const oldoffset = oldowner.indexOf(line);
    // Insert BEFORE targetBefore; if it's not a direct child, append at end
    const newoffset = targetOwner.indexOrLast(targetBefore);
    if (oldoffset + count > oldowner.children.length) {
        throw new RangeError(`Move change out of range`);
    }
    const lines = oldowner.children.slice(oldoffset, oldoffset + count);
    
    // Check if moving would create a loop (moving a line into its own subtree)
    for (const lineToMove of lines) {
        if (isAncestorOf(lineToMove, targetOwner)) {
            return new NoOp();
        }
    }
    
    return new Move(oldowner, targetOwner, oldoffset, newoffset, lines);
}

function isAncestorOf(potentialAncestor: DocLine, descendant: DocLine): boolean {
    let current = descendant;
    while (current !== DocLine.end) {
        if (current === potentialAncestor) {
            return true;
        }
        current = current.parent;
    }
    return false;
}

export type Change = Insert | Reinsert | Delete | Text | Move | NoOp;