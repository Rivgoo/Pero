import { HydratedIssue } from '../../shared/contracts';
import { IssuePresenter } from '../../core/i18n/IssuePresenter';
import { Bridge } from './bridge';
import { Overlay } from '../ui/overlay';
import { Tooltip } from '../ui/tooltip';

/**
 * Manages the lifecycle of interaction with a single text input field.
 */
export class InputSession {
  private element: HTMLInputElement | HTMLTextAreaElement;
  private overlay: Overlay;
  private debounceTimer: ReturnType<typeof setTimeout> | null = null;
  private animationFrameId: number | null = null;
  private errors: HydratedIssue[] = [];

  constructor(element: HTMLInputElement | HTMLTextAreaElement) {
    this.element = element;
    this.overlay = new Overlay(element);
    this.bindEvents();
    this.runCheck(); 
  }

  private bindEvents() {
    this.element.addEventListener('input', this.onInput);
    this.element.addEventListener('scroll', this.onScroll); 
    this.element.addEventListener('click', this.onClick);
    window.addEventListener('resize', this.onWindowResize); 
  }

  destroy() {
    this.element.removeEventListener('input', this.onInput);
    this.element.removeEventListener('scroll', this.onScroll);
    this.element.removeEventListener('click', this.onClick);
    window.removeEventListener('resize', this.onWindowResize);
    
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
    if (this.animationFrameId) cancelAnimationFrame(this.animationFrameId);

    this.overlay.destroy();
    Tooltip.getInstance().hide();
  }

  private onWindowResize = () => {
    if (this.animationFrameId) return;
    this.animationFrameId = requestAnimationFrame(() => {
      this.overlay.refreshPosition();
      this.animationFrameId = null;
    });
  };

  private onInput = () => {
    Tooltip.getInstance().hide();
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
    this.debounceTimer = setTimeout(() => this.runCheck(), 500);
  };

  private onScroll = () => {
    Tooltip.getInstance().hide();
  };

  private onClick = (event: Event) => {
    const caret = this.element.selectionStart;
    if (caret === null) return;

    const error = this.errors.find(e => caret >= e.start && caret <= e.end);
    
    if (error) {
      const errorId = `${error.start}-${error.end}`;
      const targetEl = this.overlay.getElementByErrorId(errorId);
      
      if (targetEl) {
        event.stopPropagation();
        Tooltip.getInstance().show(
          targetEl, 
          this.element, 
          error, 
          errorId, 
          (fix) => this.applyFix(error, fix)
        );
        return;
      }
    }

    Tooltip.getInstance().hide();
  };

  private async runCheck() {
    const text = this.element.value;
    if (!text.trim()) {
      this.errors = [];
      this.overlay.renderErrors(text, []);
      return;
    }

    const response = await Bridge.checkText(text);
    if (this.element.value !== text) return; 

    if (response.isSuccess) {
      this.errors = response.issues.map(IssuePresenter.hydrate);
      this.overlay.renderErrors(text, this.errors);
    }
  }

  private applyFix(error: HydratedIssue, fixValue: string) {
    const originalText = this.element.value;
    const currentSegment = originalText.substring(error.start, error.end);
    if (currentSegment !== error.original) {
      this.runCheck(); 
      return;
    }

    const newText = originalText.substring(0, error.start) + fixValue + originalText.substring(error.end);
    const newCursorPos = error.start + fixValue.length;

    this.element.value = newText;
    this.element.focus();
    this.element.setSelectionRange(newCursorPos, newCursorPos);

    this.element.dispatchEvent(new Event('input', { bubbles: true })); 
  }
}