export enum PureCellKind {
    Indent = 'indent',
    Text = 'text',
}

export class PureCell {
	public constructor(
		public readonly kind: PureCellKind,
		public readonly text: string,
		// width of cell in ems; -1 for flex, >= 0 for fixed
		public readonly width: number
	) {}

	public equals(other: PureCell): boolean {
		return this.kind === other.kind &&
			this.text === other.text &&
			this.width === other.width;
	}
}

export class PureRow {
	public constructor(
		public readonly id: string,
		public readonly indent: number,
		public readonly cells: readonly PureCell[]
	) {}

	public equals(other: PureRow): boolean {
		if (this.id !== other.id || this.indent !== other.indent || 
			this.cells.length !== other.cells.length) {
			return false;
		}
		
		for (let i = 0; i < this.cells.length; i++) {
			if (!this.cells[i].equals(other.cells[i])) {
				return false;
			}
		}
		
		return true;
	}
}

export class PureCellSelection {
	public constructor(
		public readonly rowid: string,
		public readonly cellIndex: number,
		public readonly selected: boolean,
		public readonly active: boolean
	) {}

	public equals(other: PureCellSelection): boolean {
		return this.rowid === other.rowid &&
			this.cellIndex === other.cellIndex &&
			this.selected === other.selected &&
			this.active === other.active;
	}
}

export class PureCellBlock {
	public constructor(
		public readonly activeRowid: string,
		public readonly selectedRowid: string,
		public readonly selectedCellindex: number,
		public readonly activeCellindex: number
	) {}

	public equals(other: PureCellBlock): boolean {
		return this.activeRowid === other.activeRowid &&
			this.selectedRowid === other.selectedRowid &&
			this.selectedCellindex === other.selectedCellindex &&
			this.activeCellindex === other.activeCellindex;
	}
}

export type PureSelection = PureCellSelection | PureCellBlock;
