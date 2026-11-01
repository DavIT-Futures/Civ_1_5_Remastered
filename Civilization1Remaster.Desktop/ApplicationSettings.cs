using System;
using System.IO;
using System.Text.Json;

namespace Civilization1Remaster.Desktop;

public class ApplicationSettings
{
	public int Scale { get; set; } = 4;

	private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "settings.json");

	public static ApplicationSettings Load()
	{
		try
		{
			if (File.Exists(SettingsPath))
			{
				string document = File.ReadAllText(SettingsPath);
				var settings = JsonSerializer.Deserialize<ApplicationSettings>(document);
				if (settings != null)
				{
					return settings;
				}
			}
		}
		catch
		{
		}

		return new ApplicationSettings();
	}

	public void Save()
	{
		try
		{
			string document = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
			File.WriteAllText(SettingsPath, document);
		}
		catch
		{
		}
	}
}
