import { StorageKeys } from '../../shared/constants';

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

  logRequest(json: string): void {
    if (!this.isEnabled) return;
    this.printGroup('⬆️ Pero: To .NET', json);
  }

  logResponse(json: string, durationMs: number): void {
    if (!this.isEnabled) return;
    this.printGroup(`⬇️ Pero: From .NET (${durationMs.toFixed(2)}ms)`, json);
  }

  private initializeState(): void {
    if (!this.hasStorageApi()) return;
    
    chrome.storage.local.get(StorageKeys.DebugMode, (result) => {
      this.isEnabled = Boolean(result[StorageKeys.DebugMode]);
    });
  }

  private watchStorage(): void {
    if (!this.hasStorageApi()) return;

    chrome.storage.onChanged.addListener((changes, area) => {
      if (area === 'local' && changes[StorageKeys.DebugMode]) {
        this.isEnabled = Boolean(changes[StorageKeys.DebugMode].newValue);
      }
    });
  }

  private hasStorageApi(): boolean {
    return typeof chrome !== 'undefined' && Boolean(chrome.storage?.local);
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