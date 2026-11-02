namespace Civ_1_5_Remastered.Core.Game.Opponents
{
	public partial class ComputerPlayer
	{
		private void UpgradeUnits(short participantIndex)
		{
			Player player = this.Owner.World.Participants[participantIndex];

			for (short troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
			{
				if (this.Owner.UnitOperations.UpgradeKind(participantIndex, troopIndex) == UnitUpgradeRules.NoSuccessor)
				{
					continue;
				}

				short cost = this.Owner.UnitOperations.UpgradeCost(participantIndex, troopIndex);

				if (UnitUpgradeRules.ComputerUpgrades(player.Gold, cost))
				{
					this.Owner.UnitOperations.UpgradeUnit(participantIndex, troopIndex);
				}
			}
		}
	}
}
