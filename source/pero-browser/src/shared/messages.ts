import { AnalysisRequest } from './contracts';

export type MessageType = 'PING' | 'ANALYZE_REQUEST';

export interface BaseMessage {
  type: MessageType;
  payload?: any;
}

export interface PingMessage extends BaseMessage {
  type: 'PING';
}

export interface AnalyzeRequestMessage extends BaseMessage {
  type: 'ANALYZE_REQUEST';
  payload: AnalysisRequest;
}

export type ExtensionMessage = PingMessage | AnalyzeRequestMessage;

/**
 * Standard wrapper for all asynchronous responses.
 */
export interface MessageResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
}

/**
 * Type Guard to validate message structure at runtime.
 */
export function isAnalyzeRequest(msg: any): msg is AnalyzeRequestMessage {
  return msg && msg.type === 'ANALYZE_REQUEST' && msg.payload;
}