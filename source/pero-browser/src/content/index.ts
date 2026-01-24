import './styles.css';
import { InputSession } from './logic/session';

let currentSession: InputSession | null = null;

function isSupportedInput(el: HTMLElement): el is HTMLInputElement | HTMLTextAreaElement {
  if (el.tagName === 'TEXTAREA') return true;
  if (el.tagName === 'INPUT') {
    const input = el as HTMLInputElement;
    return input.type === 'text' || input.type === 'search' || input.type === 'email';
  }
  return false;
}

document.addEventListener('focusin', (e) => {
  const target = e.target as HTMLElement;

  if (currentSession && (target === (currentSession as any).element)) {
    return;
  }

  if (currentSession) {
    currentSession.destroy();
    currentSession = null;
  }

  if (isSupportedInput(target)) {
    if (target.readOnly || target.disabled) return;
    
    currentSession = new InputSession(target);
  }
}, true);

console.log('Pero: Content Script Loaded');