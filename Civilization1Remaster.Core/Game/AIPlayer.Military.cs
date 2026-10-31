using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.UI;

namespace Civilization1Remaster.Game
{
	/// <summary>The AI guards, explorers, diplomats, nuclear weapons, aircraft and city defenders</summary>
	partial class AIPlayer
	{
		private enum CityGuard
		{
			/// <summary>Not in a city, or a defender that goes on (L166c)</summary>
			GoesOn,

			/// <summary>In a city, but not a defender (L168f)</summary>
			NotDefender,

			/// <summary>The defender stays (0x66)</summary>
			Stays,
		}

		/// <summary>
		/// Sends to the city the loaded transports at sea of the players 1 - 7 (not the human player, and only those without
		/// relations bit 2 towards the human player) that are closer to it than 3 turns of moves
		/// </summary>
		public void PostCityDefenders(short townIdx)
		{
			this.processor.Journal.OpenScope($"PostCityDefenders({townIdx})");

			short townHoriz = (short)this.owner.World.Towns[townIdx].Place.X;
			short townVert = (short)this.owner.World.Towns[townIdx].Place.Y;

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (participantIdx == this.owner.World.PersonParticipantIdx ||
					(this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 2) != 0)
					continue;

				for (int troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (!this.troops.Exists(participantIdx, troopIdx) ||
						this.troops.KindField(participantIdx, troopIdx, UnitKindTable.FerryRoom) == 0 ||
						this.troops.Following(participantIdx, troopIdx) == -1)
						continue;

					sbyte horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
					sbyte vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);

					if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) != MapRules.OceanTerrain)
						continue;

					int distance = this.owner.Helpers.ObtainDistance(horizLoc, townHoriz, vertLoc, townVert);

					if ((short)(3 * this.troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally)) > distance)
					{
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)townHoriz);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)townVert);
					}
				}
			}

			this.processor.Journal.CloseScope("PostCityDefenders");
		}

		/// <summary>
		/// A defender (role 2) in a city: alone, or better than the leader of its stack, it stays; it calls for more defenders
		/// when the city (size / need + 1; need 4, 3 with a unit of another player next to it, one less with the palace, one more
		/// on a continent at peace) has fewer, and stays while they are too few; the leader of the stack fortifies (bit 3) while
		/// the units there are too few; every eighth turn the unit loses its orders (bits 0, 2, 3)
		/// </summary>
		private CityGuard GuardCity(short participantIdx, short troopIdx)
		{
			short horizLoc = (short)this.look.Horiz;
			short vertLoc = (short)this.look.Vert;

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0)
				return CityGuard.GoesOn;

			if (this.look.Role != 2)
				return CityGuard.NotDefender;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.FollowingInStack) == 0xff)
				return CityGuard.Stays;

			// The leader of the stack without this unit (it counts as a diplomat while looking)
			this.look.Round = (ushort)this.troops.TroopKind(participantIdx, troopIdx);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, 0x1a);
			this.look.TileOwner = this.owner.UnitOps.LocateStackLeader(participantIdx, troopIdx);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, (byte)this.look.Round);

			if (this.look.TileOwner != 0xffff)
			{
				short defence = (short)UnitKindWord((sbyte)(byte)this.look.Round, UnitKindTable.GuardPower);
				short leader = (short)this.look.TileOwner;

				if ((short)UnitKindWord(this.troops.TroopKind(participantIdx, leader), UnitKindTable.GuardPower) < defence)
					return CityGuard.Stays;
			}

			this.look.GuardShare = (ushort)(this.owner.UnitOps.VerifyTileOccupation(participantIdx, horizLoc, vertLoc) != 0 ? 3 : 4);

			Game.State.City town = this.owner.World.Towns[this.look.NearestCity];

			if (town.Has(CityStructure.Palace))
				this.look.GuardShare = this.processor.DecrementWord(this.look.GuardShare);
			else if (this.look.Approach == 0)
				this.look.GuardShare = this.processor.IncrementWord(this.look.GuardShare);

			this.look.Round = this.owner.UnitOps.TallyUnitsWithFlagInStack(participantIdx, troopIdx, 2);

			short wanted = (short)(CPU.SignedQuotient(town.TrueExtent, (short)this.look.GuardShare) + 1);

			if (wanted > (short)this.look.Round)
				SendUnitToStrategicLocation(participantIdx, horizLoc, vertLoc, 2, 2);

			if (wanted >= (short)this.look.Round)
				return CityGuard.Stays;

			// The leader of the stack fortifies while the units there are too few
			ushort status = (ushort)(UnitMemory.Address(participantIdx, troopIdx) + UnitMemory.Status);
			this.look.Round = (ushort)(sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, status);
			this.processor.EmitUInt8(this.processor.SegData.Value16, status, (byte)(this.processor.PullUInt8(this.processor.SegData.Value16, status) | 8));

			if (this.owner.UnitOps.LocateStackLeader(participantIdx, troopIdx) == (ushort)troopIdx)
			{
				short units = (short)this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, troopIdx, 4);

				if ((short)(CPU.SignedQuotient(town.TrueExtent, (short)this.look.GuardShare) + 1) > units)
				{
					// A translation slip: the status is put back at SI, which the count of the stack has left at 0x600 * player
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(UnitMemory.ParticipantStride * participantIdx), (byte)this.look.Round);
					return CityGuard.Stays;
				}
			}

			this.processor.EmitUInt8(this.processor.SegData.Value16, status, (byte)this.look.Round);

			if ((((byte)troopIdx + (byte)this.owner.World.RoundTally) & 7) == 0)
				this.processor.EmitUInt8(this.processor.SegData.Value16, status, (byte)(this.processor.PullUInt8(this.processor.SegData.Value16, status) & 0xf2));

			return CityGuard.GoesOn;
		}

		/// <summary>
		/// A unit that may explore (role below 3) next to unknown land (the visibility layer): the direction with the most
		/// unknown around (0x2c, count 0x28), or straight to a hut (settlers excepted: role 0); in the first turn with 0xd76a
		/// it wanders; with enough unknown it explores (0x62 or the direction), a military unit there marks a strategic location
		/// instead. A settler next to the human player's city, with the human player ahead in technology, goes there as an
		/// ambassador (0x62). Null: the unit goes on (L14ac). The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private ushort? ExploreAround(short participantIdx, short troopIdx)
		{
			short horizLoc = (short)this.look.Horiz;
			short vertLoc = (short)this.look.Vert;

			if (this.look.Role == 0)
				DisableStrategicLocation(participantIdx, 0, horizLoc, vertLoc, 0);

			this.look.BestScore = this.owner.VideoCard.ObtainDot(2, horizLoc + 0x50, vertLoc);

			if (this.look.BestScore != 0 && (short)this.look.Role < 3)
			{
				this.look.BestDirection = 0;

				for (this.look.Direction = 1; (short)this.look.Direction <= 8;
					this.look.Direction = (ushort)(this.look.Direction + 1))
				{
					ushort direction = this.look.Direction;

					this.look.TileHoriz = (ushort)this.owner.Routing.TunePosition((short)DirectionHoriz(direction) + horizLoc);
					this.look.TileVert = (ushort)(DirectionVert(direction) + this.look.Vert);

					short nextHoriz = (short)this.look.TileHoriz;
					short nextVert = (short)this.look.TileVert;

					this.look.Score = this.owner.VideoCard.ObtainDot(2, nextHoriz + 0x50, nextVert);

					if ((short)this.look.Score > (short)this.look.BestScore)
					{
						this.look.BestScore = this.look.Score;
						this.look.BestDirection = this.look.Direction;
					}

					if (this.owner.MapOps.ContainsHut(this.owner.MapOps.ObtainTerrainType(nextHoriz, nextVert), nextHoriz, nextVert) != 0 && this.look.Role != 0)
						return this.look.Direction;
				}

				if (this.owner.World.RoundTally == 0 && EarthGame != 0)
				{
					this.look.BestScore = 0xf;
					this.look.BestDirection = 0;
				}

				if ((short)this.look.BestScore >= 8 &&
					(short)(0xf - this.look.CityDistance) <= (short)this.look.BestScore &&
					this.look.MayStay == 0)
				{
					if (this.look.Role == 0)
						return this.look.BestDirection == 0 ? (ushort)0x62 : this.look.BestDirection;

					AttachStrategicLocation(participantIdx, this.look.Horiz, this.look.Vert, 0, 2);
				}
			}

			return VisitAsAmbassador(participantIdx) ? (ushort)0x62 : null;
		}

		/// <summary>
		/// A settler (role 0) that may move on, near the nearest city of the human player (distance 0x2a above 1), with the
		/// human player ahead in technology and land known around it (the layer 0x32 below)
		/// </summary>
		private bool VisitAsAmbassador(short participantIdx)
		{
			if (this.look.Role != 0 || this.owner.World.HardnessTier == 0)
				return false;

			ushort nearest = this.look.NearestCity;
			ushort cityOwner = nearest < 128 ? (ushort)this.owner.World.Towns[nearest].ParticipantIdx : (ushort)0xffff;
			short person = this.owner.World.PersonParticipantIdx;

			if ((ushort)person != cityOwner || this.look.MayStay != 0 ||
				(short)this.look.CityDistance <= 1 || (ushort)participantIdx == cityOwner ||
				this.owner.World.Participants[participantIdx].FoundScienceTally >= this.owner.World.Participants[person].FoundScienceTally)
			{
				return false;
			}

			this.look.Round = this.owner.VideoCard.ObtainDot(2, (short)this.look.Horiz + 0x50, (short)this.look.Vert + 0x32);

			return (short)this.look.Round >= 9 &&
				(short)(0xe - this.look.CityDistance) <= (short)this.look.Round;
		}

		/// <summary>
		/// A nuclear weapon of a computer player (kind 0x19): the target is the city (0x34, score 0x28) of a player the
		/// player is at war with (bit 0x80 without peace) or much stronger than, without its own nuclear weapons, bigger than 4,
		/// scored by the enemy tiles around it (+1, own -2, others -99) and half its size, with an own city within 16 tiles; at
		/// score 10 and with at least two nuclear weapons the weapon is placed on a free tile next to it (the answer: the
		/// direction ^ 4), else 0x20. The locals of ShiftAIUnit are used as in the original.
		/// </summary>
		private ushort AimNuclearWeapon(short participantIdx, short troopIdx)
		{
			Game.State.Player player = this.owner.World.Participants[participantIdx];

			this.look.BestScore = 0xffff;

			for (this.look.Harbour = 0; (short)this.look.Harbour < 0x80;
				this.look.Harbour = (ushort)(this.look.Harbour + 1))
			{
				Game.State.City town = this.owner.World.Towns[this.look.Harbour];

				// The score starts as the owner: a dead city or one with bit 0 of the second word is compared with that
				this.look.Round = (ushort)town.ParticipantIdx;

				if (town.ConditionMarker != 0xff && !town.Has(CityStructure.SdiDefense) && IsNuclearTarget(participantIdx, town))
					ScoreNuclearTarget(participantIdx, town);

				if ((short)this.look.Round > (short)this.look.BestScore && HasCityNear(participantIdx, town))
				{
					this.look.BestScore = this.look.Round;
					this.look.NearestCity = this.look.Harbour;
				}
			}

			if ((short)this.look.BestScore < 0xa || player.LiveTroops[25] <= 1)
				return 0x20;

			this.look.Harbour = this.look.NearestCity;
			Game.State.City target = this.owner.World.Towns[this.look.Harbour];
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)target.Place.X);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)target.Place.Y);

			for (this.look.Direction = 1; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				ushort direction = this.look.Direction;
				this.look.TileHoriz = (ushort)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) +
					DirectionHoriz(direction));
				this.look.TileVert = (ushort)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelVert) +
					DirectionVert(direction));

				if (this.owner.MapOps.ObtainTileOwner((short)this.look.TileHoriz, (short)this.look.TileVert) != 0xffff)
					continue;

				this.owner.MapOps.DiscardUnitFromTile(participantIdx, troopIdx, (short)this.look.Horiz, (short)this.look.Vert);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Horiz, (byte)this.look.TileHoriz);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Vert, (byte)this.look.TileVert);
				this.owner.MapOps.SituateUnitOnTile(participantIdx, troopIdx, (short)this.look.TileHoriz, (short)this.look.TileVert);

				if (this.owner.World.Towns[this.look.NearestCity].ParticipantIdx == this.owner.World.PersonParticipantIdx)
					player.TouchParticipantTimeleft = -2;

				return (ushort)(this.look.Direction ^ 4);
			}

			return 0x20;
		}

		/// <summary>
		/// The owner of the city is an enemy (bit 0x80 without peace) and the city is bigger than 4, when the player is not
		/// much stronger (3 * its strength below twice the owner's), the owner is warned (bit 8), or the owner has no nuclear
		/// weapons and its strength is more than half the player's
		/// </summary>
		private bool IsNuclearTarget(short participantIdx, Game.State.City town)
		{
			Game.State.Player player = this.owner.World.Participants[participantIdx];
			ushort owner = (ushort)town.ParticipantIdx;
			short ownerMight = (short)(this.owner.World.Participants[owner].MartialMight << 1);

			this.look.Heading = (ushort)player.MartialMight;

			bool weighed = (short)(3 * (short)this.look.Heading) < ownerMight || (player.Relations[owner] & 8) != 0;

			if (!weighed)
			{
				if (this.owner.World.Participants[this.look.Round].LiveTroops[25] != 0 ||
					(short)this.look.Heading >= ownerMight)
				{
					return false;
				}
			}

			return (player.Relations[this.look.Round] & 0x82) == 0x80 && town.TrueExtent > 4;
		}

		/// <summary>The score of a target: the tiles around it (+1 an enemy's, -2 own, -99 another player's) and half its size</summary>
		private void ScoreNuclearTarget(short participantIdx, Game.State.City town)
		{
			Game.State.Player player = this.owner.World.Participants[participantIdx];

			this.look.Round = 0;

			for (this.look.Direction = 0; (short)this.look.Direction <= 8;
				this.look.Direction = (ushort)(this.look.Direction + 1))
			{
				ushort direction = this.look.Direction;
				this.look.TileHoriz = (ushort)(town.Place.X + DirectionHoriz(direction));
				this.look.TileVert = (ushort)(town.Place.Y + DirectionVert(direction));

				this.look.TileOwner = this.owner.MapOps.ObtainTileOwner((short)this.look.TileHoriz, (short)this.look.TileVert);
				ushort tileOwner = this.look.TileOwner;

				if (tileOwner == 0xffff)
					continue;

				short delta = (player.Relations[tileOwner] & 0x82) == 0x80 ? (short)1 : tileOwner == (ushort)participantIdx ? (short)-2 : (short)-0x63;
				this.look.Round = (ushort)(this.look.Round + delta);
			}

			this.look.Round = (ushort)(this.look.Round + town.TrueExtent / 2);
		}

		/// <summary>An own city of the player within 16 tiles of the target (0x3e), searched with 0x22</summary>
		private bool HasCityNear(short participantIdx, Game.State.City target)
		{
			this.look.CityNear = 0;

			for (this.look.CityLoop = 0; (short)this.look.CityLoop < 0x80;
				this.look.CityLoop = (ushort)(this.look.CityLoop + 1))
			{
				Game.State.City town = this.owner.World.Towns[this.look.CityLoop];

				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
					continue;

				if ((short)this.owner.Helpers.ObtainDistance(target.Place.X, town.Place.X, target.Place.Y, town.Place.Y) > 0x10)
					continue;

				this.look.CityNear = 1;
				break;
			}

			return this.look.CityNear != 0;
		}

		/// <summary>
		/// A bomber without fuel (byte 5) or a fighter without a destination and with fewer moves left than its range waits
		/// (0x68); a fighter with enough moves looks for a target, or a city to land in (0x20 without one). Null: the unit goes on.
		/// The locals of ShiftAIUnit are used as in the original (its frame is open).
		/// </summary>
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
			short range = (short)(CPU.SignedQuotient((short)UnitKindWord(kind, UnitKindTable.ShiftTally), 2) * 3);

			if (range >= this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps))
				return 0x68;

			return SendFighter(participantIdx, troopIdx);
		}

		/// <summary>
		/// The nearest bomber of the human player the fighter's player has seen (0x4, distance 0x14): within the fighter's
		/// moves it is the destination; else the own city within reach nearest to that bomber (0x34), or 0x20 without one
		/// </summary>
		private ushort? SendFighter(short participantIdx, short troopIdx)
		{
			short person = this.owner.World.PersonParticipantIdx;

			this.look.BestDistance = 0x3e7;
			this.look.NearestUnit = 0xffff;
			this.look.Direction = 0;

			do
			{
				short bomberIdx = (short)this.look.Direction;

				if (this.troops.Pull(person, bomberIdx, UnitMemory.Kind) == 0xf &&
					((1 << (byte)participantIdx) & this.troops.PullSigned(person, bomberIdx, UnitMemory.ShownToParticipants)) != 0)
				{
					short distance = (short)this.owner.Helpers.ObtainDistance(
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

			// The last distance measured, as in the original (not the nearest one)
			if (this.look.NearestUnit == 0xffff || (short)this.look.HarbourDistance <= 1)
				return null;

			short bomber = (short)this.look.NearestUnit;

			if ((short)UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.ShiftTally) >
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
				Game.State.City town = this.owner.World.Towns[this.look.Direction];

				if (town.ConditionMarker == 0xff || town.ParticipantIdx != participantIdx)
					continue;

				short reach = (short)this.owner.Helpers.ObtainDistance(
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), town.Place.X,
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert), town.Place.Y);

				if ((short)UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.ShiftTally) < reach)
					continue;

				short distance = (short)this.owner.Helpers.ObtainDistance(
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
				return 0x20;

			Game.State.City landing = this.owner.World.Towns[this.look.NearestCity];
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)landing.Place.X);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)landing.Place.Y);

			return null;
		}

		/// <summary>
		/// The surroundings of the unit in the locals of ShiftAIUnit: may it stay (0x4c), its place (0x30, 0x36), kind (0x8),
		/// the nearest city (0x34, distance 0x2a) and unit (0x4, distance 0x32; the unit is hidden while looking), the terrain
		/// (0x42), its role (0x56), continent (0x1a) and the approach there (0x2), the player's bit (0x50)
		/// </summary>
		private void SurveyAIUnit(short participantIdx, short troopIdx)
		{
			this.look.MayStay = this.owner.UnitOps.MayOccupyTile(participantIdx, this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			this.look.BestDirection = 0;
			this.look.Horiz = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			this.look.Vert = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert);
			this.look.Kind = (ushort)this.troops.TroopKind(participantIdx, troopIdx);

			short horizLoc = (short)this.look.Horiz;
			short vertLoc = (short)this.look.Vert;

			this.look.NearestCity = this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc);
			this.look.CityDistance = FoundDistance;

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, 0xff);
			this.look.NearestUnit = this.owner.Helpers.LocateNearestUnit(participantIdx, troopIdx, horizLoc, vertLoc);
			this.look.UnitDistance = FoundDistance;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, (byte)this.look.Kind);

			this.look.Terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
			this.look.Role = UnitKindWord(this.troops.TroopKind(participantIdx, troopIdx), UnitKindTable.TroopGroup);

			this.look.Continent = this.owner.MapOps.ObtainContinentID(horizLoc, vertLoc);
			this.look.Approach = (ushort)this.owner.World.Participants[participantIdx].Landmasses[this.look.Continent].Approach;
			this.look.PlayerBit = (ushort)(1 << (byte)participantIdx);
		}
	}
}
