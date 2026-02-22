import { STORAGE_KEYS } from '../shared/constants';

const debugToggle = document.getElementById('debugToggle') as HTMLInputElement;
const versionLabel = document.getElementById('version') as HTMLElement;

// Initialize Version
const manifest = chrome.runtime.getManifest();
versionLabel.textContent = `v${manifest.version}`;

// Initialize Toggle State
chrome.storage.local.get(STORAGE_KEYS.DEBUG_MODE, (result) => {
  debugToggle.checked = (result[STORAGE_KEYS.DEBUG_MODE] as boolean) ?? false;
});

// Bind Event
debugToggle.addEventListener('change', () => {
  chrome.storage.local.set({ [STORAGE_KEYS.DEBUG_MODE]: debugToggle.checked });
});