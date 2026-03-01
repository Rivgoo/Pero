import { HydratedIssue } from '../../shared/contracts';
import { IssuePresenter } from '../../core/i18n/IssuePresenter';
import { Bridge } from './bridge';
import { Overlay } from '../ui/overlay';
import { Tooltip } from '../ui/tooltip';

const SESSION_CONFIG = {
  DebounceDelayMs: 750
} as const;

interface ScrollState {
  readonly top: number;
  readonly left: number;
}

export class InputSession {
  private readonly element: HTMLInputElement | HTMLTextAreaElement;
  private readonly overlay: Overlay;
  
  private debounceTimer: ReturnType<typeof setTimeout> | null = null;
  private currentVersion = 0;
  private errors: ReadonlyArray<HydratedIssue> = [];
  private isDestroyed = false;

  constructor(element: HTMLInputElement | HTMLTextAreaElement) {
    this.element = element;
    this.overlay = new Overlay(element);
    this.bindEvents();
    this.runImmediateCheck();
  }

  destroy(): void {
    this.isDestroyed = true;
    this.unbindEvents();
    this.clearPendingTasks();
    this.overlay.destroy();
    Tooltip.getInstance().hide();
  }

  private bindEvents(): void {
    this.element.addEventListener('input', this.handleInput);
    this.element.addEventListener('scroll', this.handleScroll);
    this.element.addEventListener('click', this.handleClick);
    this.element.addEventListener('keydown', this.handleKeyDown);
  }

  private unbindEvents(): void {
    this.element.removeEventListener('input', this.handleInput);
    this.element.removeEventListener('scroll', this.handleScroll);
    this.element.removeEventListener('click', this.handleClick);
    this.element.removeEventListener('keydown', this.handleKeyDown);
  }

  private handleKeyDown = (event: Event): void => {
    const keyEvent = event as KeyboardEvent;
    if (keyEvent.key === 'Escape') {
      Tooltip.getInstance().hide();
    }
  };

  private handleInput = (): void => {
    Tooltip.getInstance().hide();
    this.clearErrorsVisually();
    this.scheduleCheck();
  };

  private handleScroll = (): void => {
    Tooltip.getInstance().hide();
    this.overlay.syncScroll();
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
    const errorId = this.buildErrorId(error);
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

  private buildErrorId(error: HydratedIssue): string {
    return `${error.start}-${error.end}`;
  }

  private clearErrorsVisually(): void {
    this.errors = [];
    this.overlay.clear();
  }

  private scheduleCheck(): void {
    this.clearPendingTasks();
    this.debounceTimer = setTimeout(() => this.executeAnalysis(), SESSION_CONFIG.DebounceDelayMs);
  }

  private runImmediateCheck(): void {
    this.clearPendingTasks();
    this.executeAnalysis();
  }

  private clearPendingTasks(): void {
    if (this.debounceTimer) {
      clearTimeout(this.debounceTimer);
      this.debounceTimer = null;
    }
  }

  private async executeAnalysis(): Promise<void> {
    if (this.isDestroyed) return;

    const text = this.element.value;
    if (!text.trim()) {
      this.updateErrors(text, []);
      return;
    }

    this.currentVersion++;
    const requestVersion = this.currentVersion;

    const response = await Bridge.checkText(text);
    
    if (this.isDestroyed || requestVersion !== this.currentVersion) {
      return;
    }

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
      this.runImmediateCheck();
      return;
    }

    const scrollState = this.captureScrollState();
    
    this.replaceText(error.start, error.end, fixValue);
    this.restoreScrollState(scrollState);
    
    this.runImmediateCheck();
  }

  private replaceText(start: number, end: number, replacement: string): void {
    this.element.focus();
    this.element.setSelectionRange(start, end);

    const success = document.execCommand('insertText', false, replacement);
    
    if (!success) {
      this.element.setRangeText(replacement, start, end, 'end');
      this.element.dispatchEvent(new Event('input', { bubbles: true }));
    }
  }

  private captureScrollState(): ScrollState {
    return {
      top: this.element.scrollTop,
      left: this.element.scrollLeft
    };
  }

  private restoreScrollState(state: ScrollState): void {
    this.element.scrollTop = state.top;
    this.element.scrollLeft = state.left;
    this.overlay.syncScroll();
  }
}