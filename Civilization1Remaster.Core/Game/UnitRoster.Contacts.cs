namespace Civilization1Remaster.Core.Game
{
	/// <summary>What a unit sees and whom it meets when it moves: the tiles around it, the cities and units near and far</summary>
	public partial class UnitRoster
	{
		/// <summary>
		/// Column of a tile around: the step of the direction table (1 - 8 the tiles next to it, 9 - 24 the ring 2 tiles far,
		/// 25 - 48 the ring 3 tiles far), wrapped around the world
		/// </summary>
		private int RingHoriz(int heading, int horizLoc)
		{
			return (short)this.Owner.Routing.TunePosition(this.Owner.MapOps.Chart.DirectionHoriz(heading) + horizLoc);
		}

		/// <summary>Row of a tile around, see RingHoriz</summary>
		private int RingVert(int heading, int vertLoc)
		{
			return (short)(this.Owner.MapOps.Chart.DirectionVert(heading) + vertLoc);
		}

		/// <summary>The player sees the tile (GameState.ChartSight)</summary>
		private bool Sees(short participantIdx, int horizLoc, int vertLoc)
		{
			return (this.Owner.World.ChartSight[horizLoc, vertLoc] & (1 << participantIdx)) != 0;
		}

		private void MarkSeen(short participantIdx, int horizLoc, int vertLoc)
		{
			this.Owner.World.ChartSight[horizLoc, vertLoc] |= (sbyte)(1 << participantIdx);
		}

		/// <summary>The unit becomes visible to the player</summary>
		private void ShowUnitTo(short participantIdx, short troopIdx, int viewerIdx)
		{
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
				(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | (1 << viewerIdx)));
		}

		private bool AtPeace(short participantIdx, int otherIdx)
		{
			return (this.Owner.World.Participants[participantIdx].Relations[otherIdx] & PeaceRelation) != 0;
		}

		/// <summary>
		/// The unit sees the tile 2 tiles far: its unit type has sight 2, and a ship only over the ocean
		/// </summary>
		private bool SeesFarTile(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			if ((this.troops.KindField(participantIdx, troopIdx, UnitKindTable.Sight) & 2) == 0)
				return false;

			return this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits ||
				this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain;
		}

		/// <summary>
		/// The human player sees the tile of the unit, the tiles next to it and, by the sight of the unit, the tiles 2 tiles far,
		/// with their improvements as they are now
		/// </summary>
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

		/// <summary>
		/// A tile next to the unit: the player sees it (not the barbarians), meets the foreign city and the foreign units on it
		/// </summary>
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

		/// <summary>
		/// A city next to the unit, when it belongs to another player: it gives back its kept units, the human player sees its size,
		/// the city owner sees a land unit and, without peace, its AI remembers the threat
		/// </summary>
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

		/// <summary>
		/// A foreign stack next to the unit: both see each other (not into a city), the stack wakes up, units lose their
		/// "go to" orders, and two land units meeting on land may make peace or remember each other
		/// </summary>
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

			// A unit on land next to a foreign ship keeps its order, except a plane
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

			// A foreign ship next to the unit
			if (this.Owner.MapOps.ObtainTerrainType(nearHoriz, nearVert) == MapRules.OceanTerrain && FoundUnitParticipant != 0 &&
				!AtPeace(participantIdx, FoundUnitParticipant))
			{
				this.Owner.Bot.SendUnitToStrategicLocation(participantIdx, (short)nearHoriz, (short)nearVert, 3, 2);
			}
		}

		/// <summary>
		/// Two land units meet on land: a peace treaty may be offered; without peace the player remembers the foreign stack
		/// (1 for a diplomat, a fortified defender or a barbarian, else 3) and, unless the unit is a diplomat, the other player
		/// remembers the unit
		/// </summary>
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

		/// <summary>
		/// A tile 2 tiles far from the unit: the unit sees it and the foreign units on it (by its sight), the foreign units there
		/// see the unit (by their sight), and a city of the human player 2 tiles far sees the unit
		/// </summary>
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

			// A city of the human player 2 tiles far, when the human sees the tile of the unit
			if ((farImprovements & CityImprovement) != 0 &&
				(short)this.Owner.MapOps.ObtainTileOwnerBits(farHoriz, farVert) == this.Owner.World.PersonParticipantIdx &&
				Sees(this.Owner.World.PersonParticipantIdx, horizLoc, vertLoc))
			{
				ShowUnitTo(participantIdx, troopIdx, this.Owner.World.PersonParticipantIdx);

				this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}
		}

		/// <summary>
		/// The unit sees the tile 2 tiles far (not the barbarians) and the foreign stack on it, except a submarine (not in a city)
		/// </summary>
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

			// A foreign ship
			if (this.Owner.MapOps.ObtainTerrainType(farHoriz, farVert) == MapRules.OceanTerrain && FoundUnitParticipant != 0 &&
				!AtPeace(participantIdx, FoundUnitParticipant))
			{
				this.Owner.Bot.SendUnitToStrategicLocation(participantIdx, (short)farHoriz, (short)farVert, 3, 2);
			}
		}

		/// <summary>
		/// The foreign stack 2 tiles far sees the unit by its sight (a ship only when the unit is on the ocean): it wakes up, loses
		/// its "go to" order, sees the unit (not in a city) and remembers it. Otherwise only a human city there sees the unit.
		/// </summary>
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

				// Seen from a ship
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

		/// <summary>
		/// Repaints a tile around the unit when the human player moves or the AI moves are shown, and marks it in the map layer
		/// at column 240: 1 over the ocean (terrain colour 1), 2 over land
		/// </summary>
		private void RepaintRingTile(short participantIdx, int ringHoriz, int ringVert)
		{
			if (participantIdx != this.Owner.World.PersonParticipantIdx && !RevealAll)
				return;

			this.Owner.MapOps.RepaintMapTile(ringHoriz, ringVert);

			ushort colour = this.Owner.VideoCard.ObtainDot(2, ringHoriz, ringVert);

			this.Owner.Services.AssignDot(2, ringHoriz + 0xf0, ringVert, (ushort)(colour == 1 ? 1 : 2));
		}

		/// <summary>
		/// The human player sees the foreign unit on the tile, and the city when there is one
		/// </summary>
		private void RevealToHuman(short participantIdx, int horizLoc, int vertLoc)
		{
			short human = this.Owner.World.PersonParticipantIdx;
			byte humanBit = (byte)(1 << human);
			ushort troopIdx = this.Owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);

			// The record of 0xffff (no unit) wraps around in the data segment, like in the original
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
