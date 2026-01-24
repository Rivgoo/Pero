import { computePosition, flip, shift, offset } from '@floating-ui/dom';
import { ValidationResult } from '../../shared/types';

export class Tooltip {
  private static instance: Tooltip;
  
  private el: HTMLElement;
  private onFixCallback: ((val: string) => void) | null = null;
  private currentTarget: HTMLElement | null = null;
  private activeContextElement: HTMLElement | null = null;
  private activeErrorId: string | null = null;

  private constructor() {
    this.el = document.createElement('div');
    this.el.className = 'ukr-checker-tooltip';
    this.el.style.display = 'none';
    document.body.appendChild(this.el);
    
    document.addEventListener('mousedown', this.handleOutsideClick, true);
  }

  static getInstance(): Tooltip {
    if (!Tooltip.instance) {
      Tooltip.instance = new Tooltip();
    }
    return Tooltip.instance;
  }

  show(
    target: HTMLElement, 
    contextElement: HTMLElement, 
    error: ValidationResult, 
    uniqueId: string, 
    onFix: (val: string) => void
  ) {
    if (this.el.style.display !== 'none' && this.activeErrorId === uniqueId) {
      return;
    }

    this.activeErrorId = uniqueId;
    this.currentTarget = target;
    this.activeContextElement = contextElement;
    this.onFixCallback = onFix;
    
    this.render(error);
    this.el.style.display = 'flex';
    this.updatePosition();
  }

  hide() {
    this.el.style.display = 'none';
    this.onFixCallback = null;
    this.currentTarget = null;
    this.activeContextElement = null;
    this.activeErrorId = null;
  }

  private handleOutsideClick = (e: MouseEvent) => {
    if (this.el.contains(e.target as Node)) return;
    
    if (this.activeContextElement && this.activeContextElement.contains(e.target as Node)) {
      return;
    }
    
    if (this.el.style.display !== 'none') {
      this.hide();
    }
  };

  private render(error: ValidationResult) {
    this.el.textContent = '';

    const header = document.createElement('div');
    header.className = 'tooltip-header-row';

    const brand = document.createElement('div');
    brand.className = 'tooltip-brand';
    brand.textContent = 'Pero 🪶';

    const infoBtn = document.createElement('div');
    infoBtn.className = 'tooltip-info-btn';
    infoBtn.textContent = 'i';
    
    header.appendChild(brand);
    header.appendChild(infoBtn);
    
    this.el.appendChild(header);

    const reasonBox = document.createElement('div');
    reasonBox.className = 'tooltip-reason';
    reasonBox.textContent = error.message; 
    this.el.appendChild(reasonBox);

    infoBtn.onclick = (e) => {
      e.stopPropagation();
      const isVisible = reasonBox.classList.toggle('visible');
      infoBtn.classList.toggle('active', isVisible);
      this.updatePosition();
    };

    if (error.replacements.length > 0) {
      const list = document.createElement('div');
      list.className = 'suggestions-list';
      
      error.replacements.forEach(rep => {
        const pill = document.createElement('div');
        pill.className = 'suggestion-pill';
        pill.textContent = rep.value;
        if (rep.description) pill.title = rep.description;
        
        pill.onclick = (e) => {
          e.stopPropagation();
          if (this.onFixCallback) this.onFixCallback(rep.value);
          this.hide();
        };
        
        list.appendChild(pill);
      });
      this.el.appendChild(list);
    } else {
      const empty = document.createElement('div');
      empty.className = 'no-suggestions';
      empty.textContent = 'Немає варіантів';
      this.el.appendChild(empty);
    }
  }

  private async updatePosition() {
    if (!this.currentTarget) return;

    const { x, y } = await computePosition(this.currentTarget, this.el, {
      placement: 'top-start',
      middleware: [offset(6), flip(), shift({ padding: 10 })]
    });
    
    Object.assign(this.el.style, { left: `${x}px`, top: `${y}px` });
  }
}