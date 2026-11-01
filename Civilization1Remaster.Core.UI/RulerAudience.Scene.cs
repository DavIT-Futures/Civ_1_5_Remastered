namespace Civilization1Remaster.Core.UI
{
	public partial class RulerAudience
	{
		public void ShowKingMessage(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayKingMessage({participantIndex})");

			this.Owner.Codes.Set(3, CivilizationNameAddress(participantIndex));

			this.Owner.Codes.Set(0, CivilizationNameAddress(this.Owner.World.PersonParticipantIndex));

			King.Speaking = 0xc;
			this.Owner.Words.Clear();

			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicTreaty);

			ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
		}

		public void ChoreographKingSpeaking()
		{
			using var _ = this.Processor.Journal.Scope("ChoreographKingSpeaking()");

			King.Pose = 0xffff;

			short eyes, mouth;

			if (King.Speaking != 0 && King.Step == 0)
			{
				eyes = King.Step != 0 ? (short)5 : (short)-1;

				mouth = this.Owner.Runtime.Dice.DrawInteger(2) != 0 ? (short)9 : (short)0;

				if (((byte)King.Speaking & 3) == 0)
				{
					mouth = 8;
				}

				King.Speaking = (ushort)(King.Speaking - 1);
				King.Pause = 4;
			}
			else
			{
				AdvanceKingGesture();

				if (King.Pause == 0 && this.Owner.Runtime.Dice.DrawInteger(12) == 0 && KingGestureStep() == 0)
				{
					switch (King.Step)
					{
						case 0:
							King.Step = 1;
							break;

						case 6:
							King.Step = 0xa;
							break;

						case 0xe:
							King.Step = 0x11;
							break;
					}
				}

				ushort step = King.Step;

				eyes = (sbyte)this.Processor.PullGlobalByte((ushort)(step + 0x3a94));
				mouth = (sbyte)this.Processor.PullGlobalByte((ushort)(step + 0x3aa6));
			}

			if (King.Pause != 0)
			{
				King.Pause = (ushort)(King.Pause - 1);
			}

			ChoreographKingLikeness((ushort)eyes, (ushort)mouth, King.Pose);

			this.Owner.TextPainter.WaitForTime(10);
		}

		public void ShowKingScreen(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayKingScreen({participantIndex})");

			King.AudiencePlayer = (ushort)participantIndex;
			King.AudienceWithoutPictures = 0;

			if ((this.Owner.World.SessionPreferenceMarkers & 8) != 0)
			{
				this.Owner.Startup.AskForDisk(2, 1);
			}
			else
			{
				if (this.Owner.Startup.AskForDisk(2, 0) == 0)
				{
					ShowKingLine(participantIndex);

					return;
				}
			}

			string advisorsPicture = NameAdvisorsPicture(participantIndex, true);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, advisorsPicture, 0);

			for (int advisorIndex = 0; advisorIndex < 4; advisorIndex++)
			{
				this.Processor.EmitGlobalWord((ushort)((6 * advisorIndex) + 0x804e), this.Owner.VideoCard.ScreenToPicture(1, (ushort)((0x50 * advisorIndex) + 1), 0x65, 0x4e, 0x63));
			}

			short king = (sbyte)this.Processor.PullGlobalByte((ushort)((ushort)this.Owner.World.Participants[participantIndex].ChiefVisuals + 0x3938));
			King.Picture = (ushort)king;

			this.Owner.Startup.AskForDisk((ushort)((King.Picture / 7) + 4), 1);

			king = (short)King.Picture;
			char tens = (char)('0' + (king / 10));
			char ones = (char)('0' + (king % 10));
			string kingPicture = (Screen.VideoGraphicsMode == 0 && (King.Picture == 0 || King.Picture == 3))
				? $"kink{tens}{ones}.pic"
				: $"king{tens}{ones}.pic";

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, kingPicture, 0);

			King.ScenePicture = this.Owner.VideoCard.ScreenToPicture(1, 0xb5, 0x43, 0x8b, 0x85);

			LoadKingScenery(participantIndex);

			this.Owner.Helpers.ReleaseResource(Icons.GovernmentPicture, 0x3aee);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, kingPicture, 0);

			short place = 0;
			ushort faceIndex;

			for (faceIndex = 0; faceIndex < 0xe; faceIndex++)
			{
				if (faceIndex == 8 || faceIndex == 0xb)
				{
					place += 2;
				}

				ushort horizontalLocation = (ushort)(((place % 5) * 0x3c) + 1);
				ushort verticalLocation = (ushort)((place / 5) * 0x32);

				this.Processor.EmitGlobalWord((ushort)((faceIndex << 2) + 0x7efe), this.Owner.VideoCard.ScreenToPicture(1, horizontalLocation, (ushort)(verticalLocation + 1), 0x3b, 0x14));

				this.Processor.EmitGlobalWord((ushort)((faceIndex << 2) + 0x7f00), this.Owner.VideoCard.ScreenToPicture(1, horizontalLocation, (ushort)(verticalLocation + 0x15), 0x3b, 0x1d));

				place++;
			}

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

			if (this.Owner.Startup.AskForDisk(2, 0) != 0)
			{
				string secondAdvisorsPicture = NameAdvisorsPicture(participantIndex, false);

				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, secondAdvisorsPicture, 0);

				ushort row, column = 0;

				for (row = 0; row < 4; row++)
				{
					for (column = 0; column < 3; column++)
					{
						this.Processor.EmitGlobalWord((ushort)((column << 1) + (6 * row) + 0x804e), this.Owner.VideoCard.ScreenToPicture(1, (ushort)((0x1d * column) + 0xa1), (ushort)((0x19 * row) + 1), 0x1c, 0x18));
					}
				}

				King.ScreenKept = 1;

				this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

				this.Owner.Startup.AskForDisk(0, 1);

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);
			}
			else
			{
				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

				King.ScreenKept = 0;
			}

			King.PictureLoaded = 1;
		}

		public void LoadKingScenery(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ImportKingScenery({participantIndex})");

			if (King.AudienceWithoutPictures == 0)
			{
				this.Owner.Startup.HideMouseCursor();

				short regime = this.Owner.World.Participants[participantIndex].RegimeKind;
				char regimeCharacter = regime == 3 ? '3' : (char)((regime / 2) + '0');
				char technologyCharacter = regime != 3 && King.ModernPictures != 0 ? 'm' : 'a';
				string sceneryPicture = $"back{regimeCharacter}{technologyCharacter}.pic";

				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, sceneryPicture, 0);

				for (int figureIndex = 0; (short)((this.Owner.World.Participants[participantIndex].Standing + 1) >> 1) > figureIndex; figureIndex++)
				{
					this.Owner.Services.DrawPictureToScreen(Screen.BackScreenBox,
						(short)this.Processor.PullGlobalWord((ushort)((figureIndex << 1) + 0x3afe)), 34,
						this.Processor.PullGlobalWord((ushort)((((figureIndex + participantIndex) & 3) * 6) + 0x804e)));
				}

				this.Owner.Services.DrawPictureToScreen(Screen.BackScreenBox, 90, 0, King.ScenePicture);

				King.Step = 0;

				string sceneryPalette = $"back{regimeCharacter}{technologyCharacter}.pal";

				short king = (short)King.Picture;
				char tens = (char)('0' + (king / 10));
				char ones = (char)('0' + (king % 10));
				string hueName = $"king{tens}{ones}.pal";

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, hueName, 0xc1d6);

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, sceneryPalette, 0xbdee);

				for (ushort place = 0xc0; place < 0x1b0; place++)
				{
					this.Processor.EmitGlobalByte((ushort)(place + 0xbdf4), this.Processor.PullGlobalByte((ushort)(place + 0xc1dc)));
				}

				this.Owner.Helpers.HaltHueCycling();

				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

				this.Owner.VideoCard.AssignHuesFromHueStructure(0xbdee);

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

				this.Owner.Startup.ShowMouseCursor();
			}
		}

		public void ReleaseKingResources()
		{
			using var _ = this.Processor.Journal.Scope("ReleaseKingResources()");

			this.Owner.Services.PerformSound(1, 0);

			if (King.PictureLoaded != 0)
			{
				this.Owner.Helpers.ReleaseResource(King.FacesPicture, 0);
			}

			King.PictureLoaded = 0x0;
		}

		public void ChoreographKingLikeness(ushort eyes, ushort mouth, ushort advisorPose)
		{
			using var _ = this.Processor.Journal.Scope($"ChoreographKingLikeness({eyes}, {mouth}, {advisorPose})");

			if (King.AudienceWithoutPictures == 0)
			{
				this.Owner.Startup.HideMouseCursor();

				short regime = this.Owner.World.Participants[King.AudiencePlayer].RegimeKind;
				short style = (short)((regime / 2) % 3);

				if (regime == 3)
				{
					style = 6;
				}
				else if (King.ModernPictures != 0)
				{
					style += 3;
				}

				if (advisorPose != 0xffff && advisorPose != King.AdvisorPoseShown && King.ScreenKept != 0)
				{
					for (short advisorIndex = 0; advisorIndex < (short)((this.Owner.World.Participants[King.AudiencePlayer].Standing + 1) >> 1); advisorIndex++)
					{
						ushort seat = (ushort)((advisorIndex + King.AudiencePlayer) & 3);
						ushort place = (ushort)((style << 4) + (seat << 2));

						this.Processor.SegmentExtra.Word = AdvisorKingVisualsSegment;

						short horizontalLocation = (short)(this.Processor.PullGlobalWord((ushort)((advisorIndex << 1) + AdvisorHorizontalOffsetsTableAddress)) +
							(this.Processor.PullWord(AdvisorKingVisualsSegment, (ushort)(place + 0x80)) - (0x50 * seat)) -
							(this.Processor.PullWord(AdvisorKingVisualsSegment, (ushort)(place + 0x82)) - 0xa0));
						short verticalLocation = (short)((this.Processor.PullWord(AdvisorKingVisualsSegment, (ushort)(place + 0xf0)) - 0x64) -
							(this.Processor.PullWord(AdvisorKingVisualsSegment, (ushort)(place + 0xf2)) - (0x19 * seat)));

						this.Owner.Services.DrawPictureToScreen(Screen.Box, horizontalLocation, verticalLocation + 34,
							this.Processor.PullGlobalWord((ushort)((advisorPose << 1) + (0x6 * seat) + AdvisorSpritesTableAddress)));

						King.AdvisorPoseShown = advisorPose;
					}
				}

				ushort king = (ushort)(King.Picture << 2);

				this.Processor.SegmentExtra.Word = AdvisorKingVisualsSegment;

				short faceHorizontal = (short)(0x5a + (this.Processor.PullWord(AdvisorKingVisualsSegment, (ushort)(king + 0x10)) - 0xb4) - this.Processor.PullWord(AdvisorKingVisualsSegment, (ushort)(king + 0x12)));
				short faceVertical = (short)((this.Processor.PullWord(AdvisorKingVisualsSegment, (ushort)(king + 0x48)) - 0x40) - this.Processor.PullWord(AdvisorKingVisualsSegment, (ushort)(king + 0x4a)));

				if (eyes != 0xffff)
				{
					this.Owner.Services.DrawPictureToScreen(Screen.Box, faceHorizontal, faceVertical - 2,
						this.Processor.PullGlobalWord((ushort)((eyes << 2) + KingEyesPicturesTableAddress)));
				}

				if (mouth != 0xffff)
				{
					this.Owner.Services.DrawPictureToScreen(Screen.Box, faceHorizontal, faceVertical + 18,
						this.Processor.PullGlobalWord((ushort)((mouth << 2) + 0x7f00)));
				}

				this.Owner.Startup.ShowMouseCursor();
			}
		}

		public ushort ShowKingDialog(ushort charactersAddress, ushort horizontalLocation, ushort verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayKingDialog(0x{charactersAddress:x4}, {horizontalLocation}, {verticalLocation})");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0x80, 0x140, 0x48,
				Screen.BackScreenBox, 0, 0x80);

			if (horizontalLocation == 0x14)
			{
				horizontalLocation = 0x24;
			}

			this.Owner.Startup.ShowMouseCursorFully();

			ushort answer = this.Owner.Dialogs.ShowMenu(charactersAddress, (short)horizontalLocation, (short)verticalLocation, 1);

			this.Owner.Startup.HideMouseCursorToSavedLevel();

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0x80, 0x140, 0x48,
				Screen.Box, 0, 0x80);

			this.Owner.Startup.ShowMouseCursor();

			return answer;
		}

		private void RestoreScreenStyle()
		{
			ushort screen = Screen.Box;
			this.Processor.EmitWord(this.Processor.SegmentData.Word, (ushort)(screen + 0x10), King.SavedScreenStyle);
		}

		private sbyte KingGestureStep()
		{
			return (sbyte)this.Processor.PullGlobalByte((ushort)(King.Step + 0x3ab8));
		}

		private void AdvanceKingGesture()
		{
			sbyte next = KingGestureStep();

			if (next > 0)
			{
				King.Step = (ushort)(King.Step + 1);
				return;
			}

			if (next < 0)
			{
				switch (next)
				{
					case -3:
						King.Step = 0xe;
						break;

					case -2:
						King.Step = 6;
						break;

					case -1:
						King.Step = 0;
						break;
				}

				return;
			}

			short gesture = -1;

			switch (King.Mood)
			{
				case 0:
					this.Processor.EmitGlobalByte(KingEyesHueAddress, King.AudienceWelcomed != 0 ? (byte)4 : (byte)2);

					if (King.Step != 6)
					{
						gesture = 3;
					}

					break;

				case 1:
					if (King.Step != 0)
					{
						gesture = 0;
					}

					break;

				case 2:
					this.Processor.EmitGlobalByte(KingGestureHueFirstAddress, King.AudienceWelcomed != 0 ? (byte)0xb : (byte)7);
					this.Processor.EmitGlobalByte(KingGestureHueSecondAddress, King.AudienceWelcomed != 0 ? (byte)0xb : (byte)7);

					if (King.Step != 0xe)
					{
						gesture = 0xc;
					}

					break;
			}

			if (King.Speaking != 0 && King.Step != 0)
			{
				gesture = 0;
			}

			if (gesture == -1)
			{
				ushort step = King.Step;

				King.Pose = step == 0xe ? (ushort)1 : step == 6 ? (ushort)2 : (ushort)0;
				return;
			}

			if (King.Step == 0)
			{
				King.Step = (ushort)gesture;
			}

			if (King.Step is 6 or 0xe)
			{
				King.Step = (ushort)(King.Step + 1);
			}
		}

		private string NameAdvisorsPicture(short participantIndex, bool first)
		{
			short regime = this.Owner.World.Participants[participantIndex].RegimeKind;

			if (first)
			{
				King.ModernPictures = this.Owner.Advisors.PlayerKnowsTechnology(participantIndex, 0x14);
			}

			char regimeCharacter = regime == 3 ? '3' : (char)((regime / 2) + '0');
			char technologyCharacter = regime != 3 && King.ModernPictures != 0 ? 'm' : 'a';
			return $"govt{regimeCharacter}{technologyCharacter}.pic";
		}

		private void ShowKingLine(short participantIndex)
		{
			King.AudienceWithoutPictures = 1;

			this.Owner.Startup.HideMouseCursor();

			this.Owner.Helpers.FillBoxWithMotif(0, 128, 320, 72);

			this.Owner.Dialogs.DrawShadowBox(0, 128, 319, 71, Hues.White, Hues.DarkGray);

			Message(Tables.LeaderName((ushort)participantIndex))
				.Text(": ")
				.Text(Tables.GovernmentTitle((ushort)this.Owner.World.Participants[participantIndex].RegimeKind))
				.Text(" of the ")
				.Text(Tables.CivilizationPlural((ushort)participantIndex))
				.Text(".");

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 8, 130, Hues.White);

			this.Owner.Startup.ShowMouseCursor();
		}
	}
}
