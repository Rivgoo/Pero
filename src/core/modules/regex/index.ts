import { BaseChecker } from '../base';
import { CheckerContext, ValidationResult } from '../../types';
import { commonRules } from './rules';
import { RegexRule } from './types';

export class RegexChecker extends BaseChecker {
  readonly id = 'regex-engine';
  private rules: RegexRule[];

  private ignorePatterns = [
    /https?:\/\/[^\s]+/g,
    /[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}/g,
    /`[^`]*`/g,
  ];

  constructor(customRules?: RegexRule[]) {
    super();
    this.rules = customRules || commonRules;
  }

  async check(text: string, context: CheckerContext): Promise<ValidationResult[]> {
    const results: ValidationResult[] = [];
    const safeZones = this.calculateSafeZones(text);

    for (const rule of this.rules) {
      if (!this.isRuleEnabled(rule.id, context)) continue;

      rule.pattern.lastIndex = 0;
      let match: RegExpExecArray | null;

      while ((match = rule.pattern.exec(text)) !== null) {
        if (match.index === rule.pattern.lastIndex) {
          rule.pattern.lastIndex++;
        }

        let start = match.index;
        let end = start + match[0].length;
        
        const capturedContext = { before: '', after: '' };

        if (rule.context?.before === 'token') {
          const tokenBefore = this.getTokenBefore(text, start);
          capturedContext.before = tokenBefore;
          start -= tokenBefore.length;
        }
        
        if (rule.context?.after === 'token') {
          const tokenAfter = this.getTokenAfter(text, end);
          capturedContext.after = tokenAfter;
          end += tokenAfter.length;
        }
        
        if (this.isInsideSafeZone(start, end, safeZones)) {
          continue; 
        }
        
        const original = text.substring(start, end);
        const replacementValue = rule.fix(match, capturedContext);

        results.push(
          this.createResult(
            rule.id,
            { start, end },
            rule.message,
            original,
            rule.type,
            rule.severity,
            [{ 
              value: replacementValue, 
              caption: 'Виправити', 
              description: `Замінити "${original}" на "${replacementValue}"` 
            }]
          )
        );
      }
    }

    return results;
  }
  
  /** Finds the sequence of non-whitespace characters (a token) immediately preceding an index. */
  private getTokenBefore(text: string, index: number): string {
    const part = text.substring(0, index);
    const match = part.match(/\S+$/);
    return match ? match[0] : '';
  }

  /** Finds the sequence of non-whitespace characters (a token) immediately following an index. */
  private getTokenAfter(text: string, index: number): string {
    const part = text.substring(index);
    const match = part.match(/^\S+/);
    return match ? match[0] : '';
  }

  private calculateSafeZones(text: string): {start: number, end: number}[] {
    const zones: {start: number, end: number}[] = [];
    this.ignorePatterns.forEach(regex => {
      regex.lastIndex = 0;
      let match;
      while ((match = regex.exec(text)) !== null) {
        zones.push({ start: match.index, end: match.index + match[0].length });
      }
    });
    return zones;
  }

  private isInsideSafeZone(start: number, end: number, zones: {start: number, end: number}[]): boolean {
    return zones.some(zone => 
      (start >= zone.start && start < zone.end) || (end > zone.start && end <= zone.end)
    );
  }
}