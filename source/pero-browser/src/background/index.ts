import { OffscreenManager } from './OffscreenManager';
import { isAnalyzeRequest } from '../shared/messages';

const offscreenManager = new OffscreenManager();

chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  
  if (isAnalyzeRequest(message)) {
    offscreenManager.ensureCreated()
      .then(() => {
        return chrome.runtime.sendMessage(message);
      })
      .then(response => {
        sendResponse(response);
      })
      .catch(err => {
        console.error('Pero: Background Error', err);
        sendResponse({ success: false, error: 'Background orchestration failed' });
      });

    return true; // Keep channel open for async response
  }

  return false;
});

console.log('Pero: Background Service Worker Ready');