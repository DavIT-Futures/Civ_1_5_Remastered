
using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>The production of a city: units, improvements, wonders, spaceship parts, buying</summary>
	partial class CityManagement
	{
		/// <summary>Where HandleCity goes on after the food and the shields of the turn (ProduceInCity)</summary>
		private enum ProductionStep
		{
			/// <summary>A unit was built (L174b)</summary>
			UnitBuilt,

			/// <summary>Not enough shields for the unit (L1b60)</summary>
			NotEnough,

			/// <summary>The city builds an improvement or a wonder (L1b63)</summary>
			Improvement,

			/// <summary>Settlers of a city of size 1 on the easiest level wait (L2307)</summary>
			SettlersWait,

			/// <summary>The city is gone (L6927)</summary>
			CityLost,
		}

		/// <summary>
		/// The turn of the city, first part: the food stored grows by the surplus (the citizens eat 2, the settlers 0x48),
		/// the shields by 0x70dc less the support of the units (0xd2f6; nothing with the flag 1). A unit (price x 0x4a) is
		/// built when the shields suffice: the replay notes the first one of its kind, the barracks make it veteran;
		/// settlers make the city smaller (a city of size 1 is lost, unless it is the last one of its owner). A computer
		/// player builds no diplomats (the shields go). The locals of HandleCity (0xba the new unit) are used as in the
		/// original.
		/// </summary>
		private ProductionStep ProduceInCity(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;

			// The food and the shields of the turn
			ushort surplus = (ushort)(CityFood - ((ushort)((short)this.round.SettlerFood * (short)CitySettlers)) - (ushort)(town.TrueExtent * 2));
			town.GrainTally += (short)surplus;

			this.round.Value = (ushort)(CityShields - CityUnitUpkeep);

			if ((town.ConditionMarker & 1) != 0)
				this.round.Value = 0;

			town.MaterialsTally += (short)this.round.Value;

			if (town.PresentMakingIdx < 0)
				return ProductionStep.Improvement;

			// A unit
			short cost = (short)((short)UnitKindWord(town.PresentMakingIdx, UnitKindTable.Price) *
				(short)this.round.CostFactor);

			if (cost > town.MaterialsTally)
				return ProductionStep.NotEnough;

			if (town.PresentMakingIdx == 0 && town.TrueExtent == 1 && this.owner.World.HardnessTier == 0)
				return ProductionStep.SettlersWait;

			town.MaterialsTally -= cost;
			this.round.NewUnit = 0xffff;

			if (owner == this.owner.World.PersonParticipantIdx || town.PresentMakingIdx != 0x1a)
			{
				// (a computer player builds no diplomats)
				this.round.NewUnit = this.owner.UnitOps.ConstructUnit(owner, town.PresentMakingIdx, town.Place.X, town.Place.Y);
			}

			// The first unit of its kind goes into the replay
			if ((this.owner.World.ScienceLeadingFoundVia[town.PresentMakingIdx] & 8) == 0)
			{
				this.owner.UnitOps.AttachReplayContent(6, (byte)CityOwner, (byte)town.PresentMakingIdx);
				this.owner.World.ScienceLeadingFoundVia[town.PresentMakingIdx] |= 8;
			}

			if (town.Has(CityStructure.Barracks) && this.round.NewUnit != 0xffff)
			{
				// The barracks make it veteran
				short troopIdx = (short)this.round.NewUnit;
				this.troops.Assign(owner, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(owner, troopIdx, UnitMemory.Status) | 0x20));
			}

			if (this.round.NewUnit == 0xffff)
				return ProductionStep.UnitBuilt;

			if (town.PresentMakingIdx != 0)
				return ProductionStep.UnitBuilt;

			// Settlers make the city smaller (but the last city of size 1)
			if (town.TrueExtent <= 1 && this.owner.World.Participants[owner].TownTally <= 1)
				return ProductionStep.UnitBuilt;

			sbyte size = town.TrueExtent;
			town.TrueExtent--;

			// (the translated code tests the flags of the comparison of the size with 1: the same as the size becoming 0)
			if (size != 1)
				return ProductionStep.UnitBuilt;

			this.owner.Advisors.FinalizeProduction(townIdx, (ushort)(short)town.Place.X, (ushort)(short)town.Place.Y);
			this.round.NewUnit = this.owner.UnitOps.ConstructUnit(owner, town.PresentMakingIdx, town.Place.X, town.Place.Y);
			this.owner.NewGameWizard.EliminateCivilization((short)CityOwner, 0);
			this.owner.Startup.DisplayMouseCursor();

			return ProductionStep.CityLost;
		}

		/// <summary>
		/// After the unit of the turn (0xba, -1 when none): a human city keeps no shields and tells of new settlers,
		/// diplomats and caravans. A computer city sends a new caravan to the foreign city with the most trade (not of the
		/// human) and a new diplomat (never built for a computer player: unreachable, kept as translated) to a city of the
		/// human; the last nuclear unit of a computer player resets the human's timer of contact; then it picks a new
		/// production. The locals of HandleCity are used as in the original.
		/// </summary>
		private void FinishUnitOfCity(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			short person = this.owner.World.PersonParticipantIdx;

			if (owner == person || this.round.NewUnit == 0xffff)
			{
				// A human city (or no unit): no shields kept, the human is told of settlers, diplomats and caravans
				town.MaterialsTally = 0;

				if (this.round.NewUnit == 0xffff || (town.PresentMakingIdx != 0 && town.PresentMakingIdx < 0x1a))
					return;

				CityAgain = 1;
				EmptyMessage()
					.City(townIdx)
					.Text(" builds ");
				this.owner.Words.Append(((ushort)(town.PresentMakingIdx * UnitKindTable.Stride + UnitKindTable.Origin)));
				this.owner.Words.Append(".\n");
				DialogStyle = 3;
				this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 80, 80);
				return;
			}

			if (town.PresentMakingIdx == 0x1b)
			{
				// A caravan goes to the foreign city with the most trade (not of the human)
				this.round.Found = 0xffff;
				this.round.Best = 0xffff;

				for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
				{
					this.round.OtherCity = otherIdx;
					City other = this.owner.World.Towns[otherIdx];

					if (other.ParticipantIdx == owner || other.ConditionMarker == 0xff || other.ParticipantIdx == person ||
						other.RootCommerce <= (short)this.round.Best)
					{
						continue;
					}

					this.round.Best = (ushort)other.RootCommerce;
					this.round.Found = this.round.OtherCity;
				}

				this.round.OtherCity = 0x80;
				this.owner.Conquest.FoundTradeRoute(owner, (short)this.round.NewUnit, (short)this.round.Found);
			}

			if (town.PresentMakingIdx == 0x1a)
				SendDiplomat(townIdx);

			if (town.PresentMakingIdx == 0x19)
			{
				// The last nuclear unit of a computer player

				if (this.owner.World.Participants[owner].LiveTroops[25] == 1)
					this.owner.World.Participants[person].TouchParticipantTimeleft = -1;
			}

			this.owner.Bot.PickNewCityProduction(townIdx);
		}

		/// <summary>
		/// A new diplomat of a computer player (unreachable: a computer player builds none) goes, from the nearest of its
		/// units, to a city of the human on the same continent (the nearest; equal ones by chance, 0x106); with none within
		/// 3 or at peace with the human the shields come back
		/// </summary>
		private void SendDiplomat(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;

			this.round.Found = 0xffff;
			this.round.Best = 0x7fff;
			this.round.Selected = 0;

			for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
			{
				this.round.OtherCity = otherIdx;
				City other = this.owner.World.Towns[otherIdx];

				if (other.ParticipantIdx != this.owner.World.PersonParticipantIdx || other.ConditionMarker == 0xff)
					continue;

				this.round.Value = this.owner.Helpers.LocateNearestUnit(owner, (short)this.round.NewUnit, other.Place.X, other.Place.Y);

				FoundDistance = (ushort)this.owner.Libc.AbsoluteValue((short)((ushort)(FoundDistance - 3)));

				if ((other.ConditionMarker & 0x20) != 0)
					FoundDistance = 3;

				short best = (short)this.round.Best;
				short distance = (short)FoundDistance;

				if (best < distance)
					continue;

				if (best == distance)
				{
					this.round.Selected = (ushort)(this.round.Selected + 1);

					if (this.owner.Libc.Dice.DrawInt(this.round.Selected) != 0)
						continue;
				}

				// The unit must stand on land of the continent of the city
				short troopIdx = (short)this.round.Value;

				if (this.owner.MapOps.ObtainTerrainType(this.troops.PullSigned(owner, troopIdx, UnitMemory.Horiz), this.troops.PullSigned(owner, troopIdx, UnitMemory.Vert)) == MapRules.OceanTerrain)
					continue;


				this.round.Continent = this.owner.MapOps.ObtainContinentID(this.troops.PullSigned(owner, troopIdx, UnitMemory.Horiz), this.troops.PullSigned(owner, troopIdx, UnitMemory.Vert));
				if (this.owner.MapOps.ObtainContinentID(other.Place.X, other.Place.Y) != this.round.Continent)
					continue;

				if (this.round.Best == FoundDistance)
					this.round.Selected = 1;
				else
					this.round.Best = FoundDistance;

				this.round.Found = this.round.Value;
				this.round.Target = this.round.OtherCity;
			}

			this.round.OtherCity = 0x80;

			if ((short)this.round.Best <= 3)
			{
				if ((this.owner.World.Participants[owner].Relations[this.owner.World.PersonParticipantIdx] & 2) == 0)
				{
					// The diplomat goes from the unit 0x108 to the city 0xb0
					short fromIdx = (short)this.round.Found;
					this.round.NewUnit = this.owner.UnitOps.ConstructUnit(owner, 0x1a,
						this.troops.PullSigned(owner, fromIdx, UnitMemory.Horiz), this.troops.PullSigned(owner, fromIdx, UnitMemory.Vert));

					if (this.round.NewUnit != 0xffff)
					{
						short troopIdx = (short)this.round.NewUnit;
						City target = this.owner.World.Towns[this.round.Target];
						this.troops.Assign(owner, troopIdx, UnitMemory.TravelHoriz, (byte)(sbyte)target.Place.X);
						this.troops.Assign(owner, troopIdx, UnitMemory.TravelVert, (byte)(sbyte)target.Place.Y);
					}

					return;
				}
			}

			// The shields come back
			ushort unitRecord = (ushort)(town.PresentMakingIdx * UnitKindTable.Stride);
			town.MaterialsTally += (short)((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(unitRecord + UnitKindTable.Origin + UnitKindTable.Price)) *
				(short)this.round.CostFactor);
		}

		/// <summary>
		/// An improvement or wonder (0xe8 = -the production) completed when the shields reach its price x 0x4a: a wonder
		/// built elsewhere or an improvement the city has already makes 0xe8 -1 (false: nothing happens). Else the nuclear
		/// test is reported (0x29), an improvement (below 0x16) is added, the shields paid; a human city and every wonder
		/// are announced (a wonder of a computer player shows its city to all; the human may see the city screen, with the
		/// option 8 and the disk) and the shields cleared, a computer city picks its next production (after the palace -2
		/// first) and counts a unit in production. The locals of HandleCity are used as in the original.
		/// </summary>
		private bool CompleteImprovement(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;

			if ((short)((short)BuildingWordAt((ushort)(-town.PresentMakingIdx * 0x1e), BuildingTable.Price) *
				(short)this.round.CostFactor) > town.MaterialsTally)
			{
				return false;
			}

			this.round.Value = (ushort)-town.PresentMakingIdx;
			short built = (short)this.round.Value;

			if (built > 0x18)
			{
				// A wonder: only the first city builds it

				if (this.owner.World.MarvelTownIdx[built - 24] != -1)
				{
					this.round.Value = 0xffff;
				}
				else
				{
					this.owner.World.MarvelTownIdx[built - 24] = townIdx;
					this.owner.UnitOps.AttachReplayContent(10, (byte)CityOwner, (byte)(built - 0x18));
				}
			}
			else if (TestImprovement(townIdx, built))
			{
				// The city has it already
				this.round.Value = 0xffff;
			}

			if (this.round.Value == 0xffff)
				return false;

			if (built == 0x29)
			{
				Message("Sensors report a\nNUCLEAR WEAPONS test\nnear ")
					.City(townIdx)
					.Text("!\n");
				this.owner.Services.PerformSound(0x24, 0);
				this.owner.Paper.DisplayNewspaper(-1);
				this.owner.Services.PerformSound(1, 0);
			}

			if (built < 0x16)
			{
				// The improvement
				uint mask = 1u << ((byte)(built - 1) & 0xff);
				town.StructureMarkers0 |= (ushort)mask;
				town.StructureMarkers1 |= (ushort)(mask >> 16);
			}

			// The price paid
			town.MaterialsTally -= (short)((short)BuildingWord(-town.PresentMakingIdx, BuildingTable.Price) *
				(short)this.round.CostFactor);

			bool humanPlayed = this.owner.World.IsHuman(owner);

			if (!humanPlayed && built <= 0x18)
			{
				// A computer city picks its next production (after the palace -2 first)
				if (built == 1)
				{
					town.PresentMakingIdx = -2;
				}

				town.PresentMakingIdx = (sbyte)(byte)this.owner.Advisors.PickCityProduction(owner, townIdx);

				StartMakingUnit(town, owner);

				return true;
			}

			// The announcement (a wonder of a computer player shows its city to all)
			if (humanPlayed)
			{
				CityAgain = 1;
			}
			else
			{
				this.owner.World.ChartSight[town.Place.X, town.Place.Y] = -1;
			}

			EmptyMessage()
				.City(townIdx)
				.Text(" builds\n");
			this.owner.Words.Append(BuildingRecordText(built));
			this.owner.Words.Append(".\n");

			bool screen = false;

			if (owner == this.owner.World.PersonParticipantIdx && (this.owner.World.SessionPrefMarkers & 8) != 0 && (built <= 0x15 || built > 0x18))
			{
				if ((town.ConditionMarker & 0x10) == 0 && built != 1)
				{
					screen = this.owner.Startup.QueryForDisk(1, 0) != 0;
				}
			}

			if (screen)
				this.owner.CityScene.DisplayCityScreen(townIdx, (short)this.round.Value);
			else
				this.owner.Paper.DisplayNewspaper(townIdx);

			town.MaterialsTally = 0;
			return true;
		}

		/// <summary>Whether the city has the improvement</summary>
		private bool TestImprovement(short townIdx, short improvement)
		{
			City town = this.owner.World.Towns[townIdx];
			uint mask = 1u << ((byte)(improvement - 1) & 0xff);

			return ((mask & town.StructureMarkers0) | ((mask >> 16) & town.StructureMarkers1)) != 0;
		}

		/// <summary>
		/// A spaceship part (0xe8 0x16 - 0x18) of a player whose spaceship has not left: the human adds it on the spaceship
		/// screen; a computer player (0xc8 = the success it computes) gets the shields back for the structures and modules
		/// it can use and keeps building them, then launches when the success says so, or (with 40 percent of the computer
		/// victories, 0x28 known by the human) when it is sure, the human is ahead, or arrives later, or a third player is
		/// rich (the gold of the player 0x... LiveRealms, as translated). The locals of HandleCity are used as in the
		/// original.
		/// </summary>
		private void BuildSpaceshipPart(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			short person = this.owner.World.PersonParticipantIdx;
			short part = (short)this.round.Value;

			if (part < 0x16 || part > 0x18 || (this.owner.World.StarshipMarkers & (1 << (owner & 0xff))) != 0)
				return;

			if (owner == person)
			{
				this.owner.RocketScreen.AttachSpaceshipPart(owner, (ushort)(part - 22));
				return;
			}

			this.round.Best = this.owner.RocketScreen.ComputeSpaceshipSuccess(owner, (short)((ushort)(part - 0x16)));

			if (this.round.Best != 0 && part < 0x18)
			{
				// The shields come back, the city builds the part again
				ushort refund = (ushort)((short)BuildingWordAt((ushort)(part * 0x1e), BuildingTable.Price) * (short)this.round.CostFactor);
				town.MaterialsTally += (short)refund;

				if (town.PresentMakingIdx <= -0x16 && town.PresentMakingIdx >= -0x18)
				{
					town.PresentMakingIdx = (sbyte)(byte)((byte)-part - 1);
				}

				this.round.Best = (ushort)(this.round.Best - 1);
			}

			bool launch;
			short success = (short)this.round.Best;

			if ((this.owner.World.StarshipMarkers & (1 << (owner & 0xff))) != 0 || this.owner.World.BotStarshipVictoryRatio < 40 || success != 0)
			{
				launch = success != 0;
			}
			else if (this.owner.Advisors.PlayerKnowsTechnology(person, 0x28) == 0)
			{
				launch = false;
			}
			else if (this.owner.World.BotStarshipVictoryRatio > 75)
			{
				launch = true;
			}
			else
			{
				Player ahead = this.owner.World.Participants[person];
				Player computer = this.owner.World.Participants[owner];

				if (ahead.Standing > computer.Standing)
				{
					launch = true;
				}
				else
				{
					launch = false;
					bool decided = false;

					if ((this.owner.World.StarshipMarkers & (1 << (person & 0xff))) != 0)
					{
						// Both spaceships fly: the computer launches when it arrives first
						short computerArrives = (short)(ushort)(computer.StarshipArrivalSeason - this.owner.World.Season);
						short humanArrives = (short)(ushort)(ahead.StarshipArrivalSeason - this.owner.World.Season);

						if (computerArrives <= humanArrives)
						{
							launch = true;
							decided = true;
						}
					}

					if (!decided)
					{
						launch = ((ushort)this.owner.World.StarshipMarkers & (ushort)(1 << (byte)(((person & 0xff) + 8) & 0xff))) != 0 &&
							this.owner.World.Participants[this.owner.World.LiveRealms].Gold > 1000;
					}
				}
			}

			if (launch)
				this.owner.RocketScreen.FireSpaceship(owner);
		}

		/// <summary>
		/// What some completed improvements do (0xe8): the palace (1) leaves every city of the owner; the human hears of
		/// the move (of a computer player only with an embassy, 0x40): then the capital is this city (HorizBegin, the
		/// palace bit set again; without the message it stays off, as translated); the wonder 0x26 gives two advances; the
		/// wonder 0x2b of the human shows every foreign city (its size, the tile seen). The locals of HandleCity are used
		/// as in the original.
		/// </summary>
		private void ApplyImprovement(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			short person = this.owner.World.PersonParticipantIdx;
			short built = (short)this.round.Value;

			if (built == 1)
			{
				// The palace leaves every city of the owner
				for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
				{
					this.round.OtherCity = otherIdx;
					City other = this.owner.World.Towns[otherIdx];

					if (other.ParticipantIdx == owner)
					{
						other.StructureMarkers0 &= 0xfffe;
						other.StructureMarkers1 &= 0xffff;
					}
				}

				this.round.OtherCity = 0x80;

				bool told = owner == person;

				if (!told)
				{
					told = (this.owner.World.Participants[person].Relations[owner] & 0x40) != 0;
				}

				if (told)
				{
					this.owner.Words.Clear();

					if (owner != person)
					{
						Message("Diplomats report:\n")
							.Style(1);
					}

					this.owner.Words.Append(CivilizationNameAt((ushort)(owner << 1)));
					this.owner.Words.Append(" capital\nmoved to ");
					this.owner.Conquest.ObtainCityLabel(townIdx);
					this.owner.Words.Append(".\n");
					DialogStyle = 5;
					this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 80, 80);

					this.owner.World.Participants[owner].HorizBegin = (short)town.Place.X;
					town.StructureMarkers0 |= 1;
					town.StructureMarkers1 |= 0;
				}
			}

			if (built == 0x26)
			{
				// Two advances
				this.owner.Startup.DisplayMouseCursorFully();
				this.owner.Advisors.UncoverTechnology(owner, 0);
				this.owner.Advisors.UncoverTechnology(owner, 0);
				this.owner.Startup.ConcealMouseCursorToSavedLevel();
			}

			if (built != 0x2b || owner != person)
				return;

			// Every foreign city shown to the human
			for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
			{
				this.round.OtherCity = otherIdx;
				City other = this.owner.World.Towns[otherIdx];

				if (other.ConditionMarker == 0xff || other.ParticipantIdx == owner)
					continue;

				other.ShownExtent = other.TrueExtent;

				this.owner.World.ChartSight[other.Place.X, other.Place.Y] |= (sbyte)(byte)(1 << (owner & 0x1f));
				this.owner.MapOps.KeepKnownTileImprovements(other.Place.X, other.Place.Y);
			}

			this.round.OtherCity = 0x80;
		}

		/// <summary>
		/// A human city with the flag 0x10 (production chosen by the advisor) picks its production again when it has no
		/// shields, or builds an improvement and nothing was completed (0xe8 -1): 0x63 (none chosen) clears the flag, else
		/// the count of units in production follows the change. The locals of HandleCity are used as in the original.
		/// </summary>
		private void AdviseHumanProduction(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;

			if (owner != this.owner.World.PersonParticipantIdx)
				return;

			if ((town.ConditionMarker & 0x10) == 0)
				return;

			if (town.MaterialsTally != 0 && (town.PresentMakingIdx >= 0 || this.round.Value != 0xffff))
				return;

			this.round.Value = this.owner.Advisors.PickCityProduction(owner, townIdx);
			DrawingBox.TypefaceIdx = 2;

			if (this.round.Value == 0x63)
			{
				// Nothing chosen: the advisor stops
				town.ConditionMarker &= 0xef;
				return;
			}

			CityAgain = 0;

			StopMakingUnit(town, owner);
			town.PresentMakingIdx = (sbyte)(byte)this.round.Value;
			StartMakingUnit(town, owner);
		}

		/// <summary>
		/// A computer city (one played by a human goes on with the city screen) buys shields with gold (0xcc the purchase):
		/// a city with the flag 0x10 picks a new production first; units of the role (the attitude of the continent 0x104:
		/// 1, 2 or 5) for a 64th of the gold, spaceship parts while the human's spaceship flies for a 128th, an improvement
		/// of a city with the flag 1 up to an 8th, a unit where the city tile has no owner (or with the flag 0x10) up to a
		/// third, the production -1 up to a third, nuclear units (the condition 0x19, as translated) up to a quarter; above
		/// 2000 gold a 512th more. The shields grow by the purchase, the gold falls by twice it, the flag 0x10 is cleared.
		/// The locals of HandleCity are used as in the original.
		/// </summary>
		private void BuyForComputerCity(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)CityOwner;
			Player player = this.owner.World.Participants[owner];

			if ((town.ConditionMarker & 0x10) != 0)
				this.owner.Bot.PickNewCityProduction(townIdx);

			this.round.Amount = 0;

			// The attitude of the continent (as translated: the landmass record of the player numbered by the continent)
			ushort continent = this.owner.MapOps.ObtainContinentID((short)this.round.Horiz, (short)this.round.Vert);
			this.round.Approach = continent >= this.owner.World.Participants.Length ? (ushort)0 :
				(ushort)this.owner.World.Participants[continent].Landmasses[owner].Approach;
			ushort approach = this.round.Approach;

			if ((approach == 1 || approach == 2 || approach == 5) && this.round.Value != 0)
			{
				if (town.PresentMakingIdx >= 0)
				{
					// A unit of the role the continent needs
					if (UnitKindWord(town.PresentMakingIdx, UnitKindTable.TroopGroup) == approach)
						this.round.Amount = (ushort)(player.Gold / 64);
				}
			}

			// A spaceship part while the human's spaceship flies
			if (((ushort)this.owner.World.StarshipMarkers & (ushort)(1 << (byte)(((this.owner.World.PersonParticipantIdx & 0xff) + 8) & 0xff))) != 0)
			{
				if (-town.PresentMakingIdx >= 0x16 && -town.PresentMakingIdx <= 0x18)
					this.round.Amount = (ushort)(player.Gold / 128);
			}

			// An improvement of a city with the flag 1
			if ((town.ConditionMarker & 1) != 0 && town.PresentMakingIdx < 0 && town.MaterialsTally != 0)
			{
				this.round.Amount = (ushort)this.owner.Helpers.VerifyValueSpan(
					(short)BuildingWord(-town.PresentMakingIdx, BuildingTable.Price) * (short)this.round.CostFactor -
						town.MaterialsTally,
					0, player.Gold / 8);
			}

			// A unit where the city tile has no owner (or with the flag 0x10)
			bool unowned = (ushort)(this.owner.MapOps.ObtainTileOwner((short)this.round.Horiz, (short)this.round.Vert) + 1) == 0;

			if ((unowned || (town.ConditionMarker & 0x10) != 0) && town.PresentMakingIdx >= 0 && town.MaterialsTally != 0)
				BuyShortfall(townIdx, player.Gold / 3);

			// The production -1

			if (town.PresentMakingIdx == -1 && town.MaterialsTally != 0)
				BuyShortfall(townIdx, player.Gold / 3);

			// Nuclear units (the condition marker compared with 0x19, as translated)

			if (town.ConditionMarker == 0x19)
			{
				if (player.LiveTroops[25] == 0)
				{
					if (town.MaterialsTally != 0)
						BuyShortfall(townIdx, player.Gold / 4);
				}
			}

			if (player.Gold > 2000)
			{
				this.round.Amount = (ushort)(this.round.Amount + ((ushort)(player.Gold / 512)));
			}

			// The purchase
			town.MaterialsTally += (short)this.round.Amount;
			player.Gold -= (short)((ushort)(this.round.Amount << 1));
			town.ConditionMarker &= 0xef;
		}

		/// <summary>0xcc: the shields the unit still needs (its price x 0x4a less the shields), within 0 and the limit</summary>
		private void BuyShortfall(short townIdx, int limit)
		{
			City town = this.owner.World.Towns[townIdx];

			this.round.Amount = (ushort)this.owner.Helpers.VerifyValueSpan(
				(short)UnitKindWord(town.PresentMakingIdx, UnitKindTable.Price) * (short)this.round.CostFactor -
					town.MaterialsTally,
				0, limit);
		}
	}
}
