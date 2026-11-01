namespace Civ_1_5_Remastered.GameModel.Rules
{
	public static class TurnEndRules
	{
		public const int DisasterDraws = 2;

		public static byte ReplayPopulationHigh(ushort population) => (byte)(population >> 8);

		public static ushort EmissaryEnemyName(int enemyIndex) => (ushort)(enemyIndex << 1);
	}
}
