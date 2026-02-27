export const CopyStyles = [
  'fontFamily', 'fontSize', 'fontWeight', 'lineHeight', 
  'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft',
  'borderTopWidth', 'borderRightWidth', 'borderBottomWidth', 'borderLeftWidth',
  'boxSizing', 'textAlign', 'whiteSpace', 'wordWrap', 'letterSpacing',
  'marginTop', 'marginRight', 'marginBottom', 'marginLeft'
] as const;

export function mirrorStyles(source: HTMLElement, target: HTMLElement): void {
  const computed = window.getComputedStyle(source);
  const rect = source.getBoundingClientRect();
  
  const topPosition = rect.top + window.scrollY;
  const leftPosition = rect.left + window.scrollX;

  const styleUpdates: Partial<CSSStyleDeclaration> = {
    top: `${topPosition}px`,
    left: `${leftPosition}px`,
    width: `${rect.width}px`,
    height: `${rect.height}px`
  };
  
  CopyStyles.forEach(prop => {
    styleUpdates[prop] = computed[prop] as string;
  });

  Object.assign(target.style, styleUpdates);
}