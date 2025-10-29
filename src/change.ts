import { ArraySpan } from './arrayspan.js';
import { Doc, DocLine, DocLineId } from './doc.js';
export enum Type {
    InsertBefore = 'insertBefore',
    InsertBelow = 'insertBelow',
    Reinsert = 'reinsert',
    Remove = 'delete',
    TextChange = 'text',
    MoveBefore = 'moveBefore',
    MoveBelow = 'moveBelow',
    NoOp = 'noop',
}
export class NoOp {
    readonly type = Type.NoOp;
}
// export class InsertText {
//     readonly type = Type.NoOp; // Type.InsertText;

//     constructor(public readonly owner: DocLine, 
//         public readonly offset: number, 
//         public readonly lines: string[]) { }
// }
export class InsertBefore {
    readonly type = Type.InsertBefore;
    constructor(public readonly lines: DocLine[], public readonly before: DocLine) { }
}
export function makeInsertBefore(before: DocLine, lines: DocLine[]): InsertBefore {
    return new InsertBefore(lines, before);
}
export class MoveBefore {
    readonly type = Type.MoveBefore;
    constructor(public readonly line: DocLine, public readonly targetBefore: DocLine) { }
}
export class MoveBelow {
    readonly type = Type.MoveBelow;
    constructor(public readonly line: DocLine, public readonly targetBelow: DocLine) { }
}
export class InsertBelow {
    readonly type = Type.InsertBelow;
    constructor(public readonly lines: DocLine[], public readonly above: DocLine) { }
}
export function makeInsertBelow(above: DocLine, lines: DocLine[]): InsertBelow {
    return new InsertBelow(lines, above);
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

export class Remove {
    readonly type = Type.Remove;
    constructor(public readonly lines: DocLine[]) { }
}
export function makeRemove(lines: DocLine[]): Remove {
    return new Remove(lines);
}
export class TextChange {
    readonly type = Type.TextChange;
    
    constructor(public readonly line: DocLine, 
        public readonly newText: string) { }
}
export function makeTextChange(line: DocLine, position: number,  length: number, newText: string): TextChange {
    const before = line.content.substring(0, position);
    const after = line.content.substring(position + length);
    const resultText = before + newText + after;
    return new TextChange(line, resultText);
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

export type Change = InsertBefore | InsertBelow | Reinsert | Remove | TextChange 
    | MoveBelow | MoveBefore | NoOp;
