namespace Civilization1Remaster.Core.Game
{
	public partial class Cities
	{
		private enum ProductionStep
		{
			UnitBuilt,

			NotEnough,

			Improvement,

			SettlersWait,

			CityLost,
		}
	}
}
