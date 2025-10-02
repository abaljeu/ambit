// Document class - immutable from outside, only Model can modify
export class Document {
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
    
    // Private method - only used by Model module
    _setLines(content: string): void {
        this._lines = content.split("\n").map(line => new Line(line));
    }
}

// Immutable line class
export class Line {
    public readonly id: string;
    constructor(public readonly content: string) 
    {
        this.id = Date.now().toString();
    }
}

// Private documents array - only accessible through Model
const documents: Document[] = [];

// Model module - single export that encapsulates all document operations
export const Model = {
    // Get all documents (read-only access)
    getAllDocuments(): readonly Document[] {
        return documents;
    },
    
    // Find a document by path
    findDoc(path: string): Document | undefined {
        return documents.find(doc => doc.path === path);
    },
    
    // Add or update a document
    addOrUpdateDoc(path: string, content: string): Document {
        const existingDoc = this.findDoc(path);
        if (existingDoc) {
            existingDoc._setLines(content);
            return existingDoc;
        } else {
            const newDoc = new Document(path, content);
            
            documents.push(newDoc);
            return newDoc;
        }
    },
    
    // Remove a document
    removeDoc(path: string): boolean {
        const index = documents.findIndex(doc => doc.path === path);
        if (index !== -1) {
            documents.splice(index, 1);
            return true;
        }
        return false;
    },
    
    // Get document count (useful for debugging)
    getDocumentCount(): number {
        return documents.length;
    }
};