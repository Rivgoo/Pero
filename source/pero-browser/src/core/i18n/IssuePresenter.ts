import { TextIssue, HydratedIssue } from '../../shared/contracts';
import ukLocale from './locales/uk.json';

interface LocaleRuleDefinition {
  readonly title: string;
  readonly description: string;
}

type LocaleRules = Readonly<Record<string, LocaleRuleDefinition>>;

export class IssuePresenter {
  static hydrate(issue: TextIssue): HydratedIssue {
    const rules = ukLocale.rules as LocaleRules;
    const definition = rules[issue.ruleId];

    const rawTitle = definition?.title ?? issue.fallbackTitle ?? 'Unknown Issue';
    const rawDescription = definition?.description ?? issue.fallbackDescription ?? `Rule Id: ${issue.ruleId}`;

    const args = issue.messageArgs;

    return {
      ...issue,
      title: IssuePresenter.interpolateArguments(rawTitle, args),
      description: IssuePresenter.interpolateArguments(rawDescription, args)
    };
  }

  private static interpolateArguments(text: string, args?: Readonly<Record<string, string>>): string {
    if (!args) return text;

    return Object.entries(args).reduce((currentText, [key, value]) => {
      const searchRegex = new RegExp(`\\{${key}\\}`, 'gi');
      return currentText.replace(searchRegex, String(value));
    }, text);
  }
}