export type IssueCategory = 'Spelling' | 'Grammar' | 'Style' | 'Typography';
export type IssueSeverity = 'Critical' | 'Warning' | 'Info';

export interface AnalysisRequest {
  readonly requestId: string;
  readonly text: string;
  readonly languageCode: string;
  readonly disabledRules?: ReadonlyArray<string>;
  readonly debug?: boolean;
}

export interface TextIssue {
  readonly ruleId: string;
  readonly category: IssueCategory;
  readonly severity: IssueSeverity;
  readonly start: number;
  readonly end: number;
  readonly original: string;
  readonly suggestions: ReadonlyArray<string>;
  readonly messageArgs?: Readonly<Record<string, string>>;
  readonly fallbackTitle?: string;
  readonly fallbackDescription?: string;
}

export interface AnalysisResponse {
  readonly requestId: string;
  readonly isSuccess: boolean;
  readonly issues: ReadonlyArray<TextIssue>;
  readonly processingTimeMs?: number;
}

export interface HydratedIssue extends TextIssue {
  readonly title: string;
  readonly description: string;
}