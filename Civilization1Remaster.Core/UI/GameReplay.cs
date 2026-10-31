using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.UI
{
	public class GameReplay : GameModule
	{
		/// <summary>The words of the frame of ObtainDistanceToReplayCity, shared by its helpers (B3: in C#, new for every call)</summary>

		public GameReplay(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// The replay of the game (the events in RerunContent): the mode 0x680a is asked (dialog 0x3fb0; 4 without asking
		/// when 0x680e, at the end of the game): 1 slow, 2 a key per event (Esc stops), 3 into replay.txt, 4 only the
		/// destruction screens. The leaders are set as at the start of the game (the identity bits) and put back afterwards,
		/// the map is drawn in miniature, and every event written at the top (and its city on the map). Asked again until
		/// mode 3 or more, or none.
		/// </summary>
		public void DisplayReplay()
		{
			this.processor.Journal.OpenScope("DisplayReplay()");

			// The frame like the original (the functions called see the stack at the same depth)

			while (true)
			{
				if (ReplayAtGameEnd == 0)
				{
					ReplayMode = this.owner.TurnHooks.DisplayDialog(GameTexts.TextSeeReplayNoReplayQuickReplayCompleteRepl, 100, 80);
				}
				else
				{
					ReplayMode = 0x4;
				}

				if ((short)ReplayMode <= 0)
					break;

				if (ReplayMode == 3)
				{
					ushort handle = (ushort)this.owner.Libc.OpenHandle("replay.txt", 0x8301, 0x80);

					ReplayFile = handle;

					if (handle == 0xffff)
					{
						Message("File Error\n")
							.Dialog(100, 80);

						break;
					}

					DumpReplayCities();
				}

				ReplayGame();

				if (ReplayMode == 3)
				{
					this.owner.Libc.CloseHandle((short)ReplayFile);
				}

				if ((short)ReplayMode >= 3)
					break;
			}


			this.processor.Journal.CloseScope("DisplayReplay");
		}

		/// <summary>
		/// One replay: the screen kept, the leaders as at the start (put back at the end), the miniature map (not in mode 4),
		/// all events
		/// </summary>
		private void ReplayGame()
		{
			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			// No city shown on the replay map yet (segment 0x3725, 128 cities)
			for (int townIdx = 0; townIdx < 0x80; townIdx++)
			{
				EmitReplayCity(townIdx, ReplayCityActiveColumnOffset, 0xffff);
			}

			short[] leaders = new short[8];

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				leaders[participantIdx] = this.owner.World.Participants[participantIdx].ChiefVisuals;
			}

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (participantIdx == this.owner.World.PersonParticipantIdx)
					continue;

				bool secondLeader = (this.owner.World.ParticipantIdentMarkers & (1 << participantIdx)) != 0;

				this.owner.World.Participants[participantIdx].ChiefVisuals = (short)(secondLeader ? participantIdx + 8 : participantIdx);

				ObtainReplayPlayerLabel(participantIdx);
			}

			if (ReplayMode != 4)
			{
				this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

				for (short vertLoc = 1; vertLoc < 0x31; vertLoc++)
				{
					for (short horizLoc = 0; horizLoc < 0x50; horizLoc++)
					{
						bool ocean = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain;

						this.owner.Services.FloodBox(ScreenBox, horizLoc * 4, (vertLoc * 4) + 4, 4, 4, (ushort)(ocean ? 1 : 8));
					}
				}
			}

			ushort spot = 0;

			while ((short)spot < this.owner.World.RerunContentSpan)
			{
				spot = ReplayEvent(spot);

				if (ReplayMode == 2 && this.owner.Libc.ReadKey() == 0x1b)
					break;
			}

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				this.owner.World.Participants[participantIdx].ChiefVisuals = leaders[participantIdx];
			}

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (participantIdx != this.owner.World.PersonParticipantIdx)
				{
					ObtainReplayPlayerLabel(participantIdx);
				}
			}

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.owner.Startup.DisplayMouseCursor();
		}

		/// <summary>The next byte of the replay</summary>
		private byte ReplayByte(ref ushort spot)
		{
			return this.owner.World.RerunContent[spot++];
		}

		/// <summary>The plural name (0x1992) of the player</summary>
		private ushort ReplayPlural(int participantIdx)
		{
			return CivilizationPlural(participantIdx);
		}

		/// <summary>
		/// One event: the kind (high 4 bits) and the turn (low 4 bits and the next byte), the year ("x BC: " / "x AD: "), then
		/// the event; the acting player in 0x6810 (-1: none)
		/// </summary>
		/// <returns>The spot of the next event</returns>
		private ushort ReplayEvent(ushort spot)
		{
			byte head = ReplayByte(ref spot);
			ushort kind = (ushort)(head >> 4);
			ushort turn = (ushort)(((head & 0xf) << 8) + ReplayByte(ref spot));

			short year = (short)ObtainReplayYear(turn);

			ReplayActor = 0xffff;
			EmptyMessage()
				.Number(Math.Abs(year))
				.Text(year < 0 ? " BC" : " AD")
				.Text(": ");

			switch (kind)
			{
				case 1:
				case 9:
					ReplayCityEvent(ref spot, kind);
					break;

				case 2:
				case 3:
				{
					byte pair = ReplayByte(ref spot);

					this.owner.Words.Append(ReplayPlural(pair >> 4));

					this.owner.Words.Append(kind == 2 ? " declare war on " : " make peace with ");

					this.owner.Words.Append(ReplayPlural(pair & 0xf));

					RenderReplayPreamble();
					break;
				}

				case 5:
				case 6:
				case 8:
				case 10:
				{
					ushort actor = ReplayByte(ref spot);

					ReplayActor = actor;

					ushort thing = ReplayByte(ref spot);

					this.owner.Words.Append(ReplayPlural(ReplayActor));

					switch (kind)
					{
						case 5:
							this.owner.Words.Append(" discover ");
							this.owner.Words.Append(TechnologyText(thing));
							break;

						case 6:
							this.owner.Words.Append(" produce first ");
							this.owner.Words.Append(UnitText(thing));
							break;

						case 8:
							this.owner.Words.Append(" form ");
							this.owner.Words.Append(GovernmentName(thing));
							break;

						default:
							this.owner.Words.Append(" build ");

							if ((short)thing <= 7)
							{
								this.owner.Words.Append("the ");
							}

							this.owner.Words.Append(WonderText(thing));
							break;
					}

					RenderReplayPreamble();
					break;
				}

				case 11:
				{
					// The status of the human player: cities, population (two parts)
					ReplayActor = (ushort)this.owner.World.PersonParticipantIdx;

					short towns = ReplayByte(ref spot);
					sbyte populationHigh = (sbyte)ReplayByte(ref spot);
					short populationLow = ReplayByte(ref spot);

					this.owner.Words.Append(ReplayPlural(this.owner.World.PersonParticipantIdx));

					this.owner.Words.Append(": ");

					this.owner.Words.Append(this.owner.Libc.IntegerToText(towns, 10));

					this.owner.Words.Append(" cities; ");

					this.owner.Helpers.PopulationToText(populationHigh + populationLow);

					this.owner.Words.Append(" population");

					RenderReplayPreamble();
					break;
				}

				case 12:
					ReplayRanking(ref spot);
					break;

				case 13:
				{
					ushort fallen = ReplayByte(ref spot);

					ReplayActor = fallen;

					short destroyer = ReplayByte(ref spot);

					this.owner.Words.Append(CivilizationName(ReplayActor));

					this.owner.Words.Append(" civilization destroyed by ");

					this.owner.Words.Append(ReplayPlural(destroyer));

					if (ReplayMode == 4 && destroyer == this.owner.World.PersonParticipantIdx)
					{
						DisplayCivilizationDestroyedScreen((short)ReplayActor, (ushort)year);
					}

					this.owner.World.Participants[ReplayActor].ChiefVisuals ^= 8;

					ObtainReplayPlayerLabel((short)ReplayActor);

					RenderReplayPreamble();
					break;
				}
			}

			return spot;
		}

		/// <summary>
		/// A city founded (kind 1) or captured (kind 9) by the player, or destroyed (player 0xff): the text with the city name
		/// (13 bytes of TownLabels), the city on the replay map, a pause in mode 1
		/// </summary>
		private void ReplayCityEvent(ref ushort spot, ushort kind)
		{
			ushort actor = ReplayByte(ref spot);

			ReplayActor = actor;

			ushort labelIdx = ReplayByte(ref spot);
			ushort horizLoc = ReplayByte(ref spot);
			ushort vertLoc = ReplayByte(ref spot);

			if (ReplayActor != 0xff)
			{
				this.owner.Words.Append(ReplayPlural(ReplayActor));

				if (kind == 1)
				{
					this.owner.Words.Append(" found the city of ");

					AssignReplayCityContent(ReplayActor, horizLoc, vertLoc);
				}
				else
				{
					this.owner.Words.Append(" capture ");

					AssignReplayCityContentAlt(ReplayActor, horizLoc, vertLoc);
				}

				AppendReplayCityName(labelIdx);

				RenderReplayPreamble();
			}
			else
			{
				AppendReplayCityName(labelIdx);

				this.owner.Words.Append(" destroyed");

				RenderReplayPreamble();

				AssignReplayCityContentAlt(0xffff, horizLoc, vertLoc);
			}

			RenderReplayMapTile(horizLoc, vertLoc);

			if (ReplayMode == 1)
			{
				this.owner.TextPainter.AwaitTime(30);
			}
		}

		/// <summary>The 13 bytes of the city name (TownLabels) after the text at TextBuffer.Address</summary>
		private void AppendReplayCityName(ushort labelIdx)
		{
			ushort end = (ushort)(TextBuffer.Address + this.owner.Words.Length);

			for (int idx = 0; idx < 0xd; idx++)
			{
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(end + idx), (byte)this.owner.World.TownLabels[labelIdx][idx]);
			}
		}

		/// <summary>
		/// The ranking ("*** 1:x 2:y ..."): 4 bytes of two places each (high and low 4 bits, 0 = none); written only into the
		/// replay file (mode 3)
		/// </summary>
		private void ReplayRanking(ref ushort spot)
		{
			this.owner.Words.Set("*** ");

			for (short pairIdx = 0; pairIdx < 4; pairIdx++)
			{
				byte places = this.owner.World.RerunContent[spot];

				if ((places & 0xf0) != 0)
				{
					this.owner.Words.Append(this.owner.Libc.IntegerToText((short)((pairIdx << 1) + 1), 10));

					this.owner.Words.Append(":");

					this.owner.Words.Append(ReplayPlural(places >> 4));

					this.owner.Words.Append(" ");
				}

				if ((places & 0xf) != 0)
				{
					this.owner.Words.Append(this.owner.Libc.IntegerToText((short)((pairIdx << 1) + 2), 10));

					this.owner.Words.Append(":");

					this.owner.Words.Append(ReplayPlural(places & 0xf));

					this.owner.Words.Append(" ");
				}

				spot++;
			}

			if (ReplayMode == 3)
			{
				RenderReplayPreamble();
			}
		}

		/// <summary>
		/// Shows the line of a replay event (the text at TextBuffer.Address) at the top of the screen, in capitals for the human's events
		/// (0x6810), and writes it with CR LF to the replay file (0x680c) in mode 3 of 0x680a; nothing in mode 4. The text is
		/// drawn, then drawn again shortened to 312 pixels.
		/// </summary>
		public void RenderReplayPreamble()
		{
			this.processor.Journal.OpenScope("RenderReplayPreamble()");

			if (ReplayMode != 4)
			{
				if (ReplayActor == (ushort)this.owner.World.PersonParticipantIdx)
				{
					this.owner.Words.Uppercase();
				}

				this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 8, Hues.White);

				this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 4, 1, Hues.Black);

				if (ReplayMode == 3)
				{
					ushort length = this.owner.Words.Length;

					this.processor.EmitGlobalByte((ushort)(length + TextBuffer.Address), 0xd);
					this.owner.Words[length + 1] = 0xa;
					this.owner.Words[length + 2] = 0x0;

					this.owner.Libc.WriteHandle((short)ReplayFile, TextBuffer.Address, (ushort)(length + 2));
				}

				this.owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 312);

				this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 8, Hues.White);

				this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 4, 1, Hues.Black);
			}

			this.processor.Journal.CloseScope("RenderReplayPreamble");
		}

		/// <summary>
		/// The year shown for the round of the replay, as the original computes it: 1750 + 2 x the round up to round 250, after
		/// it 1850 + what is left after 250 and up to three times 50
		/// </summary>
		public ushort ObtainReplayYear(ushort round)
		{
			this.processor.Journal.OpenScope($"ObtainReplayYear({round})");

			ushort replayYear;

			if (round > 250)
			{
				round -= 250;

				if (round > 50)
				{
					round -= 50;

					if (round > 50)
					{
						round -= 50;

						if (round > 50)
						{
							round -= 50;

							replayYear = (ushort)(round + 1850);
						}
						else
						{
							replayYear = (ushort)((round << 1) + 1750);
						}
					}
					else
					{
						replayYear = (ushort)((round * 5) + 1500);
					}
				}
				else
				{
					replayYear = (ushort)((round * 10) + 1000);
				}
			}
			else
			{
				replayYear = (ushort)((round * 20) - 4000);
			}

			this.processor.Journal.CloseScope("ObtainReplayYear");

			return replayYear;
		}

		/// <summary>
		/// Puts the city into the first free entry (first column 0xffff) of the replay city table (segment 0x3725: three word
		/// columns of 128 entries at 0, 0x100 and 0x200)
		/// </summary>
		public void AssignReplayCityContent(ushort content, ushort horizLoc, ushort vertLoc)
		{
			this.processor.Journal.OpenScope($"AssignReplayCityContent({content}, {horizLoc}, {vertLoc})");

			this.processor.SegExtra.Value16 = ReplayCitySegment;

			for (int idx = 0; idx < 0x80; idx++)
			{
				if (PullReplayCity(idx, ReplayCityActiveColumnOffset) != 0xffff)
					continue;

				EmitReplayCity(idx, ReplayCityActiveColumnOffset, content);
				EmitReplayCity(idx, ReplayCityXColumnOffset, horizLoc);
				EmitReplayCity(idx, ReplayCityYColumnOffset, vertLoc);
				break;
			}

			this.processor.Journal.CloseScope("AssignReplayCityContent");
		}

		private const ushort ReplayCitySegment = 0x3725;
		private const ushort ReplayCityActiveColumnOffset = 0x0;
		private const ushort ReplayCityXColumnOffset = 0x100;
		private const ushort ReplayCityYColumnOffset = 0x200;

		private const ushort ReplayFallenLeadersSegment = 0x3772;
		private const ushort ReplayFallenLeadersTableAddress = 0x37c4;
		private const ushort ReplayLeaderFaceSegment = 0x36fa;

		private ushort PullReplayCity(int idx, int column)
		{
			return this.processor.PullUInt16(ReplayCitySegment, (ushort)((idx << 1) + column));
		}

		private void EmitReplayCity(int idx, int column, ushort amount)
		{
			this.processor.EmitUInt16(ReplayCitySegment, (ushort)((idx << 1) + column), amount);
		}

		/// <summary>
		/// Sets the first column of the first entry of the replay city table whose second and third columns are horizLoc and vertLoc
		/// </summary>
		public void AssignReplayCityContentAlt(ushort content, ushort horizLoc, ushort vertLoc)
		{
			this.processor.Journal.OpenScope($"AssignReplayCityContentAlt({content}, {horizLoc}, {vertLoc})");

			this.processor.SegExtra.Value16 = ReplayCitySegment;

			for (int idx = 0; idx < 0x80; idx++)
			{
				if (PullReplayCity(idx, ReplayCityXColumnOffset) != horizLoc || PullReplayCity(idx, ReplayCityYColumnOffset) != vertLoc)
					continue;

				EmitReplayCity(idx, ReplayCityActiveColumnOffset, content);
				break;
			}

			this.processor.Journal.CloseScope("AssignReplayCityContentAlt");
		}

		/// <summary>
		/// The entry of the replay city table nearest to the position; its distance goes to 0x6812 (999 when none is nearer).
		/// Like the original, without an entry nearer than 999 the result is what was left in the stack frame (local 4), so the
		/// frame stays, and its locals get the values the original leaves there.
		/// </summary>
		/// <returns>Entry, also in AX</returns>
		public ushort ObtainDistanceToReplayCity(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainDistanceToReplayCity({horizLoc}, {vertLoc})");

			ushort nearest = 0;

			ReplayCityDistance = 999;
			this.processor.SegExtra.Value16 = ReplayCitySegment;

			for (int idx = 0; idx < 0x80; idx++)
			{
				if (PullReplayCity(idx, ReplayCityActiveColumnOffset) == 0xffff)
					continue;

				short distance = (short)this.owner.Helpers.ObtainDistance(horizLoc, (short)PullReplayCity(idx, ReplayCityXColumnOffset),
					vertLoc, (short)PullReplayCity(idx, ReplayCityYColumnOffset));

				if (distance < (short)ReplayCityDistance)
				{
					ReplayCityDistance = (ushort)distance;
					nearest = (ushort)idx;
				}
			}


			this.processor.Journal.CloseScope("ObtainDistanceToReplayCity");

			return nearest;
		}

		/// <summary>
		/// Unless 0x680a is 4: colours the land tiles of the replay map around the position (5 tiles in every direction, columns
		/// 0 - 79, rows 2 - 48) by the nearest replay city: grey (8) when it is 6 or more tiles away or on another continent,
		/// else 7 or 15 (the city's player is 1 or not) on its own tile, else the colour of its player (0x1946). The stack frame
		/// stays: ObtainDistanceToReplayCity reads what is left below it, so it must be called at the same depth as in the original.
		/// </summary>
		public void RenderReplayMapTile(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"RenderReplayMapTile({horizLoc}, {vertLoc})");


			if (ReplayMode != 4)
			{
				int left = this.owner.Helpers.VerifyValueSpan(horizLoc - 5, 0, 80);
				int right = this.owner.Helpers.VerifyValueSpan(horizLoc + 5, 0, 80);
				int top = this.owner.Helpers.VerifyValueSpan(vertLoc - 5, 2, 49);
				int bottom = this.owner.Helpers.VerifyValueSpan(vertLoc + 5, 2, 49);

				for (int tileVert = top; tileVert < bottom; tileVert++)
				{
					for (int tileHoriz = left; tileHoriz < right; tileHoriz++)
					{
						if (this.owner.MapOps.ObtainTerrainType(tileHoriz, tileVert) == MapRules.OceanTerrain)
							continue;

						ushort cityIdx = ObtainDistanceToReplayCity(tileHoriz, tileVert);
						ushort hue = 8;

						if ((short)ReplayCityDistance < 6 &&
							this.owner.MapOps.ObtainContinentID((short)PullReplayCity(cityIdx, ReplayCityXColumnOffset), (short)PullReplayCity(cityIdx, ReplayCityYColumnOffset)) ==
							this.owner.MapOps.ObtainContinentID(tileHoriz, tileVert))
						{
							ushort cityOwner = PullReplayCity(cityIdx, ReplayCityActiveColumnOffset);

							hue = ReplayCityDistance == 0 ? (ushort)(cityOwner == 1 ? 7 : 15) :
								PlayerColour(cityOwner);
						}

						this.owner.Services.FloodBox(ScreenBox, tileHoriz * 4, (tileVert * 4) + 4, 4, 4, hue);
					}
				}
			}


			this.processor.Journal.CloseScope("RenderReplayMapTile");
		}

		/// <summary>
		/// Writes the map into the replay file (handle 0x680c) as text, a line of 80 characters and CR LF for each of the 50 rows:
		/// " " ocean, "." land, "+" a city ("*" with city walls, improvement bit 1) followed by its name (as far as the line goes)
		/// </summary>
		public void DumpReplayCities()
		{
			this.processor.Journal.OpenScope("DumpReplayCities()");

			for (int vertLoc = 0; vertLoc < 50; vertLoc++)
			{
				this.owner.Words[80] = 0xd;
				this.owner.Words[81] = 0xa;
				this.owner.Words[82] = 0;

				for (int horizLoc = 0; horizLoc < 80; horizLoc++)
				{
					bool ocean = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain;
					this.processor.EmitGlobalByte((ushort)(TextBuffer.Address + horizLoc), (byte)(ocean ? 0x20 : 0x2e));
				}

				for (int horizLoc = 0; horizLoc < 80; horizLoc++)
				{
					if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain)
						continue;

					for (int labelIdx = 0; labelIdx < 0x100; labelIdx++)
					{
						if ((sbyte)this.owner.World.TownPlaces[labelIdx].X != horizLoc || (sbyte)this.owner.World.TownPlaces[labelIdx].Y != vertLoc)
							continue;

						bool walls = (this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0;
						this.processor.EmitGlobalByte((ushort)(TextBuffer.Address + horizLoc), (byte)(walls ? 0x2a : 0x2b));

						// The name after it: at least its first character, then while the line goes on and the name does
						int place = 0;

						do
						{
							this.owner.Words[horizLoc + place + 1] = (byte)this.owner.World.TownLabels[labelIdx][place];
							place++;
						} while (horizLoc + place + 1 < 0x50 && this.owner.World.TownLabels[labelIdx][place] != '\0');

						break;
					}
				}

				this.owner.Libc.WriteHandle((short)ReplayFile, TextBuffer.Address, 0x52);
			}

			this.processor.Journal.CloseScope("DumpReplayCities");
		}

		/// <summary>
		/// The names of the civilization of the player for the replay: its name (leader record 0x1502 + 0x3a * leader) into the
		/// text at 0x1982 + 2 * player, its plural (0x14f2 + ..., or the name with "s" when empty) into the text at 0x1992 + 2 * player.
		/// The plural is made in a buffer in the stack frame, like in the original.
		/// </summary>
		public void ObtainReplayPlayerLabel(short participantIdx)
		{
			this.processor.Journal.OpenScope($"ObtainReplayPlayerLabel({participantIdx})");

			ushort leader = (ushort)(0x3a * this.owner.World.Participants[participantIdx].ChiefVisuals);
			ushort civName = LeaderTextAt(leader, LeaderTable.Civilization);
			ushort pluralName = LeaderTextAt(leader, LeaderTable.CivilizationPlural);

			this.owner.Names.Set(CivilizationName(participantIdx), civName);

			if (this.processor.PullGlobalByte(pluralName) == 0)
			{
				string plural = this.processor.PullText(CPU.AsLinearAddress(this.processor.SegData.Value16, civName)) + "s";
				this.owner.Names.Set(CivilizationPlural(participantIdx), plural);
			}
			else
			{
				this.owner.Names.Set(CivilizationPlural(participantIdx), pluralName);
			}

			this.processor.Journal.CloseScope("ObtainReplayPlayerLabel");
		}

		/// <summary>
		/// The end of a conquest victory: the replay of the game (0x680e set meanwhile, the 14 bytes at 0x3772:0x37c4 cleared to
		/// 0xff), the leader's music, "The entire world hails ... the CONQUEROR!" until a key or click, then the map screen
		/// again. The texts have a shadow, in other colours with the colour cycles (0xd762).
		/// The stack frame stays: a called function leaves BP changed, and the original ends with the stack that gives.
		/// </summary>
		public void DisplayConquerorScreen()
		{
			this.processor.Journal.OpenScope("DisplayConquerorScreen()");

			ReplayAtGameEnd = 0x1;

			this.owner.Helpers.HaltHueCycling();

			this.owner.Startup.ConcealMouseCursor();

			FallenLeadersCount = 0;

			for (int idx = 0; idx < 0xe; idx++)
			{
				this.processor.EmitUInt8(ReplayFallenLeadersSegment, (ushort)(idx + ReplayFallenLeadersTableAddress), 0xff);
			}

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

			DrawingBox.TypefaceIdx = 0x6;

			DisplayReplay();

			ushort leader = (ushort)(0x3a * this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ChiefVisuals);
			this.owner.Services.PerformSound(LeaderWordAt(leader, LeaderTable.Tune), 3);

			this.owner.Services.FloodBox(ScreenBox, 18, 150, 284, 32,
				(ushort)(VgaMode != 0 ? 34 : 7));

			this.owner.Words.Set("The entire world hails");

			this.owner.TextPainter.RenderCenteredTextToScreen0(this.owner.Words.ToString(), 160, 152, 20);
			this.owner.TextPainter.RenderCenteredTextToScreen0(this.owner.Words.ToString(), 160, 151, (ushort)(VgaMode != 0 ? 23 : 15));

			Message(LeaderName(this.owner.World.PersonParticipantIdx))
				.Text(" the CONQUEROR!");

			this.owner.TextPainter.RenderCenteredTextToScreen0(this.owner.Words.ToString(), 160, 168, 20);
			this.owner.TextPainter.RenderCenteredTextToScreen0(this.owner.Words.ToString(), 160, 167, (ushort)(VgaMode != 0 ? 23 : 15));

			this.owner.TurnLogic.AwaitForKey();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.Services.PerformSound(1, 0);

			DrawingBox.TypefaceIdx = 0x1;

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.TurnHooks.RepaintMainScreen();

			ReplayAtGameEnd = 0x0;

			this.processor.Journal.CloseScope("DisplayConquerorScreen");
		}

		/// <summary>
		/// The screen of a destroyed civilization: its leader (king00.pic with the number 0x3938 + ChiefVisuals, kink on EGA
		/// for leaders 0 and 3) looks out of the wall of the fallen leaders (0x3772:0x37c4, 14 places from slam2.pic, slag2.pic on
		/// EGA) and says his last words; the leader is put into the wall at place 0xb1d4 (the places of the two rows taken in
		/// turn), then "&lt;year&gt;: &lt;the human civilization&gt; destroy the &lt;civilization&gt;!" (the second line overwrites "the ").
		/// The file names and pictures are in the stack frame, like in the original.
		/// </summary>
		public void DisplayCivilizationDestroyedScreen(short participantIdx, ushort year)
		{
			this.processor.Journal.OpenScope($"DisplayCivilizationDestroyedScreen({participantIdx}, {year})");

			short leaderIdx = (sbyte)this.processor.PullGlobalByte((ushort)((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals + 0x3938));
			char tens = (char)('0' + (leaderIdx / 10));
			char ones = (char)('0' + (leaderIdx % 10));
			string portraitPic = (VgaMode == 0 && (leaderIdx == 0 || leaderIdx == 3))
				? $"kink{tens}{ones}.pic"
				: $"king{tens}{ones}.pic";

			this.owner.Startup.QueryForDisk((ushort)(((ushort)leaderIdx / 7) + 4), 1);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, portraitPic, 0);

			ushort portrait = this.owner.VideoCard.ScreenToPicture(1, 0xb5, 0x43, 0x8b, 0x85);

			ushort speakingMouth = this.owner.VideoCard.ScreenToPicture(1, 1, 0x33, 0x3b, 0x31);

			ushort closedMouth = this.owner.VideoCard.ScreenToPicture(1, 1, 0x97, 0x3b, 0x31);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, GameTexts.FileSlam1Pic, 0);

			if (VgaMode != 0)
			{
				this.owner.Services.FloodColors(5, 0, 0, 0);
			}

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			string wallName = VgaMode == 0 ? "slag2.pic" : "slam2.pic";

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0x85, wallName, 0);

			RenderWallOfLeaders(ScreenBox);

			short mouthHoriz = 0x5a;
			short mouthVert = 0;
			ushort mouths = (ushort)(leaderIdx << 2);

			mouthHoriz += (short)(this.processor.PullUInt16(ReplayLeaderFaceSegment, (ushort)(mouths + 0x10)) - 0xb4);
			mouthHoriz -= (short)this.processor.PullUInt16(ReplayLeaderFaceSegment, (ushort)(mouths + 0x12));
			mouthVert += (short)(this.processor.PullUInt16(ReplayLeaderFaceSegment, (ushort)(mouths + 0x48)) - 0x40);
			mouthVert -= (short)this.processor.PullUInt16(ReplayLeaderFaceSegment, (ushort)(mouths + 0x4a));

			this.owner.Services.RenderPictureToScreen(ScreenBox, 90, 0, portrait);

			this.owner.Services.RenderPictureToScreen(ScreenBox, mouthHoriz, mouthVert - 2, speakingMouth);

			this.owner.Services.PerformSound(LeaderWord(this.owner.World.Participants[participantIdx].ChiefVisuals, LeaderTable.Tune), 3);

			if (VgaMode != 0)
			{
				string portraitPal = $"king{tens}{ones}.pal";

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, portraitPal, 0xc1d6);

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, GameTexts.FileSlam1Pal, 0xbdee);

				for (ushort colourIdx = 0xc0; colourIdx < 0x1b0; colourIdx++)
				{
					this.processor.EmitGlobalByte((ushort)(colourIdx + 0xbdf4), this.processor.PullGlobalByte((ushort)(colourIdx + 0xc1dc)));
				}

				this.owner.Services.ImportColorsFromBuffer(8, 0xbdee);
			}

			sbyte fallen = (sbyte)FallenLeadersCount;
			short place = (short)(fallen % 7);

			place = (place & 1) != 0 ? (short)-((place / 2) - 6) : (short)(place / 2);

			FallenLeadersCount = (ushort)(fallen + 1);

			if (fallen >= 7)
				place += 7;

			this.processor.EmitUInt8(ReplayFallenLeadersSegment, (ushort)(place + ReplayFallenLeadersTableAddress), (byte)leaderIdx);

			this.owner.Services.RestoreTimer();

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, GameTexts.FileSlam1PicB, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0x85, wallName, 0);

			RenderWallOfLeaders(BackScreenBox);

			while ((short)this.owner.Timer.Beats < 0xf0)
			{
				this.owner.Words.Clear();
			}

			this.owner.Words.Append(this.owner.Libc.IntegerToText(Math.Abs((short)year), 10));

			this.owner.Words.Append((short)year < 0 ? " BC" : " AD");

			this.owner.Words.Append(": ");

			this.owner.Words.Append(CivilizationPlural((ushort)this.owner.World.PersonParticipantIdx));

			this.owner.Words.Append(" destroy");

			RenderShadowedCentredLine(152);

			this.owner.Words.Set("the ");

			Message(CivilizationName((ushort)participantIdx))
				.Text(" civilization!");

			RenderShadowedCentredLine(168);

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0x85, 0x140, 0x43, BackScreenBox, 0, 0x85);

			this.owner.Services.RenderPictureToScreen(ScreenBox, mouthHoriz, mouthVert - 2, closedMouth);

			this.owner.Helpers.ReleaseResource(portrait, 0);

			this.owner.Services.PerformSound(1, 0);

			this.owner.TextPainter.AwaitTime(60);

			this.owner.Services.PerformSound(LeaderWord(this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ChiefVisuals, LeaderTable.Tune), 3);

			this.owner.VideoCard.RenderScreenToMainScreen(1, 0x1e);

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.owner.Services.DisplayScreen(0);

			this.owner.Services.PerformSound(1, 0);

			this.processor.Journal.CloseScope("DisplayCivilizationDestroyedScreen");
		}

		/// <summary>
		/// The 14 places of the wall of fallen leaders (0x3772:0x37c4, 0xff empty) on the screen: the leaders' pictures from the
		/// back screen 0x19d4 (7 a row from y 0x85) at 0x2e apart, the second row 0x2a higher
		/// </summary>
		private void RenderWallOfLeaders(ushort screen)
		{
			ushort placeIdx;

			for (placeIdx = 0; (short)placeIdx < 0xe; placeIdx++)
			{
				byte leaderIdx = this.processor.PullUInt8(ReplayFallenLeadersSegment, (ushort)(placeIdx + ReplayFallenLeadersTableAddress));

				if (leaderIdx == 0xff)
					continue;

				this.owner.VideoCard.RenderImage(
					BackScreenBox,
					(ushort)(((ushort)leaderIdx % 7 * 0x1c) + 1),
					(ushort)(((ushort)leaderIdx / 7 * 0x22) + 0x85),
					0x1b, 0x21,
					screen,
					(ushort)((placeIdx % 7 * 0x2e) + 8),
					(ushort)((-(placeIdx / 7) * 0x2a) + 0x31));
			}

		}

		/// <summary>The text TextBuffer.Address centred at x 160 in colour 20, with the text in colour 23 (VGA) or 15 one line higher</summary>
		private void RenderShadowedCentredLine(int vertLoc)
		{
			this.owner.TextPainter.RenderCenteredTextToScreen0(this.owner.Words.ToString(), 160, vertLoc, 20);

			this.owner.TextPainter.RenderCenteredTextToScreen0(this.owner.Words.ToString(), 160, vertLoc - 1, VgaMode != 0 ? (ushort)23 : (ushort)15);
		}
	}
}
