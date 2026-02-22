using Pero.Languages.Uk_UA.Tools.Console;
using System.Runtime.InteropServices;
using System.Text;

Console.OutputEncoding = Encoding.UTF8;

if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
{
	Console.InputEncoding = Encoding.Unicode;
}
else
{
	Console.InputEncoding = Encoding.UTF8;
}

try
{
	var app = new Application();
	app.Run();
}
catch (Exception ex)
{
	Console.ForegroundColor = ConsoleColor.Red;
	Console.WriteLine("\n[CRITICAL ERROR] The application crashed:");
	Console.WriteLine(ex.ToString());
	Console.ResetColor();
	Console.WriteLine("\nPress any key to exit...");
	Console.ReadKey(intercept: true);
}