using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.UI
{
	public class WorldMap : GameModule
	{
		public WorldMap(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// The world map, 4 pixels a tile, of what the human player has seen (everything with the cheat 0xd806): shifted so that
		/// his start column (HorizBegin) is in the middle and his units and cities in the middle rows (all rows when he has seen
		/// a pole); terrain as land / sea (marker 0) or as the small pictures of 0x19e8, cities he knows in their owner's colour,
		/// units he sees as a small box in their owner's colour. Waits for a key or a click unless 0xb278 is 1. The final values
		/// of the locals stay in the stack frame, like in the original.
		/// </summary>
		public void DisplayWorldMap(ushort marker)
		{
			this.processor.Journal.OpenScope($"DisplayWorldMap({marker})");

			this.processor.EnterFrame(0x22);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);

			short person = this.owner.World.PersonParticipantIdx;
			ushort seen = (ushort)(1 << (byte)person);
			ushort top = 0x3e7, bottom = 0;
			short shiftHoriz = (short)(0x28 - this.owner.World.Participants[person].HorizBegin), shiftVert;

			if (MapRevealed != 0)
			{
				shiftHoriz = 0;
				shiftVert = 0;
			}
			else
			{
				for (int troopIdx = 0; troopIdx < 0x80; troopIdx++)
				{
					ushort troop = (ushort)((0x600 * person) + (0xc * troopIdx) + 0x81d4);

					if (this.processor.PullGlobalByte((ushort)(troop + 3)) != 0xff)
						FitWorldMapRows(this.processor.PullGlobalByte((ushort)(troop + 2)), ref top, ref bottom);
				}

				for (int townIdx = 0; townIdx < 0x80; townIdx++)
				{
					if (this.owner.World.Towns[townIdx].ParticipantIdx == person)
						FitWorldMapRows((byte)(sbyte)this.owner.World.Towns[townIdx].Place.Y, ref top, ref bottom);
				}

				for (ushort horizLoc = 0; horizLoc < 0x50; horizLoc++)
				{
					if ((this.owner.World.ChartSight[horizLoc, 0] & seen) != 0 || (this.owner.World.ChartSight[horizLoc, 1] & seen) != 0)
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
					bool shown = (this.owner.World.ChartSight[(ushort)column, (ushort)row] & seen) != 0 &&
						this.owner.MapOps.VerifyMapLimits(0, row + shiftVert) != 0;

					if (!shown && MapRevealed == 0)
						continue;

					RenderWorldMapTile(marker, column, row, shiftHoriz, shiftVert);
				}
			}

			if (WorldMapView != 1)
			{
				this.owner.Conquest.AwaitForKeyPressOrMouseClick();

				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);
			}

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayWorldMap");
		}

		/// <summary>The rows of the world map widen to the row (unsigned) of a unit or a city of the player</summary>
		private static void FitWorldMapRows(byte vertLoc, ref ushort top, ref ushort bottom)
		{
			if (top > vertLoc)
				top = vertLoc;

			if (bottom < vertLoc)
				bottom = vertLoc;
		}

		/// <summary>One tile of the world map (see DisplayWorldMap)</summary>
		private void RenderWorldMapTile(ushort marker, short horizLoc, short vertLoc, short shiftHoriz, short shiftVert)
		{
			short pixelHoriz = (short)(this.owner.Routing.TunePosition(horizLoc + shiftHoriz) << 2);
			short pixelVert = (short)((vertLoc + shiftVert) << 2);

			ushort terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

			if (marker == 0)
			{
				this.owner.Services.FloodBox(ScreenBox, pixelHoriz, pixelVert, 4, 4, terrain == 0xa ? (ushort)1 : (ushort)2);
			}
			else
			{
				this.owner.VideoCard.RenderImage(PictureScreenBox, (ushort)((terrain << 2) + 0xa0), (ushort)((((horizLoc + vertLoc) & 1) << 2) + 0x6f),
					4, 4, ScreenBox, (ushort)pixelHoriz, (ushort)pixelVert);
			}

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				ushort townIdx = this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc);

				if (this.owner.World.Towns[townIdx].ShownExtent != 0 || MapRevealed != 0 ||
					this.owner.World.Towns[townIdx].ParticipantIdx == this.owner.World.PersonParticipantIdx)
				{
					ushort colour = this.processor.PullGlobalWord((ushort)((this.owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc) << 1) + 0x1946));

					this.owner.Services.FloodBox(ScreenBox, pixelHoriz, pixelVert, 4, 4, colour);
				}

				return;
			}

			ushort ownerIdx = this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc);

			if (ownerIdx == 0xffff)
				return;

			if (ownerIdx != (ushort)this.owner.World.PersonParticipantIdx && MapRevealed == 0)
			{
				ushort troopIdx = this.owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);
				sbyte visibility = (sbyte)this.processor.PullGlobalByte((ushort)((ushort)(0xc * troopIdx) + (ushort)(0x600 * ownerIdx) + 0x81dd));

				if ((visibility & (1 << (byte)this.owner.World.PersonParticipantIdx)) == 0)
					return;
			}

			ushort ownerColour = this.processor.PullGlobalWord((ushort)((ownerIdx << 1) + 0x1946));

			this.owner.Services.FloodBox(ScreenBox, pixelHoriz + 1, pixelVert + 1, 3, 3, 0);

			this.owner.Services.FloodBox(ScreenBox, pixelHoriz, pixelVert, 3, 3, ownerColour);
		}

		/// <summary>
		/// The (debug) screen of the continents: for the players 1 - 7 their name and unit picture, and for every continent 1 - 14
		/// its value (0xdcfe) and, for the players with attack there, "defence/attack/cities"; waits for a key or a click and
		/// puts the screen back
		/// </summary>
		public void DisplayGameFigures()
		{
			this.processor.Journal.OpenScope("DisplayGameFigures()");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 4);

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				this.owner.TextPainter.RenderTextWithShadow(this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)), participantIdx * 40, 8,
					this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1946)));

				this.owner.Services.RenderPictureToScreen(ScreenBox, participantIdx * 40, 16,
					this.processor.PullGlobalWord((ushort)((((participantIdx << 5) + 0x40) << 1) + 0xd4ce)));
			}

			for (int continentIdx = 1; continentIdx < 0xf; continentIdx++)
			{
				int vertLoc = (continentIdx << 3) + 0x18;

				EmptyMessage()
					.Number((short)this.processor.PullGlobalWord((ushort)((continentIdx << 1) + 0xdcfe)));
				this.owner.TextPainter.RenderTextToScreen0(0xba06, 8, vertLoc, 0);

				for (int participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					PlayerContinent landmass = this.owner.World.Participants[participantIdx].Landmasses[continentIdx];

					if (landmass.Assault == 0)
						continue;

					EmptyMessage()
						.Number(landmass.Guard)
						.Text("/")
						.Number(landmass.Assault)
						.Text("/")
						.Number(landmass.TownTally);

					this.owner.TextPainter.RenderTextToScreen0(0xba06, participantIdx * 40, vertLoc,
						this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1946)));
				}
			}

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("DisplayGameFigures");
		}

		/// <summary>
		/// The graph of the civilizations' scores from the replay history (0x3772: a byte per player for every 4 turns): year lines
		/// every 50 turns (labelled every 100), the names of the players 1 - 8 that live or were in the game (player 8 reads the
		/// next table, like in the original), and a line per player 1 - 7 scaled to the highest value (at least 50), until a key
		/// or a click
		/// </summary>
		public void DisplayCivilizationGraph()
		{
			this.processor.Journal.OpenScope("DisplayCivilizationGraph()");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 8);

			ushort peak = 0x32;

			this.owner.Dialogs.RenderBox(4, 9, 311, 183, 0);

			this.processor.EmitGlobalWord((ushort)(ScreenBox + 0x10), 2);

			for (short round = 0; round <= this.owner.World.RoundTally && round <= 0x258; round += 0x32)
			{
				short horizLoc = (short)(round / 2);

				this.owner.VideoCard.RenderLine(ScreenBox, horizLoc + 4, 9, horizLoc + 4, 192, 0);

				if (round % 100 != 0)
					continue;

				short year = (short)this.owner.Replayer.ObtainReplayYear((ushort)round);

				EmptyMessage()
					.Number(Math.Abs(year))
					.Text(year < 0 ? (ushort)0x43d2 : (ushort)0x43d5);
				this.owner.TextPainter.RenderTextToScreen0(0xba06, horizLoc, 194, 15);
			}

			this.processor.EmitGlobalWord((ushort)(ScreenBox + 0x10), 1);

			short epochTally = (short)(this.owner.World.RoundTally / 4);

			for (int participantIdx = 1; participantIdx <= 8; participantIdx++)
			{
				if (!InGraph(participantIdx))
					continue;

				this.owner.TextPainter.RenderTextToScreen0(this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)), 8, (participantIdx << 3) + 4,
					this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1946)));

				for (int epochIdx = 0; epochIdx < epochTally; epochIdx++)
				{
					byte score = HistoryScore(epochIdx, participantIdx);

					if (peak < score)
					{
						peak = score;
					}
				}
			}

			this.owner.TextPainter.RenderTextWithShadow(0x43d8, 100, 2, 15);

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

					this.owner.VideoCard.RenderLine(ScreenBox, previousHoriz + 4, 192 - previousVert, horizLoc + 4, 192 - vertLoc,
						this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1946)));

					previousHoriz = horizLoc;
					previousVert = vertLoc;
				}
			}

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("DisplayCivilizationGraph");
		}

		/// <summary>The player is shown in the graph: alive, or one of the players the game started with</summary>
		private bool InGraph(int participantIdx)
		{
			return (this.owner.World.LiveRealms & (1 << participantIdx)) != 0 || (short)participantIdx <= (short)(this.owner.World.BotRivalTally + 1);
		}

		/// <summary>The score of the player in the replay history (0x3772, 8 bytes every 4 turns)</summary>
		private byte HistoryScore(int epochIdx, int participantIdx)
		{
			return this.processor.PullUInt8(0x3772, (ushort)((epochIdx << 3) + participantIdx));
		}

		/// <summary>
		/// The Wonders of the World: a box for every built wonder (1 - 21) in the colour of the city's player, with its icon,
		/// "The " (wonders 1 - 7) and its name, and "in"/"of" (0x440e, 0x4413) the city and civilization, or " (Destroyed)" for
		/// city 128; pages of up to 7 boxes, each after a key or a click
		/// </summary>
		public void DisplayWondersOfTheWorld()
		{
			this.processor.Journal.OpenScope("DisplayWondersOfTheWorld()");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			short vertLoc = 999;

			for (short wonderIdx = 1; wonderIdx <= 0x15; wonderIdx++)
			{
				short townIdx = this.owner.World.MarvelTownIdx[wonderIdx];

				if (townIdx == -1)
					continue;

				// A new page
				if (vertLoc > 0xb4)
				{
					if (vertLoc != 999)
					{
						this.owner.Conquest.AwaitForKeyPressOrMouseClick();
					}

					this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 3);

					this.owner.TextPainter.RenderTextWithShadow(0x43f0, 100, 12, 15);

					vertLoc = 0x20;
				}

				short participantIdx = this.owner.World.Towns[townIdx].ParticipantIdx;

				this.processor.EmitGlobalByte(0xba06, 0);

				if (wonderIdx <= 7)
				{
					this.owner.Libc.AppendText(0xba06, "The ");
				}

				this.owner.Libc.AppendText(0xba06, (ushort)((wonderIdx * 0x1e) + 0xe6a));

				if (townIdx == 128)
				{
					this.owner.Libc.AppendText(0xba06, " (Destroyed)");

					participantIdx = 0;
				}
				else
				{
					this.owner.Libc.AppendText(0xba06, wonderIdx > 7 ? (ushort)0x4413 : (ushort)0x440e);
					this.owner.Conquest.ObtainCityLabel(townIdx);
					this.owner.Libc.AppendText(0xba06, ". (");
					this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)));
					this.owner.Libc.AppendText(0xba06, ")");
				}

				this.owner.Dialogs.RenderBox(8, vertLoc, 303, 15, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1946)));

				this.owner.CityOps.RenderResourceIcon((short)(wonderIdx + 0x18), 16, (short)(vertLoc + 3));

				this.owner.BlockText.ShrinkTextToWidth(0xba06, 270);

				this.owner.TextPainter.RenderTextToScreen0(0xba06, 40, vertLoc + 5, 15);

				vertLoc += 0x18;
			}

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("DisplayWondersOfTheWorld");
		}

		/// <summary>
		/// A historian's ranking of the living civilizations by one of five measures (random): 0 gold, 1 might, 2 technologies,
		/// 3 the size plus the happy minus the unhappy citizens of the cities, 4 the size of the cities. The places go into
		/// RunningSagaStanding (7 - place, the barbarians +1); known civilizations (all on the easiest level) are shown with
		/// their small palace.
		/// </summary>
		public void DisplayEpicChronicle()
		{
			this.processor.Journal.OpenScope("DisplayEpicChronicle()");

			// The values of the players are in the stack frame (the barbarians' one never set for the measures 3 and 4), like in the
			// original: HandleCity and RenderPalace read leftovers under the stack; the final values of the locals stay there
			this.processor.EnterFrame(0x20);
			this.processor.PreserveIndexRegisters();

			ushort values = (ushort)(this.processor.RegFrame.Value16 - 0x1c);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			ushort measure = (ushort)this.owner.Libc.Dice.DrawInt(5);

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 3);

			Message(this.processor.PullGlobalWord((ushort)((this.owner.Libc.Dice.DrawInt(4) << 1) + 0x4516)))
				.Text(" completes his epic history:");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 0, 4, 15);

			Message("'The ")
				.Text(this.processor.PullGlobalWord((ushort)((measure << 1) + 0x452c)))
				.Text(" Civilizations in the World'");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 12, 15);

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				this.processor.EmitGlobalWord((ushort)(values + (participantIdx << 1)), 0x0);
			}

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.owner.World.Towns[townIdx];

				if (town.ParticipantIdx == -1 || town.ConditionMarker == 0xff)
					continue;

				this.owner.CityOps.HandleCity(townIdx, -1);

				ushort place = (ushort)(values + (town.ParticipantIdx << 1));

				if (measure == 3)
				{
					this.processor.EmitGlobalWord(place, (ushort)(this.processor.PullGlobalWord(place) +
						town.TrueExtent + CityHappy - CityUnhappy));
				}

				if (measure == 4)
				{
					this.processor.EmitGlobalWord(place, (ushort)(this.processor.PullGlobalWord(place) + town.TrueExtent));
				}
			}

			for (short participantIdx = 0; participantIdx < 8; participantIdx++)
			{
				ushort place = (ushort)(values + (participantIdx << 1));

				switch (measure)
				{
					case 0:
						this.processor.EmitGlobalWord(place, (ushort)this.owner.World.Participants[participantIdx].Gold);
						break;

					case 1:
						this.processor.EmitGlobalWord(place, (ushort)this.owner.World.Participants[participantIdx].MartialMight);
						break;

					case 2:
						this.processor.EmitGlobalWord(place, 0x0);

						for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
						{
							if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
							{
								this.processor.EmitGlobalWord(place, (ushort)(this.processor.PullGlobalWord(place) + 1));
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
				if (((1 << (byte)placeIdx) & (ushort)this.owner.World.LiveRealms) == 0)
					continue;

				bestValue = 0;
				best = -1;

				for (short participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					short value = (short)this.processor.PullGlobalWord((ushort)(values + (participantIdx << 1)));

					if (value >= bestValue && ((1 << (byte)participantIdx) & (ushort)this.owner.World.LiveRealms) != 0)
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
					.Text(this.processor.PullGlobalWord((ushort)((rank << 1) + 0x451e)))
					.Text(" Civilization of the ")
					.Text(this.processor.PullGlobalWord((ushort)((best << 1) + 0x1992)))
					.Text(".");

				short humanIdx = this.owner.World.PersonParticipantIdx;

				if ((this.owner.World.Participants[humanIdx].Relations[best] & 0x40) != 0 || best == humanIdx || this.owner.World.HardnessTier == 0)
				{
					this.owner.TextPainter.AwaitTime(60);

					this.owner.TurnHooks.RenderPalace(best, this.owner.TurnHooks.ObtainPalaceLevel(this.owner.World.Participants[best].Tally), 128, (short)(vertLoc - 13));

					this.owner.Dialogs.RenderBox(8, vertLoc, 303, 15, this.processor.PullGlobalWord((ushort)((best << 1) + 0x1946)));

					this.owner.TextPainter.RenderTextToScreen0(0xba06, 40, vertLoc + 5, 15);

					vertLoc += 0x18;
				}

				this.owner.World.Participants[best].RunningSagaStanding += (short)(7 - rank);
				rank++;
				this.processor.EmitGlobalWord((ushort)(values + (best << 1)), 0xffff);
			}

			this.owner.World.Participants[0].RunningSagaStanding++;

			this.owner.TurnLogic.AwaitForKey();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayEpicChronicle");
		}

		/// <summary>
		/// The demographics of the player: statistics of all cities (each computed by HandleCity) and land tiles per player,
		/// then 13 lines with the player's value and rank (RenderCensusLine): approval, population, GNP, manufacturing goods,
		/// land area, literacy, disease, pollution, life expectancy, family size, military service, annual income, productivity.
		/// The values shown are kept in the frame (bp - 0xa8, read by RenderCensusLine) like the original.
		/// </summary>
		public void DisplayCensus(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayCensus({participantIdx})");

			this.processor.EnterFrame(0xba);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 1);

			Message(this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)))
				.Text(" Demographics");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 4, 15);

			CensusTally tally = GatherCensus();

			RenderCensus(participantIdx, tally);

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("DisplayCensus");
		}

		/// <summary>The sums of the demographics per player 0 - 7</summary>
		private sealed class CensusTally
		{
			/// <summary>City sizes (+1, so never 0)</summary>
			public readonly short[] Size = { 1, 1, 1, 1, 1, 1, 1, 1 };

			/// <summary>Land tiles owned</summary>
			public readonly short[] Land = new short[8];

			/// <summary>Taxes * 2 + 0xe17a of the cities</summary>
			public readonly short[] Money = new short[8];

			/// <summary>Sizes of the cities with the improvements 0x20 and 0x800 (each counts)</summary>
			public readonly short[] Learning = new short[8];

			/// <summary>Sizes of the cities with the improvements 0x4 and 0x100; later the life expectancy</summary>
			public readonly short[] Health = new short[8];

			/// <summary>Luxury (0x70da) - 2 * size</summary>
			public readonly short[] Unrest = new short[8];

			/// <summary>Size + happy (0x70e2) - unhappy (0x70e4)</summary>
			public readonly short[] Approval = new short[8];

			/// <summary>Luxury + shields + trade (0x70da, 0x70dc, 0x70de)</summary>
			public readonly short[] Output = new short[8];

			/// <summary>Shields (0x70dc)</summary>
			public readonly short[] Shields = new short[8];

			/// <summary>Pollution: shields / 0x6c98 - 20 + size * 0xb882 / 4, when above 0</summary>
			public readonly short[] Pollution = new short[8];
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
					if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain)
						continue;

					tally.Land[this.owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc)]++;
				}
			}

			for (short townIdx = 0; townIdx < 0x80; townIdx++)
			{
				City town = this.owner.World.Towns[townIdx];

				if (town.ParticipantIdx == -1 || town.ConditionMarker == 0xff)
					continue;

				this.owner.CityOps.HandleCity(townIdx, -1);

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

		/// <summary>The value of the player for the current line (the frame, bp - 0xa8)</summary>
		private ushort CensusValueAddress(int participantIdx)
		{
			return (ushort)(this.processor.RegFrame.Value16 + (participantIdx << 1) - 0xa8);
		}

		private short CensusValue(int participantIdx)
		{
			return (short)this.processor.PullGlobalWord(CensusValueAddress(participantIdx));
		}

		private void SetCensusValue(int participantIdx, short value)
		{
			this.processor.EmitGlobalWord(CensusValueAddress(participantIdx), (ushort)value);
		}

		/// <summary>
		/// One line: the title, the values of the players 1 - 7 (the player's one written at 104), the rank (a negative row:
		/// a lower value is better)
		/// </summary>
		private void RenderCensusRow(ushort title, ref short vertLoc, bool lowerIsBetter, ushort unit, Action<short> valueOfPlayer)
		{
			this.owner.TextPainter.RenderTextWithShadow(title, 8, vertLoc, 15);

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				valueOfPlayer(participantIdx);
			}

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), (short)(lowerIsBetter ? -vertLoc : vertLoc), unit);

			vertLoc += 0xc;
		}

		/// <summary>The value with the unit text, written at (104, row) for the player itself</summary>
		private void RenderCensusFigure(short value, string unit, short vertLoc)
		{
			EmptyMessage()
				.Number(value);

			if (unit.Length > 0)
			{
				this.owner.Libc.AppendText(0xba06, unit);
			}

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, vertLoc, 15);
		}

		/// <summary>The 13 lines of the demographics</summary>
		private void RenderCensus(short shownIdx, CensusTally tally)
		{
			short vertLoc = 0x18;
			short row = vertLoc;

			// Approval rating
			RenderCensusRow(0x4544, ref vertLoc, false, 0x4557, participantIdx =>
			{
				SetCensusValue(participantIdx, CPU.SignedQuotient((short)(0x32 * tally.Approval[participantIdx]), tally.Size[participantIdx]));

				if (participantIdx == shownIdx)
				{
					RenderCensusFigure(CensusValue(participantIdx), "%", row);
				}
			});

			// Population (the text of ObtainPlayerPopulation)
			row = vertLoc;

			RenderCensusRow(0x4559, ref vertLoc, false, 0x4565, participantIdx =>
			{
				this.processor.EmitGlobalByte(0xba06, 0x0);

				SetCensusValue(participantIdx, (short)this.owner.Helpers.ObtainPlayerPopulation(participantIdx));

				if (participantIdx == shownIdx)
				{
					this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, row, 15);
				}
			});

			// GNP (the text is made for every player)
			row = vertLoc;

			RenderCensusRow(0x456a, ref vertLoc, false, 0x457a, participantIdx =>
			{
				this.processor.EmitGlobalByte(0xba06, 0x0);

				SetCensusValue(participantIdx, tally.Money[participantIdx]);

				EmptyMessage()
					.Number(CensusValue(participantIdx))
					.Text(" million $");

				if (participantIdx == shownIdx)
				{
					this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, row, 15);
				}
			});

			// Manufacturing goods
			row = vertLoc;

			RenderCensusRow(0x457d, ref vertLoc, false, 0x4590, participantIdx =>
			{
				SetCensusValue(participantIdx, tally.Shields[participantIdx]);

				if (participantIdx == shownIdx)
				{
					RenderCensusFigure(CensusValue(participantIdx), " Mtons", row);
				}
			});

			// Land area (the text is made for every player)
			row = vertLoc;

			RenderCensusRow(0x4597, ref vertLoc, false, 0x45b0, participantIdx =>
			{
				this.processor.EmitGlobalByte(0xba06, 0x0);

				SetCensusValue(participantIdx, tally.Land[participantIdx]);

				EmptyMessage()
					.Number(CensusValue(participantIdx))
					.Text(",000 sq.miles");

				if (participantIdx == shownIdx)
				{
					this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, row, 15);
				}
			});

			// Literacy: (2 * size + learning) * 2 / size, doubled for each of the technologies 0, 0x16, 0x1e, 0xd, at most 100
			row = vertLoc;

			RenderCensusRow(0x45b5, ref vertLoc, false, 0x45c1, participantIdx =>
			{
				SetCensusValue(participantIdx, CPU.SignedQuotient(
					(short)(((tally.Size[participantIdx] << 1) + tally.Learning[participantIdx]) << 1), tally.Size[participantIdx]));

				foreach (short scienceIdx in new short[] { 0, 0x16, 0x1e, 0xd })
				{
					if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
					{
						SetCensusValue(participantIdx, (short)(CensusValue(participantIdx) << 1));
					}
				}

				SetCensusValue(participantIdx, (short)this.owner.Helpers.VerifyValueSpan(CensusValue(participantIdx), 0, 100));

				if (participantIdx == shownIdx)
				{
					RenderCensusFigure(CensusValue(participantIdx), "%", row);
				}
			});

			// Disease: 50 * size / (size + health), halved with technology 10; the life expectancy from it (1500 / (disease + 20))
			row = vertLoc;

			RenderCensusRow(0x45c3, ref vertLoc, true, 0x45ce, participantIdx =>
			{
				SetCensusValue(participantIdx, CPU.SignedQuotient((short)(0x32 * tally.Size[participantIdx]),
					(short)(tally.Size[participantIdx] + tally.Health[participantIdx])));

				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 10) != 0)
				{
					SetCensusValue(participantIdx, CPU.SignedQuotient(CensusValue(participantIdx), 2));
				}

				tally.Health[participantIdx] = CPU.SignedQuotient(0x5dc, (short)(CensusValue(participantIdx) + 0x14));

				if (participantIdx == shownIdx)
				{
					RenderCensusFigure(CensusValue(participantIdx), "%", row);
				}
			});

			// Pollution, which shortens the life expectancy
			row = vertLoc;

			RenderCensusRow(0x45d0, ref vertLoc, true, 0x45e7, participantIdx =>
			{
				SetCensusValue(participantIdx, tally.Pollution[participantIdx]);

				tally.Health[participantIdx] -= CPU.SignedQuotient((short)(0xa * tally.Pollution[participantIdx]), tally.Size[participantIdx]);

				if (participantIdx == shownIdx)
				{
					RenderCensusFigure(CensusValue(participantIdx), "0 tons/year", row);
				}
			});

			// Life expectancy (20 - 99)
			row = vertLoc;

			RenderCensusRow(0x45ee, ref vertLoc, false, 0x4606, participantIdx =>
			{
				SetCensusValue(participantIdx, (short)this.owner.Helpers.VerifyValueSpan(tally.Health[participantIdx], 20, 99));

				if (participantIdx == shownIdx)
				{
					RenderCensusFigure(CensusValue(participantIdx), " years", row);
				}
			});

			// Family size: 40 * unrest / size + 20 tenths (the original writes the tenths as value / 10 twice)
			row = vertLoc;

			RenderCensusRow(0x460b, ref vertLoc, false, 0x4624, participantIdx =>
			{
				SetCensusValue(participantIdx, (short)(CPU.SignedQuotient((short)(0x28 * tally.Unrest[participantIdx]), tally.Size[participantIdx]) + 0x14));

				if (participantIdx == shownIdx)
				{
					EmptyMessage()
						.Number((short)(CensusValue(participantIdx) / 10))
						.Text(".")
						.Number((short)(CensusValue(participantIdx) / 10))
						.Text(" children");

					this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, row, 15);
				}

				SetCensusValue(participantIdx, CPU.SignedQuotient(CensusValue(participantIdx), 0xa));
			});

			// Military service: 10 * units / size
			row = vertLoc;

			RenderCensusRow(0x4625, ref vertLoc, false, 0x463e, participantIdx =>
			{
				SetCensusValue(participantIdx, CPU.SignedQuotient((short)(0xa * this.owner.World.Participants[participantIdx].TroopTally),
					tally.Size[participantIdx]));

				if (participantIdx == shownIdx)
				{
					RenderCensusFigure(CensusValue(participantIdx), " years", row);
				}
			});

			// Annual income: money * technologies / size (divided first when the product would overflow)
			row = vertLoc;

			RenderCensusRow(0x4643, ref vertLoc, false, 0x465f, participantIdx =>
			{
				short sciences = this.owner.World.Participants[participantIdx].FoundScienceTally;
				short money = tally.Money[participantIdx];

				SetCensusValue(participantIdx, CPU.SignedQuotient(0x7530, (short)(sciences + 1)) > money
					? CPU.SignedQuotient((short)(money * sciences), tally.Size[participantIdx])
					: (short)(CPU.SignedQuotient(money, tally.Size[participantIdx]) * sciences));

				if (participantIdx == shownIdx)
				{
					RenderCensusFigure(CensusValue(participantIdx), "$ per capita", row);
				}
			});

			// Productivity: 10 * output / size
			row = vertLoc;

			RenderCensusRow(0x4661, ref vertLoc, false, 0x466f, participantIdx =>
			{
				SetCensusValue(participantIdx, CPU.SignedQuotient((short)(0xa * tally.Output[participantIdx]), tally.Size[participantIdx]));

				if (participantIdx == shownIdx)
				{
					RenderCensusFigure(CensusValue(participantIdx), "", row);
				}
			});
		}

		/// <summary>
		/// A line of the demographics: the human player's rank ("1st" ...) among the living players 1 - 7 by the words at arg1
		/// (one per player; with a negative row they are negated first, a lower value is better), and the best player's value
		/// "(civilization: value text)" in its colour when the human player has relations bit 0x40 towards it. The values are
		/// read from memory as they change, like in the original: the human player's own value is negated in the loop too.
		/// </summary>
		public void RenderCensusLine(ushort arg1, short vertLoc, ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"RenderCensusLine({arg1}, {vertLoc}, 0x{charsAddr:x4})");

			short humanIdx = this.owner.World.PersonParticipantIdx;
			short rank = 1;
			short best = -9999;
			short bestIdx = humanIdx;

			short Amount(int participantIdx) => (short)this.processor.PullGlobalWord((ushort)(arg1 + (participantIdx << 1)));

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (((1 << participantIdx) & this.owner.World.LiveRealms) == 0)
					continue;

				if (vertLoc < 0)
				{
					this.processor.EmitGlobalWord((ushort)(arg1 + (participantIdx << 1)), (ushort)-Amount(participantIdx));
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
			this.owner.Libc.AppendText(0xba06, rank switch { 1 => (ushort)0x4670, 2 => (ushort)0x4673, 3 => (ushort)0x4676, _ => (ushort)0x4679 });

			this.owner.VideoCard.RenderLine(ScreenBox, 4, Math.Abs(vertLoc) - 3, 316, Math.Abs(vertLoc) - 3, 9);

			this.owner.TextPainter.RenderTextWithShadow(0xba06, 192, Math.Abs(vertLoc), 15);

			if (bestIdx != humanIdx && (this.owner.World.Participants[humanIdx].Relations[bestIdx] & 0x40) != 0)
			{
				Message("(")
					.Text(this.processor.PullGlobalWord((ushort)((bestIdx << 1) + 0x1992)))
					.Text(": ")
					.Number(Math.Abs(best))
					.Text(charsAddr)
					.Text(")");

				this.owner.TextPainter.RenderTextToScreen0(0xba06, -(this.owner.TextPainter.ObtainTextWidth(0xba06) - 318), Math.Abs(vertLoc),
					this.processor.PullGlobalWord((ushort)((bestIdx << 1) + 0x1946)));
			}

			this.processor.Journal.CloseScope("RenderCensusLine");
		}
	}
}
