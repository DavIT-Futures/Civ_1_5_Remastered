namespace Civ_1_5_Remastered.Core.Game.Units
{
	public partial class UnitRoster
	{
		public short UpgradeKind(short participantIndex, short troopIndex)
		{
			byte kind = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind);

			if (kind == 0xff)
			{
				return UnitUpgradeRules.NoSuccessor;
			}

			short successor = (short)UnitUpgradeRules.Successor(kind);

			if (successor == UnitUpgradeRules.NoSuccessor ||
				this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, (short)Tables.UnitKindWord(successor, UnitKindTable.Technology)) == 0)
			{
				return UnitUpgradeRules.NoSuccessor;
			}

			short horizontalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal);
			short verticalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical);

			if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & CityImprovement) == 0)
			{
				return UnitUpgradeRules.NoSuccessor;
			}

			ushort townIndex = this.Owner.MapOperations.FindCityAtTile(horizontalLocation, verticalLocation);

			if (!WonderCityRules.ReadsCity((short)townIndex) || this.Owner.World.Towns[townIndex].ParticipantIndex != participantIndex)
			{
				return UnitUpgradeRules.NoSuccessor;
			}

			return successor;
		}

		public short UpgradeCost(short participantIndex, short troopIndex)
		{
			short successor = UpgradeKind(participantIndex, troopIndex);

			if (successor == UnitUpgradeRules.NoSuccessor)
			{
				return 0;
			}

			byte kind = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind);

			return (short)UnitUpgradeRules.Cost((short)Tables.UnitKindWord(kind, UnitKindTable.Price), (short)Tables.UnitKindWord(successor, UnitKindTable.Price));
		}

		public bool UpgradeUnit(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"UpgradeUnit({participantIndex}, {troopIndex})");

			short successor = UpgradeKind(participantIndex, troopIndex);
			short cost = UpgradeCost(participantIndex, troopIndex);
			Player player = this.Owner.World.Participants[participantIndex];

			if (successor == UnitUpgradeRules.NoSuccessor || player.Gold < cost)
			{
				return false;
			}

			byte kind = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind);
			int lostHitPoints = this.Owner.World.Vitals.ObtainLostHitPoints(participantIndex, troopIndex, kind);
			bool elite = this.Owner.World.Vitals.VerifyElite(participantIndex, troopIndex, kind);

			player.Gold -= cost;
			player.LiveTroops[kind]--;
			player.LiveTroops[successor]++;

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Kind, (byte)successor);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.LeftoverSteps, 0);
			this.Owner.World.Vitals.Assign(participantIndex, troopIndex, successor, lostHitPoints, elite);

			return true;
		}
	}
}
