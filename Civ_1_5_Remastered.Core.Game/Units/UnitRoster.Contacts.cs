namespace Civ_1_5_Remastered.Core.Game.Units
{
	public partial class UnitRoster
	{
		private int RingHorizontal(int heading, int horizontalLocation)
		{
			return (short)this.Owner.Routing.TunePosition(this.Owner.MapOperations.Chart.DirectionHorizontal(heading) + horizontalLocation);
		}

		private int RingVertical(int heading, int verticalLocation)
		{
			return (short)(this.Owner.MapOperations.Chart.DirectionVertical(heading) + verticalLocation);
		}

		private bool Sees(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			return (this.Owner.World.ChartSight[horizontalLocation, verticalLocation] & (1 << participantIndex)) != 0;
		}

		private void MarkSeen(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			this.Owner.World.ChartSight[horizontalLocation, verticalLocation] |= (sbyte)(1 << participantIndex);
		}

		private void ShowUnitTo(short participantIndex, short troopIndex, int viewerIndex)
		{
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.ShownToParticipants,
				(byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.ShownToParticipants) | (1 << viewerIndex)));
		}

		private bool AtPeace(short participantIndex, int otherIndex)
		{
			return (this.Owner.World.Participants[participantIndex].Relations[otherIndex] & PeaceRelation) != 0;
		}

		private bool SeesFarTile(short participantIndex, short troopIndex, int horizontalLocation, int verticalLocation)
		{
			if ((this.troops.KindField(participantIndex, troopIndex, UnitKindTable.Sight) & 2) == 0)
			{
				return false;
			}

			return this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits ||
				this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == MapRules.OceanTerrain;
		}

		private void RevealSurroundings(short participantIndex, short troopIndex, int horizontalLocation, int verticalLocation)
		{
			for (int heading = 0; heading < 25; heading++)
			{
				int ringHorizontal = RingHorizontal(heading, horizontalLocation);
				int ringVertical = RingVertical(heading, verticalLocation);

				if (heading >= 9 && !SeesFarTile(participantIndex, troopIndex, ringHorizontal, ringVertical))
				{
					continue;
				}

				if (this.Owner.MapOperations.CheckMapLimits(ringHorizontal, ringVertical) == 0)
				{
					continue;
				}

				MarkSeen(participantIndex, ringHorizontal, ringVertical);

				this.Owner.MapOperations.KeepKnownTileImprovements(ringHorizontal, ringVertical);
			}
		}

		private void MeetNearTile(short participantIndex, short troopIndex, int horizontalLocation, int verticalLocation, int heading,
			int startTerrain, int startImprovements, bool repaint)
		{
			int nearHorizontal = RingHorizontal(heading, horizontalLocation);
			int nearVertical = RingVertical(heading, verticalLocation);

			if (this.Owner.MapOperations.CheckMapLimits(nearHorizontal, nearVertical) == 0)
			{
				return;
			}

			if (participantIndex != 0)
			{
				MarkSeen(participantIndex, nearHorizontal, nearVertical);
			}

			int nearImprovements = this.Owner.MapOperations.ObtainTileImprovements(nearHorizontal, nearVertical);

			if ((nearImprovements & CityImprovement) != 0)
			{
				MeetNearCity(participantIndex, troopIndex, nearHorizontal, nearVertical);
			}

			ushort nearTroopIndex = this.Owner.MapOperations.ObtainUnitAtTile(nearHorizontal, nearVertical);

			if (nearTroopIndex != 0xffff && participantIndex != FoundUnitParticipant)
			{
				MeetNearUnit(participantIndex, troopIndex, horizontalLocation, verticalLocation, nearHorizontal, nearVertical, (short)nearTroopIndex,
					startTerrain, startImprovements, nearImprovements);
			}

			if (repaint)
			{
				RepaintRingTile(participantIndex, nearHorizontal, nearVertical);
			}
		}

		private void MeetNearCity(short participantIndex, short troopIndex, int nearHorizontal, int nearVertical)
		{
			short cityParticipantIndex = (short)this.Owner.MapOperations.ObtainTileOwnerBits(nearHorizontal, nearVertical);

			if (cityParticipantIndex == participantIndex)
			{
				return;
			}

			ushort townIndex = this.Owner.Helpers.FindCityAtPosition(nearHorizontal, nearVertical);

			CreateUnitsFromCityList((short)townIndex);

			if (participantIndex == this.Owner.World.PersonParticipantIndex)
			{
				this.Owner.World.Towns[townIndex].ShownExtent = this.Owner.World.Towns[townIndex].TrueExtent;
			}

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) != UnitKindTable.LandUnits)
			{
				return;
			}

			ShowUnitTo(participantIndex, troopIndex, cityParticipantIndex);

			if (!AtPeace(participantIndex, cityParticipantIndex))
			{
				this.Owner.Computer.SendUnitToStrategicLocation(cityParticipantIndex, (short)nearHorizontal, (short)nearVertical, 1, 4);
				this.Owner.Computer.SendUnitToStrategicLocation(cityParticipantIndex, (short)nearHorizontal, (short)nearVertical, 2, 2);
			}
		}

		private void MeetNearUnit(short participantIndex, short troopIndex, int horizontalLocation, int verticalLocation, int nearHorizontal, int nearVertical,
			short nearTroopIndex, int startTerrain, int startImprovements, int nearImprovements)
		{
			if ((startImprovements & CityImprovement) == 0)
			{
				ShowUnitTo(participantIndex, troopIndex, FoundUnitParticipant);
			}

			VisitEachUnitInStack(FoundUnitParticipant, nearTroopIndex, 0x14a2);

			if (this.Owner.MapOperations.ObtainTerrainType(nearHorizontal, nearVertical) == MapRules.OceanTerrain || startTerrain != MapRules.OceanTerrain)
			{
				ClearTravel(FoundUnitParticipant, nearTroopIndex);
			}

			if (this.Owner.MapOperations.ObtainTerrainType(nearHorizontal, nearVertical) != MapRules.OceanTerrain || startTerrain == MapRules.OceanTerrain ||
				this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
			{
				ClearTravel(participantIndex, troopIndex);
			}

			if ((nearImprovements & CityImprovement) == 0)
			{
				ShowUnitTo(FoundUnitParticipant, nearTroopIndex, participantIndex);
			}

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) != MapRules.OceanTerrain &&
				this.Owner.MapOperations.ObtainTerrainType(nearHorizontal, nearVertical) != MapRules.OceanTerrain &&
				this.troops.KindField(FoundUnitParticipant, nearTroopIndex, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
			{
				ConfrontNearUnit(participantIndex, troopIndex, horizontalLocation, verticalLocation, nearHorizontal, nearVertical, nearTroopIndex);
			}

			if (this.Owner.MapOperations.ObtainTerrainType(nearHorizontal, nearVertical) == MapRules.OceanTerrain && FoundUnitParticipant != 0 &&
				!AtPeace(participantIndex, FoundUnitParticipant))
			{
				this.Owner.Computer.SendUnitToStrategicLocation(participantIndex, (short)nearHorizontal, (short)nearVertical, 3, 2);
			}
		}

		private void ConfrontNearUnit(short participantIndex, short troopIndex, int horizontalLocation, int verticalLocation, int nearHorizontal, int nearVertical,
			short nearTroopIndex)
		{
			this.Owner.Politics.BargainPeaceTreaty(participantIndex, FoundUnitParticipant, horizontalLocation, verticalLocation);

			if (!AtPeace(participantIndex, FoundUnitParticipant))
			{
				short strategy = 1;
				short nearParticipantIndex = FoundUnitParticipant;

				if (nearParticipantIndex != 0 && this.troops.Pull(nearParticipantIndex, nearTroopIndex, UnitMemory.Kind) != UnitKindTable.Diplomat)
				{
					bool fortifiedDefender = (this.troops.Pull(nearParticipantIndex, nearTroopIndex, UnitMemory.Status) & (int)UnitStatus.Fortified) != 0 &&
						this.troops.KindField(nearParticipantIndex, nearTroopIndex, UnitKindTable.TroopGroup) == UnitKindTable.DefenceTroops;

					if (!fortifiedDefender)
					{
						strategy = 3;
					}
				}

				this.Owner.Computer.SendUnitToStrategicLocation(participantIndex, (short)nearHorizontal, (short)nearVertical, 1, strategy);
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != UnitKindTable.Diplomat)
			{
				this.Owner.Computer.SendUnitToStrategicLocation(FoundUnitParticipant, (short)horizontalLocation, (short)verticalLocation, 2,
					(short)(participantIndex == 0 ? 1 : 2));
			}
		}

		private void MeetFarTile(short participantIndex, short troopIndex, int horizontalLocation, int verticalLocation, int heading,
			int startImprovements, bool repaint)
		{
			int farHorizontal = RingHorizontal(heading, horizontalLocation);
			int farVertical = RingVertical(heading, verticalLocation);

			if (this.Owner.MapOperations.CheckMapLimits(farHorizontal, farVertical) == 0)
			{
				return;
			}

			ushort farTroopIndex = this.Owner.MapOperations.ObtainUnitAtTile(farHorizontal, farVertical);
			int farImprovements = this.Owner.MapOperations.ObtainTileImprovements(farHorizontal, farVertical);

			if (SeesFarTile(participantIndex, troopIndex, farHorizontal, farVertical))
			{
				SpotFarUnit(participantIndex, farHorizontal, farVertical, farTroopIndex, farImprovements);

				if (repaint)
				{
					RepaintRingTile(participantIndex, farHorizontal, farVertical);
				}
			}

			if (farTroopIndex != 0xffff && participantIndex != FoundUnitParticipant &&
				this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != UnitKindTable.Submarine)
			{
				SpottedByFarUnit(participantIndex, troopIndex, horizontalLocation, verticalLocation, farHorizontal, farVertical, (short)farTroopIndex, startImprovements);
			}

			if ((farImprovements & CityImprovement) != 0 &&
				(short)this.Owner.MapOperations.ObtainTileOwnerBits(farHorizontal, farVertical) == this.Owner.World.PersonParticipantIndex &&
				Sees(this.Owner.World.PersonParticipantIndex, horizontalLocation, verticalLocation))
			{
				ShowUnitTo(participantIndex, troopIndex, this.Owner.World.PersonParticipantIndex);

				this.Owner.MapOperations.RepaintMapTile(horizontalLocation, verticalLocation);
			}
		}

		private void SpotFarUnit(short participantIndex, int farHorizontal, int farVertical, ushort farTroopIndex, int farImprovements)
		{
			if (participantIndex != 0)
			{
				MarkSeen(participantIndex, farHorizontal, farVertical);
			}

			if (farTroopIndex == 0xffff || participantIndex == FoundUnitParticipant ||
				this.troops.Pull(FoundUnitParticipant, farTroopIndex, UnitMemory.Kind) == UnitKindTable.Submarine)
			{
				return;
			}

			if ((farImprovements & CityImprovement) == 0)
			{
				ShowUnitTo(FoundUnitParticipant, (short)farTroopIndex, participantIndex);
			}

			if (this.Owner.MapOperations.ObtainTerrainType(farHorizontal, farVertical) == MapRules.OceanTerrain && FoundUnitParticipant != 0 &&
				!AtPeace(participantIndex, FoundUnitParticipant))
			{
				this.Owner.Computer.SendUnitToStrategicLocation(participantIndex, (short)farHorizontal, (short)farVertical, 3, 2);
			}
		}

		private void SpottedByFarUnit(short participantIndex, short troopIndex, int horizontalLocation, int verticalLocation, int farHorizontal, int farVertical,
			short farTroopIndex, int startImprovements)
		{
			bool farSees = (this.troops.KindField(FoundUnitParticipant, farTroopIndex, UnitKindTable.Sight) & 2) != 0 &&
				(this.troops.KindField(FoundUnitParticipant, farTroopIndex, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits ||
				this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == MapRules.OceanTerrain);

			if (farSees)
			{
				this.troops.Assign(FoundUnitParticipant, farTroopIndex, UnitMemory.Status,
					(byte)(this.troops.Pull(FoundUnitParticipant, farTroopIndex, UnitMemory.Status) & ~(int)UnitStatus.Sentry));
				ClearTravel(FoundUnitParticipant, farTroopIndex);

				if ((startImprovements & CityImprovement) == 0)
				{
					ShowUnitTo(participantIndex, troopIndex, FoundUnitParticipant);
				}

				if (this.Owner.MapOperations.ObtainTerrainType(farHorizontal, farVertical) == MapRules.OceanTerrain && FoundUnitParticipant != 0 &&
					!AtPeace(participantIndex, FoundUnitParticipant))
				{
					this.Owner.Computer.SendUnitToStrategicLocation(FoundUnitParticipant, (short)horizontalLocation, (short)verticalLocation, 3, 2);
				}
			}
			else if (FoundUnitParticipant == this.Owner.World.PersonParticipantIndex &&
				(this.Owner.MapOperations.ObtainTileImprovements(farHorizontal, farVertical) & CityImprovement) != 0 &&
				Sees(this.Owner.World.PersonParticipantIndex, horizontalLocation, verticalLocation))
			{
				ShowUnitTo(participantIndex, troopIndex, FoundUnitParticipant);

				this.Owner.MapOperations.RepaintMapTile(horizontalLocation, verticalLocation);
			}
		}

		private void RepaintRingTile(short participantIndex, int ringHorizontal, int ringVertical)
		{
			if (participantIndex != this.Owner.World.PersonParticipantIndex && !RevealAll)
			{
				return;
			}

			this.Owner.MapOperations.RepaintMapTile(ringHorizontal, ringVertical);

			ushort colour = this.Owner.VideoCard.ObtainDot(2, ringHorizontal, ringVertical);

			this.Owner.Services.AssignDot(2, ringHorizontal + 0xf0, ringVertical, (ushort)(colour == 1 ? 1 : 2));
		}

		private void RevealToHuman(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			short human = this.Owner.World.PersonParticipantIndex;
			byte humanBit = (byte)(1 << human);
			ushort troopIndex = this.Owner.MapOperations.ObtainUnitAtTile(horizontalLocation, verticalLocation);

			int shownTo = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.ShownToParticipants);

			if ((shownTo & (ushort)(1 << human)) == 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.ShownToParticipants) | humanBit));
				this.Owner.World.ChartSight[horizontalLocation, verticalLocation] |= (sbyte)humanBit;

				this.Owner.MapOperations.RepaintMapTile(horizontalLocation, verticalLocation);
			}

			if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) != 0)
			{
				ushort townIndex = this.Owner.Helpers.FindCityAtPosition(horizontalLocation, verticalLocation);
				this.Owner.World.Towns[townIndex].ShownExtent = this.Owner.World.Towns[townIndex].TrueExtent;

				this.Owner.MapOperations.KeepKnownTileImprovements(horizontalLocation, verticalLocation);
				this.Owner.World.ChartSight[horizontalLocation, verticalLocation] |= (sbyte)humanBit;

				this.Owner.MapOperations.RepaintMapTile(horizontalLocation, verticalLocation);
			}
		}

		private void RepaintForHuman(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			if (participantIndex == this.Owner.World.PersonParticipantIndex)
			{
				this.Owner.MapOperations.RepaintMapTile(horizontalLocation, verticalLocation);
			}
		}
	}
}
