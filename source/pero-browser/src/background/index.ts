import { OffscreenManager } from './OffscreenManager';
import { isAnalyzeRequest, OffscreenAnalyzeMessage, MessageTypes } from '../shared/messages';

const offscreenManager = new OffscreenManager();

chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  if (!isAnalyzeRequest(message)) return false;

  handleAnalysisRequest(message, sendResponse);
  return true;
});

async function handleAnalysisRequest(message: any, sendResponse: (response: any) => void): Promise<void> {
  try {
    await offscreenManager.ensureCreated();
    
    const offscreenMsg: OffscreenAnalyzeMessage = {
      type: MessageTypes.OffscreenAnalyze,
      payload: message.payload
    };
    
    const response = await chrome.runtime.sendMessage(offscreenMsg);
    sendResponse(response);
  } catch (error) {
    sendResponse({ 
      success: false, 
      error: (error as Error).message 
    });
  }
}