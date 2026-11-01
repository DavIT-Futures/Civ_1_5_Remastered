using System;

namespace Civilization1Remaster.Core.UI
{
	public class ReplayViewer : GameComponent, IReplayViewer
	{
		private const ushort ReplayCitySegment = 0x3725;
		private const ushort ReplayCityActiveColumnOffset = 0x0;
		private const ushort ReplayCityXColumnOffset = 0x100;
		private const ushort ReplayCityYColumnOffset = 0x200;
		private const ushort ReplayFallenLeadersSegment = 0x3772;
		private const ushort ReplayFallenLeadersTableAddress = 0x37c4;
		private const ushort ReplayLeaderFaceSegment = 0x36fa;

		public ReplayViewer(GameEngine owner)
			: base(owner)
		{
		}

		public void ShowReplay()
		{
			using var _ = this.Processor.Journal.Scope("DisplayReplay()");

			while (true)
			{
				if (Replay.AtGameEnd == 0)
				{
					Replay.Mode = this.Owner.TurnHooks.ShowDialog(GameTexts.TextSeeReplayNoReplayQuickReplayCompleteRepl, 100, 80);
				}
				else
				{
					Replay.Mode = 0x4;
				}

				if ((short)Replay.Mode <= 0)
				{
					break;
				}

				if (Replay.Mode == 3)
				{
					ushort handle = (ushort)this.Owner.Libc.OpenHandle("replay.txt", 0x8301, 0x80);

					Replay.File = handle;

					if (handle == 0xffff)
					{
						Message("File Error\n")
							.Dialog(100, 80);

						break;
					}

					DumpReplayCities();
				}

				ReplayGame();

				if (Replay.Mode == 3)
				{
					this.Owner.Libc.CloseHandle((short)Replay.File);
				}

				if ((short)Replay.Mode >= 3)
				{
					break;
				}
			}
		}

		public void DrawReplayPreamble()
		{
			using var _ = this.Processor.Journal.Scope("RenderReplayPreamble()");

			if (Replay.Mode != 4)
			{
				if (Replay.Actor == (ushort)this.Owner.World.PersonParticipantIdx)
				{
					this.Owner.Words.Uppercase();
				}

				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 8, Hues.White);

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 4, 1, Hues.Black);

				if (Replay.Mode == 3)
				{
					ushort length = this.Owner.Words.Length;

					this.Processor.EmitGlobalByte((ushort)(length + TextBuffer.Address), 0xd);
					this.Owner.Words[length + 1] = 0xa;
					this.Owner.Words[length + 2] = 0x0;

					this.Owner.Libc.WriteHandle((short)Replay.File, TextBuffer.Address, (ushort)(length + 2));
				}

				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 312);

				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 8, Hues.White);

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 4, 1, Hues.Black);
			}
		}

		public ushort ObtainReplayYear(ushort round)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainReplayYear({round})");

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

			return replayYear;
		}

		public void AssignReplayCityContent(ushort content, ushort horizLoc, ushort vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"AssignReplayCityContent({content}, {horizLoc}, {vertLoc})");

			this.Processor.SegExtra.Value16 = ReplayCitySegment;

			for (int idx = 0; idx < 0x80; idx++)
			{
				if (PullReplayCity(idx, ReplayCityActiveColumnOffset) != 0xffff)
				{
					continue;
				}

				EmitReplayCity(idx, ReplayCityActiveColumnOffset, content);
				EmitReplayCity(idx, ReplayCityXColumnOffset, horizLoc);
				EmitReplayCity(idx, ReplayCityYColumnOffset, vertLoc);
				break;
			}
		}

		public void AssignReplayCityContentAlt(ushort content, ushort horizLoc, ushort vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"AssignReplayCityContentAlt({content}, {horizLoc}, {vertLoc})");

			this.Processor.SegExtra.Value16 = ReplayCitySegment;

			for (int idx = 0; idx < 0x80; idx++)
			{
				if (PullReplayCity(idx, ReplayCityXColumnOffset) != horizLoc || PullReplayCity(idx, ReplayCityYColumnOffset) != vertLoc)
				{
					continue;
				}

				EmitReplayCity(idx, ReplayCityActiveColumnOffset, content);
				break;
			}
		}

		public ushort ComputeDistanceToReplayCity(int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainDistanceToReplayCity({horizLoc}, {vertLoc})");

			ushort nearest = 0;

			Replay.CityDistance = 999;
			this.Processor.SegExtra.Value16 = ReplayCitySegment;

			for (int idx = 0; idx < 0x80; idx++)
			{
				if (PullReplayCity(idx, ReplayCityActiveColumnOffset) == 0xffff)
				{
					continue;
				}

				short distance = (short)this.Owner.Helpers.ObtainDistance(horizLoc, (short)PullReplayCity(idx, ReplayCityXColumnOffset),
					vertLoc, (short)PullReplayCity(idx, ReplayCityYColumnOffset));

				if (distance < (short)Replay.CityDistance)
				{
					Replay.CityDistance = (ushort)distance;
					nearest = (ushort)idx;
				}
			}

			return nearest;
		}

		public void DrawReplayMapTile(int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"RenderReplayMapTile({horizLoc}, {vertLoc})");

			if (Replay.Mode != 4)
			{
				int left = this.Owner.Helpers.ClampValue(horizLoc - 5, 0, 80);
				int right = this.Owner.Helpers.ClampValue(horizLoc + 5, 0, 80);
				int top = this.Owner.Helpers.ClampValue(vertLoc - 5, 2, 49);
				int bottom = this.Owner.Helpers.ClampValue(vertLoc + 5, 2, 49);

				for (int tileVert = top; tileVert < bottom; tileVert++)
				{
					for (int tileHoriz = left; tileHoriz < right; tileHoriz++)
					{
						if (this.Owner.MapOps.ObtainTerrainType(tileHoriz, tileVert) == MapRules.OceanTerrain)
						{
							continue;
						}

						ushort cityIdx = ComputeDistanceToReplayCity(tileHoriz, tileVert);
						ushort hue = 8;

						if ((short)Replay.CityDistance < 6 &&
							this.Owner.MapOps.ObtainContinentID((short)PullReplayCity(cityIdx, ReplayCityXColumnOffset), (short)PullReplayCity(cityIdx, ReplayCityYColumnOffset)) ==
							this.Owner.MapOps.ObtainContinentID(tileHoriz, tileVert))
						{
							ushort cityOwner = PullReplayCity(cityIdx, ReplayCityActiveColumnOffset);

							hue = Replay.CityDistance == 0 ? (ushort)(cityOwner == 1 ? 7 : 15) : Tables.PlayerColour(cityOwner);
						}

						this.Owner.Services.FloodBox(Screen.Box, tileHoriz * 4, (tileVert * 4) + 4, 4, 4, hue);
					}
				}
			}
		}

		public void DumpReplayCities()
		{
			using var _ = this.Processor.Journal.Scope("DumpReplayCities()");

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
					{
						continue;
					}

					for (int labelIdx = 0; labelIdx < 0x100; labelIdx++)
					{
						if ((sbyte)this.Owner.World.TownPlaces[labelIdx].X != horizLoc || (sbyte)this.Owner.World.TownPlaces[labelIdx].Y != vertLoc)
						{
							continue;
						}

						bool walls = (this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0;
						this.Processor.EmitGlobalByte((ushort)(TextBuffer.Address + horizLoc), (byte)(walls ? 0x2a : 0x2b));

						int place = 0;

						do
						{
							this.Owner.Words[horizLoc + place + 1] = (byte)this.Owner.World.TownLabels[labelIdx][place];
							place++;
						}
						while (horizLoc + place + 1 < 0x50 && this.Owner.World.TownLabels[labelIdx][place] != '\0');

						break;
					}
				}

				this.Owner.Libc.WriteHandle((short)Replay.File, TextBuffer.Address, 0x52);
			}
		}

		public void ObtainReplayPlayerLabel(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainReplayPlayerLabel({participantIdx})");

			ushort leader = (ushort)(0x3a * this.Owner.World.Participants[participantIdx].ChiefVisuals);
			ushort civName = GameTables.LeaderTextAt(leader, LeaderTable.Civilization);
			ushort pluralName = GameTables.LeaderTextAt(leader, LeaderTable.CivilizationPlural);

			this.Owner.Names.Set(Tables.CivilizationName(participantIdx), civName);

			if (this.Processor.PullGlobalByte(pluralName) == 0)
			{
				string plural = this.Processor.PullText(CPU.AsLinearAddress(this.Processor.SegData.Value16, civName)) + "s";
				this.Owner.Names.Set(Tables.CivilizationPlural(participantIdx), plural);
			}
			else
			{
				this.Owner.Names.Set(Tables.CivilizationPlural(participantIdx), pluralName);
			}
		}

		public void ShowConquerorScreen()
		{
			using var _ = this.Processor.Journal.Scope("DisplayConquerorScreen()");

			Replay.AtGameEnd = 0x1;

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.Startup.HideMouseCursor();

			Scoring.FallenLeadersCount = 0;

			for (int idx = 0; idx < 0xe; idx++)
			{
				this.Processor.EmitUInt8(ReplayFallenLeadersSegment, (ushort)(idx + ReplayFallenLeadersTableAddress), 0xff);
			}

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			DrawingBox.TypefaceIdx = 0x6;

			ShowReplay();

			ushort leader = (ushort)(0x3a * this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].ChiefVisuals);
			this.Owner.Services.PerformSound(Tables.LeaderWordAt(leader, LeaderTable.Tune), 3);

			this.Owner.Services.FloodBox(Screen.Box, 18, 150, 284, 32,
				(ushort)(Screen.VgaMode != 0 ? 34 : 7));

			this.Owner.Words.Set("The entire world hails");

			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 152, 20);
			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 151, (ushort)(Screen.VgaMode != 0 ? 23 : 15));

			Message(Tables.LeaderName(this.Owner.World.PersonParticipantIdx))
				.Text(" the CONQUEROR!");

			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 168, 20);
			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 167, (ushort)(Screen.VgaMode != 0 ? 23 : 15));

			this.Owner.TurnLogic.WaitForKey();

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.Services.PerformSound(1, 0);

			DrawingBox.TypefaceIdx = 0x1;

			this.Owner.Startup.AskForDisk(0, 1);

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.TurnHooks.RepaintMainScreen();

			Replay.AtGameEnd = 0x0;
		}

		public void ShowCivilizationDestroyedScreen(short participantIdx, ushort year)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayCivilizationDestroyedScreen({participantIdx}, {year})");

			short leaderIdx = (sbyte)this.Processor.PullGlobalByte((ushort)((ushort)this.Owner.World.Participants[participantIdx].ChiefVisuals + 0x3938));
			char tens = (char)('0' + (leaderIdx / 10));
			char ones = (char)('0' + (leaderIdx % 10));
			string portraitPic = (Screen.VgaMode == 0 && (leaderIdx == 0 || leaderIdx == 3))
				? $"kink{tens}{ones}.pic"
				: $"king{tens}{ones}.pic";

			this.Owner.Startup.AskForDisk((ushort)(((ushort)leaderIdx / 7) + 4), 1);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, portraitPic, 0);

			ushort portrait = this.Owner.VideoCard.ScreenToPicture(1, 0xb5, 0x43, 0x8b, 0x85);

			ushort speakingMouth = this.Owner.VideoCard.ScreenToPicture(1, 1, 0x33, 0x3b, 0x31);

			ushort closedMouth = this.Owner.VideoCard.ScreenToPicture(1, 1, 0x97, 0x3b, 0x31);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileSlam1Pic, 0);

			if (Screen.VgaMode != 0)
			{
				this.Owner.Services.FillColors(5, 0, 0, 0);
			}

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

			string wallName = Screen.VgaMode == 0 ? "slag2.pic" : "slam2.pic";

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0x85, wallName, 0);

			DrawWallOfLeaders(Screen.Box);

			short mouthHoriz = 0x5a;
			short mouthVert = 0;
			ushort mouths = (ushort)(leaderIdx << 2);

			mouthHoriz += (short)(this.Processor.PullUInt16(ReplayLeaderFaceSegment, (ushort)(mouths + 0x10)) - 0xb4);
			mouthHoriz -= (short)this.Processor.PullUInt16(ReplayLeaderFaceSegment, (ushort)(mouths + 0x12));
			mouthVert += (short)(this.Processor.PullUInt16(ReplayLeaderFaceSegment, (ushort)(mouths + 0x48)) - 0x40);
			mouthVert -= (short)this.Processor.PullUInt16(ReplayLeaderFaceSegment, (ushort)(mouths + 0x4a));

			this.Owner.Services.DrawPictureToScreen(Screen.Box, 90, 0, portrait);

			this.Owner.Services.DrawPictureToScreen(Screen.Box, mouthHoriz, mouthVert - 2, speakingMouth);

			this.Owner.Services.PerformSound(Tables.LeaderWord(this.Owner.World.Participants[participantIdx].ChiefVisuals, LeaderTable.Tune), 3);

			if (Screen.VgaMode != 0)
			{
				string portraitPal = $"king{tens}{ones}.pal";

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, portraitPal, 0xc1d6);

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileSlam1Pal, 0xbdee);

				for (ushort colourIdx = 0xc0; colourIdx < 0x1b0; colourIdx++)
				{
					this.Processor.EmitGlobalByte((ushort)(colourIdx + 0xbdf4), this.Processor.PullGlobalByte((ushort)(colourIdx + 0xc1dc)));
				}

				this.Owner.Services.LoadColorsFromBuffer(8, 0xbdee);
			}

			sbyte fallen = (sbyte)Scoring.FallenLeadersCount;
			short place = (short)(fallen % 7);

			place = (place & 1) != 0 ? (short)-((place / 2) - 6) : (short)(place / 2);

			Scoring.FallenLeadersCount = (ushort)(fallen + 1);

			if (fallen >= 7)
			{
				place += 7;
			}

			this.Processor.EmitUInt8(ReplayFallenLeadersSegment, (ushort)(place + ReplayFallenLeadersTableAddress), (byte)leaderIdx);

			this.Owner.Services.RestoreTimer();

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileSlam1PicB, 0);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0x85, wallName, 0);

			DrawWallOfLeaders(Screen.BackScreenBox);

			while ((short)this.Owner.Timer.Beats < 0xf0)
			{
				this.Owner.Words.Clear();
			}

			this.Owner.Words.Append(this.Owner.Libc.IntegerToText(Math.Abs((short)year), 10));

			this.Owner.Words.Append((short)year < 0 ? " BC" : " AD");

			this.Owner.Words.Append(": ");

			this.Owner.Words.Append(Tables.CivilizationPlural((ushort)this.Owner.World.PersonParticipantIdx));

			this.Owner.Words.Append(" destroy");

			DrawShadowedCentredLine(152);

			this.Owner.Words.Set("the ");

			Message(Tables.CivilizationName((ushort)participantIdx))
				.Text(" civilization!");

			DrawShadowedCentredLine(168);

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0x85, 0x140, 0x43, Screen.BackScreenBox, 0, 0x85);

			this.Owner.Services.DrawPictureToScreen(Screen.Box, mouthHoriz, mouthVert - 2, closedMouth);

			this.Owner.Helpers.ReleaseResource(portrait, 0);

			this.Owner.Services.PerformSound(1, 0);

			this.Owner.TextPainter.WaitForTime(60);

			this.Owner.Services.PerformSound(Tables.LeaderWord(this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].ChiefVisuals, LeaderTable.Tune), 3);

			this.Owner.VideoCard.DrawScreenToMainScreen(1);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

			this.Owner.Services.ShowScreen(0);

			this.Owner.Services.PerformSound(1, 0);
		}

		private void ReplayGame()
		{
			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

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
				{
					continue;
				}

				bool secondLeader = (this.Owner.World.ParticipantIdentMarkers & (1 << participantIdx)) != 0;

				this.Owner.World.Participants[participantIdx].ChiefVisuals = (short)(secondLeader ? participantIdx + 8 : participantIdx);

				ObtainReplayPlayerLabel(participantIdx);
			}

			if (Replay.Mode != 4)
			{
				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

				for (short vertLoc = 1; vertLoc < 0x31; vertLoc++)
				{
					for (short horizLoc = 0; horizLoc < 0x50; horizLoc++)
					{
						bool ocean = this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain;

						this.Owner.Services.FloodBox(Screen.Box, horizLoc * 4, (vertLoc * 4) + 4, 4, 4, (ushort)(ocean ? 1 : 8));
					}
				}
			}

			ushort spot = 0;

			while ((short)spot < this.Owner.World.RerunContentSpan)
			{
				spot = ReplayEvent(spot);

				if (Replay.Mode == 2 && this.Owner.Libc.ReadKey() == 0x1b)
				{
					break;
				}
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

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		private byte ReplayByte(ref ushort spot)
		{
			return this.Owner.World.RerunContent[spot++];
		}

		private ushort ReplayPlural(int participantIdx)
		{
			return Tables.CivilizationPlural(participantIdx);
		}

		private ushort ReplayEvent(ushort spot)
		{
			byte head = ReplayByte(ref spot);
			ushort kind = (ushort)(head >> 4);
			ushort turn = (ushort)(((head & 0xf) << 8) + ReplayByte(ref spot));

			short year = (short)ObtainReplayYear(turn);

			Replay.Actor = 0xffff;
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

					DrawReplayPreamble();
					break;
				}

				case 5:
				case 6:
				case 8:
				case 10:
				{
					ushort actor = ReplayByte(ref spot);

					Replay.Actor = actor;

					ushort thing = ReplayByte(ref spot);

					this.Owner.Words.Append(ReplayPlural(Replay.Actor));

					switch (kind)
					{
						case 5:
							this.Owner.Words.Append(" discover ");
							this.Owner.Words.Append(GameTables.TechnologyText(thing));
							break;

						case 6:
							this.Owner.Words.Append(" produce first ");
							this.Owner.Words.Append(GameTables.UnitText(thing));
							break;

						case 8:
							this.Owner.Words.Append(" form ");
							this.Owner.Words.Append(Tables.GovernmentName(thing));
							break;

						default:
							this.Owner.Words.Append(" build ");

							if ((short)thing <= 7)
							{
								this.Owner.Words.Append("the ");
							}

							this.Owner.Words.Append(GameTables.WonderText(thing));
							break;
					}

					DrawReplayPreamble();
					break;
				}

				case 11:
				{
					Replay.Actor = (ushort)this.Owner.World.PersonParticipantIdx;

					short towns = ReplayByte(ref spot);
					sbyte populationHigh = (sbyte)ReplayByte(ref spot);
					short populationLow = ReplayByte(ref spot);

					this.Owner.Words.Append(ReplayPlural(this.Owner.World.PersonParticipantIdx));

					this.Owner.Words.Append(": ");

					this.Owner.Words.Append(this.Owner.Libc.IntegerToText(towns, 10));

					this.Owner.Words.Append(" cities; ");

					this.Owner.Helpers.FormatPopulation(populationHigh + populationLow);

					this.Owner.Words.Append(" population");

					DrawReplayPreamble();
					break;
				}

				case 12:
					ReplayRanking(ref spot);
					break;

				case 13:
				{
					ushort fallen = ReplayByte(ref spot);

					Replay.Actor = fallen;

					short destroyer = ReplayByte(ref spot);

					this.Owner.Words.Append(Tables.CivilizationName(Replay.Actor));

					this.Owner.Words.Append(" civilization destroyed by ");

					this.Owner.Words.Append(ReplayPlural(destroyer));

					if (Replay.Mode == 4 && destroyer == this.Owner.World.PersonParticipantIdx)
					{
						ShowCivilizationDestroyedScreen((short)Replay.Actor, (ushort)year);
					}

					this.Owner.World.Participants[Replay.Actor].ChiefVisuals ^= 8;

					ObtainReplayPlayerLabel((short)Replay.Actor);

					DrawReplayPreamble();
					break;
				}
			}

			return spot;
		}

		private void ReplayCityEvent(ref ushort spot, ushort kind)
		{
			ushort actor = ReplayByte(ref spot);

			Replay.Actor = actor;

			ushort labelIdx = ReplayByte(ref spot);
			ushort horizLoc = ReplayByte(ref spot);
			ushort vertLoc = ReplayByte(ref spot);

			if (Replay.Actor != 0xff)
			{
				this.Owner.Words.Append(ReplayPlural(Replay.Actor));

				if (kind == 1)
				{
					this.Owner.Words.Append(" found the city of ");

					AssignReplayCityContent(Replay.Actor, horizLoc, vertLoc);
				}
				else
				{
					this.Owner.Words.Append(" capture ");

					AssignReplayCityContentAlt(Replay.Actor, horizLoc, vertLoc);
				}

				AppendReplayCityName(labelIdx);

				DrawReplayPreamble();
			}
			else
			{
				AppendReplayCityName(labelIdx);

				this.Owner.Words.Append(" destroyed");

				DrawReplayPreamble();

				AssignReplayCityContentAlt(0xffff, horizLoc, vertLoc);
			}

			DrawReplayMapTile(horizLoc, vertLoc);

			if (Replay.Mode == 1)
			{
				this.Owner.TextPainter.WaitForTime(30);
			}
		}

		private void AppendReplayCityName(ushort labelIdx)
		{
			ushort end = (ushort)(TextBuffer.Address + this.Owner.Words.Length);

			for (int idx = 0; idx < 0xd; idx++)
			{
				this.Processor.EmitUInt8(this.Processor.SegData.Value16, (ushort)(end + idx), (byte)this.Owner.World.TownLabels[labelIdx][idx]);
			}
		}

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

			if (Replay.Mode == 3)
			{
				DrawReplayPreamble();
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

		private void DrawWallOfLeaders(ushort screen)
		{
			ushort placeIdx;

			for (placeIdx = 0; (short)placeIdx < 0xe; placeIdx++)
			{
				byte leaderIdx = this.Processor.PullUInt8(ReplayFallenLeadersSegment, (ushort)(placeIdx + ReplayFallenLeadersTableAddress));

				if (leaderIdx == 0xff)
				{
					continue;
				}

				this.Owner.VideoCard.RenderImage(
					Screen.BackScreenBox,
					(ushort)((((ushort)leaderIdx % 7) * 0x1c) + 1),
					(ushort)(((ushort)leaderIdx / 7 * 0x22) + 0x85),
					0x1b, 0x21,
					screen,
					(ushort)(((placeIdx % 7) * 0x2e) + 8),
					(ushort)((-(placeIdx / 7) * 0x2a) + 0x31));
			}
		}

		private void DrawShadowedCentredLine(int vertLoc)
		{
			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, vertLoc, 20);

			this.Owner.TextPainter.DrawCenteredTextToScreen0(this.Owner.Words.ToString(), 160, vertLoc - 1, Screen.VgaMode != 0 ? (ushort)23 : (ushort)15);
		}
	}
}
