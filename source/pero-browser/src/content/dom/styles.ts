export const COPY_STYLES = [
  'fontFamily', 'fontSize', 'fontWeight', 'lineHeight', 
  'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft',
  'borderTopWidth', 'borderRightWidth', 'borderBottomWidth', 'borderLeftWidth',
  'boxSizing', 'textAlign', 'whiteSpace', 'wordWrap', 'letterSpacing',
  'marginTop', 'marginRight', 'marginBottom', 'marginLeft'
] as const;

export function mirrorStyles(source: HTMLElement, target: HTMLElement) {
  const computed = window.getComputedStyle(source);
  const rect = source.getBoundingClientRect();
  
  const styleUpdates: Partial<CSSStyleDeclaration> = {
    top: `${rect.top + window.scrollY}px`,
    left: `${rect.left + window.scrollX}px`,
    width: `${rect.width}px`,
    height: `${rect.height}px`
  };
  
  COPY_STYLES.forEach(prop => {
    styleUpdates[prop] = computed[prop];
  });

  Object.assign(target.style, styleUpdates);
}