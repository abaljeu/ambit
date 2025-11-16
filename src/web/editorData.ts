export enum PureCellKind {
    Indent = 'indent',
    Text = 'text',
}

export interface PureCell {
	readonly kind: PureCellKind;
	readonly text: string;
	// width of cell in ems; -1 for flex, >= 0 for fixed
	readonly width: number;
}

export interface PureRow {
	readonly id: string;
	readonly indent: number;
	readonly cells: readonly PureCell[];
}

export interface PureCellSelection {
    readonly rowid : string
	readonly cellIndex: number;
	readonly selected: boolean;
	readonly active: boolean;
}

export interface PureCellBlock{
    readonly activeRowid : string;
    readonly selectedRowid : string;
	readonly selectedCellindex: number;
	readonly activeCellindex: number;
}

export type PureSelection = PureCellSelection | PureCellBlock;


