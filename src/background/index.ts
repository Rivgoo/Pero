import { AnalysisService } from '../core/services/AnalysisService';

const service = new AnalysisService();
service.init(); // Pre-load WASM on extension startup

chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  if (message.type === 'CHECK_TEXT') {
    service.analyze(message.payload.text)
      .then(sendResponse)
      .catch((error) => {
        console.error('Pero: Analysis failed', error);
        sendResponse({ isSuccess: false, issues: [] });
      });
    
    return true; // Indicates an asynchronous response
  }

  return false; // No response for other message types
});

console.log('Pero: Background Service Ready');