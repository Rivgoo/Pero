import { AnalysisService } from '../core/services/AnalysisService';

const service = new AnalysisService();
service.init();

console.log('Pero: Offscreen Document Initialized');

chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  if (message.type === 'CHECK_TEXT') {
    // Perform analysis
    service.analyze(message.payload.text)
      .then(sendResponse)
      .catch((error) => {
        console.error('Pero: Offscreen Analysis failed', error);
        sendResponse({ isSuccess: false, issues: [] });
      });
    
    return true; 
  }

    return false;
});