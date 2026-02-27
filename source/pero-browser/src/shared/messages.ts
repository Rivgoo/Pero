import { AnalysisRequest } from './contracts';

export const MessageTypes = {
  Ping: 'PING',
  AnalyzeRequest: 'ANALYZE_REQUEST',
  OffscreenAnalyze: 'OFFSCREEN_ANALYZE'
} as const;

export type MessageType = typeof MessageTypes[keyof typeof MessageTypes];

export interface BaseMessage {
  readonly type: MessageType;
}

export interface PingMessage extends BaseMessage {
  readonly type: typeof MessageTypes.Ping;
}

export interface AnalyzeRequestMessage extends BaseMessage {
  readonly type: typeof MessageTypes.AnalyzeRequest;
  readonly payload: AnalysisRequest;
}

export interface OffscreenAnalyzeMessage extends BaseMessage {
  readonly type: typeof MessageTypes.OffscreenAnalyze;
  readonly payload: AnalysisRequest;
}

export type ExtensionMessage = PingMessage | AnalyzeRequestMessage | OffscreenAnalyzeMessage;

export interface MessageResponse<T> {
  readonly success: boolean;
  readonly data?: T;
  readonly error?: string;
}

export function isAnalyzeRequest(msg: unknown): msg is AnalyzeRequestMessage {
  return isMessageOfType<AnalyzeRequestMessage>(msg, MessageTypes.AnalyzeRequest);
}

export function isOffscreenAnalyzeRequest(msg: unknown): msg is OffscreenAnalyzeMessage {
  return isMessageOfType<OffscreenAnalyzeMessage>(msg, MessageTypes.OffscreenAnalyze);
}

function isMessageOfType<T extends BaseMessage>(msg: unknown, type: MessageType): msg is T {
  return typeof msg === 'object' && msg !== null && 'type' in msg && (msg as BaseMessage).type === type;
}