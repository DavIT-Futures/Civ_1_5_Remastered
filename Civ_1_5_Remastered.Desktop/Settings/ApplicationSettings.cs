using System;
using System.IO;
using System.Text.Json;

namespace Civ_1_5_Remastered.Desktop.Settings;

public class ApplicationSettings
{
	public int Scale { get; set; } = 4;

	/// <summary>Columns of map tiles in the main screen's map window (15 in the original).</summary>
	public int MapColumns { get; set; } = 15;

	/// <summary>Rows of map tiles in the main screen's map window (12 in the original).</summary>
	public int MapRows { get; set; } = 12;

	private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "settings.json");

	public static ApplicationSettings Load()
	{
		try
		{
			if (File.Exists(SettingsPath))
			{
				string document = File.ReadAllText(SettingsPath);
				var settings = JsonSerializer.Deserialize(document, ApplicationSettingsContext.Default.ApplicationSettings);
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
			string document = JsonSerializer.Serialize(this, ApplicationSettingsContext.Default.ApplicationSettings);
			File.WriteAllText(SettingsPath, document);
		}
		catch
		{
		}
	}
}
