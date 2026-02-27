const Icons = {
  feather: `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 20h4l10.5 -10.5a2.828 2.828 0 1 0 -4 -4l-10.5 10.5v4" /><path d="M13.5 6.5l4 4" /><path d="M16 19h6" /><path d="M19 16v6" /></svg>`,
  dashboard: `<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M4 4h6v8h-6z" /><path d="M4 16h6v4h-6z" /><path d="M14 12h6v8h-6z" /><path d="M14 4h6v4h-6z" /></svg>`
};

class PopupController {
  private readonly versionLabel: HTMLElement;
  private readonly openDashboardBtn: HTMLButtonElement;

  constructor() {
    this.versionLabel = document.getElementById('appVersion') as HTMLElement;
    this.openDashboardBtn = document.getElementById('openDashboardBtn') as HTMLButtonElement;
  }

  initialize(): void {
    this.injectIcons();
    this.renderVersion();
    this.bindEvents();
  }

  private injectIcons(): void {
    document.getElementById('icon-feather')!.innerHTML = Icons.feather;
    document.getElementById('icon-dashboard')!.innerHTML = Icons.dashboard;
  }

  private renderVersion(): void {
    const manifest = chrome.runtime.getManifest();
    this.versionLabel.textContent = `v${manifest.version}`;
  }

  private bindEvents(): void {
    this.openDashboardBtn.addEventListener('click', () => this.openDashboard());
  }

  private openDashboard(): void {
    const dashboardUrl = chrome.runtime.getURL('src/app/index.html');
    chrome.tabs.create({ url: dashboardUrl });
  }
}

const controller = new PopupController();
controller.initialize();