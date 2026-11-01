namespace Civ_1_5_Remastered.Core.Game.Turns
{
	public partial class TurnController
	{
		private enum LookStep
		{
			Order,

			OrderOrKey,

			Direction,

			Move,

			Key,

			NextLook,

			EndOfTurn,
		}
	}
}
