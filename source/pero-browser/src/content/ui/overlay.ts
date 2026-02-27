import { HydratedIssue } from '../../shared/contracts';
import { mirrorStyles } from '../dom/styles';

export const OverlayClasses = {
  Container: 'pero-overlay',
  Error: 'pero-overlay__error'
} as const;

export class Overlay {
  private readonly element: HTMLElement;
  private readonly target: HTMLElement;
  private readonly resizeObserver: ResizeObserver;
  private isDestroyed = false;

  constructor(target: HTMLElement) {
    this.target = target;
    this.element = this.createOverlayElement();
    
    this.resizeObserver = new ResizeObserver(() => this.refreshPosition());
    this.resizeObserver.observe(target);
    
    this.target.addEventListener('scroll', this.handleScroll);
    this.syncGeometry();
  }

  renderErrors(text: string, errors: ReadonlyArray<HydratedIssue>): void {
    if (this.isDestroyed) return;

    const fragment = document.createDocumentFragment();
    const sortedErrors = [...errors].sort((a, b) => a.start - b.start);
    let cursor = 0;
    
    for (const error of sortedErrors) {
      if (cursor < error.start) {
        fragment.appendChild(document.createTextNode(text.substring(cursor, error.start)));
      }

      const errorSpan = this.createErrorSpan(text, error);
      fragment.appendChild(errorSpan);
      cursor = error.end;
    }

    if (cursor < text.length) {
      fragment.appendChild(document.createTextNode(text.substring(cursor)));
    }

    this.element.innerHTML = '';
    this.element.appendChild(fragment);
    this.syncGeometry();
  }

  getElementByErrorId(id: string): HTMLElement | null {
    return this.element.querySelector(`[data-error-id="${id}"]`);
  }

  refreshPosition(): void {
    if (this.isDestroyed) return;
    this.syncGeometry();
  }

  destroy(): void {
    this.isDestroyed = true;
    this.resizeObserver.disconnect();
    this.target.removeEventListener('scroll', this.handleScroll);
    this.element.remove();
  }

  private createOverlayElement(): HTMLElement {
    const el = document.createElement('div');
    el.className = OverlayClasses.Container;
    document.body.appendChild(el);
    return el;
  }

  private createErrorSpan(text: string, error: HydratedIssue): HTMLSpanElement {
    const span = document.createElement('span');
    span.className = OverlayClasses.Error;
    span.dataset.errorId = `${error.start}-${error.end}`;
    span.textContent = text.substring(error.start, error.end);
    return span;
  }

  private handleScroll = (): void => {
    requestAnimationFrame(() => {
      if (this.isDestroyed) return;
      this.element.scrollTop = this.target.scrollTop;
      this.element.scrollLeft = this.target.scrollLeft;
    });
  };

  private syncGeometry(): void {
    mirrorStyles(this.target, this.element);
    this.handleScroll(); 
  }
}