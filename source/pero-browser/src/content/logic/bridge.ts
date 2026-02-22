import { AnalysisResponse, AnalysisRequest } from '../../shared/contracts';
import { AnalyzeRequestMessage, MessageResponse } from '../../shared/messages';
import { STORAGE_KEYS } from '../../shared/constants';

export class Bridge {
  static async checkText(text: string): Promise<AnalysisResponse> {
    try {
      if (!chrome.runtime?.id) {
        throw new Error('Extension context invalidated');
      }

      const storage = await chrome.storage.local.get(STORAGE_KEYS.DEBUG_MODE);
      const isDebug = (storage[STORAGE_KEYS.DEBUG_MODE] as boolean) ?? false;

      const payload: AnalysisRequest = {
        requestId: crypto.randomUUID(),
        text: text,
        languageCode: 'uk-UA',
        debug: isDebug
      };

      const message: AnalyzeRequestMessage = {
        type: 'ANALYZE_REQUEST',
        payload
      };

      const response = await chrome.runtime.sendMessage(message) as MessageResponse<AnalysisResponse>;

      if (response.success && response.data) {
        return response.data;
      }

      console.warn('Pero: Analysis failed with error:', response.error);
      return this.createEmptyResponse();

    } catch (error) {
      console.warn('Pero Bridge Error:', error);
      return this.createEmptyResponse();
    }
  }

  private static createEmptyResponse(): AnalysisResponse {
    return { requestId: '', isSuccess: false, issues: [] };
  }
}