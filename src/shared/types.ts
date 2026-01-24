export enum Severity {
  Critical = 'critical',
  Warning = 'warning',
  Info = 'info',
}

export enum IssueType {
  Spelling = 'spelling',
  Grammar = 'grammar',
  Punctuation = 'punctuation',
  Style = 'style',
  Whitespace = 'whitespace',
  Typography = 'typography',
}

export interface TextRange {
  start: number;
  end: number;
}

export interface Replacement {
  value: string;
  caption?: string;
  description?: string;
}

export interface ValidationResult {
  checkerId: string;
  ruleId: string;
  range: TextRange;
  original: string;
  message: string;
  replacements: Replacement[];
  severity: Severity;
  type: IssueType;
}

export interface CheckerContext {
  language: string;
  ignoredRules?: string[];
}

export interface CheckRequest {
  type: 'CHECK_TEXT';
  payload: {
    text: string;
    context?: Partial<CheckerContext>;
  };
}

export interface CheckResponse {
  success: boolean;
  errors: ValidationResult[];
  error?: string;
}

export type ExtensionMessage = CheckRequest; 