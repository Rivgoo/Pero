import { OffscreenManager } from './OffscreenManager';
import { isAnalyzeRequest, OffscreenAnalyzeMessage } from '../shared/messages';

const offscreenManager = new OffscreenManager();

chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  
  if (isAnalyzeRequest(message)) {
    offscreenManager.ensureCreated()
      .then(() => {
        const offscreenMsg: OffscreenAnalyzeMessage = {
          type: 'OFFSCREEN_ANALYZE',
          payload: message.payload
        };
        return chrome.runtime.sendMessage(offscreenMsg);
      })
      .then(response => {
        sendResponse(response);
      })
      .catch(err => {
        console.error('Pero: Background Error', err);
        sendResponse({ success: false, error: 'Background orchestration failed' });
      });

    return true; 
  }

  return false;
});

console.log('Pero: Background Service Worker Ready');