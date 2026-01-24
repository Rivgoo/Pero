export class OffscreenManager {
  private static readonly OFFSCREEN_PATH = 'src/offscreen/index.html';
  private creating: Promise<void> | null = null;

  async ensureCreated(): Promise<void> {
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
    } catch (e: any) {
      // Ignore error if document was created in a race condition
      if (!e.message.includes('Only one offscreen')) {
        throw e;
      }
    }
  }
}