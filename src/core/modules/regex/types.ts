import { IssueType, Severity } from '../../types';

export interface RegexRule {
  id: string;
  // The RegEx pattern. MUST have the 'g' (global) flag.
  pattern: RegExp;
  
  // The user-facing error message
  message: string;

  /**
   * Defines how to expand the context around the initial regex match.
   * 'token' will grab the entire adjacent sequence of non-whitespace characters.
   */
  context?: {
    before?: 'token';
    after?: 'token';
  };
  
 /**
   * Generates the replacement string.
   * @param match The result from RegExp.exec().
   * @param context An object containing the captured text before/after the match.
   */
  fix: (match: RegExpExecArray, context: { before: string, after: string }) => string;
  
  type: IssueType;
  severity: Severity;
}