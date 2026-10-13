using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.UI
{
	public class WorldMap
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public WorldMap(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
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

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0x19d4), 0, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			short person = this.owner.World.PersonParticipantIdx;
			ushort seen = (ushort)(1 << (byte)person);
			ushort top = 0x3e7, bottom = 0;
			short shiftHoriz = (short)(0x28 - this.owner.World.Participants[person].HorizBegin), shiftVert;

			this.processor.EmitLocalWord(0x2, (ushort)person);
			this.processor.EmitLocalWord(0x18, 0x3e7);
			this.processor.EmitLocalWord(0x1c, 0);
			this.processor.EmitLocalWord(0x1e, seen);

			if (this.processor.PullGlobalWord(0xd806) != 0)
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

				this.processor.EmitLocalWord(0xe, 0x50);

				shiftVert = (short)-((short)(bottom + top) / 2 - 0x19);
			}

			this.processor.EmitLocalWord(0x6, (ushort)shiftHoriz);
			this.processor.EmitLocalWord(0x8, (ushort)shiftVert);
			this.processor.EmitLocalWord(0x1a, top);
			this.processor.EmitLocalWord(0x20, bottom);

			short column = 0, row;

			for (row = 0; row < 0x32; row++)
			{
				for (column = 0; column < 0x50; column++)
				{
					bool shown = (this.owner.World.ChartSight[(ushort)column, (ushort)row] & seen) != 0 &&
						this.owner.MapOps.VerifyMapLimits(0, row + shiftVert) != 0;

					if (!shown && this.processor.PullGlobalWord(0xd806) == 0)
						continue;

					RenderWorldMapTile(marker, column, row, shiftHoriz, shiftVert);
				}
			}

			this.processor.EmitLocalWord(0xa, (ushort)column);
			this.processor.EmitLocalWord(0x10, (ushort)row);

			if (this.processor.PullGlobalWord(0xb278) != 1)
			{
				this.owner.Conquest.AwaitForKeyPressOrMouseClick();

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0xaa), 0, 0);
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

			this.processor.EmitLocalWord(0x12, (ushort)pixelHoriz);
			this.processor.EmitLocalWord(0x16, (ushort)pixelVert);

			ushort terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
			this.processor.EmitLocalWord(0x14, terrain);

			if (marker == 0)
			{
				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), pixelHoriz, pixelVert, 4, 4, terrain == 0xa ? (ushort)1 : (ushort)2);
			}
			else
			{
				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19e8), (ushort)((terrain << 2) + 0xa0), (ushort)((((horizLoc + vertLoc) & 1) << 2) + 0x6f),
					4, 4, this.processor.PullGlobalWord(0xaa), (ushort)pixelHoriz, (ushort)pixelVert);
			}

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				ushort townIdx = this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc);
				this.processor.EmitLocalWord(0x4, townIdx);

				if (this.owner.World.Towns[townIdx].ShownExtent != 0 || this.processor.PullGlobalWord(0xd806) != 0 ||
					this.owner.World.Towns[townIdx].ParticipantIdx == this.owner.World.PersonParticipantIdx)
				{
					ushort colour = this.processor.PullGlobalWord((ushort)((this.owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc) << 1) + 0x1946));
					this.processor.EmitLocalWord(0x22, colour);

					this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), pixelHoriz, pixelVert, 4, 4, colour);
				}

				return;
			}

			ushort ownerIdx = this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc);
			this.processor.EmitLocalWord(0x22, ownerIdx);

			if (ownerIdx == 0xffff)
				return;

			if (ownerIdx != (ushort)this.owner.World.PersonParticipantIdx && this.processor.PullGlobalWord(0xd806) == 0)
			{
				ushort troopIdx = this.owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc);
				sbyte visibility = (sbyte)this.processor.PullGlobalByte((ushort)((ushort)(0xc * troopIdx) + (ushort)(0x600 * ownerIdx) + 0x81dd));

				if ((visibility & (1 << (byte)this.owner.World.PersonParticipantIdx)) == 0)
					return;
			}

			ushort ownerColour = this.processor.PullGlobalWord((ushort)((ownerIdx << 1) + 0x1946));
			this.processor.EmitLocalWord(0x22, ownerColour);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), pixelHoriz + 1, pixelVert + 1, 3, 3, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), pixelHoriz, pixelVert, 3, 3, ownerColour);
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

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0x19d4), 0, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 4);

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				this.owner.TextPainter.RenderTextWithShadow(this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)), participantIdx * 40, 8,
					this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1946)));

				this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), participantIdx * 40, 16,
					this.processor.PullGlobalWord((ushort)((((participantIdx << 5) + 0x40) << 1) + 0xd4ce)));
			}

			for (int continentIdx = 1; continentIdx < 0xf; continentIdx++)
			{
				int vertLoc = (continentIdx << 3) + 0x18;

				this.processor.EmitGlobalByte(0xba06, 0);
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)this.processor.PullGlobalWord((ushort)((continentIdx << 1) + 0xdcfe)), 10));
				this.owner.TextPainter.RenderTextToScreen0(0xba06, 8, vertLoc, 0);

				for (int participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					PlayerContinent landmass = this.owner.World.Participants[participantIdx].Landmasses[continentIdx];

					if (landmass.Assault == 0)
						continue;

					this.processor.EmitGlobalByte(0xba06, 0);
					this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(landmass.Guard, 10));
					this.owner.Libc.AppendText(0xba06, "/");
					this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(landmass.Assault, 10));
					this.owner.Libc.AppendText(0xba06, "/");
					this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(landmass.TownTally, 10));

					this.owner.TextPainter.RenderTextToScreen0(0xba06, participantIdx * 40, vertLoc,
						this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1946)));
				}
			}

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0xaa), 0, 0);

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

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0x19d4), 0, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 8);

			ushort peak = 0x32;

			this.owner.Dialogs.RenderBox(4, 9, 311, 183, 0);

			this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 2);

			for (short round = 0; round <= this.owner.World.RoundTally && round <= 0x258; round += 0x32)
			{
				short horizLoc = (short)(round / 2);

				this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), horizLoc + 4, 9, horizLoc + 4, 192, 0);

				if (round % 100 != 0)
					continue;

				short year = (short)this.owner.Replayer.ObtainReplayYear((ushort)round);

				this.processor.EmitGlobalByte(0xba06, 0);
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(Math.Abs(year), 10));
				this.owner.Libc.AppendText(0xba06, year < 0 ? (ushort)0x43d2 : (ushort)0x43d5);
				this.owner.TextPainter.RenderTextToScreen0(0xba06, horizLoc, 194, 15);
			}

			this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 1);

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

					this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), previousHoriz + 4, 192 - previousVert, horizLoc + 4, 192 - vertLoc,
						this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1946)));

					previousHoriz = horizLoc;
					previousVert = vertLoc;
				}
			}

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0xaa), 0, 0);

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

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0x19d4), 0, 0);

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

					this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 3);

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

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0xaa), 0, 0);

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

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0x19d4), 0, 0);

			ushort measure = (ushort)this.owner.Libc.Dice.DrawInt(5);

			this.processor.EmitLocalWord(0x4, measure);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 3);

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((this.owner.Libc.Dice.DrawInt(4) << 1) + 0x4516)));

			this.owner.Libc.AppendText(0xba06, " completes his epic history:");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 0, 4, 15);

			this.owner.Libc.CopyText(0xba06, "'The ");

			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((measure << 1) + 0x452c)));

			this.owner.Libc.AppendText(0xba06, " Civilizations in the World'");

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
						town.TrueExtent + this.processor.PullGlobalWord(0x70e2) - this.processor.PullGlobalWord(0x70e4)));
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
			bool ranked = false;

			for (short placeIdx = 1; placeIdx < 8; placeIdx++)
			{
				if (((1 << (byte)placeIdx) & (ushort)this.owner.World.LiveRealms) == 0)
					continue;

				bestValue = 0;
				best = -1;
				ranked = true;

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

				this.processor.EmitGlobalByte(0xba06, 0x0);

				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText((short)(rank + 1), 10));

				this.owner.Libc.AppendText(0xba06, ". The ");

				this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((rank << 1) + 0x451e)));

				this.owner.Libc.AppendText(0xba06, " Civilization of the ");

				this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((best << 1) + 0x1992)));

				this.owner.Libc.AppendText(0xba06, ".");

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

			this.processor.EmitLocalWord(0x2, 0x8);
			this.processor.EmitLocalWord(0xa, (ushort)vertLoc);
			this.processor.EmitLocalWord(0xc, 0x8);
			this.processor.EmitLocalWord(0x20, (ushort)rank);

			if (ranked)
			{
				this.processor.EmitLocalWord(0x6, (ushort)bestValue);
				this.processor.EmitLocalWord(0x1e, (ushort)best);
			}

			this.owner.World.Participants[0].RunningSagaStanding++;

			this.owner.TurnLogic.AwaitForKey();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0xaa), 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayEpicChronicle");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void DisplayCensus(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayCensus({participantIdx})");

			// function body
			this.processor.EnterFrame(0xba);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 1);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " Demographics");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 4, 15);

			this.processor.EmitLocalWord(0x56, 0x0);

		L0dde:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98), 0x1);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x40), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xb8), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x78), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x52), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x10), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x88), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x20), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x30), this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x66), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x56);
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0dde;
			this.processor.EmitLocalWord(0x42, this.processor.RegAcc.Value16);
			goto L0e4f;

		L0e18:
			this.processor.EmitLocalWord(0x54, this.processor.IncrementWord(this.processor.PullLocalWord(0x54)));

		L0e1b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x54), 0x30);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0e4c;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x42),
				this.processor.PullLocalSignedWord(0x54));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L0e18;

			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x42),
				this.processor.PullLocalSignedWord(0x54));

			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xb8), 
				this.processor.IncrementWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xb8))));
			goto L0e18;

		L0e4c:
			this.processor.EmitLocalWord(0x42, this.processor.IncrementWord(this.processor.PullLocalWord(0x42)));

		L0e4f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x42), 0x50);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0e5c;
			this.processor.EmitLocalWord(0x54, 0x2);
			goto L0e1b;

		L0e5c:
			this.processor.EmitLocalWord(0x56, 0x0);
			goto L0f84;

		L0e64:
			this.owner.CityOps.HandleCity(this.processor.PullLocalSignedWord(0x56), -1);

			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x56));
			this.processor.RegDest.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].ParticipantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0xba, this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x98), this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x98)), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e2));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x20), this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x20)), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e0);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xe17a));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x78), this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x78)), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70da);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0xba);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x40), this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x40)), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70da);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70dc));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70de));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x30), this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x30)), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70dc);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x66), this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x66)), this.processor.RegAcc.Value16));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0x6c98);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x14);
			this.processor.EmitLocalWord(0x68, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xba);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xb882));
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.EmitLocalWord(0x68, this.processor.SumWord(this.processor.PullLocalWord(0x68), this.processor.RegAcc.Value16));
			this.processor.CompareWord(this.processor.PullLocalWord(0x68), 0x0);
			if (this.processor.FlagSet.IsLessOrEqual) goto L0eff;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x68);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x88), this.processor.SumWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 - 0x88)), this.processor.RegAcc.Value16));

		L0eff:
			this.processor.RegAcc.Value16 = 28;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x56));
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0x56)].StructureMarkers0, 0x4);
			if (this.processor.FlagSet.IsEqual) goto L0f1f;
			this.processor.RegDest.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].ParticipantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x10), 
				this.processor.SumWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x10)), this.processor.RegAcc.Value16));

		L0f1f:
			this.processor.RegAcc.Value16 = 28;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x56));
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0x56)].StructureMarkers0, 0x100);
			if (this.processor.FlagSet.IsEqual) goto L0f40;
			this.processor.RegDest.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].ParticipantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x10), 
				this.processor.SumWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x10)), this.processor.RegAcc.Value16));

		L0f40:
			this.processor.RegAcc.Value16 = 28;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x56));
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0x56)].StructureMarkers0, 0x20);
			if (this.processor.FlagSet.IsEqual) goto L0f60;
			this.processor.RegDest.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].ParticipantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x52), 
				this.processor.SumWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x52)), this.processor.RegAcc.Value16));

		L0f60:
			this.processor.RegAcc.Value16 = 28;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x56));
			this.processor.TestBitsWord(this.owner.World.Towns[this.processor.PullLocalWord(0x56)].StructureMarkers0, 0x800);
			if (this.processor.FlagSet.IsEqual) goto L0f81;
			this.processor.RegDest.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].ParticipantIdx;
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x56)].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x52), 
				this.processor.SumWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x52)), this.processor.RegAcc.Value16));

		L0f81:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));

		L0f84:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x80);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0fa6;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x56));
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x56)].ParticipantIdx == -1) goto L0f81;
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x56)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0fa4;
			goto L0e64;

		L0fa4:
			goto L0f81;

		L0fa6:
			this.processor.EmitLocalWord(0x54, 0x18);

			this.owner.TextPainter.RenderTextWithShadow(0x4544, 8, 24, 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L0fc8:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = 0x32;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x20)));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1035;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.Libc.AppendText(0xba06, "%");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L1035:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0fc8;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x4557);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x4559, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L1071:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Helpers.ObtainPlayerPopulation(
				this.processor.PullLocalSignedWord(0x56));

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L10a9;

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L10a9:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L1071;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x4565);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x456a, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L10e5:
			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x78));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.Libc.AppendText(0xba06, " million $");

			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L114d;

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L114d:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L10e5;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x457a);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x457d, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L1189:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x66));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L11ec;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.Libc.AppendText(0xba06, " Mtons");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L11ec:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L1189;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x4590);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x4597, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L1228:
			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xb8));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.Libc.AppendText(0xba06, ",000 sq.miles");

			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1291;

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L1291:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L1228;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x45b0);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x45b5, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L12cd:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x52)));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				this.processor.PullLocalSignedWord(0x56), 0);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.ShiftLeftWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 0x1));
			}

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				this.processor.PullLocalSignedWord(0x56), 0x16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
				this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8), 
					this.processor.ShiftLeftWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8)), 0x1));
			}
		
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				this.processor.PullLocalSignedWord(0x56), 0x1e);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
				this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8), 
					this.processor.ShiftLeftWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8)), 0x1));
			}
		
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				this.processor.PullLocalSignedWord(0x56), 0xd);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
				this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8), 
					this.processor.ShiftLeftWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8)), 0x1));
			}
		
			this.processor.RegSource.Value16 = (ushort)((this.processor.PullLocalWord(0x56) << 1) + 
				this.processor.RegFrame.Value16 - 0xa8);
			this.owner.Helpers.VerifyValueSpan(this.processor.PullInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0, 100);

			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L13c5;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16), 10));

			this.owner.Libc.AppendText(0xba06, "%");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L13c5:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L13d1;
			goto L12cd;

		L13d1:
			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x45c1);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x45c3, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L1404:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = 0x32;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98)));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98));
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x10)));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(
				this.processor.PullLocalSignedWord(0x56), 10);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.RegCount.Value16 = 0x2;
				this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8));
				this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
				this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			}
		
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = 0x5dc;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8));
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, 0x14);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x10), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L14ad;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.Libc.AppendText(0xba06, "%");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L14ad:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L14b9;
			goto L1404;

		L14b9:
			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), (short)(-this.processor.PullLocalSignedWord(0x54)), 0x45ce);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x45d0, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L14ef:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x88));
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0xa;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x88)));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x10), this.processor.SubtractWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x10)), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1564;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.Libc.AppendText(0xba06, "0 tons/year");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L1564:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L14ef;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), (short)(-this.processor.PullLocalSignedWord(0x54)), 0x45e7);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x45ee, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L15a3:
			this.processor.RegSource.Value16 = (ushort)((this.processor.PullLocalWord(0x56) << 1) +
				this.processor.RegFrame.Value16);

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x10)), 20, 99);

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1616;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.Libc.AppendText(0xba06, " years");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L1616:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L15a3;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x4606);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x460b, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L1652:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = 0x28;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x40)));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x14);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1678;
			goto L1704;

		L1678:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)) / 10), 10));

			this.owner.Libc.AppendText(0xba06, ".");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)) / 10), 10));

			this.owner.Libc.AppendText(0xba06, " children");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L1704:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegCount.Value16 = 0xa;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x56);
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1723;
			goto L1652;

		L1723:
			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x4624);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x4625, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L1756:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xa;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, 
				(ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x56)].TroopTally);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x98));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L17c2;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.Libc.AppendText(0xba06, " years");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L17c2:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L1756;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x463e);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x4643, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);
			goto L1874;

		L1800:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x78));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x98));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, 
				(ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x56)].FoundScienceTally);

		L1813:
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1871;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegStack.Value16,
					(ushort)((this.processor.RegFrame.Value16 + (this.processor.PullLocalWord(0x56) << 1) - 0xa8) & 0xffff)), 10));

			this.owner.Libc.AppendText(0xba06, "$ per capita");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L1871:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));

		L1874:
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L18a3;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0x7530;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = (ushort)(this.owner.World.Participants[this.processor.PullLocalWord(0x56)].FoundScienceTally + 1);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x78)));
			if (this.processor.FlagSet.IsGreater) goto L1892;
			goto L1800;

		L1892:
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x78));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, 
				(ushort)this.owner.World.Participants[this.processor.PullLocalWord(0x56)].FoundScienceTally);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x98));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			goto L1813;

		L18a3:
			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x465f);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.TextPainter.RenderTextWithShadow(0x4661, 8, this.processor.PullLocalSignedWord(0x54), 15);

			this.processor.EmitLocalWord(0x56, 0x1);

		L18d6:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x56);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegFrame.Value16);
			this.processor.RegAcc.Value16 = 0xa;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x30)));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0x98));
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8), this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1933;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 - 0xa8)), 10));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 104, this.processor.PullLocalSignedWord(0x54), 15);

		L1933:
			this.processor.EmitLocalWord(0x56, this.processor.IncrementWord(this.processor.PullLocalWord(0x56)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x56), 0x8);
			if (this.processor.FlagSet.IsLess) goto L18d6;

			RenderCensusLine((ushort)(this.processor.RegFrame.Value16 - 0xa8), this.processor.PullLocalSignedWord(0x54), 0x466f);

			this.processor.EmitLocalWord(0x54, this.processor.SumWord(this.processor.PullLocalWord(0x54), 0xc));

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayCensus");
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

			this.processor.EmitGlobalByte(0xba06, 0);
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(rank, 10));

			// "st", "nd", "rd", "th"
			this.owner.Libc.AppendText(0xba06, rank switch { 1 => (ushort)0x4670, 2 => (ushort)0x4673, 3 => (ushort)0x4676, _ => (ushort)0x4679 });

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 4, Math.Abs(vertLoc) - 3, 316, Math.Abs(vertLoc) - 3, 9);

			this.owner.TextPainter.RenderTextWithShadow(0xba06, 192, Math.Abs(vertLoc), 15);

			if (bestIdx != humanIdx && (this.owner.World.Participants[humanIdx].Relations[bestIdx] & 0x40) != 0)
			{
				this.owner.Libc.CopyText(0xba06, "(");
				this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((bestIdx << 1) + 0x1992)));
				this.owner.Libc.AppendText(0xba06, ": ");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(Math.Abs(best), 10));
				this.owner.Libc.AppendText(0xba06, charsAddr);
				this.owner.Libc.AppendText(0xba06, ")");

				this.owner.TextPainter.RenderTextToScreen0(0xba06, -(this.owner.TextPainter.ObtainTextWidth(0xba06) - 318), Math.Abs(vertLoc),
					this.processor.PullGlobalWord((ushort)((bestIdx << 1) + 0x1946)));
			}

			this.processor.Journal.CloseScope("RenderCensusLine");
		}
	}
}
