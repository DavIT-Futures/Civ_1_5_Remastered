using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.UI
{
	public partial class WorldStatistics : GameComponent, IWorldStatistics
	{
		private const ushort ReplayHistoryScoresSegment = 0x3772;
		private const int ReplayHistoryStride = 8;
		private const ushort WonderInCityStringAddress = 0x440e;
		private const ushort WonderOfCityStringAddress = 0x4413;

		private static readonly GameText WondersOfTheWorldTitleAddress = new(0x43f0, "The Wonders of the World");

		/// <summary>The values of the players for the current line of the census (the original kept them under SP)</summary>
		private readonly short[] censusValues = new short[8];

		public WorldStatistics(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public void ShowWorldMap(ushort marker)
		{
			this.Processor.Journal.OpenScope($"DisplayWorldMap({marker})");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

			short person = this.Owner.World.PersonParticipantIdx;
			ushort seen = (ushort)(1 << (byte)person);
			ushort top = 0x3e7, bottom = 0;
			short shiftHoriz = (short)(0x28 - this.Owner.World.Participants[person].HorizBegin), shiftVert;

			if (MapRevealed != 0)
			{
				shiftHoriz = 0;
				shiftVert = 0;
			}
			else
			{
				for (int troopIdx = 0; troopIdx < 0x80; troopIdx++)
				{
					if (this.Troops.Pull(person, troopIdx, UnitMemory.Kind) != 0xff)
						FitWorldMapRows(this.Troops.Pull(person, troopIdx, UnitMemory.Vert), ref top, ref bottom);
				}

				for (int townIdx = 0; townIdx < 0x80; townIdx++)
				{
					if (this.Owner.World.Towns[townIdx].ParticipantIdx == person)
						FitWorldMapRows((byte)(sbyte)this.Owner.World.Towns[townIdx].Place.Y, ref top, ref bottom);
				}

				for (ushort horizLoc = 0; horizLoc < 0x50; horizLoc++)
				{
					if ((this.Owner.World.ChartSight[horizLoc, 0] & seen) != 0 || (this.Owner.World.ChartSight[horizLoc, 1] & seen) != 0)
					{
						top = 0;
						bottom = 0x31;
					}
				}

				shiftVert = (short)-((short)(bottom + top) / 2 - 0x19);
			}

			short column = 0, row;

			for (row = 0; row < 0x32; row++)
			{
				for (column = 0; column < 0x50; column++)
				{
					bool shown = (this.Owner.World.ChartSight[(ushort)column, (ushort)row] & seen) != 0 &&
						this.Owner.MapOps.CheckMapLimits(0, row + shiftVert) != 0;

					if (!shown && MapRevealed == 0)
						continue;

					DrawWorldMapTile(marker, column, row, shiftHoriz, shiftVert);
				}
			}

			if (WorldMapView != 1)
			{
				this.Owner.Conquest.WaitForKeyPressOrMouseClick();

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);
			}

			this.Owner.Startup.ShowMouseCursor();

			this.Processor.Journal.CloseScope("DisplayWorldMap");
		}

		/// <inheritdoc/>
		public void ShowGameFigures()
		{
			this.Processor.Journal.OpenScope("DisplayGameFigures()");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Red);

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				this.Owner.TextPainter.DrawTextWithShadow(CivilizationName(participantIdx), participantIdx * 40, 8,
					PlayerColour(participantIdx));

				this.Owner.Services.DrawPictureToScreen(ScreenBox, participantIdx * 40, 16,
					SpritePicture(((participantIdx << 5) + 0x40)));
			}

			for (int continentIdx = 1; continentIdx < 0xf; continentIdx++)
			{
				int vertLoc = (continentIdx << 3) + 0x18;

				EmptyMessage()
					.Number((short)this.Processor.PullGlobalWord((ushort)((continentIdx << 1) + 0xdcfe)));
				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 8, vertLoc, Hues.Black);

				for (int participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					PlayerContinent landmass = this.Owner.World.Participants[participantIdx].Landmasses[continentIdx];

					if (landmass.Assault == 0)
						continue;

					EmptyMessage()
						.Number(landmass.Guard)
						.Text("/")
						.Number(landmass.Assault)
						.Text("/")
						.Number(landmass.TownTally);

					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), participantIdx * 40, vertLoc,
						PlayerColour(participantIdx));
				}
			}

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			this.Processor.Journal.CloseScope("DisplayGameFigures");
		}

		/// <inheritdoc/>
		public void ShowCivilizationGraph()
		{
			this.Processor.Journal.OpenScope("DisplayCivilizationGraph()");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.DarkGray);

			ushort peak = 0x32;

			this.Owner.Dialogs.DrawBox(4, 9, 311, 183, Hues.Black);

			DrawingBox.TypefaceIdx = 2;

			for (short round = 0; round <= this.Owner.World.RoundTally && round <= 0x258; round += 0x32)
			{
				short horizLoc = (short)(round / 2);

				this.Owner.VideoCard.RenderLine(ScreenBox, horizLoc + 4, 9, horizLoc + 4, 192, Hues.Black);

				if (round % 100 != 0)
					continue;

				short year = (short)this.Owner.Replayer.ObtainReplayYear((ushort)round);

				EmptyMessage()
					.Number(Math.Abs(year))
					.Text(year < 0 ? (ushort)0x43d2 : (ushort)0x43d5);
				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), horizLoc, 194, Hues.White);
			}

			DrawingBox.TypefaceIdx = 1;

			short epochTally = (short)(this.Owner.World.RoundTally / 4);

			for (int participantIdx = 1; participantIdx <= 8; participantIdx++)
			{
				if (!InGraph(participantIdx))
					continue;

				this.Owner.TextPainter.DrawTextToScreen0(CivilizationName(participantIdx), 8, (participantIdx << 3) + 4,
					PlayerColour(participantIdx));

				for (int epochIdx = 0; epochIdx < epochTally; epochIdx++)
				{
					byte score = HistoryScore(epochIdx, participantIdx);

					if (peak < score)
					{
						peak = score;
					}
				}
			}

			this.Owner.TextPainter.DrawTextWithShadow(GameTexts.TextCivilizationPOWERGraph, 100, 2, Hues.White);

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (!InGraph(participantIdx))
					continue;

				short previousHoriz = 0;
				short previousVert = 0;

				for (int epochIdx = 0; epochIdx < epochTally && epochIdx < 0x96; epochIdx++)
				{
					short horizLoc = (short)(epochIdx << 1);
					short vertLoc = (short)((ushort)(0xb4 * HistoryScore(epochIdx, participantIdx)) / peak);

					this.Owner.VideoCard.RenderLine(ScreenBox, previousHoriz + 4, 192 - previousVert, horizLoc + 4, 192 - vertLoc,
						PlayerColour(participantIdx));

					previousHoriz = horizLoc;
					previousVert = vertLoc;
				}
			}

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			this.Processor.Journal.CloseScope("DisplayCivilizationGraph");
		}

		/// <inheritdoc/>
		public void ShowWondersOfTheWorld()
		{
			this.Processor.Journal.OpenScope("DisplayWondersOfTheWorld()");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			short vertLoc = 999;

			for (short wonderIdx = 1; wonderIdx <= 0x15; wonderIdx++)
			{
				short townIdx = this.Owner.World.MarvelTownIdx[wonderIdx];

				if (townIdx == -1)
					continue;

				// A new page
				if (vertLoc > 0xb4)
				{
					if (vertLoc != 999)
					{
						this.Owner.Conquest.WaitForKeyPressOrMouseClick();
					}

					this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Cyan);

					this.Owner.TextPainter.DrawTextWithShadow(WondersOfTheWorldTitleAddress, 100, 12, Hues.White);

					vertLoc = 0x20;
				}

				short participantIdx = WonderCityRules.ReadsCity(townIdx, this.Owner.TurnHooks.ClassicWonderCities) ?
					this.Owner.World.Towns[townIdx].ParticipantIdx : (short)0;

				this.Owner.Words.Clear();

				if (wonderIdx <= 7)
				{
					this.Owner.Words.Append("The ");
				}

				this.Owner.Words.Append(WonderText(wonderIdx));

				if (townIdx == 128)
				{
					this.Owner.Words.Append(" (Destroyed)");

					participantIdx = 0;
				}
				else
				{
					this.Owner.Words.Append(wonderIdx > 7 ? WonderOfCityStringAddress : WonderInCityStringAddress);
					this.Owner.Conquest.ObtainCityLabel(townIdx);
					this.Owner.Words.Append(". (");
					this.Owner.Words.Append(CivilizationName(participantIdx));
					this.Owner.Words.Append(")");
				}

				this.Owner.Dialogs.DrawBox(8, vertLoc, 303, 15, PlayerColour(participantIdx));

				this.Owner.CityOps.DrawResourceIcon((short)(wonderIdx + 0x18), 16, (short)(vertLoc + 3));

				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 270);

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 40, vertLoc + 5, Hues.White);

				vertLoc += 0x18;
			}

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			this.Processor.Journal.CloseScope("DisplayWondersOfTheWorld");
		}

		/// <inheritdoc/>
		public void ShowEpicChronicle()
		{
			this.Processor.Journal.OpenScope("DisplayEpicChronicle()");

			// The values of the players of the frame of the original (B3: in C#; the barbarians' one never set for the
			// measures 3 and 4)

			ushort[] values = new ushort[8];

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			ushort measure = (ushort)this.Owner.Libc.Dice.DrawInt(5);

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Cyan);

			Message(this.Processor.PullGlobalWord((ushort)((this.Owner.Libc.Dice.DrawInt(4) << 1) + 0x4516)))
				.Text(" completes his epic history:");

			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 0, 4, Hues.White);

			Message("'The ")
				.Text(this.Processor.PullGlobalWord((ushort)((measure << 1) + 0x452c)))
				.Text(" Civilizations in the World'");

			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 12, Hues.White);

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				values[participantIdx] = 0x0;
			}

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.Owner.World.Towns[townIdx];

				if (town.ParticipantIdx == -1 || town.ConditionMarker == 0xff)
					continue;

				this.Owner.CityOps.HandleCity(townIdx, -1);

				short place = town.ParticipantIdx;

				if (measure == 3)
				{
					values[place] = (ushort)(values[place] + town.TrueExtent + CityHappy - CityUnhappy);
				}

				if (measure == 4)
				{
					values[place] = (ushort)(values[place] + town.TrueExtent);
				}
			}

			for (short participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				short place = participantIdx;

				switch (measure)
				{
					case 0:
						values[place] = (ushort)this.Owner.World.Participants[participantIdx].Gold;
						break;

					case 1:
						values[place] = (ushort)this.Owner.World.Participants[participantIdx].MartialMight;
						break;

					case 2:
						values[place] = 0x0;

						for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
						{
							if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
							{
								values[place] = (ushort)(values[place] + 1);
							}
						}
						break;
				}
			}

			short vertLoc = 0x20;
			short rank = 0;
			short best = -1;
			short bestValue = 0;

			for (short placeIdx = 1; placeIdx < 8; placeIdx++)
			{
				if (((1 << (byte)placeIdx) & (ushort)this.Owner.World.LiveRealms) == 0)
					continue;

				bestValue = 0;
				best = -1;

				for (short participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					short value = (short)values[participantIdx];

					if (value >= bestValue && ((1 << (byte)participantIdx) & (ushort)this.Owner.World.LiveRealms) != 0)
					{
						bestValue = value;
						best = participantIdx;
					}
				}

				if (best == -1)
					continue;

				EmptyMessage()
					.Number((short)(rank + 1))
					.Text(". The ")
					.Text(this.Processor.PullGlobalWord((ushort)((rank << 1) + 0x451e)))
					.Text(" Civilization of the ")
					.Text(CivilizationPlural(best))
					.Text(".");

				short humanIdx = this.Owner.World.PersonParticipantIdx;

				if ((this.Owner.World.Participants[humanIdx].Relations[best] & 0x40) != 0 || best == humanIdx || this.Owner.World.HardnessTier == 0)
				{
					this.Owner.TextPainter.WaitForTime(60);

					this.Owner.TurnHooks.DrawPalace(best, this.Owner.TurnHooks.ObtainPalaceLevel(this.Owner.World.Participants[best].Tally), 128, (short)(vertLoc - 13));

					this.Owner.Dialogs.DrawBox(8, vertLoc, 303, 15, PlayerColour(best));

					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 40, vertLoc + 5, Hues.White);

					vertLoc += 0x18;
				}

				this.Owner.World.Participants[best].RunningSagaStanding += (short)(7 - rank);
				rank++;
				values[best] = 0xffff;
			}

			this.Owner.World.Participants[0].RunningSagaStanding++;

			this.Owner.TurnLogic.WaitForKey();

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			this.Processor.Journal.CloseScope("DisplayEpicChronicle");
		}

		/// <inheritdoc/>
		public void ShowCensus(short participantIdx)
		{
			this.Processor.Journal.OpenScope($"DisplayCensus({participantIdx})");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Blue);

			Message(CivilizationName(participantIdx))
				.Text(" Demographics");

			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 4, Hues.White);

			CensusTally tally = GatherCensus();

			DrawCensus(participantIdx, tally);

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			this.Processor.Journal.CloseScope("DisplayCensus");
		}

		/// <inheritdoc/>
		public void DrawCensusLine(ushort valuesAddr, short vertLoc, ushort charsAddr)
		{
			short[] values = new short[8];

			for (int participantIdx = 0; participantIdx < 8; participantIdx++)
				values[participantIdx] = (short)this.Processor.PullGlobalWord((ushort)(valuesAddr + (participantIdx << 1)));

			DrawCensusLine(values, valuesAddr, vertLoc, charsAddr);

			// (the values made positive for a line where lower is better, back in the memory)
			for (int participantIdx = 0; participantIdx < 8; participantIdx++)
				this.Processor.EmitGlobalWord((ushort)(valuesAddr + (participantIdx << 1)), (ushort)values[participantIdx]);
		}

		/// <inheritdoc/>
		public void DrawCensusLine(short[] values, short vertLoc, ushort charsAddr) =>
			DrawCensusLine(values, TextBuffer.StackTextAddress, vertLoc, charsAddr);

		/// <summary>The rows of the world map widen to the row (unsigned) of a unit or a city of the player</summary>
		private static void FitWorldMapRows(byte vertLoc, ref ushort top, ref ushort bottom)
		{
			if (top > vertLoc)
				top = vertLoc;

			if (bottom < vertLoc)
				bottom = vertLoc;
		}

		/// <summary>One tile of the world map (see ShowWorldMap)</summary>
		private void DrawWorldMapTile(ushort marker, short horizLoc, short vertLoc, short shiftHoriz, short shiftVert)
		{
			short pixelHoriz = (short)(this.Owner.Routing.TunePosition(horizLoc + shiftHoriz) << 2);
			short pixelVert = (short)((vertLoc + shiftVert) << 2);

			ushort terrain = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			if (marker == 0)
			{
				this.Owner.Services.FloodBox(ScreenBox, pixelHoriz, pixelVert, 4, 4, terrain == 0xa ? (ushort)1 : (ushort)2);
			}
			else
			{
				this.Owner.VideoCard.RenderImage(PictureScreenBox, (ushort)((terrain << 2) + 0xa0), (ushort)((((horizLoc + vertLoc) & 1) << 2) + 0x6f),
					4, 4, ScreenBox, (ushort)pixelHoriz, (ushort)pixelVert);
			}

			if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				ushort townIdx = this.Owner.Helpers.FindCityAtPosition(horizLoc, vertLoc);

				if (this.Owner.World.Towns[townIdx].ShownExtent != 0 || MapRevealed != 0 ||
					this.Owner.World.Towns[townIdx].ParticipantIdx == this.Owner.World.PersonParticipantIdx)
				{
					ushort colour = PlayerColour(this.Owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc));

					this.Owner.Services.FloodBox(ScreenBox, pixelHoriz, pixelVert, 4, 4, colour);
				}

				return;
			}

			ushort ownerIdx = this.Owner.MapOps.ObtainTileOwner(horizLoc, vertLoc);

			if (ownerIdx == 0xffff)
				return;

			if (ownerIdx != (ushort)this.Owner.World.PersonParticipantIdx && MapRevealed == 0)
			{
				ushort troopIdx = this.Owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);
				sbyte visibility = (sbyte)this.Troops.PullAt(((ushort)(0xc * troopIdx) + (ushort)(0x600 * ownerIdx)), UnitMemory.ShownToParticipants);

				if ((visibility & (1 << (byte)this.Owner.World.PersonParticipantIdx)) == 0)
					return;
			}

			ushort ownerColour = PlayerColour(ownerIdx);

			this.Owner.Services.FloodBox(ScreenBox, pixelHoriz + 1, pixelVert + 1, 3, 3, Hues.Black);

			this.Owner.Services.FloodBox(ScreenBox, pixelHoriz, pixelVert, 3, 3, ownerColour);
		}

		/// <summary>The player is shown in the graph: alive, or one of the players the game started with</summary>
		private bool InGraph(int participantIdx)
		{
			return (this.Owner.World.LiveRealms & (1 << participantIdx)) != 0 || (short)participantIdx <= (short)(this.Owner.World.BotRivalTally + 1);
		}

		/// <summary>The score of the player in the replay history (0x3772, 8 bytes every 4 turns)</summary>
		private byte HistoryScore(int epochIdx, int participantIdx)
		{
			return this.Processor.PullUInt8(ReplayHistoryScoresSegment, (ushort)((epochIdx * ReplayHistoryStride) + participantIdx));
		}

		/// <summary>
		/// The land tiles of each player (rows 2 - 47) and the statistics of every city (HandleCity with -1 fills the city
		/// figures 0x70da - 0x70e4)
		/// </summary>
		private CensusTally GatherCensus()
		{
			CensusTally tally = new CensusTally();

			for (short horizLoc = 0; horizLoc < 0x50; horizLoc++)
			{
				for (short vertLoc = 2; vertLoc < 0x30; vertLoc++)
				{
					if (this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain)
						continue;

					tally.Land[this.Owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc)]++;
				}
			}

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.Owner.World.Towns[townIdx];

				if (town.ParticipantIdx == -1 || town.ConditionMarker == 0xff)
					continue;

				this.Owner.CityOps.HandleCity(townIdx, -1);

				int owner = town.ParticipantIdx;
				short size = (sbyte)town.TrueExtent;

				tally.Size[owner] += size;
				tally.Approval[owner] += (short)(size + CityHappy - CityUnhappy);
				tally.Money[owner] += (short)((CityLuxuries << 1) + CityTaxes);
				tally.Unrest[owner] += (short)(CityFood - (size << 1));
				tally.Output[owner] += (short)(CityFood + CityShields + CityTrade);

				short shields = (short)CityShields;

				tally.Shields[owner] += shields;

				short pollution = (short)(CPU.SignedQuotient(shields, (short)PollutionFactor) - 0x14);
				pollution += (short)((short)(size * ContentAdvances) / 4);

				if (pollution > 0)
				{
					tally.Pollution[owner] += pollution;
				}

				if (town.Has(CityStructure.Granary))
				{
					tally.Health[owner] += size;
				}

				if (town.Has(CityStructure.Aqueduct))
				{
					tally.Health[owner] += size;
				}

				if (town.Has(CityStructure.Library))
				{
					tally.Learning[owner] += size;
				}

				if (town.Has(CityStructure.University))
				{
					tally.Learning[owner] += size;
				}
			}

			return tally;
		}

		private short CensusValue(int participantIdx)
		{
			return this.censusValues[participantIdx];
		}

		private void SetCensusValue(int participantIdx, short value)
		{
			this.censusValues[participantIdx] = value;
		}

		/// <summary>
		/// One line: the title, the values of the players 1 - 7 (the player's one written at 104), the rank (a negative row:
		/// a lower value is better)
		/// </summary>
		private void DrawCensusRow(GameText title, ref short vertLoc, bool lowerIsBetter, ushort unit, Action<short> valueOfPlayer)
		{
			this.Owner.TextPainter.DrawTextWithShadow(title, 8, vertLoc, Hues.White);

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				valueOfPlayer(participantIdx);
			}

			DrawCensusLine(this.censusValues, (short)(lowerIsBetter ? -vertLoc : vertLoc), unit);

			vertLoc += 0xc;
		}

		/// <summary>The value with the unit text, written at (104, row) for the player itself</summary>
		private void DrawCensusFigure(short value, string unit, short vertLoc)
		{
			EmptyMessage()
				.Number(value);

			if (unit.Length > 0)
			{
				this.Owner.Words.Append(unit);
			}

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 104, vertLoc, Hues.White);
		}

		/// <summary>The 13 lines of the demographics</summary>
		private void DrawCensus(short shownIdx, CensusTally tally)
		{
			short vertLoc = 0x18;
			short row = vertLoc;

			// Approval rating
			DrawCensusRow(GameTexts.TextApprovalRating, ref vertLoc, false, 0x4557, participantIdx =>
			{
				SetCensusValue(participantIdx, CensusRules.ApprovalRating(
					tally.Approval[participantIdx], tally.Size[participantIdx], this.Owner.TurnHooks.ClassicCensus));

				if (participantIdx == shownIdx)
				{
					DrawCensusFigure(CensusValue(participantIdx), "%", row);
				}
			});

			// Population (the text of ComputePlayerPopulation)
			row = vertLoc;

			DrawCensusRow(GameTexts.TextPopulation, ref vertLoc, false, 0x4565, participantIdx =>
			{
				this.Owner.Words.Clear();

				SetCensusValue(participantIdx, (short)this.Owner.Helpers.ComputePlayerPopulation(participantIdx));

				if (participantIdx == shownIdx)
				{
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 104, row, Hues.White);
				}
			});

			// GNP (the text is made for every player)
			row = vertLoc;

			DrawCensusRow(GameTexts.TextGnp, ref vertLoc, false, 0x457a, participantIdx =>
			{
				this.Owner.Words.Clear();

				SetCensusValue(participantIdx, tally.Money[participantIdx]);

				EmptyMessage()
					.Number(CensusValue(participantIdx))
					.Text(" million $");

				if (participantIdx == shownIdx)
				{
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 104, row, Hues.White);
				}
			});

			// Manufacturing goods
			row = vertLoc;

			DrawCensusRow(GameTexts.TextMfgGoods, ref vertLoc, false, 0x4590, participantIdx =>
			{
				SetCensusValue(participantIdx, tally.Shields[participantIdx]);

				if (participantIdx == shownIdx)
				{
					DrawCensusFigure(CensusValue(participantIdx), " Mtons", row);
				}
			});

			// Land area (the text is made for every player)
			row = vertLoc;

			DrawCensusRow(GameTexts.TextLandArea, ref vertLoc, false, 0x45b0, participantIdx =>
			{
				this.Owner.Words.Clear();

				SetCensusValue(participantIdx, tally.Land[participantIdx]);

				EmptyMessage()
					.Number(CensusValue(participantIdx))
					.Text(",000 sq.miles");

				if (participantIdx == shownIdx)
				{
					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 104, row, Hues.White);
				}
			});

			// Literacy: (2 * size + learning) * 2 / size, doubled for each of the technologies 0, 0x16, 0x1e, 0xd, at most 100
			row = vertLoc;

			DrawCensusRow(GameTexts.TextLiteracy, ref vertLoc, false, 0x45c1, participantIdx =>
			{
				SetCensusValue(participantIdx, CPU.SignedQuotient(
					(short)(((tally.Size[participantIdx] << 1) + tally.Learning[participantIdx]) << 1), tally.Size[participantIdx]));

				foreach (short scienceIdx in new short[] { 0, 0x16, 0x1e, 0xd })
				{
					if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
					{
						SetCensusValue(participantIdx, (short)(CensusValue(participantIdx) << 1));
					}
				}

				SetCensusValue(participantIdx, (short)this.Owner.Helpers.ClampValue(CensusValue(participantIdx), 0, 100));

				if (participantIdx == shownIdx)
				{
					DrawCensusFigure(CensusValue(participantIdx), "%", row);
				}
			});

			// Disease: 50 * size / (size + health), halved with technology 10; the life expectancy from it (1500 / (disease + 20))
			row = vertLoc;

			DrawCensusRow(GameTexts.TextDisease, ref vertLoc, true, 0x45ce, participantIdx =>
			{
				SetCensusValue(participantIdx, CensusRules.DiseaseRate(
					tally.Size[participantIdx], tally.Health[participantIdx], this.Owner.TurnHooks.ClassicCensus));

				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 10) != 0)
				{
					SetCensusValue(participantIdx, CPU.SignedQuotient(CensusValue(participantIdx), 2));
				}

				tally.Health[participantIdx] = CPU.SignedQuotient(0x5dc, (short)(CensusValue(participantIdx) + 0x14));

				if (participantIdx == shownIdx)
				{
					DrawCensusFigure(CensusValue(participantIdx), "%", row);
				}
			});

			// Pollution, which shortens the life expectancy
			row = vertLoc;

			DrawCensusRow(GameTexts.TextPollution, ref vertLoc, true, 0x45e7, participantIdx =>
			{
				SetCensusValue(participantIdx, tally.Pollution[participantIdx]);

				tally.Health[participantIdx] -= CPU.SignedQuotient((short)(0xa * tally.Pollution[participantIdx]), tally.Size[participantIdx]);

				if (participantIdx == shownIdx)
				{
					DrawCensusFigure(CensusValue(participantIdx), "0 tons/year", row);
				}
			});

			// Life expectancy (20 - 99)
			row = vertLoc;

			DrawCensusRow(GameTexts.TextLifeExpectancy, ref vertLoc, false, 0x4606, participantIdx =>
			{
				SetCensusValue(participantIdx, (short)this.Owner.Helpers.ClampValue(tally.Health[participantIdx], 20, 99));

				if (participantIdx == shownIdx)
				{
					DrawCensusFigure(CensusValue(participantIdx), " years", row);
				}
			});

			// Family size: 40 * unrest / size + 20 tenths (the translated code writes the tenths as value / 10 twice, TenthsRules)
			row = vertLoc;

			DrawCensusRow(GameTexts.TextFamilySize, ref vertLoc, false, 0x4624, participantIdx =>
			{
				SetCensusValue(participantIdx, CensusRules.FamilySize(
					tally.Unrest[participantIdx], tally.Size[participantIdx], this.Owner.TurnHooks.ClassicCensus));

				if (participantIdx == shownIdx)
				{
					EmptyMessage()
						.Number(TenthsRules.Whole(CensusValue(participantIdx)))
						.Text(".")
						.Number(TenthsRules.Tenth(CensusValue(participantIdx), this.Owner.TurnHooks.ClassicTenths))
						.Text(" children");

					this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 104, row, Hues.White);
				}

				SetCensusValue(participantIdx, CPU.SignedQuotient(CensusValue(participantIdx), 0xa));
			});

			// Military service: 10 * units / size
			row = vertLoc;

			DrawCensusRow(GameTexts.TextMilitaryService, ref vertLoc, false, 0x463e, participantIdx =>
			{
				SetCensusValue(participantIdx, CPU.SignedQuotient((short)(0xa * this.Owner.World.Participants[participantIdx].TroopTally),
					tally.Size[participantIdx]));

				if (participantIdx == shownIdx)
				{
					DrawCensusFigure(CensusValue(participantIdx), " years", row);
				}
			});

			// Annual income: money * technologies / size (divided first when the product would overflow)
			row = vertLoc;

			DrawCensusRow(GameTexts.TextAnnualIncome, ref vertLoc, false, 0x465f, participantIdx =>
			{
				short sciences = this.Owner.World.Participants[participantIdx].FoundScienceTally;
				short money = tally.Money[participantIdx];

				SetCensusValue(participantIdx, CPU.SignedQuotient(0x7530, (short)(sciences + 1)) > money
					? CPU.SignedQuotient((short)(money * sciences), tally.Size[participantIdx])
					: (short)(CPU.SignedQuotient(money, tally.Size[participantIdx]) * sciences));

				if (participantIdx == shownIdx)
				{
					DrawCensusFigure(CensusValue(participantIdx), "$ per capita", row);
				}
			});

			// Productivity: 10 * output / size
			row = vertLoc;

			DrawCensusRow(GameTexts.TextProductivity, ref vertLoc, false, 0x466f, participantIdx =>
			{
				SetCensusValue(participantIdx, CensusRules.Productivity(
					tally.Output[participantIdx], tally.Size[participantIdx], this.Owner.TurnHooks.ClassicCensus));

				if (participantIdx == shownIdx)
				{
					DrawCensusFigure(CensusValue(participantIdx), "", row);
				}
			});
		}

		private void DrawCensusLine(short[] values, ushort valuesAddr, short vertLoc, ushort charsAddr)
		{
			this.Processor.Journal.OpenScope($"RenderCensusLine({valuesAddr}, {vertLoc}, 0x{charsAddr:x4})");

			short humanIdx = this.Owner.World.PersonParticipantIdx;
			short rank = 1;
			short best = -9999;
			short bestIdx = humanIdx;

			short Amount(int participantIdx) => values[participantIdx];

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (((1 << participantIdx) & this.Owner.World.LiveRealms) == 0)
					continue;

				if (vertLoc < 0)
				{
					values[participantIdx] = (short)-Amount(participantIdx);
				}

				if (Amount(participantIdx) > Amount(humanIdx))
				{
					rank++;
				}

				if (best < Amount(participantIdx))
				{
					best = Amount(participantIdx);
					bestIdx = participantIdx;
				}
			}

			EmptyMessage()
				.Number(rank);

			// "st", "nd", "rd", "th"
			this.Owner.Words.Append(rank switch { 1 => (ushort)0x4670, 2 => (ushort)0x4673, 3 => (ushort)0x4676, _ => (ushort)0x4679 });

			this.Owner.VideoCard.RenderLine(ScreenBox, 4, Math.Abs(vertLoc) - 3, 316, Math.Abs(vertLoc) - 3, Hues.LightBlue);

			this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), 192, Math.Abs(vertLoc), Hues.White);

			if (bestIdx != humanIdx && (this.Owner.World.Participants[humanIdx].Relations[bestIdx] & 0x40) != 0)
			{
				Message("(")
					.Text(CivilizationPlural(bestIdx))
					.Text(": ")
					.Number(Math.Abs(best))
					.Text(charsAddr)
					.Text(")");

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), -(this.Owner.TextPainter.MeasureTextWidth(this.Owner.Words.ToString()) - 318), Math.Abs(vertLoc),
					PlayerColour(bestIdx));
			}

			this.Processor.Journal.CloseScope("RenderCensusLine");
		}
	}
}
