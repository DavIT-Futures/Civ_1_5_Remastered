
using System;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>The city round: civil order, celebration, research, pollution, upkeep, the steps of HandleCity</summary>
	partial class Cities
	{
		/// <inheritdoc/>
		public ushort HandleCity(short townIdx, short marker)
		{
			using var _ = this.Processor.Journal.Scope($"HandleCity({townIdx}, {marker})");

			// HandleCity calls itself (the advisor counts the city), each call has its own locals as its own frame
			CityRound outer = this.round;
			this.round = new CityRound(this.Processor);

			// (a city that is gone, also in its turn, is at most of size 1: HandleAllCities reads no result of it)
			ushort result = 0;

			if (this.Owner.World.Towns[townIdx].ConditionMarker != 0xff)
				result = RunCity(townIdx, marker);

			this.round = outer;

			return result;
		}

		/// <summary>
		/// The shields and the trade of the city back as they were before the factories and the power (0x70dc / 0x70de from
		/// the words 0x10a / 0xa of the frame) before the city screen counts them again; the trade
		/// </summary>
		private void RestoreShieldsAndTrade()
		{
			CityTurn.Shields = this.round.ShieldsKept;
			CityTurn.Trade = this.round.TradeKept;
		}

		/// <summary>The city stops making its unit (the owner's count of units in making)</summary>
		private void StopMakingUnit(City town, short owner)
		{
			this.Owner.World.Participants[owner].StopMaking(town.PresentMakingIdx);
		}

		/// <summary>The city starts making its unit (the owner's count of units in making)</summary>
		private void StartMakingUnit(City town, short owner)
		{
			this.Owner.World.Participants[owner].StartMaking(town.PresentMakingIdx);
		}

		/// <summary>The advisor picks the production of the city; the units in making of the owner counted over again</summary>
		private void RepickCityProduction(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;

			StopMakingUnit(town, owner);
			town.PresentMakingIdx = (sbyte)(byte)this.Owner.Advisors.PickCityProduction(owner, townIdx);
			StartMakingUnit(town, owner);
		}

		/// <summary>
		/// The order of the city at the end of its turn (0xe8 the happy less the unhappy, 0x70e2 - 0x70e4): fewer happy than
		/// unhappy is civil disorder (the flag 1; a human hears of it – with the riot of the city view when the option 8 is on,
		/// the first time – the computer picks another production), under democracy a disorder that goes on overthrows the
		/// government; otherwise a disorder ends ("Order restored") and the taxes 0xe17a are paid, but not in anarchy.
		/// </summary>
		private void ApplyCivilOrder(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			bool human = this.Owner.World.IsHuman(owner);

			this.round.Value = (ushort)(CityTurn.Happy - CityTurn.Unhappy);

			if ((short)this.round.Value < 0)
			{
				if (human)
				{
					Message("Civil Disorder in\n")
						.City(townIdx)
						.Text("! Mayor\nflees in panic.\n");
					this.Owner.Services.PerformSound(0x24, 0);

					if ((this.Owner.World.SessionPrefMarkers & 8) != 0 && (town.ConditionMarker & 1) == 0 && Turn.RiotShown == 0)
					{
						// The riot in the view of the city, once
						this.Owner.CityScene.ShowCityScreen(townIdx, -2);
						this.Owner.CityScene.RiotAnimation();
						this.Owner.Startup.AskForDisk(0, 1);
						this.Owner.TurnHooks.RepaintMainScreen();
						Turn.RiotShown = 1;
					}
					else
					{
						Screen.DialogStyle = 4;
						this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80);
					}

					this.Owner.Services.PerformSound(1, 0);

					if ((town.ConditionMarker & 1) == 0)
					{
						CityTurn.Again = 1;

						if ((this.Owner.World.SessionPrefMarkers & 1) != 0)
							this.Owner.HelpSystem.ShowHelpPopup(GameTexts.TopicDisorder);
					}
				}

				if ((town.ConditionMarker & 1) == 0)
				{
					town.ConditionMarker |= 1;

					if (!human)
						RepickCityProduction(townIdx);

					this.Owner.TurnLogic.RepaintTileIfVisible(this.round.Horiz, this.round.Vert);
				}
				else if (this.Owner.World.Participants[owner].RegimeKind == 5)
				{
					// The disorder goes on under democracy
					this.Owner.Services.PerformSound(0x24, 0);
					Message("Discontented citizens of\n")
						.City(townIdx)
						.Text(" revolt:\nGovernment Collapses!\n");

					if (owner == this.Owner.World.PersonParticipantIdx)
					{
						this.Owner.Paper.ShowNewspaper(townIdx);
						this.Owner.World.Participants[owner].Relations[0] |= 4;
					}

					this.Owner.Politics.SwitchGovernment(owner, 0);
					this.Owner.Services.PerformSound(1, 0);
				}

				return;
			}

			if ((town.ConditionMarker & 1) != 0)
			{
				town.ConditionMarker &= 0xfe;

				if (human)
				{
					Screen.DialogStyle = 4;
					Message("Order restored\nin ")
						.City(townIdx)
						.Text(".\n")
						.Dialog(100, 80);
				}
				else
				{
					RepickCityProduction(townIdx);
				}

				this.Owner.TurnLogic.RepaintTileIfVisible(this.round.Horiz, this.round.Vert);
			}

			// The taxes, but not in anarchy
			if (this.Owner.World.Participants[owner].RegimeKind != 0)
				this.Owner.World.Participants[owner].Gold += (short)CityTurn.Taxes;
		}

		/// <summary>
		/// The celebration of the city ("We love the ... day", the flag 0x40): no unhappy citizens (0x70e4), bigger than 2, at
		/// least half of the citizens happy (0x70e2), not in anarchy. It starts with a message for a human (the city view with
		/// the option 8, else the newspaper); going on under republic or democracy the city grows while it has food to spare
		/// (from 10 only with an aqueduct, 0x100); it ends (without disorder, 0xe8 not below 0) with "celebration canceled".
		/// </summary>
		private void ApplyCelebration(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			Player player = this.Owner.World.Participants[owner];
			bool human = this.Owner.World.IsHuman(owner);

			bool celebrates = CityTurn.Unhappy == 0 && town.TrueExtent > 2 &&
				(short)((town.TrueExtent + 1) / 2) <= (short)CityTurn.Happy && player.RegimeKind != 0;

			if (celebrates)
			{
				if ((town.ConditionMarker & 0x40) == 0)
				{
					// The celebration starts
					if (human && player.RegimeKind != 0)
					{
						Message("'We love the ")
							.Text(GovernmentTitle(player.RegimeKind))
							.Text("'\nday celebrated in\n")
							.City(townIdx)
							.Text("!\n");
						this.Owner.Services.PerformSound(0x22, 0);

						if ((this.Owner.World.SessionPrefMarkers & 8) != 0)
						{
							this.Owner.CityScene.ShowCityScreen(townIdx, -2);
							this.Owner.CityScene.DrawCitySceneryForHuman();
							this.Owner.Startup.AskForDisk(0, 1);
							this.Owner.TurnHooks.RepaintMainScreen();
						}
						else
						{
							this.Owner.Paper.ShowNewspaper(townIdx);
						}

						this.Owner.Services.PerformSound(1, 0);
					}

					town.ConditionMarker |= 0x40;
				}
				else if (player.RegimeKind >= 4)
				{
					// Growth while it goes on under republic or democracy, with food to spare
					short eaten = (short)((town.TrueExtent * 2) + ((short)this.round.SettlerFood * (short)CityTurn.Settlers));

					if (eaten < (short)CityTurn.Food && (town.TrueExtent < 10 || town.Has(CityStructure.Aqueduct)))
						town.TrueExtent++;
				}

				return;
			}

			if ((short)this.round.Value < 0 || (town.ConditionMarker & 0x40) == 0)
				return;

			// The celebration ends
			if (human && player.RegimeKind != 0)
			{
				this.Owner.Services.PerformSound(0x23, 0);
				Message("'We love the ")
					.Text(GovernmentTitle(player.RegimeKind))
					.Text("'\ncelebration canceled\nin ")
					.City(townIdx)
					.Text(".\n");
				this.Owner.Paper.ShowNewspaper(townIdx);
				this.Owner.Services.PerformSound(1, 0);
			}

			town.ConditionMarker &= 0xbf;
		}

		/// <summary>
		/// The science of the city (0xe8, from 0x70e6; none in anarchy) goes to the research of its owner (InquiryHeadway):
		/// for the human a finished or empty goal first starts a new one (with the flag 8 of DiagMarkers the advance is
		/// uncovered), on the easiest level the science counts twice for an advance others found, the wonder 0x14 adds half.
		/// The cost (0xf4: 14 less the level for the computer, 6 and twice the level for the human, more 0xd2de; for the human
		/// at least 11 less its advances) by the advances found; past it the research starts again (the advance uncovered with
		/// the flag 8), not for the human while 0xb1e8 is set.
		/// </summary>
		private void ApplyResearch()
		{
			short owner = (short)CityTurn.Owner;
			short person = this.Owner.World.PersonParticipantIdx;
			Player player = this.Owner.World.Participants[owner];
			Player human = this.Owner.World.Participants[person];

			this.round.Value = CityTurn.Science;

			if (owner == person)
			{
				bool finished = human.PresentInquiryIdx != -1 ?
					this.Owner.Advisors.PlayerKnowsTechnology(owner, human.PresentInquiryIdx) != 0 :
					player.InquiryHeadway != 0;

				if (finished)
				{
					human.PresentInquiryIdx = -1;
					UncoverTechnologyWhenFlagged(owner);
					player.InquiryHeadway = 0;
				}
			}

			if (player.RegimeKind == 0)
				this.round.Value = 0;

			player.InquiryHeadway += (short)this.round.Value;

			// On the easiest level an advance others found costs half
			if (owner == person && this.Owner.World.HardnessTier == 0 && human.PresentInquiryIdx >= 0 &&
				(this.Owner.World.ScienceLeadingFoundVia[human.PresentInquiryIdx] & 7) != 0)
			{
				player.InquiryHeadway += (short)this.round.Value;
			}

			if (PlayerEnjoysWonderEffect(owner, 0x14) != 0)
				player.InquiryHeadway += (short)((short)this.round.Value / 2);

			// The cost of an advance
			this.round.Measure = (ushort)(owner != person ? 14 - this.Owner.World.HardnessTier : (this.Owner.World.HardnessTier << 1) + 6);
			this.round.Measure = (ushort)(this.round.Measure + Turn.TechnologyLevel);

			if (owner == person && (short)(11 - player.FoundScienceTally) > (short)this.round.Measure)
				this.round.Measure = (ushort)(11 - player.FoundScienceTally);

			if (owner == person && CityTurn.Again != 0)
				return;

			short progress = (short)(player.InquiryHeadway / (this.Owner.World.Season < 0 ? 1 : 2));

			if (progress > (short)(player.FoundScienceTally * (short)this.round.Measure))
			{
				player.InquiryHeadway = 0;
				UncoverTechnologyWhenFlagged(owner);
			}
		}

		/// <summary>With the flag 8 of DiagMarkers the player uncovers an advance (the mouse shown meanwhile)</summary>
		private void UncoverTechnologyWhenFlagged(short participant)
		{
			if ((this.Owner.World.DiagMarkers & 8) == 0)
				return;

			this.Owner.Startup.ShowMouseCursorFully();
			this.Owner.Advisors.UncoverTechnology(participant, 0);
			this.Owner.Startup.HideMouseCursorToSavedLevel();
		}

		/// <summary>
		/// The pollution of the city (0xe0): the shields over the divisor 0x6c98 less 20, a quarter of the size by the
		/// advances 0xb882 more (a quarter rounded towards zero; the original takes the absolute value, -32768 can't come out
		/// of the size)
		/// </summary>
		private short CountPollution(City town)
		{
			short divisor = (short)Turn.PollutionFactor;

			if (divisor == 0)
				throw new Exception("Division by zero");

			int quotient = (short)CityTurn.Shields / divisor;

			if (quotient != (short)quotient)
				throw new Exception("Division error");

			short pollution = (short)((short)(quotient - 20) + ((short)(town.TrueExtent * (short)Turn.ContentAdvances) / 4));
			this.round.Pollution = (ushort)pollution;

			return pollution;
		}

		/// <summary>
		/// The end of the turn of a city: the score of the owner grows by the size and the happy less the unhappy. For the human
		/// only (false: not the human): the pollution may land on a square of the city (twice 0xe0 against a draw below 256 less
		/// half the advances by the level; 0xe8 the square, 0xc6 / 0xd2 its place: not polluted, not the sea, not the city);
		/// a city in disorder with a nuclear plant (0x10 of the second word) may melt down, one time in three, until the
		/// advance 0x42.
		/// </summary>
		private bool ApplyScoreAndPollution(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			Player player = this.Owner.World.Participants[owner];
			short person = this.Owner.World.PersonParticipantIdx;

			player.Tally += (short)(town.TrueExtent + CityTurn.Happy - CityTurn.Unhappy);

			if (owner != person)
				return false;

			// The pollution
			short pollution = CountPollution(town);
			ushort chance = (ushort)(0x100 - (short)((short)(player.FoundScienceTally * this.Owner.World.HardnessTier) / 2));

			if ((short)(pollution << 1) > (short)(ushort)this.Owner.Libc.Dice.DrawInt(chance))
			{
				this.round.Value = (ushort)this.Owner.Libc.Dice.DrawInt(20);
				var delta = this.Owner.TownDeltas[this.round.Value];
				this.round.TileHoriz = (ushort)(short)(delta.X + (short)this.round.Horiz);
				this.round.TileVert = (ushort)(short)(delta.Y + (short)this.round.Vert);
				short horiz = (short)this.round.TileHoriz;
				short vert = (short)this.round.TileVert;

				if ((this.Owner.MapOps.ObtainTileImprovements(horiz, vert) & 0x40) == 0 && this.Owner.MapOps.ObtainTerrainType(horiz, vert) != 0xa &&
					(this.Owner.MapOps.ObtainTileImprovements(horiz, vert) & 1) == 0)
				{
					AttachSmog(horiz, vert);
					this.Owner.MapOps.FocusMapView(person, horiz - 8, vert - 6);
					this.Owner.MapOps.RepaintMapTile(horiz, vert);
					Screen.DialogStyle = 6;
					Message("Pollution near ")
						.City(townIdx)
						.Text(".\n")
						.Dialog(80, 64);
				}
			}

			// The nuclear plant melts down
			if ((town.ConditionMarker & 1) != 0 && town.Has(CityStructure.NuclearPlant) && this.Owner.Libc.Dice.DrawInt(3) == 0 &&
				this.Owner.Advisors.PlayerKnowsTechnology(owner, 0x42) == 0)
			{
				short horiz = (short)this.round.Horiz;
				short vert = (short)this.round.Vert;

				this.Owner.MapOps.FocusMapView(person, horiz - 8, vert - 6);
				this.Owner.Diplomats.DrawExplosion(horiz, vert);
				this.Owner.Combat.EnactNuclearFallout(horiz, vert);
				town.Remove(CityStructure.NuclearPlant);
				this.Owner.Services.PerformSound(0x24, 0);
				Message("Nuclear Catastrophe\nin ")
					.City(townIdx)
					.Text("!\nContamination feared!\n");
				this.Owner.Paper.ShowNewspaper(townIdx);
				this.Owner.Services.PerformSound(1, 0);
			}

			return true;
		}

		/// <summary>
		/// The upkeep of the improvements of the human's city (0xbc the improvement 1 - 23, its word at 0xbd2; not in
		/// anarchy; the palace costs one more from the level 2 and from 4, and with each of the advances 0x22 and 0x25):
		/// when the money runs out the improvement is sold ("can't maintain", ten times its word at 0xbd0).
		/// </summary>
		private void PayImprovementUpkeep(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];
			short owner = (short)CityTurn.Owner;
			Player player = this.Owner.World.Participants[owner];
			ushort improvement = 1;

			for (; (short)improvement < 0x18; improvement++)
			{
				this.round.Item = improvement;
				uint bit = 1u << (byte)improvement;

				if ((town.Structures & bit) == 0 || player.RegimeKind == 0)
					continue;

				player.Gold -= (short)BuildingWord(improvement + 1, BuildingTable.Upkeep);

				if (improvement == 1)
				{
					if (this.Owner.World.HardnessTier >= 2)
						player.Gold--;

					if (this.Owner.World.HardnessTier >= 4)
						player.Gold--;

					if (this.Owner.Advisors.PlayerKnowsTechnology(owner, 0x22) != 0)
						player.Gold--;

					if (this.Owner.Advisors.PlayerKnowsTechnology(owner, 0x25) != 0)
						player.Gold--;
				}

				if (player.Gold >= 0)
					continue;

				// Sold for the debt
				player.Gold = 0;
				town.Structures &= ~bit;
				EmptyMessage()
					.City(townIdx)
					.Text("\ncan't maintain\n")
					.Text(BuildingText(improvement))
					.Text(".\n")
					.Style(4)
					.Dialog(100, 80);
				player.Gold += (short)(0xa * BuildingWord(improvement + 1, BuildingTable.Price));
			}

			this.round.Item = improvement;
		}

		/// <summary>
		/// The very end of HandleCity: a city with the flag 4 and no foreign unit next to it keeps its list of units; the
		/// mouse shown again; it gives back the happy less the unhappy.
		/// </summary>
		private ushort FinishHandleCity(short townIdx)
		{
			City town = this.Owner.World.Towns[townIdx];

			if ((town.ConditionMarker & 4) != 0)
			{
				if (this.Owner.UnitOps.ContainsAdjacentForeignUnit(town.ParticipantIdx, town.Place.X, town.Place.Y) == 0)
					this.Owner.UnitOps.KeepUnitsInCityList(townIdx);
			}

			DrawingBox.TypefaceIdx = 1;
			this.Owner.Startup.ShowMouseCursor();

			return (ushort)(CityTurn.Happy - CityTurn.Unhappy);
		}

		/// <summary>
		/// The start of HandleCity: the locals 0xb8, 0xda, 0xfe, 0xca and 0x2496 cleared, the mouse hidden, the 5 x 5 array
		/// of the tiles around (BP-0xa6) cleared; 0x653e the city; the materials outside 0 - 999 cleared. Over the other
		/// cities: 0xb2 the distance to the nearest one (at most 7), 0x6b30 the distance to the nearest palace of the owner
		/// (0 with an own palace, else at most 0x20), the tiles within two of the city worked by a city at most 5 away
		/// marked in the array. Then 0x6548 the owner, 0x6540 0x15, 0xc2 / 0xd0 the map view, 0x48 the waste (1 for
		/// despotism and anarchy, else 2), 0xb882 the advances that make citizens content (without the wonder 0x1000), 0x4a
		/// 10 the content citizens (for a computer player 16 - 2 x the level, 2 less on the levels above 1 for the leader
		/// of a later game), 0xe8b8 the specialists (bits 26 - 31 of the workers), for a computer player in the turn
		/// (marker 0) a city without the flag 1 has none, else all workers are taken off. The locals of HandleCity are used
		/// as in the original.
		/// </summary>
		private void PrepareCity(short townIdx, short marker)
		{
			City town = this.Owner.World.Towns[townIdx];

			CityTurn.ScreenTab = 0;
			this.round.CursorOn = 0;
			this.round.ListStart = 0;
			this.round.ListEnd = 0;
			this.round.ListFlags = 0;

			this.Owner.Startup.HideMouseCursor();

			// The 5 x 5 array of the tiles around
			for (ushort column = 0; column < 5; column++)
			{
				this.round.Horiz = column;

				for (ushort row = 0; row < 5; row++)
				{
					this.round.Vert = row;
					this.round.SetFrame(0xa6 - ((ushort)(column * 0xa + row * 2)), 0);
				}

				this.round.Vert = 5;
			}

			this.round.Horiz = (ushort)(short)town.Place.X;
			this.round.Vert = (ushort)(short)town.Place.Y;
			CityTurn.ShownCity = (ushort)townIdx;
			this.round.Distance = 7;
			CityTurn.PalaceDistance = (ushort)(town.Has(CityStructure.Palace) ? 0 : 0x20);

			if (town.MaterialsTally > 999 || town.MaterialsTally < 0)
				town.MaterialsTally = 0;

			for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
			{
				this.round.OtherCity = otherIdx;
				City other = this.Owner.World.Towns[otherIdx];

				if (other.ConditionMarker == 0xff || otherIdx == (ushort)townIdx)
					continue;


				short distance = (short)(ushort)this.Owner.Helpers.ObtainDistance((short)this.round.Horiz, other.Place.X, (short)this.round.Vert, other.Place.Y);
				this.round.Target = (ushort)distance;

				if (distance < (short)this.round.Distance && distance != 0)
					this.round.Distance = (ushort)distance;

				if (other.ParticipantIdx == town.ParticipantIdx && other.Has(CityStructure.Palace) && distance < (short)CityTurn.PalaceDistance)
				{
					// The nearest palace of the owner
					CityTurn.PalaceDistance = (ushort)distance;
				}

				if (distance > 5)
					continue;

				// The tiles near the city worked by the other city (and the other city itself, tile 20)
				for (ushort tile = 0; tile <= 20; tile++)
				{
					this.round.Index = tile;
					uint mask = 1u << tile;
					if ((ushort)(((mask >> 16) & other.LaborerMarkers1) | (ushort)(mask & other.LaborerMarkers0)) == 0 && tile != 20)
						continue;

					short horizLoc = (short)(other.Place.X + this.Owner.TownDeltas[tile].X);
					short vertLoc = (short)(other.Place.Y + this.Owner.TownDeltas[tile].Y);
					this.round.TileHoriz = (ushort)horizLoc;
					this.round.TileVert = (ushort)vertLoc;

					if ((short)(ushort)this.Owner.Libc.AbsoluteValue((short)((ushort)(horizLoc - (short)this.round.Horiz))) > 2)
						continue;

					if ((short)(ushort)this.Owner.Libc.AbsoluteValue((short)((ushort)(vertLoc - (short)this.round.Vert))) > 2)
						continue;

					int frameOffset = (horizLoc - (short)this.round.Horiz) * 0xa +
						((vertLoc - (short)this.round.Vert) << 1);
					this.round.SetFrame(0x8e - frameOffset, 1);
				}

				this.round.Index = 21;
			}

			this.round.OtherCity = 0x80;

			// The tile of the city itself
			this.round.CityTileNeighbour = 0;
			CityTurn.Owner = (ushort)town.ParticipantIdx;
			CityTurn.Tiles = 0x15;
			this.round.MapViewHoriz = MapView.Horiz;
			this.round.MapViewVert = MapView.Vert;

			short owner = (short)CityTurn.Owner;
			this.round.SettlerFood = (ushort)(this.Owner.World.Participants[owner].RegimeKind <= 1 ? 1 : 2);

			// The advances that make citizens content, unless the wonder 0x1000 is in the city
			Turn.ContentAdvances = 0;

			if (!town.Has(CityStructure.MassTransit))
			{
				foreach (short advance in new short[] { 0x23, 0x3a, 0x29, 0x3c })
				{
					if (this.Owner.Advisors.PlayerKnowsTechnology(owner, advance) != 0)
						Turn.ContentAdvances = (ushort)(Turn.ContentAdvances + 1);
				}
			}

			this.round.CostFactor = 0xa;
			CityTurn.Specialists = (ushort)((((uint)town.LaborerMarkers1 << 16) | town.LaborerMarkers0) >> 0x1a);

			if (owner == this.Owner.World.PersonParticipantIdx)
				return;

			// A computer player: the content citizens by the level
			this.round.CostFactor = (ushort)-(this.Owner.World.HardnessTier * 2 - 0x10);

			if (this.Owner.World.Season >= 0 &&
				this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].Standing == 7 &&
				this.Owner.World.HardnessTier > 1)
			{
				this.round.CostFactor = (ushort)(this.round.CostFactor - 2);
			}

			if (marker != 0)
				return;

			if ((town.ConditionMarker & 1) == 0)
			{
				CityTurn.Specialists = 0;
			}
			else
			{
				town.LaborerMarkers0 = 0;
				town.LaborerMarkers1 = 0;
			}
		}

		/// <summary>
		/// HandleCity on a city that exists: the round from the start (again after most buttons of the city screen), the
		/// economy (again from the citizens after the others), the city screen while it is open, then the end of the turn.
		/// </summary>
		private ushort RunCity(short townIdx, short marker)
		{
			// The neighbouring cities, the palace, the governments, the content citizens
			PrepareCity(townIdx, marker);

			bool fromStart = true;

			while (true)
			{
				if (fromStart && !StartCityRound(townIdx, marker))
					return 0;

				if (!CountCityEconomy(townIdx, marker))
					return 0;

				if (marker != 1)
					break;

				CityScreenStep step = RunCityScreen(townIdx, marker);

				if (step == CityScreenStep.Leave)
				{
					// The end of the city screen
					MapView.Horiz = this.round.MapViewHoriz;
					MapView.Vert = this.round.MapViewVert;
					break;
				}

				fromStart = step == CityScreenStep.Restart;
			}

			if (marker == 0)
				EndCityTurn(townIdx);

			return FinishHandleCity(townIdx);
		}

		/// <summary>
		/// The start of a round of HandleCity: the place of the city (0xd8 / 0xe4), the title and the map on the city screen;
		/// in the turn the starvation and the growth (false: the city is gone); the units, the tiles it cannot work, the workers.
		/// </summary>
		private bool StartCityRound(short townIdx, short marker)
		{
			this.round.Horiz = (ushort)(short)this.Owner.World.Towns[townIdx].Place.X;
			this.round.Vert = (ushort)(short)this.Owner.World.Towns[townIdx].Place.Y;

			// The city screen: the title and the map of the city
			if (marker == 1)
				DrawCityHeaderAndMap(townIdx);

			// In the turn of the city: starvation and growth

			if (marker == 0 && !FeedCity(townIdx))
				return false;

			// The units the city supports
			CountCityUnits(townIdx);

			// The tiles the city cannot work
			MarkUnavailableTiles(townIdx);

			// The workers the city keeps and the new ones, the specialists
			PickWorkers(townIdx, marker, KeepWorkers(townIdx, marker));

			return true;
		}

		/// <summary>
		/// The economy of the city from its citizens: the factories and the power, in the turn the production (false: the
		/// city is gone), the trade, the luxuries, taxes and science, the content and happy citizens, the units it supports
		/// (counted again after a disbanded one) and in the turn their costs.
		/// </summary>
		private bool CountCityEconomy(short townIdx, short marker)
		{
			// The citizen icons, the factories and the power
			PrepareCitizensAndFactories(townIdx, marker);

			if (marker == 0 && !ProduceInTurn(townIdx))
				return false;

			// The city screen: the frames and the tabs
			if (marker == 1)
				DrawCityScreenFrames();

			// The corruption and the trade routes
			CountCorruptionAndTradeRoutes(townIdx);

			// The trade split into luxuries, taxes and science; the science buildings; the content citizens
			SplitTrade(townIdx);
			CountScienceAndContent(townIdx);

			// The luxuries (and the citizens on the city screen)
			ApplyLuxuries(townIdx, marker);

			// The improvements that make citizens content
			ApplyContentImprovements(townIdx, marker);

			// The military and the citizens
			ApplyMilitaryHappiness(townIdx, marker);

			// The wonders and the citizens
			ApplyWonderHappiness(townIdx, marker);

			// The units of the city: the support, disbanding (then all are counted again), the units on the city screen
			bool disbanded;

			do
			{
				StartUnitSupport(townIdx);
				disbanded = false;

				for (this.round.Index = 0; (short)this.round.Index < 0x80;
					this.round.Index = (ushort)(this.round.Index + 1))
				{
					if (SupportUnit(townIdx, marker))
					{
						disbanded = true;
						break;
					}
				}
			}
			while (disbanded);

			// In the turn: the costs of the support
			if (marker == 0)
				CountSupportCosts(townIdx);

			return true;
		}

		/// <summary>
		/// The production of the turn: a unit built (the human is told, a computer city sends its caravan and picks again), an
		/// improvement or wonder completed (a spaceship part, what the improvement does), the advisor of a human city; a
		/// computer city buys shields with gold. False: the city is gone.
		/// </summary>
		private bool ProduceInTurn(short townIdx)
		{
			// The food and the shields of the turn, a unit built
			switch (ProduceInCity(townIdx))
			{
				case ProductionStep.CityLost:
					return false;

				case ProductionStep.SettlersWait:
					break;

				case ProductionStep.Improvement:
					// An improvement or a wonder completed: a spaceship part, what some improvements do
					if (CompleteImprovement(townIdx))
					{
						BuildSpaceshipPart(townIdx);
						ApplyImprovement(townIdx);
					}

					AdviseHumanProduction(townIdx);
					break;

				case ProductionStep.NotEnough:
					AdviseHumanProduction(townIdx);
					break;

				default:
					FinishUnitOfCity(townIdx);
					AdviseHumanProduction(townIdx);
					break;
			}

			// A computer city buys shields with gold
			if (!this.Owner.World.IsHuman(CityTurn.Owner))
				BuyForComputerCity(townIdx);

			return true;
		}

		/// <summary>
		/// The city screen once: everything drawn, then (for the human) one input handled. Redraw: from the citizens again;
		/// Restart: the city from the start; Leave: the screen ends (a computer city's screen waits for a key or a click).
		/// </summary>
		private CityScreenStep RunCityScreen(short townIdx, short marker)
		{
			// The list of improvements
			DrawImprovementList(townIdx);
			FinishImprovementList(townIdx);

			// The production box, its shields, the food box
			DrawProductionBox(townIdx);
			DrawShieldsInBox(townIdx);
			DrawFoodBox(townIdx);

			// The resources box and the row of luxuries, taxes and science
			DrawResourceRows(townIdx);
			DrawTradeRow();

			// The shortfalls, the citizens, the trade routes, the pollution
			DrawShortfalls(townIdx);
			DrawCitizensAndRoutes(townIdx);
			DrawCityDotAndPollution(townIdx);

			if (CityTurn.Owner != (ushort)this.Owner.World.PersonParticipantIdx)
			{
				this.Owner.Conquest.WaitForKeyPressOrMouseClick();
				return CityScreenStep.Leave;
			}

			// The buttons, the input, the Change button
			DrawExitAndBuyButtons(townIdx);
			WaitForCityScreenInput();
			CityScreenStep step = ReadCityScreenInput(townIdx);

			if (step != CityScreenStep.Others)
				return step;

			// The other buttons: Buy, Change, the tabs, the view of the city
			step = HandleCityScreenButtons(townIdx);

			if (step != CityScreenStep.Others)
				return step;

			// A: the units in the city
			if (this.round.Key == 'a')
			{
				ActivateUnitsInCity();
				return CityScreenStep.Redraw;
			}

			// S: an improvement to sell, else the specialists, P (the cursor on the map), M, R and the clicks on the list
			if (!PickImprovementToSell(townIdx))
			{
				step = ToggleSpecialist(townIdx);

				if (step != CityScreenStep.Others)
					return step;

				if (this.round.Key == 'p')
					return MoveCityMapCursor(townIdx) ? ToggleCityTile(townIdx, marker) : CityScreenStep.Restart;

				step = HandleListAndRename(townIdx);

				if (step == CityScreenStep.Others)
					return HandleCityScreenClicks(townIdx, marker);

				if (step != CityScreenStep.Sell)
					return step;
			}

			if (SellImprovement(townIdx))
				return CityScreenStep.Restart;

			return HandleCityScreenClicks(townIdx, marker);
		}

		/// <summary>The clicks on the city screen: the citizens at the top, the units in the city, the map of the city</summary>
		private CityScreenStep HandleCityScreenClicks(short townIdx, short marker)
		{
			if ((short)this.Owner.GlobDb3c < 0xc8 && (short)this.Owner.GlobDb3e < 0x14)
				return ClickCitizen(townIdx);

			if ((short)this.Owner.GlobDb3c >= 0x64 && (short)this.Owner.GlobDb3c < 0x2580 && (short)this.Owner.GlobDb3e >= 0x74 && ClickUnitOfCity())
				return CityScreenStep.Redraw;

			if ((short)this.Owner.GlobDb3e < 0x18 || (short)this.Owner.GlobDb3e >= 0x68)
				return CityScreenStep.Leave;

			this.round.DrawHoriz = (ushort)((short)this.Owner.GlobDb3c / 16 - 10);
			this.round.DrawVert = (ushort)((short)(this.Owner.GlobDb3e - 0x18) / 16 - 2);

			return ToggleCityTile(townIdx, marker);
		}

		/// <summary>
		/// The end of the city's turn: civil disorder and the celebration (not for the barbarians), the science, the score and
		/// the pollution, for the human the upkeep of the improvements
		/// </summary>
		private void EndCityTurn(short townIdx)
		{
			if (CityTurn.Owner != 0)
			{
				// Civil disorder, order restored, the taxes; the celebration
				ApplyCivilOrder(townIdx);
				ApplyCelebration(townIdx);
			}

			ApplyResearch();

			// The score, the pollution, the nuclear plant; the upkeep of the improvements
			if (ApplyScoreAndPollution(townIdx))
				PayImprovementUpkeep(townIdx);
		}
	}
}
