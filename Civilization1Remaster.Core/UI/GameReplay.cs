using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.UI
{
	public class GameReplay : GameModule
	{
		private const ushort ReplayCitySegment = 0x3725;
		private const ushort ReplayCityActiveColumnOffset = 0x0;
		private const ushort ReplayCityXColumnOffset = 0x100;
		private const ushort ReplayCityYColumnOffset = 0x200;
		private const ushort ReplayFallenLeadersSegment = 0x3772;
		private const ushort ReplayFallenLeadersTableAddress = 0x37c4;
		private const ushort ReplayLeaderFaceSegment = 0x36fa;

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
			this.Processor.Journal.OpenScope("DisplayReplay()");

			// The frame like the original (the functions called see the stack at the same depth)

			while (true)
			{
				if (ReplayAtGameEnd == 0)
				{
					ReplayMode = this.Owner.TurnHooks.DisplayDialog(GameTexts.TextSeeReplayNoReplayQuickReplayCompleteRepl, 100, 80);
				}
				else
				{
					ReplayMode = 0x4;
				}

				if ((short)ReplayMode <= 0)
					break;

				if (ReplayMode == 3)
				{
					ushort handle = (ushort)this.Owner.Libc.OpenHandle("replay.txt", 0x8301, 0x80);

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
					this.Owner.Libc.CloseHandle((short)ReplayFile);
				}

				if ((short)ReplayMode >= 3)
					break;
			}


			this.Processor.Journal.CloseScope("DisplayReplay");
		}

		/// <summary>
		/// Shows the line of a replay event (the text at TextBuffer.Address) at the top of the screen, in capitals for the human's events
		/// (0x6810), and writes it with CR LF to the replay file (0x680c) in mode 3 of 0x680a; nothing in mode 4. The text is
		/// drawn, then drawn again shortened to 312 pixels.
		/// </summary>
		public void RenderReplayPreamble()
		{
			this.Processor.Journal.OpenScope("RenderReplayPreamble()");

			if (ReplayMode != 4)
			{
				if (ReplayActor == (ushort)this.Owner.World.PersonParticipantIdx)
				{
					this.Owner.Words.Uppercase();
				}

				this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 8, Hues.White);

				this.Owner.TextPainter.RenderTextToScreen0(this.Owner.Words.ToString(), 4, 1, Hues.Black);

				if (ReplayMode == 3)
				{
					ushort length = this.Owner.Words.Length;

					this.Processor.EmitGlobalByte((ushort)(length + TextBuffer.Address), 0xd);
					this.Owner.Words[length + 1] = 0xa;
					this.Owner.Words[length + 2] = 0x0;

					this.Owner.Libc.WriteHandle((short)ReplayFile, TextBuffer.Address, (ushort)(length + 2));
				}

				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 312);

				this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 8, Hues.White);

				this.Owner.TextPainter.RenderTextToScreen0(this.Owner.Words.ToString(), 4, 1, Hues.Black);
			}

			this.Processor.Journal.CloseScope("RenderReplayPreamble");
		}

		/// <summary>
		/// The year shown for the round of the replay, as the original computes it: 1750 + 2 x the round up to round 250, after
		/// it 1850 + what is left after 250 and up to three times 50
		/// </summary>
		public ushort ObtainReplayYear(ushort round)
		{
			this.Processor.Journal.OpenScope($"ObtainReplayYear({round})");

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

			this.Processor.Journal.CloseScope("ObtainReplayYear");

			return replayYear;
		}

		/// <summary>
		/// Puts the city into the first free entry (first column 0xffff) of the replay city table (segment 0x3725: three word
		/// columns of 128 entries at 0, 0x100 and 0x200)
		/// </summary>
		public void AssignReplayCityContent(ushort content, ushort horizLoc, ushort vertLoc)
		{
			this.Processor.Journal.OpenScope($"AssignReplayCityContent({content}, {horizLoc}, {vertLoc})");

			this.Processor.SegExtra.Value16 = ReplayCitySegment;

			for (int idx = 0; idx < 0x80; idx++)
			{
				if (PullReplayCity(idx, ReplayCityActiveColumnOffset) != 0xffff)
					continue;

				EmitReplayCity(idx, ReplayCityActiveColumnOffset, content);
				EmitReplayCity(idx, ReplayCityXColumnOffset, horizLoc);
				EmitReplayCity(idx, ReplayCityYColumnOffset, vertLoc);
				break;
			}

			this.Processor.Journal.CloseScope("AssignReplayCityContent");
		}

		/// <summary>
		/// Sets the first column of the first entry of the replay city table whose second and third columns are horizLoc and vertLoc
		/// </summary>
		public void AssignReplayCityContentAlt(ushort content, ushort horizLoc, ushort vertLoc)
		{
			this.Processor.Journal.OpenScope($"AssignReplayCityContentAlt({content}, {horizLoc}, {vertLoc})");

			this.Processor.SegExtra.Value16 = ReplayCitySegment;

			for (int idx = 0; idx < 0x80; idx++)
			{
				if (PullReplayCity(idx, ReplayCityXColumnOffset) != horizLoc || PullReplayCity(idx, ReplayCityYColumnOffset) != vertLoc)
					continue;

				EmitReplayCity(idx, ReplayCityActiveColumnOffset, content);
				break;
			}

			this.Processor.Journal.CloseScope("AssignReplayCityContentAlt");
		}

		/// <summary>
		/// The entry of the replay city table nearest to the position; its distance goes to 0x6812 (999 when none is nearer).
		/// Like the original, without an entry nearer than 999 the result is what was left in the stack frame (local 4), so the
		/// frame stays, and its locals get the values the original leaves there.
		/// </summary>
		/// <returns>Entry, also in AX</returns>
		public ushort ObtainDistanceToReplayCity(int horizLoc, int vertLoc)
		{
			this.Processor.Journal.OpenScope($"ObtainDistanceToReplayCity({horizLoc}, {vertLoc})");

			ushort nearest = 0;

			ReplayCityDistance = 999;
			this.Processor.SegExtra.Value16 = ReplayCitySegment;

			for (int idx = 0; idx < 0x80; idx++)
			{
				if (PullReplayCity(idx, ReplayCityActiveColumnOffset) == 0xffff)
					continue;

				short distance = (short)this.Owner.Helpers.ObtainDistance(horizLoc, (short)PullReplayCity(idx, ReplayCityXColumnOffset),
					vertLoc, (short)PullReplayCity(idx, ReplayCityYColumnOffset));

				if (distance < (short)ReplayCityDistance)
				{
					ReplayCityDistance = (ushort)distance;
					nearest = (ushort)idx;
				}
			}


			this.Processor.Journal.CloseScope("ObtainDistanceToReplayCity");

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
			this.Processor.Journal.OpenScope($"RenderReplayMapTile({horizLoc}, {vertLoc})");


			if (ReplayMode != 4)
			{
				int left = this.Owner.Helpers.VerifyValueSpan(horizLoc - 5, 0, 80);
				int right = this.Owner.Helpers.VerifyValueSpan(horizLoc + 5, 0, 80);
				int top = this.Owner.Helpers.VerifyValueSpan(vertLoc - 5, 2, 49);
				int bottom = this.Owner.Helpers.VerifyValueSpan(vertLoc + 5, 2, 49);

				for (int tileVert = top; tileVert < bottom; tileVert++)
				{
					for (int tileHoriz = left; tileHoriz < right; tileHoriz++)
					{
						if (this.Owner.MapOps.ObtainTerrainType(tileHoriz, tileVert) == MapRules.OceanTerrain)
							continue;

						ushort cityIdx = ObtainDistanceToReplayCity(tileHoriz, tileVert);
						ushort hue = 8;

						if ((short)ReplayCityDistance < 6 &&
							this.Owner.MapOps.ObtainContinentID((short)PullReplayCity(cityIdx, ReplayCityXColumnOffset), (short)PullReplayCity(cityIdx, ReplayCityYColumnOffset)) ==
							this.Owner.MapOps.ObtainContinentID(tileHoriz, tileVert))
						{
							ushort cityOwner = PullReplayCity(cityIdx, ReplayCityActiveColumnOffset);

							hue = ReplayCityDistance == 0 ? (ushort)(cityOwner == 1 ? 7 : 15) :
								PlayerColour(cityOwner);
						}

						this.Owner.Services.FloodBox(ScreenBox, tileHoriz * 4, (tileVert * 4) + 4, 4, 4, hue);
					}
				}
			}


			this.Processor.Journal.CloseScope("RenderReplayMapTile");
		}

		/// <summary>
		/// Writes the map into the replay file (handle 0x680c) as text, a line of 80 characters and CR LF for each of the 50 rows:
		/// " " ocean, "." land, "+" a city ("*" with city walls, improvement bit 1) followed by its name (as far as the line goes)
		/// </summary>
		public void DumpReplayCities()
		{
			this.Processor.Journal.OpenScope("DumpReplayCities()");

			for (int vertLoc = 0; vertLoc < 50; vertLoc++)
			{
				this.Owner.Words[80] = 0xd;
				this.Owner.Words[81] = 0xa;
				this.Owner.Words[82] = 0;

				for (int horizLoc = 0; horizLoc < 80; horizLoc++)
				{
					bool ocean = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain;
					this.Processor.EmitGlobalByte((ushort)(TextBuffer.Address + horizLoc), (byte)(ocean ? 0x20 : 0x2e));
				}

				for (int horizLoc = 0; horizLoc < 80; horizLoc++)
				{
					if (this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain)
						continue;

					for (int labelIdx = 0; labelIdx < 0x100; labelIdx++)
					{
						if ((sbyte)this.Owner.World.TownPlaces[labelIdx].X != horizLoc || (sbyte)this.Owner.World.TownPlaces[labelIdx].Y != vertLoc)
							continue;

						bool walls = (this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0;
						this.Processor.EmitGlobalByte((ushort)(TextBuffer.Address + horizLoc), (byte)(walls ? 0x2a : 0x2b));

						// The name after it: at least its first character, then while the line goes on and the name does
						int place = 0;

						do
						{
							this.Owner.Words[horizLoc + place + 1] = (byte)this.Owner.World.TownLabels[labelIdx][place];
							place++;
						} while (horizLoc + place + 1 < 0x50 && this.Owner.World.TownLabels[labelIdx][place] != '\0');

						break;
					}
				}

				this.Owner.Libc.WriteHandle((short)ReplayFile, TextBuffer.Address, 0x52);
			}

			this.Processor.Journal.CloseScope("DumpReplayCities");
		}

		/// <summary>
		/// The names of the civilization of the player for the replay: its name (leader record 0x1502 + 0x3a * leader) into the
		/// text at 0x1982 + 2 * player, its plural (0x14f2 + ..., or the name with "s" when empty) into the text at 0x1992 + 2 * player.
		/// The plural is made in a buffer in the stack frame, like in the original.
		/// </summary>
		public void ObtainReplayPlayerLabel(short participantIdx)
		{
			this.Processor.Journal.OpenScope($"ObtainReplayPlayerLabel({participantIdx})");

			ushort leader = (ushort)(0x3a * this.Owner.World.Participants[participantIdx].ChiefVisuals);
			ushort civName = LeaderTextAt(leader, LeaderTable.Civilization);
			ushort pluralName = LeaderTextAt(leader, LeaderTable.CivilizationPlural);

			this.Owner.Names.Set(CivilizationName(participantIdx), civName);

			if (this.Processor.PullGlobalByte(pluralName) == 0)
			{
				string plural = this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, civName)) + "s";
				this.Owner.Names.Set(CivilizationPlural(participantIdx), plural);
			}
			else
			{
				this.Owner.Names.Set(CivilizationPlural(participantIdx), pluralName);
			}

			this.Processor.Journal.CloseScope("ObtainReplayPlayerLabel");
		}

		/// <summary>
		/// The end of a conquest victory: the replay of the game (0x680e set meanwhile, the 14 bytes at 0x3772:0x37c4 cleared to
		/// 0xff), the leader's music, "The entire world hails ... the CONQUEROR!" until a key or click, then the map screen
		/// again. The texts have a shadow, in other colours with the colour cycles (0xd762).
		/// The stack frame stays: a called function leaves BP changed, and the original ends with the stack that gives.
		/// </summary>
		public void DisplayConquerorScreen()
		{
			this.Processor.Journal.OpenScope("DisplayConquerorScreen()");

			ReplayAtGameEnd = 0x1;

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.Startup.ConcealMouseCursor();

			FallenLeadersCount = 0;

			for (int idx = 0; idx < 0xe; idx++)
			{
				this.Processor.EmitUInt8(ReplayFallenLeadersSegment, (ushort)(idx + ReplayFallenLeadersTableAddress), 0xff);
			}

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

			DrawingBox.TypefaceIdx = 0x6;

			DisplayReplay();

			ushort leader = (ushort)(0x3a * this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].ChiefVisuals);
			this.Owner.Services.PerformSound(LeaderWordAt(leader, LeaderTable.Tune), 3);

			this.Owner.Services.FloodBox(ScreenBox, 18, 150, 284, 32,
				(ushort)(VgaMode != 0 ? 34 : 7));

			this.Owner.Words.Set("The entire world hails");

			this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 152, 20);
			this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 151, (ushort)(VgaMode != 0 ? 23 : 15));

			Message(LeaderName(this.Owner.World.PersonParticipantIdx))
				.Text(" the CONQUEROR!");

			this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 168, 20);
			this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 167, (ushort)(VgaMode != 0 ? 23 : 15));

			this.Owner.TurnLogic.AwaitForKey();

			this.Owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.Owner.Services.PerformSound(1, 0);

			DrawingBox.TypefaceIdx = 0x1;

			this.Owner.Startup.QueryForDisk(0, 1);

			this.Owner.Startup.DisplayMouseCursor();

			this.Owner.TurnHooks.RepaintMainScreen();

			ReplayAtGameEnd = 0x0;

			this.Processor.Journal.CloseScope("DisplayConquerorScreen");
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
			this.Processor.Journal.OpenScope($"DisplayCivilizationDestroyedScreen({participantIdx}, {year})");

			short leaderIdx = (sbyte)this.Processor.PullGlobalByte((ushort)((ushort)this.Owner.World.Participants[participantIdx].ChiefVisuals + 0x3938));
			char tens = (char)('0' + (leaderIdx / 10));
			char ones = (char)('0' + (leaderIdx % 10));
			string portraitPic = (VgaMode == 0 && (leaderIdx == 0 || leaderIdx == 3))
				? $"kink{tens}{ones}.pic"
				: $"king{tens}{ones}.pic";

			this.Owner.Startup.QueryForDisk((ushort)(((ushort)leaderIdx / 7) + 4), 1);

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, portraitPic, 0);

			ushort portrait = this.Owner.VideoCard.ScreenToPicture(1, 0xb5, 0x43, 0x8b, 0x85);

			ushort speakingMouth = this.Owner.VideoCard.ScreenToPicture(1, 1, 0x33, 0x3b, 0x31);

			ushort closedMouth = this.Owner.VideoCard.ScreenToPicture(1, 1, 0x97, 0x3b, 0x31);

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, GameTexts.FileSlam1Pic, 0);

			if (VgaMode != 0)
			{
				this.Owner.Services.FloodColors(5, 0, 0, 0);
			}

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			string wallName = VgaMode == 0 ? "slag2.pic" : "slam2.pic";

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0x85, wallName, 0);

			RenderWallOfLeaders(ScreenBox);

			short mouthHoriz = 0x5a;
			short mouthVert = 0;
			ushort mouths = (ushort)(leaderIdx << 2);

			mouthHoriz += (short)(this.Processor.PullUInt16(ReplayLeaderFaceSegment, (ushort)(mouths + 0x10)) - 0xb4);
			mouthHoriz -= (short)this.Processor.PullUInt16(ReplayLeaderFaceSegment, (ushort)(mouths + 0x12));
			mouthVert += (short)(this.Processor.PullUInt16(ReplayLeaderFaceSegment, (ushort)(mouths + 0x48)) - 0x40);
			mouthVert -= (short)this.Processor.PullUInt16(ReplayLeaderFaceSegment, (ushort)(mouths + 0x4a));

			this.Owner.Services.RenderPictureToScreen(ScreenBox, 90, 0, portrait);

			this.Owner.Services.RenderPictureToScreen(ScreenBox, mouthHoriz, mouthVert - 2, speakingMouth);

			this.Owner.Services.PerformSound(LeaderWord(this.Owner.World.Participants[participantIdx].ChiefVisuals, LeaderTable.Tune), 3);

			if (VgaMode != 0)
			{
				string portraitPal = $"king{tens}{ones}.pal";

				this.Owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, portraitPal, 0xc1d6);

				this.Owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, GameTexts.FileSlam1Pal, 0xbdee);

				for (ushort colourIdx = 0xc0; colourIdx < 0x1b0; colourIdx++)
				{
					this.Processor.EmitGlobalByte((ushort)(colourIdx + 0xbdf4), this.Processor.PullGlobalByte((ushort)(colourIdx + 0xc1dc)));
				}

				this.Owner.Services.ImportColorsFromBuffer(8, 0xbdee);
			}

			sbyte fallen = (sbyte)FallenLeadersCount;
			short place = (short)(fallen % 7);

			place = (place & 1) != 0 ? (short)-((place / 2) - 6) : (short)(place / 2);

			FallenLeadersCount = (ushort)(fallen + 1);

			if (fallen >= 7)
				place += 7;

			this.Processor.EmitUInt8(ReplayFallenLeadersSegment, (ushort)(place + ReplayFallenLeadersTableAddress), (byte)leaderIdx);

			this.Owner.Services.RestoreTimer();

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, GameTexts.FileSlam1PicB, 0);

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0x85, wallName, 0);

			RenderWallOfLeaders(BackScreenBox);

			while ((short)this.Owner.Timer.Beats < 0xf0)
			{
				this.Owner.Words.Clear();
			}

			this.Owner.Words.Append(this.Owner.Libc.IntegerToText(Math.Abs((short)year), 10));

			this.Owner.Words.Append((short)year < 0 ? " BC" : " AD");

			this.Owner.Words.Append(": ");

			this.Owner.Words.Append(CivilizationPlural((ushort)this.Owner.World.PersonParticipantIdx));

			this.Owner.Words.Append(" destroy");

			RenderShadowedCentredLine(152);

			this.Owner.Words.Set("the ");

			Message(CivilizationName((ushort)participantIdx))
				.Text(" civilization!");

			RenderShadowedCentredLine(168);

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0x85, 0x140, 0x43, BackScreenBox, 0, 0x85);

			this.Owner.Services.RenderPictureToScreen(ScreenBox, mouthHoriz, mouthVert - 2, closedMouth);

			this.Owner.Helpers.ReleaseResource(portrait, 0);

			this.Owner.Services.PerformSound(1, 0);

			this.Owner.TextPainter.AwaitTime(60);

			this.Owner.Services.PerformSound(LeaderWord(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].ChiefVisuals, LeaderTable.Tune), 3);

			this.Owner.VideoCard.RenderScreenToMainScreen(1, 0x1e);

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.Owner.Services.DisplayScreen(0);

			this.Owner.Services.PerformSound(1, 0);

			this.Processor.Journal.CloseScope("DisplayCivilizationDestroyedScreen");
		}

		/// <summary>
		/// One replay: the screen kept, the leaders as at the start (put back at the end), the miniature map (not in mode 4),
		/// all events
		/// </summary>
		private void ReplayGame()
		{
			this.Owner.Startup.ConcealMouseCursor();

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			// No city shown on the replay map yet (segment 0x3725, 128 cities)
			for (int townIdx = 0; townIdx < 0x80; townIdx++)
			{
				EmitReplayCity(townIdx, ReplayCityActiveColumnOffset, 0xffff);
			}

			short[] leaders = new short[8];

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				leaders[participantIdx] = this.Owner.World.Participants[participantIdx].ChiefVisuals;
			}

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (participantIdx == this.Owner.World.PersonParticipantIdx)
					continue;

				bool secondLeader = (this.Owner.World.ParticipantIdentMarkers & (1 << participantIdx)) != 0;

				this.Owner.World.Participants[participantIdx].ChiefVisuals = (short)(secondLeader ? participantIdx + 8 : participantIdx);

				ObtainReplayPlayerLabel(participantIdx);
			}

			if (ReplayMode != 4)
			{
				this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

				for (short vertLoc = 1; vertLoc < 0x31; vertLoc++)
				{
					for (short horizLoc = 0; horizLoc < 0x50; horizLoc++)
					{
						bool ocean = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain;

						this.Owner.Services.FloodBox(ScreenBox, horizLoc * 4, (vertLoc * 4) + 4, 4, 4, (ushort)(ocean ? 1 : 8));
					}
				}
			}

			ushort spot = 0;

			while ((short)spot < this.Owner.World.RerunContentSpan)
			{
				spot = ReplayEvent(spot);

				if (ReplayMode == 2 && this.Owner.Libc.ReadKey() == 0x1b)
					break;
			}

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				this.Owner.World.Participants[participantIdx].ChiefVisuals = leaders[participantIdx];
			}

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (participantIdx != this.Owner.World.PersonParticipantIdx)
				{
					ObtainReplayPlayerLabel(participantIdx);
				}
			}

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

			this.Owner.Startup.DisplayMouseCursor();
		}

		/// <summary>The next byte of the replay</summary>
		private byte ReplayByte(ref ushort spot)
		{
			return this.Owner.World.RerunContent[spot++];
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

					this.Owner.Words.Append(ReplayPlural(pair >> 4));

					this.Owner.Words.Append(kind == 2 ? " declare war on " : " make peace with ");

					this.Owner.Words.Append(ReplayPlural(pair & 0xf));

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

					this.Owner.Words.Append(ReplayPlural(ReplayActor));

					switch (kind)
					{
						case 5:
							this.Owner.Words.Append(" discover ");
							this.Owner.Words.Append(TechnologyText(thing));
							break;

						case 6:
							this.Owner.Words.Append(" produce first ");
							this.Owner.Words.Append(UnitText(thing));
							break;

						case 8:
							this.Owner.Words.Append(" form ");
							this.Owner.Words.Append(GovernmentName(thing));
							break;

						default:
							this.Owner.Words.Append(" build ");

							if ((short)thing <= 7)
							{
								this.Owner.Words.Append("the ");
							}

							this.Owner.Words.Append(WonderText(thing));
							break;
					}

					RenderReplayPreamble();
					break;
				}

				case 11:
				{
					// The status of the human player: cities, population (two parts)
					ReplayActor = (ushort)this.Owner.World.PersonParticipantIdx;

					short towns = ReplayByte(ref spot);
					sbyte populationHigh = (sbyte)ReplayByte(ref spot);
					short populationLow = ReplayByte(ref spot);

					this.Owner.Words.Append(ReplayPlural(this.Owner.World.PersonParticipantIdx));

					this.Owner.Words.Append(": ");

					this.Owner.Words.Append(this.Owner.Libc.IntegerToText(towns, 10));

					this.Owner.Words.Append(" cities; ");

					this.Owner.Helpers.PopulationToText(populationHigh + populationLow);

					this.Owner.Words.Append(" population");

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

					this.Owner.Words.Append(CivilizationName(ReplayActor));

					this.Owner.Words.Append(" civilization destroyed by ");

					this.Owner.Words.Append(ReplayPlural(destroyer));

					if (ReplayMode == 4 && destroyer == this.Owner.World.PersonParticipantIdx)
					{
						DisplayCivilizationDestroyedScreen((short)ReplayActor, (ushort)year);
					}

					this.Owner.World.Participants[ReplayActor].ChiefVisuals ^= 8;

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
				this.Owner.Words.Append(ReplayPlural(ReplayActor));

				if (kind == 1)
				{
					this.Owner.Words.Append(" found the city of ");

					AssignReplayCityContent(ReplayActor, horizLoc, vertLoc);
				}
				else
				{
					this.Owner.Words.Append(" capture ");

					AssignReplayCityContentAlt(ReplayActor, horizLoc, vertLoc);
				}

				AppendReplayCityName(labelIdx);

				RenderReplayPreamble();
			}
			else
			{
				AppendReplayCityName(labelIdx);

				this.Owner.Words.Append(" destroyed");

				RenderReplayPreamble();

				AssignReplayCityContentAlt(0xffff, horizLoc, vertLoc);
			}

			RenderReplayMapTile(horizLoc, vertLoc);

			if (ReplayMode == 1)
			{
				this.Owner.TextPainter.AwaitTime(30);
			}
		}

		/// <summary>The 13 bytes of the city name (TownLabels) after the text at TextBuffer.Address</summary>
		private void AppendReplayCityName(ushort labelIdx)
		{
			ushort end = (ushort)(TextBuffer.Address + this.Owner.Words.Length);

			for (int idx = 0; idx < 0xd; idx++)
			{
				this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(end + idx), (byte)this.Owner.World.TownLabels[labelIdx][idx]);
			}
		}

		/// <summary>
		/// The ranking ("*** 1:x 2:y ..."): 4 bytes of two places each (high and low 4 bits, 0 = none); written only into the
		/// replay file (mode 3)
		/// </summary>
		private void ReplayRanking(ref ushort spot)
		{
			this.Owner.Words.Set("*** ");

			for (short pairIdx = 0; pairIdx < 4; pairIdx++)
			{
				byte places = this.Owner.World.RerunContent[spot];

				if ((places & 0xf0) != 0)
				{
					this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)((pairIdx << 1) + 1), 10));

					this.Owner.Words.Append(":");

					this.Owner.Words.Append(ReplayPlural(places >> 4));

					this.Owner.Words.Append(" ");
				}

				if ((places & 0xf) != 0)
				{
					this.Owner.Words.Append(this.Owner.Libc.IntegerToText((short)((pairIdx << 1) + 2), 10));

					this.Owner.Words.Append(":");

					this.Owner.Words.Append(ReplayPlural(places & 0xf));

					this.Owner.Words.Append(" ");
				}

				spot++;
			}

			if (ReplayMode == 3)
			{
				RenderReplayPreamble();
			}
		}

		private ushort PullReplayCity(int idx, int column)
		{
			return this.Processor.PullUInt16(ReplayCitySegment, (ushort)((idx << 1) + column));
		}

		private void EmitReplayCity(int idx, int column, ushort amount)
		{
			this.Processor.EmitUInt16(ReplayCitySegment, (ushort)((idx << 1) + column), amount);
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
				byte leaderIdx = this.Processor.PullUInt8(ReplayFallenLeadersSegment, (ushort)(placeIdx + ReplayFallenLeadersTableAddress));

				if (leaderIdx == 0xff)
					continue;

				this.Owner.VideoCard.RenderImage(
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
			this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, vertLoc, 20);

			this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, vertLoc - 1, VgaMode != 0 ? (ushort)23 : (ushort)15);
		}
	}
}
