import './styles.css'; // Vite will extract this to a separate CSS file
import { HighlighterOverlay } from './ui';
import { ValidationResult } from '../core/types';

let currentOverlay: HighlighterOverlay | null = null;
let debounceTimer: ReturnType<typeof setTimeout> | null = null;

// Listen for focus on ANY input field on the page
document.addEventListener('focusin', (e) => {
  const target = e.target as HTMLElement;
  
  // Check if it's a text field we want to support
  if (isEditable(target)) {
    attachChecker(target as HTMLTextAreaElement | HTMLInputElement);
  }
});

function isEditable(el: HTMLElement): boolean {
  return el.tagName === 'TEXTAREA' || 
         (el.tagName === 'INPUT' && (el as HTMLInputElement).type === 'text');
}

function attachChecker(el: HTMLTextAreaElement | HTMLInputElement) {
  // 1. Cleanup previous overlay if exists
  if (currentOverlay) {
    currentOverlay.destroy();
    currentOverlay = null;
  }

  // 2. Create new overlay
  currentOverlay = new HighlighterOverlay(el, (error, fix) => {
    applyFix(el, error, fix);
  });

  // 3. Listen for typing
  const onInput = () => {
    if (debounceTimer) clearTimeout(debounceTimer);
    
    // Wait 500ms after last keystroke
    debounceTimer = setTimeout(() => {
      runCheck(el);
    }, 500);
  };

  el.addEventListener('input', onInput);
  
  // Run initial check immediately
  runCheck(el);

  // Cleanup when element loses focus
  el.addEventListener('blur', () => {
    // Optional: Destroy overlay on blur to save memory? 
    // For now, let's keep it visible so user can see errors after clicking away.
    // If we wanted to remove it:
    // if (currentOverlay) { currentOverlay.destroy(); currentOverlay = null; }
  }, { once: true });
}

async function runCheck(el: HTMLTextAreaElement | HTMLInputElement) {
  const text = el.value;
  if (!text.trim()) return;

  try {
    // Send to background
    const response = await chrome.runtime.sendMessage({
      type: 'CHECK_TEXT',
      text: text
    });

    if (response && response.success) {
      if (currentOverlay) {
        currentOverlay.update(text, response.errors);
      }
    }
  } catch (err) {
    console.error('Connection to background script failed (likely extension update):', err);
  }
}

function applyFix(el: HTMLTextAreaElement | HTMLInputElement, error: ValidationResult, fixValue: string) {
  const originalText = el.value;
  
  // Security check: ensure the text at that position hasn't changed since validation
  const targetSegment = originalText.substring(error.range.start, error.range.end);
  if (targetSegment !== error.original) {
    console.warn('Text changed since validation. Aborting fix.');
    // Re-run check to sync state
    runCheck(el);
    return;
  }

  // Replace text
  const newText = 
    originalText.substring(0, error.range.start) + 
    fixValue + 
    originalText.substring(error.range.end);
    
  el.value = newText;
  
  // IMPORTANT: Dispatch 'input' event so frameworks (React, Vue) know the value changed
  el.dispatchEvent(new Event('input', { bubbles: true }));
  
  // Re-validate immediately
  runCheck(el);
}

console.log('Ukrainian Grammar Checker: Content Script Loaded');