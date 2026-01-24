import { ValidationResult, CheckerContext } from '../shared/types';

export interface IChecker {
  readonly id: string;
  check(text: string, context: CheckerContext): Promise<ValidationResult[]>;
}