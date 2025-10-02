export class Document{
    constructor(public readonly path: string, public content: string) {}
}
export class Line {
    constructor(public readonly id: string, public readonly content: string) {}
}

// Global documents array
export const documents: Document[] = [];

// Helper functions for document management
export function findDoc(path: string): Document | undefined {
    return documents.find(doc => doc.path === path);
}

export function addOrUpdateDoc(path: string, content: string): Document {
    const existingDoc = findDoc(path);
    if (existingDoc) {
        existingDoc.content = content;
        return existingDoc;
    } else {
        const newDoc = new Document(path, content);
        documents.push(newDoc);
        return newDoc;
    }
}

export function removeDoc(path: string): boolean {
    const index = documents.findIndex(doc => doc.path === path);
    if (index !== -1) {
        documents.splice(index, 1);
        return true;
    }
    return false;
}