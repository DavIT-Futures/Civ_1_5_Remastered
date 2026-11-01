namespace Civilization1Remaster.GameModel.Rules
{
	public static class HomeCityRules
	{
		public const int NoHome = 0xff;

		public static bool HasHome(int homeIndex) => homeIndex != NoHome;
	}
}
