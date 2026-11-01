namespace Civ_1_5_Remastered.GameModel.Rules
{
	public static class TenthsRules
	{
		public static short Whole(short tenths) => (short)(tenths / 10);

		public static short Tenth(short tenths) => (short)(tenths % 10);
	}
}
