export class OffscreenManager {
  private static readonly OFFSCREEN_PATH = 'src/offscreen/index.html';
  private static readonly IDLE_THRESHOLD_MS = 2 * 60 * 1000; 

  private creating: Promise<void> | null = null;
  private shutdownTimer: ReturnType<typeof setTimeout> | null = null;

  async ensureCreated(): Promise<void> {
    this.resetInactivityTimer();

    const hasDoc = await this.hasOffscreenDocument();
    if (hasDoc) return;

    if (this.creating) {
      await this.creating;
    } else {
      this.creating = this.createDocument();
      await this.creating;
      this.creating = null;
    }
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
        url: OffscreenManager.OFFSCREEN_PATH,
        reasons: [chrome.offscreen.Reason.WORKERS],
        justification: 'Hosting .NET WASM runtime for text analysis',
      });
      console.log('Pero: Offscreen Document Created');
    } catch (e: any) {
      if (!e.message.includes('Only one offscreen')) {
        throw e;
      }
    }
  }

  private resetInactivityTimer() {
    if (this.shutdownTimer) {
      clearTimeout(this.shutdownTimer);
    }
    this.shutdownTimer = setTimeout(() => {
      this.terminate();
    }, OffscreenManager.IDLE_THRESHOLD_MS);
  }

  private async terminate() {
    this.shutdownTimer = null;
    if (await this.hasOffscreenDocument()) {
      try {
        await chrome.offscreen.closeDocument();
        console.log('Pero: Offscreen Document Terminated due to inactivity');
      } catch (e) {
        console.warn('Pero: Failed to terminate offscreen document', e);
      }
    }
  }
}