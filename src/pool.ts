export function idstring(tag : string, n: number): string {
    return tag + n.toString(36).toUpperCase().padStart(6, '0');
}

// Base ID class with type branding
export class Id<T extends string> {
    public readonly value: string;
    public constructor(_value: string) {
        this.value = _value;
    }
    public create(tag: T, n : number) {
        return new Id(idstring(tag, n));
     }
    public equals(other: Id<T>): boolean {
        return this.value === other.value;
    }
    public toString(): string {
        return this.value;
    }
}

export abstract class Poolable<IdType extends Id<string>> {
    public readonly id: IdType;
    public constructor(id: IdType) {
        this.id = id;
    }
}
// Generic Pool class
export abstract class Pool<ElementType, IdType extends Id<string>> {
    public abstract readonly tag: string;
    protected abstract fromString(t : string): IdType;
    public abstract get end(): ElementType;

    private _pool: Map<string, ElementType> = new Map();
    protected _nextNumber: number = 1;
    
    public makeIdFromString(t : string): IdType {
        if (!this.validateIdString(t)) {
            throw new Error(`Invalid ID: ${t}`);
        }
        return this.fromString(t);
    }
    private validateIdString(t : string): boolean {
        // starts with tag, ends with /^[0-9A-Z]{6}$/
        return this.tag == t.substring(0, this.tag.length) &&
            /^[0-9A-Z]{6}$/.test(t.substring(this.tag.length));
    }
    public  makeIdFromNumber(n : number): IdType {
        return this.makeIdFromString(idstring(this.tag, n));
    }
    public nextIdString(): string {
        return idstring(this.tag, this._nextNumber++);
    }
    public nextId(): IdType {
        return this.fromString(this.nextIdString());
    }
    constructor() { }
    
    public create(factoryFn: (id: IdType) => ElementType): ElementType {
        const id = this.nextId();
        const element = factoryFn(id);
        this._pool.set(id.value, element);
        return element;
    }
    
    public find(id: IdType): ElementType {
        return this._pool.get(id.value) ?? this.end;
    }
    public search(predicate: (element: ElementType) => boolean): ElementType {
        return Array.from(this._pool.values()).find(predicate) ?? this.end;
    }
    
    public remove(id: IdType): boolean {
        return this._pool.delete(id.value);
    }
}
