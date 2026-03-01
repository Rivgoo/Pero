export const CopyStyles = [
  'direction', 'fontFamily', 'fontSize', 'fontWeight', 'lineHeight', 'letterSpacing',
  'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft',
  'borderTopWidth', 'borderRightWidth', 'borderBottomWidth', 'borderLeftWidth',
  'boxSizing', 'textAlign', 'whiteSpace', 'wordWrap', 'wordBreak',
  'marginTop', 'marginRight', 'marginBottom', 'marginLeft'
] as const;

export function mirrorStyles(source: HTMLElement, target: HTMLElement): void {
  const computed = window.getComputedStyle(source);
  const rect = source.getBoundingClientRect();
  
  const topPosition = rect.top + window.scrollY;
  const leftPosition = rect.left + window.scrollX;

  const styleUpdates: Partial<CSSStyleDeclaration> = {
    position: 'absolute',
    top: `${topPosition}px`,
    left: `${leftPosition}px`,
    width: `${rect.width}px`,
    height: `${rect.height}px`,
    overflow: 'hidden',
    pointerEvents: 'none',
    color: 'transparent',
    backgroundColor: 'transparent'
  };
  
  CopyStyles.forEach(prop => {
    styleUpdates[prop] = computed[prop] as string;
  });

  Object.assign(target.style, styleUpdates);
}