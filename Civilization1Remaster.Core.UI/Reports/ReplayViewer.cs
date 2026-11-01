using System;

namespace Civilization1Remaster.Core.UI.Reports
{
	public class ReplayViewer : GameComponent, IReplayViewer
	{
		private const ushort ReplayCitySegment = 0x3725;
		private const ushort ReplayCityActiveColumnOffset = 0x0;
		private const ushort ReplayCityHorizontalColumnOffset = 0x100;
		private const ushort ReplayCityVerticalColumnOffset = 0x200;
		private const ushort ReplayFallenLeadersSegment = 0x3772;
		private const ushort ReplayFallenLeadersTableAddress = 0x37c4;
		private const ushort ReplayLeaderFaceSegment = 0x36fa;

		public ReplayViewer(IGameEngine owner)
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
					Replay.Mode = this.Owner.TurnHooks.ShowDialog(GameTexts.TextSeeReplayNoReplayQuickReplayCompleteReplay, 100, 80);
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
					ushort handle = (ushort)this.Owner.Runtime.OpenHandle("replay.txt", 0x8301, 0x80);

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
					this.Owner.Runtime.CloseHandle((short)Replay.File);
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
				if (Replay.Actor == (ushort)this.Owner.World.PersonParticipantIndex)
				{
					this.Owner.Words.Uppercase();
				}

				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 8, Hues.White);

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 4, 1, Hues.Black);

				if (Replay.Mode == 3)
				{
					ushort length = this.Owner.Words.Length;

					this.Processor.EmitGlobalByte((ushort)(length + TextBuffer.Address), 0xd);
					this.Owner.Words[length + 1] = 0xa;
					this.Owner.Words[length + 2] = 0x0;

					this.Owner.Runtime.WriteHandle((short)Replay.File, TextBuffer.Address, (ushort)(length + 2));
				}

				this.Owner.BlockText.ShrinkTextToWidth(TextBuffer.Address, 312);

				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 8, Hues.White);

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 4, 1, Hues.Black);
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

		public void AssignReplayCityContent(ushort content, ushort horizontalLocation, ushort verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"AssignReplayCityContent({content}, {horizontalLocation}, {verticalLocation})");

			this.Processor.SegmentExtra.Word = ReplayCitySegment;

			for (int index = 0; index < 0x80; index++)
			{
				if (PullReplayCity(index, ReplayCityActiveColumnOffset) != 0xffff)
				{
					continue;
				}

				EmitReplayCity(index, ReplayCityActiveColumnOffset, content);
				EmitReplayCity(index, ReplayCityHorizontalColumnOffset, horizontalLocation);
				EmitReplayCity(index, ReplayCityVerticalColumnOffset, verticalLocation);
				break;
			}
		}

		public void AssignReplayCityContentAlternate(ushort content, ushort horizontalLocation, ushort verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"AssignReplayCityContentAlt({content}, {horizontalLocation}, {verticalLocation})");

			this.Processor.SegmentExtra.Word = ReplayCitySegment;

			for (int index = 0; index < 0x80; index++)
			{
				if (PullReplayCity(index, ReplayCityHorizontalColumnOffset) != horizontalLocation || PullReplayCity(index, ReplayCityVerticalColumnOffset) != verticalLocation)
				{
					continue;
				}

				EmitReplayCity(index, ReplayCityActiveColumnOffset, content);
				break;
			}
		}

		public ushort ComputeDistanceToReplayCity(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainDistanceToReplayCity({horizontalLocation}, {verticalLocation})");

			ushort nearest = 0;

			Replay.CityDistance = 999;
			this.Processor.SegmentExtra.Word = ReplayCitySegment;

			for (int index = 0; index < 0x80; index++)
			{
				if (PullReplayCity(index, ReplayCityActiveColumnOffset) == 0xffff)
				{
					continue;
				}

				short distance = (short)this.Owner.Helpers.ObtainDistance(horizontalLocation, (short)PullReplayCity(index, ReplayCityHorizontalColumnOffset),
					verticalLocation, (short)PullReplayCity(index, ReplayCityVerticalColumnOffset));

				if (distance < (short)Replay.CityDistance)
				{
					Replay.CityDistance = (ushort)distance;
					nearest = (ushort)index;
				}
			}

			return nearest;
		}

		public void DrawReplayMapTile(int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"RenderReplayMapTile({horizontalLocation}, {verticalLocation})");

			if (Replay.Mode != 4)
			{
				int left = this.Owner.Helpers.ClampValue(horizontalLocation - 5, 0, 80);
				int right = this.Owner.Helpers.ClampValue(horizontalLocation + 5, 0, 80);
				int top = this.Owner.Helpers.ClampValue(verticalLocation - 5, 2, 49);
				int bottom = this.Owner.Helpers.ClampValue(verticalLocation + 5, 2, 49);

				for (int tileVertical = top; tileVertical < bottom; tileVertical++)
				{
					for (int tileHorizontal = left; tileHorizontal < right; tileHorizontal++)
					{
						if (this.Owner.MapOperations.ObtainTerrainType(tileHorizontal, tileVertical) == MapRules.OceanTerrain)
						{
							continue;
						}

						ushort cityIndex = ComputeDistanceToReplayCity(tileHorizontal, tileVertical);
						ushort hue = 8;

						if ((short)Replay.CityDistance < 6 &&
							this.Owner.MapOperations.ObtainContinentIdentifier((short)PullReplayCity(cityIndex, ReplayCityHorizontalColumnOffset), (short)PullReplayCity(cityIndex, ReplayCityVerticalColumnOffset)) ==
							this.Owner.MapOperations.ObtainContinentIdentifier(tileHorizontal, tileVertical))
						{
							ushort cityOwner = PullReplayCity(cityIndex, ReplayCityActiveColumnOffset);

							hue = Replay.CityDistance == 0 ? (ushort)(cityOwner == 1 ? 7 : 15) : Tables.PlayerColour(cityOwner);
						}

						this.Owner.Services.FloodBox(Screen.Box, tileHorizontal * 4, (tileVertical * 4) + 4, 4, 4, hue);
					}
				}
			}
		}

		public void DumpReplayCities()
		{
			using var _ = this.Processor.Journal.Scope("DumpReplayCities()");

			for (int verticalLocation = 0; verticalLocation < 50; verticalLocation++)
			{
				this.Owner.Words[80] = 0xd;
				this.Owner.Words[81] = 0xa;
				this.Owner.Words[82] = 0;

				for (int horizontalLocation = 0; horizontalLocation < 80; horizontalLocation++)
				{
					bool ocean = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == MapRules.OceanTerrain;
					this.Processor.EmitGlobalByte((ushort)(TextBuffer.Address + horizontalLocation), (byte)(ocean ? 0x20 : 0x2e));
				}

				for (int horizontalLocation = 0; horizontalLocation < 80; horizontalLocation++)
				{
					if (this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == MapRules.OceanTerrain)
					{
						continue;
					}

					for (int labelIndex = 0; labelIndex < 0x100; labelIndex++)
					{
						if ((sbyte)this.Owner.World.TownPlaces[labelIndex].X != horizontalLocation || (sbyte)this.Owner.World.TownPlaces[labelIndex].Y != verticalLocation)
						{
							continue;
						}

						bool walls = (this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) != 0;
						this.Processor.EmitGlobalByte((ushort)(TextBuffer.Address + horizontalLocation), (byte)(walls ? 0x2a : 0x2b));

						int place = 0;

						do
						{
							this.Owner.Words[horizontalLocation + place + 1] = (byte)this.Owner.World.TownLabels[labelIndex][place];
							place++;
						}
						while (horizontalLocation + place + 1 < 0x50 && this.Owner.World.TownLabels[labelIndex][place] != '\0');

						break;
					}
				}

				this.Owner.Runtime.WriteHandle((short)Replay.File, TextBuffer.Address, 0x52);
			}
		}

		public void ObtainReplayPlayerLabel(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainReplayPlayerLabel({participantIndex})");

			ushort leader = (ushort)(0x3a * this.Owner.World.Participants[participantIndex].ChiefVisuals);
			ushort civilizationName = GameTables.LeaderTextAt(leader, LeaderTable.Civilization);
			ushort pluralName = GameTables.LeaderTextAt(leader, LeaderTable.CivilizationPlural);

			this.Owner.Names.Set(Tables.CivilizationName(participantIndex), civilizationName);

			if (this.Processor.PullGlobalByte(pluralName) == 0)
			{
				string plural = this.Processor.PullText(CentralProcessor.AsLinearAddress(this.Processor.SegmentData.Word, civilizationName)) + "s";
				this.Owner.Names.Set(Tables.CivilizationPlural(participantIndex), plural);
			}
			else
			{
				this.Owner.Names.Set(Tables.CivilizationPlural(participantIndex), pluralName);
			}
		}

		public void ShowConquerorScreen()
		{
			using var _ = this.Processor.Journal.Scope("DisplayConquerorScreen()");

			Replay.AtGameEnd = 0x1;

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.Startup.HideMouseCursor();

			Scoring.FallenLeadersCount = 0;

			for (int index = 0; index < 0xe; index++)
			{
				this.Processor.EmitByte(ReplayFallenLeadersSegment, (ushort)(index + ReplayFallenLeadersTableAddress), 0xff);
			}

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			DrawingBox.TypefaceIndex = 0x6;

			ShowReplay();

			ushort leader = (ushort)(0x3a * this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].ChiefVisuals);
			this.Owner.Services.PerformSound(Tables.LeaderWordAt(leader, LeaderTable.Tune), 3);

			this.Owner.Services.FloodBox(Screen.Box, 18, 150, 284, 32,
				(ushort)(Screen.VideoGraphicsMode != 0 ? 34 : 7));

			this.Owner.Words.Set("The entire world hails");

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 152, 20);
			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 151, (ushort)(Screen.VideoGraphicsMode != 0 ? 23 : 15));

			Message(Tables.LeaderName(this.Owner.World.PersonParticipantIndex))
				.Text(" the CONQUEROR!");

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 168, 20);
			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 167, (ushort)(Screen.VideoGraphicsMode != 0 ? 23 : 15));

			this.Owner.TurnLogic.WaitForKey();

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			this.Owner.Services.PerformSound(1, 0);

			DrawingBox.TypefaceIndex = 0x1;

			this.Owner.Startup.AskForDisk(0, 1);

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.TurnHooks.RepaintMainScreen();

			Replay.AtGameEnd = 0x0;
		}

		public void ShowCivilizationDestroyedScreen(short participantIndex, ushort year)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayCivilizationDestroyedScreen({participantIndex}, {year})");

			short leaderIndex = (sbyte)this.Processor.PullGlobalByte((ushort)((ushort)this.Owner.World.Participants[participantIndex].ChiefVisuals + 0x3938));
			char tens = (char)('0' + (leaderIndex / 10));
			char ones = (char)('0' + (leaderIndex % 10));
			string portraitPicture = (Screen.VideoGraphicsMode == 0 && (leaderIndex == 0 || leaderIndex == 3))
				? $"kink{tens}{ones}.pic"
				: $"king{tens}{ones}.pic";

			this.Owner.Startup.AskForDisk((ushort)(((ushort)leaderIndex / 7) + 4), 1);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, portraitPicture, 0);

			ushort portrait = this.Owner.VideoCard.ScreenToPicture(1, 0xb5, 0x43, 0x8b, 0x85);

			ushort speakingMouth = this.Owner.VideoCard.ScreenToPicture(1, 1, 0x33, 0x3b, 0x31);

			ushort closedMouth = this.Owner.VideoCard.ScreenToPicture(1, 1, 0x97, 0x3b, 0x31);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileSlamPicture, 0);

			if (Screen.VideoGraphicsMode != 0)
			{
				this.Owner.Services.FillColors(5, 0, 0, 0);
			}

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

			string wallName = Screen.VideoGraphicsMode == 0 ? "slag2.pic" : "slam2.pic";

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0x85, wallName, 0);

			DrawWallOfLeaders(Screen.Box);

			short mouthHorizontal = 0x5a;
			short mouthVertical = 0;
			ushort mouths = (ushort)(leaderIndex << 2);

			mouthHorizontal += (short)(this.Processor.PullWord(ReplayLeaderFaceSegment, (ushort)(mouths + 0x10)) - 0xb4);
			mouthHorizontal -= (short)this.Processor.PullWord(ReplayLeaderFaceSegment, (ushort)(mouths + 0x12));
			mouthVertical += (short)(this.Processor.PullWord(ReplayLeaderFaceSegment, (ushort)(mouths + 0x48)) - 0x40);
			mouthVertical -= (short)this.Processor.PullWord(ReplayLeaderFaceSegment, (ushort)(mouths + 0x4a));

			this.Owner.Services.DrawPictureToScreen(Screen.Box, 90, 0, portrait);

			this.Owner.Services.DrawPictureToScreen(Screen.Box, mouthHorizontal, mouthVertical - 2, speakingMouth);

			this.Owner.Services.PerformSound(Tables.LeaderWord(this.Owner.World.Participants[participantIndex].ChiefVisuals, LeaderTable.Tune), 3);

			if (Screen.VideoGraphicsMode != 0)
			{
				string portraitPalette = $"king{tens}{ones}.pal";

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, portraitPalette, 0xc1d6);

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, GameTexts.FileSlamPalette, 0xbdee);

				for (ushort colourIndex = 0xc0; colourIndex < 0x1b0; colourIndex++)
				{
					this.Processor.EmitGlobalByte((ushort)(colourIndex + 0xbdf4), this.Processor.PullGlobalByte((ushort)(colourIndex + 0xc1dc)));
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

			this.Processor.EmitByte(ReplayFallenLeadersSegment, (ushort)(place + ReplayFallenLeadersTableAddress), (byte)leaderIndex);

			this.Owner.Services.RestoreTimer();

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileSlamPictureSecond, 0);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0x85, wallName, 0);

			DrawWallOfLeaders(Screen.BackScreenBox);

			while ((short)this.Owner.Timer.Beats < 0xf0)
			{
				this.Owner.Words.Clear();
			}

			this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(Math.Abs((short)year), 10));

			this.Owner.Words.Append((short)year < 0 ? " BC" : " AD");

			this.Owner.Words.Append(": ");

			this.Owner.Words.Append(Tables.CivilizationPlural((ushort)this.Owner.World.PersonParticipantIndex));

			this.Owner.Words.Append(" destroy");

			DrawShadowedCentredLine(152);

			this.Owner.Words.Set("the ");

			Message(Tables.CivilizationName((ushort)participantIndex))
				.Text(" civilization!");

			DrawShadowedCentredLine(168);

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0x85, 0x140, 0x43, Screen.BackScreenBox, 0, 0x85);

			this.Owner.Services.DrawPictureToScreen(Screen.Box, mouthHorizontal, mouthVertical - 2, closedMouth);

			this.Owner.Helpers.ReleaseResource(portrait, 0);

			this.Owner.Services.PerformSound(1, 0);

			this.Owner.TextPainter.WaitForTime(60);

			this.Owner.Services.PerformSound(Tables.LeaderWord(this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].ChiefVisuals, LeaderTable.Tune), 3);

			this.Owner.VideoCard.DrawScreenToMainScreen(1);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

			this.Owner.Services.ShowScreen(0);

			this.Owner.Services.PerformSound(1, 0);
		}

		private void ReplayGame()
		{
			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

			for (int townIndex = 0; townIndex < 0x80; townIndex++)
			{
				EmitReplayCity(townIndex, ReplayCityActiveColumnOffset, 0xffff);
			}

			short[] leaders = new short[8];

			for (int participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				leaders[participantIndex] = this.Owner.World.Participants[participantIndex].ChiefVisuals;
			}

			for (short participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				if (participantIndex == this.Owner.World.PersonParticipantIndex)
				{
					continue;
				}

				bool secondLeader = (this.Owner.World.ParticipantIdentifierMarkers & (1 << participantIndex)) != 0;

				this.Owner.World.Participants[participantIndex].ChiefVisuals = (short)(secondLeader ? participantIndex + 8 : participantIndex);

				ObtainReplayPlayerLabel(participantIndex);
			}

			if (Replay.Mode != 4)
			{
				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

				for (short verticalLocation = 1; verticalLocation < 0x31; verticalLocation++)
				{
					for (short horizontalLocation = 0; horizontalLocation < 0x50; horizontalLocation++)
					{
						bool ocean = this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == MapRules.OceanTerrain;

						this.Owner.Services.FloodBox(Screen.Box, horizontalLocation * 4, (verticalLocation * 4) + 4, 4, 4, (ushort)(ocean ? 1 : 8));
					}
				}
			}

			ushort spot = 0;

			while ((short)spot < this.Owner.World.RerunContentSpan)
			{
				spot = ReplayEvent(spot);

				if (Replay.Mode == 2 && this.Owner.Runtime.ReadKey() == 0x1b)
				{
					break;
				}
			}

			for (int participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				this.Owner.World.Participants[participantIndex].ChiefVisuals = leaders[participantIndex];
			}

			for (short participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				if (participantIndex != this.Owner.World.PersonParticipantIndex)
				{
					ObtainReplayPlayerLabel(participantIndex);
				}
			}

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

			this.Owner.Startup.ShowMouseCursor();
		}

		private byte ReplayByte(ref ushort spot)
		{
			return this.Owner.World.RerunContent[spot++];
		}

		private ushort ReplayPlural(int participantIndex)
		{
			return Tables.CivilizationPlural(participantIndex);
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
						Replay.Actor = (ushort)this.Owner.World.PersonParticipantIndex;

						short towns = ReplayByte(ref spot);
						sbyte populationHigh = (sbyte)ReplayByte(ref spot);
						short populationLow = ReplayByte(ref spot);

						this.Owner.Words.Append(ReplayPlural(this.Owner.World.PersonParticipantIndex));

						this.Owner.Words.Append(": ");

						this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(towns, 10));

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

						if (Replay.Mode == 4 && destroyer == this.Owner.World.PersonParticipantIndex)
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

			ushort labelIndex = ReplayByte(ref spot);
			ushort horizontalLocation = ReplayByte(ref spot);
			ushort verticalLocation = ReplayByte(ref spot);

			if (Replay.Actor != 0xff)
			{
				this.Owner.Words.Append(ReplayPlural(Replay.Actor));

				if (kind == 1)
				{
					this.Owner.Words.Append(" found the city of ");

					AssignReplayCityContent(Replay.Actor, horizontalLocation, verticalLocation);
				}
				else
				{
					this.Owner.Words.Append(" capture ");

					AssignReplayCityContentAlternate(Replay.Actor, horizontalLocation, verticalLocation);
				}

				AppendReplayCityName(labelIndex);

				DrawReplayPreamble();
			}
			else
			{
				AppendReplayCityName(labelIndex);

				this.Owner.Words.Append(" destroyed");

				DrawReplayPreamble();

				AssignReplayCityContentAlternate(0xffff, horizontalLocation, verticalLocation);
			}

			DrawReplayMapTile(horizontalLocation, verticalLocation);

			if (Replay.Mode == 1)
			{
				this.Owner.TextPainter.WaitForTime(30);
			}
		}

		private void AppendReplayCityName(ushort labelIndex)
		{
			ushort end = (ushort)(TextBuffer.Address + this.Owner.Words.Length);

			for (int index = 0; index < 0xd; index++)
			{
				this.Processor.EmitByte(this.Processor.SegmentData.Word, (ushort)(end + index), (byte)this.Owner.World.TownLabels[labelIndex][index]);
			}
		}

		private void ReplayRanking(ref ushort spot)
		{
			this.Owner.Words.Set("*** ");

			for (short pairIndex = 0; pairIndex < 4; pairIndex++)
			{
				byte places = this.Owner.World.RerunContent[spot];

				if ((places & 0xf0) != 0)
				{
					this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)((pairIndex << 1) + 1), 10));

					this.Owner.Words.Append(":");

					this.Owner.Words.Append(ReplayPlural(places >> 4));

					this.Owner.Words.Append(" ");
				}

				if ((places & 0xf) != 0)
				{
					this.Owner.Words.Append(this.Owner.Runtime.IntegerToText((short)((pairIndex << 1) + 2), 10));

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

		private ushort PullReplayCity(int index, int column)
		{
			return this.Processor.PullWord(ReplayCitySegment, (ushort)((index << 1) + column));
		}

		private void EmitReplayCity(int index, int column, ushort amount)
		{
			this.Processor.EmitWord(ReplayCitySegment, (ushort)((index << 1) + column), amount);
		}

		private void DrawWallOfLeaders(ushort screen)
		{
			ushort placeIndex;

			for (placeIndex = 0; (short)placeIndex < 0xe; placeIndex++)
			{
				byte leaderIndex = this.Processor.PullByte(ReplayFallenLeadersSegment, (ushort)(placeIndex + ReplayFallenLeadersTableAddress));

				if (leaderIndex == 0xff)
				{
					continue;
				}

				this.Owner.VideoCard.RenderImage(
					Screen.BackScreenBox,
					(ushort)((((ushort)leaderIndex % 7) * 0x1c) + 1),
					(ushort)(((ushort)leaderIndex / 7 * 0x22) + 0x85),
					0x1b, 0x21,
					screen,
					(ushort)(((placeIndex % 7) * 0x2e) + 8),
					(ushort)((-(placeIndex / 7) * 0x2a) + 0x31));
			}
		}

		private void DrawShadowedCentredLine(int verticalLocation)
		{
			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, verticalLocation, 20);

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, verticalLocation - 1, Screen.VideoGraphicsMode != 0 ? (ushort)23 : (ushort)15);
		}
	}
}
