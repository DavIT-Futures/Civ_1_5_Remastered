using System;
using System.IO;
using System.Text.Json;

namespace Civilization1Remaster.Desktop;

public class AppSettings
{
	public int Scale { get; set; } = 4;
	public bool FullScreen { get; set; } = false;
	public bool Maximized { get; set; } = true;

	private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "settings.json");

	public static AppSettings Load()
	{
		try
		{
			if (File.Exists(SettingsPath))
			{
				string json = File.ReadAllText(SettingsPath);
				var settings = JsonSerializer.Deserialize<AppSettings>(json);
				if (settings != null)
				{
					return settings;
				}
			}
		}
		catch
		{
		}

		return new AppSettings();
	}

	public void Save()
	{
		try
		{
			string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(SettingsPath, json);
		}
		catch
		{
		}
	}
}
