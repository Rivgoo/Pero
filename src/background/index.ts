import { AnalysisEngine } from '../core/engine';
import { RegexChecker } from '../core/modules/regex';

// 1. Initialize the Engine
const engine = new AnalysisEngine();

// 2. Register Modules
// In the future, we will await dictionary loading here
engine.register(new RegexChecker());

console.log('Ukrainian Grammar Checker: Service Worker Started');

// 3. Message Listener
chrome.runtime.onMessage.addListener((request, _sender, sendResponse) => {
  if (request.type === 'CHECK_TEXT') {
    handleCheckText(request.text, sendResponse);
    return true; // Tells Chrome: "Wait, I will answer asynchronously"
  }
  return false; // Tells Chrome: "I am not handling this message"
});

/**
 * Handles the text analysis request.
 * Wrapper function to handle async/await cleanly inside the listener.
 */
async function handleCheckText(text: string, sendResponse: (response: any) => void) {
  try {
    const startTime = performance.now();
    
    // Run the analysis
    const errors = await engine.analyze(text);
    
    const duration = performance.now() - startTime;
    console.log(`Analyzed ${text.length} chars in ${duration.toFixed(2)}ms. Found ${errors.length} issues.`);

    sendResponse({ success: true, errors });
  } catch (error) {
    console.error('Analysis failed:', error);
    sendResponse({ success: false, error: (error as Error).message });
  }
}