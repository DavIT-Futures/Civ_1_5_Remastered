namespace Civilization1Remaster.GameModel.Rules
{
	public static class WonderCityRules
	{
		public const short Destroyed = 128;

		public static bool ReadsCity(int townIndex) => townIndex >= 0 && townIndex < Destroyed;
	}
}
