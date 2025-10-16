export function idstring(tag : string, n: number): string {
    return tag + n.toString(36).toUpperCase().padStart(6, '0');
}

// Base ID class with type branding
export class Id<T extends string> {
    private readonly _value: string;
    
    protected constructor(value: string) { this._value = value; }
    
    public static pool_make_id<T extends string>(tag: string, n: number): Id<T> {
        return new Id(idstring(tag, n));
    }
    public static fromString<T extends string>(t: string): Id<T> {
        return new Id(t);
    }
    public equals(other: Id<T>): boolean { return this._value === other._value; }
    public serialize(): string { return this._value; }
}

export class Poolable<IdType extends string> {
    constructor(public readonly id: Id<IdType>) {}
}

// Generic Pool class
export class Pool<T extends Poolable<IdType>, IdType extends string> {
    private _pool: Map<string, T> = new Map();
    protected _nextId: number = 1;
    private _end: T;
    
    constructor(
        private readonly _tag: string,
        private readonly _factory: (id: Id<IdType>) => T
    ) {
        this._end = this._factory(Id.pool_make_id(this._tag, 0));
        this._pool.set(this._end.id.serialize(), this._end);
    }
    
    public create(): T {
        const id = this.createId();
        const t = this._factory(id);
        this._add(t);
        return t;
    }

    public createId(): Id<IdType> {
        return Id.pool_make_id<IdType>(this._tag, this._nextId++);
    }
    public createIdFromString(t : string): Id<IdType> {
        return Id.fromString<IdType>(t);
    }
    private _add(t: T): void {
        this._pool.set(t.id.serialize(), t);
    }
    
    public find(id: Id<IdType>): T {
        return this._pool.get(id.serialize()) ?? this._end;
    }
    
    public remove(id: Id<IdType>): boolean {
        return this._pool.delete(id.serialize());
    }
        
    public get end(): T {
        return this._end;
    }
}
