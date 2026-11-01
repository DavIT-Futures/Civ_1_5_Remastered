using System;

namespace Civ_1_5_Remastered.Core.UI.Reports
{
	public partial class WorldStatistics : GameComponent, IWorldStatistics
	{
		private const ushort ReplayHistoryScoresSegment = 0x3772;
		private const int ReplayHistoryStride = 8;
		private const ushort WonderInCityStringAddress = 0x440e;
		private const ushort WonderOfCityStringAddress = 0x4413;

		private static readonly GameText WondersOfTheWorldTitleAddress = new(0x43f0, "The Wonders of the World");

		private readonly short[] censusValues = new short[8];

		public WorldStatistics(IGameEngine owner)
			: base(owner)
		{
		}

		public void ShowWorldMap(ushort marker)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayWorldMap({marker})");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			short person = this.Owner.World.PersonParticipantIndex;
			ushort seen = (ushort)(1 << (byte)person);
			ushort top = 0x3e7, bottom = 0;
			short shiftHorizontal = (short)(0x28 - this.Owner.World.Participants[person].HorizontalBegin), shiftVertical;

			if (MapView.Revealed != 0)
			{
				shiftHorizontal = 0;
				shiftVertical = 0;
			}
			else
			{
				for (int troopIndex = 0; troopIndex < 0x80; troopIndex++)
				{
					if (this.Troops.Pull(person, troopIndex, UnitMemory.Kind) != 0xff)
					{
						FitWorldMapRows(this.Troops.Pull(person, troopIndex, UnitMemory.Vertical), ref top, ref bottom);
					}
				}

				for (int townIndex = 0; townIndex < 0x80; townIndex++)
				{
					if (this.Owner.World.Towns[townIndex].ParticipantIndex == person)
					{
						FitWorldMapRows((byte)(sbyte)this.Owner.World.Towns[townIndex].Place.Y, ref top, ref bottom);
					}
				}

				for (ushort horizontalLocation = 0; horizontalLocation < 0x50; horizontalLocation++)
				{
					if ((this.Owner.World.ChartSight[horizontalLocation, 0] & seen) != 0 || (this.Owner.World.ChartSight[horizontalLocation, 1] & seen) != 0)
					{
						top = 0;
						bottom = 0x31;
					}
				}

				shiftVertical = (short)-(((short)(bottom + top) / 2) - 0x19);
			}

			short column = 0, row;

			for (row = 0; row < 0x32; row++)
			{
				for (column = 0; column < 0x50; column++)
				{
					bool shown = (this.Owner.World.ChartSight[(ushort)column, (ushort)row] & seen) != 0 &&
						this.Owner.MapOperations.CheckMapLimits(0, row + shiftVertical) != 0;

					if (!shown && MapView.Revealed == 0)
					{
						continue;
					}

					DrawWorldMapTile(marker, column, row, shiftHorizontal, shiftVertical);
				}
			}

			if (MapView.WorldMapView != 1)
			{
				this.Owner.Conquest.WaitForKeyPressOrMouseClick();

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);
			}

			this.Owner.Startup.ShowMouseCursor();
		}

		public void ShowGameFigures()
		{
			using var _ = this.Processor.Journal.Scope("DisplayGameFigures()");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Red);

			for (int participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				this.Owner.TextPainter.DrawTextWithShadow(Tables.CivilizationName(participantIndex), participantIndex * 40, 8,
					Tables.PlayerColour(participantIndex));

				this.Owner.Services.DrawPictureToScreen(Screen.Box, participantIndex * 40, 16,
					Tables.SpritePicture((participantIndex << 5) + 0x40));
			}

			for (int continentIndex = 1; continentIndex < 0xf; continentIndex++)
			{
				int verticalLocation = (continentIndex << 3) + 0x18;

				EmptyMessage()
					.Number((short)this.Processor.PullGlobalWord((ushort)((continentIndex << 1) + 0xdcfe)));
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 8, verticalLocation, Hues.Black);

				for (int participantIndex = 1; participantIndex < 8; participantIndex++)
				{
					PlayerContinent landmass = this.Owner.World.Participants[participantIndex].Landmasses[continentIndex];

					if (landmass.Assault == 0)
					{
						continue;
					}

					EmptyMessage()
						.Number(landmass.Guard)
						.Text("/")
						.Number(landmass.Assault)
						.Text("/")
						.Number(landmass.TownTally);

					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), participantIndex * 40, verticalLocation,
						Tables.PlayerColour(participantIndex));
				}
			}

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		public void ShowCivilizationGraph()
		{
			using var _ = this.Processor.Journal.Scope("DisplayCivilizationGraph()");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.DarkGray);

			ushort peak = 0x32;

			this.Owner.Dialogs.DrawBox(4, 9, 311, 183, Hues.Black);

			DrawingBox.TypefaceIndex = 2;

			for (short round = 0; round <= this.Owner.World.RoundTally && round <= 0x258; round += 0x32)
			{
				short horizontalLocation = (short)(round / 2);

				this.Owner.VideoCard.RenderLine(Screen.Box, horizontalLocation + 4, 9, horizontalLocation + 4, 192, Hues.Black);

				if (round % 100 != 0)
				{
					continue;
				}

				short year = (short)this.Owner.Replayer.ObtainReplayYear((ushort)round);

				EmptyMessage()
					.Number(Math.Abs(year))
					.Text(year < 0 ? (ushort)0x43d2 : (ushort)0x43d5);
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), horizontalLocation, 194, Hues.White);
			}

			DrawingBox.TypefaceIndex = 1;

			short epochTally = (short)(this.Owner.World.RoundTally / 4);

			for (int participantIndex = 1; participantIndex <= 8; participantIndex++)
			{
				if (!InGraph(participantIndex))
				{
					continue;
				}

				this.Owner.TextPainter.DrawTextToPrimaryScreen(Tables.CivilizationName(participantIndex), 8, (participantIndex << 3) + 4,
					Tables.PlayerColour(participantIndex));

				for (int epochIndex = 0; epochIndex < epochTally; epochIndex++)
				{
					byte score = HistoryScore(epochIndex, participantIndex);

					if (peak < score)
					{
						peak = score;
					}
				}
			}

			this.Owner.TextPainter.DrawTextWithShadow(GameTexts.TextCivilizationPowerGraph, 100, 2, Hues.White);

			for (int participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				if (!InGraph(participantIndex))
				{
					continue;
				}

				short previousHorizontal = 0;
				short previousVertical = 0;

				for (int epochIndex = 0; epochIndex < epochTally && epochIndex < 0x96; epochIndex++)
				{
					short horizontalLocation = (short)(epochIndex << 1);
					short verticalLocation = (short)((ushort)(0xb4 * HistoryScore(epochIndex, participantIndex)) / peak);

					this.Owner.VideoCard.RenderLine(Screen.Box, previousHorizontal + 4, 192 - previousVertical, horizontalLocation + 4, 192 - verticalLocation,
						Tables.PlayerColour(participantIndex));

					previousHorizontal = horizontalLocation;
					previousVertical = verticalLocation;
				}
			}

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		public void ShowWondersOfTheWorld()
		{
			using var _ = this.Processor.Journal.Scope("DisplayWondersOfTheWorld()");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

			short verticalLocation = 999;

			for (short wonderIndex = 1; wonderIndex <= 0x15; wonderIndex++)
			{
				short townIndex = this.Owner.World.MarvelTownIndex[wonderIndex];

				if (townIndex == -1)
				{
					continue;
				}

				if (verticalLocation > 0xb4)
				{
					if (verticalLocation != 999)
					{
						this.Owner.Conquest.WaitForKeyPressOrMouseClick();
					}

					this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Cyan);

					this.Owner.TextPainter.DrawTextWithShadow(WondersOfTheWorldTitleAddress, 100, 12, Hues.White);

					verticalLocation = 0x20;
				}

				short participantIndex = WonderCityRules.ReadsCity(townIndex) ? this.Owner.World.Towns[townIndex].ParticipantIndex : (short)0;

				this.Owner.Words.Clear();

				if (wonderIndex <= 7)
				{
					this.Owner.Words.Append("The ");
				}

				this.Owner.Words.Append(GameTables.WonderText(wonderIndex));

				if (townIndex == 128)
				{
					this.Owner.Words.Append(" (Destroyed)");

					participantIndex = 0;
				}
				else
				{
					this.Owner.Words.Append(wonderIndex > 7 ? WonderOfCityStringAddress : WonderInCityStringAddress);
					this.Owner.Conquest.ObtainCityLabel(townIndex);
					this.Owner.Words.Append(". (");
					this.Owner.Words.Append(Tables.CivilizationName(participantIndex));
					this.Owner.Words.Append(")");
				}

				this.Owner.Dialogs.DrawBox(8, verticalLocation, 303, 15, Tables.PlayerColour(participantIndex));

				this.Owner.CityOperations.DrawResourceIcon((short)(wonderIndex + 0x18), 16, (short)(verticalLocation + 3));

				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 270);

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 40, verticalLocation + 5, Hues.White);

				verticalLocation += 0x18;
			}

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		public void ShowEpicChronicle()
		{
			using var _ = this.Processor.Journal.Scope("DisplayEpicChronicle()");

			ushort[] values = new ushort[8];

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

			ushort measure = (ushort)this.Owner.Runtime.Dice.DrawInteger(5);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Cyan);

			Message(this.Processor.PullGlobalWord((ushort)((this.Owner.Runtime.Dice.DrawInteger(4) << 1) + 0x4516)))
				.Text(" completes his epic history:");

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 0, 4, Hues.White);

			Message("'The ")
				.Text(this.Processor.PullGlobalWord((ushort)((measure << 1) + 0x452c)))
				.Text(" Civilizations in the World'");

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 12, Hues.White);

			for (int participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				values[participantIndex] = 0x0;
			}

			for (short townIndex = 0; townIndex < 0x80; townIndex++)
			{
				City town = this.Owner.World.Towns[townIndex];

				if (town.ParticipantIndex == -1 || town.ConditionMarker == 0xff)
				{
					continue;
				}

				this.Owner.CityOperations.HandleCity(townIndex, -1);

				short place = town.ParticipantIndex;

				if (measure == 3)
				{
					values[place] = (ushort)(values[place] + town.TrueExtent + CityTurn.Happy - CityTurn.Unhappy);
				}

				if (measure == 4)
				{
					values[place] = (ushort)(values[place] + town.TrueExtent);
				}
			}

			for (short participantIndex = 0; participantIndex < 8; participantIndex++)
			{
				short place = participantIndex;

				switch (measure)
				{
					case 0:
						values[place] = (ushort)this.Owner.World.Participants[participantIndex].Gold;
						break;

					case 1:
						values[place] = (ushort)this.Owner.World.Participants[participantIndex].MartialMight;
						break;

					case 2:
						values[place] = 0x0;

						for (short scienceIndex = 0; scienceIndex < 0x48; scienceIndex++)
						{
							if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, scienceIndex) != 0)
							{
								values[place] = (ushort)(values[place] + 1);
							}
						}

						break;
				}
			}

			short verticalLocation = 0x20;
			short rank = 0;
			short best = -1;
			short bestValue = 0;

			for (short placeIndex = 1; placeIndex < 8; placeIndex++)
			{
				if (((1 << (byte)placeIndex) & (ushort)this.Owner.World.LiveRealms) == 0)
				{
					continue;
				}

				bestValue = 0;
				best = -1;

				for (short participantIndex = 1; participantIndex < 8; participantIndex++)
				{
					short value = (short)values[participantIndex];

					if (value >= bestValue && ((1 << (byte)participantIndex) & (ushort)this.Owner.World.LiveRealms) != 0)
					{
						bestValue = value;
						best = participantIndex;
					}
				}

				if (best == -1)
				{
					continue;
				}

				EmptyMessage()
					.Number((short)(rank + 1))
					.Text(". The ")
					.Text(this.Processor.PullGlobalWord((ushort)((rank << 1) + 0x451e)))
					.Text(" Civilization of the ")
					.Text(Tables.CivilizationPlural(best))
					.Text(".");

				short humanIndex = this.Owner.World.PersonParticipantIndex;

				if ((this.Owner.World.Participants[humanIndex].Relations[best] & 0x40) != 0 || best == humanIndex || this.Owner.World.HardnessTier == 0)
				{
					this.Owner.TextPainter.WaitForTime(60);

					this.Owner.TurnHooks.DrawPalace(best, this.Owner.TurnHooks.ObtainPalaceLevel(this.Owner.World.Participants[best].Tally), 128, (short)(verticalLocation - 13));

					this.Owner.Dialogs.DrawBox(8, verticalLocation, 303, 15, Tables.PlayerColour(best));

					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 40, verticalLocation + 5, Hues.White);

					verticalLocation += 0x18;
				}

				this.Owner.World.Participants[best].RunningSagaStanding += (short)(7 - rank);
				rank++;
				values[best] = 0xffff;
			}

			this.Owner.World.Participants[0].RunningSagaStanding++;

			this.Owner.TurnLogic.WaitForKey();

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		public void ShowCensus(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayCensus({participantIndex})");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Blue);

			Message(Tables.CivilizationName(participantIndex))
				.Text(" Demographics");

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 4, Hues.White);

			CensusTally tally = GatherCensus();

			DrawCensus(participantIndex, tally);

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		public void DrawCensusLine(ushort valuesAddress, short verticalLocation, ushort charactersAddress)
		{
			short[] values = new short[8];

			for (int participantIndex = 0; participantIndex < 8; participantIndex++)
			{
				values[participantIndex] = (short)this.Processor.PullGlobalWord((ushort)(valuesAddress + (participantIndex << 1)));
			}

			DrawCensusLine(values, valuesAddress, verticalLocation, charactersAddress);

			for (int participantIndex = 0; participantIndex < 8; participantIndex++)
			{
				this.Processor.EmitGlobalWord((ushort)(valuesAddress + (participantIndex << 1)), (ushort)values[participantIndex]);
			}
		}

		public void DrawCensusLine(short[] values, short verticalLocation, ushort charactersAddress) =>
			DrawCensusLine(values, TextBuffer.StackTextAddress, verticalLocation, charactersAddress);

		private static void FitWorldMapRows(byte verticalLocation, ref ushort top, ref ushort bottom)
		{
			if (top > verticalLocation)
			{
				top = verticalLocation;
			}

			if (bottom < verticalLocation)
			{
				bottom = verticalLocation;
			}
		}

		private void DrawWorldMapTile(ushort marker, short horizontalLocation, short verticalLocation, short shiftHorizontal, short shiftVertical)
		{
			short pixelHorizontal = (short)(this.Owner.Routing.TunePosition(horizontalLocation + shiftHorizontal) << 2);
			short pixelVertical = (short)((verticalLocation + shiftVertical) << 2);

			ushort terrain = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation);

			if (marker == 0)
			{
				this.Owner.Services.FloodBox(Screen.Box, pixelHorizontal, pixelVertical, 4, 4, terrain == 0xa ? (ushort)1 : (ushort)2);
			}
			else
			{
				this.Owner.VideoCard.RenderImage(Screen.PictureScreenBox, (ushort)((terrain << 2) + 0xa0), (ushort)((((horizontalLocation + verticalLocation) & 1) << 2) + 0x6f),
					4, 4, Screen.Box, (ushort)pixelHorizontal, (ushort)pixelVertical);
			}

			if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) != 0)
			{
				ushort townIndex = this.Owner.Helpers.FindCityAtPosition(horizontalLocation, verticalLocation);

				if (this.Owner.World.Towns[townIndex].ShownExtent != 0 || MapView.Revealed != 0 ||
					this.Owner.World.Towns[townIndex].ParticipantIndex == this.Owner.World.PersonParticipantIndex)
				{
					ushort colour = Tables.PlayerColour(this.Owner.MapOperations.ObtainTileOwnerBits(horizontalLocation, verticalLocation));

					this.Owner.Services.FloodBox(Screen.Box, pixelHorizontal, pixelVertical, 4, 4, colour);
				}

				return;
			}

			ushort ownerIndex = this.Owner.MapOperations.ObtainTileOwner(horizontalLocation, verticalLocation);

			if (ownerIndex == 0xffff)
			{
				return;
			}

			if (ownerIndex != (ushort)this.Owner.World.PersonParticipantIndex && MapView.Revealed == 0)
			{
				ushort troopIndex = this.Owner.MapOperations.ObtainUnitAtTile(horizontalLocation, verticalLocation);
				sbyte visibility = (sbyte)this.Troops.PullAt((ushort)(0xc * troopIndex) + (ushort)(0x600 * ownerIndex), UnitMemory.ShownToParticipants);

				if ((visibility & (1 << (byte)this.Owner.World.PersonParticipantIndex)) == 0)
				{
					return;
				}
			}

			ushort ownerColour = Tables.PlayerColour(ownerIndex);

			this.Owner.Services.FloodBox(Screen.Box, pixelHorizontal + 1, pixelVertical + 1, 3, 3, Hues.Black);

			this.Owner.Services.FloodBox(Screen.Box, pixelHorizontal, pixelVertical, 3, 3, ownerColour);
		}

		private bool InGraph(int participantIndex)
		{
			return (this.Owner.World.LiveRealms & (1 << participantIndex)) != 0 || (short)participantIndex <= (short)(this.Owner.World.ComputerRivalTally + 1);
		}

		private byte HistoryScore(int epochIndex, int participantIndex)
		{
			return this.Processor.PullByte(ReplayHistoryScoresSegment, (ushort)((epochIndex * ReplayHistoryStride) + participantIndex));
		}

		private CensusTally GatherCensus()
		{
			CensusTally tally = new CensusTally();

			for (short horizontalLocation = 0; horizontalLocation < 0x50; horizontalLocation++)
			{
				for (short verticalLocation = 2; verticalLocation < 0x30; verticalLocation++)
				{
					if (this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == MapRules.OceanTerrain)
					{
						continue;
					}

					tally.Land[this.Owner.MapOperations.ObtainTileOwnerBits(horizontalLocation, verticalLocation)]++;
				}
			}

			for (short townIndex = 0; townIndex < 0x80; townIndex++)
			{
				City town = this.Owner.World.Towns[townIndex];

				if (town.ParticipantIndex == -1 || town.ConditionMarker == 0xff)
				{
					continue;
				}

				this.Owner.CityOperations.HandleCity(townIndex, -1);

				int owner = town.ParticipantIndex;
				short size = (sbyte)town.TrueExtent;

				tally.Size[owner] += size;
				tally.Approval[owner] += (short)(size + CityTurn.Happy - CityTurn.Unhappy);
				tally.Money[owner] += (short)((CityTurn.Luxuries << 1) + CityTurn.Taxes);
				tally.Unrest[owner] += (short)(CityTurn.Food - (size << 1));
				tally.Output[owner] += (short)(CityTurn.Food + CityTurn.Shields + CityTurn.Trade);

				short shields = (short)CityTurn.Shields;

				tally.Shields[owner] += shields;

				short pollution = (short)(CentralProcessor.SignedQuotient(shields, (short)Turn.PollutionFactor) - 0x14);
				pollution += (short)((short)(size * Turn.ContentAdvances) / 4);

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

		private short CensusValue(int participantIndex)
		{
			return this.censusValues[participantIndex];
		}

		private void SetCensusValue(int participantIndex, short value)
		{
			this.censusValues[participantIndex] = value;
		}

		private void DrawCensusRow(GameText title, ref short verticalLocation, bool lowerIsBetter, ushort unit, Action<short> valueOfPlayer)
		{
			this.Owner.TextPainter.DrawTextWithShadow(title, 8, verticalLocation, Hues.White);

			for (short participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				valueOfPlayer(participantIndex);
			}

			DrawCensusLine(this.censusValues, (short)(lowerIsBetter ? -verticalLocation : verticalLocation), unit);

			verticalLocation += 0xc;
		}

		private void DrawCensusFigure(short value, string unit, short verticalLocation)
		{
			EmptyMessage()
				.Number(value);

			if (unit.Length > 0)
			{
				this.Owner.Words.Append(unit);
			}

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 104, verticalLocation, Hues.White);
		}

		private void DrawCensus(short shownIndex, CensusTally tally)
		{
			short verticalLocation = 0x18;
			short row = verticalLocation;

			DrawCensusRow(GameTexts.TextApprovalRating, ref verticalLocation, false, 0x4557, participantIndex =>
			{
				SetCensusValue(participantIndex, CensusRules.ApprovalRating(
					tally.Approval[participantIndex], tally.Size[participantIndex]));

				if (participantIndex == shownIndex)
				{
					DrawCensusFigure(CensusValue(participantIndex), "%", row);
				}
			});

			row = verticalLocation;

			DrawCensusRow(GameTexts.TextPopulation, ref verticalLocation, false, 0x4565, participantIndex =>
			{
				this.Owner.Words.Clear();

				SetCensusValue(participantIndex, (short)this.Owner.Helpers.ComputePlayerPopulation(participantIndex));

				if (participantIndex == shownIndex)
				{
					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 104, row, Hues.White);
				}
			});

			row = verticalLocation;

			DrawCensusRow(GameTexts.TextGrossProduct, ref verticalLocation, false, 0x457a, participantIndex =>
			{
				this.Owner.Words.Clear();

				SetCensusValue(participantIndex, tally.Money[participantIndex]);

				EmptyMessage()
					.Number(CensusValue(participantIndex))
					.Text(" million $");

				if (participantIndex == shownIndex)
				{
					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 104, row, Hues.White);
				}
			});

			row = verticalLocation;

			DrawCensusRow(GameTexts.TextManufacturedGoods, ref verticalLocation, false, 0x4590, participantIndex =>
			{
				SetCensusValue(participantIndex, tally.Shields[participantIndex]);

				if (participantIndex == shownIndex)
				{
					DrawCensusFigure(CensusValue(participantIndex), " Mtons", row);
				}
			});

			row = verticalLocation;

			DrawCensusRow(GameTexts.TextLandArea, ref verticalLocation, false, 0x45b0, participantIndex =>
			{
				this.Owner.Words.Clear();

				SetCensusValue(participantIndex, tally.Land[participantIndex]);

				EmptyMessage()
					.Number(CensusValue(participantIndex))
					.Text(",000 sq.miles");

				if (participantIndex == shownIndex)
				{
					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 104, row, Hues.White);
				}
			});

			row = verticalLocation;

			DrawCensusRow(GameTexts.TextLiteracy, ref verticalLocation, false, 0x45c1, participantIndex =>
			{
				SetCensusValue(participantIndex, CentralProcessor.SignedQuotient(
					(short)(((tally.Size[participantIndex] << 1) + tally.Learning[participantIndex]) << 1), tally.Size[participantIndex]));

				foreach (short scienceIndex in new short[] { 0, 0x16, 0x1e, 0xd })
				{
					if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, scienceIndex) != 0)
					{
						SetCensusValue(participantIndex, (short)(CensusValue(participantIndex) << 1));
					}
				}

				SetCensusValue(participantIndex, (short)this.Owner.Helpers.ClampValue(CensusValue(participantIndex), 0, 100));

				if (participantIndex == shownIndex)
				{
					DrawCensusFigure(CensusValue(participantIndex), "%", row);
				}
			});

			row = verticalLocation;

			DrawCensusRow(GameTexts.TextDisease, ref verticalLocation, true, 0x45ce, participantIndex =>
			{
				SetCensusValue(participantIndex, CensusRules.DiseaseRate(
					tally.Size[participantIndex], tally.Health[participantIndex]));

				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 10) != 0)
				{
					SetCensusValue(participantIndex, CentralProcessor.SignedQuotient(CensusValue(participantIndex), 2));
				}

				tally.Health[participantIndex] = CentralProcessor.SignedQuotient(0x5dc, (short)(CensusValue(participantIndex) + 0x14));

				if (participantIndex == shownIndex)
				{
					DrawCensusFigure(CensusValue(participantIndex), "%", row);
				}
			});

			row = verticalLocation;

			DrawCensusRow(GameTexts.TextPollution, ref verticalLocation, true, 0x45e7, participantIndex =>
			{
				SetCensusValue(participantIndex, tally.Pollution[participantIndex]);

				tally.Health[participantIndex] -= CentralProcessor.SignedQuotient((short)(0xa * tally.Pollution[participantIndex]), tally.Size[participantIndex]);

				if (participantIndex == shownIndex)
				{
					DrawCensusFigure(CensusValue(participantIndex), "0 tons/year", row);
				}
			});

			row = verticalLocation;

			DrawCensusRow(GameTexts.TextLifeExpectancy, ref verticalLocation, false, 0x4606, participantIndex =>
			{
				SetCensusValue(participantIndex, (short)this.Owner.Helpers.ClampValue(tally.Health[participantIndex], 20, 99));

				if (participantIndex == shownIndex)
				{
					DrawCensusFigure(CensusValue(participantIndex), " years", row);
				}
			});

			row = verticalLocation;

			DrawCensusRow(GameTexts.TextFamilySize, ref verticalLocation, false, 0x4624, participantIndex =>
			{
				SetCensusValue(participantIndex, CensusRules.FamilySize(
					tally.Unrest[participantIndex], tally.Size[participantIndex]));

				if (participantIndex == shownIndex)
				{
					EmptyMessage()
						.Number(TenthsRules.Whole(CensusValue(participantIndex)))
						.Text(".")
						.Number(TenthsRules.Tenth(CensusValue(participantIndex)))
						.Text(" children");

					this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 104, row, Hues.White);
				}

				SetCensusValue(participantIndex, CentralProcessor.SignedQuotient(CensusValue(participantIndex), 0xa));
			});

			row = verticalLocation;

			DrawCensusRow(GameTexts.TextMilitaryService, ref verticalLocation, false, 0x463e, participantIndex =>
			{
				SetCensusValue(participantIndex, CentralProcessor.SignedQuotient((short)(0xa * this.Owner.World.Participants[participantIndex].TroopTally),
					tally.Size[participantIndex]));

				if (participantIndex == shownIndex)
				{
					DrawCensusFigure(CensusValue(participantIndex), " years", row);
				}
			});

			row = verticalLocation;

			DrawCensusRow(GameTexts.TextAnnualIncome, ref verticalLocation, false, 0x465f, participantIndex =>
			{
				short sciences = this.Owner.World.Participants[participantIndex].FoundScienceTally;
				short money = tally.Money[participantIndex];

				SetCensusValue(participantIndex, CentralProcessor.SignedQuotient(0x7530, (short)(sciences + 1)) > money
					? CentralProcessor.SignedQuotient((short)(money * sciences), tally.Size[participantIndex])
					: (short)(CentralProcessor.SignedQuotient(money, tally.Size[participantIndex]) * sciences));

				if (participantIndex == shownIndex)
				{
					DrawCensusFigure(CensusValue(participantIndex), "$ per capita", row);
				}
			});

			row = verticalLocation;

			DrawCensusRow(GameTexts.TextProductivity, ref verticalLocation, false, 0x466f, participantIndex =>
			{
				SetCensusValue(participantIndex, CensusRules.Productivity(
					tally.Output[participantIndex], tally.Size[participantIndex]));

				if (participantIndex == shownIndex)
				{
					DrawCensusFigure(CensusValue(participantIndex), string.Empty, row);
				}
			});
		}

		private void DrawCensusLine(short[] values, ushort valuesAddress, short verticalLocation, ushort charactersAddress)
		{
			using var scope = this.Processor.Journal.Scope($"RenderCensusLine({valuesAddress}, {verticalLocation}, 0x{charactersAddress:x4})");

			short humanIndex = this.Owner.World.PersonParticipantIndex;
			short rank = 1;
			short best = -9999;
			short bestIndex = humanIndex;

			short Amount(int participantIndex) => values[participantIndex];

			for (short participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				if (((1 << participantIndex) & this.Owner.World.LiveRealms) == 0)
				{
					continue;
				}

				if (verticalLocation < 0)
				{
					values[participantIndex] = (short)-Amount(participantIndex);
				}

				if (Amount(participantIndex) > Amount(humanIndex))
				{
					rank++;
				}

				if (best < Amount(participantIndex))
				{
					best = Amount(participantIndex);
					bestIndex = participantIndex;
				}
			}

			EmptyMessage()
				.Number(rank);

			this.Owner.Words.Append(rank switch { 1 => (ushort)0x4670, 2 => (ushort)0x4673, 3 => (ushort)0x4676, _ => (ushort)0x4679 });

			this.Owner.VideoCard.RenderLine(Screen.Box, 4, Math.Abs(verticalLocation) - 3, 316, Math.Abs(verticalLocation) - 3, Hues.LightBlue);

			this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), 192, Math.Abs(verticalLocation), Hues.White);

			if (bestIndex != humanIndex && (this.Owner.World.Participants[humanIndex].Relations[bestIndex] & 0x40) != 0)
			{
				Message("(")
					.Text(Tables.CivilizationPlural(bestIndex))
					.Text(": ")
					.Number(Math.Abs(best))
					.Text(charactersAddress)
					.Text(")");

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), -(this.Owner.TextPainter.MeasureTextWidth(this.Owner.Words.ToString()) - 318), Math.Abs(verticalLocation),
					Tables.PlayerColour(bestIndex));
			}
		}
	}
}
