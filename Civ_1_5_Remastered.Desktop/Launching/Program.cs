using System;
using Avalonia;
using Civ_1_5_Remastered.Foundation.Support;

namespace Civ_1_5_Remastered.Desktop.Launching;

internal class Program
{
	[STAThread]
	public static void Main(string[] commandLine)
	{
		AppDomain.CurrentDomain.UnhandledException += (sender, arguments) =>
		{
			if (arguments.ExceptionObject is Exception exception)
			{
				CrashLogger.Log(exception, "AppDomain.UnhandledException");
			}
		};

		System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (sender, arguments) =>
		{
			CrashLogger.Log(arguments.Exception, "TaskScheduler.UnobservedTaskException");
		};

		BuildAvaloniaApp().StartWithClassicDesktopLifetime(commandLine);
	}

	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<DesktopApplication>()
			.UsePlatformDetect()
#if DEBUG
			.WithDeveloperTools()
#endif
			.WithInterFont()
			.LogToTrace();
}
