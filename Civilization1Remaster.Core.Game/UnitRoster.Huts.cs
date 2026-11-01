namespace Civilization1Remaster.Core.Game
{
	public partial class UnitRoster
	{
		public void VisitHut(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"VisitHut({participantIndex}, {troopIndex})");

			if (participantIndex != 0 &&
				this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
			{
				int horizontalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal);
				int verticalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical);
				short townDistance = DistanceToNearestCity(horizontalLocation, verticalLocation);

				switch (this.Owner.Runtime.Dice.DrawInteger(4))
				{
					case 0:
						int hutKind = this.Owner.VideoCard.ObtainDot(2, horizontalLocation + 80, verticalLocation) & 0x7;

						if (townDistance < 4)
						{
							MeetMercenaries(participantIndex, horizontalLocation, verticalLocation);
						}
						else if (hutKind <= 4)
						{
							FindMetalDeposits(participantIndex);
						}
						else
						{
							AnnounceHut(participantIndex, "You have discovered\nan advanced tribe.\n");

							this.Owner.Founding.EstablishCity(participantIndex, 0, horizontalLocation, verticalLocation, 1);

							RepaintForHuman(participantIndex, horizontalLocation, verticalLocation);
						}

						break;

					case 1:
						if (this.Owner.World.RoundTally != 0 && this.Owner.World.Season <= 1000)
						{
							FindAncientScrolls(participantIndex);
						}
						else
						{
							FindMetalDeposits(participantIndex);
						}

						break;

					case 2:
						FindMetalDeposits(participantIndex);
						break;

					case 3:
						if (townDistance >= 4 && this.Owner.World.Participants[participantIndex].TownTally != 0)
						{
							UnleashBarbarians(participantIndex, troopIndex);
						}
						else
						{
							MeetMercenaries(participantIndex, horizontalLocation, verticalLocation);
						}

						break;
				}
			}
		}

		private short DistanceToNearestCity(int horizontalLocation, int verticalLocation)
		{
			ushort townIndex = this.Owner.Helpers.FindNearestCity(horizontalLocation, verticalLocation);
			bool found = (short)townIndex >= 0 && (short)townIndex < 128;

			return (short)this.Owner.Helpers.ObtainDistance(
				horizontalLocation, found ? this.Owner.World.Towns[townIndex].Place.X : -1,
				verticalLocation, found ? this.Owner.World.Towns[townIndex].Place.Y : -1);
		}

		private void AnnounceHut(short participantIndex, string text)
		{
			this.Owner.Words.Set(text);

			if (participantIndex == this.Owner.World.PersonParticipantIndex)
			{
				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
			}
		}

		private void FindMetalDeposits(short participantIndex)
		{
			AnnounceHut(participantIndex, "You have discovered\nvaluable metal deposits\nworth 50$\n");

			this.Owner.World.Participants[participantIndex].Gold += 50;
		}

		private void MeetMercenaries(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			AnnounceHut(participantIndex, "You have discovered\na friendly tribe of\nskilled mercenaries.\n");

			CreateUnit(participantIndex, (short)(this.Owner.Runtime.Dice.DrawInteger(2) != 0 ? 3 : 6), horizontalLocation, verticalLocation);

			RepaintForHuman(participantIndex, horizontalLocation, verticalLocation);
		}

		private void FindAncientScrolls(short participantIndex)
		{
			AnnounceHut(participantIndex, "You have discovered\nscrolls of ancient wisdom.\n");

			int scienceIndex = this.Owner.Runtime.Dice.DrawInteger(ScienceTally);

			for (int tries = 0; tries < 999; tries++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, (short)scienceIndex) == 0 &&
					this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, RequiredScience(scienceIndex, 0)) != 0 &&
					this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, RequiredScience(scienceIndex, 1)) != 0)
				{
					this.Owner.Advisors.EarnTechnology(participantIndex, (short)scienceIndex, 0);
					break;
				}

				scienceIndex = (scienceIndex + 1) % ScienceTally;
			}
		}

		private sbyte RequiredScience(int scienceIndex, int requiredIndex)
		{
			return this.Processor.PullSignedByte(this.Processor.SegmentData.Word, (ushort)(TechnologyTable.PrerequisiteFirstAddress + requiredIndex + (TechnologyTable.Stride * scienceIndex)));
		}

		private void UnleashBarbarians(short participantIndex, short troopIndex)
		{
			AnnounceHut(participantIndex, "You have unleashed\na horde of barbarians!\n");

			for (int heading = 1; heading <= 8;
				heading += this.Owner.Helpers.ClampValue(4 - this.Owner.World.Participants[participantIndex].TownTally, 1, 4))
			{
				int nearHorizontal = (short)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal) + this.Owner.MapOperations.Chart.DirectionHorizontal(heading));
				int nearVertical = (short)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical) + this.Owner.MapOperations.Chart.DirectionVertical(heading));

				if (this.Owner.MapOperations.ObtainTileOwner(nearHorizontal, nearVertical) != 0xffff ||
					(this.Owner.MapOperations.ObtainTileImprovements(nearHorizontal, nearVertical) & CityImprovement) != 0 ||
					this.Owner.MapOperations.ObtainTerrainType(nearHorizontal, nearVertical) == MapRules.OceanTerrain)
				{
					continue;
				}

				int terrain = this.Owner.MapOperations.ObtainTerrainType(nearHorizontal, nearVertical);
				short kind = (short)(TerrainMoveCost(terrain) > 2 ? 3 : 6);

				ushort barbarianIndex = CreateUnit(0, kind, nearHorizontal, nearVertical);

				ShowUnitTo(0, (short)barbarianIndex, participantIndex);

				RepaintForHuman(participantIndex, nearHorizontal, nearVertical);
			}
		}
	}
}
