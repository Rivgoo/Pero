import { AnalysisResponse } from '../../shared/contracts';

/**
 * A simple bridge for sending messages from the content script to the background.
 */
export class Bridge {
  static async checkText(text: string): Promise<AnalysisResponse> {
    try {
      if (!chrome.runtime?.id) throw new Error('Extension context invalidated');

      const request = {
        type: 'CHECK_TEXT',
        payload: { text }
      };

      return await chrome.runtime.sendMessage(request);
    } catch (error) {
      console.warn('Pero Bridge Error:', error);
      return { requestId: '', isSuccess: false, issues: [] };
    }
  }
}