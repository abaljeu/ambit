export class RowId {
	private _id: string;
    private static pool: Map<string, RowId> = new Map();
// Line ID generator - private to module
    private static nextLineId : number = 10;
    private constructor(id: string) {
		this._id = id;
	}
    static fromString(id: string): RowId {
        let existing = this.pool.get(id);
        if (existing) return existing;
        const created = new RowId(id);
        this.pool.set(id, created);
        return created;
    }
    static generate(): RowId {
        const id = this.nextLineId++;
        // Convert to base-36 (0-9, a-z) and pad to 6 characters
        return RowId.fromString(id.toString(36).padStart(6, '0'));
    }
    
    // Equality comparison
    equals(other: RowId): boolean {
        return this._id === other._id;
    }
    
    // Get the string representation
    getString(): string {
        return this._id;
    }
}
export const endRowId = RowId.fromString('000000');
