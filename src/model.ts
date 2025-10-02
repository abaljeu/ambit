// Line ID generator - private to module
let nextLineId = 0;

function generateLineId(): string {
    const id = nextLineId++;
    // Convert to base-36 (0-9, a-z) and pad to 6 characters
    return id.toString(36).padStart(6, '0');
}

// Private documents array - only accessible through Model
const documents: Model.Doc[] = [];

// Model module - single export that encapsulates all document operations
export namespace Model {
    // Doc class - immutable from outside, only Model can modify
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
            this.id = generateLineId();
        }
    }

    // Get all documents (read-only access)
    export function getAllDocuments(): readonly Doc[] {
        return documents;
    }
    
    // Find a document by path
    export function findDoc(path: string): Doc | undefined {
        return documents.find(doc => doc.path === path);
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
}