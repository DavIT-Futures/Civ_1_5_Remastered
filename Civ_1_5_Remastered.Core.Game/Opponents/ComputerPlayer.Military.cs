namespace Civ_1_5_Remastered.Core.Game.Opponents
{
	public partial class ComputerPlayer
	{
		public void PostCityDefenders(short townIndex)
		{
			using var _ = this.Processor.Journal.Scope($"PostCityDefenders({townIndex})");

			short townHorizontal = (short)this.Owner.World.Towns[townIndex].Place.X;
			short townVertical = (short)this.Owner.World.Towns[townIndex].Place.Y;

			for (short participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				if (participantIndex == this.Owner.World.PersonParticipantIndex ||
					(this.Owner.World.Participants[participantIndex].Relations[this.Owner.World.PersonParticipantIndex] & 2) != 0)
				{
					continue;
				}

				for (int troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
				{
					if (!this.troops.Exists(participantIndex, troopIndex) ||
						this.troops.KindField(participantIndex, troopIndex, UnitKindTable.FerryRoom) == 0 ||
						this.troops.Following(participantIndex, troopIndex) == -1)
					{
						continue;
					}

					sbyte horizontalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal);
					sbyte verticalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical);

					if (this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) != MapRules.OceanTerrain)
					{
						continue;
					}

					int distance = this.Owner.Helpers.ObtainDistance(horizontalLocation, townHorizontal, verticalLocation, townVertical);

					if ((short)(3 * this.troops.KindField(participantIndex, troopIndex, UnitKindTable.ShiftTally)) > distance)
					{
						this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)townHorizontal);
						this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)townVertical);
					}
				}
			}
		}

		private CityGuard GuardCity(short participantIndex, short troopIndex)
		{
			short horizontalLocation = (short)this.look.Horizontal;
			short verticalLocation = (short)this.look.Vertical;

			if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) == 0)
			{
				return CityGuard.GoesOn;
			}

			if (this.look.Role != 2)
			{
				return CityGuard.NotDefender;
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.FollowingInStack) == 0xff)
			{
				return CityGuard.Stays;
			}

			this.look.Round = (ushort)this.troops.TroopKind(participantIndex, troopIndex);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Kind, 0x1a);
			this.look.TileOwner = this.Owner.UnitOperations.FindStackLeader(participantIndex, troopIndex);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Kind, (byte)this.look.Round);

			if (this.look.TileOwner != 0xffff)
			{
				short defence = (short)Tables.UnitKindWord((sbyte)(byte)this.look.Round, UnitKindTable.GuardPower);
				short leader = (short)this.look.TileOwner;

				if ((short)Tables.UnitKindWord(this.troops.TroopKind(participantIndex, leader), UnitKindTable.GuardPower) < defence)
				{
					return CityGuard.Stays;
				}
			}

			this.look.GuardShare = (ushort)(this.Owner.UnitOperations.CheckTileOccupation(participantIndex, horizontalLocation, verticalLocation) != 0 ? 3 : 4);

			GameModel.State.City town = this.Owner.World.Towns[this.look.NearestCity];

			if (town.Has(CityStructure.Palace))
			{
				this.look.GuardShare = this.Processor.DecrementWord(this.look.GuardShare);
			}
			else if (this.look.Approach == 0)
			{
				this.look.GuardShare = this.Processor.IncrementWord(this.look.GuardShare);
			}

			this.look.Round = this.Owner.UnitOperations.CountUnitsWithFlagInStack(participantIndex, troopIndex, 2);

			short wanted = (short)(CentralProcessor.SignedQuotient(town.TrueExtent, (short)this.look.GuardShare) + 1);

			if (wanted > (short)this.look.Round)
			{
				SendUnitToStrategicLocation(participantIndex, horizontalLocation, verticalLocation, 2, 2);
			}

			if (wanted >= (short)this.look.Round)
			{
				return CityGuard.Stays;
			}

			ushort status = (ushort)(UnitMemory.Address(participantIndex, troopIndex) + UnitMemory.Status);
			this.look.Round = (ushort)(sbyte)this.Processor.PullByte(this.Processor.SegmentData.Word, status);
			this.Processor.EmitByte(this.Processor.SegmentData.Word, status, (byte)(this.Processor.PullByte(this.Processor.SegmentData.Word, status) | 8));

			if (this.Owner.UnitOperations.FindStackLeader(participantIndex, troopIndex) == (ushort)troopIndex)
			{
				short units = (short)this.Owner.UnitOperations.CountUnitsByDomainInStack(participantIndex, troopIndex, 4);

				if ((short)(CentralProcessor.SignedQuotient(town.TrueExtent, (short)this.look.GuardShare) + 1) > units)
				{
					this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(UnitMemory.ParticipantStride * participantIndex), (byte)this.look.Round);
					return CityGuard.Stays;
				}
			}

			this.Processor.EmitByte(this.Processor.SegmentData.Word, status, (byte)this.look.Round);

			if ((((byte)troopIndex + (byte)this.Owner.World.RoundTally) & 7) == 0)
			{
				this.Processor.EmitByte(this.Processor.SegmentData.Word, status, (byte)(this.Processor.PullByte(this.Processor.SegmentData.Word, status) & 0xf2));
			}

			return CityGuard.GoesOn;
		}

		private ushort? ExploreAround(short participantIndex)
		{
			short horizontalLocation = (short)this.look.Horizontal;
			short verticalLocation = (short)this.look.Vertical;

			if (this.look.Role == 0)
			{
				DisableStrategicLocation(participantIndex, 0, horizontalLocation, verticalLocation, 0);

				// Remastered: computer settlers choose their city sites with the site planner (DirectSettlers).
				if (ComputerExpansion.Controls(this.Owner, participantIndex))
				{
					return null;
				}
			}

			this.look.BestScore = this.Owner.VideoCard.ObtainDot(2, horizontalLocation + 0x50, verticalLocation);

			if (this.look.BestScore != 0 && (short)this.look.Role < 3)
			{
				this.look.BestDirection = 0;

				for (this.look.Direction = 1; (short)this.look.Direction <= 8;
					this.look.Direction = (ushort)(this.look.Direction + 1))
				{
					ushort direction = this.look.Direction;

					this.look.TileHorizontal = (ushort)this.Owner.Routing.TunePosition((short)Tables.DirectionHorizontal(direction) + horizontalLocation);
					this.look.TileVertical = (ushort)(Tables.DirectionVertical(direction) + this.look.Vertical);

					short nextHorizontal = (short)this.look.TileHorizontal;
					short nextVertical = (short)this.look.TileVertical;

					this.look.Score = this.Owner.VideoCard.ObtainDot(2, nextHorizontal + 0x50, nextVertical);

					if ((short)this.look.Score > (short)this.look.BestScore)
					{
						this.look.BestScore = this.look.Score;
						this.look.BestDirection = this.look.Direction;
					}

					if (this.Owner.MapOperations.ContainsHut(this.Owner.MapOperations.ObtainTerrainType(nextHorizontal, nextVertical), nextHorizontal, nextVertical) != 0 && this.look.Role != 0)
					{
						return this.look.Direction;
					}
				}

				if (this.Owner.World.RoundTally == 0 && NewWorld.EarthGame != 0)
				{
					this.look.BestScore = 0xf;
					this.look.BestDirection = 0;
				}

				if ((short)this.look.BestScore >= 8 &&
					(short)(0xf - this.look.CityDistance) <= (short)this.look.BestScore &&
					this.look.MayStay == 0)
				{
					if (this.look.Role == 0)
					{
						return this.look.BestDirection == 0 ? (ushort)0x62 : this.look.BestDirection;
					}

					AttachStrategicLocation(participantIndex, this.look.Horizontal, this.look.Vertical, 0, 2);
				}
			}

			return VisitAsAmbassador(participantIndex) ? (ushort)0x62 : null;
		}

		private bool VisitAsAmbassador(short participantIndex)
		{
			if (this.look.Role != 0 || this.Owner.World.HardnessTier == 0)
			{
				return false;
			}

			ushort nearest = this.look.NearestCity;
			ushort cityOwner = nearest < 128 ? (ushort)this.Owner.World.Towns[nearest].ParticipantIndex : (ushort)0xffff;
			short person = this.Owner.World.PersonParticipantIndex;

			if ((ushort)person != cityOwner || this.look.MayStay != 0 ||
				(short)this.look.CityDistance <= 1 || (ushort)participantIndex == cityOwner ||
				this.Owner.World.Participants[participantIndex].FoundScienceTally >= this.Owner.World.Participants[person].FoundScienceTally)
			{
				return false;
			}

			this.look.Round = this.Owner.VideoCard.ObtainDot(2, (short)this.look.Horizontal + 0x50, (short)this.look.Vertical + 0x32);

			return (short)this.look.Round >= 9 &&
				(short)(0xe - this.look.CityDistance) <= (short)this.look.Round;
		}

		private ushort AimNuclearWeapon(short participantIndex, short troopIndex)
		{
			GameModel.State.Player player = this.Owner.World.Participants[participantIndex];

			this.look.BestScore = 0xffff;

			for (this.look.Harbour = 0; (short)this.look.Harbour < 0x80;
				this.look.Harbour = (ushort)(this.look.Harbour + 1))
			{
				GameModel.State.City town = this.Owner.World.Towns[this.look.Harbour];

				this.look.Round = (ushort)town.ParticipantIndex;

				if (town.ConditionMarker != 0xff && !town.Has(CityStructure.StrategicDefense) && IsNuclearTarget(participantIndex, town))
				{
					ScoreNuclearTarget(participantIndex, town);
				}

				if ((short)this.look.Round > (short)this.look.BestScore && HasCityNear(participantIndex, town))
				{
					this.look.BestScore = this.look.Round;
					this.look.NearestCity = this.look.Harbour;
				}
			}

			if ((short)this.look.BestScore < 0xa || player.LiveTroops[25] <= 1)
			{
				return 0x20;
			}

			this.look.Harbour = this.look.NearestCity;
			GameModel.State.City target = this.Owner.World.Towns[this.look.Harbour];
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)(sbyte)target.Place.X);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)(sbyte)target.Place.Y);

			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				ushort direction = this.look.Direction;
				this.look.TileHorizontal = (ushort)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) +
					Tables.DirectionHorizontal(direction));
				this.look.TileVertical = (ushort)(this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelVertical) +
					Tables.DirectionVertical(direction));

				if (this.Owner.MapOperations.ObtainTileOwner((short)this.look.TileHorizontal, (short)this.look.TileVertical) != 0xffff)
				{
					continue;
				}

				this.Owner.MapOperations.RemoveUnitFromTile(participantIndex, troopIndex, (short)this.look.Horizontal, (short)this.look.Vertical);
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.Horizontal, (byte)this.look.TileHorizontal);
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.Vertical, (byte)this.look.TileVertical);
				this.Owner.MapOperations.PlaceUnitOnTile(participantIndex, troopIndex, (short)this.look.TileHorizontal, (short)this.look.TileVertical);

				if (this.Owner.World.Towns[this.look.NearestCity].ParticipantIndex == this.Owner.World.PersonParticipantIndex)
				{
					player.TouchParticipantTimeLeft = -2;
				}

				return (ushort)(this.look.Direction ^ 4);
			}

			return 0x20;
		}

		private bool IsNuclearTarget(short participantIndex, GameModel.State.City town)
		{
			GameModel.State.Player player = this.Owner.World.Participants[participantIndex];
			ushort owner = (ushort)town.ParticipantIndex;
			short ownerMight = (short)(this.Owner.World.Participants[owner].MartialMight << 1);

			this.look.Heading = (ushort)player.MartialMight;

			bool weighed = (short)(3 * (short)this.look.Heading) < ownerMight || (player.Relations[owner] & 8) != 0;

			if (!weighed)
			{
				if (this.Owner.World.Participants[this.look.Round].LiveTroops[25] != 0 ||
					(short)this.look.Heading >= ownerMight)
				{
					return false;
				}
			}

			return (player.Relations[this.look.Round] & 0x82) == 0x80 && town.TrueExtent > 4;
		}

		private void ScoreNuclearTarget(short participantIndex, GameModel.State.City town)
		{
			GameModel.State.Player player = this.Owner.World.Participants[participantIndex];

			this.look.Round = 0;

			for (this.look.Direction = 0; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				ushort direction = this.look.Direction;
				this.look.TileHorizontal = (ushort)(town.Place.X + Tables.DirectionHorizontal(direction));
				this.look.TileVertical = (ushort)(town.Place.Y + Tables.DirectionVertical(direction));

				this.look.TileOwner = this.Owner.MapOperations.ObtainTileOwner((short)this.look.TileHorizontal, (short)this.look.TileVertical);
				ushort tileOwner = this.look.TileOwner;

				if (tileOwner == 0xffff)
				{
					continue;
				}

				short delta = (player.Relations[tileOwner] & 0x82) == 0x80 ? (short)1 : tileOwner == (ushort)participantIndex ? (short)-2 : (short)-0x63;
				this.look.Round = (ushort)(this.look.Round + delta);
			}

			this.look.Round = (ushort)(this.look.Round + (town.TrueExtent / 2));
		}

		private bool HasCityNear(short participantIndex, GameModel.State.City target)
		{
			this.look.CityNear = 0;

			for (this.look.CityLoop = 0; (short)this.look.CityLoop < 0x80;
				this.look.CityLoop = (ushort)(this.look.CityLoop + 1))
			{
				GameModel.State.City town = this.Owner.World.Towns[this.look.CityLoop];

				if (town.ConditionMarker == 0xff || town.ParticipantIndex != participantIndex)
				{
					continue;
				}

				if ((short)this.Owner.Helpers.ObtainDistance(target.Place.X, town.Place.X, target.Place.Y, town.Place.Y) > 0x10)
				{
					continue;
				}

				this.look.CityNear = 1;
				break;
			}

			return this.look.CityNear != 0;
		}

		private ushort? PlanAirUnit(short participantIndex, short troopIndex)
		{
			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == 0xf &&
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.UnusualSteps) <= 0)
			{
				return 0x68;
			}

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) != 0xe ||
				this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) != 0xff)
			{
				return null;
			}

			sbyte kind = this.troops.TroopKind(participantIndex, troopIndex);
			short range = (short)(CentralProcessor.SignedQuotient((short)Tables.UnitKindWord(kind, UnitKindTable.ShiftTally), 2) * 3);

			if (range >= this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.LeftoverSteps))
			{
				return 0x68;
			}

			return SendFighter(participantIndex, troopIndex);
		}

		private ushort? SendFighter(short participantIndex, short troopIndex)
		{
			short person = this.Owner.World.PersonParticipantIndex;

			this.look.BestDistance = 0x3e7;
			this.look.NearestUnit = 0xffff;
			this.look.Direction = 0;

			do
			{
				short bomberIndex = (short)this.look.Direction;

				if (this.troops.Pull(person, bomberIndex, UnitMemory.Kind) == 0xf &&
					((1 << (byte)participantIndex) & this.troops.PullSigned(person, bomberIndex, UnitMemory.ShownToParticipants)) != 0)
				{
					short distance = (short)this.Owner.Helpers.ObtainDistance(
						this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal), this.troops.PullSigned(person, bomberIndex, UnitMemory.Horizontal),
						this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical), this.troops.PullSigned(person, bomberIndex, UnitMemory.Vertical));
					this.look.HarbourDistance = (ushort)distance;

					if ((short)this.look.HarbourDistance < (short)this.look.BestDistance)
					{
						this.look.BestDistance = this.look.HarbourDistance;
						this.look.NearestUnit = this.look.Direction;
					}
				}

				this.look.Direction = (ushort)(this.look.Direction + 1);
			}
			while ((short)this.look.Direction < 0x80);

			if (this.look.NearestUnit == 0xffff || (short)this.look.HarbourDistance <= 1)
			{
				return null;
			}

			short bomber = (short)this.look.NearestUnit;

			if ((short)Tables.UnitKindWord(this.troops.TroopKind(participantIndex, troopIndex), UnitKindTable.ShiftTally) >
				(short)this.look.BestDistance)
			{
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, this.troops.Pull(person, bomber, UnitMemory.Horizontal));
				this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, this.troops.Pull(person, bomber, UnitMemory.Vertical));
				return null;
			}

			this.look.BestDistance = 0x3e7;
			this.look.NearestCity = 0xffff;

			for (this.look.Direction = 0; (short)this.look.Direction < 0x80;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				GameModel.State.City town = this.Owner.World.Towns[this.look.Direction];

				if (town.ConditionMarker == 0xff || town.ParticipantIndex != participantIndex)
				{
					continue;
				}

				short reach = (short)this.Owner.Helpers.ObtainDistance(
					this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal), town.Place.X,
					this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical), town.Place.Y);

				if ((short)Tables.UnitKindWord(this.troops.TroopKind(participantIndex, troopIndex), UnitKindTable.ShiftTally) < reach)
				{
					continue;
				}

				short distance = (short)this.Owner.Helpers.ObtainDistance(
					this.troops.PullSigned(person, (short)this.look.NearestUnit, UnitMemory.Horizontal), town.Place.X,
					this.troops.PullSigned(person, (short)this.look.NearestUnit, UnitMemory.Vertical), town.Place.Y);
				this.look.HarbourDistance = (ushort)distance;

				if ((short)this.look.HarbourDistance < (short)this.look.BestDistance)
				{
					this.look.BestDistance = this.look.HarbourDistance;
					this.look.NearestCity = this.look.Direction;
				}
			}

			if (this.look.NearestCity == 0xffff)
			{
				return 0x20;
			}

			GameModel.State.City landing = this.Owner.World.Towns[this.look.NearestCity];
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)(sbyte)landing.Place.X);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)(sbyte)landing.Place.Y);

			return null;
		}

		private void SurveyComputerUnit(short participantIndex, short troopIndex)
		{
			this.look.MayStay = this.Owner.UnitOperations.MayOccupyTile(participantIndex, this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
				this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));

			this.look.BestDirection = 0;
			this.look.Horizontal = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal);
			this.look.Vertical = this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical);
			this.look.Kind = (ushort)this.troops.TroopKind(participantIndex, troopIndex);

			short horizontalLocation = (short)this.look.Horizontal;
			short verticalLocation = (short)this.look.Vertical;

			this.look.NearestCity = this.Owner.Helpers.FindNearestCity(horizontalLocation, verticalLocation);
			this.look.CityDistance = Search.FoundDistance;

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Kind, 0xff);
			this.look.NearestUnit = this.Owner.Helpers.FindNearestUnit(participantIndex, troopIndex, horizontalLocation, verticalLocation);
			this.look.UnitDistance = Search.FoundDistance;
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.Kind, (byte)this.look.Kind);

			this.look.Terrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);
			this.look.Role = Tables.UnitKindWord(this.troops.TroopKind(participantIndex, troopIndex), UnitKindTable.TroopGroup);

			this.look.Continent = this.Owner.MapOperations.ObtainContinentIdentifier(horizontalLocation, verticalLocation);
			this.look.Approach = (ushort)this.Owner.World.Participants[participantIndex].Landmasses[this.look.Continent].Approach;
			this.look.PlayerBit = (ushort)(1 << (byte)participantIndex);
		}
	}
}
