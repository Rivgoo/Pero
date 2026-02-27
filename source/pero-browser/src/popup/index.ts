import { StorageKeys } from '../shared/constants';

const Icons = {
  feather: `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 20h4l10.5 -10.5a2.828 2.828 0 1 0 -4 -4l-10.5 10.5v4" /><path d="M13.5 6.5l4 4" /><path d="M16 19h6" /><path d="M19 16v6" /></svg>`,
  bug: `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M9 9v-1a3 3 0 0 1 6 0v1" /><path d="M8 9h8a6 6 0 0 1 1 3v3a5 5 0 0 1 -10 0v-3a6 6 0 0 1 1 -3" /><path d="M3 13l4 0" /><path d="M17 13l4 0" /><path d="M4 17l4.5 -1.5" /><path d="M15.5 15.5l4.5 1.5" /><path d="M4 9l4.5 1.5" /><path d="M15.5 10.5l4.5 -1.5" /></svg>`,
  dashboard: `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 4h6v8h-6z" /><path d="M4 16h6v4h-6z" /><path d="M14 12h6v8h-6z" /><path d="M14 4h6v4h-6z" /></svg>`
};

class PopupController {
  private readonly debugToggle: HTMLInputElement;
  private readonly versionLabel: HTMLElement;
  private readonly openDashboardBtn: HTMLButtonElement;

  constructor() {
    this.debugToggle = document.getElementById('debugToggle') as HTMLInputElement;
    this.versionLabel = document.getElementById('appVersion') as HTMLElement;
    this.openDashboardBtn = document.getElementById('openDashboardBtn') as HTMLButtonElement;
  }

  initialize(): void {
    this.injectIcons();
    this.renderVersion();
    this.loadInitialState();
    this.bindEvents();
  }

  private injectIcons(): void {
    document.getElementById('icon-feather')!.innerHTML = Icons.feather;
    document.getElementById('icon-bug')!.innerHTML = Icons.bug;
    document.getElementById('icon-dashboard')!.innerHTML = Icons.dashboard;
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
    this.openDashboardBtn.addEventListener('click', () => this.openDashboard());
  }

  private handleToggleChange(): void {
    chrome.storage.local.set({ [StorageKeys.DebugMode]: this.debugToggle.checked });
  }

  private openDashboard(): void {
    const dashboardUrl = chrome.runtime.getURL('src/app/index.html');
    chrome.tabs.create({ url: dashboardUrl });
  }
}

const controller = new PopupController();
controller.initialize();