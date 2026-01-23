import { IChecker } from './interfaces';
import { ValidationResult, CheckerContext } from './types';

export class AnalysisEngine {
  private checkers: IChecker[] = [];
  
  // Default configuration
  private context: CheckerContext = {
    language: 'uk-UA',
    ignoredRules: [],
    customDictionary: new Set()
  };

  /**
   * Registers a new checker module (e.g., Regex, Dictionary, AI).
   */
  register(checker: IChecker): void {
    // Prevent duplicate registrations
    if (!this.checkers.find(c => c.id === checker.id)) {
      this.checkers.push(checker);
    }
  }

  /**
   * Updates the global context (settings).
   */
  updateContext(newContext: Partial<CheckerContext>): void {
    this.context = { ...this.context, ...newContext };
  }

  /**
   * The main method called by the background script.
   * Runs all registered checkers in parallel.
   */
  async analyze(text: string): Promise<ValidationResult[]> {
    if (!text || text.trim().length === 0) {
      return [];
    }

    // 1. Launch all checks in parallel
    const promises = this.checkers.map(checker => 
      checker.check(text, this.context)
        .catch(err => {
          console.error(`Checker "${checker.id}" failed:`, err);
          return []; // Return empty array on failure so we don't break the app
        })
    );

    // 2. Wait for all to finish (success or failure)
    const results = await Promise.all(promises);

    // 3. Flatten the array of arrays into a single list
    const allErrors = results.flat();

    // 4. Sort errors by position (start index)
    // This ensures the UI highlights text from top to bottom
    return allErrors.sort((a, b) => a.range.start - b.range.start);
  }
}