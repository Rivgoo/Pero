export class OffscreenManager {
  private static readonly OffscreenPath = 'src/offscreen/index.html';
  private static readonly IdleThresholdMs = 120_000;

  private creatingPromise: Promise<void> | null = null;
  private shutdownTimerId: ReturnType<typeof setTimeout> | null = null;

  async ensureCreated(): Promise<void> {
    this.resetInactivityTimer();

    const isCreated = await this.hasOffscreenDocument();
    if (isCreated) return;

    if (!this.creatingPromise) {
      this.creatingPromise = this.createDocument();
    }
    
    await this.creatingPromise;
    this.creatingPromise = null;
  }

  private async hasOffscreenDocument(): Promise<boolean> {
    const contexts = await chrome.runtime.getContexts({
      contextTypes: [chrome.runtime.ContextType.OFFSCREEN_DOCUMENT],
    });
    return contexts.length > 0;
  }

  private async createDocument(): Promise<void> {
    try {
      await chrome.offscreen.createDocument({
        url: OffscreenManager.OffscreenPath,
        reasons: [chrome.offscreen.Reason.WORKERS],
        justification: 'Hosting .NET WASM runtime for text analysis',
      });
    } catch (error) {
      this.handleCreationError(error as Error);
    }
  }

  private handleCreationError(error: Error): void {
    if (!error.message.includes('Only one offscreen')) {
      throw error;
    }
  }

  private resetInactivityTimer(): void {
    if (this.shutdownTimerId) {
      clearTimeout(this.shutdownTimerId);
    }
    this.shutdownTimerId = setTimeout(() => this.terminate(), OffscreenManager.IdleThresholdMs);
  }

  private async terminate(): Promise<void> {
    this.shutdownTimerId = null;
    const isCreated = await this.hasOffscreenDocument();
    
    if (isCreated) {
      await chrome.offscreen.closeDocument();
    }
  }
}