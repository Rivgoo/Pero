export enum Severity {
  Critical = 'critical', // Red error (Spelling)
  Warning = 'warning',   // Yellow warning (Punctuation)
  Info = 'info',         // Blue hint (Style)
  Success = 'success'    // Green (rare, but good for validation confirmation)
}

export enum IssueType {
  Spelling = 'spelling',
  Grammar = 'grammar',
  Punctuation = 'punctuation',
  Style = 'style',
  Whitespace = 'whitespace', // Specific type for spacing errors
  Typography = 'typography'  // e.g., quotes, dashes
}

export interface TextRange {
  start: number; // Absolute index in the string
  end: number;
}

export interface Replacement {
  value: string;      // The new text
  caption?: string;   // Short description for the button (e.g., "Remove space")
  description?: string; // Longer explanation if needed
}

// The core "Context" object passed to every checker
// This allows us to change behavior without rewriting code
export interface CheckerContext {
  language: string;
  ignoredRules?: string[]; // Array of rule IDs to skip
  customDictionary?: Set<string>; // User's added words
}

export interface ValidationResult {
  // Metadata
  checkerId: string;      // Who found this? (e.g., 'regex-engine')
  ruleId: string;         // Which specific rule? (e.g., 'double-space')
  
  // Location
  range: TextRange;
  
  // Content
  original: string;       // The text that was flagged
  message: string;        // User-friendly message
  replacements: Replacement[];
  
  // Classification
  severity: Severity;
  type: IssueType;
}