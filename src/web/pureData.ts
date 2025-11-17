import { SceneRowId } from "../scene";

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
		public readonly id: SceneRowId,
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
		public readonly rowid: SceneRowId,
		public readonly cellIndex: number,
		public readonly selected: boolean,
		public readonly active: boolean,
	) {}

	public equals(other: PureCellSelection): boolean {
		return this.rowid === other.rowid &&
			this.cellIndex === other.cellIndex &&
			this.selected === other.selected &&
			this.active === other.active;
	}
}
export class PureTextSelection {
	public constructor(
		public readonly rowid: SceneRowId,
		public readonly cellIndex: number,
		public readonly focus: number,
		public readonly anchor: number
	) {}

	public equals(other: PureTextSelection): boolean {
		return this.rowid === other.rowid &&
			this.cellIndex === other.cellIndex &&
			this.focus === other.focus &&
			this.anchor === other.anchor;
	}
}

export type PureSelection = PureCellSelection | PureTextSelection;