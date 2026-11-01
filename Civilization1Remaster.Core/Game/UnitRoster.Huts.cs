namespace Civilization1Remaster.Core.Game
{
	public partial class UnitRoster
	{
		public void VisitHut(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"VisitHut({participantIdx}, {troopIdx})");

			if (participantIdx != 0 &&
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
			{
				int horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
				int vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);
				short townDistance = DistanceToNearestCity(horizLoc, vertLoc);

				switch (this.Owner.Libc.Dice.DrawInt(4))
				{
					case 0:
						int hutKind = this.Owner.VideoCard.ObtainDot(2, horizLoc + 80, vertLoc) & 0x7;

						if (townDistance < 4)
						{
							MeetMercenaries(participantIdx, horizLoc, vertLoc);
						}
						else if (hutKind <= 4)
						{
							FindMetalDeposits(participantIdx);
						}
						else
						{
							AnnounceHut(participantIdx, "You have discovered\nan advanced tribe.\n");

							this.Owner.Founding.EstablishCity(participantIdx, 0, horizLoc, vertLoc, 1);

							RepaintForHuman(participantIdx, horizLoc, vertLoc);
						}
						break;

					case 1:
						if (this.Owner.World.RoundTally != 0 && this.Owner.World.Season <= 1000)
						{
							FindAncientScrolls(participantIdx);
						}
						else
						{
							FindMetalDeposits(participantIdx);
						}
						break;

					case 2:
						FindMetalDeposits(participantIdx);
						break;

					case 3:
						if (townDistance >= 4 && this.Owner.World.Participants[participantIdx].TownTally != 0)
						{
							UnleashBarbarians(participantIdx, troopIdx);
						}
						else
						{
							MeetMercenaries(participantIdx, horizLoc, vertLoc);
						}
						break;
				}
			}
		}

		private short DistanceToNearestCity(int horizLoc, int vertLoc)
		{
			ushort townIdx = this.Owner.Helpers.FindNearestCity(horizLoc, vertLoc);
			bool found = (short)townIdx >= 0 && (short)townIdx < 128;

			return (short)this.Owner.Helpers.ObtainDistance(
				horizLoc, found ? this.Owner.World.Towns[townIdx].Place.X : -1,
				vertLoc, found ? this.Owner.World.Towns[townIdx].Place.Y : -1);
		}

		private void AnnounceHut(short participantIdx, string text)
		{
			this.Owner.Words.Set(text);

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
			}
		}

		private void FindMetalDeposits(short participantIdx)
		{
			AnnounceHut(participantIdx, "You have discovered\nvaluable metal deposits\nworth 50$\n");

			this.Owner.World.Participants[participantIdx].Gold += 50;
		}

		private void MeetMercenaries(short participantIdx, int horizLoc, int vertLoc)
		{
			AnnounceHut(participantIdx, "You have discovered\na friendly tribe of\nskilled mercenaries.\n");

			CreateUnit(participantIdx, (short)(this.Owner.Libc.Dice.DrawInt(2) != 0 ? 3 : 6), horizLoc, vertLoc);

			RepaintForHuman(participantIdx, horizLoc, vertLoc);
		}

		private void FindAncientScrolls(short participantIdx)
		{
			AnnounceHut(participantIdx, "You have discovered\nscrolls of ancient wisdom.\n");

			int scienceIdx = this.Owner.Libc.Dice.DrawInt(ScienceTally);

			for (int tries = 0; tries < 999; tries++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, (short)scienceIdx) == 0 &&
					this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, RequiredScience(scienceIdx, 0)) != 0 &&
					this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, RequiredScience(scienceIdx, 1)) != 0)
				{
					this.Owner.Advisors.EarnTechnology(participantIdx, (short)scienceIdx, 0);
					break;
				}

				scienceIdx = (scienceIdx + 1) % ScienceTally;
			}
		}

		private sbyte RequiredScience(int scienceIdx, int requiredIdx)
		{
			return this.Processor.PullInt8(this.Processor.SegData.Value16, (ushort)(TechnologyTable.Prerequisite1Address + requiredIdx + (TechnologyTable.Stride * scienceIdx)));
		}

		private void UnleashBarbarians(short participantIdx, short troopIdx)
		{
			AnnounceHut(participantIdx, "You have unleashed\na horde of barbarians!\n");

			for (int heading = 1; heading <= 8;
				heading += this.Owner.Helpers.ClampValue(4 - this.Owner.World.Participants[participantIdx].TownTally, 1, 4))
			{
				int nearHoriz = (short)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz) + this.Owner.MapOps.Chart.DirectionHoriz(heading));
				int nearVert = (short)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert) + this.Owner.MapOps.Chart.DirectionVert(heading));

				if (this.Owner.MapOps.ObtainTileOwner(nearHoriz, nearVert) != 0xffff ||
					(this.Owner.MapOps.ObtainTileImprovements(nearHoriz, nearVert) & CityImprovement) != 0 ||
					this.Owner.MapOps.ObtainTerrainType(nearHoriz, nearVert) == MapRules.OceanTerrain)
					continue;

				int terrain = this.Owner.MapOps.ObtainTerrainType(nearHoriz, nearVert);
				short kind = (short)(TerrainMoveCost(terrain) > 2 ? 3 : 6);

				ushort barbarianIdx = CreateUnit(0, kind, nearHoriz, nearVert);

				ShowUnitTo(0, (short)barbarianIdx, participantIdx);

				RepaintForHuman(participantIdx, nearHoriz, nearVert);
			}
		}
	}
}
