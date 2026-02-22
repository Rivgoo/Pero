import { AnalysisRequest } from './contracts';

export type MessageType = 'PING' | 'ANALYZE_REQUEST' | 'OFFSCREEN_ANALYZE';

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

export interface OffscreenAnalyzeMessage extends BaseMessage {
  type: 'OFFSCREEN_ANALYZE';
  payload: AnalysisRequest;
}

export type ExtensionMessage = PingMessage | AnalyzeRequestMessage | OffscreenAnalyzeMessage;

export interface MessageResponse<T> {
  success: boolean;
  data?: T;
  error?: string;
}

export function isAnalyzeRequest(msg: any): msg is AnalyzeRequestMessage {
  return msg && msg.type === 'ANALYZE_REQUEST' && msg.payload;
}

export function isOffscreenAnalyzeRequest(msg: any): msg is OffscreenAnalyzeMessage {
  return msg && msg.type === 'OFFSCREEN_ANALYZE' && msg.payload;
}