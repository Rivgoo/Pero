import { TextIssue, HydratedIssue } from '../../shared/contracts';
import ukLocale from './locales/uk.json';

type LocaleRules = Record<string, { title: string; description: string }>;

export class IssuePresenter {
  static hydrate(issue: TextIssue): HydratedIssue {
    const rules = ukLocale.rules as LocaleRules;
    const definition = rules[issue.ruleId];

    let title = definition?.title ?? issue.fallbackTitle ?? 'Unknown Issue';
    let description = definition?.description ?? issue.fallbackDescription ?? `Rule Id: ${issue.ruleId}`;

    const args = issue.messageArgs || (issue as any).MessageArgs;

    if (args && typeof args === 'object') {
      for (const [key, value] of Object.entries(args)) {

        const searchRegex = new RegExp(`\\{${key}\\}`, 'gi');
        title = title.replace(searchRegex, String(value));
        description = description.replace(searchRegex, String(value));
      }
    }

    return {
      ...issue,
      title,
      description
    };
  }
}