namespace Pero.Languages.Uk_UA.Tools.Console.UI;

public class ConsoleInterface
{
	public void ShowHeader(string title)
	{
		System.Console.Clear();
		System.Console.WriteLine(new string('=', 50));
		System.Console.WriteLine($" {title}");
		System.Console.WriteLine(new string('=', 50));
		System.Console.WriteLine();
	}

	public void ShowMessage(string message)
	{
		System.Console.WriteLine(message);
	}

	public void ShowError(string message)
	{
		var previousColor = System.Console.ForegroundColor;
		System.Console.ForegroundColor = ConsoleColor.Red;
		System.Console.WriteLine($"[ERROR] {message}");
		System.Console.ForegroundColor = previousColor;
	}

	public void ShowSuccess(string message)
	{
		var previousColor = System.Console.ForegroundColor;
		System.Console.ForegroundColor = ConsoleColor.Green;
		System.Console.WriteLine($"[SUCCESS] {message}");
		System.Console.ForegroundColor = previousColor;
	}

	public string? PromptInput(string prompt)
	{
		System.Console.Write($"{prompt}: ");
		return System.Console.ReadLine();
	}

	public int PromptForInteger(string prompt, int min, int max)
	{
		while (true)
		{
			System.Console.Write($"{prompt} ({min:N0}-{max:N0}): ");
			var input = System.Console.ReadLine();

			if (int.TryParse(input, out int value) && value >= min && value <= max)
			{
				return value;
			}
			ShowError($"Invalid input. Please enter a number between {min:N0} and {max:N0}.");
		}
	}

	public int SelectOption(string prompt, IReadOnlyList<string> options)
	{
		if (options.Count == 0)
		{
			return -1;
		}

		for (int i = 0; i < options.Count; i++)
		{
			System.Console.WriteLine($" {i + 1}. {options[i]}");
		}

		System.Console.WriteLine();

		while (true)
		{
			System.Console.Write($"{prompt} (1-{options.Count}): ");
			var input = System.Console.ReadLine();

			if (int.TryParse(input, out int selection) && selection >= 1 && selection <= options.Count)
			{
				return selection - 1; // Return zero-based index
			}

			ShowError("Invalid selection. Try again.");
		}
	}

	public void WaitForKey()
	{
		System.Console.WriteLine("\nPress any key to continue...");
		System.Console.ReadKey(intercept: true);
	}
}