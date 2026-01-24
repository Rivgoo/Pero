import { TextIssue, HydratedIssue } from '../../shared/contracts';
import ukLocale from './locales/uk.json';

type LocaleRules = Record<string, { title: string; description: string }>;

/**
 * Hydrates "mute" TextIssue objects with UI-specific strings.
 */
export class IssuePresenter {
  static hydrate(issue: TextIssue): HydratedIssue {
    const rules = ukLocale.rules as LocaleRules;
    const definition = rules[issue.ruleId];

    if (!definition) {
      return {
        ...issue,
        title: 'Unknown error',
        description: `Rule ID: ${issue.ruleId}`
      };
    }

    let description = definition.description;
    if (issue.messageArgs) {
      for (const [key, value] of Object.entries(issue.messageArgs)) {
        description = description.replace(`{${key}}`, value);
      }
    }

    return {
      ...issue,
      title: definition.title,
      description
    };
  }
}