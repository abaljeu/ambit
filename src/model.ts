// Line ID generator - private to module
let nextLineId = 10;

function generateLineId(): string {
    const id = nextLineId++;
    // Convert to base-36 (0-9, a-z) and pad to 6 characters
    return id.toString(36).padStart(6, '0');
}

// Private documents array - only accessible through Model
const documents: Doc[] = [];

// Model module - single export that encapsulates all document operations
export class Doc {
        constructor(public readonly path: string, public readonly original_text: string) {
            this._setLines(original_text);
        }
        private _lines: Line[] = [];
        
        // Public getter - read-only access to lines
        get lines(): readonly Line[] {
            return this._lines;
        }
        
    public CurrentText(): string {
        return this._lines.map(line => line.content).join("\n");
    }

    // Start-of-document implicit line
    public End(): Line { return endLineSingleton; }

    // Never returns null; returns the implicit start line if not found
    public getLineById(id: string): Line {
            const found = this._lines.find(l => l.id === id);
        return found ?? endLineSingleton
    }
    public deleteLine(id: string): void {
        this._lines.splice(this._lines.findIndex(l => l.id === id), 1);
    }
    findIndexByLineId(id: string): number {
        let i = this._lines.findIndex(l => l.id === id);
        if (i === -1) return this._lines.length;
        return i;
    }

    insertBefore(lineId: string, content: string): Line {
        const idx = this.findIndexByLineId(lineId);
        const newLine = new Line(content);
        // 0 <= idx <= this._rows.length
        this._lines.splice(idx, 0, newLine);
        return newLine;
    }

    // Replace a line's content preserving its id; returns the new Line object
    updateLineContent(lineId: string, content: string): void {
        const line = this.getLineById(lineId);
        line.updateContent(content);
    }
    
    // Private method - only used by Model module
    _setLines(content: string): void {
        this._lines = content.split("\n").map(line => new Line(line));
    }
}

export class Line {
    public readonly id: string;
    constructor(public content: string, idOverride?: string) 
    {
        this.id = idOverride ?? generateLineId();
    }
    public updateContent(content: string) {
        this.content = content;
    }
}

const END_LINE_ID: string = '000000';
const endLineSingleton = new Line('', END_LINE_ID);

export function getAllDocuments(): readonly Doc[] {
    return documents;
}

export function findDoc(path: string): Doc {
    return documents.find(doc => doc.path === path) ?? createNoDoc();
}

//  Create a transient NoDoc (not stored)
export function createNoDoc(): Doc {
    return new Doc("", "");
}

// Get a document or a transient NoDoc if not found (never returns null)
export function getDoc(path: string): Doc {
    return findDoc(path) ?? createNoDoc();
}

// Never returns null; returns the implicit start line if doc/line not found
export function getLine(path: string, id: string): Line {
    const doc = getDoc(path);
    return doc.getLineById(id);
}

// Add or update a document
export function addOrUpdateDoc(path: string, content: string): Doc {
    const existingDoc = findDoc(path);
    if (existingDoc) {
        existingDoc._setLines(content);
        return existingDoc;
    } else {
        const newDoc = new Doc(path, content);
        
        documents.push(newDoc);
        return newDoc;
    }
}

// Remove a document
export function removeDoc(path: string): boolean {
    const index = documents.findIndex(doc => doc.path === path);
    if (index !== -1) {
        documents.splice(index, 1);
        return true;
    }
    return false;
}

// Get document count (useful for debugging)
export function getDocumentCount(): number {
    return documents.length;
}

export function updateLineContent(line : Line, content: string): void {
    line.updateContent(content);
}

// Update the content of a document by path
export function updateDocContent(path: string, content: string): boolean {
    const doc = findDoc(path);
    if (doc) {
        doc._setLines(content);
        return true;
    }
    return false;
}