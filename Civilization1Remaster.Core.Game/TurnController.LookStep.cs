namespace Civilization1Remaster.Core.Game
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
