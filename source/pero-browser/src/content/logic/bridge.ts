import { AnalysisResponse, AnalysisRequest } from '../../shared/contracts';
import { AnalyzeRequestMessage, MessageResponse, MessageTypes } from '../../shared/messages';
import { StorageKeys } from '../../shared/constants';

export class Bridge {
  static async checkText(text: string): Promise<AnalysisResponse> {
    try {
      this.ensureValidContext();

      const storage = await chrome.storage.local.get(StorageKeys.DebugMode);
      const isDebugMode = Boolean(storage[StorageKeys.DebugMode]);

      const payload: AnalysisRequest = {
        requestId: crypto.randomUUID(),
        text,
        languageCode: 'uk-UA',
        debug: isDebugMode
      };

      const message: AnalyzeRequestMessage = {
        type: MessageTypes.AnalyzeRequest,
        payload
      };

      const response = await chrome.runtime.sendMessage(message) as MessageResponse<AnalysisResponse>;
      return this.extractResponseData(response);

    } catch (error) {
      return this.createEmptyResponse();
    }
  }

  private static ensureValidContext(): void {
    if (!chrome.runtime?.id) {
      throw new Error('Extension context invalidated.');
    }
  }

  private static extractResponseData(response: MessageResponse<AnalysisResponse>): AnalysisResponse {
    if (response.success && response.data) {
      return response.data;
    }
    return this.createEmptyResponse();
  }

  private static createEmptyResponse(): AnalysisResponse {
    return { 
      requestId: '', 
      isSuccess: false, 
      issues: [] 
    };
  }
}