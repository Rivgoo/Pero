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

var app = new Application();
app.Run();