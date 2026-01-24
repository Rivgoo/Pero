import { WasmLoader } from '../core/services/WasmLoader';
import { isAnalyzeRequest, MessageResponse } from '../shared/messages';
import { AnalysisResponse } from '../shared/contracts';

const loader = WasmLoader.getInstance();

// Pre-load runtime to reduce latency for the first request
loader.initialize().catch(console.error);

chrome.runtime.onMessage.addListener((message, _sender, sendResponse) => {
  // 1. Health Check (PING)
  if (message.type === 'PING') {
    sendResponse({ success: true } as MessageResponse<void>);
    return false; // Synchronous response
  }

  // 2. Analysis Request
  if (isAnalyzeRequest(message)) {
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
    
    return true; // Keep channel open for async response
  }

  return false;
});