import { StorageKeys } from '../shared/constants';

class PopupController {
  private readonly debugToggle: HTMLInputElement;
  private readonly versionLabel: HTMLElement;

  constructor() {
    this.debugToggle = document.getElementById('debugToggle') as HTMLInputElement;
    this.versionLabel = document.getElementById('appVersion') as HTMLElement;
  }

  initialize(): void {
    this.renderVersion();
    this.loadInitialState();
    this.bindEvents();
  }

  private renderVersion(): void {
    const manifest = chrome.runtime.getManifest();
    this.versionLabel.textContent = `v${manifest.version}`;
  }

  private loadInitialState(): void {
    chrome.storage.local.get(StorageKeys.DebugMode, (result) => {
      this.debugToggle.checked = Boolean(result[StorageKeys.DebugMode]);
    });
  }

  private bindEvents(): void {
    this.debugToggle.addEventListener('change', () => this.handleToggleChange());
  }

  private handleToggleChange(): void {
    chrome.storage.local.set({ [StorageKeys.DebugMode]: this.debugToggle.checked });
  }
}

const controller = new PopupController();
controller.initialize();