using System;
using Avalonia;
using Civilization1Remaster.Foundation.Support;

namespace Civilization1Remaster.Desktop;

internal class Program
{
	[STAThread]
	public static void Main(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += (s, e) =>
		{
			if (e.ExceptionObject is Exception ex)
			{
				CrashLogger.Log(ex, "AppDomain.UnhandledException");
			}
		};

		System.Threading.Tasks.TaskScheduler.UnobservedTaskException += (s, e) =>
		{
			CrashLogger.Log(e.Exception, "TaskScheduler.UnobservedTaskException");
		};

		BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
	}

	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
#if DEBUG
			.WithDeveloperTools()
#endif
			.WithInterFont()
			.LogToTrace();
}
