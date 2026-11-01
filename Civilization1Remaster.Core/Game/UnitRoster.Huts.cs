namespace Civilization1Remaster.Core.Game
{
	/// <summary>The huts: what a unit finds in them</summary>
	public partial class UnitRoster
	{
		/// <inheritdoc/>
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

		/// <summary>Distance to the nearest city, from position -1, -1 when there is no city</summary>
		private short DistanceToNearestCity(int horizLoc, int vertLoc)
		{
			ushort townIdx = this.Owner.Helpers.FindNearestCity(horizLoc, vertLoc);
			bool found = (short)townIdx >= 0 && (short)townIdx < 128;

			return (short)this.Owner.Helpers.ObtainDistance(
				horizLoc, found ? this.Owner.World.Towns[townIdx].Place.X : -1,
				vertLoc, found ? this.Owner.World.Towns[townIdx].Place.Y : -1);
		}

		/// <summary>The text of the hut, shown to the human player</summary>
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

		/// <summary>A legion or a cavalry unit (DrawInt(2)) joins the player</summary>
		private void MeetMercenaries(short participantIdx, int horizLoc, int vertLoc)
		{
			AnnounceHut(participantIdx, "You have discovered\na friendly tribe of\nskilled mercenaries.\n");

			CreateUnit(participantIdx, (short)(this.Owner.Libc.Dice.DrawInt(2) != 0 ? 3 : 6), horizLoc, vertLoc);

			RepaintForHuman(participantIdx, horizLoc, vertLoc);
		}

		/// <summary>
		/// The player learns the first technology from a random one on (at most 999 tries) that it does not know and whose two
		/// required technologies it knows
		/// </summary>
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

		/// <summary>One of the two technologies required by the technology (bytes 0x4ee and 0x4ef of records of 0x16 bytes), -1 for none</summary>
		private sbyte RequiredScience(int scienceIdx, int requiredIdx)
		{
			return this.Processor.PullInt8(this.Processor.SegData.Value16, (ushort)(TechnologyTable.Prerequisite1Address + requiredIdx + (TechnologyTable.Stride * scienceIdx)));
		}

		/// <summary>
		/// Barbarian units appear on the free land tiles around the unit (no units, no city), every 1 - 4 directions (fewer
		/// for a player with more cities): a legion on mountains (move cost above 2), a cavalry unit elsewhere. The player sees them.
		/// </summary>
		private void UnleashBarbarians(short participantIdx, short troopIdx)
		{
			AnnounceHut(participantIdx, "You have unleashed\na horde of barbarians!\n");

			for (int heading = 1; heading <= 8;
				heading += this.Owner.Helpers.ClampValue(4 - this.Owner.World.Participants[participantIdx].TownTally, 1, 4))
			{
				// No wrap around the world, like the original
				int nearHoriz = (short)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz) + this.Owner.MapOps.Chart.DirectionHoriz(heading));
				int nearVert = (short)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert) + this.Owner.MapOps.Chart.DirectionVert(heading));

				if (this.Owner.MapOps.ObtainTileOwner(nearHoriz, nearVert) != 0xffff ||
					(this.Owner.MapOps.ObtainTileImprovements(nearHoriz, nearVert) & CityImprovement) != 0 ||
					this.Owner.MapOps.ObtainTerrainType(nearHoriz, nearVert) == MapRules.OceanTerrain)
					continue;

				int terrain = this.Owner.MapOps.ObtainTerrainType(nearHoriz, nearVert);
				short kind = (short)(TerrainMoveCost(terrain) > 2 ? 3 : 6);

				ushort barbarianIdx = CreateUnit(0, kind, nearHoriz, nearVert);

				// Also for a unit not made (0xffff), like the original
				ShowUnitTo(0, (short)barbarianIdx, participantIdx);

				RepaintForHuman(participantIdx, nearHoriz, nearVert);
			}
		}
	}
}
