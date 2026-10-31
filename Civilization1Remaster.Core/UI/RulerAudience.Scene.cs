
namespace Civilization1Remaster.Core.UI
{
	/// <summary>The throne room: the pictures of the king and the advisors, their gestures and speaking, and the lines and dialogs of the king</summary>
	public partial class RulerAudience
	{
		/// <inheritdoc/>
		public void ShowKingMessage(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayKingMessage({participantIdx})");

			this.Owner.Codes.Set(3, CivilizationNameAddr(participantIdx));

			this.Owner.Codes.Set(0, CivilizationNameAddr(this.Owner.World.PersonParticipantIdx));

			King.Speaking = 0xc;
			this.Owner.Words.Clear();

			this.Owner.BlockText.ShowTextDialog(GameTexts.TopicTreaty);

			ShowKingDialog(TextBuffer.Address, 0x14, 0x8b);
		}

		/// <inheritdoc/>
		public void ChoreographKingSpeaking()
		{
			using var _ = this.Processor.Journal.Scope("ChoreographKingSpeaking()");

			// The final values of the locals stay in the stack frame, like in the original

			King.Pose = 0xffff;

			short eyes, mouth;

			if (King.Speaking != 0 && King.Step == 0)
			{
				eyes = King.Step != 0 ? (short)5 : (short)-1;

				mouth = this.Owner.Libc.Dice.DrawInt(2) != 0 ? (short)9 : (short)0;

				if (((byte)King.Speaking & 3) == 0)
					mouth = 8;

				King.Speaking = (ushort)(King.Speaking - 1);
				King.Pause = 4;
			}
			else
			{
				AdvanceKingGesture();

				if (King.Pause == 0 && this.Owner.Libc.Dice.DrawInt(12) == 0 && KingGestureStep() == 0)
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
				King.Pause = (ushort)(King.Pause - 1);

			ChoreographKingLikeness((ushort)eyes, (ushort)mouth, King.Pose);

			this.Owner.TextPainter.WaitForTime(10);
		}

		/// <inheritdoc/>
		public void ShowKingScreen(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayKingScreen({participantIdx})");


			King.AudiencePlayer = (ushort)participantIdx;
			King.AudienceWithoutPictures = 0;

			if ((this.Owner.World.SessionPrefMarkers & 8) != 0)
			{
				this.Owner.Startup.AskForDisk(2, 1);
			}
			else
			{
				if (this.Owner.Startup.AskForDisk(2, 0) == 0)
				{
					ShowKingLine(participantIdx);

					return;
				}
			}

			string advisorsPic = NameAdvisorsPicture(participantIdx, true);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, advisorsPic, 0);

			for (int advisorIdx = 0; advisorIdx < 4; advisorIdx++)
			{
				this.Processor.EmitGlobalWord((ushort)((6 * advisorIdx) + 0x804e), this.Owner.VideoCard.ScreenToPicture(1, (ushort)((0x50 * advisorIdx) + 1), 0x65, 0x4e, 0x63));
			}

			short king = (sbyte)this.Processor.PullGlobalByte((ushort)((ushort)this.Owner.World.Participants[participantIdx].ChiefVisuals + 0x3938));
			King.Picture = (ushort)king;

			this.Owner.Startup.AskForDisk((ushort)((King.Picture / 7) + 4), 1);

			king = (short)King.Picture;
			char tens = (char)('0' + (king / 10));
			char ones = (char)('0' + (king % 10));
			string kingPic = (Screen.VgaMode == 0 && (King.Picture == 0 || King.Picture == 3))
				? $"kink{tens}{ones}.pic"
				: $"king{tens}{ones}.pic";

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, kingPic, 0);

			King.ScenePicture = this.Owner.VideoCard.ScreenToPicture(1, 0xb5, 0x43, 0x8b, 0x85);

			LoadKingScenery(participantIdx);

			this.Owner.Helpers.ReleaseResource(Icons.GovernmentPicture, 0x3aee);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, kingPic, 0);

			// The 14 faces with their mouths, 5 a row (the 9th and the 12th skip two places)
			short place = 0;
			ushort faceIdx;

			for (faceIdx = 0; faceIdx < 0xe; faceIdx++)
			{
				if (faceIdx == 8 || faceIdx == 0xb)
					place += 2;

				ushort horizLoc = (ushort)(((place % 5) * 0x3c) + 1);
				ushort vertLoc = (ushort)((place / 5) * 0x32);

				this.Processor.EmitGlobalWord((ushort)((faceIdx << 2) + 0x7efe), this.Owner.VideoCard.ScreenToPicture(1, horizLoc, (ushort)(vertLoc + 1), 0x3b, 0x14));

				this.Processor.EmitGlobalWord((ushort)((faceIdx << 2) + 0x7f00), this.Owner.VideoCard.ScreenToPicture(1, horizLoc, (ushort)(vertLoc + 0x15), 0x3b, 0x1d));

				place++;
			}

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8, Screen.BackScreenBox, 0, 0);

			if (this.Owner.Startup.AskForDisk(2, 0) != 0)
			{
				string secondAdvisorsPic = NameAdvisorsPicture(participantIdx, false);

				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, secondAdvisorsPic, 0);

				// The 12 small advisors, 3 a row
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

		/// <inheritdoc/>
		public void LoadKingScenery(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ImportKingScenery({participantIdx})");

			if (King.AudienceWithoutPictures == 0)
			{
				this.Owner.Startup.HideMouseCursor();

				short regime = this.Owner.World.Participants[participantIdx].RegimeKind;
				char regimeChar = regime == 3 ? '3' : (char)((regime / 2) + '0');
				char techChar = regime != 3 && King.ModernPictures != 0 ? 'm' : 'a';
				string sceneryPic = $"back{regimeChar}{techChar}.pic";

				this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, sceneryPic, 0);

				for (int figureIdx = 0; (short)((this.Owner.World.Participants[participantIdx].Standing + 1) >> 1) > figureIdx; figureIdx++)
				{
					this.Owner.Services.DrawPictureToScreen(Screen.BackScreenBox,
						(short)this.Processor.PullGlobalWord((ushort)((figureIdx << 1) + 0x3afe)), 34,
						this.Processor.PullGlobalWord((ushort)((((figureIdx + participantIdx) & 3) * 6) + 0x804e)));
				}

				this.Owner.Services.DrawPictureToScreen(Screen.BackScreenBox, 90, 0, King.ScenePicture);

				King.Step = 0;

				// The background's colours: "backNa.pal"
				string sceneryPal = $"back{regimeChar}{techChar}.pal";

				short king = (short)King.Picture;
				char tens = (char)('0' + (king / 10));
				char ones = (char)('0' + (king % 10));
				string hueName = $"king{tens}{ones}.pal";

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, hueName, 0xc1d6);

				this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, sceneryPal, 0xbdee);

				// The colours 0x40 - 0x8f of the king over the background's
				for (ushort place = 0xc0; place < 0x1b0; place++)
				{
					this.Processor.EmitGlobalByte((ushort)(place + 0xbdf4), this.Processor.PullGlobalByte((ushort)(place + 0xc1dc)));
				}

				this.Owner.Helpers.HaltHueCycling();

				this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

				this.Owner.VideoCard.AssignHuesFromHueStruct(0xbdee);

				this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8, Screen.Box, 0, 0);

				this.Owner.Startup.ShowMouseCursor();
			}
		}

		/// <inheritdoc/>
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

		/// <inheritdoc/>
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
					for (short advisorIdx = 0; advisorIdx < (short)((this.Owner.World.Participants[King.AudiencePlayer].Standing + 1) >> 1); advisorIdx++)
					{
						ushort seat = (ushort)((advisorIdx + King.AudiencePlayer) & 3);
						ushort place = (ushort)((style << 4) + (seat << 2));

						// The tables of the advisors are in the segment 0x36fa (ES is left there, like in the original)
						this.Processor.SegExtra.Value16 = AdvisorKingVisualsSegment;

						short horizLoc = (short)(this.Processor.PullGlobalWord((ushort)((advisorIdx << 1) + AdvisorHorizOffsetsTableAddress)) +
							(this.Processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(place + 0x80)) - (0x50 * seat)) -
							(this.Processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(place + 0x82)) - 0xa0));
						short vertLoc = (short)((this.Processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(place + 0xf0)) - 0x64) -
							(this.Processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(place + 0xf2)) - (0x19 * seat)));

						this.Owner.Services.DrawPictureToScreen(Screen.Box, horizLoc, vertLoc + 34,
							this.Processor.PullGlobalWord((ushort)((advisorPose << 1) + (0x6 * seat) + AdvisorSpritesTableAddress)));

						King.AdvisorPoseShown = advisorPose;
					}
				}

				ushort king = (ushort)(King.Picture << 2);

				this.Processor.SegExtra.Value16 = AdvisorKingVisualsSegment;

				short faceHoriz = (short)(0x5a + (this.Processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(king + 0x10)) - 0xb4) - this.Processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(king + 0x12)));
				short faceVert = (short)((this.Processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(king + 0x48)) - 0x40) - this.Processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(king + 0x4a)));

				if (eyes != 0xffff)
				{
					this.Owner.Services.DrawPictureToScreen(Screen.Box, faceHoriz, faceVert - 2,
						this.Processor.PullGlobalWord((ushort)((eyes << 2) + KingEyesPicturesTableAddress)));
				}

				if (mouth != 0xffff)
				{
					this.Owner.Services.DrawPictureToScreen(Screen.Box, faceHoriz, faceVert + 18,
						this.Processor.PullGlobalWord((ushort)((mouth << 2) + 0x7f00)));
				}

				this.Owner.Startup.ShowMouseCursor();
			}
		}

		/// <inheritdoc/>
		public ushort ShowKingDialog(ushort charsAddr, ushort horizLoc, ushort vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayKingDialog(0x{charsAddr:x4}, {horizLoc}, {vertLoc})");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0x80, 0x140, 0x48,
				Screen.BackScreenBox, 0, 0x80);

			if (horizLoc == 0x14)
			{
				horizLoc = 0x24;
			}

			this.Owner.Startup.ShowMouseCursorFully();

			ushort answer = this.Owner.Dialogs.ShowMenu(charsAddr, (short)horizLoc, (short)vertLoc, 1);

			this.Owner.Startup.HideMouseCursorToSavedLevel();

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0x80, 0x140, 0x48,
				Screen.Box, 0, 0x80);

			this.Owner.Startup.ShowMouseCursor();

			return answer;
		}

		/// <summary>The style of screen 0 (word 0x10 of it) back from 0x67ec</summary>
		private void RestoreScreenStyle()
		{
			ushort screen = Screen.Box;
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)(screen + 0x10), King.SavedScreenStyle);
		}

		/// <summary>The entry of table 0x3ab8 for the king's step 0x67f4</summary>
		private sbyte KingGestureStep()
		{
			return (sbyte)this.Processor.PullGlobalByte((ushort)(King.Step + 0x3ab8));
		}

		/// <summary>The next step of the king's gesture (see ChoreographKingSpeaking)</summary>
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
						gesture = 3;
					break;

				case 1:
					if (King.Step != 0)
						gesture = 0;
					break;

				case 2:
					this.Processor.EmitGlobalByte(KingGestureHue1Address, King.AudienceWelcomed != 0 ? (byte)0xb : (byte)7);
					this.Processor.EmitGlobalByte(KingGestureHue2Address, King.AudienceWelcomed != 0 ? (byte)0xb : (byte)7);

					if (King.Step != 0xe)
						gesture = 0xc;
					break;
			}

			if (King.Speaking != 0 && King.Step != 0)
				gesture = 0;

			if (gesture == -1)
			{
				ushort step = King.Step;

				King.Pose = step == 0xe ? (ushort)1 : step == 6 ? (ushort)2 : (ushort)0;
				return;
			}

			if (King.Step == 0)
				King.Step = (ushort)gesture;

			if (King.Step is 6 or 0xe)
				King.Step = (ushort)(King.Step + 1);
		}

		/// <summary>
		/// "govtNa.pic" in the buffer: N the government / 2 ('3' for government 3), "m" instead of "a" with technology 0x14
		/// (0x67ee, read from the player the first time)
		/// </summary>
		private string NameAdvisorsPicture(short participantIdx, bool first)
		{
			short regime = this.Owner.World.Participants[participantIdx].RegimeKind;

			if (first)
			{
				King.ModernPictures = this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x14);
			}

			char regimeChar = regime == 3 ? '3' : (char)((regime / 2) + '0');
			char techChar = regime != 3 && King.ModernPictures != 0 ? 'm' : 'a';
			return $"govt{regimeChar}{techChar}.pic";
		}

		/// <summary>The audience without pictures: "&lt;title&gt;: &lt;government&gt; of the &lt;civilization&gt;." over a motif</summary>
		private void ShowKingLine(short participantIdx)
		{
			King.AudienceWithoutPictures = 1;

			this.Owner.Startup.HideMouseCursor();

			this.Owner.Helpers.FillBoxWithMotif(0, 128, 320, 72);

			this.Owner.Dialogs.DrawShadowBox(0, 128, 319, 71, Hues.White, Hues.DarkGray);

			Message(LeaderName((ushort)participantIdx))
				.Text(": ")
				.Text(GovernmentTitle((ushort)this.Owner.World.Participants[participantIdx].RegimeKind))
				.Text(" of the ")
				.Text(CivilizationPlural((ushort)participantIdx))
				.Text(".");

			this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 8, 130, Hues.White);

			this.Owner.Startup.ShowMouseCursor();
		}
	}
}
