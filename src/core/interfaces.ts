import { ValidationResult, CheckerContext } from './types';

export interface IChecker {
  // Unique identifier for the module
  readonly id: string;

  /**
   * Analyzes text and returns a list of issues.
   * @param text The full text to analyze
   * @param context Configuration and state for this specific check
   */
  check(text: string, context: CheckerContext): Promise<ValidationResult[]>;
}