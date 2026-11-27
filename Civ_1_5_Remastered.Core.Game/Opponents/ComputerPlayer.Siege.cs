using System.Collections.Generic;

namespace Civ_1_5_Remastered.Core.Game.Opponents
{
	public partial class ComputerPlayer
	{
		private const byte NoTravel = 0xff;
		private const ushort FortifyOrder = 0x66;
		private const ushort PillageOrder = 0x50;
		private const ushort TravelOrder = 0;
		private const int StrongholdBit = 0x20;

		/// <summary>Attackers this far from a besieged city still count for the siege (they are on their way).</summary>
		private const int ForceRange = SiegePlanner.GatherRange + 5;

		/// <summary>An advancing attacker that has not come closer for this many turns gives up the siege.</summary>
		private const int StuckRounds = 3;

		/// <summary>How far a raiding attacker looks for something to pillage.</summary>
		private const int RaidRange = 3;

		private readonly Dictionary<int, SiegeRecord> sieges = new Dictionary<int, SiegeRecord>();

		/// <summary>Where and in which turn each attacker was last given a siege order.</summary>
		private readonly Dictionary<int, (int Round, int Horizontal, int Vertical)> asks = new Dictionary<int, (int Round, int Horizontal, int Vertical)>();

		/// <summary>The distance to the besieged city of each advancing attacker, and since when it has not changed.</summary>
		private readonly Dictionary<int, (int Distance, int Round)> approaches = new Dictionary<int, (int Distance, int Round)>();

		/// <summary>
		/// Remastered: an attacker near an enemy city waits on the tiles around it until enough attackers stand there,
		/// then all of them attack in the same turn; when the city cannot be taken they pillage its land instead.
		/// Returns null when the unit is not part of a siege and moves as before.
		/// </summary>
		private ushort? DirectSiege(short participantIndex, short troopIndex)
		{
			if (!ComputerSiege.Controls(this.Owner, participantIndex) || this.look.Role != 1 ||
				this.look.Terrain == MapRules.OceanTerrain || !IsSiegeAttacker(participantIndex, troopIndex))
			{
				return null;
			}

			short horizontalLocation = (short)this.look.Horizontal;
			short verticalLocation = (short)this.look.Vertical;

			if (AskedAgainInPlace(participantIndex, troopIndex, horizontalLocation, verticalLocation))
			{
				// The last order did not move the unit (the way is blocked): it moves as before for the rest of the turn.
				return null;
			}

			int townIndex = FindSiegeTarget(participantIndex, horizontalLocation, verticalLocation, out int distance);

			if (townIndex < 0)
			{
				return null;
			}

			City town = this.Owner.World.Towns[townIndex];

			byte travelHorizontal = this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal);

			if (travelHorizontal != NoTravel && this.Owner.Helpers.ObtainDistance(travelHorizontal, town.Place.X,
				this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelVertical), town.Place.Y) > 2)
			{
				// On its way somewhere else: it only passes by.
				return null;
			}

			SiegeRecord record = TrackSiege(participantIndex, townIndex, distance);
			SiegeView view = SurveySiege(participantIndex, town, distance, record);
			SiegeOrder order = SiegePlanner.Decide(view);
			ComputerSiege.Orders[(int)order]++;

			switch (order)
			{
				case SiegeOrder.Assault:
					record.AssaultRound = this.Owner.World.RoundTally;
					return HeadFor(horizontalLocation, verticalLocation, town.Place.X, town.Place.Y);

				case SiegeOrder.Advance:
					return IsStuck(participantIndex, troopIndex, distance) ? null : AdvanceToRing(participantIndex, troopIndex, town);

				case SiegeOrder.Hold:
					// Skips the turn: unlike a fortified unit, it is asked again next turn.
					return WaitOrder;

				case SiegeOrder.Raid:
					return Raid(participantIndex, troopIndex, town);

				default:
					return null;
			}
		}

		/// <summary>
		/// Whether the attacker has not come closer to the city for <see cref="StuckRounds"/> turns (the way to the
		/// ring is blocked); it then moves as before.
		/// </summary>
		private bool IsStuck(short participantIndex, short troopIndex, int distance)
		{
			int key = (participantIndex * UnitMemory.TroopTally) + troopIndex;
			int round = this.Owner.World.RoundTally;

			if (!this.approaches.TryGetValue(key, out (int Distance, int Round) approach) ||
				approach.Distance != distance || round < approach.Round)
			{
				this.approaches[key] = (distance, round);
				return false;
			}

			return round - approach.Round >= StuckRounds;
		}

		/// <summary>Whether the unit is asked again in the same turn on the same tile where it was given a siege order.</summary>
		private bool AskedAgainInPlace(short participantIndex, short troopIndex, short horizontalLocation, short verticalLocation)
		{
			int key = (participantIndex * UnitMemory.TroopTally) + troopIndex;
			(int Round, int Horizontal, int Vertical) ask = (this.Owner.World.RoundTally, horizontalLocation, verticalLocation);

			if (this.asks.TryGetValue(key, out (int Round, int Horizontal, int Vertical) last) && last == ask)
			{
				return true;
			}

			this.asks[key] = ask;
			return false;
		}

		private bool IsSiegeAttacker(short participantIndex, short troopIndex)
		{
			sbyte kind = this.troops.TroopKind(participantIndex, troopIndex);

			return kind >= 0 && kind < UnitKindTable.Diplomat &&
				Tables.UnitKindWord(kind, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				Tables.UnitKindWord(kind, UnitKindTable.TroopGroup) == 1 &&
				(short)Tables.UnitKindWord(kind, UnitKindTable.AssaultPower) > 0;
		}

		private bool IsEnemyCity(short participantIndex, City town) =>
			town.ConditionMarker != 0xff && town.ParticipantIndex != participantIndex &&
			(this.Owner.World.Participants[participantIndex].Relations[town.ParticipantIndex] & 2) == 0;

		/// <summary>The nearest enemy city the civilization has seen, on the unit's landmass and within reach of a siege.</summary>
		private int FindSiegeTarget(short participantIndex, short horizontalLocation, short verticalLocation, out int distance)
		{
			int best = -1;
			distance = int.MaxValue;

			for (int townIndex = 0; townIndex < 0x80; townIndex++)
			{
				City town = this.Owner.World.Towns[townIndex];

				if (!IsEnemyCity(participantIndex, town) ||
					((sbyte)this.Owner.World.ChartSight[town.Place.X, town.Place.Y] & (1 << participantIndex)) == 0)
				{
					continue;
				}

				int reach = this.Owner.Helpers.ObtainDistance(horizontalLocation, town.Place.X, verticalLocation, town.Place.Y);

				if (reach > SiegePlanner.GatherRange || reach >= distance ||
					this.Owner.MapOperations.ObtainContinentIdentifier(town.Place.X, town.Place.Y) != this.look.Continent)
				{
					continue;
				}

				best = townIndex;
				distance = reach;
			}

			return best;
		}

		private SiegeRecord TrackSiege(short participantIndex, int townIndex, int distance)
		{
			int key = (participantIndex * 0x80) + townIndex;
			int round = this.Owner.World.RoundTally;
			int cityOwner = this.Owner.World.Towns[townIndex].ParticipantIndex;

			if (!this.sieges.TryGetValue(key, out SiegeRecord? record))
			{
				record = new SiegeRecord();
				this.sieges[key] = record;
			}

			if (record.CityOwner != cityOwner || round - record.LastRound > 2 || round < record.LastRound)
			{
				record.CityOwner = cityOwner;
				record.FirstRound = -1;
				record.AssaultRound = -1;
			}

			if (distance <= 1 && record.FirstRound < 0)
			{
				record.FirstRound = round;
			}

			record.LastRound = round;

			return record;
		}

		private SiegeView SurveySiege(short participantIndex, City town, int distance, SiegeRecord record)
		{
			int defenders = 0;
			int guardianOwner = -1;
			int guardian = -1;
			ushort cityUnit = this.Owner.MapOperations.ObtainUnitAtTile(town.Place.X, town.Place.Y);

			if (cityUnit != 0xffff)
			{
				guardianOwner = Search.FoundPlayer;
				defenders = this.Owner.UnitOperations.CountUnitsByDomainInStack((short)guardianOwner, (short)cityUnit, 2);
				guardian = this.Owner.UnitOperations.FindBestGuardianInStack((short)guardianOwner, (short)cityUnit);
			}

			int ringAttackers = 0;
			double ringKills = 0;
			double forceKills = 0;
			ushort continent = this.look.Continent;

			for (short troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
			{
				if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Kind) == 0xff || !IsSiegeAttacker(participantIndex, troopIndex))
				{
					continue;
				}

				short horizontalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal);
				short verticalLocation = this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical);

				if (this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == MapRules.OceanTerrain ||
					this.Owner.MapOperations.ObtainContinentIdentifier(horizontalLocation, verticalLocation) != continent)
				{
					continue;
				}

				int reach = this.Owner.Helpers.ObtainDistance(horizontalLocation, town.Place.X, verticalLocation, town.Place.Y);

				if (reach > ForceRange)
				{
					continue;
				}

				double chance = guardian < 0 ? 1 : EstimateWinChance(participantIndex, troopIndex, (short)guardianOwner, (short)guardian, town);
				forceKills += chance;

				if (reach <= 1)
				{
					ringAttackers++;
					ringKills += chance;
				}
			}

			return new SiegeView
			{
				Distance = distance,
				Defenders = defenders,
				RingAttackers = ringAttackers,
				RingKills = ringKills,
				ForceKills = forceKills,
				RingTiles = CountRingTiles(participantIndex, town),
				RoundsWaiting = record.FirstRound < 0 ? 0 : this.Owner.World.RoundTally - record.FirstRound,
				AssaultUnderway = record.AssaultRound == this.Owner.World.RoundTally,
			};
		}

		/// <summary>The chance that the attacker beats the city's best defender, counted like a battle (see Battles).</summary>
		private double EstimateWinChance(short participantIndex, short troopIndex, short guardianOwner, short guardian, City town)
		{
			sbyte assailantKind = this.troops.TroopKind(participantIndex, troopIndex);
			sbyte guardianKind = this.troops.TroopKind(guardianOwner, guardian);
			bool assailantSeasoned = (this.troops.Pull(participantIndex, troopIndex, UnitMemory.Status) & (int)UnitStatus.Veteran) != 0;
			byte guardianStatus = this.troops.Pull(guardianOwner, guardian, UnitMemory.Status);
			bool guardianSeasoned = (guardianStatus & (int)UnitStatus.Veteran) != 0;

			int assailantAttack = (short)Tables.UnitKindWord(assailantKind, UnitKindTable.AssaultPower);
			int assault = assailantAttack * 8;

			if (assailantSeasoned)
			{
				assault += assault / 2;
			}

			if (this.Owner.World.HardnessTier <= 1 && guardianOwner == this.Owner.World.PersonParticipantIndex)
			{
				assault /= 2;
			}

			ushort terrain = this.Owner.MapOperations.ObtainTerrainType(town.Place.X, town.Place.Y);
			int landGuard = (sbyte)Tables.TerrainField(terrain, TerrainTable.DefenseBonus);
			int guardianGroup = Tables.UnitKindWord(guardianKind, UnitKindTable.LandGroup);

			int guard = DefenseStrength.Compute((short)Tables.UnitKindWord(guardianKind, UnitKindTable.GuardPower), landGuard,
				(this.Owner.MapOperations.ObtainTileImprovements(town.Place.X, town.Place.Y) & StrongholdBit) != 0,
				(guardianStatus & (int)UnitStatus.Fortified) != 0,
				guardianGroup == UnitKindTable.SeaUnits, guardianGroup == UnitKindTable.AirUnits,
				town.Has(CityStructure.CityWalls), assailantAttack, guardianSeasoned, town.Has(CityStructure.Stockade));

			int assailantHitPoints = this.Owner.World.Vitals.ObtainHitPoints(participantIndex, troopIndex, assailantKind, assailantSeasoned);
			int guardianHitPoints = this.Owner.World.Vitals.ObtainHitPoints(guardianOwner, guardian, guardianKind, guardianSeasoned);

			return SiegePlanner.WinChance(assault, guard, assailantHitPoints, guardianHitPoints);
		}

		/// <summary>Land tiles next to the city where an attacker may stand (no foreign unit on them).</summary>
		private int CountRingTiles(short participantIndex, City town)
		{
			int tiles = 0;

			for (int direction = 1; direction <= 8; direction++)
			{
				if (MayStandInRing(participantIndex, RingHorizontal(town, direction), RingVertical(town, direction)))
				{
					tiles++;
				}
			}

			return tiles;
		}

		private int RingHorizontal(City town, int direction) =>
			this.Owner.Routing.TunePosition((short)Tables.DirectionHorizontal(direction) + town.Place.X);

		private int RingVertical(City town, int direction) => (short)Tables.DirectionVertical(direction) + town.Place.Y;

		private bool MayStandInRing(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			if (this.Owner.MapOperations.CheckMapLimits(horizontalLocation, verticalLocation) == 0 ||
				this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == MapRules.OceanTerrain)
			{
				return false;
			}

			ushort tileOwner = this.Owner.MapOperations.ObtainTileOwner(horizontalLocation, verticalLocation);

			return tileOwner == 0xffff || tileOwner == (ushort)participantIndex;
		}

		/// <summary>
		/// Sends the attacker to a tile next to the city: one where fewer of its own units stand (so the city is
		/// surrounded from every side), near the attacker, and on good defensive ground.
		/// </summary>
		private ushort? AdvanceToRing(short participantIndex, short troopIndex, City town)
		{
			int bestScore = int.MaxValue;
			int bestHorizontal = -1;
			int bestVertical = -1;

			for (int direction = 1; direction <= 8; direction++)
			{
				int horizontalLocation = RingHorizontal(town, direction);
				int verticalLocation = RingVertical(town, direction);

				if (!MayStandInRing(participantIndex, horizontalLocation, verticalLocation))
				{
					continue;
				}

				int crowd = 0;
				ushort tileUnit = this.Owner.MapOperations.ObtainUnitAtTile(horizontalLocation, verticalLocation);

				if (tileUnit != 0xffff)
				{
					crowd = this.Owner.UnitOperations.CountUnitsByDomainInStack(participantIndex, (short)tileUnit, 2);
				}

				int reach = this.Owner.Helpers.ObtainDistance((short)this.look.Horizontal, horizontalLocation, (short)this.look.Vertical, verticalLocation);
				ushort terrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);
				int ground = (sbyte)Tables.TerrainField(terrain, TerrainTable.DefenseBonus);
				int score = (crowd * 20) + (reach * 3) - (ground * 2);

				if (score < bestScore)
				{
					bestScore = score;
					bestHorizontal = horizontalLocation;
					bestVertical = verticalLocation;
				}
			}

			if (bestHorizontal < 0)
			{
				return null;
			}

			return TravelTo(participantIndex, troopIndex, bestHorizontal, bestVertical);
		}

		/// <summary>
		/// The city cannot be taken: pillage the enemy land around it. A damaged attacker looks for irrigation (which
		/// restores its health), the others for mines (the most gold), then railroads and roads.
		/// </summary>
		private ushort? Raid(short participantIndex, short troopIndex, City town)
		{
			short horizontalLocation = (short)this.look.Horizontal;
			short verticalLocation = (short)this.look.Vertical;
			sbyte kind = this.troops.TroopKind(participantIndex, troopIndex);
			bool damaged = this.Owner.World.Vitals.ObtainLostHitPoints(participantIndex, troopIndex, kind) > 0;

			if (this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.LeftoverSteps) > 0 &&
				ScoreSpoils(participantIndex, horizontalLocation, verticalLocation, damaged) > 0)
			{
				return PillageOrder;
			}

			int bestScore = 0;
			int bestHorizontal = -1;
			int bestVertical = -1;

			for (int vertical = verticalLocation - RaidRange; vertical <= verticalLocation + RaidRange; vertical++)
			{
				for (int offset = -RaidRange; offset <= RaidRange; offset++)
				{
					int horizontal = this.Owner.Routing.TunePosition(horizontalLocation + offset);

					if (this.Owner.MapOperations.CheckMapLimits(horizontal, vertical) == 0 ||
						this.Owner.MapOperations.ObtainUnitAtTile(horizontal, vertical) != 0xffff ||
						this.Owner.MapOperations.ObtainContinentIdentifier(horizontal, vertical) != this.look.Continent)
					{
						continue;
					}

					int reach = this.Owner.Helpers.ObtainDistance(horizontalLocation, horizontal, verticalLocation, vertical);
					int score = ScoreSpoils(participantIndex, horizontal, vertical, damaged);

					if (score <= 0)
					{
						continue;
					}

					score = (score * 4) - reach;

					if (score > bestScore)
					{
						bestScore = score;
						bestHorizontal = horizontal;
						bestVertical = vertical;
					}
				}
			}

			if (bestHorizontal < 0)
			{
				// Nothing left to pillage: wait next to the city for reinforcements.
				return this.Owner.Helpers.ObtainDistance(horizontalLocation, town.Place.X, verticalLocation, town.Place.Y) <= 1
					? WaitOrder
					: null;
			}

			return TravelTo(participantIndex, troopIndex, bestHorizontal, bestVertical);
		}

		/// <summary>How much the civilization gains by pillaging the tile, 0 when it is not enemy land worth pillaging.</summary>
		private int ScoreSpoils(short participantIndex, int horizontalLocation, int verticalLocation, bool damaged)
		{
			if (this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == MapRules.OceanTerrain)
			{
				return 0;
			}

			int improvements = this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation);

			if (!PillageRewards.HasSpoils(improvements))
			{
				return 0;
			}

			ushort nearest = this.Owner.Helpers.FindNearestCity(horizontalLocation, verticalLocation);

			if (nearest >= 0x80 || !IsEnemyCity(participantIndex, this.Owner.World.Towns[nearest]) || Search.FoundDistance > 3)
			{
				return 0;
			}

			PillageReward reward = PillageRewards.For(PillageRewards.Removed(improvements));

			return reward.Gold + (reward.Heals ? (damaged ? 60 : 10) : 0);
		}

		private ushort HeadFor(short horizontalLocation, short verticalLocation, int targetHorizontal, int targetVertical)
		{
			for (ushort direction = 1; direction <= 8; direction++)
			{
				if (this.Owner.Routing.TunePosition((short)Tables.DirectionHorizontal(direction) + horizontalLocation) == targetHorizontal &&
					(short)Tables.DirectionVertical(direction) + verticalLocation == targetVertical)
				{
					return direction;
				}
			}

			return FortifyOrder;
		}

		private ushort TravelTo(short participantIndex, short troopIndex, int horizontalLocation, int verticalLocation)
		{
			short here = (short)this.look.Horizontal;
			short hereVertical = (short)this.look.Vertical;

			if (this.Owner.Helpers.ObtainDistance(here, horizontalLocation, hereVertical, verticalLocation) <= 1)
			{
				return HeadFor(here, hereVertical, horizontalLocation, verticalLocation);
			}

			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelHorizontal, (byte)horizontalLocation);
			this.troops.Assign(participantIndex, troopIndex, UnitMemory.TravelVertical, (byte)verticalLocation);

			return TravelOrder;
		}
	}
}
