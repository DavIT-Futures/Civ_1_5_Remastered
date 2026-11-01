namespace Civilization1Remaster.Core.Game
{
	partial class ComputerPlayer
	{
		public ushort ShiftAIUnit(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ShiftAIUnit({participantIdx}, {troopIdx})");

			ushort move;

			if (participantIdx == 0)
			{
				move = ManageBarbarianUnit(participantIdx, troopIdx);
			}
			else
			{
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

		private ushort DecideAIUnitMove(short participantIdx, short troopIdx)
		{
			ushort? airAnswer = PlanAirUnit(participantIdx, troopIdx);

			if (airAnswer != null)
				return airAnswer.Value;

			SurveyAIUnit(participantIdx, troopIdx);

			if (this.look.Kind == 0x19)
				return AimNuclearWeapon(participantIdx, troopIdx);

			if (this.look.Kind == UnitKindTable.Diplomat)
			{
				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
					return 0;

				GameModel.State.City town = this.Owner.World.Towns[this.look.NearestCity];

				if (town.ParticipantIdx != participantIdx)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)town.Place.X);
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)town.Place.Y);

					return 0;
				}

				this.Owner.UnitOps.DestroyUnit(participantIdx, troopIdx);
			}

			ushort? explored = ExploreAround(participantIdx);

			if (explored != null)
				return explored.Value;

			switch (GuardCity(participantIdx, troopIdx))
			{
				case CityGuard.Stays:
					return 0x66;

				case CityGuard.NotDefender:
					if ((short)this.look.UnitDistance > 0 && this.look.Role != 0)
					{
						SendUnitToStrategicLocation(participantIdx, (short)this.look.Horiz, (short)this.look.Vert, 2, 4);
						return 0x20;
					}

					break;
			}

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

			if (StandsOnHumanFields(participantIdx, troopIdx))
				return 0x50;

			if (this.look.Role == 0)
			{
				ushort? settlerAnswer = DirectSettlers(participantIdx, troopIdx);

				if (settlerAnswer != null)
					return settlerAnswer.Value;
			}

			ushort? stayAnswer = DecideBeforeFight(participantIdx, troopIdx);

			if (stayAnswer != null)
				return stayAnswer.Value;

			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				if (MayAimAtDirection(participantIdx, troopIdx) && ScoreTile(participantIdx, troopIdx) && WeighUnitAtTile(participantIdx, troopIdx))
					ScoreSurroundings(participantIdx, troopIdx);
			}

			return ChooseHeading(participantIdx, troopIdx);
		}

		private bool MayAimAtDirection(short participantIdx, short troopIdx)
		{
			ushort direction = (ushort)(this.look.Direction << 1);

			this.look.TileHoriz = (ushort)this.Owner.Routing.TunePosition((short)Tables.DirectionHorizAt(direction) +
				(short)this.look.Horiz);
			this.look.TileVert = (ushort)(Tables.DirectionVertAt(direction) + this.look.Vert);

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
				if (free || this.look.TileUnitOwner == (ushort)participantIdx || this.look.Kind == UnitKindTable.Submarine ||
					this.look.Terrain != MapRules.OceanTerrain)
				{
					return false;
				}
			}

			return free || this.look.TileUnitOwner != (ushort)participantIdx ||
				(short)this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, (short)this.look.TileUnit, 2) < 2;
		}

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
							ushort share = DivideWords(strength, tallyPlusOne);
							this.look.Score = (ushort)(this.look.Score + share);
						}

						if (role == 2)
						{
							ushort tallyPlusOne = (ushort)(this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, stack, 1) + 1);
							ushort strength = (ushort)(this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, stack, 3) << 1);
							ushort share = DivideWords(strength, tallyPlusOne);
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

		private short TerrainByte(ushort offset)
		{
			return (sbyte)this.Processor.PullUInt8(this.Processor.SegData.Value16, (ushort)(this.look.TileTerrain * 0x13 + offset));
		}

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

		private void ScoreSurroundings(short participantIdx, short troopIdx)
		{
			if (this.look.NoEnemyNear != 0)
			{
				ushort direction = (ushort)(this.look.Direction << 1);
				this.look.TileHoriz = (ushort)((Tables.DirectionHorizAt(direction) << 2) + this.look.Horiz);
				this.look.TileVert = (ushort)((Tables.DirectionVertAt(direction) << 2) + this.look.Vert);

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

					this.look.AroundHoriz = (ushort)this.Owner.Routing.TunePosition((short)Tables.DirectionHorizAt(around) +
						(short)this.look.TileHoriz);
					this.look.AroundVert = (ushort)(Tables.DirectionVertAt(around) + this.look.TileVert);

					short horizLoc = (short)this.look.AroundHoriz;
					short vertLoc = (short)this.look.AroundVert;

					if (this.Owner.MapOps.CheckMapLimits(horizLoc, vertLoc) == 0)
						continue;

					if ((this.Owner.World.ChartSight[this.look.AroundHoriz, this.look.AroundVert] & (short)this.look.PlayerBit) == 0 &&
						(this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) != MapRules.OceanTerrain ||
							Tables.UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.LandGroup) == UnitKindTable.SeaUnits))
					{
						this.look.Score = (ushort)(this.look.Score + 2);
					}

					if (this.Owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc) != 0xffff)
						this.look.Score = (ushort)(this.look.Score - 2);

					if (this.look.Role == 0)
					{
						ushort terrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
						this.look.Score = (ushort)(this.look.Score +
							(sbyte)Tables.TerrainField(terrain, TerrainTable.Food));
					}
				}
			}

			if (this.look.Foreign != 0 && this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) < 3)
			{
				this.Owner.Helpers.ClampValue((short)this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, troopIdx, 3), 1, 99);

				ushort weighed = MultiplyWords(this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, troopIdx, 1), this.look.Score);
				this.look.Score = DivideWords(weighed, (ushort)(participantIdx * UnitMemory.ParticipantStride));
			}

			if ((short)this.look.Score > (short)this.look.BestScore)
			{
				this.look.BestScore = this.look.Score;
				this.look.BestDirection = this.look.Direction;
				this.look.BestNextToEnemy = this.look.Foreign;
			}
		}

		private ushort ChooseHeading(short participantIdx, short troopIdx)
		{
			if (this.look.BestNextToEnemy != 0 && this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) < 3)
				this.look.BestDirection = 0;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, (byte)this.look.BestDirection);

			return this.look.BestDirection;
		}

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

		private bool StandsOnHumanFields(short participantIdx, short troopIdx)
		{
			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);

			if ((short)Tables.UnitKindWord(kind, UnitKindTable.ShiftTally) >= 2 || (short)this.look.CityDistance >= 4)
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
