import { computePosition, flip, shift, offset } from '@floating-ui/dom';
import { HydratedIssue } from '../../shared/contracts';

const TooltipConfig = {
  Offset: 6,
  ShiftPadding: 10
} as const;

const TooltipClasses = {
  Container: 'pero-tooltip',
  Header: 'pero-tooltip__header',
  Brand: 'pero-tooltip__brand',
  InfoBtn: 'pero-tooltip__info-btn',
  InfoBtnActive: 'pero-tooltip__info-btn--active',
  Title: 'pero-tooltip__title',
  Reason: 'pero-tooltip__reason',
  ReasonVisible: 'pero-tooltip__reason--visible',
  Suggestions: 'pero-tooltip__suggestions',
  SuggestionPill: 'pero-tooltip__suggestion'
} as const;

export class Tooltip {
  private static instance: Tooltip;
  
  private readonly element: HTMLElement;
  private onFixCallback: ((val: string) => void) | null = null;
  private currentTarget: HTMLElement | null = null;
  private activeContextElement: HTMLElement | null = null;
  private activeErrorId: string | null = null;

  private constructor() {
    this.element = this.createContainer();
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
    error: HydratedIssue, 
    uniqueId: string, 
    onFix: (val: string) => void
  ): void {
    if (this.isVisible() && this.activeErrorId === uniqueId) return;

    this.activeErrorId = uniqueId;
    this.currentTarget = target;
    this.activeContextElement = contextElement;
    this.onFixCallback = onFix;
    
    this.render(error);
    this.element.style.display = 'flex';
    this.updatePosition();
  }

  hide(): void {
    this.element.style.display = 'none';
    this.activeErrorId = null;
    this.currentTarget = null;
    this.activeContextElement = null;
    this.onFixCallback = null;
  }

  private createContainer(): HTMLElement {
    const el = document.createElement('div');
    el.className = TooltipClasses.Container;
    el.style.display = 'none';
    document.body.appendChild(el);
    return el;
  }

  private isVisible(): boolean {
    return this.element.style.display !== 'none';
  }

  private handleOutsideClick = (event: MouseEvent): void => {
    const targetNode = event.target as Node;
    if (this.element.contains(targetNode)) return;
    if (this.activeContextElement?.contains(targetNode)) return;
    if (this.isVisible()) this.hide();
  };

  private render(error: HydratedIssue): void {
    this.element.innerHTML = '';

    const reasonBox = this.createReasonBox(error.description);
    const header = this.createHeader(reasonBox);
    const title = this.createTitle(error.title);
    
    this.element.appendChild(header);
    this.element.appendChild(title);
    this.element.appendChild(reasonBox);

    if (error.suggestions.length > 0) {
      const suggestionsList = this.createSuggestionsList(error.suggestions);
      this.element.appendChild(suggestionsList);
    }
  }

  private createHeader(reasonBox: HTMLElement): HTMLElement {
    const header = document.createElement('div');
    header.className = TooltipClasses.Header;

    const brand = document.createElement('div');
    brand.className = TooltipClasses.Brand;
    brand.textContent = 'Pero 🪶';

    const infoBtn = document.createElement('div');
    infoBtn.className = TooltipClasses.InfoBtn;
    infoBtn.textContent = 'i';
    
    infoBtn.onclick = (event) => {
      event.stopPropagation();
      const isVisible = reasonBox.classList.toggle(TooltipClasses.ReasonVisible);
      infoBtn.classList.toggle(TooltipClasses.InfoBtnActive, isVisible);
      this.updatePosition();
    };

    header.appendChild(brand);
    header.appendChild(infoBtn);
    return header;
  }

  private createTitle(titleText: string): HTMLElement {
    const titleEl = document.createElement('div');
    titleEl.className = TooltipClasses.Title;
    titleEl.textContent = titleText;
    return titleEl;
  }

  private createReasonBox(description: string): HTMLElement {
    const reasonBox = document.createElement('div');
    reasonBox.className = TooltipClasses.Reason;
    reasonBox.textContent = description;
    return reasonBox;
  }

  private createSuggestionsList(suggestions: ReadonlyArray<string>): HTMLElement {
    const list = document.createElement('div');
    list.className = TooltipClasses.Suggestions;
    
    for (const suggestion of suggestions) {
      const pill = document.createElement('div');
      pill.className = TooltipClasses.SuggestionPill;
      pill.textContent = suggestion;
      pill.onclick = (event) => {
        event.stopPropagation();
        this.onFixCallback?.(suggestion);
        this.hide();
      };
      list.appendChild(pill);
    }
    
    return list;
  }

  private async updatePosition(): Promise<void> {
    if (!this.currentTarget) return;
    
    const { x, y } = await computePosition(this.currentTarget, this.element, {
      placement: 'top-start',
      middleware: [
        offset(TooltipConfig.Offset), 
        flip(), 
        shift({ padding: TooltipConfig.ShiftPadding })
      ]
    });
    
    Object.assign(this.element.style, { left: `${x}px`, top: `${y}px` });
  }
}