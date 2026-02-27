import './styles.css';
import { InputSession } from './logic/session';

class SessionManager {
  private activeSession: InputSession | null = null;
  private activeElement: HTMLElement | null = null;

  initialize(): void {
    document.addEventListener('focusin', this.handleFocusIn, true);
  }

  private handleFocusIn = (event: FocusEvent): void => {
    const target = event.target as HTMLElement;

    if (this.activeSession && target === this.activeElement) {
      return;
    }

    this.clearCurrentSession();

    if (this.isSupportedInput(target)) {
      if (target.readOnly || target.disabled) return;
      
      this.activeElement = target;
      this.activeSession = new InputSession(target);
    }
  };

  private clearCurrentSession(): void {
    if (this.activeSession) {
      this.activeSession.destroy();
      this.activeSession = null;
      this.activeElement = null;
    }
  }

  private isSupportedInput(element: HTMLElement): element is HTMLInputElement | HTMLTextAreaElement {
    if (element.tagName === 'TEXTAREA') return true;
    
    if (element.tagName === 'INPUT') {
      const input = element as HTMLInputElement;
      return input.type === 'text' || input.type === 'search' || input.type === 'email';
    }
    
    return false;
  }
}

const sessionManager = new SessionManager();
sessionManager.initialize();