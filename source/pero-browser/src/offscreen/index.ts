import { WasmLoader } from '../core/services/WasmLoader';
import { isOffscreenAnalyzeRequest, MessageResponse } from '../shared/messages';
import { AnalysisResponse } from '../shared/contracts';

const loader = WasmLoader.getInstance();

loader.initialize().catch(console.error);

chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  if (message.type === 'PING') {
    sendResponse({ success: true } as MessageResponse<void>);
    return false;
  }

  if (isOffscreenAnalyzeRequest(message)) {
    loader.initialize()
      .then(() => {
        const result = loader.analyze(message.payload);
        const response: MessageResponse<AnalysisResponse> = {
          success: true,
          data: result
        };
        sendResponse(response);
      })
      .catch((err) => {
        const response: MessageResponse<null> = {
          success: false,
          error: err instanceof Error ? err.message : 'Unknown WASM error'
        };
        sendResponse(response);
      });
    
    return true; 
  }

  return false;
});