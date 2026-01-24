import { HydratedIssue } from '../../shared/contracts';
import { mirrorStyles } from '../dom/styles';

export class Overlay {
  private el: HTMLElement;
  private resizeObserver: ResizeObserver;
  private target: HTMLElement;
  private isDestroyed = false;

  constructor(target: HTMLElement) {
    this.target = target;
    
    this.el = document.createElement('div');
    this.el.className = 'pero-overlay';
    document.body.appendChild(this.el);

    this.resizeObserver = new ResizeObserver(() => {
      if (!this.isDestroyed) this.syncGeometry();
    });
    this.resizeObserver.observe(target);
    
    this.target.addEventListener('scroll', this.handleScroll);
    this.syncGeometry();
  }

  renderErrors(text: string, errors: HydratedIssue[]) {
    if (this.isDestroyed) return;

    const fragment = document.createDocumentFragment();
    let cursor = 0;
    
    errors.sort((a, b) => a.start - b.start).forEach(error => {
      if (cursor < error.start) {
        fragment.appendChild(document.createTextNode(text.substring(cursor, error.start)));
      }

      const span = document.createElement('span');
      span.className = 'pero-error';
      span.dataset.errorId = `${error.start}-${error.end}`;
      span.textContent = text.substring(error.start, error.end);
      fragment.appendChild(span);

      cursor = error.end;
    });

    if (cursor < text.length) {
      fragment.appendChild(document.createTextNode(text.substring(cursor)));
    }

    this.el.innerHTML = '';
    this.el.appendChild(fragment);
    this.syncGeometry();
  }

  getElementByErrorId(id: string): HTMLElement | null {
    return this.el.querySelector(`[data-error-id="${id}"]`);
  }

  refreshPosition() {
    if (this.isDestroyed) return;
    this.syncGeometry();
  }

  destroy() {
    this.isDestroyed = true;
    this.resizeObserver.disconnect();
    this.target.removeEventListener('scroll', this.handleScroll);
    this.el.remove();
  }

  private handleScroll = () => {
    requestAnimationFrame(() => {
      if (!this.isDestroyed) {
        this.el.scrollTop = this.target.scrollTop;
        this.el.scrollLeft = this.target.scrollLeft;
      }
    });
  };

  private syncGeometry() {
    mirrorStyles(this.target, this.el);
    this.handleScroll(); 
  }
}