namespace Civilization1Remaster.Core.Game
{
	public partial class UnitRoster
	{
		private int RingHoriz(int heading, int horizLoc)
		{
			return (short)this.Owner.Routing.TunePosition(this.Owner.MapOps.Chart.DirectionHoriz(heading) + horizLoc);
		}

		private int RingVert(int heading, int vertLoc)
		{
			return (short)(this.Owner.MapOps.Chart.DirectionVert(heading) + vertLoc);
		}

		private bool Sees(short participantIdx, int horizLoc, int vertLoc)
		{
			return (this.Owner.World.ChartSight[horizLoc, vertLoc] & (1 << participantIdx)) != 0;
		}

		private void MarkSeen(short participantIdx, int horizLoc, int vertLoc)
		{
			this.Owner.World.ChartSight[horizLoc, vertLoc] |= (sbyte)(1 << participantIdx);
		}

		private void ShowUnitTo(short participantIdx, short troopIdx, int viewerIdx)
		{
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
				(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | (1 << viewerIdx)));
		}

		private bool AtPeace(short participantIdx, int otherIdx)
		{
			return (this.Owner.World.Participants[participantIdx].Relations[otherIdx] & PeaceRelation) != 0;
		}

		private bool SeesFarTile(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			if ((this.troops.KindField(participantIdx, troopIdx, UnitKindTable.Sight) & 2) == 0)
				return false;

			return this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits ||
				this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain;
		}

		private void RevealSurroundings(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			for (int heading = 0; heading < 25; heading++)
			{
				int ringHoriz = RingHoriz(heading, horizLoc);
				int ringVert = RingVert(heading, vertLoc);

				if (heading >= 9 && !SeesFarTile(participantIdx, troopIdx, ringHoriz, ringVert))
					continue;

				if (this.Owner.MapOps.CheckMapLimits(ringHoriz, ringVert) == 0)
					continue;

				MarkSeen(participantIdx, ringHoriz, ringVert);

				this.Owner.MapOps.KeepKnownTileImprovements(ringHoriz, ringVert);
			}
		}

		private void MeetNearTile(short participantIdx, short troopIdx, int horizLoc, int vertLoc, int heading,
			int startTerrain, int startImprovements, bool repaint)
		{
			int nearHoriz = RingHoriz(heading, horizLoc);
			int nearVert = RingVert(heading, vertLoc);

			if (this.Owner.MapOps.CheckMapLimits(nearHoriz, nearVert) == 0)
				return;

			if (participantIdx != 0)
			{
				MarkSeen(participantIdx, nearHoriz, nearVert);
			}

			int nearImprovements = this.Owner.MapOps.ObtainTileImprovements(nearHoriz, nearVert);

			if ((nearImprovements & CityImprovement) != 0)
			{
				MeetNearCity(participantIdx, troopIdx, nearHoriz, nearVert);
			}

			ushort nearTroopIdx = this.Owner.MapOps.ObtainUnitAtTile(nearHoriz, nearVert);

			if (nearTroopIdx != 0xffff && participantIdx != FoundUnitParticipant)
			{
				MeetNearUnit(participantIdx, troopIdx, horizLoc, vertLoc, nearHoriz, nearVert, (short)nearTroopIdx,
					startTerrain, startImprovements, nearImprovements);
			}

			if (repaint)
			{
				RepaintRingTile(participantIdx, nearHoriz, nearVert);
			}
		}

		private void MeetNearCity(short participantIdx, short troopIdx, int nearHoriz, int nearVert)
		{
			short cityParticipantIdx = (short)this.Owner.MapOps.ObtainTileOwnerBits(nearHoriz, nearVert);

			if (cityParticipantIdx == participantIdx)
				return;

			ushort townIdx = this.Owner.Helpers.FindCityAtPosition(nearHoriz, nearVert);

			CreateUnitsFromCityList((short)townIdx);

			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.World.Towns[townIdx].ShownExtent = this.Owner.World.Towns[townIdx].TrueExtent;
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.LandUnits)
				return;

			ShowUnitTo(participantIdx, troopIdx, cityParticipantIdx);

			if (!AtPeace(participantIdx, cityParticipantIdx))
			{
				this.Owner.Bot.SendUnitToStrategicLocation(cityParticipantIdx, (short)nearHoriz, (short)nearVert, 1, 4);
				this.Owner.Bot.SendUnitToStrategicLocation(cityParticipantIdx, (short)nearHoriz, (short)nearVert, 2, 2);
			}
		}

		private void MeetNearUnit(short participantIdx, short troopIdx, int horizLoc, int vertLoc, int nearHoriz, int nearVert,
			short nearTroopIdx, int startTerrain, int startImprovements, int nearImprovements)
		{
			if ((startImprovements & CityImprovement) == 0)
			{
				ShowUnitTo(participantIdx, troopIdx, FoundUnitParticipant);
			}

			VisitEachUnitInStack(FoundUnitParticipant, nearTroopIdx, 0x14a2);

			if (this.Owner.MapOps.ObtainTerrainType(nearHoriz, nearVert) == MapRules.OceanTerrain || startTerrain != MapRules.OceanTerrain)
			{
				ClearTravel(FoundUnitParticipant, nearTroopIdx);
			}

			if (this.Owner.MapOps.ObtainTerrainType(nearHoriz, nearVert) != MapRules.OceanTerrain || startTerrain == MapRules.OceanTerrain ||
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
			{
				ClearTravel(participantIdx, troopIdx);
			}

			if ((nearImprovements & CityImprovement) == 0)
			{
				ShowUnitTo(FoundUnitParticipant, nearTroopIdx, participantIdx);
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) != MapRules.OceanTerrain &&
				this.Owner.MapOps.ObtainTerrainType(nearHoriz, nearVert) != MapRules.OceanTerrain &&
				this.troops.KindField(FoundUnitParticipant, nearTroopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
			{
				ConfrontNearUnit(participantIdx, troopIdx, horizLoc, vertLoc, nearHoriz, nearVert, nearTroopIdx);
			}

			if (this.Owner.MapOps.ObtainTerrainType(nearHoriz, nearVert) == MapRules.OceanTerrain && FoundUnitParticipant != 0 &&
				!AtPeace(participantIdx, FoundUnitParticipant))
			{
				this.Owner.Bot.SendUnitToStrategicLocation(participantIdx, (short)nearHoriz, (short)nearVert, 3, 2);
			}
		}

		private void ConfrontNearUnit(short participantIdx, short troopIdx, int horizLoc, int vertLoc, int nearHoriz, int nearVert,
			short nearTroopIdx)
		{
			this.Owner.Politics.BargainPeaceTreaty(participantIdx, FoundUnitParticipant, horizLoc, vertLoc);

			if (!AtPeace(participantIdx, FoundUnitParticipant))
			{
				short strategy = 1;
				short nearParticipantIdx = FoundUnitParticipant;

				if (nearParticipantIdx != 0 && this.troops.Pull(nearParticipantIdx, nearTroopIdx, UnitMemory.Kind) != UnitKindTable.Diplomat)
				{
					bool fortifiedDefender = (this.troops.Pull(nearParticipantIdx, nearTroopIdx, UnitMemory.Status) & (int)UnitStatus.Fortified) != 0 &&
						this.troops.KindField(nearParticipantIdx, nearTroopIdx, UnitKindTable.TroopGroup) == UnitKindTable.DefenceTroops;

					if (!fortifiedDefender)
					{
						strategy = 3;
					}
				}

				this.Owner.Bot.SendUnitToStrategicLocation(participantIdx, (short)nearHoriz, (short)nearVert, 1, strategy);
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != UnitKindTable.Diplomat)
			{
				this.Owner.Bot.SendUnitToStrategicLocation(FoundUnitParticipant, (short)horizLoc, (short)vertLoc, 2,
					(short)(participantIdx == 0 ? 1 : 2));
			}
		}

		private void MeetFarTile(short participantIdx, short troopIdx, int horizLoc, int vertLoc, int heading,
			int startImprovements, bool repaint)
		{
			int farHoriz = RingHoriz(heading, horizLoc);
			int farVert = RingVert(heading, vertLoc);

			if (this.Owner.MapOps.CheckMapLimits(farHoriz, farVert) == 0)
				return;

			ushort farTroopIdx = this.Owner.MapOps.ObtainUnitAtTile(farHoriz, farVert);
			int farImprovements = this.Owner.MapOps.ObtainTileImprovements(farHoriz, farVert);

			if (SeesFarTile(participantIdx, troopIdx, farHoriz, farVert))
			{
				SpotFarUnit(participantIdx, farHoriz, farVert, farTroopIdx, farImprovements);

				if (repaint)
				{
					RepaintRingTile(participantIdx, farHoriz, farVert);
				}
			}

			if (farTroopIdx != 0xffff && participantIdx != FoundUnitParticipant &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != UnitKindTable.Submarine)
			{
				SpottedByFarUnit(participantIdx, troopIdx, horizLoc, vertLoc, farHoriz, farVert, (short)farTroopIdx, startImprovements);
			}

			if ((farImprovements & CityImprovement) != 0 &&
				(short)this.Owner.MapOps.ObtainTileOwnerBits(farHoriz, farVert) == this.Owner.World.PersonParticipantIdx &&
				Sees(this.Owner.World.PersonParticipantIdx, horizLoc, vertLoc))
			{
				ShowUnitTo(participantIdx, troopIdx, this.Owner.World.PersonParticipantIdx);

				this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}
		}

		private void SpotFarUnit(short participantIdx, int farHoriz, int farVert, ushort farTroopIdx, int farImprovements)
		{
			if (participantIdx != 0)
			{
				MarkSeen(participantIdx, farHoriz, farVert);
			}

			if (farTroopIdx == 0xffff || participantIdx == FoundUnitParticipant ||
				this.troops.Pull(FoundUnitParticipant, farTroopIdx, UnitMemory.Kind) == UnitKindTable.Submarine)
				return;

			if ((farImprovements & CityImprovement) == 0)
			{
				ShowUnitTo(FoundUnitParticipant, (short)farTroopIdx, participantIdx);
			}

			if (this.Owner.MapOps.ObtainTerrainType(farHoriz, farVert) == MapRules.OceanTerrain && FoundUnitParticipant != 0 &&
				!AtPeace(participantIdx, FoundUnitParticipant))
			{
				this.Owner.Bot.SendUnitToStrategicLocation(participantIdx, (short)farHoriz, (short)farVert, 3, 2);
			}
		}

		private void SpottedByFarUnit(short participantIdx, short troopIdx, int horizLoc, int vertLoc, int farHoriz, int farVert,
			short farTroopIdx, int startImprovements)
		{
			bool farSees = (this.troops.KindField(FoundUnitParticipant, farTroopIdx, UnitKindTable.Sight) & 2) != 0 &&
				(this.troops.KindField(FoundUnitParticipant, farTroopIdx, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits ||
				this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain);

			if (farSees)
			{
				this.troops.Assign(FoundUnitParticipant, farTroopIdx, UnitMemory.Status,
					(byte)(this.troops.Pull(FoundUnitParticipant, farTroopIdx, UnitMemory.Status) & ~(int)UnitStatus.Sentry));
				ClearTravel(FoundUnitParticipant, farTroopIdx);

				if ((startImprovements & CityImprovement) == 0)
				{
					ShowUnitTo(participantIdx, troopIdx, FoundUnitParticipant);
				}

				if (this.Owner.MapOps.ObtainTerrainType(farHoriz, farVert) == MapRules.OceanTerrain && FoundUnitParticipant != 0 &&
					!AtPeace(participantIdx, FoundUnitParticipant))
				{
					this.Owner.Bot.SendUnitToStrategicLocation(FoundUnitParticipant, (short)horizLoc, (short)vertLoc, 3, 2);
				}
			}
			else if (FoundUnitParticipant == this.Owner.World.PersonParticipantIdx &&
				(this.Owner.MapOps.ObtainTileImprovements(farHoriz, farVert) & CityImprovement) != 0 &&
				Sees(this.Owner.World.PersonParticipantIdx, horizLoc, vertLoc))
			{
				ShowUnitTo(participantIdx, troopIdx, FoundUnitParticipant);

				this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}
		}

		private void RepaintRingTile(short participantIdx, int ringHoriz, int ringVert)
		{
			if (participantIdx != this.Owner.World.PersonParticipantIdx && !RevealAll)
				return;

			this.Owner.MapOps.RepaintMapTile(ringHoriz, ringVert);

			ushort colour = this.Owner.VideoCard.ObtainDot(2, ringHoriz, ringVert);

			this.Owner.Services.AssignDot(2, ringHoriz + 0xf0, ringVert, (ushort)(colour == 1 ? 1 : 2));
		}

		private void RevealToHuman(short participantIdx, int horizLoc, int vertLoc)
		{
			short human = this.Owner.World.PersonParticipantIdx;
			byte humanBit = (byte)(1 << human);
			ushort troopIdx = this.Owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);

			int shownTo = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants);

			if ((shownTo & (ushort)(1 << human)) == 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | humanBit));
				this.Owner.World.ChartSight[horizLoc, vertLoc] |= (sbyte)humanBit;

				this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}

			if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				ushort townIdx = this.Owner.Helpers.FindCityAtPosition(horizLoc, vertLoc);
				this.Owner.World.Towns[townIdx].ShownExtent = this.Owner.World.Towns[townIdx].TrueExtent;

				this.Owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);
				this.Owner.World.ChartSight[horizLoc, vertLoc] |= (sbyte)humanBit;

				this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}
		}

		private void RepaintForHuman(short participantIdx, int horizLoc, int vertLoc)
		{
			if (participantIdx == this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}
		}
	}
}
