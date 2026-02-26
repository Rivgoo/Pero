/**
 * Request sent from the TypeScript view to the C# WASM core.
 */
export interface AnalysisRequest {
  requestId: string;
  text: string;
  languageCode: string; // e.g., "uk-UA"
  disabledRules?: string[];
  debug?: boolean;
}

/**
 * Response received from the C# WASM core.
 */
export interface AnalysisResponse {
  requestId: string;
  isSuccess: boolean;
  issues: TextIssue[];
  processingTimeMs?: number;
}

/**
 * A single, "mute" issue found by the C# core.
 * Contains only data, no UI-specific text.
 */
export interface TextIssue {
  ruleId: string;
  category: IssueCategory;
  severity: IssueSeverity;
  
  start: number;
  end: number;
  
  original: string;
  suggestions: string[];
  
  messageArgs?: Record<string, string>;
  fallbackTitle?: string;
  fallbackDescription?: string;
}

export type IssueCategory = 'Spelling' | 'Grammar' | 'Style' | 'Typography';
export type IssueSeverity = 'Critical' | 'Warning' | 'Info';

/**
 * A "hydrated" issue, enriched with UI strings for display.
 * This type should only exist within the View layer.
 */
export interface HydratedIssue extends TextIssue {
  title: string;
  description: string;
}