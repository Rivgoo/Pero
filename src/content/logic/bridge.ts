import { CheckRequest, CheckResponse, ValidationResult } from '../../shared/types';

export class Bridge {
  static async checkText(text: string): Promise<ValidationResult[]> {
    try {
      if (!chrome.runtime?.id) {
        throw new Error('Extension context invalidated');
      }

      const request: CheckRequest = {
        type: 'CHECK_TEXT',
        payload: { text }
      };

      const response = await chrome.runtime.sendMessage(request) as CheckResponse;

      if (!response.success) {
        throw new Error(response.error || 'Unknown error');
      }

      return response.errors;
    } catch (error) {
      console.warn('Pero Bridge Error:', error);
      return [];
    }
  }
}