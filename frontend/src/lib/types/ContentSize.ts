export enum ContentHeight {
    Max = 'h-max',
    Full = 'h-full'
}

export enum ContentWidth {
    Max = 'w-max',
    Full = 'w-full'
}

export interface ContentSize {
    height?: ContentHeight;
    width?: ContentWidth;
}
