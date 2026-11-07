namespace Civ_1_5_Remastered.Core.Game.Opponents
{
	public partial class ComputerPlayer
	{
		public ushort ShiftComputerUnit(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ShiftAIUnit({participantIndex}, {troopIndex})");

			ushort move;

			if (participantIndex == 0)
			{
				move = ManageBarbarianUnit(participantIndex, troopIndex);
			}
			else
			{
				UnitLook outer = this.look;
				this.look = new UnitLook();

				try
				{
					move = DecideComputerUnitMove(participantIndex, troopIndex);
				}
				finally
				{
					this.look = outer;
				}
			}

			return move;
		}

		private ushort DecideComputerUnitMove(short participantIndex, short troopIndex)
		{
			ushort? airAnswer = PlanAirUnit(participantIndex, troopIndex);

			if (airAnswer != null)
			{
				return airAnswer.Value;
			}

			SurveyComputerUnit(participantIndex, troopIndex);

			ushort? colonyAnswer = DirectColonyShip(participantIndex, troopIndex);

			if (colonyAnswer != null)
			{
				return colonyAnswer.Value;
			}

			if (this.look.Kind == 0x19)
			{
				return AimNuclearWeapon(participantIndex, troopIndex);
			}

			if (this.look.Kind == UnitKindTable.Diplomat)
			{
				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) != 0xff)
				{
					return 0;
				}

				GameModel.State.City town = this.Owner.World.Towns[this.look.NearestCity];

				if (town.ParticipantIndex != participantIndex)
				{
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)(sbyte)town.Place.X);
					this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)(sbyte)town.Place.Y);

					return 0;
				}

				this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);
			}

			ushort? explored = ExploreAround(participantIndex);

			if (explored != null)
			{
				return explored.Value;
			}

			switch (GuardCity(participantIndex, troopIndex))
			{
				case CityGuard.Stays:
					return 0x66;

				case CityGuard.NotDefender:
					if ((short)this.look.UnitDistance > 0 && this.look.Role != 0)
					{
						SendUnitToStrategicLocation(participantIndex, (short)this.look.Horizontal, (short)this.look.Vertical, 2, 4);
						return 0x20;
					}

					break;
			}

			if (this.look.Role == 5 || this.look.Role == 3)
			{
				SurveyStack(participantIndex, troopIndex);

				if (!ChooseStagingCity(participantIndex, troopIndex))
				{
					ushort? landing = PlanTransportLanding(participantIndex, troopIndex);

					if (landing != null)
					{
						return landing.Value;
					}

					ChooseTransportDestination(participantIndex, troopIndex);
				}
			}

			if (StandsOnHumanFields(participantIndex, troopIndex))
			{
				return 0x50;
			}

			if (this.look.Role == 0)
			{
				ushort? settlerAnswer = DirectSettlers(participantIndex, troopIndex);

				if (settlerAnswer != null)
				{
					return settlerAnswer.Value;
				}
			}

			ushort? stayAnswer = DecideBeforeFight(participantIndex, troopIndex);

			if (stayAnswer != null)
			{
				return stayAnswer.Value;
			}

			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				if (MayAimAtDirection(participantIndex, troopIndex) && ScoreTile(participantIndex, troopIndex) && WeighUnitAtTile(participantIndex, troopIndex))
				{
					ScoreSurroundings(participantIndex, troopIndex);
				}
			}

			return ChooseHeading(participantIndex, troopIndex);
		}

		private bool MayAimAtDirection(short participantIndex, short troopIndex)
		{
			ushort direction = (ushort)(this.look.Direction << 1);

			this.look.TileHorizontal = (ushort)this.Owner.Routing.TunePosition((short)Tables.DirectionHorizontalAt(direction) +
				(short)this.look.Horizontal);
			this.look.TileVertical = (ushort)(Tables.DirectionVerticalAt(direction) + this.look.Vertical);

			short horizontalLocation = (short)this.look.TileHorizontal;
			short verticalLocation = (short)this.look.TileVertical;

			if (this.Owner.MapOperations.CheckMapLimits(horizontalLocation, verticalLocation) == 0)
			{
				return false;
			}

			this.look.TileUnitOwner = this.Owner.MapOperations.ObtainTileOwnerBits(horizontalLocation, verticalLocation);
			this.look.TileTerrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);

			if (this.look.TileTerrain == MapRules.OceanTerrain &&
				this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				return false;
			}

			this.look.TileUnit = this.Owner.MapOperations.ObtainUnitAtTile(horizontalLocation, verticalLocation);

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
					{
						this.look.TileUnit = this.look.TileOwner;
					}
				}
			}

			bool free = this.look.TileUnit == 0xffff;

			if (this.look.MayStay != 0 && this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				free && this.Owner.UnitOperations.MayOccupyTile(participantIndex, horizontalLocation, verticalLocation) != 0 &&
				this.troops.TroopKind(participantIndex, troopIndex) < UnitKindTable.Diplomat && this.look.Terrain != MapRules.OceanTerrain)
			{
				return false;
			}

			if (this.look.Terrain == MapRules.OceanTerrain &&
				this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				!free && this.look.TileUnitOwner != (ushort)participantIndex)
			{
				return false;
			}

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits &&
				this.look.TileTerrain != MapRules.OceanTerrain)
			{
				if (free || this.look.TileUnitOwner == (ushort)participantIndex || this.look.Kind == UnitKindTable.Submarine ||
					this.look.Terrain != MapRules.OceanTerrain)
				{
					return false;
				}
			}

			return free || this.look.TileUnitOwner != (ushort)participantIndex ||
				(short)this.Owner.UnitOperations.CountUnitsByDomainInStack(participantIndex, (short)this.look.TileUnit, 2) < 2;
		}

		private bool ScoreTile(short participantIndex, short troopIndex)
		{
			ushort role = this.look.Role;

			if (role == 0)
			{
				this.look.Score = 0;

				if (this.look.Terrain == MapRules.OceanTerrain &&
					this.Owner.UnitOperations.CheckTileOccupation(participantIndex, (short)this.look.TileHorizontal, (short)this.look.TileVertical) != 0)
				{
					return false;
				}
			}
			else
			{
				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.ShownToParticipants) == 0 && this.look.Terrain != MapRules.OceanTerrain)
				{
					short die = (short)this.Owner.Runtime.Dice.DrawInteger(3);
					short defence = role == 1 ? (short)(TerrainByte(0x28e) << 1) : TerrainByte(0x28f);
					this.look.Score = (ushort)(die - defence);
				}
				else
				{
					this.look.Score = (ushort)this.Owner.Runtime.Dice.DrawInteger(5);

					if (this.look.TileUnit != 0xffff && this.look.TileUnitOwner == (ushort)participantIndex)
					{
						short stack = (short)this.look.TileUnit;

						if (role == 1)
						{
							ushort tallyPlusOne = (ushort)(this.Owner.UnitOperations.CountUnitsByDomainInStack(participantIndex, stack, 2) + 1);
							ushort strength = (ushort)(this.Owner.UnitOperations.CountUnitsByDomainInStack(participantIndex, stack, 1) << 2);
							ushort share = DivideWords(strength, tallyPlusOne);
							this.look.Score = (ushort)(this.look.Score + share);
						}

						if (role == 2)
						{
							ushort tallyPlusOne = (ushort)(this.Owner.UnitOperations.CountUnitsByDomainInStack(participantIndex, stack, 1) + 1);
							ushort strength = (ushort)(this.Owner.UnitOperations.CountUnitsByDomainInStack(participantIndex, stack, 3) << 1);
							ushort share = DivideWords(strength, tallyPlusOne);
							this.look.Score = (ushort)(this.look.Score + share);
						}
					}
					else
					{
						this.look.Score = (ushort)(this.look.Score + (TerrainByte(0x28f) << 2));
					}
				}

				if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
				{
					this.look.Score = (ushort)this.Owner.Runtime.Dice.DrawInteger(3);
				}
			}

			byte heading = this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHeading);
			this.look.HeadingLow = heading;

			if (heading != 0xff)
			{
				short away = this.Owner.Runtime.AbsoluteValue((short)((sbyte)heading - (short)this.look.Direction));

				if (away > 4)
				{
					away = (short)(8 - away);
				}

				this.look.Away = (ushort)away;
				this.look.Score = (ushort)(this.look.Score - ((away * away) << 1));
			}

			return true;
		}

		private short TerrainByte(ushort offset)
		{
			return (sbyte)this.Processor.PullByte(this.Processor.SegmentData.Word, (ushort)((this.look.TileTerrain * 0x13) + offset));
		}

		private bool WeighUnitAtTile(short participantIndex, short troopIndex)
		{
			short horizontalLocation = (short)this.look.TileHorizontal;
			short verticalLocation = (short)this.look.TileVertical;
			this.look.Foreign = 0;

			if (this.look.TileUnit == 0xffff)
			{
				if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) != 0 && this.look.TileUnitOwner != (ushort)participantIndex)
				{
					this.look.Score = 999;
				}

				if (this.Owner.MapOperations.ContainsHut(this.look.TileTerrain, horizontalLocation, verticalLocation) != 0)
				{
					this.look.Score = (ushort)(this.look.Score + 20);
				}

				return true;
			}

			if (this.look.TileUnitOwner == (ushort)participantIndex)
			{
				this.look.Score = (ushort)(this.look.Score - this.troops.KindField(participantIndex, troopIndex, UnitKindTable.GuardPower));
				return true;
			}

			this.look.Foreign = 1;
			ushort role = this.look.Role;

			if (role == 0)
			{
				return false;
			}

			if ((this.Owner.World.Participants[participantIndex].Relations[this.look.TileUnitOwner] & 2) != 0)
			{
				BuyUnitAtPeace(participantIndex, troopIndex, horizontalLocation, verticalLocation);
				return false;
			}

			short enemy = (short)this.look.TileUnitOwner;
			this.look.TileUnit = this.Owner.UnitOperations.FindBestGuardianInStack(enemy, (short)this.look.TileUnit);

			bool airGuardian = this.troops.KindField(enemy, (short)this.look.TileUnit, UnitKindTable.LandGroup) == UnitKindTable.AirUnits;

			if (airGuardian && this.troops.TroopKind(participantIndex, troopIndex) != 0xe &&
				(this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) == 0)
			{
				return false;
			}

			this.look.BattleScore = this.Owner.Combat.SettleBattle(participantIndex, troopIndex,
				(short)this.look.TileUnitOwner, (short)this.look.TileUnit, 0);

			ushort losses = MultiplyWords((ushort)(this.Owner.UnitOperations.CountUnitsByDomainInStack((short)this.look.TileUnitOwner,
				(short)this.look.TileUnit, 0) + 1), this.look.BattleScore);
			this.look.BattleScore = losses;
			this.look.BattleScore = DivideWords(losses, this.troops.KindField(participantIndex, troopIndex, UnitKindTable.Price));

			if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) != 0)
			{
				this.look.BattleScore = (ushort)(this.look.BattleScore * 3);
			}

			if (role == 1 && this.look.Approach == 1)
			{
				this.look.BattleScore = (ushort)(this.look.BattleScore * 3);
			}

			if (role == 1)
			{
				short attack = (short)this.Owner.UnitOperations.CountUnitsByDomainInStack(participantIndex, troopIndex, 3);

				if ((short)(this.troops.KindField(participantIndex, troopIndex, UnitKindTable.AssaultPower) << 1) < attack)
				{
					this.look.BattleScore = (ushort)(this.look.BattleScore << 1);
				}
			}

			short enough = (short)(participantIndex == 0 ? 0xc : 6);

			if (enough <= (short)this.look.BattleScore)
			{
				this.look.Score = (ushort)(this.look.Score + (this.look.BattleScore << 2));
				return true;
			}

			this.look.Score = (ushort)(this.look.Score - 999);

			if (role == 1 && this.look.Approach == 1 &&
				this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				(this.troops.Pull(this.look.TileUnitOwner, this.look.TileUnit, UnitMemory.Status) & 0x28) != 0 &&
				TerrainByte(0x28f) >= 4 &&
				(short)this.Owner.UnitOperations.CountUnitsByDomainInStack((short)this.look.TileUnitOwner, (short)this.look.TileUnit, 2) < 2 &&
				this.Owner.Runtime.Dice.DrawInteger(4) == 0 &&
				(this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) == 0)
			{
				this.Owner.Diplomats.BuyUnit((short)this.look.TileUnitOwner, (short)this.look.TileUnit, participantIndex);
			}

			return true;
		}

		private void BuyUnitAtPeace(short participantIndex, short troopIndex, short horizontalLocation, short verticalLocation)
		{
			if (this.look.Role != 1 || this.look.Approach != 1)
			{
				return;
			}

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) != UnitKindTable.LandUnits)
			{
				return;
			}

			short other = (short)this.look.TileUnitOwner;
			short stack = (short)this.look.TileUnit;
			short person = this.Owner.World.PersonParticipantIndex;

			if ((this.troops.Pull(other, stack, UnitMemory.Status) & 8) == 0 ||
				(this.Owner.World.Participants[participantIndex].Relations[person] & 3) != 1 ||
				(this.Owner.World.Participants[other].Relations[person] & 3) == 1 ||
				this.Owner.World.Participants[person].Landmasses[this.look.Continent].TownTally == 0 ||
				(short)this.Owner.UnitOperations.CountUnitsByDomainInStack(other, stack, 2) >= 2)
			{
				return;
			}

			if (this.Owner.Runtime.Dice.DrawInteger(8) != 0 && this.Owner.World.Towns[this.look.NearestCity].ParticipantIndex != person)
			{
				return;
			}

			if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) != 0)
			{
				return;
			}

			this.Owner.Diplomats.BuyUnit((short)this.look.TileUnitOwner, (short)this.look.TileUnit, participantIndex);
		}

		private void ScoreSurroundings(short participantIndex, short troopIndex)
		{
			if (this.look.NoEnemyNear != 0)
			{
				ushort direction = (ushort)(this.look.Direction << 1);
				this.look.TileHorizontal = (ushort)((Tables.DirectionHorizontalAt(direction) << 2) + this.look.Horizontal);
				this.look.TileVertical = (ushort)((Tables.DirectionVerticalAt(direction) << 2) + this.look.Vertical);

				int cellOffset = (short)(((short)this.look.TileHorizontal / 4) * 13) + ((short)this.look.TileVertical / 4);
				this.Processor.SegmentExtra.Word = 0x3772;

				short farHorizontal = (short)this.look.TileHorizontal;
				short farVertical = (short)this.look.TileVertical;

				if (this.Processor.PullByte(this.Processor.SegmentExtra.Word, (ushort)(cellOffset + 0x2438)) == 0 &&
					this.Owner.MapOperations.ObtainTerrainType(farHorizontal, farVertical) != MapRules.OceanTerrain)
				{
					if (this.Owner.MapOperations.CheckMapLimits(farHorizontal, farVertical) != 0)
					{
						this.look.Score = (ushort)(this.look.Score + 8);
					}
				}

				for (this.look.Around = 1; (short)this.look.Around <= 8;
					this.look.Around = (ushort)(this.look.Around + 1))
				{
					ushort around = (ushort)(this.look.Around << 1);

					this.look.AroundHorizontal = (ushort)this.Owner.Routing.TunePosition((short)Tables.DirectionHorizontalAt(around) +
						(short)this.look.TileHorizontal);
					this.look.AroundVertical = (ushort)(Tables.DirectionVerticalAt(around) + this.look.TileVertical);

					short horizontalLocation = (short)this.look.AroundHorizontal;
					short verticalLocation = (short)this.look.AroundVertical;

					if (this.Owner.MapOperations.CheckMapLimits(horizontalLocation, verticalLocation) == 0)
					{
						continue;
					}

					if ((this.Owner.World.ChartSight[this.look.AroundHorizontal, this.look.AroundVertical] & (short)this.look.PlayerBit) == 0 &&
						(this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) != MapRules.OceanTerrain ||
							Tables.UnitKindWord(this.troops.TroopKind(participantIndex, troopIndex), UnitKindTable.LandGroup) == UnitKindTable.SeaUnits))
					{
						this.look.Score = (ushort)(this.look.Score + 2);
					}

					if (this.Owner.MapOperations.ObtainUnitAtTile(horizontalLocation, verticalLocation) != 0xffff)
					{
						this.look.Score = (ushort)(this.look.Score - 2);
					}

					if (this.look.Role == 0)
					{
						ushort terrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);
						this.look.Score = (ushort)(this.look.Score +
							(sbyte)Tables.TerrainField(terrain, TerrainTable.Food));
					}
				}
			}

			if (this.look.Foreign != 0 && this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.LeftoverSteps) < 3)
			{
				this.Owner.Helpers.ClampValue((short)this.Owner.UnitOperations.CountUnitsByDomainInStack(participantIndex, troopIndex, 3), 1, 99);

				ushort weighed = MultiplyWords(this.Owner.UnitOperations.CountUnitsByDomainInStack(participantIndex, troopIndex, 1), this.look.Score);
				this.look.Score = DivideWords(weighed, (ushort)(participantIndex * UnitMemory.ParticipantStride));
			}

			if ((short)this.look.Score > (short)this.look.BestScore)
			{
				this.look.BestScore = this.look.Score;
				this.look.BestDirection = this.look.Direction;
				this.look.BestNextToEnemy = this.look.Foreign;
			}
		}

		private ushort ChooseHeading(short participantIndex, short troopIndex)
		{
			if (this.look.BestNextToEnemy != 0 && this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.LeftoverSteps) < 3)
			{
				this.look.BestDirection = 0;
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHeading, (byte)this.look.BestDirection);

			return this.look.BestDirection;
		}

		private ushort? DecideBeforeFight(short participantIndex, short troopIndex)
		{
			ushort role = this.look.Role;
			short horizontalLocation = (short)this.look.Horizontal;
			short verticalLocation = (short)this.look.Vertical;

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) != 0xff &&
				(role != 3 || this.Owner.UnitOperations.ContainsAdjacentForeignUnit(participantIndex, horizontalLocation, verticalLocation) == 0))
			{
				return 0;
			}

			if (role == 2 && this.look.Terrain != MapRules.OceanTerrain)
			{
				if ((short)this.look.UnitDistance > 1 && (short)this.look.CityDistance <= 3 &&
					this.Owner.World.Towns[this.look.NearestCity].ParticipantIndex == participantIndex)
				{
					return 0x66;
				}

				if (this.look.MayStay != 0 && this.look.UnitDistance != 0)
				{
					return 0x66;
				}
			}

			if (role == 0)
			{
				ushort continent = this.look.Continent;
				int spotThreshold = (short)this.Owner.World.Landmasses[continent].ConstructionSpotTally / 8;

				if (this.Owner.World.Participants[participantIndex].Landmasses[continent].TownTally > spotThreshold)
				{
					GameModel.State.City town = this.Owner.World.Towns[this.look.NearestCity];

					if (this.look.CityDistance == 0 && (sbyte)town.TrueExtent < 10)
					{
						return 0x62;
					}

					if (town.ParticipantIndex == participantIndex && (sbyte)town.TrueExtent < 10)
					{
						this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)(sbyte)town.Place.X);
						this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)(sbyte)town.Place.Y);

						return 0;
					}
				}
			}

			this.look.BestScore = 0xfc19;
			this.look.BestDirection = 0;
			this.look.NoEnemyNear = 0;

			if (this.Owner.UnitOperations.CheckTileOccupation(participantIndex, horizontalLocation, verticalLocation) != 0)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHeading, 0xff);
			}
			else
			{
				this.look.NoEnemyNear = 1;
			}

			if (this.look.Kind == 1 && this.look.Approach == 1)
			{
				this.look.Role = 1;
			}

			return null;
		}

		private bool StandsOnHumanFields(short participantIndex, short troopIndex)
		{
			sbyte kind = this.troops.TroopKind(participantIndex, troopIndex);

			if ((short)Tables.UnitKindWord(kind, UnitKindTable.ShiftTally) >= 2 || (short)this.look.CityDistance >= 4)
			{
				return false;
			}

			short person = this.Owner.World.PersonParticipantIndex;

			if (this.Owner.World.Towns[this.look.NearestCity].ParticipantIndex != person)
			{
				return false;
			}

			if ((this.Owner.World.Participants[person].Relations[participantIndex] & 2) != 0 ||
				(this.Owner.MapOperations.ObtainTileImprovements((short)this.look.Horizontal, (short)this.look.Vertical) & 6) == 0)
			{
				return false;
			}

			return this.Owner.World.Towns[this.look.NearestCity].ParticipantIndex != participantIndex;
		}
	}
}
