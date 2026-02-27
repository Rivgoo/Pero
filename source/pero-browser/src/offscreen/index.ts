import { WasmLoader } from '../core/services/WasmLoader';
import { isOffscreenAnalyzeRequest, MessageResponse, MessageTypes, ExtensionMessage } from '../shared/messages';
import { AnalysisResponse, AnalysisRequest } from '../shared/contracts';

const wasmLoader = WasmLoader.getInstance();

wasmLoader.initialize().catch(console.error);

chrome.runtime.onMessage.addListener((message: unknown, _sender, sendResponse) => {
  const typedMessage = message as ExtensionMessage;

  if (typedMessage.type === MessageTypes.Ping) {
    sendResponse({ success: true } as MessageResponse<void>);
    return false;
  }

  if (isOffscreenAnalyzeRequest(typedMessage)) {
    processAnalysisRequest(typedMessage.payload, sendResponse);
    return true; 
  }

  return false;
});

async function processAnalysisRequest(
  request: AnalysisRequest, 
  sendResponse: (response: MessageResponse<AnalysisResponse | null>) => void
): Promise<void> {
  try {
    await wasmLoader.initialize();
    const result = wasmLoader.analyze(request);
    sendResponse({ success: true, data: result });
  } catch (error) {
    sendResponse({ success: false, error: (error as Error).message });
  }
}