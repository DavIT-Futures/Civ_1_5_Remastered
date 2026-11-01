namespace Civilization1Remaster.Core.Game
{
	public partial class ComputerPlayer
	{
		private enum CityGuard
		{
			GoesOn,

			NotDefender,

			Stays,
		}

		public void PostCityDefenders(short townIdx)
		{
			using var _ = this.Processor.Journal.Scope($"PostCityDefenders({townIdx})");

			short townHoriz = (short)this.Owner.World.Towns[townIdx].Place.X;
			short townVert = (short)this.Owner.World.Towns[townIdx].Place.Y;

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (participantIdx == this.Owner.World.PersonParticipantIdx ||
					(this.Owner.World.Participants[participantIdx].Relations[this.Owner.World.PersonParticipantIdx] & 2) != 0)
				{
					continue;
				}

				for (int troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (!this.troops.Exists(participantIdx, troopIdx) ||
						this.troops.KindField(participantIdx, troopIdx, UnitKindTable.FerryRoom) == 0 ||
						this.troops.Following(participantIdx, troopIdx) == -1)
					{
						continue;
					}

					sbyte horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
					sbyte vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);

					if (this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) != MapRules.OceanTerrain)
					{
						continue;
					}

					int distance = this.Owner.Helpers.ObtainDistance(horizLoc, townHoriz, vertLoc, townVert);

					if ((short)(3 * this.troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally)) > distance)
					{
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)townHoriz);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)townVert);
					}
				}
			}
		}

		private CityGuard GuardCity(short participantIdx, short troopIdx)
		{
			short horizLoc = (short)this.look.Horiz;
			short vertLoc = (short)this.look.Vert;

			if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0)
			{
				return CityGuard.GoesOn;
			}

			if (this.look.Role != 2)
			{
				return CityGuard.NotDefender;
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.FollowingInStack) == 0xff)
			{
				return CityGuard.Stays;
			}

			this.look.Round = (ushort)this.troops.TroopKind(participantIdx, troopIdx);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, 0x1a);
			this.look.TileOwner = this.Owner.UnitOps.FindStackLeader(participantIdx, troopIdx);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, (byte)this.look.Round);

			if (this.look.TileOwner != 0xffff)
			{
				short defence = (short)Tables.UnitKindWord((sbyte)(byte)this.look.Round, UnitKindTable.GuardPower);
				short leader = (short)this.look.TileOwner;

				if ((short)Tables.UnitKindWord(this.troops.TroopKind(participantIdx, leader), UnitKindTable.GuardPower) < defence)
				{
					return CityGuard.Stays;
				}
			}

			this.look.GuardShare = (ushort)(this.Owner.UnitOps.CheckTileOccupation(participantIdx, horizLoc, vertLoc) != 0 ? 3 : 4);

			GameModel.State.City town = this.Owner.World.Towns[this.look.NearestCity];

			if (town.Has(CityStructure.Palace))
			{
				this.look.GuardShare = this.Processor.DecrementWord(this.look.GuardShare);
			}
			else if (this.look.Approach == 0)
			{
				this.look.GuardShare = this.Processor.IncrementWord(this.look.GuardShare);
			}

			this.look.Round = this.Owner.UnitOps.CountUnitsWithFlagInStack(participantIdx, troopIdx, 2);

			short wanted = (short)(CPU.SignedQuotient(town.TrueExtent, (short)this.look.GuardShare) + 1);

			if (wanted > (short)this.look.Round)
			{
				SendUnitToStrategicLocation(participantIdx, horizLoc, vertLoc, 2, 2);
			}

			if (wanted >= (short)this.look.Round)
			{
				return CityGuard.Stays;
			}

			ushort status = (ushort)(UnitMemory.Address(participantIdx, troopIdx) + UnitMemory.Status);
			this.look.Round = (ushort)(sbyte)this.Processor.PullUInt8(this.Processor.SegData.Value16, status);
			this.Processor.EmitUInt8(this.Processor.SegData.Value16, status, (byte)(this.Processor.PullUInt8(this.Processor.SegData.Value16, status) | 8));

			if (this.Owner.UnitOps.FindStackLeader(participantIdx, troopIdx) == (ushort)troopIdx)
			{
				short units = (short)this.Owner.UnitOps.CountUnitsByDomainInStack(participantIdx, troopIdx, 4);

				if ((short)(CPU.SignedQuotient(town.TrueExtent, (short)this.look.GuardShare) + 1) > units)
				{
					this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(UnitMemory.ParticipantStride * participantIdx), (byte)this.look.Round);
					return CityGuard.Stays;
				}
			}

			this.Processor.EmitUInt8(this.Processor.SegData.Value16, status, (byte)this.look.Round);

			if ((((byte)troopIdx + (byte)this.Owner.World.RoundTally) & 7) == 0)
			{
				this.Processor.EmitUInt8(this.Processor.SegData.Value16, status, (byte)(this.Processor.PullUInt8(this.Processor.SegData.Value16, status) & 0xf2));
			}

			return CityGuard.GoesOn;
		}

		private ushort? ExploreAround(short participantIdx)
		{
			short horizLoc = (short)this.look.Horiz;
			short vertLoc = (short)this.look.Vert;

			if (this.look.Role == 0)
			{
				DisableStrategicLocation(participantIdx, 0, horizLoc, vertLoc, 0);
			}

			this.look.BestScore = this.Owner.VideoCard.ObtainDot(2, horizLoc + 0x50, vertLoc);

			if (this.look.BestScore != 0 && (short)this.look.Role < 3)
			{
				this.look.BestDirection = 0;

				for (this.look.Direction = 1; (short)this.look.Direction <= 8;
					this.look.Direction = (ushort)(this.look.Direction + 1))
				{
					ushort direction = this.look.Direction;

					this.look.TileHoriz = (ushort)this.Owner.Routing.TunePosition((short)Tables.DirectionHoriz(direction) + horizLoc);
					this.look.TileVert = (ushort)(Tables.DirectionVert(direction) + this.look.Vert);

					short nextHoriz = (short)this.look.TileHoriz;
					short nextVert = (short)this.look.TileVert;

					this.look.Score = this.Owner.VideoCard.ObtainDot(2, nextHoriz + 0x50, nextVert);

					if ((short)this.look.Score > (short)this.look.BestScore)
					{
						this.look.BestScore = this.look.Score;
						this.look.BestDirection = this.look.Direction;
					}

					if (this.Owner.MapOps.ContainsHut(this.Owner.MapOps.ObtainTerrainType(nextHoriz, nextVert), nextHoriz, nextVert) != 0 && this.look.Role != 0)
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

					AttachStrategicLocation(participantIdx, this.look.Horiz, this.look.Vert, 0, 2);
				}
			}

			return VisitAsAmbassador(participantIdx) ? (ushort)0x62 : null;
		}

		private bool VisitAsAmbassador(short participantIdx)
		{
			if (this.look.Role != 0 || this.Owner.World.HardnessTier == 0)
			{
				return false;
			}

			ushort nearest = this.look.NearestCity;
			ushort cityOwner = nearest < 128 ? (ushort)this.Owner.World.Towns[nearest].ParticipantIdx : (ushort)0xffff;
			short person = this.Owner.World.PersonParticipantIdx;

			if ((ushort)person != cityOwner || this.look.MayStay != 0 ||
				(short)this.look.CityDistance <= 1 || (ushort)participantIdx == cityOwner ||
				this.Owner.World.Participants[participantIdx].FoundScienceTally >= this.Owner.World.Participants[person].FoundScienceTally)
			{
				return false;
			}

			this.look.Round = this.Owner.VideoCard.ObtainDot(2, (short)this.look.Horiz + 0x50, (short)this.look.Vert + 0x32);

			return (short)this.look.Round >= 9 &&
				(short)(0xe - this.look.CityDistance) <= (short)this.look.Round;
		}

		private ushort AimNuclearWeapon(short participantIdx, short troopIdx)
		{
			GameModel.State.Player player = this.Owner.World.Participants[participantIdx];

			this.look.BestScore = 0xffff;

			for (this.look.Harbour = 0; (short)this.look.Harbour < 0x80;
				this.look.Harbour = (ushort)(this.look.Harbour + 1))
			{
				GameModel.State.City town = this.Owner.World.Towns[this.look.Harbour];

				this.look.Round = (ushort)town.ParticipantIdx;

				if (town.ConditionMarker != 0xff && !town.Has(CityStructure.SdiDefense) && IsNuclearTarget(participantIdx, town))
				{
					ScoreNuclearTarget(participantIdx, town);
				}

				if ((short)this.look.Round > (short)this.look.BestScore && HasCityNear(participantIdx, town))
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
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)target.Place.X);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)target.Place.Y);

			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				ushort direction = this.look.Direction;
				this.look.TileHoriz = (ushort)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) +
					Tables.DirectionHoriz(direction));
				this.look.TileVert = (ushort)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelVert) +
					Tables.DirectionVert(direction));

				if (this.Owner.MapOps.ObtainTileOwner((short)this.look.TileHoriz, (short)this.look.TileVert) != 0xffff)
				{
					continue;
				}

				this.Owner.MapOps.RemoveUnitFromTile(participantIdx, troopIdx, (short)this.look.Horiz, (short)this.look.Vert);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Horiz, (byte)this.look.TileHoriz);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Vert, (byte)this.look.TileVert);
				this.Owner.MapOps.PlaceUnitOnTile(participantIdx, troopIdx, (short)this.look.TileHoriz, (short)this.look.TileVert);

				if (this.Owner.World.Towns[this.look.NearestCity].ParticipantIdx == this.Owner.World.PersonParticipantIdx)
				{
					player.TouchParticipantTimeleft = -2;
				}

				return (ushort)(this.look.Direction ^ 4);
			}

			return 0x20;
		}

		private bool IsNuclearTarget(short participantIdx, GameModel.State.City town)
		{
			GameModel.State.Player player = this.Owner.World.Participants[participantIdx];
			ushort owner = (ushort)town.ParticipantIdx;
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

		private void ScoreNuclearTarget(short participantIdx, GameModel.State.City town)
		{
			GameModel.State.Player player = this.Owner.World.Participants[participantIdx];

			this.look.Round = 0;

			for (this.look.Direction = 0; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				ushort direction = this.look.Direction;
				this.look.TileHoriz = (ushort)(town.Place.X + Tables.DirectionHoriz(direction));
				this.look.TileVert = (ushort)(town.Place.Y + Tables.DirectionVert(direction));

				this.look.TileOwner = this.Owner.MapOps.ObtainTileOwner((short)this.look.TileHoriz, (short)this.look.TileVert);
				ushort tileOwner = this.look.TileOwner;

				if (tileOwner == 0xffff)
				{
					continue;
				}

				short delta = (player.Relations[tileOwner] & 0x82) == 0x80 ? (short)1 : tileOwner == (ushort)participantIdx ? (short)-2 : (short)-0x63;
				this.look.Round = (ushort)(this.look.Round + delta);
			}

			this.look.Round = (ushort)(this.look.Round + (town.TrueExtent / 2));
		}

		private bool HasCityNear(short participantIdx, GameModel.State.City target)
		{
			this.look.CityNear = 0;

			for (this.look.CityLoop = 0; (short)this.look.CityLoop < 0x80;
				this.look.CityLoop = (ushort)(this.look.CityLoop + 1))
			{
				GameModel.State.City town = this.Owner.World.Towns[this.look.CityLoop];

				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
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

		private ushort? PlanAirUnit(short participantIdx, short troopIdx)
		{
			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xf &&
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps) <= 0)
			{
				return 0x68;
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0xe ||
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
			{
				return null;
			}

			sbyte kind = this.troops.TroopKind(participantIdx, troopIdx);
			short range = (short)(CPU.SignedQuotient((short)Tables.UnitKindWord(kind, UnitKindTable.ShiftTally), 2) * 3);

			if (range >= this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps))
			{
				return 0x68;
			}

			return SendFighter(participantIdx, troopIdx);
		}

		private ushort? SendFighter(short participantIdx, short troopIdx)
		{
			short person = this.Owner.World.PersonParticipantIdx;

			this.look.BestDistance = 0x3e7;
			this.look.NearestUnit = 0xffff;
			this.look.Direction = 0;

			do
			{
				short bomberIdx = (short)this.look.Direction;

				if (this.troops.Pull(person, bomberIdx, UnitMemory.Kind) == 0xf &&
					((1 << (byte)participantIdx) & this.troops.PullSigned(person, bomberIdx, UnitMemory.ShownToParticipants)) != 0)
				{
					short distance = (short)this.Owner.Helpers.ObtainDistance(
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), this.troops.PullSigned(person, bomberIdx, UnitMemory.Horiz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert), this.troops.PullSigned(person, bomberIdx, UnitMemory.Vert));
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

			if ((short)Tables.UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.ShiftTally) >
				(short)this.look.BestDistance)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, this.troops.Pull(person, bomber, UnitMemory.Horiz));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, this.troops.Pull(person, bomber, UnitMemory.Vert));
				return null;
			}

			this.look.BestDistance = 0x3e7;
			this.look.NearestCity = 0xffff;

			for (this.look.Direction = 0; (short)this.look.Direction < 0x80;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				GameModel.State.City town = this.Owner.World.Towns[this.look.Direction];

				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
				{
					continue;
				}

				short reach = (short)this.Owner.Helpers.ObtainDistance(
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), town.Place.X,
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert), town.Place.Y);

				if ((short)Tables.UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.ShiftTally) < reach)
				{
					continue;
				}

				short distance = (short)this.Owner.Helpers.ObtainDistance(
					this.troops.PullSigned(person, (short)this.look.NearestUnit, UnitMemory.Horiz), town.Place.X,
					this.troops.PullSigned(person, (short)this.look.NearestUnit, UnitMemory.Vert), town.Place.Y);
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
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)landing.Place.X);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)landing.Place.Y);

			return null;
		}

		private void SurveyAIUnit(short participantIdx, short troopIdx)
		{
			this.look.MayStay = this.Owner.UnitOps.MayOccupyTile(participantIdx, this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			this.look.BestDirection = 0;
			this.look.Horiz = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			this.look.Vert = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
			this.look.Kind = (ushort)this.troops.TroopKind(participantIdx, troopIdx);

			short horizLoc = (short)this.look.Horiz;
			short vertLoc = (short)this.look.Vert;

			this.look.NearestCity = this.Owner.Helpers.FindNearestCity(horizLoc, vertLoc);
			this.look.CityDistance = Search.FoundDistance;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, 0xff);
			this.look.NearestUnit = this.Owner.Helpers.FindNearestUnit(participantIdx, troopIdx, horizLoc, vertLoc);
			this.look.UnitDistance = Search.FoundDistance;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, (byte)this.look.Kind);

			this.look.Terrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
			this.look.Role = Tables.UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.TroopGroup);

			this.look.Continent = this.Owner.MapOps.ObtainContinentID(horizLoc, vertLoc);
			this.look.Approach = (ushort)this.Owner.World.Participants[participantIdx].Landmasses[this.look.Continent].Approach;
			this.look.PlayerBit = (ushort)(1 << (byte)participantIdx);
		}
	}
}
