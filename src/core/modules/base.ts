import { IChecker } from '../interfaces';
import { ValidationResult, CheckerContext, TextRange, Severity, IssueType, Replacement } from '../../shared/types';

export abstract class BaseChecker implements IChecker {
  abstract readonly id: string;

  abstract check(text: string, context: CheckerContext): Promise<ValidationResult[]>;

  /**
   * Factory method to standardize error creation.
   * Helps avoid typos in property names across different modules.
   */
  protected createResult(
    ruleId: string,
    range: TextRange,
    message: string,
    original: string,
    type: IssueType = IssueType.Grammar,
    severity: Severity = Severity.Warning,
    replacements: Replacement[] = []
  ): ValidationResult {
    return {
      checkerId: this.id,
      ruleId,
      range,
      message,
      original,
      type,
      severity,
      replacements
    };
  }

  /**
   * Utility to check if a specific rule is ignored in the context.
   */
  protected isRuleEnabled(ruleId: string, context: CheckerContext): boolean {
    if (!context.ignoredRules) return true;
    return !context.ignoredRules.includes(ruleId);
  }
}