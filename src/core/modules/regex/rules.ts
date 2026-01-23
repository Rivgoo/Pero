import { RegexRule } from './types';
import { IssueType, Severity } from '../../types';

export const commonRules: RegexRule[] = [
  {
    id: 'double_space',
    pattern: /  +/g,
    message: 'Зайві пробіли',
    context: {
      before: 'token',
      after: 'token',
    },
    // FIX LOGIC: Combine the captured words with a single space.
    // e.g., context.before="Hello", context.after="world" -> "Hello world"
    fix: (_, context) => `${context.before} ${context.after}`, 
    type: IssueType.Whitespace,
    severity: Severity.Warning
  },
  {
    id: 'space_before_punctuation',
    pattern: /\s+([.,:;!?])/g,
    message: 'Пробіл перед розділовим знаком',
    context: {
      before: 'token',
    },
    // FIX LOGIC: Combine the captured word with the punctuation.
    // e.g., context.before="Привіт", match[1]="," -> "Привіт,"
    fix: (match, context) => `${context.before}${match[1]}`, 
    type: IssueType.Typography,
    severity: Severity.Warning
  },
  {
    id: 'missing_space_after_punctuation',
    pattern: /([,:])(?=[^\s\d])/g,
    message: 'Відсутній пробіл після розділового знаку',
    // CAPTURE CONTEXT: Get the word after the comma.
    context: {
      after: 'token',
    },
    // FIX LOGIC: Combine the punctuation, a space, and the captured word.
    // e.g., match[1]=",", context.after="світе" -> ", світе"
    fix: (match, context) => `${match[1]} ${context.after}`,
    type: IssueType.Typography,
    severity: Severity.Warning
  },
  {
    id: 'russian_letters',
    pattern: /[эыъёЭЫЪЁ]/g,
    message: 'Російська літера в тексті',
    // No context, no automatic fix.
    fix: (match) => match[0],
    type: IssueType.Spelling,
    severity: Severity.Critical
  }
];