import { STORAGE_KEYS } from '../../shared/constants';

export class DebugLogger {
  private static instance: DebugLogger;
  private isEnabled = false;

  private constructor() {
    this.initializeState();
    this.watchStorage();
  }

  static getInstance(): DebugLogger {
    if (!DebugLogger.instance) {
      DebugLogger.instance = new DebugLogger();
    }
    return DebugLogger.instance;
  }

  logRequest(json: string, forceLog = false): void {
    if (!this.isEnabled && !forceLog) return;
    
    this.printGroup('⬆️ Pero: To .NET', json);
  }

  logResponse(json: string, duration?: number, forceLog = false): void {
    if (!this.isEnabled && !forceLog) return;

    const title = duration 
      ? `⬇️ Pero: From .NET (${duration.toFixed(2)}ms)` 
      : '⬇️ Pero: From .NET';

    this.printGroup(title, json);
  }

  private initializeState(): void {
    if (typeof chrome === 'undefined' || !chrome.storage || !chrome.storage.local) {
      return;
    }

    chrome.storage.local.get(STORAGE_KEYS.DEBUG_MODE, (result) => {
      this.isEnabled = (result[STORAGE_KEYS.DEBUG_MODE] as boolean) ?? false;
    });
  }

  private watchStorage(): void {
    if (typeof chrome === 'undefined' || !chrome.storage || !chrome.storage.onChanged) {
      return;
    }

    chrome.storage.onChanged.addListener((changes, area) => {
      if (area === 'local' && changes[STORAGE_KEYS.DEBUG_MODE]) {
        this.isEnabled = changes[STORAGE_KEYS.DEBUG_MODE].newValue as boolean;
      }
    });
  }

  private printGroup(title: string, json: string): void {
    try {
      const parsed = JSON.parse(json);
      console.groupCollapsed(title);
      console.log(parsed);
      console.log('Raw:', json);
      console.groupEnd();
    } catch {
      console.groupCollapsed(title);
      console.log('Raw (Parse Failed):', json);
      console.groupEnd();
    }
  }
}