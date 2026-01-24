import { IChecker } from './interfaces';
import { ValidationResult, CheckerContext } from '../shared/types';

export class AnalysisEngine {
  private checkers: IChecker[] = [];
  
  private context: CheckerContext = {
    language: 'uk-UA',
    ignoredRules: [],
  };

  register(checker: IChecker): void {
    if (!this.checkers.find(c => c.id === checker.id)) {
      this.checkers.push(checker);
    }
  }

  async analyze(text: string): Promise<ValidationResult[]> {
    if (!text || text.trim().length === 0) return [];

    const results = await Promise.allSettled(
      this.checkers.map(c => c.check(text, this.context))
    );

    const allErrors: ValidationResult[] = [];
    
    results.forEach((res, index) => {
      if (res.status === 'fulfilled') {
        allErrors.push(...res.value);
      } else {
        console.error(`Checker ${this.checkers[index].id} failed:`, res.reason);
      }
    });

    return allErrors.sort((a, b) => a.range.start - b.range.start);
  }
}