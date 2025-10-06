export class ArraySpan<T> implements Iterable<T> {
  private readonly _array: ReadonlyArray<T>;
  private readonly _begin: number;
  private readonly _end: number;

  constructor(array: ReadonlyArray<T>, beginIndex: number = 0, endIndex?: number) {
    const resolvedEnd: number = endIndex === undefined ? array.length : endIndex;
    ArraySpan.assertValidRange(array, beginIndex, resolvedEnd);
    this._array = array;
    this._begin = beginIndex;
    this._end = resolvedEnd;
  }

  get array(): ReadonlyArray<T> { return this._array; }

  get beginIndex(): number { return this._begin; }

  get endIndex(): number { return this._end; }

  get length(): number { return this._end - this._begin; }

  isEmpty(): boolean { return this.length === 0; }

  getAt(index: number): T {
    const absoluteIndex = this._begin + index;
    if (index < 0 || absoluteIndex >= this._end) {
      throw new RangeError("ArraySpan.getAt index out of range");
    }
    return this._array[absoluteIndex] as T;
  }

  tryGetAt(index: number, fallback: T): T {
    const absoluteIndex = this._begin + index;
    if (index < 0 || absoluteIndex >= this._end) {
      return fallback;
    }
    return this._array[absoluteIndex] as T;
  }

  toArray(): T[] {
    return this._array.slice(this._begin, this._end) as T[];
  }

  createSubspan(offsetBegin: number = 0, offsetEnd?: number): ArraySpan<T> {
    const newBegin = this._begin + offsetBegin;
    const newEnd =
      offsetEnd === undefined ? this._end : this._begin + (offsetEnd as number);
    ArraySpan.assertValidRange(this._array, newBegin, newEnd);
    return new ArraySpan<T>(this._array, newBegin, newEnd);
  }

  map<U>(mapper: (value: T, index: number) => U): U[] {
    const result: U[] = [];
    let i = 0;
    for (let idx = this._begin; idx < this._end; idx += 1) {
      result.push(mapper(this._array[idx] as T, i));
      i += 1;
    }
    return result;
  }

  [Symbol.iterator](): Iterator<T> {
    let idx = this._begin;
    const end = this._end;
    const arr = this._array;
    return {
      next(): IteratorResult<T> {
        if (idx < end) {
          const value = arr[idx] as T;
          idx += 1;
          return { value, done: false };
        }
        return { value: undefined as unknown as T, done: true };
      }
    };
  }

  private static assertValidRange<TItem>(
    array: ReadonlyArray<TItem>,
    beginIndex: number,
    endIndex: number
  ): void {
    if (!Number.isInteger(beginIndex) || !Number.isInteger(endIndex)) {
      throw new RangeError("ArraySpan requires integer begin and end indexes");
    }
    if (beginIndex < 0 || endIndex < 0) {
      throw new RangeError("ArraySpan indexes must be non-negative");
    }
    if (beginIndex > endIndex) {
      throw new RangeError("ArraySpan begin must be <= end");
    }
    if (endIndex > array.length) {
      throw new RangeError("ArraySpan end exceeds array length");
    }
  }
}


