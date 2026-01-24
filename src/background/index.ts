import { AnalysisEngine } from '../core/engine';
import { RegexChecker } from '../core/modules/regex';
import { CheckRequest, CheckResponse } from '../shared/types';

const engine = new AnalysisEngine();
engine.register(new RegexChecker());

console.log('Pero: Background Service Started');

chrome.runtime.onMessage.addListener(
  (message: CheckRequest, _sender, sendResponse) => {
    if (message.type === 'CHECK_TEXT') {
      // Async handler pattern
      handleCheck(message.payload.text)
        .then(response => sendResponse(response));
      
      return true; // Keep channel open
    }
    return false; // No response
  }
);

async function handleCheck(text: string): Promise<CheckResponse> {
  try {
    const errors = await engine.analyze(text);
    return { success: true, errors };
  } catch (err) {
    console.error('Analysis fatal error:', err);
    return { success: false, errors: [], error: (err as Error).message };
  }
}