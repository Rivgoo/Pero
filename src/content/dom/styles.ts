export const COPY_STYLES = [
  'fontFamily', 'fontSize', 'fontWeight', 'lineHeight', 
  'paddingTop', 'paddingRight', 'paddingBottom', 'paddingLeft',
  'borderTopWidth', 'borderRightWidth', 'borderBottomWidth', 'borderLeftWidth',
  'boxSizing', 'textAlign', 'whiteSpace', 'wordWrap', 'letterSpacing',
  'marginTop', 'marginRight', 'marginBottom', 'marginLeft'
] as const;

export function mirrorStyles(source: HTMLElement, target: HTMLElement) {
  const computed = window.getComputedStyle(source);
  
  const styleUpdates: Partial<CSSStyleDeclaration> = {};
  
  COPY_STYLES.forEach(prop => {
    styleUpdates[prop] = computed[prop];
  });

  // Critical for overlay alignment
  const rect = source.getBoundingClientRect();
  
  styleUpdates.top = `${rect.top + window.scrollY}px`;
  styleUpdates.left = `${rect.left + window.scrollX}px`;
  styleUpdates.width = `${rect.width}px`;
  styleUpdates.height = `${rect.height}px`;

  Object.assign(target.style, styleUpdates);
}