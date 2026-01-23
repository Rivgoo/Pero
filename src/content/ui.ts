import { computePosition, flip, shift, offset } from '@floating-ui/dom';
import { ValidationResult } from '../core/types';

function escapeHTML(str: string): string {
  return str.replace(/[&<>"']/g, m => ({
    '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#039;',
  }[m]!));
}

class TooltipManager {
  private el: HTMLElement;
  private onApply: (fix: string) => void;

  constructor(onApplyFix: (fix: string) => void) {
    this.onApply = onApplyFix;
    this.el = document.createElement('div');
    this.el.className = 'ukr-checker-tooltip';
    this.el.style.position = 'absolute'; 
    document.body.appendChild(this.el);

    document.addEventListener('mousedown', (e) => {
      if (this.el.style.display !== 'none' && !this.el.contains(e.target as Node)) {
        this.hide();
      }
    }, true);
  }

  async show(targetElement: HTMLElement, error: ValidationResult) {
    this.el.innerHTML = this.buildTooltipHTML(error);
    this.attachHandlers(targetElement);
    this.el.style.display = 'flex';
    await this.reposition(targetElement);
  }

  hide() {
    this.el.style.display = 'none';
  }

  private async reposition(targetElement: HTMLElement) {
    const { x, y } = await computePosition(targetElement, this.el, {
      placement: 'top-start',
      middleware: [ offset(8), flip(), shift({ padding: 10 }) ]
    });
    Object.assign(this.el.style, { left: `${x}px`, top: `${y}px` });
  }

  private buildTooltipHTML(error: ValidationResult): string {
    let suggestionsHTML = '';
    if (error.replacements?.length > 0) {
      const pills = error.replacements.map(rep => `
        <div class="suggestion-pill" data-val="${escapeHTML(rep.value)}" title="${escapeHTML(rep.description || '')}">
          ${escapeHTML(rep.value)}
        </div>
      `).join('');
      suggestionsHTML = `<div class="suggestions-list">${pills}</div>`;
    } else {
      suggestionsHTML = `<div class="no-suggestions">Немає варіантів</div>`;
    }
    return `
      <div class="tooltip-header-row">
        <div class="tooltip-info-btn" id="ukr-checker-info-toggle">i</div>
        <div class="tooltip-brand">Pero</div>
      </div>
      <div class="tooltip-reason" id="ukr-checker-reason-box">${escapeHTML(error.message)}</div>
      ${suggestionsHTML}
    `;
  }
  
  private attachHandlers(targetElement: HTMLElement) {
    const pills = this.el.querySelectorAll<HTMLElement>('.suggestion-pill');
    pills.forEach(pill => {
      pill.addEventListener('click', (e) => {
        const val = (e.currentTarget as HTMLElement).dataset.val;
        if (val) this.onApply(val);
        this.hide();
      }, { once: true });
    });

    const infoBtn = this.el.querySelector('#ukr-checker-info-toggle');
    const reasonBox = this.el.querySelector('#ukr-checker-reason-box');
    if (infoBtn && reasonBox) {
      infoBtn.addEventListener('click', async (e) => {
        e.stopPropagation();
        reasonBox.classList.toggle('visible');
        infoBtn.classList.toggle('active');
        await this.reposition(targetElement);
      });
    }
  }
}

export class HighlighterOverlay {
  private overlay: HTMLElement;
  private tooltip: TooltipManager;
  private activeError: ValidationResult | null = null;
  private errorMap: Map<string, ValidationResult> = new Map();

  constructor(
    private target: HTMLTextAreaElement | HTMLInputElement,
    private onFix: (error: ValidationResult, newText: string) => void
  ) {
    this.overlay = document.createElement('div');
    this.overlay.className = 'ukr-checker-overlay';
    document.body.appendChild(this.overlay);
    this.tooltip = new TooltipManager((fixValue) => {
      if (this.activeError) this.onFix(this.activeError, fixValue);
    });
    this.syncStyles();
    this.bindEvents();
  }

  update(text: string, errors: ValidationResult[]) {
    this.syncStyles();
    this.errorMap.clear();
    const sortedErrors = [...errors].sort((a, b) => a.range.start - b.range.start);
    let html = '';
    let cursor = 0;
    sortedErrors.forEach(error => {
      html += escapeHTML(text.substring(cursor, error.range.start));
      const errorText = text.substring(error.range.start, error.range.end);
      const errorId = `${error.range.start}-${error.range.end}`;
      this.errorMap.set(errorId, error);
      html += `<span class="ukr-checker-error" data-error-id="${errorId}">${escapeHTML(errorText)}</span>`;
      cursor = error.range.end;
    });
    html += escapeHTML(text.substring(cursor));
    this.overlay.innerHTML = html + '\u200b';
    this.syncScroll();
  }

  destroy() {
    this.overlay.remove();
    this.tooltip.hide();
  }

  private bindEvents() {
    // Listen to scroll and resize for syncing
    this.target.addEventListener('scroll', this.syncScroll);
    new ResizeObserver(this.syncStyles).observe(this.target);

    this.target.addEventListener('click', () => {
      const caretPosition = this.target.selectionStart;

      if (caretPosition === null) {
        this.tooltip.hide(); // Unsure of state, so hide tooltip
        return;
      }

      let foundError = false;

      // Check if the caret is inside any known error range
      for (const error of this.errorMap.values()) {
        if (caretPosition >= error.range.start && caretPosition <= error.range.end) {
          const errorId = `${error.range.start}-${error.range.end}`;
          const spanElement = this.overlay.querySelector<HTMLElement>(`[data-error-id="${errorId}"]`);
          
          if (spanElement) {
            this.activeError = error;
            this.tooltip.show(spanElement, error);
            foundError = true;
            break; // Stop after finding the first error
          }
        }
      }

      // If the user clicks outside any error, hide the tooltip
      if (!foundError) {
        this.tooltip.hide();
      }
    });
  }

  private syncStyles = () => {
    const rect = this.target.getBoundingClientRect();
    const style = window.getComputedStyle(this.target);
    Object.assign(this.overlay.style, {
      top: `${rect.top + window.scrollY}px`,
      left: `${rect.left + window.scrollX}px`,
      width: `${rect.width}px`,
      height: `${rect.height}px`,
    });
    const props = [
      'fontFamily', 'fontSize', 'fontWeight', 'lineHeight', 
      'padding', 'borderWidth', 'boxSizing', 'textAlign', 
      'whiteSpace', 'wordWrap', 'letterSpacing'
    ];
    props.forEach(prop => {
      this.overlay.style[prop as any] = style[prop as any];
    });
  }

  private syncScroll = () => {
    this.overlay.scrollTop = this.target.scrollTop;
    this.overlay.scrollLeft = this.target.scrollLeft;
  }
}