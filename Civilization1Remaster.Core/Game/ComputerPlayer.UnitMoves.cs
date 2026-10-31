using System;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>The move of an AI unit: the scores of the tiles around it and the choice of the heading</summary>
	partial class ComputerPlayer
	{
		public ushort ShiftAIUnit(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ShiftAIUnit({participantIdx}, {troopIdx})");

			ushort move;

			// (the barbarians leave the accumulator as their move leaves it)
			if (participantIdx == 0)
			{
				move = ManageBarbarianUnit(participantIdx, troopIdx);
			}
			else
			{
				// The locals of this call (UnitLook), new ones every time
				UnitLook outer = this.look;
				this.look = new UnitLook();

				try
				{
					move = DecideAIUnitMove(participantIdx, troopIdx);
				}
				finally
				{
					this.look = outer;
				}
			}

			return move;
		}

		/// <summary>
		/// The move of a unit of a computer player (not a barbarian), as ShiftAIUnit answers it: planes, a look at the
		/// surroundings, nuclear weapons, diplomats, exploring, the defence of a city, the strategic locations, the transports
		/// and attacking ships, pillaging, settlers, the decisions before a fight and at last the best of the 8 directions.
		/// The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private ushort DecideAIUnitMove(short participantIdx, short troopIdx)
		{
			// Planes out of fuel or moves, the fighters after a bomber of the human player or a city to land in
			ushort? airAnswer = PlanAirUnit(participantIdx, troopIdx);

			if (airAnswer != null)
				return airAnswer.Value;

			SurveyAIUnit(participantIdx, troopIdx);

			if (this.look.Kind == 0x19)
				return AimNuclearWeapon(participantIdx, troopIdx);

			if (this.look.Kind == UnitKindTable.Diplomat)
			{
				// A diplomat with a destination goes on, one near another player's city goes there, one in an own city is given up
				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
					return 0;

				GameModel.State.City town = this.Owner.World.Towns[this.look.NearestCity];

				if (town.ParticipantIdx != participantIdx)
				{
					// (DI and DX as the original leaves them: the unit and the high word of its multiplications)
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)town.Place.X);
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)town.Place.Y);

					return 0;
				}

				// (the look goes on for the unit given up)
				this.Owner.UnitOps.DestroyUnit(participantIdx, troopIdx);
			}

			ushort? explored = ExploreAround(participantIdx, troopIdx);

			if (explored != null)
				return explored.Value;

			switch (GuardCity(participantIdx, troopIdx))
			{
				case CityGuard.Stays:
					return 0x66;

				case CityGuard.NotDefender:
					// A unit (not settlers) with another unit somewhere near goes to a strategic location
					if ((short)this.look.UnitDistance > 0 && this.look.Role != 0)
					{
						SendUnitToStrategicLocation(participantIdx, (short)this.look.Horiz, (short)this.look.Vert, 2, 4);
						return 0x20;
					}

					break;
			}

			// The attacking ships (role 3) and the transports (role 5) after a look at their stack: a transport with too few
			// land units on board goes to a coastal city, one next to land where its units may land unloads them there, else a
			// destination is chosen
			if (this.look.Role == 5 || this.look.Role == 3)
			{
				SurveyStack(participantIdx, troopIdx);

				if (!ChooseStagingCity(participantIdx, troopIdx))
				{
					ushort? landing = PlanTransportLanding(participantIdx, troopIdx);

					if (landing != null)
						return landing.Value;

					ChooseTransportDestination(participantIdx, troopIdx);
				}
			}

			// A slow unit next to a human city, not at peace, on an irrigated or mined tile of it
			if (StandsOnHumanFields(participantIdx, troopIdx))
				return 0x50;

			if (this.look.Role == 0)
			{
				// Settlers improve the land around their city, build roads or look for a place for a city
				ushort? settlerAnswer = DirectSettlers(participantIdx, troopIdx);

				if (settlerAnswer != null)
					return settlerAnswer.Value;
			}

			// A unit with a destination goes there; defenders stay, settlers found or join cities
			ushort? stayAnswer = DecideBeforeFight(participantIdx, troopIdx);

			if (stayAnswer != null)
				return stayAnswer.Value;

			// Each of the 8 directions around: some are no target, the others are scored (the tile, the unit there and the
			// surroundings), the best direction so far is kept
			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				if (MayAimAtDirection(participantIdx, troopIdx) && ScoreTile(participantIdx, troopIdx) && WeighUnitAtTile(participantIdx, troopIdx))
					ScoreSurroundings(participantIdx, troopIdx);
			}

			// The heading: the best direction (none for an attacker short of moves next to an enemy)
			return ChooseHeading(participantIdx, troopIdx);
		}

		/// <summary>
		/// The tile of the direction 0x40 (0x26 / 0x2e, owner 0x58, terrain 0x38, unit 0x24: a diplomat stands back behind
		/// another unit of its stack) is no target (false) outside the map, at sea for a land unit, as a free tile it may
		/// occupy for a land unit that may stay (0x4c) on land, with a foreign unit for a land unit at sea, on land for a
		/// ship (but a foreign unit there for a ship at sea, not a submarine) and with an own stack of 2 or more units.
		/// The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private bool MayAimAtDirection(short participantIdx, short troopIdx)
		{
			ushort direction = (ushort)(this.look.Direction << 1);

			this.look.TileHoriz = (ushort)this.Owner.Routing.TunePosition((short)DirectionHorizAt(direction) +
				(short)this.look.Horiz);
			this.look.TileVert = (ushort)(DirectionVertAt(direction) + this.look.Vert);

			short horizLoc = (short)this.look.TileHoriz;
			short vertLoc = (short)this.look.TileVert;

			if (this.Owner.MapOps.CheckMapLimits(horizLoc, vertLoc) == 0)
				return false;

			this.look.TileUnitOwner = this.Owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc);
			this.look.TileTerrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			if (this.look.TileTerrain == MapRules.OceanTerrain &&
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				return false;
			}

			this.look.TileUnit = this.Owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);

			if (this.look.TileUnit != 0xffff)
			{
				ushort owner = Search.FoundPlayer;

				if (this.troops.Pull(owner, this.look.TileUnit, UnitMemory.Kind) == UnitKindTable.Diplomat)
				{
					// The next unit of the stack that is not a diplomat
					this.look.TileOwner = this.look.TileUnit;

					do
					{
						this.look.TileOwner = (ushort)this.troops.PullSigned(owner, this.look.TileOwner, UnitMemory.FollowingInStack);
					}
					while (this.look.TileOwner != 0xffff && this.look.TileOwner != this.look.TileUnit &&
						this.troops.Pull(owner, this.look.TileOwner, UnitMemory.Kind) == UnitKindTable.Diplomat);

					if (this.look.TileOwner != 0xffff)
						this.look.TileUnit = this.look.TileOwner;
				}
			}

			bool free = this.look.TileUnit == 0xffff;

			if (this.look.MayStay != 0 && this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				free && this.Owner.UnitOps.MayOccupyTile(participantIdx, horizLoc, vertLoc) != 0 &&
				this.troops.TroopKind(participantIdx, troopIdx) < UnitKindTable.Diplomat && this.look.Terrain != MapRules.OceanTerrain)
			{
				return false;
			}

			if (this.look.Terrain == MapRules.OceanTerrain &&
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				!free && this.look.TileUnitOwner != (ushort)participantIdx)
			{
				return false;
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits &&
				this.look.TileTerrain != MapRules.OceanTerrain)
			{
				// A ship next to land: only a foreign unit, not for a submarine, and only from the sea
				if (free || this.look.TileUnitOwner == (ushort)participantIdx || this.look.Kind == UnitKindTable.Submarine ||
					this.look.Terrain != MapRules.OceanTerrain)
				{
					return false;
				}
			}

			return free || this.look.TileUnitOwner != (ushort)participantIdx ||
				(short)this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, (short)this.look.TileUnit, 2) < 2;
		}

		/// <summary>
		/// The first score 0x10 of the tile of the direction 0x40 (0x26 / 0x2e, terrain 0x38, unit 0x24 of the player 0x58):
		/// settlers start from 0 (on a ship they skip a tile they may not occupy: false); other units unseen (byte 9) on land a
		/// die of 3 less the defence (twice it for a defender) of the terrain, else a die of 5 and with an own stack there its
		/// strength (StackStrengthRules: the tally + 1, or with ClassicStackStrength SI as the second tally leaves
		/// it), or 4 times the defence of the terrain;
		/// planes a die of 3. The farther the direction from the heading of the unit (0x5e), the less. The locals of
		/// ShiftAIUnit are used as in the original.
		/// </summary>
		private bool ScoreTile(short participantIdx, short troopIdx)
		{
			ushort role = this.look.Role;

			if (role == 0)
			{
				this.look.Score = 0;

				if (this.look.Terrain == MapRules.OceanTerrain &&
					this.Owner.UnitOps.CheckTileOccupation(participantIdx, (short)this.look.TileHoriz, (short)this.look.TileVert) != 0)
				{
					return false;
				}
			}
			else
			{
				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) == 0 && this.look.Terrain != MapRules.OceanTerrain)
				{
					short die = (short)this.Owner.Libc.Dice.DrawInt(3);
					short defence = role == 1 ? (short)(TerrainByte(0x28e) << 1) : TerrainByte(0x28f);
					this.look.Score = (ushort)(die - defence);
				}
				else
				{
					this.look.Score = (ushort)this.Owner.Libc.Dice.DrawInt(5);

					if (this.look.TileUnit != 0xffff && this.look.TileUnitOwner == (ushort)participantIdx)
					{
						short stack = (short)this.look.TileUnit;

						if (role == 1)
						{
							ushort tallyPlusOne = (ushort)(this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, stack, 2) + 1);
							ushort strength = (ushort)(this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, stack, 1) << 2);
							ushort share = DivideWords(strength,
								StackStrengthRules.Divisor(tallyPlusOne, (ushort)(participantIdx * UnitMemory.ParticipantStride), this.Owner.TurnHooks.ClassicStackStrength));
							this.look.Score = (ushort)(this.look.Score + share);
						}

						if (role == 2)
						{
							ushort tallyPlusOne = (ushort)(this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, stack, 1) + 1);
							ushort strength = (ushort)(this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, stack, 3) << 1);
							ushort share = DivideWords(strength,
								StackStrengthRules.Divisor(tallyPlusOne, (ushort)(participantIdx * UnitMemory.ParticipantStride), this.Owner.TurnHooks.ClassicStackStrength));
							this.look.Score = (ushort)(this.look.Score + share);
						}
					}
					else
					{
						this.look.Score = (ushort)(this.look.Score + (TerrainByte(0x28f) << 2));
					}
				}

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
					this.look.Score = (ushort)this.Owner.Libc.Dice.DrawInt(3);
			}

			byte heading = this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHeading);
			this.look.HeadingLow = heading;

			if (heading != 0xff)
			{
				short away = this.Owner.Libc.AbsoluteValue((short)((sbyte)heading - (short)this.look.Direction));

				if (away > 4)
					away = (short)(8 - away);

				this.look.Away = (ushort)away;
				this.look.Score = (ushort)(this.look.Score - ((away * away) << 1));
			}

			return true;
		}

		/// <summary>A signed byte of the terrain 0x38 in the table of terrains (0x13 bytes each)</summary>
		private short TerrainByte(ushort offset)
		{
			return (sbyte)this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)(this.look.TileTerrain * 0x13 + offset));
		}

		/// <summary>
		/// The unit at the tile of the direction counts in the score 0x10 (0x1c marks a foreign one): a free tile +20 with a
		/// hut, 999 for a foreign city; an own unit less its defence. Settlers next to a foreign unit and units at peace with
		/// it skip the tile (false); at peace a militia under approach 1 may buy a lone veteran unit of a player at war with
		/// the human player (who has cities on the continent) outside a city. At war the best defender of the stack (a plane
		/// outside a city only for kind 0xe) is weighed by the battle (0x5a: the result times the units of the stack over the
		/// price, x3 in a city, x3 for a militia under approach 1, x2 for a defender with a weak stack): from 6 (12 for the
		/// barbarians) 4 times that, else -999 and a militia on land may buy a lone unit on a good defensive terrain.
		/// The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private bool WeighUnitAtTile(short participantIdx, short troopIdx)
		{
			short horizLoc = (short)this.look.TileHoriz;
			short vertLoc = (short)this.look.TileVert;
			this.look.Foreign = 0;

			if (this.look.TileUnit == 0xffff)
			{
				if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0 && this.look.TileUnitOwner != (ushort)participantIdx)
					this.look.Score = 999;

				if (this.Owner.MapOps.ContainsHut(this.look.TileTerrain, horizLoc, vertLoc) != 0)
					this.look.Score = (ushort)(this.look.Score + 20);

				return true;
			}

			if (this.look.TileUnitOwner == (ushort)participantIdx)
			{
				this.look.Score = (ushort)(this.look.Score - this.troops.KindField(participantIdx, troopIdx, UnitKindTable.GuardPower));
				return true;
			}

			this.look.Foreign = 1;
			ushort role = this.look.Role;

			if (role == 0)
				return false;

			if ((this.Owner.World.Participants[participantIdx].Relations[this.look.TileUnitOwner] & 2) != 0)
			{
				BuyUnitAtPeace(participantIdx, troopIdx, horizLoc, vertLoc);
				return false;
			}

			short enemy = (short)this.look.TileUnitOwner;
			this.look.TileUnit = this.Owner.UnitOps.FindBestGuardianInStack(enemy, (short)this.look.TileUnit);

			bool airGuardian = this.troops.KindField(enemy, (short)this.look.TileUnit, UnitKindTable.LandGroup) == UnitKindTable.AirUnits;

			if (airGuardian && this.troops.TroopKind(participantIdx, troopIdx) != 0xe &&
				(this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0)
			{
				return false;
			}

			this.look.BattleScore = this.Owner.Combat.SettleBattle(participantIdx, troopIdx,
				(short)this.look.TileUnitOwner, (short)this.look.TileUnit, 0);

			ushort losses = MultiplyWords((ushort)(this.Owner.UnitOps.CountUnitsByDomainInStack((short)this.look.TileUnitOwner,
				(short)this.look.TileUnit, 0) + 1), this.look.BattleScore);
			this.look.BattleScore = losses;
			this.look.BattleScore = DivideWords(losses, this.troops.KindField(participantIdx, troopIdx, UnitKindTable.Price));

			if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
				this.look.BattleScore = (ushort)(this.look.BattleScore * 3);

			if (role == 1 && this.look.Approach == 1)
				this.look.BattleScore = (ushort)(this.look.BattleScore * 3);

			if (role == 1)
			{
				short attack = (short)this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, troopIdx, 3);

				if ((short)(this.troops.KindField(participantIdx, troopIdx, UnitKindTable.AssaultPower) << 1) < attack)
					this.look.BattleScore = (ushort)(this.look.BattleScore << 1);
			}

			short enough = (short)(participantIdx == 0 ? 0xc : 6);

			if (enough <= (short)this.look.BattleScore)
			{
				this.look.Score = (ushort)(this.look.Score + (this.look.BattleScore << 2));
				return true;
			}

			this.look.Score = (ushort)(this.look.Score - 999);

			if (role == 1 && this.look.Approach == 1 &&
				this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				(this.troops.Pull(this.look.TileUnitOwner, this.look.TileUnit, UnitMemory.Status) & 0x28) != 0 &&
				TerrainByte(0x28f) >= 4 &&
				(short)this.Owner.UnitOps.CountUnitsByDomainInStack((short)this.look.TileUnitOwner, (short)this.look.TileUnit, 2) < 2 &&
				this.Owner.Libc.Dice.DrawInt(4) == 0 &&
				(this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0)
			{
				this.Owner.Diplomats.BuyUnit((short)this.look.TileUnitOwner, (short)this.look.TileUnit, participantIdx);
			}

			return true;
		}

		/// <summary>
		/// At peace with the player of the unit at the tile: a militia under approach 1 (a land unit) may buy it when it is a
		/// veteran (status bit 3), alone, outside a city, its player at war with the human player while this one is at peace
		/// with him and he has cities on the continent (0x1a); seven times in eight only next to a city of the human player.
		/// </summary>
		private void BuyUnitAtPeace(short participantIdx, short troopIdx, short horizLoc, short vertLoc)
		{
			if (this.look.Role != 1 || this.look.Approach != 1)
				return;

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.LandUnits)
				return;

			short other = (short)this.look.TileUnitOwner;
			short stack = (short)this.look.TileUnit;
			short person = this.Owner.World.PersonParticipantIdx;

			if ((this.troops.Pull(other, stack, UnitMemory.Status) & 8) == 0 ||
				(this.Owner.World.Participants[participantIdx].Relations[person] & 3) != 1 ||
				(this.Owner.World.Participants[other].Relations[person] & 3) == 1 ||
				this.Owner.World.Participants[person].Landmasses[this.look.Continent].TownTally == 0 ||
				(short)this.Owner.UnitOps.CountUnitsByDomainInStack(other, stack, 2) >= 2)
			{
				return;
			}

			if (this.Owner.Libc.Dice.DrawInt(8) != 0 && this.Owner.World.Towns[this.look.NearestCity].ParticipantIdx != person)
				return;

			if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
				return;

			this.Owner.Diplomats.BuyUnit((short)this.look.TileUnitOwner, (short)this.look.TileUnit, participantIdx);
		}

		/// <summary>
		/// The score 0x10 of the direction 0x40 grows by the surroundings when no enemy stands next to the unit (0x5c): +8 for
		/// an unmarked land tile four steps away (the pattern 0x3772:0x2438), and for each tile around the tile of the direction
		/// (0x44, at 0xe / 0x16) +2 when the player has not seen it (land, or the sea for a ship), -2 with units there and the
		/// food of its terrain for settlers. Next to an enemy (0x1c) with fewer than 3 moves left it is scaled by
		/// the share of the attackers in the stack. The best score (0x28) keeps the direction (0x2c) and the enemy mark (0x52).
		/// The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private void ScoreSurroundings(short participantIdx, short troopIdx)
		{
			if (this.look.NoEnemyNear != 0)
			{
				ushort direction = (ushort)(this.look.Direction << 1);
				this.look.TileHoriz = (ushort)((DirectionHorizAt(direction) << 2) + this.look.Horiz);
				this.look.TileVert = (ushort)((DirectionVertAt(direction) << 2) + this.look.Vert);

				// (the divisions as the original makes them)
				// (signed divisions toward zero: a place off the map, -2, is in cell 0 like in the original)
				int cellOffset = (short)(((short)this.look.TileHoriz / 4) * 13) + ((short)this.look.TileVert / 4);
				this.Processor.SegExtra.Value16 = 0x3772;

				short farHoriz = (short)this.look.TileHoriz;
				short farVert = (short)this.look.TileVert;

				if (this.Processor.PullUInt8(this.Processor.SegExtra.Value16, (ushort)(cellOffset + 0x2438)) == 0 &&
					this.Owner.MapOps.ObtainTerrainType(farHoriz, farVert) != MapRules.OceanTerrain)
				{
					if (this.Owner.MapOps.CheckMapLimits(farHoriz, farVert) != 0)
						this.look.Score = (ushort)(this.look.Score + 8);
				}

				for (this.look.Around = 1; (short)this.look.Around <= 8;
					this.look.Around = (ushort)(this.look.Around + 1))
				{
					ushort around = (ushort)(this.look.Around << 1);

					this.look.AroundHoriz = (ushort)this.Owner.Routing.TunePosition((short)DirectionHorizAt(around) +
						(short)this.look.TileHoriz);
					this.look.AroundVert = (ushort)(DirectionVertAt(around) + this.look.TileVert);

					short horizLoc = (short)this.look.AroundHoriz;
					short vertLoc = (short)this.look.AroundVert;

					if (this.Owner.MapOps.CheckMapLimits(horizLoc, vertLoc) == 0)
						continue;

					// Not seen by the player: land, or the sea for a ship
					if ((this.Owner.World.ChartSight[this.look.AroundHoriz, this.look.AroundVert] & (short)this.look.PlayerBit) == 0 &&
						(this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) != MapRules.OceanTerrain ||
							UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.LandGroup) == UnitKindTable.SeaUnits))
					{
						this.look.Score = (ushort)(this.look.Score + 2);
					}

					if (this.Owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc) != 0xffff)
						this.look.Score = (ushort)(this.look.Score - 2);

					if (this.look.Role == 0)
					{
						ushort terrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
						this.look.Score = (ushort)(this.look.Score +
							(sbyte)TerrainField(terrain, TerrainTable.Food));
					}
				}
			}

			if (this.look.Foreign != 0 && this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) < 3)
			{
				// The size of the stack, as the original computes it (unused, but the tally sets the globals of the stack and
				// the calls stay, like in the original)
				this.Owner.Helpers.ClampValue((short)this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, troopIdx, 3), 1, 99);

				ushort weighed = MultiplyWords(this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, troopIdx, 1), this.look.Score);
				// (the original divides by SI as the tally leaves it, not by the number of units kept there)
				this.look.Score = DivideWords(weighed, (ushort)(participantIdx * UnitMemory.ParticipantStride));
			}

			if ((short)this.look.Score > (short)this.look.BestScore)
			{
				this.look.BestScore = this.look.Score;
				this.look.BestDirection = this.look.Direction;
				this.look.BestNextToEnemy = this.look.Foreign;
			}
		}

		/// <summary>
		/// The end of the look around: the best direction (0x2c) becomes the heading of the unit and the answer of ShiftAIUnit;
		/// none (0) when the best was next to an enemy (0x52) and fewer than 3 moves are left.
		/// </summary>
		private ushort ChooseHeading(short participantIdx, short troopIdx)
		{
			if (this.look.BestNextToEnemy != 0 && this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) < 3)
				this.look.BestDirection = 0;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, (byte)this.look.BestDirection);

			return this.look.BestDirection;
		}

		/// <summary>
		/// Before the look around for a fight: a unit with a destination goes on (0), an attacker only when no foreign unit
		/// stands next to it; a defender (role 2) on land fortifies (0x66) next to its own city or with an enemy around; settlers
		/// on a continent with enough cities (more than an eighth of the places) build a city in a small city (0x62) or go
		/// to their small city (0). Else the look starts (0x28 the best -999, 0x2c, 0x5c: no enemy around; the heading of the
		/// unit is forgotten when there is one) and a militia under approach 1 counts as a defender (0x56 = 1); null.
		/// The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private ushort? DecideBeforeFight(short participantIdx, short troopIdx)
		{
			ushort role = this.look.Role;
			short horizLoc = (short)this.look.Horiz;
			short vertLoc = (short)this.look.Vert;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff &&
				(role != 3 || this.Owner.UnitOps.ContainsAdjacentForeignUnit(participantIdx, horizLoc, vertLoc) == 0))
			{
				return 0;
			}

			if (role == 2 && this.look.Terrain != MapRules.OceanTerrain)
			{
				if ((short)this.look.UnitDistance > 1 && (short)this.look.CityDistance <= 3 &&
					this.Owner.World.Towns[this.look.NearestCity].ParticipantIdx == participantIdx)
				{
					return 0x66;
				}

				if (this.look.MayStay != 0 && this.look.UnitDistance != 0)
					return 0x66;
			}

			if (role == 0)
			{
				ushort continent = this.look.Continent;
				int spotThreshold = (short)this.Owner.World.Landmasses[continent].ConstructionSpotTally / 8;

				if (this.Owner.World.Participants[participantIdx].Landmasses[continent].TownTally > spotThreshold)
				{
					GameModel.State.City town = this.Owner.World.Towns[this.look.NearestCity];

					if (this.look.CityDistance == 0 && (sbyte)town.TrueExtent < 10)
						return 0x62;

					if (town.ParticipantIdx == participantIdx && (sbyte)town.TrueExtent < 10)
					{
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)town.Place.X);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)town.Place.Y);

						return 0;
					}
				}
			}

			this.look.BestScore = 0xfc19;
			this.look.BestDirection = 0;
			this.look.NoEnemyNear = 0;

			if (this.Owner.UnitOps.CheckTileOccupation(participantIdx, horizLoc, vertLoc) != 0)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			else
				this.look.NoEnemyNear = 1;

			if (this.look.Kind == 1 && this.look.Approach == 1)
				this.look.Role = 1;

			return null;
		}

		/// <summary>
		/// A unit with fewer than 2 moves next to (0x2a under 4) a city of the human player (0x34), who is not at peace with
		/// the player, standing on an irrigated or mined tile: it pillages (0x50). The locals of ShiftAIUnit are used as in the
		/// original; DI as the original leaves it.
		/// </summary>
		private bool StandsOnHumanFields(short participantIdx, short troopIdx)
		{
			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			if ((short)UnitKindWord(kind, UnitKindTable.ShiftTally) >= 2 || (short)this.look.CityDistance >= 4)
				return false;

			short person = this.Owner.World.PersonParticipantIdx;

			if (this.Owner.World.Towns[this.look.NearestCity].ParticipantIdx != person)
				return false;

			if ((this.Owner.World.Participants[person].Relations[participantIdx] & 2) != 0 ||
				(this.Owner.MapOps.ObtainTileImprovements((short)this.look.Horiz, (short)this.look.Vert) & 6) == 0)
			{
				return false;
			}

			return this.Owner.World.Towns[this.look.NearestCity].ParticipantIdx != participantIdx;
		}
	}
}
