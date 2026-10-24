using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1866.
	/// </summary>
	public class UnitManagement : GameModule
	{
		private readonly UnitMemory troops;

		public UnitManagement(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>
		/// Puts the units kept in a city of the human player (KeepUnitsInCityList) back on the map, fortified
		/// </summary>
		public void ConstructUnitsFromCityList(short townIdx)
		{
			this.processor.Journal.OpenScope($"ConstructUnitsFromCityList({townIdx})");

			City town = this.owner.World.Towns[townIdx];

			if (town.ParticipantIdx == this.owner.World.PersonParticipantIdx)
			{
				// The original keeps SI and DI for its caller
				ushort savedSource = this.processor.RegSource.Value16;
				ushort savedDest = this.processor.RegDest.Value16;

				this.processor.EmitGlobalWord(0x20f4, 0x1);

				for (int keptIdx = 0; keptIdx < 2; keptIdx++)
				{
					sbyte kept = town.Unfamiliar[keptIdx];

					if (kept == -1)
						continue;

					ushort troopIdx = ConstructUnit(town.ParticipantIdx, (short)(kept & 0x3f), town.Place.X, town.Place.Y);

					if (troopIdx == 0xffff)
						continue;

					byte status = (byte)(this.troops.Pull(town.ParticipantIdx, troopIdx, UnitMemory.Status) | (int)UnitStatusEnum.Fortified);

					if ((kept & KeptVeteran) != 0)
						status |= (byte)UnitStatusEnum.Veteran;

					this.troops.Assign(town.ParticipantIdx, troopIdx, UnitMemory.Status, status);
					town.Unfamiliar[keptIdx] = -1;
				}

				this.processor.EmitGlobalWord(0x20f4, 0x0);
				town.ConditionMarker |= KeptUnitsCondition;

				this.processor.RegSource.Value16 = savedSource;
				this.processor.RegDest.Value16 = savedDest;
			}

			this.processor.Journal.CloseScope("ConstructUnitsFromCityList");
		}

		/// <summary>Bit of a kept unit type (City.Unfamiliar): the unit was a veteran</summary>
		private const sbyte KeptVeteran = 0x40;

		/// <summary>Bit of City.ConditionMarker changed when the kept units are put back or taken away</summary>
		private const byte KeptUnitsCondition = 4;

		/// <summary>
		/// A city of the human player keeps up to two of its fortified home units standing in it: they are taken off the map and
		/// their types (with the veteran bit) are stored in City.Unfamiliar. The first such unit stays on the map.
		/// Losses are not counted meanwhile (0x70d8).
		/// </summary>
		public void KeepUnitsInCityList(short townIdx)
		{
			this.processor.Journal.OpenScope($"KeepUnitsInCityList({townIdx})");

			City town = this.owner.World.Towns[townIdx];
			short participantIdx = town.ParticipantIdx;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				// The original keeps SI and DI for its caller
				ushort savedSource = this.processor.RegSource.Value16;
				ushort savedDest = this.processor.RegDest.Value16;

				ushort countLosses = this.processor.PullGlobalWord(0x70d8);
				this.processor.EmitGlobalWord(0x70d8, 0);
				this.processor.EmitGlobalWord(0x20f4, 0x1);

				bool firstSeen = false;

				for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (!IsFortifiedHomeUnitIn(town, townIdx, participantIdx, troopIdx))
						continue;

					if (!firstSeen)
					{
						firstSeen = true;
						continue;
					}

					int keptIdx = town.Unfamiliar[0] == -1 ? 0 : 1;

					if (keptIdx == 1 && town.Unfamiliar[1] != -1)
						break;

					town.Unfamiliar[keptIdx] = (sbyte)this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

					if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & (int)UnitStatusEnum.Veteran) != 0)
						town.Unfamiliar[keptIdx] |= KeptVeteran;

					EliminateUnit(participantIdx, troopIdx);
				}

				this.processor.EmitGlobalWord(0x20f4, 0x0);
				this.processor.EmitGlobalWord(0x70d8, countLosses);
				town.ConditionMarker &= unchecked((byte)~KeptUnitsCondition);

				this.processor.RegSource.Value16 = savedSource;
				this.processor.RegDest.Value16 = savedDest;
			}

			this.processor.Journal.CloseScope("KeepUnitsInCityList");
		}

		/// <summary>A fortified unit of the city standing in it</summary>
		private bool IsFortifiedHomeUnitIn(City town, short townIdx, short participantIdx, short troopIdx)
		{
			return this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0xff &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz) == (byte)(sbyte)town.Place.X &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert) == (byte)(sbyte)town.Place.Y &&
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown) == townIdx &&
				(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & (int)UnitStatusEnum.Fortified) != 0;
		}

		/// <summary>
		/// A unit has come onto the tile. The human player sees the tiles around it (2 tiles far by the sight of the unit type),
		/// cities next to it give back their kept units, and the unit meets the foreign units around: they see each other, wake up,
		/// lose their "go to" orders, a peace treaty may be offered and the AI players remember the places (SendUnitToStrategicLocation).
		/// </summary>
		/// <param name="marker">Not 0: repaint the tiles around when the human player watches</param>
		public void ShiftUnit(int horizLoc, int vertLoc, short participantIdx, short troopIdx, ushort marker)
		{
			this.processor.Journal.OpenScope($"ShiftUnit({horizLoc}, {vertLoc}, {participantIdx}, {troopIdx}, {marker})");

			// The stack frame of the original stays, see ShiftUnitFrame
			this.processor.EnterFrame(0x16);
			this.processor.PreserveIndexRegisters();

			ShiftUnitFrame frame = new ShiftUnitFrame();

			int startTerrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
			frame.StartTerrain = (ushort)startTerrain;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				RevealSurroundings(participantIdx, troopIdx, horizLoc, vertLoc);
			}

			int startImprovements = this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);
			frame.StartImprovements = (ushort)startImprovements;
			bool repaint = marker != 0;

			for (int heading = 1; heading < 9; heading++)
			{
				MeetNearTile(frame, participantIdx, troopIdx, horizLoc, vertLoc, heading, startTerrain, startImprovements, repaint);
			}

			for (int heading = 9; heading < 25; heading++)
			{
				MeetFarTile(frame, participantIdx, troopIdx, horizLoc, vertLoc, heading, startImprovements, repaint);
			}

			frame.Heading = 25;

			if (participantIdx == this.owner.World.PersonParticipantIdx && repaint)
			{
				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);

				for (int heading = 9; heading < 49; heading++)
				{
					int ringHoriz = RingHoriz(heading, horizLoc);
					int ringVert = RingVert(heading, vertLoc);
					frame.AtRing(ringHoriz, ringVert);

					if (this.owner.MapOps.VerifyMapLimits(ringHoriz, ringVert) != 0 && Sees(participantIdx, ringHoriz, ringVert))
					{
						this.owner.MapOps.RepaintMapTile(ringHoriz, ringVert);
					}
				}

				frame.Heading = 49;
			}

			frame.Leave(this.processor);

			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("ShiftUnit");
		}

		/// <summary>
		/// The values the original leaves in the locals of its stack frame (offsets from BP). ShiftAIUnit, called later by
		/// HandlePlayerTurn from the same stack depth, reads its local 0xc (a city index) on a path before setting it and so
		/// gets the terrain type left here. Kept until the end of the migration, see MIGRATION-BACKLOG.md.
		/// A local that the original sets only on some paths is null when it was not set: the stack keeps its older value.
		/// </summary>
		private sealed class ShiftUnitFrame
		{
			public ushort? StackTroopIdx;
			public ushort? FarKind;
			public ushort? Strategy;
			public ushort? TownIdx;
			public ushort Heading;
			public ushort StartTerrain;
			public ushort StartImprovements;
			public ushort RingHoriz;
			public ushort RingVert;
			public ushort? CityParticipantIdx;
			public ushort? RingImprovements;

			public void AtRing(int ringHoriz, int ringVert)
			{
				this.RingHoriz = (ushort)ringHoriz;
				this.RingVert = (ushort)ringVert;
			}

			public void Leave(CPU processor)
			{
				Emit(processor, 0x2, this.StackTroopIdx);
				Emit(processor, 0x4, this.FarKind);
				Emit(processor, 0x6, this.Strategy);
				Emit(processor, 0x8, this.TownIdx);
				Emit(processor, 0xa, this.Heading);
				Emit(processor, 0xc, this.StartTerrain);
				Emit(processor, 0xe, this.StartImprovements);
				Emit(processor, 0x10, this.RingHoriz);
				Emit(processor, 0x12, this.RingVert);
				Emit(processor, 0x14, this.CityParticipantIdx);
				Emit(processor, 0x16, this.RingImprovements);
			}

			private static void Emit(CPU processor, ushort local, ushort? amount)
			{
				if (amount.HasValue)
				{
					processor.EmitLocalWord(local, amount.Value);
				}
			}
		}

		/// <summary>Bit of Player.Relations: the two players have peace</summary>
		private const int PeaceRelation = 2;

		/// <summary>Bit of the tile improvements: a city</summary>
		private const int CityImprovement = 1;

		/// <summary>The player whose unit ObtainUnitAtTile found last (0xd7f0)</summary>
		private short FoundUnitParticipant => (short)FoundPlayer;

		/// <summary>The AI players see all units (0xd806, a debug option of the original)</summary>
		private bool RevealAll => MapRevealed != 0;

		/// <summary>
		/// Column of a tile around: the step of the direction table (1 - 8 the tiles next to it, 9 - 24 the ring 2 tiles far,
		/// 25 - 48 the ring 3 tiles far), wrapped around the world
		/// </summary>
		private int RingHoriz(int heading, int horizLoc)
		{
			return (short)this.owner.Routing.TunePosition(this.owner.MapOps.Chart.DirectionHoriz(heading) + horizLoc);
		}

		/// <summary>Row of a tile around, see RingHoriz</summary>
		private int RingVert(int heading, int vertLoc)
		{
			return (short)(this.owner.MapOps.Chart.DirectionVert(heading) + vertLoc);
		}

		/// <summary>The player sees the tile (GameState.ChartSight)</summary>
		private bool Sees(short participantIdx, int horizLoc, int vertLoc)
		{
			return (this.owner.World.ChartSight[horizLoc, vertLoc] & (1 << participantIdx)) != 0;
		}

		private void MarkSeen(short participantIdx, int horizLoc, int vertLoc)
		{
			this.owner.World.ChartSight[horizLoc, vertLoc] |= (sbyte)(1 << participantIdx);
		}

		/// <summary>The unit becomes visible to the player</summary>
		private void ShowUnitTo(short participantIdx, short troopIdx, int viewerIdx)
		{
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
				(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | (1 << viewerIdx)));
		}

		/// <summary>The unit forgets its "go to" order</summary>
		private void ClearTravel(short participantIdx, short troopIdx)
		{
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
		}

		private bool AtPeace(short participantIdx, int otherIdx)
		{
			return (this.owner.World.Participants[participantIdx].Relations[otherIdx] & PeaceRelation) != 0;
		}

		/// <summary>
		/// The unit sees the tile 2 tiles far: its unit type has sight 2, and a ship only over the ocean
		/// </summary>
		private bool SeesFarTile(short participantIdx, short troopIdx, int horizLoc, int vertLoc)
		{
			if ((this.troops.KindField(participantIdx, troopIdx, UnitKindTable.Sight) & 2) == 0)
				return false;

			return this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits ||
				this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain;
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

				if (this.owner.MapOps.VerifyMapLimits(ringHoriz, ringVert) == 0)
					continue;

				MarkSeen(participantIdx, ringHoriz, ringVert);

				this.owner.MapOps.KeepKnownTileImprovements(ringHoriz, ringVert);
			}
		}

		/// <summary>
		/// A tile next to the unit: the player sees it (not the barbarians), meets the foreign city and the foreign units on it
		/// </summary>
		private void MeetNearTile(ShiftUnitFrame frame, short participantIdx, short troopIdx, int horizLoc, int vertLoc, int heading,
			int startTerrain, int startImprovements, bool repaint)
		{
			int nearHoriz = RingHoriz(heading, horizLoc);
			int nearVert = RingVert(heading, vertLoc);
			frame.AtRing(nearHoriz, nearVert);

			if (this.owner.MapOps.VerifyMapLimits(nearHoriz, nearVert) == 0)
				return;

			if (participantIdx != 0)
			{
				MarkSeen(participantIdx, nearHoriz, nearVert);
			}

			int nearImprovements = this.owner.MapOps.ObtainTileImprovements(nearHoriz, nearVert);
			frame.RingImprovements = (ushort)nearImprovements;

			if ((nearImprovements & CityImprovement) != 0)
			{
				MeetNearCity(frame, participantIdx, troopIdx, nearHoriz, nearVert);
			}

			ushort nearTroopIdx = this.owner.MapOps.ObtainUnitAtTile(nearHoriz, nearVert);
			frame.StackTroopIdx = nearTroopIdx;

			if (nearTroopIdx != 0xffff && participantIdx != FoundUnitParticipant)
			{
				MeetNearUnit(frame, participantIdx, troopIdx, horizLoc, vertLoc, nearHoriz, nearVert, (short)nearTroopIdx,
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
		private void MeetNearCity(ShiftUnitFrame frame, short participantIdx, short troopIdx, int nearHoriz, int nearVert)
		{
			short cityParticipantIdx = (short)this.owner.MapOps.ObtainTileOwnerBits(nearHoriz, nearVert);

			if (cityParticipantIdx == participantIdx)
				return;

			frame.CityParticipantIdx = (ushort)cityParticipantIdx;

			ushort townIdx = this.owner.Helpers.LocateCityAtPosition(nearHoriz, nearVert);
			frame.TownIdx = townIdx;

			ConstructUnitsFromCityList((short)townIdx);

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.World.Towns[townIdx].ShownExtent = this.owner.World.Towns[townIdx].TrueExtent;
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.LandUnits)
				return;

			ShowUnitTo(participantIdx, troopIdx, cityParticipantIdx);

			if (!AtPeace(participantIdx, cityParticipantIdx))
			{
				this.owner.Bot.SendUnitToStrategicLocation(cityParticipantIdx, (short)nearHoriz, (short)nearVert, 1, 4);
				this.owner.Bot.SendUnitToStrategicLocation(cityParticipantIdx, (short)nearHoriz, (short)nearVert, 2, 2);
			}
		}

		/// <summary>
		/// A foreign stack next to the unit: both see each other (not into a city), the stack wakes up, units lose their
		/// "go to" orders, and two land units meeting on land may make peace or remember each other
		/// </summary>
		private void MeetNearUnit(ShiftUnitFrame frame, short participantIdx, short troopIdx, int horizLoc, int vertLoc, int nearHoriz, int nearVert,
			short nearTroopIdx, int startTerrain, int startImprovements, int nearImprovements)
		{
			if ((startImprovements & CityImprovement) == 0)
			{
				ShowUnitTo(participantIdx, troopIdx, FoundUnitParticipant);
			}

			VisitEachUnitInStack(FoundUnitParticipant, nearTroopIdx, 0x14a2);

			if (this.owner.MapOps.ObtainTerrainType(nearHoriz, nearVert) == MapRules.OceanTerrain || startTerrain != MapRules.OceanTerrain)
			{
				ClearTravel(FoundUnitParticipant, nearTroopIdx);
			}

			// A unit on land next to a foreign ship keeps its order, except a plane
			if (this.owner.MapOps.ObtainTerrainType(nearHoriz, nearVert) != MapRules.OceanTerrain || startTerrain == MapRules.OceanTerrain ||
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
			{
				ClearTravel(participantIdx, troopIdx);
			}

			if ((nearImprovements & CityImprovement) == 0)
			{
				ShowUnitTo(FoundUnitParticipant, nearTroopIdx, participantIdx);
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) != MapRules.OceanTerrain &&
				this.owner.MapOps.ObtainTerrainType(nearHoriz, nearVert) != MapRules.OceanTerrain &&
				this.troops.KindField(FoundUnitParticipant, nearTroopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
			{
				ConfrontNearUnit(frame, participantIdx, troopIdx, horizLoc, vertLoc, nearHoriz, nearVert, nearTroopIdx);
			}

			// A foreign ship next to the unit
			if (this.owner.MapOps.ObtainTerrainType(nearHoriz, nearVert) == MapRules.OceanTerrain && FoundUnitParticipant != 0 &&
				!AtPeace(participantIdx, FoundUnitParticipant))
			{
				this.owner.Bot.SendUnitToStrategicLocation(participantIdx, (short)nearHoriz, (short)nearVert, 3, 2);
			}
		}

		/// <summary>
		/// Two land units meet on land: a peace treaty may be offered; without peace the player remembers the foreign stack
		/// (1 for a diplomat, a fortified defender or a barbarian, else 3) and, unless the unit is a diplomat, the other player
		/// remembers the unit
		/// </summary>
		private void ConfrontNearUnit(ShiftUnitFrame frame, short participantIdx, short troopIdx, int horizLoc, int vertLoc, int nearHoriz, int nearVert,
			short nearTroopIdx)
		{
			this.owner.Politics.BargainPeaceTreaty(participantIdx, FoundUnitParticipant, horizLoc, vertLoc);

			if (!AtPeace(participantIdx, FoundUnitParticipant))
			{
				short strategy = 1;
				short nearParticipantIdx = FoundUnitParticipant;

				if (nearParticipantIdx != 0 && this.troops.Pull(nearParticipantIdx, nearTroopIdx, UnitMemory.Kind) != UnitKindTable.Diplomat)
				{
					bool fortifiedDefender = (this.troops.Pull(nearParticipantIdx, nearTroopIdx, UnitMemory.Status) & (int)UnitStatusEnum.Fortified) != 0 &&
						this.troops.KindField(nearParticipantIdx, nearTroopIdx, UnitKindTable.TroopGroup) == UnitKindTable.DefenceTroops;

					if (!fortifiedDefender)
					{
						strategy = 3;
					}
				}

				frame.Strategy = (ushort)strategy;

				this.owner.Bot.SendUnitToStrategicLocation(participantIdx, (short)nearHoriz, (short)nearVert, 1, strategy);
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != UnitKindTable.Diplomat)
			{
				this.owner.Bot.SendUnitToStrategicLocation(FoundUnitParticipant, (short)horizLoc, (short)vertLoc, 2,
					(short)(participantIdx == 0 ? 1 : 2));
			}
		}

		/// <summary>
		/// A tile 2 tiles far from the unit: the unit sees it and the foreign units on it (by its sight), the foreign units there
		/// see the unit (by their sight), and a city of the human player 2 tiles far sees the unit
		/// </summary>
		private void MeetFarTile(ShiftUnitFrame frame, short participantIdx, short troopIdx, int horizLoc, int vertLoc, int heading,
			int startImprovements, bool repaint)
		{
			int farHoriz = RingHoriz(heading, horizLoc);
			int farVert = RingVert(heading, vertLoc);
			frame.AtRing(farHoriz, farVert);

			if (this.owner.MapOps.VerifyMapLimits(farHoriz, farVert) == 0)
				return;

			ushort farTroopIdx = this.owner.MapOps.ObtainUnitAtTile(farHoriz, farVert);
			frame.StackTroopIdx = farTroopIdx;
			int farImprovements = this.owner.MapOps.ObtainTileImprovements(farHoriz, farVert);
			frame.RingImprovements = (ushort)farImprovements;

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
				frame.FarKind = (ushort)this.troops.TroopKind(FoundUnitParticipant, farTroopIdx);

				SpottedByFarUnit(participantIdx, troopIdx, horizLoc, vertLoc, farHoriz, farVert, (short)farTroopIdx, startImprovements);
			}

			// A city of the human player 2 tiles far, when the human sees the tile of the unit
			if ((farImprovements & CityImprovement) != 0 &&
				(short)this.owner.MapOps.ObtainTileOwnerBits(farHoriz, farVert) == this.owner.World.PersonParticipantIdx &&
				Sees(this.owner.World.PersonParticipantIdx, horizLoc, vertLoc))
			{
				ShowUnitTo(participantIdx, troopIdx, this.owner.World.PersonParticipantIdx);

				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
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
			if (this.owner.MapOps.ObtainTerrainType(farHoriz, farVert) == MapRules.OceanTerrain && FoundUnitParticipant != 0 &&
				!AtPeace(participantIdx, FoundUnitParticipant))
			{
				this.owner.Bot.SendUnitToStrategicLocation(participantIdx, (short)farHoriz, (short)farVert, 3, 2);
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
				this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain);

			if (farSees)
			{
				this.troops.Assign(FoundUnitParticipant, farTroopIdx, UnitMemory.Status,
					(byte)(this.troops.Pull(FoundUnitParticipant, farTroopIdx, UnitMemory.Status) & ~(int)UnitStatusEnum.Sentry));
				ClearTravel(FoundUnitParticipant, farTroopIdx);

				if ((startImprovements & CityImprovement) == 0)
				{
					ShowUnitTo(participantIdx, troopIdx, FoundUnitParticipant);
				}

				// Seen from a ship
				if (this.owner.MapOps.ObtainTerrainType(farHoriz, farVert) == MapRules.OceanTerrain && FoundUnitParticipant != 0 &&
					!AtPeace(participantIdx, FoundUnitParticipant))
				{
					this.owner.Bot.SendUnitToStrategicLocation(FoundUnitParticipant, (short)horizLoc, (short)vertLoc, 3, 2);
				}
			}
			else if (FoundUnitParticipant == this.owner.World.PersonParticipantIdx &&
				(this.owner.MapOps.ObtainTileImprovements(farHoriz, farVert) & CityImprovement) != 0 &&
				Sees(this.owner.World.PersonParticipantIdx, horizLoc, vertLoc))
			{
				ShowUnitTo(participantIdx, troopIdx, FoundUnitParticipant);

				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}
		}

		/// <summary>
		/// Repaints a tile around the unit when the human player moves or the AI moves are shown, and marks it in the map layer
		/// at column 240: 1 over the ocean (terrain colour 1), 2 over land
		/// </summary>
		private void RepaintRingTile(short participantIdx, int ringHoriz, int ringVert)
		{
			if (participantIdx != this.owner.World.PersonParticipantIdx && !RevealAll)
				return;

			this.owner.MapOps.RepaintMapTile(ringHoriz, ringVert);

			ushort colour = this.owner.VideoCard.ObtainDot(2, ringHoriz, ringVert);

			this.owner.Services.AssignDot(2, ringHoriz + 0xf0, ringVert, (ushort)(colour == 1 ? 1 : 2));
		}

		/// <summary>
		/// Blinks the active unit on the map and its dot on the world map, and the cursor frame on a map tile, until a key is
		/// pressed or the mouse is clicked. A unit index from 128 blinks a text instead (no unit).
		/// </summary>
		/// <param name="cursorHoriz">Map position of the cursor frame, 0xffff for none</param>
		/// <param name="cursorVert">Map position of the cursor frame</param>
		public void ChoreographUnitMove(short participantIdx, short troopIdx, ushort cursorHoriz, ushort cursorVert)
		{
			this.processor.Journal.OpenScope($"ChoreographUnitMove({participantIdx}, {troopIdx}, {cursorHoriz}, {cursorVert})");

			// The original keeps SI for its caller
			ushort savedSource = this.processor.RegSource.Value16;

			// Screen position of the cursor frame, -1 when there is none or it is outside the map view
			short frameHoriz = -1;
			short frameVert = 0;

			if (cursorHoriz != 0xffff)
			{
				frameHoriz = (short)((this.owner.Routing.TunePosition((short)cursorHoriz - (short)MapViewHoriz) << 4) + 0x50);
				frameVert = (short)(((cursorVert - MapViewVert) << 4) + 0x8);

				if (frameHoriz < 0x50 || frameHoriz >= 0x140 || frameVert < 0x8 || frameVert > 0xc0)
					frameHoriz = -1;
			}

			// Position of the unit on the world map (top left corner of the screen)
			short worldMapTop = this.processor.PullGlobalSignedWord(0x70ea);
			short dotHoriz = (short)this.owner.Routing.TunePosition(
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) - this.processor.PullGlobalSignedWord(0x6ed6));
			int unitVert = troopIdx < 127 ? this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert) : 0xff;
			short dotVert = (short)(unitVert - worldMapTop);
			bool dotVisible = dotVert >= 0 && dotVert < 50;

			do
			{
				// Hidden phase
				this.owner.Startup.ConcealMouseCursor();

				if (troopIdx >= UnitMemory.TroopTally)
				{
					this.owner.TextPainter.RenderTextToScreen0(0x20f6, 4, 124, 7);
				}
				else
				{
					this.owner.MapOps.RenderMapTile(
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

					if (dotVisible)
					{
						this.owner.Services.AssignDot(dotHoriz, dotVert + 8, 15);
					}
				}

				if (frameHoriz != -1)
				{
					this.owner.Dialogs.RenderBox(frameHoriz, frameVert, 15, 15, 15);
				}

				AwaitBlink();

				// Shown phase
				this.owner.Startup.ConcealMouseCursor();

				if (troopIdx >= UnitMemory.TroopTally)
				{
					this.owner.TextPainter.RenderTextToScreen0(0x2102, 4, 124, 0);
				}
				else
				{
					this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, troopIdx);

					if (dotVisible)
					{
						ushort dotColour = this.owner.MapOps.ObtainTerrainType(
							this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
							this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) == MapRules.OceanTerrain ? (ushort)1 : (ushort)2;

						this.owner.Services.AssignDot(dotHoriz, dotVert + 8, dotColour);
					}
				}

				if (frameHoriz != -1)
				{
					this.owner.Dialogs.RenderBox(frameHoriz, frameVert, 15, 15, 0);
				}

				AwaitBlink();

				this.owner.Startup.ObtainMouseState();
			}
			while (this.owner.Libc.KeyAvailable() == 0 && this.owner.GlobDb3a == 0);

			this.processor.RegSource.Value16 = savedSource;

			this.processor.Journal.CloseScope("ChoreographUnitMove");
		}

		/// <summary>
		/// Shows the mouse pointer and waits 10 timer ticks, 1 when a key is waiting
		/// </summary>
		private void AwaitBlink()
		{
			this.owner.Startup.DisplayMouseCursor();

			this.owner.TextPainter.AwaitTime((short)(this.owner.Libc.KeyAvailable() != 0 ? 1 : 10));
		}

		/// <summary>
		/// Creates a unit of the type on the tile in the first free record (0 - 126). Its home city is the nearest city when that
		/// city belongs to the player. When there is no free record, the human player is told once.
		/// </summary>
		/// <param name="kind">Unit type</param>
		/// <returns>Index of the new unit, or 0xffff</returns>
		public ushort ConstructUnit(short participantIdx, short kind, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ConstructUnit({participantIdx}, {kind}, {horizLoc}, {vertLoc})");

			// The original keeps SI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			short troopIdx = -1;

			for (short idx = 0; idx < UnitMemory.TroopTally - 1; idx++)
			{
				if (this.troops.Pull(participantIdx, idx, UnitMemory.Kind) == 0xff)
				{
					troopIdx = idx;
					break;
				}
			}

			ushort result;

			if (troopIdx == -1)
			{
				if (participantIdx == this.owner.World.PersonParticipantIdx && this.processor.PullGlobalWord(0xd760) == 0)
				{
					DialogStyle = 0x3;
					this.processor.EmitGlobalByte(0xba06, 0x0);

					this.owner.BlockText.DisplayTextDialog(0x2126);

					this.owner.TurnHooks.DisplayDialog(0xba06, 80, 64);

					this.processor.EmitGlobalWord(0xd760, 0x1);
				}

				result = 0xffff;
			}
			else
			{
				SetUpUnit(participantIdx, troopIdx, kind, horizLoc, vertLoc);

				result = (ushort)troopIdx;
			}

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegAcc.Value16 = result;

			this.processor.Journal.CloseScope("ConstructUnit");

			return result;
		}

		/// <summary>
		/// Fills the free record of the new unit, puts it on the map and lets it see around
		/// </summary>
		private void SetUpUnit(short participantIdx, short troopIdx, short kind, int horizLoc, int vertLoc)
		{
			byte participantBit = (byte)(1 << participantIdx);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Horiz, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.FollowingInStack, 0xff);

			this.owner.MapOps.MapAssignCityOwner(participantIdx, horizLoc, vertLoc);
			this.owner.MapOps.SituateUnitOnTile(participantIdx, troopIdx, horizLoc, vertLoc);

			this.owner.World.ChartSight[horizLoc, vertLoc] |= (sbyte)participantBit;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, 0);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Horiz, (byte)horizLoc);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Vert, (byte)vertLoc);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, (byte)kind);

			// A new unit is fully healthy
			this.owner.World.Vitals.Wipe(participantIdx, troopIdx);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants, participantBit);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps,
				(byte)this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway));

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, (byte)this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc));

			byte baseTown = this.troops.Pull(participantIdx, troopIdx, UnitMemory.BaseTown);
			ushort baseOwner = baseTown < 128 ? (ushort)this.owner.World.Towns[baseTown].ParticipantIdx : (ushort)0xffff;

			if (baseOwner != (ushort)participantIdx)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, 0xff);
			}

			// Planes start with one turn of fuel used
			byte fuel = this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps);

			if (fuel != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(fuel - 1));
			}

			this.owner.World.Participants[participantIdx].LiveTroops[kind]++;

			if (this.processor.PullGlobalWord(0x20f4) == 0)
			{
				if (MapRevealed != 0 || participantIdx == this.owner.World.PersonParticipantIdx ||
					(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & (ushort)(1 << this.owner.World.PersonParticipantIdx)) != 0)
				{
					this.owner.MapOps.RepaintMapTile(
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));
				}

				ShiftUnit(horizLoc, vertLoc, participantIdx, troopIdx, 1);
			}

			if (participantIdx == this.owner.World.PersonParticipantIdx && this.owner.World.RoundTally != 0 &&
				this.processor.PullGlobalWord(0x20f4) == 0)
			{
				if (this.owner.World.Participants[participantIdx].TroopTally == 0)
				{
					this.owner.HelpSystem.DisplayHelpPopup(0x210e);
				}

				if (this.owner.World.Participants[participantIdx].TroopTally == 1)
				{
					this.owner.HelpSystem.DisplayHelpPopup(0x211a);
				}
			}
		}

		/// <summary>
		/// Removes the unit. A transport sinking on the ocean takes the land units of its stack along, a carrier its planes.
		/// The tile is repainted when the human player saw the unit.
		/// </summary>
		public void EliminateUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"EliminateUnit({participantIdx}, {troopIdx})");

			// The original keeps SI and DI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			ushort savedDest = this.processor.RegDest.Value16;

			if (this.processor.PullGlobalWord(0x70d8) != 0)
			{
				// Losses are counted (the kind as an unsigned word: an empty record, -1, is not counted)
				ushort lostKind = (ushort)this.troops.TroopKind(participantIdx, troopIdx);

				if (lostKind < this.owner.World.Participants[participantIdx].MissingTroops.Length)
				{
					this.owner.World.Participants[participantIdx].MissingTroops[lostKind]++;
				}
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.FerryRoom) != 0 &&
				this.troops.Following(participantIdx, troopIdx) != -1 &&
				IsOnOcean(participantIdx, troopIdx))
			{
				VisitEachUnitInStack(participantIdx, troopIdx, 0x1610);
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == UnitKindTable.Carrier &&
				this.troops.Following(participantIdx, troopIdx) != -1 &&
				IsOnOcean(participantIdx, troopIdx))
			{
				VisitEachUnitInStack(participantIdx, troopIdx, 0x1676);
			}

			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			if (kind >= 0 && kind < this.owner.World.Participants[participantIdx].LiveTroops.Length)
				this.owner.World.Participants[participantIdx].LiveTroops[kind]--;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			int horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
			int vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);

			this.owner.MapOps.DiscardUnitFromTile(participantIdx, troopIdx, horizLoc, vertLoc);

			if (this.processor.PullGlobalWord(0x20f4) == 0 && this.processor.PullGlobalWord(0x3936) == 0xffff &&
				(MapRevealed != 0 || participantIdx == this.owner.World.PersonParticipantIdx ||
				(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & (ushort)(1 << this.owner.World.PersonParticipantIdx)) != 0))
			{
				this.owner.MapOps.RepaintMapTile(
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));
			}

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegDest.Value16 = savedDest;

			this.processor.Journal.CloseScope("EliminateUnit");
		}

		/// <summary>Does the unit stand on an ocean tile</summary>
		private bool IsOnOcean(short participantIdx, short troopIdx)
		{
			return this.owner.MapOps.ObtainTerrainType(
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)) == MapRules.OceanTerrain;
		}

		/// <summary>
		/// The best defender of the tile of the unit: the stack of the first unit of the player standing there is searched with
		/// LocateBestGuardianStep. A unit alone is its own defender.
		/// </summary>
		/// <returns>Unit index</returns>
		public ushort LocateStackLeader(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"LocateStackLeader({participantIdx}, {troopIdx})");

			// The original keeps SI and DI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			ushort savedDest = this.processor.RegDest.Value16;
			ushort leader = (ushort)troopIdx;

			if (this.troops.Following(participantIdx, troopIdx) != -1)
			{
				this.processor.EmitGlobalWord(0x6530, (ushort)troopIdx);
				this.processor.EmitGlobalWord(0x6534, 0);

				for (short firstIdx = 0; firstIdx < UnitMemory.TroopTally; firstIdx++)
				{
					if (this.troops.Pull(participantIdx, firstIdx, UnitMemory.Kind) != 0xff &&
						this.troops.Pull(participantIdx, firstIdx, UnitMemory.Horiz) == this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz) &&
						this.troops.Pull(participantIdx, firstIdx, UnitMemory.Vert) == this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert))
					{
						VisitEachUnitInStack(participantIdx, firstIdx, 0x1169);
						break;
					}
				}

				leader = this.processor.PullGlobalWord(0x6530);
			}

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegDest.Value16 = savedDest;
			this.processor.RegAcc.Value16 = leader;

			this.processor.Journal.CloseScope("LocateStackLeader");

			return leader;
		}

		/// <summary>
		/// The best defender in the stack of the unit (LocateBestGuardianStep). A unit alone is its own defender.
		/// </summary>
		/// <returns>Unit index</returns>
		public ushort LocateBestGuardianInStack(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"LocateBestGuardianInStack({participantIdx}, {troopIdx})");

			// The original keeps SI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			ushort guardian = (ushort)troopIdx;

			if (this.troops.Following(participantIdx, troopIdx) != -1)
			{
				this.processor.EmitGlobalWord(0x6530, (ushort)troopIdx);
				this.processor.EmitGlobalWord(0x6534, 0xffff);

				VisitEachUnitInStack(participantIdx, troopIdx, 0x1169);

				guardian = this.processor.PullGlobalWord(0x6530);
			}

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegAcc.Value16 = guardian;

			this.processor.Journal.CloseScope("LocateBestGuardianInStack");

			return guardian;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void LocateBestGuardianStep(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"LocateBestGuardianStep({participantIdx}, {troopIdx})");

			// Extract unit parameters
			int unitKind = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(0x81d7 + participantIdx * 0x600 + troopIdx * 0xc));
			int horiz = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(0x81d5 + participantIdx * 0x600 + troopIdx * 0xc));
			int vert = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(0x81d6 + participantIdx * 0x600 + troopIdx * 0xc));
			int terrainType = this.owner.MapOps.ObtainTerrainType(horiz, vert);

			int landGroup = (short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(0x1138 + unitKind * 0x22));
			int baseGuardPower = (short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(0x1140 + unitKind * 0x22));
			int terrainDefenseFactor = (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(0x28f + terrainType * 0x13));

			byte condition = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(0x81d4 + participantIdx * 0x600 + troopIdx * 0xc));
			bool isFortified = (condition & 0x8) != 0;
			bool isVeteran = (condition & 0x20) != 0;

			int score = CombatRules.ComputeGuardianScore(
				unitKind, landGroup, baseGuardPower, terrainType, terrainDefenseFactor, isFortified, isVeteran);

			if (score >= 0)
			{
				int bestScore = (short)this.processor.PullGlobalWord(0x6534);
				if (score > bestScore)
				{
					this.processor.EmitGlobalWord(0x6534, (ushort)score);
					this.processor.EmitGlobalWord(0x6530, (ushort)troopIdx);
				}
			}

			this.processor.Journal.CloseScope("LocateBestGuardianStep");
		}

		/// <summary>
		/// Sum over the stack of the unit, the mode chooses what (TallyUnitsByDomainStep: 0 price, 1 defence, 2 units,
		/// 3 attack, 4 defensive units before the unit). Leaves SI = 0x600 * player like the translated original,
		/// AIPlayer depends on it (see MIGRATION-BACKLOG.md).
		/// </summary>
		public ushort TallyUnitsByDomainInStack(short participantIdx, short troopIdx, ushort mode)
		{
			this.processor.Journal.OpenScope($"TallyUnitsByDomainInStack({participantIdx}, {troopIdx}, {mode})");

			this.processor.EmitGlobalWord(0x6532, 0x0);
			this.processor.EmitGlobalWord(0x6536, mode);
			this.processor.EmitGlobalWord(0x6538, (ushort)troopIdx);

			VisitEachUnitInStack(participantIdx, troopIdx, 0x1280);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6532);

			this.processor.Journal.CloseScope("TallyUnitsByDomainInStack");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Step of TallyUnitsByDomainInStack, adds to the total in 0x6532 what the mode in 0x6536 asks for:
		/// 0 the price, 1 the defence, 2 one per unit, 3 the attack, 4 one per defensive unit with an index below 0x6538.
		/// </summary>
		public void TallyUnitsByDomainStep(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"TallyUnitsByDomainStep({participantIdx}, {troopIdx})");

			switch (this.processor.PullGlobalWord(0x6536))
			{
				case 0:
					AddToGlobal(0x6532, this.troops.KindField(participantIdx, troopIdx, UnitKindTable.Price));
					break;

				case 1:
					AddToGlobal(0x6532, this.troops.KindField(participantIdx, troopIdx, UnitKindTable.GuardPower));
					break;

				case 2:
					AddToGlobal(0x6532, 1);
					break;

				case 3:
					AddToGlobal(0x6532, this.troops.KindField(participantIdx, troopIdx, UnitKindTable.AssaultPower));
					break;

				case 4:
					if (troopIdx < (short)this.processor.PullGlobalWord(0x6538) &&
						this.troops.KindField(participantIdx, troopIdx, UnitKindTable.TroopGroup) == UnitKindTable.DefenceTroops)
					{
						AddToGlobal(0x6532, 1);
					}
					break;
			}

			this.processor.Journal.CloseScope("TallyUnitsByDomainStep");
		}

		/// <summary>Adds to a word of the data segment (with the 16 bit overflow of the original)</summary>
		private void AddToGlobal(ushort address, int amount)
		{
			this.processor.EmitGlobalWord(address, (ushort)(this.processor.PullGlobalWord(address) + amount));
		}

		/// <summary>
		/// Number of the units of the type in the stack of the unit. Leaves SI = 0x600 * player like the translated original (VisitEachUnitInStack).
		/// </summary>
		public ushort TallyUnitsOfTypeInStack(short participantIdx, short troopIdx, ushort arg3)
		{
			this.processor.Journal.OpenScope($"TallyUnitsOfTypeInStack({participantIdx}, {troopIdx}, {arg3})");

			this.processor.EmitGlobalWord(0x653a, arg3);
			this.processor.EmitGlobalWord(0x653c, 0x0);

			VisitEachUnitInStack(participantIdx, troopIdx, 0x135a);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x653c);

			this.processor.Journal.CloseScope("TallyUnitsOfTypeInStack");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Step of TallyUnitsOfTypeInStack: counts in 0x653c the units of the type in 0x653a
		/// </summary>
		public void TallyUnitsOfTypeStep(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"TallyUnitsOfTypeStep({participantIdx}, {troopIdx})");

			if ((ushort)this.troops.TroopKind(participantIdx, troopIdx) == this.processor.PullGlobalWord(0x653a))
			{
				AddToGlobal(0x653c, 1);
			}

			this.processor.Journal.CloseScope("TallyUnitsOfTypeStep");
		}

		/// <summary>
		/// Number of the units with the TroopGroup in the stack of the unit. Leaves SI = 0x600 * player like the translated original (VisitEachUnitInStack).
		/// </summary>
		public ushort TallyUnitsWithFlagInStack(short participantIdx, short troopIdx, ushort arg3)
		{
			this.processor.Journal.OpenScope($"TallyUnitsWithFlagInStack({participantIdx}, {troopIdx}, {arg3})");

			this.processor.EmitGlobalWord(0x653a, arg3);
			this.processor.EmitGlobalWord(0x653c, 0x0);

			VisitEachUnitInStack(participantIdx, troopIdx, 0x13a9);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x653c);

			this.processor.Journal.CloseScope("TallyUnitsWithFlagInStack");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Step of TallyUnitsWithFlagInStack: counts in 0x653c the units whose TroopGroup is the one in 0x653a
		/// </summary>
		public void TallyUnitsWithFlagStep(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"TallyUnitsWithFlagStep({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.TroopGroup) == this.processor.PullGlobalWord(0x653a))
			{
				AddToGlobal(0x653c, 1);
			}

			this.processor.Journal.CloseScope("TallyUnitsWithFlagStep");
		}

		/// <summary>
		/// The free room on the transports of the stack of the unit (TallySpecialUnitsStep).
		/// Leaves SI = 0x600 * player like the translated original (VisitEachUnitInStack).
		/// </summary>
		public ushort TallySpecialUnitsInStack(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"TallySpecialUnitsInStack({participantIdx}, {troopIdx})");

			this.processor.EmitGlobalWord(0x653c, 0x0);

			VisitEachUnitInStack(participantIdx, troopIdx, 0x13f8);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x653c);

			this.processor.Journal.CloseScope("TallySpecialUnitsInStack");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Step of TallySpecialUnitsInStack, the free room on the transports in 0x653c: a land unit takes one place,
		/// a transport adds its room (FerryRoom)
		/// </summary>
		public void TallySpecialUnitsStep(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"TallySpecialUnitsStep({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				AddToGlobal(0x653c, -1);
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.TroopGroup) == UnitKindTable.TransportTroops)
			{
				AddToGlobal(0x653c, this.troops.KindField(participantIdx, troopIdx, UnitKindTable.FerryRoom));
			}

			this.processor.Journal.CloseScope("TallySpecialUnitsStep");
		}

		/// <summary>
		/// Calls a step function for every unit of the stack of the unit, at most for 10 units. The step function is given by
		/// its address in the original code.
		/// </summary>
		public void VisitEachUnitInStack(short participantIdx, short troopIdx, int funcAddr)
		{
			this.processor.Journal.OpenScope($"VisitEachUnitInStack({participantIdx}, {troopIdx}, 0x{funcAddr:x4})");

			Action<short, short> step = StackStep(funcAddr);

			ForEachUnitInStack(participantIdx, troopIdx, (stepParticipantIdx, stepTroopIdx) =>
			{
				// The translated original left BX = 0xc * unit and SI = 0x600 * player before every step, so SI = 0x600 * player
				// after the call. A caller depends on it: AIPlayer divides by SI after TallyUnitsByDomainInStack (L2947),
				// where the DOS code had kept its own SI. Kept until the end of the migration, see MIGRATION-BACKLOG.md.
				this.processor.RegBase.Value16 = (ushort)(UnitMemory.TroopStride * stepTroopIdx);
				this.processor.RegSource.Value16 = (ushort)(UnitMemory.ParticipantStride * stepParticipantIdx);

				step(stepParticipantIdx, stepTroopIdx);
			});

			this.processor.Journal.CloseScope("VisitEachUnitInStack");
		}

		/// <summary>
		/// Calls the action for every unit of the stack of the unit, starting with it, at most for 10 units.
		/// The following unit is read before the action, so the action may remove the unit from the stack.
		/// </summary>
		public void ForEachUnitInStack(short participantIdx, short troopIdx, Action<short, short> visit)
		{
			short present = troopIdx;

			for (int idx = 0; idx < 10; idx++)
			{
				short following = this.troops.Following(participantIdx, present);

				visit(participantIdx, present);

				present = following;

				if (present == -1 || present == troopIdx)
					break;
			}
		}

		/// <summary>The step functions of VisitEachUnitInStack by their address in the original code</summary>
		private Action<short, short> StackStep(int funcAddr)
		{
			return funcAddr switch
			{
				0xf10 => EliminateUnit,
				0x1169 => LocateBestGuardianStep,
				0x1280 => TallyUnitsByDomainStep,
				0x135a => TallyUnitsOfTypeStep,
				0x13a9 => TallyUnitsWithFlagStep,
				0x13f8 => TallySpecialUnitsStep,
				0x14a2 => VerifyUnitTerrainFlag,
				0x14f6 => WipeUnitOrders,
				0x1560 => RestoreUnitOrders,
				0x1593 => RestoreUnitStatus,
				0x1610 => EliminateLandUnit,
				0x1643 => EliminateNonLandUnit,
				0x1676 => EliminateSeaUnit,
				_ => throw new Exception("Unknown function address"),
			};
		}

		/// <summary>
		/// Stack step: wakes the unit (clears Sentry), except a land unit carried on the ocean
		/// </summary>
		public void VerifyUnitTerrainFlag(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"VerifyUnitTerrainFlag({participantIdx}, {troopIdx})");

			bool carried = IsOnOcean(participantIdx, troopIdx) &&
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits;

			if (!carried)
			{
				ClearStatus(participantIdx, troopIdx, UnitStatusEnum.Sentry);
			}

			this.processor.Journal.CloseScope("VerifyUnitTerrainFlag");
		}

		private void ClearStatus(short participantIdx, short troopIdx, UnitStatusEnum flags)
		{
			byte status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(status & ~(int)flags));
		}

		/// <summary>The full moves of the unit type of the unit, in thirds</summary>
		private byte FullSteps(short participantIdx, short troopIdx)
		{
			return (byte)(3 * this.troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally));
		}

		/// <summary>
		/// Stack step: a sleeping (Sentry) unit wakes up with full moves (and full fuel for a plane); every unit loses its
		/// "go to" order
		/// </summary>
		public void WipeUnitOrders(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"WipeUnitOrders({participantIdx}, {troopIdx})");

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & (int)UnitStatusEnum.Sentry) != 0)
			{
				ClearStatus(participantIdx, troopIdx, UnitStatusEnum.Sentry);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, FullSteps(participantIdx, troopIdx));

				ushort roundsAway = this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway);

				if (roundsAway != 0)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(roundsAway - 1));
				}
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

			this.processor.Journal.CloseScope("WipeUnitOrders");
		}

		/// <summary>
		/// Stack step: RestoreUnitStatus for the ships of the stack
		/// </summary>
		public void RestoreUnitOrders(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"RestoreUnitOrders({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits)
			{
				RestoreUnitStatus(participantIdx, troopIdx);
			}

			this.processor.Journal.CloseScope("RestoreUnitOrders");
		}

		/// <summary>
		/// Gives the unit back to the player without orders: settler work stops, a sleeping, fortified or working unit gets
		/// its full moves, only the Veteran (and 0x10) status stays, the "go to" order is removed
		/// </summary>
		public void RestoreUnitStatus(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"RestoreUnitStatus({participantIdx}, {troopIdx})");

			// The original keeps SI for its caller
			ushort savedSource = this.processor.RegSource.Value16;

			byte status = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status);

			if ((status & (int)UnitStatusEnum.SettlerBuildMask) != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
			}

			if ((status & (int)(UnitStatusEnum.SettlerBuildMask | UnitStatusEnum.Fortified | UnitStatusEnum.Sentry)) != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, FullSteps(participantIdx, troopIdx));
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status,
				(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & (int)(UnitStatusEnum.Veteran | UnitStatusEnum.Unknown1)));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, troopIdx);
			}

			this.processor.RegSource.Value16 = savedSource;

			this.processor.Journal.CloseScope("RestoreUnitStatus");
		}

		/// <summary>
		/// Stack step: removes the unit when it is a land unit (cargo of a sinking transport)
		/// </summary>
		public void EliminateLandUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"EliminateLandUnit({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				EliminateUnit(participantIdx, troopIdx);
			}

			this.processor.Journal.CloseScope("EliminateLandUnit");
		}

		/// <summary>
		/// Stack step: removes the unit when it is a plane or a ship
		/// </summary>
		public void EliminateNonLandUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"EliminateNonLandUnit({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.LandUnits)
			{
				EliminateUnit(participantIdx, troopIdx);
			}

			this.processor.Journal.CloseScope("EliminateNonLandUnit");
		}

		/// <summary>
		/// Stack step: removes the unit when it is a plane (LandGroup 1, despite the name: the planes of a sinking carrier)
		/// </summary>
		public void EliminateSeaUnit(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"EliminateSeaUnit({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
			{
				EliminateUnit(participantIdx, troopIdx);
			}

			this.processor.Journal.CloseScope("EliminateSeaUnit");
		}

		/// <summary>
		/// Scrolls the map view (left column 0xd4cc, top row 0xd75e) so that the tile is in its inner part
		/// (columns 2 - 13, rows 2 - 9 of the view); otherwise the view stays
		/// </summary>
		public void PanMapToPosition(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"PanMapToPosition({participantIdx}, {horizLoc}, {vertLoc})");

			int viewLeft = (short)MapViewHoriz;

			// The view may reach over the right edge of the world
			if (horizLoc < 16 && viewLeft > 64)
			{
				horizLoc += MapRules.Width;
			}

			int viewTop = (short)MapViewVert;
			bool outside = horizLoc < viewLeft + 2 || horizLoc > viewLeft + 13 || vertLoc < viewTop + 2 || vertLoc > viewTop + 9;

			if (outside)
			{
				int left = (short)this.owner.Routing.TunePosition(horizLoc - 8);

				this.owner.MapOps.FocusMapView(participantIdx, left, vertLoc - 6);
			}

			this.processor.Journal.CloseScope("PanMapToPosition");
		}

		/// <summary>
		/// Is the move onto the tile blocked by the zone of control: never into a city, otherwise VerifyTileOccupation
		/// </summary>
		/// <returns>1 when a foreign unit stands next to the tile</returns>
		public ushort MayOccupyTile(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"MayOccupyTile({participantIdx}, {horizLoc}, {vertLoc})");

			ushort blocked = (this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0 ? (ushort)0 :
				VerifyTileOccupation(participantIdx, horizLoc, vertLoc);

			this.processor.RegAcc.Value16 = blocked;

			this.processor.Journal.CloseScope("MayOccupyTile");

			return blocked;
		}

		/// <summary>
		/// Does a unit of another player stand on one of the 8 tiles around, on the same kind of tile (land or ocean).
		/// In the turn of the human player that unit, and its city, become visible to the human.
		/// </summary>
		/// <returns>1 when a foreign unit stands next to the tile, 0 otherwise</returns>
		public ushort VerifyTileOccupation(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"VerifyTileOccupation({participantIdx}, {horizLoc}, {vertLoc})");

			// The original keeps SI and DI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			ushort savedDest = this.processor.RegDest.Value16;

			bool onOcean = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain;
			ushort found = 0;

			for (int heading = 1; heading < 9; heading++)
			{
				int nearHoriz = this.owner.Routing.TunePosition(this.owner.MapOps.Chart.DirectionHoriz(heading) + horizLoc);
				int nearVert = (short)(this.owner.MapOps.Chart.DirectionVert(heading) + vertLoc);

				bool nearOcean = this.owner.MapOps.ObtainTerrainType(nearHoriz, nearVert) == MapRules.OceanTerrain;

				if (nearOcean != onOcean)
					continue;

				ushort nearParticipantIdx = this.owner.MapOps.ObtainTileOwner(nearHoriz, nearVert);

				if (nearParticipantIdx == 0xffff || nearParticipantIdx == (ushort)participantIdx)
					continue;

				if (participantIdx == this.owner.World.PersonParticipantIdx)
				{
					RevealToHuman((short)nearParticipantIdx, nearHoriz, nearVert);
				}

				found = 1;
				break;
			}

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegDest.Value16 = savedDest;
			this.processor.RegAcc.Value16 = found;

			this.processor.Journal.CloseScope("VerifyTileOccupation");

			return found;
		}

		/// <summary>
		/// The human player sees the foreign unit on the tile, and the city when there is one
		/// </summary>
		private void RevealToHuman(short participantIdx, int horizLoc, int vertLoc)
		{
			short human = this.owner.World.PersonParticipantIdx;
			byte humanBit = (byte)(1 << human);
			ushort troopIdx = this.owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);

			// The record of 0xffff (no unit) wraps around in the data segment, like in the original
			int shownTo = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants);

			if ((shownTo & (ushort)(1 << human)) == 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | humanBit));
				this.owner.World.ChartSight[horizLoc, vertLoc] |= (sbyte)humanBit;

				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				ushort townIdx = this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc);
				this.owner.World.Towns[townIdx].ShownExtent = this.owner.World.Towns[townIdx].TrueExtent;

				this.owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);
				this.owner.World.ChartSight[horizLoc, vertLoc] |= (sbyte)humanBit;

				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public ushort ContainsAdjacentForeignUnit(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ContainsAdjacentForeignUnit({participantIdx}, {horizLoc}, {vertLoc})");

			// Check the 8 neighboring tiles for any foreign unit (Zone of Control)
			bool found = MovementRules.HasAdjacentForeignUnit(horizLoc, vertLoc, participantIdx, (x, y) =>
			{
				return (int)(short)this.owner.MapOps.ObtainTileOwner(x, y);
			});

			ushort result = (ushort)(found ? 1 : 0);
			this.processor.RegAcc.Value16 = result;

			// Far return
			this.processor.Journal.CloseScope("ContainsAdjacentForeignUnit");

			return result;
		}

		/// <summary>
		/// A unit enters a hut (not a barbarian, not a plane). DrawInt(4) chooses:
		/// 0: an advanced tribe founds a city for the player, when no city is 4 tiles near and the hut layer says so; 50 gold
		/// otherwise, mercenaries near a city;
		/// 1: scrolls of ancient wisdom (a technology) after the first turn up to the year 1000, 50 gold otherwise;
		/// 2: 50 gold;
		/// 3: barbarians around the unit when no city is 4 tiles near and the player has a city, mercenaries otherwise.
		/// </summary>
		public void VisitHut(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"VisitHut({participantIdx}, {troopIdx})");

			// The original keeps SI and DI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			ushort savedDest = this.processor.RegDest.Value16;

			if (participantIdx != 0 &&
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
			{
				int horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
				int vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);
				short townDistance = DistanceToNearestCity(horizLoc, vertLoc);

				switch (this.owner.Libc.Dice.DrawInt(4))
				{
					case 0:
						int hutKind = this.owner.VideoCard.ObtainDot(2, horizLoc + 80, vertLoc) & 0x7;

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

							this.owner.Founding.EstablishCity(participantIdx, 0, horizLoc, vertLoc, 1);

							RepaintForHuman(participantIdx, horizLoc, vertLoc);
						}
						break;

					case 1:
						if (this.owner.World.RoundTally != 0 && this.owner.World.Season <= 1000)
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
						if (townDistance >= 4 && this.owner.World.Participants[participantIdx].TownTally != 0)
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

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegDest.Value16 = savedDest;

			this.processor.Journal.CloseScope("VisitHut");
		}

		/// <summary>Distance to the nearest city, from position -1, -1 when there is no city</summary>
		private short DistanceToNearestCity(int horizLoc, int vertLoc)
		{
			ushort townIdx = this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc);
			bool found = (short)townIdx >= 0 && (short)townIdx < 128;

			return (short)this.owner.Helpers.ObtainDistance(
				horizLoc, found ? this.owner.World.Towns[townIdx].Place.X : -1,
				vertLoc, found ? this.owner.World.Towns[townIdx].Place.Y : -1);
		}

		/// <summary>The text of the hut, shown to the human player</summary>
		private void AnnounceHut(short participantIdx, string text)
		{
			this.owner.Libc.CopyText(0xba06, text);

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}
		}

		private void RepaintForHuman(short participantIdx, int horizLoc, int vertLoc)
		{
			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}
		}

		private void FindMetalDeposits(short participantIdx)
		{
			AnnounceHut(participantIdx, "You have discovered\nvaluable metal deposits\nworth 50$\n");

			this.owner.World.Participants[participantIdx].Gold += 50;
		}

		/// <summary>A legion or a cavalry unit (DrawInt(2)) joins the player</summary>
		private void MeetMercenaries(short participantIdx, int horizLoc, int vertLoc)
		{
			AnnounceHut(participantIdx, "You have discovered\na friendly tribe of\nskilled mercenaries.\n");

			ConstructUnit(participantIdx, (short)(this.owner.Libc.Dice.DrawInt(2) != 0 ? 3 : 6), horizLoc, vertLoc);

			RepaintForHuman(participantIdx, horizLoc, vertLoc);
		}

		/// <summary>Number of technologies</summary>
		private const int ScienceTally = 72;

		/// <summary>
		/// The player learns the first technology from a random one on (at most 999 tries) that it does not know and whose two
		/// required technologies it knows
		/// </summary>
		private void FindAncientScrolls(short participantIdx)
		{
			AnnounceHut(participantIdx, "You have discovered\nscrolls of ancient wisdom.\n");

			int scienceIdx = this.owner.Libc.Dice.DrawInt(ScienceTally);

			for (int tries = 0; tries < 999; tries++)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, (short)scienceIdx) == 0 &&
					this.owner.Advisors.PlayerKnowsTechnology(participantIdx, RequiredScience(scienceIdx, 0)) != 0 &&
					this.owner.Advisors.PlayerKnowsTechnology(participantIdx, RequiredScience(scienceIdx, 1)) != 0)
				{
					this.owner.Advisors.EarnTechnology(participantIdx, (short)scienceIdx, 0);
					break;
				}

				scienceIdx = (scienceIdx + 1) % ScienceTally;
			}
		}

		/// <summary>One of the two technologies required by the technology (bytes 0x4ee and 0x4ef of records of 0x16 bytes), -1 for none</summary>
		private sbyte RequiredScience(int scienceIdx, int requiredIdx)
		{
			return this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(0x4ee + requiredIdx + (0x16 * scienceIdx)));
		}

		/// <summary>
		/// Barbarian units appear on the free land tiles around the unit (no units, no city), every 1 - 4 directions (fewer
		/// for a player with more cities): a legion on mountains (move cost above 2), a cavalry unit elsewhere. The player sees them.
		/// </summary>
		private void UnleashBarbarians(short participantIdx, short troopIdx)
		{
			AnnounceHut(participantIdx, "You have unleashed\na horde of barbarians!\n");

			for (int heading = 1; heading <= 8;
				heading += this.owner.Helpers.VerifyValueSpan(4 - this.owner.World.Participants[participantIdx].TownTally, 1, 4))
			{
				// No wrap around the world, like the original
				int nearHoriz = (short)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz) + this.owner.MapOps.Chart.DirectionHoriz(heading));
				int nearVert = (short)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert) + this.owner.MapOps.Chart.DirectionVert(heading));

				if (this.owner.MapOps.ObtainTileOwner(nearHoriz, nearVert) != 0xffff ||
					(this.owner.MapOps.ObtainTileImprovements(nearHoriz, nearVert) & CityImprovement) != 0 ||
					this.owner.MapOps.ObtainTerrainType(nearHoriz, nearVert) == MapRules.OceanTerrain)
					continue;

				int terrain = this.owner.MapOps.ObtainTerrainType(nearHoriz, nearVert);
				short kind = (short)(TerrainMoveCost(terrain) > 2 ? 3 : 6);

				ushort barbarianIdx = ConstructUnit(0, kind, nearHoriz, nearVert);

				// Also for a unit not made (0xffff), like the original
				ShowUnitTo(0, (short)barbarianIdx, participantIdx);

				RepaintForHuman(participantIdx, nearHoriz, nearVert);
			}
		}

		/// <summary>Move cost of the terrain type (byte of the terrain records of 0x13 bytes at 0x28e)</summary>
		private sbyte TerrainMoveCost(int terrain)
		{
			return (sbyte)this.processor.PullGlobalByte((ushort)(0x28e + (0x13 * terrain)));
		}

		/// <summary>
		/// The attack animation: the unit slides one tile towards the attacked tile in 17 frames, over the next unit of its
		/// stack. Only when the map view shows both tiles; with 0xb278 = 1 the tile of the unit is only drawn.
		/// </summary>
		/// <param name="arg3">Direction of the attack (1 - 8)</param>
		public void ChoreographUnitCombat(short participantIdx, short troopIdx, ushort arg3)
		{
			this.processor.Journal.OpenScope($"ChoreographUnitCombat({participantIdx}, {troopIdx}, {arg3})");

			// The original keeps SI and DI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			ushort savedDest = this.processor.RegDest.Value16;

			int horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
			int vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);

			if (this.processor.PullGlobalWord(0xb278) == 1)
			{
				this.owner.MapOps.RenderMapTile(horizLoc, vertLoc);
			}
			else
			{
				this.owner.Startup.ConcealMouseCursor();

				if (this.owner.MapOps.RenderMapTile(horizLoc, vertLoc) != 0 &&
					this.owner.TurnLogic.VerifyPositionOnScreen(horizLoc, vertLoc) != 0 &&
					this.owner.TurnLogic.VerifyPositionOnScreen(this.owner.MapOps.Chart.DirectionHoriz(arg3) + horizLoc,
						this.owner.MapOps.Chart.DirectionVert(arg3) + vertLoc) != 0)
				{
					AnimateAttack(participantIdx, troopIdx, arg3, horizLoc, vertLoc);
				}

				this.owner.Startup.DisplayMouseCursor();
			}

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegDest.Value16 = savedDest;

			this.processor.Journal.CloseScope("ChoreographUnitCombat");
		}

		/// <summary>
		/// Draws the next unit of the stack on the tile (on the ocean only a ship), keeps the map view in the back screen (0x19d4)
		/// and moves the picture of the unit 16 pixels in the direction, restoring the background after every frame
		/// </summary>
		private void AnimateAttack(short participantIdx, short troopIdx, int heading, int horizLoc, int vertLoc)
		{
			sbyte followingIdx = this.troops.Following(participantIdx, troopIdx);

			if (followingIdx != -1 &&
				(this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) != MapRules.OceanTerrain ||
				this.troops.KindField(participantIdx, followingIdx, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits))
			{
				this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, followingIdx);
			}

			ushort screen = ScreenBox;
			ushort backScreen = BackScreenBox;

			this.owner.VideoCard.RenderImage(screen, 0x50, 0, 0xf0, 0xc8, backScreen, 0x50, 0);

			// Top left pixel of the tile in the map view (16 x 16 pixels a tile, from 80, 8)
			short tileLeft = (short)((this.owner.Routing.TunePosition(horizLoc - (short)MapViewHoriz) << 4) + 0x50);
			short tileTop = (short)(((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert) - MapViewVert) << 4) + 8);

			int stepHoriz = this.owner.MapOps.Chart.DirectionHoriz(heading);
			int stepVert = this.owner.MapOps.Chart.DirectionVert(heading);

			for (int frame = 0; frame <= 16; frame++)
			{
				ushort picture = this.processor.PullGlobalWord(
					(ushort)(0xd4ce + ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) + (participantIdx << 5) + 0x40) << 1)));

				this.owner.Services.RenderPictureToScreen(screen,
					(stepHoriz * frame) + tileLeft + 1, (stepVert * frame) + tileTop + 1, picture);

				this.owner.TextPainter.AwaitTime(1);

				ushort frameLeft = (ushort)((stepHoriz * frame) + tileLeft);
				ushort frameTop = (ushort)((stepVert * frame) + tileTop);

				this.owner.VideoCard.RenderImage(backScreen, frameLeft, frameTop, 0x10, 0x10, screen, frameLeft, frameTop);
			}
		}

		/// <summary>
		/// The list of the units of the stack (at most 12): the human player wakes up units (the cursor keys and Enter or Space,
		/// or a click) and picks the one to go on with. Woken units lose their orders and a settler at work gets its full moves.
		/// </summary>
		/// <returns>The picked unit, the stack leader (LocateStackLeader) when none; also in AX</returns>
		public ushort DisplayBattleScreen(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"DisplayBattleScreen({participantIdx}, {troopIdx})");

			// The original keeps SI and DI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			ushort savedDest = this.processor.RegDest.Value16;

			ushort screen = ScreenBox;
			ushort backScreen = BackScreenBox;

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(screen, 0, 0, 0x140, 0xc8, backScreen, 0, 0);

			ushort picked = LocateStackLeader(participantIdx, troopIdx);
			short[] stack = new short[StackListTally];
			int stackTally = 0;
			short present = troopIdx;

			do
			{
				stack[stackTally] = present;
				present = this.troops.Following(participantIdx, present);
				stackTally++;
			}
			while (stackTally < StackListTally && present != -1 && present != troopIdx);

			short listTop = (short)(0x60 - (stackTally << 3));
			int selected = 0;

			while (true)
			{
				stackTally = RenderStackList(participantIdx, troopIdx, listTop, stackTally);

				int choice = ChooseFromStackList(listTop, stackTally, ref selected);

				if (choice < 0 || choice >= stackTally)
					break;

				picked = (ushort)stack[choice];

				if ((this.troops.Pull(participantIdx, picked, UnitMemory.Status) & (int)UnitStatusEnum.SettlerBuildMask) != 0)
				{
					this.troops.Assign(participantIdx, picked, UnitMemory.LeftoverSteps,
						(byte)(3 * this.troops.KindField(participantIdx, picked, UnitKindTable.ShiftTally)));
				}

				// Only Unknown1 and Veteran stay
				this.troops.Assign(participantIdx, picked, UnitMemory.Status,
					(byte)(this.troops.Pull(participantIdx, picked, UnitMemory.Status) & 0x30));
				ClearTravel(participantIdx, (short)picked);

				if (stackTally <= 1)
					break;
			}

			this.owner.VideoCard.RenderImage(backScreen, 0, 0, 0x140, 0xc8, screen, 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.TurnLogic.AwaitForKey();

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegDest.Value16 = savedDest;
			this.processor.RegAcc.Value16 = picked;

			this.processor.Journal.CloseScope("DisplayBattleScreen");

			return picked;
		}

		/// <summary>Most units shown in the list of DisplayBattleScreen</summary>
		private const int StackListTally = 12;

		/// <summary>
		/// The box with the units of the stack: picture, name with " (V)" for a veteran, home city. The units are counted again
		/// while drawing, the box has the size of the count before.
		/// </summary>
		/// <returns>Number of units in the list</returns>
		private int RenderStackList(short participantIdx, short troopIdx, short listTop, int boxTally)
		{
			this.owner.Dialogs.FloodBoxWithDoubleShadow(100, listTop, 120, (boxTally << 4) + 5, 3);

			int stackTally = 0;
			short present = troopIdx;
			int lineTop = listTop + 5;

			do
			{
				this.owner.MapOps.RenderUnitSprite(participantIdx, present, 106, lineTop);

				this.owner.Libc.CopyText(0xba06, (ushort)(UnitKindTable.Origin + (UnitKindTable.Stride * this.troops.TroopKind(participantIdx, present))));

				if ((this.troops.Pull(participantIdx, present, UnitMemory.Status) & (int)UnitStatusEnum.Veteran) != 0)
				{
					this.owner.Libc.AppendText(0xba06, " (V)");
				}

				this.owner.TextPainter.RenderTextToScreen0(0xba06, 128, lineTop, 15);

				EmptyMessage()
					.City(this.troops.PullSigned(participantIdx, present, UnitMemory.BaseTown));

				this.owner.TextPainter.RenderTextToScreen0(0xba06, 128, lineTop + 8, 14);

				present = this.troops.Following(participantIdx, present);
				lineTop += 0x10;
				stackTally++;
			}
			while (stackTally < StackListTally && present != -1 && present != troopIdx);

			return stackTally;
		}

		/// <summary>
		/// Waits for the choice in the list: with the mouse (0x1a3c) a click on a line or a key; without it the cursor keys move
		/// the highlight, Enter or Space choose, Escape ends. Like the original, the mouse pointer is hidden again except after
		/// Enter or Space.
		/// </summary>
		/// <returns>Line of the list, a negative number or a number past the list when none was chosen</returns>
		private int ChooseFromStackList(short listTop, int stackTally, ref int selected)
		{
			ushort screen = ScreenBox;

			this.owner.Startup.DisplayMouseCursor();

			this.owner.TurnLogic.AwaitForKey();

			if (MousePresent != 0)
			{
				do
				{
					this.owner.Startup.ObtainMouseState();

					if (this.owner.GlobDb3a != 0)
						break;
				}
				while (this.owner.Libc.KeyAvailable() == 0);
			}
			else
			{
				while (true)
				{
					this.owner.VideoCard.SubstituteHue(screen, 105, (selected * 16) + listTop + 4, 112, 17, 3, 11);

					int highlighted = selected;
					ushort key = this.owner.Dialogs.ObtainNavigationKey();

					if (key == 0x4800)
					{
						if (selected != 0)
							selected--;
					}
					else if (key == 0x5000)
					{
						if (stackTally - 1 > selected)
							selected++;
					}

					this.owner.VideoCard.SubstituteHue(screen, 105, (highlighted * 16) + listTop + 4, 112, 17, 11, 3);

					if (key == 0xd || key == 0x20)
						return selected;

					if (key == 0x1b)
						break;
				}
			}

			this.owner.Startup.ConcealMouseCursor();

			if (this.owner.GlobDb3a == 1 && (short)this.owner.GlobDb3c > 0x64 && (short)this.owner.GlobDb3c < 0xdc)
				return (short)(this.owner.GlobDb3e - listTop - 5) / 16;

			return -1;
		}

		/// <summary>
		/// The direction (1 - 8) of the step of the unit towards the nearest own city or carrier (a plane flying home). Tiles with
		/// foreign units and foreign cities are avoided, 0 when no step gets nearer.
		/// </summary>
		/// <returns>Direction, also in AX</returns>
		public ushort LocateNearestFriendlyCity(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"LocateNearestFriendlyCity({participantIdx}, {troopIdx})");

			// The original keeps SI and DI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			ushort savedDest = this.processor.RegDest.Value16;

			// The frame of the original: without an own city or carrier the target stays what was left in the stack there
			this.processor.EnterFrame(0x16);
			int targetHoriz = this.processor.PullLocalSignedWord(0x14);
			int targetVert = this.processor.PullLocalSignedWord(0x16);

			int unitHoriz = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			int unitVert = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
			int nearest = 999;

			foreach (City town in this.owner.World.Towns)
			{
				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
					continue;

				int distance = WrappedChebyshev(unitHoriz - town.Place.X, unitVert - town.Place.Y);

				if (distance < nearest)
				{
					nearest = distance;
					targetHoriz = town.Place.X;
					targetVert = town.Place.Y;
				}
			}

			for (short carrierIdx = 0; carrierIdx < UnitMemory.TroopTally; carrierIdx++)
			{
				if (this.troops.Pull(participantIdx, carrierIdx, UnitMemory.Kind) != UnitKindTable.Carrier)
					continue;

				int carrierHoriz = this.troops.Pull(participantIdx, carrierIdx, UnitMemory.Horiz);
				int carrierVert = this.troops.Pull(participantIdx, carrierIdx, UnitMemory.Vert);
				int distance = WrappedChebyshev(unitHoriz - carrierHoriz, unitVert - carrierVert);

				if (distance < nearest)
				{
					nearest = distance;
					targetHoriz = carrierHoriz;
					targetVert = carrierVert;
				}
			}

			int best = this.owner.Helpers.ObtainDistanceFromOffsets(
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) - targetHoriz,
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert) - targetVert);
			ushort heading = 0;

			for (int direction = 1; direction <= 8; direction++)
			{
				// Like the original, the step doesn't wrap around the map
				int stepHoriz = (short)(unitHoriz + this.owner.MapOps.Chart.DirectionHoriz(direction));
				int stepVert = (short)(unitVert + this.owner.MapOps.Chart.DirectionVert(direction));

				ushort stepOwner = this.owner.MapOps.ObtainTileOwner(stepHoriz, stepVert);

				if (stepOwner != 0xffff && stepOwner != (ushort)participantIdx)
					continue;

				if ((this.owner.MapOps.ObtainTileImprovements(stepHoriz, stepVert) & 1) != 0 &&
					this.owner.MapOps.ObtainTileOwnerBits(stepHoriz, stepVert) != (ushort)participantIdx)
					continue;

				int distance = this.owner.Helpers.ObtainDistanceFromOffsets(stepHoriz - targetHoriz, stepVert - targetVert);

				if (distance < best)
				{
					best = distance;
					heading = (ushort)direction;
				}
			}

			this.processor.LeaveFrame();

			this.processor.RegSource.Value16 = savedSource;
			this.processor.RegDest.Value16 = savedDest;
			this.processor.RegAcc.Value16 = heading;

			this.processor.Journal.CloseScope("LocateNearestFriendlyCity");

			return heading;
		}

		/// <summary>The larger of the two steps, the horizontal one wrapping over 40</summary>
		private static int WrappedChebyshev(int horizStep, int vertStep)
		{
			horizStep = Math.Abs(horizStep);

			if (horizStep > 40)
				horizStep = 80 - horizStep;

			return Math.Max(horizStep, Math.Abs(vertStep));
		}

		/// <summary>
		/// An entry of the replay of the game: the kind and the turn (4 and 12 bits, high byte first), then its bytes; nothing
		/// when the 0x1000 bytes of the replay have no room for all of it
		/// </summary>
		private void WriteReplayEntry(ushort rerunIdx, params byte[] content)
		{
			if (this.owner.World.RerunContentSpan + 2 + content.Length >= 0x1000)
				return;

			ushort interim = (ushort)((rerunIdx << 12) | ((ushort)this.owner.World.RoundTally & 0xfff));
			this.owner.World.RerunContent[this.owner.World.RerunContentSpan++] = (byte)((interim & 0xff00) >> 8);
			this.owner.World.RerunContent[this.owner.World.RerunContentSpan++] = (byte)(interim & 0xff);

			foreach (byte entry in content)
				this.owner.World.RerunContent[this.owner.World.RerunContentSpan++] = entry;
		}

		/// <summary>An entry of the replay with 1 byte(s): the kind rerunIdx, the turn, the bytes</summary>
		public void AttachReplayContent(ushort rerunIdx, byte content1)
		{
			this.processor.Journal.OpenScope($"AttachReplayContent({rerunIdx}, {content1})");
			WriteReplayEntry(rerunIdx, content1);
			this.processor.Journal.CloseScope("AttachReplayContent");
		}

		/// <summary>An entry of the replay with 2 byte(s): the kind rerunIdx, the turn, the bytes</summary>
		public void AttachReplayContent(ushort rerunIdx, byte content1, byte content2)
		{
			this.processor.Journal.OpenScope($"AttachReplayContent({rerunIdx}, {content1}, {content2})");
			WriteReplayEntry(rerunIdx, content1, content2);
			this.processor.Journal.CloseScope("AttachReplayContent");
		}

		/// <summary>An entry of the replay with 3 byte(s): the kind rerunIdx, the turn, the bytes</summary>
		public void AttachReplayContent(ushort rerunIdx, byte content1, byte content2, byte content3)
		{
			this.processor.Journal.OpenScope($"AttachReplayContent({rerunIdx}, {content1}, {content2}, {content3})");
			WriteReplayEntry(rerunIdx, content1, content2, content3);
			this.processor.Journal.CloseScope("AttachReplayContent");
		}

		/// <summary>An entry of the replay with 4 byte(s): the kind rerunIdx, the turn, the bytes</summary>
		public void AttachReplayContent(ushort rerunIdx, byte content1, byte content2, byte content3, byte content4)
		{
			this.processor.Journal.OpenScope($"AttachReplayContent({rerunIdx}, {content1}, {content2}, {content3}, {content4})");
			WriteReplayEntry(rerunIdx, content1, content2, content3, content4);
			this.processor.Journal.CloseScope("AttachReplayContent");
		}

		/// <summary>
		/// Draws the border of the screen (320 x 200) from pieces of 8 x 8 pixels of the picture 0x19e8 (from row 120), in one of
		/// two styles (DrawInt(2)): the four corners, the left and right edges, then the top and bottom edges
		/// </summary>
		public void RenderBusyIndicator()
		{
			this.processor.Journal.OpenScope("RenderBusyIndicator()");

			int styleLeft = (this.owner.Libc.Dice.DrawInt(2) << 5) + 0xc0;

			RenderBorderPiece(styleLeft, 0x78, 0, 0);
			RenderBorderPiece(styleLeft + 8, 0x78, 0x138, 0);
			RenderBorderPiece(styleLeft + 0x18, 0x78, 0x138, 0xc0);
			RenderBorderPiece(styleLeft + 0x10, 0x78, 0, 0xc0);

			for (int row = 1; row < 0x18; row++)
			{
				RenderBorderPiece(styleLeft + 0x18, 0x80, 0, row << 3);
				RenderBorderPiece(styleLeft + 8, 0x80, 0x138, row << 3);
			}

			for (int column = 1; column < 0x27; column++)
			{
				RenderBorderPiece(styleLeft, 0x80, column << 3, 0);
				RenderBorderPiece(styleLeft + 0x10, 0x80, column << 3, 0xc0);
			}

			this.processor.Journal.CloseScope("RenderBusyIndicator");
		}

		private void RenderBorderPiece(int pieceLeft, int pieceTop, int horizLoc, int vertLoc)
		{
			this.owner.VideoCard.RenderImage(PictureScreenBox, pieceLeft, pieceTop, 8, 8,
				ScreenBox, horizLoc, vertLoc);
		}
	}
}
