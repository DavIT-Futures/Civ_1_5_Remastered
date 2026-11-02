using System.Text.Json.Serialization;

namespace Civ_1_5_Remastered.Desktop.Settings;

[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(ApplicationSettings))]
public partial class ApplicationSettingsContext : JsonSerializerContext
{
}
