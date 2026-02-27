import { HydratedIssue } from '../../shared/contracts';
import { IssuePresenter } from '../../core/i18n/IssuePresenter';
import { Bridge } from './bridge';
import { Overlay } from '../ui/overlay';
import { Tooltip } from '../ui/tooltip';

const SessionConfig = {
  DebounceDelayMs: 500
} as const;

export class InputSession {
  private readonly element: HTMLInputElement | HTMLTextAreaElement;
  private readonly overlay: Overlay;
  private debounceTimer: ReturnType<typeof setTimeout> | null = null;
  private animationFrameId: number | null = null;
  private errors: ReadonlyArray<HydratedIssue> = [];

  constructor(element: HTMLInputElement | HTMLTextAreaElement) {
    this.element = element;
    this.overlay = new Overlay(element);
    this.bindEvents();
    this.runCheck(); 
  }

  destroy(): void {
    this.unbindEvents();
    this.clearPendingTasks();
    this.overlay.destroy();
    Tooltip.getInstance().hide();
  }

  private bindEvents(): void {
    this.element.addEventListener('input', this.handleInput);
    this.element.addEventListener('scroll', this.handleScroll); 
    this.element.addEventListener('click', this.handleClick);
    window.addEventListener('resize', this.handleWindowResize); 
  }

  private unbindEvents(): void {
    this.element.removeEventListener('input', this.handleInput);
    this.element.removeEventListener('scroll', this.handleScroll);
    this.element.removeEventListener('click', this.handleClick);
    window.removeEventListener('resize', this.handleWindowResize);
  }

  private clearPendingTasks(): void {
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
    if (this.animationFrameId) cancelAnimationFrame(this.animationFrameId);
  }

  private handleWindowResize = (): void => {
    if (this.animationFrameId) return;
    this.animationFrameId = requestAnimationFrame(() => {
      this.overlay.refreshPosition();
      this.animationFrameId = null;
    });
  };

  private handleInput = (): void => {
    Tooltip.getInstance().hide();
    if (this.debounceTimer) clearTimeout(this.debounceTimer);
    this.debounceTimer = setTimeout(() => this.runCheck(), SessionConfig.DebounceDelayMs);
  };

  private handleScroll = (): void => {
    Tooltip.getInstance().hide();
  };

  private handleClick = (event: Event): void => {
    const caretPosition = this.element.selectionStart;
    if (caretPosition === null) return;

    const activeError = this.errors.find(e => caretPosition >= e.start && caretPosition <= e.end);
    
    if (activeError) {
      this.displayTooltipForError(event, activeError);
      return;
    }

    Tooltip.getInstance().hide();
  };

  private displayTooltipForError(event: Event, error: HydratedIssue): void {
    const errorId = `${error.start}-${error.end}`;
    const targetElement = this.overlay.getElementByErrorId(errorId);
    
    if (targetElement) {
      event.stopPropagation();
      Tooltip.getInstance().show(
        targetElement, 
        this.element, 
        error, 
        errorId, 
        (fix) => this.applyFix(error, fix)
      );
    }
  }

  private async runCheck(): Promise<void> {
    const text = this.element.value;
    if (!text.trim()) {
      this.updateErrors(text, []);
      return;
    }

    const response = await Bridge.checkText(text);
    if (this.element.value !== text) return; 

    if (response.isSuccess) {
      const hydratedErrors = response.issues.map(IssuePresenter.hydrate);
      this.updateErrors(text, hydratedErrors);
    }
  }

  private updateErrors(text: string, errors: ReadonlyArray<HydratedIssue>): void {
    this.errors = errors;
    this.overlay.renderErrors(text, this.errors);
  }

  private applyFix(error: HydratedIssue, fixValue: string): void {
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