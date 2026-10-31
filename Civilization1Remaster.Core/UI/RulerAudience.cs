using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;

namespace Civilization1Remaster.UI
{
	public class RulerAudience : GameComponent, IRulerAudience
	{
		private const ushort KingEyesHueAddress = 0x3a9a;
		private const ushort KingGestureHue1Address = 0x3ab4;
		private const ushort KingGestureHue2Address = 0x3ab7;
		private const ushort LeaderFontTableSegment = 0x36fa;
		private const ushort AdvisorKingVisualsSegment = 0x36fa;
		private const ushort AdvisorHorizOffsetsTableAddress = 0x3afe;
		private const ushort AdvisorSpritesTableAddress = 0x804e;
		private const ushort KingEyesPicturesTableAddress = 0x7efe;

		private readonly UnitMemory troops;

		private ushort audienceNuclearThreat;
		private ushort audienceTribute;
		private ushort audienceDemand;
		private ushort audienceWantedTechnology;
		private ushort audienceOfferedTechnology;
		private ushort audienceWasHostile;
		private ushort audienceHostile;
		private ushort audiencePeaceBroken;
		private ushort audienceDanger;
		private ushort audienceCommonEnemy;
		private ushort audienceLastAudience;

		public RulerAudience(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		private ushort HostileValue => this.audienceHostile;
		private Game.State.Player Person => this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx];

		/// <inheritdoc/>
		public void LaunchAudience(short participantIdx, int horizLoc, int vertLoc, ushort occasion)
		{
			this.Processor.Journal.OpenScope($"LaunchAudience({participantIdx}, {horizLoc}, {vertLoc}, {occasion})");

			this.Owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);
			this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);

			this.audienceLastAudience = (ushort)Leader(participantIdx).TouchParticipantTimeleft;
			NameEmpress(participantIdx);

			if (occasion == 0)
				Leader(participantIdx).TouchParticipantTimeleft = this.Owner.World.RoundTally;

			EmissaryAudience = 0;
			this.audienceHostile = 0;
			KingAnswer = occasion;
			this.audienceDanger = 0;
			this.audienceTribute = 0;
			this.audienceDemand = 1;
			AudienceWelcomed = 0;

			WeighContinents(participantIdx);
			WeighNuclearWeapons(participantIdx);
			WeighEnemies(participantIdx);

			bool ended = !ExchangeTechnologies(participantIdx, horizLoc, vertLoc, occasion);

			if (!ended)
			{
				if ((this.Owner.World.DiagMarkers & 1) == 0)
				{
					// Without the diplomacy option the leader only threatens
					this.audienceHostile = 1;
					this.audienceDemand = 0;
					this.audienceDanger = 0;
					this.audiencePeaceBroken = 0;
				}

				AskForAlliance(participantIdx, horizLoc, vertLoc);
				DemandTribute(participantIdx, horizLoc, vertLoc);
				DemandTechnology(participantIdx, horizLoc, vertLoc);
				Threaten(participantIdx, horizLoc, vertLoc, occasion);

				if (HostileValue == 0 && !OfferTreaty(participantIdx, horizLoc, vertLoc, occasion))
					DeclareWar(participantIdx);

				NameEmpressBack(participantIdx);
				AudienceWelcomed = 0;

				WelcomeFriendship(participantIdx, horizLoc, vertLoc);
				AudienceWelcomed = 1;

				if ((Relation(participantIdx) & 2) != 0 && WelcomeEmissary(participantIdx, horizLoc, vertLoc) != 0xffff)
					NegotiateUnderPeace(participantIdx);

				if (this.audiencePeaceBroken != 0)
					this.Owner.World.Participants[participantIdx].Relations[this.Owner.World.PersonParticipantIdx] |= 0x100;

				this.Owner.World.Participants[participantIdx].Relations[this.Owner.World.PersonParticipantIdx] &= 0xffdf;
				this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].Relations[participantIdx] &= 0xffdf;

				AdviseAboutLeader(participantIdx);
			}

			if (KingAnswer != 0xffff)
				FinishAudience();

			NameEmpressBack(participantIdx);

			EmissaryAudience = 0;
			KingAnswer = 0xffff;
			KingMood = 0xffff;
			this.Processor.Journal.CloseScope("LaunchAudience");
		}

		/// <inheritdoc/>
		public void FinishAudience()
		{
			this.Processor.Journal.OpenScope("FinishAudience()");

			ReleaseKingResources();

			DrawingBox.TypefaceIdx = 0x1;

			this.Owner.TurnHooks.RepaintMainScreen();

			KingMood = 0xffff;
			KingAnswer = 0;
			EmissaryAudience = KingAnswer;

			this.Processor.Journal.CloseScope("FinishAudience");
		}

		/// <inheritdoc/>
		public ushort WelcomeEmissary(short participantIdx, int horizLoc, int vertLoc)
		{
			this.Processor.Journal.OpenScope($"WelcomeEmissary({participantIdx}, {horizLoc}, {vertLoc})");

			if (EmissaryAudience == 0)
			{
				EmissaryAudience = 1;

				if (KingAnswer == 0)
				{
					AskToReceiveEmissary(participantIdx, horizLoc, vertLoc);
				}

				if (KingAnswer != 0xffff)
				{
					ReceiveEmissary(participantIdx);
				}
			}

			ushort answer = KingAnswer;

			this.Processor.Journal.CloseScope("WelcomeEmissary");

			return answer;
		}

		/// <inheritdoc/>
		public void ObtainKingSalutation(short participantIdx)
		{
			this.Processor.Journal.OpenScope($"ObtainKingSalutation({participantIdx})");

			ushort leader = (ushort)((participantIdx << 1) + 0x19a2);
			ushort Regime() => GovernmentTitle(this.Owner.World.Participants[participantIdx].RegimeKind);

			this.Owner.Words.Set((this.Owner.World.Participants[participantIdx].Relations[this.Owner.World.PersonParticipantIdx] & 8) != 0 ? (ushort)0x39b1 : (ushort)0x39e8);

			switch (this.Owner.Libc.Dice.DrawInt(4))
			{
				case 0:
					this.Owner.Words.Append("the most exalted\n");
					this.Owner.Words.Append(Regime());
					this.Owner.Words.Append(": ");
					this.Owner.Words.Append(this.Processor.PullGlobalWord(leader));
					this.Owner.Words.Append(" ");
					break;

				case 1:
					this.Owner.Words.Append(this.Processor.PullGlobalWord(leader));
					this.Owner.Words.Append(", ruler\nand ");
					this.Owner.Words.Append(Regime());
					this.Owner.Words.Append(" ");
					break;

				case 2:
					this.Owner.Words.Append("our most wise\n");
					this.Owner.Words.Append(Regime());
					this.Owner.Words.Append(": ");
					this.Owner.Words.Append(this.Processor.PullGlobalWord(leader));
					this.Owner.Words.Append(" ");
					break;

				case 3:
					this.Owner.Words.Append("he who makes mortals\ntremble: ");
					this.Owner.Words.Append(this.Processor.PullGlobalWord(leader));
					this.Owner.Words.Append(" ");
					break;
			}

			this.Owner.Words.Append("of the ");
			this.Owner.Words.Append(CivilizationPlural(participantIdx));
			this.Owner.Words.Append("...\n");

			if (this.Owner.World.Participants[participantIdx].LiveTroops[25] != 0)
			{
				this.Owner.Words.Append("Our words are backed\nwith NUCLEAR WEAPONS!\n");
			}

			this.Processor.Journal.CloseScope("ObtainKingSalutation");
		}

		/// <inheritdoc/>
		public void DisplayKingMessage(short participantIdx)
		{
			this.Processor.Journal.OpenScope($"DisplayKingMessage({participantIdx})");

			this.Owner.Codes.Set(3, CivilizationNameAddr(participantIdx));

			this.Owner.Codes.Set(0, CivilizationNameAddr(this.Owner.World.PersonParticipantIdx));

			KingSpeaking = 0xc;
			this.Owner.Words.Clear();

			this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicTreaty);

			DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);

			this.Processor.Journal.CloseScope("DisplayKingMessage");
		}

		/// <inheritdoc/>
		public void LogAudience(short participantIdx)
		{
			this.Processor.Journal.OpenScope($"LogAudience({participantIdx})");

			this.Owner.Politics.AssignDiplomacyFlags(this.Owner.World.PersonParticipantIdx, participantIdx, 2);

			this.Owner.World.Participants[participantIdx].TouchParticipantTimeleft = (short)(this.Owner.World.RoundTally + 16);

			if (this.Owner.World.HardnessTier == 0)
			{
				for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (!this.troops.Exists(participantIdx, troopIdx))
						continue;

					ushort townIdx = this.Owner.Helpers.LocateNearestCity(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

					if (this.Owner.World.Towns[townIdx].ParticipantIdx == this.Owner.World.PersonParticipantIdx &&
						(short)FoundDistance <= 2)
					{
						this.Owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
					}
				}
			}

			this.Processor.Journal.CloseScope("LogAudience");
		}

		/// <inheritdoc/>
		public void ChoreographKingSpeaking()
		{
			this.Processor.Journal.OpenScope("ChoreographKingSpeaking()");

			// The final values of the locals stay in the stack frame, like in the original

			KingPose = 0xffff;

			short eyes, mouth;

			if (KingSpeaking != 0 && KingStep == 0)
			{
				eyes = KingStep != 0 ? (short)5 : (short)-1;

				mouth = this.Owner.Libc.Dice.DrawInt(2) != 0 ? (short)9 : (short)0;

				if (((byte)KingSpeaking & 3) == 0)
					mouth = 8;

				KingSpeaking = (ushort)(KingSpeaking - 1);
				KingPause = 4;
			}
			else
			{
				AdvanceKingGesture();

				if (KingPause == 0 && this.Owner.Libc.Dice.DrawInt(12) == 0 && KingGestureStep() == 0)
				{
					switch (KingStep)
					{
						case 0:
							KingStep = 1;
							break;

						case 6:
							KingStep = 0xa;
							break;

						case 0xe:
							KingStep = 0x11;
							break;
					}
				}

				ushort step = KingStep;

				eyes = (sbyte)this.Processor.PullGlobalByte((ushort)(step + 0x3a94));
				mouth = (sbyte)this.Processor.PullGlobalByte((ushort)(step + 0x3aa6));
			}

			if (KingPause != 0)
				KingPause = (ushort)(KingPause - 1);

			ChoreographKingLikeness((ushort)eyes, (ushort)mouth, KingPose);

			this.Owner.TextPainter.AwaitTime(10);

			this.Processor.Journal.CloseScope("ChoreographKingSpeaking");
		}

		/// <inheritdoc/>
		public void DisplayKingScreen(short participantIdx)
		{
			this.Processor.Journal.OpenScope($"DisplayKingScreen({participantIdx})");


			AudiencePlayer = (ushort)participantIdx;
			AudienceWithoutPictures = 0;

			if ((this.Owner.World.SessionPrefMarkers & 8) != 0)
			{
				this.Owner.Startup.QueryForDisk(2, 1);
			}
			else
			{
				if (this.Owner.Startup.QueryForDisk(2, 0) == 0)
				{
					DisplayKingLine(participantIdx);

					this.Processor.Journal.CloseScope("DisplayKingScreen");
					return;
				}
			}

			string advisorsPic = NameAdvisorsPicture(participantIdx, true);

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, advisorsPic, 0);

			for (int advisorIdx = 0; advisorIdx < 4; advisorIdx++)
			{
				this.Processor.EmitGlobalWord((ushort)((6 * advisorIdx) + 0x804e), this.Owner.VideoCard.ScreenToPicture(1, (ushort)((0x50 * advisorIdx) + 1), 0x65, 0x4e, 0x63));
			}

			short king = (sbyte)this.Processor.PullGlobalByte((ushort)((ushort)this.Owner.World.Participants[participantIdx].ChiefVisuals + 0x3938));
			KingPicture = (ushort)king;

			this.Owner.Startup.QueryForDisk((ushort)((KingPicture / 7) + 4), 1);

			king = (short)KingPicture;
			char tens = (char)('0' + (king / 10));
			char ones = (char)('0' + (king % 10));
			string kingPic = (VgaMode == 0 && (KingPicture == 0 || KingPicture == 3))
				? $"kink{tens}{ones}.pic"
				: $"king{tens}{ones}.pic";

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, kingPic, 0);

			KingScenePicture = this.Owner.VideoCard.ScreenToPicture(1, 0xb5, 0x43, 0x8b, 0x85);

			ImportKingScenery(participantIdx);

			this.Owner.Helpers.ReleaseResource(GovernmentPicture, 0x3aee);

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, kingPic, 0);

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

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			if (this.Owner.Startup.QueryForDisk(2, 0) != 0)
			{
				string secondAdvisorsPic = NameAdvisorsPicture(participantIdx, false);

				this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, secondAdvisorsPic, 0);

				// The 12 small advisors, 3 a row
				ushort row, column = 0;

				for (row = 0; row < 4; row++)
				{
					for (column = 0; column < 3; column++)
					{
						this.Processor.EmitGlobalWord((ushort)((column << 1) + (6 * row) + 0x804e), this.Owner.VideoCard.ScreenToPicture(1, (ushort)((0x1d * column) + 0xa1), (ushort)((0x19 * row) + 1), 0x1c, 0x18));
					}
				}

				KingScreenKept = 1;

				this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

				this.Owner.Startup.QueryForDisk(0, 1);

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);
			}
			else
			{
				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

				KingScreenKept = 0;
			}

			KingPictureLoaded = 1;

			this.Processor.Journal.CloseScope("DisplayKingScreen");
		}

		/// <inheritdoc/>
		public void ImportKingScenery(short participantIdx)
		{
			this.Processor.Journal.OpenScope($"ImportKingScenery({participantIdx})");

			if (AudienceWithoutPictures == 0)
			{
				this.Owner.Startup.ConcealMouseCursor();

				short regime = this.Owner.World.Participants[participantIdx].RegimeKind;
				char regimeChar = regime == 3 ? '3' : (char)((regime / 2) + '0');
				char techChar = regime != 3 && KingModernPictures != 0 ? 'm' : 'a';
				string sceneryPic = $"back{regimeChar}{techChar}.pic";

				this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, sceneryPic, 0);

				for (int figureIdx = 0; (short)((this.Owner.World.Participants[participantIdx].Standing + 1) >> 1) > figureIdx; figureIdx++)
				{
					this.Owner.Services.RenderPictureToScreen(BackScreenBox,
						(short)this.Processor.PullGlobalWord((ushort)((figureIdx << 1) + 0x3afe)), 34,
						this.Processor.PullGlobalWord((ushort)((((figureIdx + participantIdx) & 3) * 6) + 0x804e)));
				}

				this.Owner.Services.RenderPictureToScreen(BackScreenBox, 90, 0, KingScenePicture);

				KingStep = 0;

				// The background's colours: "backNa.pal"
				string sceneryPal = $"back{regimeChar}{techChar}.pal";

				short king = (short)KingPicture;
				char tens = (char)('0' + (king / 10));
				char ones = (char)('0' + (king % 10));
				string hueName = $"king{tens}{ones}.pal";

				this.Owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, hueName, 0xc1d6);

				this.Owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, sceneryPal, 0xbdee);

				// The colours 0x40 - 0x8f of the king over the background's
				for (ushort place = 0xc0; place < 0x1b0; place++)
				{
					this.Processor.EmitGlobalByte((ushort)(place + 0xbdf4), this.Processor.PullGlobalByte((ushort)(place + 0xc1dc)));
				}

				this.Owner.Helpers.HaltHueCycling();

				this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

				this.Owner.VideoCard.AssignHuesFromHueStruct(0xbdee);

				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

				this.Owner.Startup.DisplayMouseCursor();
			}

			this.Processor.Journal.CloseScope("'LoadKingBackground'");
		}

		/// <inheritdoc/>
		public void ReleaseKingResources()
		{
			this.Processor.Journal.OpenScope("ReleaseKingResources()");

			this.Owner.Services.PerformSound(1, 0);

			if (KingPictureLoaded != 0)
			{
				this.Owner.Helpers.ReleaseResource(KingFacesPicture, 0);
			}

			KingPictureLoaded = 0x0;

			this.Processor.Journal.CloseScope("ReleaseKingResources");
		}

		/// <inheritdoc/>
		public void ChoreographKingLikeness(ushort eyes, ushort mouth, ushort advisorPose)
		{
			this.Processor.Journal.OpenScope($"ChoreographKingLikeness({eyes}, {mouth}, {advisorPose})");

			if (AudienceWithoutPictures == 0)
			{
				this.Owner.Startup.ConcealMouseCursor();

				short regime = this.Owner.World.Participants[AudiencePlayer].RegimeKind;
				short style = (short)((regime / 2) % 3);

				if (regime == 3)
				{
					style = 6;
				}
				else if (KingModernPictures != 0)
				{
					style += 3;
				}

				if (advisorPose != 0xffff && advisorPose != AdvisorPoseShown && KingScreenKept != 0)
				{
					for (short advisorIdx = 0; advisorIdx < (short)((this.Owner.World.Participants[AudiencePlayer].Standing + 1) >> 1); advisorIdx++)
					{
						ushort seat = (ushort)((advisorIdx + AudiencePlayer) & 3);
						ushort place = (ushort)((style << 4) + (seat << 2));

						// The tables of the advisors are in the segment 0x36fa (ES is left there, like in the original)
						this.Processor.SegExtra.Value16 = AdvisorKingVisualsSegment;

						short horizLoc = (short)(this.Processor.PullGlobalWord((ushort)((advisorIdx << 1) + AdvisorHorizOffsetsTableAddress)) +
							(this.Processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(place + 0x80)) - (0x50 * seat)) -
							(this.Processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(place + 0x82)) - 0xa0));
						short vertLoc = (short)((this.Processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(place + 0xf0)) - 0x64) -
							(this.Processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(place + 0xf2)) - (0x19 * seat)));

						this.Owner.Services.RenderPictureToScreen(ScreenBox, horizLoc, vertLoc + 34,
							this.Processor.PullGlobalWord((ushort)((advisorPose << 1) + (0x6 * seat) + AdvisorSpritesTableAddress)));

						AdvisorPoseShown = advisorPose;
					}
				}

				ushort king = (ushort)(KingPicture << 2);

				this.Processor.SegExtra.Value16 = AdvisorKingVisualsSegment;

				short faceHoriz = (short)(0x5a + (this.Processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(king + 0x10)) - 0xb4) - this.Processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(king + 0x12)));
				short faceVert = (short)((this.Processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(king + 0x48)) - 0x40) - this.Processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(king + 0x4a)));

				if (eyes != 0xffff)
				{
					this.Owner.Services.RenderPictureToScreen(ScreenBox, faceHoriz, faceVert - 2,
						this.Processor.PullGlobalWord((ushort)((eyes << 2) + KingEyesPicturesTableAddress)));
				}

				if (mouth != 0xffff)
				{
					this.Owner.Services.RenderPictureToScreen(ScreenBox, faceHoriz, faceVert + 18,
						this.Processor.PullGlobalWord((ushort)((mouth << 2) + 0x7f00)));
				}

				this.Owner.Startup.DisplayMouseCursor();
			}

			this.Processor.Journal.CloseScope("ChoreographKingLikeness");
		}

		/// <inheritdoc/>
		public ushort DisplayKingDialog(ushort charsAddr, ushort horizLoc, ushort vertLoc)
		{
			this.Processor.Journal.OpenScope($"DisplayKingDialog(0x{charsAddr:x4}, {horizLoc}, {vertLoc})");

			this.Owner.Startup.ConcealMouseCursor();

			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0x80, 0x140, 0x48,
				BackScreenBox, 0, 0x80);

			if (horizLoc == 0x14)
			{
				horizLoc = 0x24;
			}

			this.Owner.Startup.DisplayMouseCursorFully();

			ushort answer = this.Owner.Dialogs.DisplayMenu(charsAddr, (short)horizLoc, (short)vertLoc, 1);

			this.Owner.Startup.ConcealMouseCursorToSavedLevel();

			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0x80, 0x140, 0x48,
				ScreenBox, 0, 0x80);

			this.Owner.Startup.DisplayMouseCursor();

			this.Processor.Journal.CloseScope("DisplayKingDialog");

			return answer;
		}

		/// <summary>The name of a technology (records of 0x16 bytes from 0x4da)</summary>
		private static ushort TechnologyName(ushort scienceIdx) => TechnologyText(scienceIdx);

		private Game.State.Player Leader(int participantIdx) => this.Owner.World.Participants[participantIdx];

		/// <summary>The relation of the computer player to the human player</summary>
		private ushort Relation(short participantIdx) => Leader(participantIdx).Relations[this.Owner.World.PersonParticipantIdx];

		/// <summary>The leader 14 is an empress: her titles for the audience</summary>
		private void NameEmpress(short participantIdx)
		{
			if (Leader(participantIdx).ChiefVisuals == 14)
			{
				this.Owner.Names.Set(GovernmentTitleAt(2), "Empress");
				this.Owner.Names.Set(GovernmentTitleAt(4), "Empress");
			}
		}

		private void NameEmpressBack(short participantIdx)
		{
			if (Leader(participantIdx).ChiefVisuals == 14)
			{
				this.Owner.Names.Set(GovernmentTitleAt(2), SavedGovernmentTitleMonarchy);
				this.Owner.Names.Set(GovernmentTitleAt(4), SavedGovernmentTitleCommunism);
			}
		}

		/// <summary>
		/// The continents 1 - 14: the danger from the human player's defence where the leader has cities (0x2e), hostility
		/// where both have attackers (0x24), and the tribute the leader may ask (0x6)
		/// </summary>
		private void WeighContinents(short participantIdx)
		{
			Game.State.Player leader = Leader(participantIdx);

			for (int continentIdx = 1; continentIdx < 0xf; continentIdx++)
			{
				var own = leader.Landmasses[continentIdx];
				var human = Person.Landmasses[continentIdx];
				short cities = (short)(leader.TownTally > 1 ? 1 : 0);

				if (own.TownTally > cities && human.Guard > own.Assault)
				{
					short factor = (short)(this.Owner.World.HardnessTier == 0 ? 4 : 2);
					short danger = CPU.SignedQuotient((short)(factor * human.Guard), (short)(own.Assault + 1));
					this.audienceDanger = (ushort)(this.audienceDanger + danger);
					continue;
				}

				if (human.Assault != 0 && own.Assault != 0)
					this.audienceHostile = 1;

				short tribute;

				if (human.TownTally != 0)
				{
					tribute = own.TownTally > 1 ? (short)(own.Guard - human.Guard) : own.Guard;
				}
				else if (own.TownTally != 0)
				{
					tribute = CPU.SignedQuotient((short)(own.Assault - human.Guard), 2);
				}
				else
				{
					tribute = CPU.SignedQuotient((short)(own.Guard - human.Guard), 4);
				}

				this.audienceTribute = (ushort)(this.audienceTribute + tribute);
			}
		}

		/// <summary>
		/// Nuclear weapons: a leader threatens a human player who leads (standing 7) late in the game without them (0x4), a
		/// leader with them asks for more, a human player with them makes a leader without them afraid; the Great Wall and
		/// the United Nations of the human player calm the leader; the tribute in fifties, within the human player's gold
		/// </summary>
		private void WeighNuclearWeapons(short participantIdx)
		{
			Game.State.Player leader = Leader(participantIdx);
			Game.State.Player person = Person;
			short personIdx = this.Owner.World.PersonParticipantIdx;

			bool threat = person.Standing == 7 && person.TownTally > 4 && leader.TownTally > 1 && person.LiveTroops[25] == 0 &&
				this.Owner.World.RoundTally > 200;
			this.audienceNuclearThreat = (ushort)(threat ? 1 : 0);

			if ((threat || ((Relation(participantIdx) & 8) != 0 && (short)(3 * leader.MartialMight) > person.MartialMight)) &&
				person.LiveTroops[25] == 0)
			{
				this.audienceHostile = 1;
				this.audienceTribute = (ushort)this.Owner.Helpers.VerifyValueSpan((short)this.audienceTribute, 10 * leader.Standing, 9999);
				this.audienceDanger = 0;
			}

			if (leader.LiveTroops[25] != 0)
				leader.Relations[personIdx] |= 0x80;

			if (leader.LiveTroops[25] != 0 && person.LiveTroops[25] == 0)
			{
				this.audienceHostile = 1;
				this.audienceTribute = (ushort)this.Owner.Helpers.VerifyValueSpan((short)this.audienceTribute, 100, 9999);
			}

			if (person.LiveTroops[25] != 0)
			{
				if (leader.LiveTroops[25] == 0)
				{
					this.audienceDanger = 4;
				}
				else
				{
					short half = CPU.SignedQuotient((short)this.audienceTribute, 2);
					this.audienceTribute = (ushort)CPU.SignedQuotient((short)(leader.LiveTroops[25] * half), person.LiveTroops[25]);
				}
			}

			if (this.audienceDanger != 0)
			{
				this.audienceHostile = 0;
				this.audienceDemand = 0;
			}

			if (this.Owner.CityOps.PlayerEnjoysWonderEffect(personIdx, 7) != 0 || this.Owner.CityOps.PlayerEnjoysWonderEffect(personIdx, 0x12) != 0)
			{
				this.audienceNuclearThreat = 0;
				this.audienceHostile = 0;

				if (this.audienceLastAudience == 0xfffe)
					this.audienceLastAudience = 0xffff;
			}

			if ((Relation(participantIdx) & 8) != 0)
				this.audienceTribute = (ushort)(this.audienceTribute << 1);
			else if (this.audienceLastAudience == 0xfffe)
				this.audienceTribute = (ushort)CPU.SignedQuotient((short)this.audienceTribute, 4);

			this.audienceTribute = (ushort)((ushort)this.Owner.Helpers.VerifyValueSpan(((short)this.audienceTribute * (this.Owner.World.HardnessTier + 1)) / 32, 0, 20) * 0x32);

			short gold = person.Gold;

			if ((short)this.audienceTribute > gold && (short)this.audienceTribute <= (short)(gold << 1) && gold >= 0x32)
				this.audienceTribute = (ushort)(CPU.SignedQuotient(gold, 0x32) * 0x32);

			if (leader.LiveTroops[25] != 0)
			{
				if ((Relation(participantIdx) & 0x80) == 0 && person.Gold < (short)this.audienceTribute)
					this.audienceTribute = (ushort)(CPU.SignedQuotient(person.Gold, 0x32) * 0x32);

				leader.Relations[personIdx] |= 0x80;
			}

			if (this.audienceTribute == 0 || (short)(3 * leader.MartialMight) < person.MartialMight)
				this.audienceHostile = 0;

			if (this.audienceHostile != 0)
				this.audienceDemand = 2;

			if (this.audienceHostile == 0 && (Relation(participantIdx) & 4) != 0)
				this.audienceDemand = 0;
		}

		/// <summary>
		/// The leader's attitude (0x2a): its wars against stronger players, its character, the human player's strength; a
		/// common enemy at war with the leader and at peace with the human player (0x30); a calm leader gives up hostility (and
		/// at random the tribute); a democracy or communism threatens only when much stronger, else turns to monarchy
		/// </summary>
		private void WeighEnemies(short participantIdx)
		{
			Game.State.Player leader = Leader(participantIdx);
			short personIdx = this.Owner.World.PersonParticipantIdx;

			this.audienceWasHostile = HostileValue;
			short attitude = 0;

			if ((Relation(participantIdx) & 8) != 0)
				attitude = -2;

			this.audienceCommonEnemy = 0xffff;

			for (short otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				if (otherIdx == personIdx || (leader.Relations[otherIdx] & 3) != 1)
					continue;

				if (leader.MartialMight < (short)(Leader(otherIdx).MartialMight << 2))
					attitude++;

				if (Leader(otherIdx).MartialMight > leader.MartialMight)
					attitude++;

				if ((Person.Relations[otherIdx] & 2) != 0)
					this.audienceCommonEnemy = (ushort)otherIdx;
			}

			ushort character = this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(0x1512 + 0x3a * leader.ChiefVisuals));
			attitude = (short)(attitude - character);

			if (Person.MartialMight < leader.MartialMight)
				attitude--;

			short calm = (short)((Relation(participantIdx) & 2) == 0 ? 1 : 0);

			if (calm < attitude && this.audienceNuclearThreat == 0)
			{
				this.audienceHostile = 0;

				if (this.Owner.Libc.Dice.DrawInt(2) != 0)
					this.audienceTribute = 0;
			}

			if (leader.RegimeKind >= 4 && HostileValue != 0 && (Relation(participantIdx) & 8) == 0)
			{
				if ((short)(3 * Person.MartialMight) > (short)(leader.MartialMight << 1) && this.audienceNuclearThreat == 0)
					this.audienceHostile = 0;
				else
					this.Owner.Politics.SwitchGovernment(participantIdx, 2);
			}

			// At peace and weaker (or already warned): the leader keeps the peace for now
			this.audiencePeaceBroken = 0;

			if ((Relation(participantIdx) & 2) != 0 && HostileValue != 0 &&
				(leader.MartialMight < Person.MartialMight || (Relation(participantIdx) & 8) != 0))
			{
				this.audienceHostile = 0;
				this.audiencePeaceBroken = 1;
				this.audienceDemand = 0;
			}
		}

		/// <summary>
		/// The technologies each side knows (0xe, 0x32), one the leader knows and the human player doesn't (0xa), the most
		/// valuable one the human player knows and the leader doesn't (0xc, value 0x2c); a friendly leader with fewer
		/// technologies offers an exchange, again after each one. False: the audience ends here (a friendly leader at peace
		/// in danger).
		/// </summary>
		private bool ExchangeTechnologies(short participantIdx, int horizLoc, int vertLoc, ushort occasion)
		{
			short personIdx = this.Owner.World.PersonParticipantIdx;

			while (true)
			{
				this.audienceOfferedTechnology = 0xffff;
				this.audienceWantedTechnology = 0xffff;
				short offeredValue = 0;
				short leaderTechnologies = 0;
				short personTechnologies = 0;

				for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
				{
					if (this.Owner.Advisors.PlayerKnowsTechnology(personIdx, scienceIdx) != 0)
						personTechnologies++;

					if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
						leaderTechnologies++;

					if (this.Owner.Advisors.PlayerKnowsTechnology(personIdx, scienceIdx) == 0 &&
						this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
					{
						this.audienceWantedTechnology = (ushort)scienceIdx;
					}

					if (this.Owner.Advisors.PlayerKnowsTechnology(personIdx, scienceIdx) != 0 &&
						this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) == 0 &&
						(short)this.Owner.Advisors.ObtainTechnologyInfo(participantIdx, (ushort)scienceIdx) >= offeredValue)
					{
						this.audienceOfferedTechnology = (ushort)scienceIdx;
						offeredValue = (short)this.Owner.Advisors.ObtainTechnologyInfo(participantIdx, (ushort)scienceIdx);
					}
				}

				if (((Relation(participantIdx) & 0x20) != 0 || HostileValue != 0) && leaderTechnologies > 6)
					break;

				if (personTechnologies < leaderTechnologies ||
					this.audienceWantedTechnology == 0xffff || WelcomeEmissary(participantIdx, horizLoc, vertLoc) == 0xffff)
				{
					break;
				}

				KingMood = 1;
				this.Owner.Words.Clear();
				this.Owner.Codes.Set(3, TechnologyName(this.audienceWantedTechnology));
				this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicExchange);
				KingSpeaking = 0xc;

				if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
					break;

				this.Owner.Startup.ConcealMouseCursor();
				this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);
				this.Owner.Startup.DisplayMouseCursor();

				KingMood = 0xffff;
				CivilopediaWithoutPictures = 1;
				this.Owner.Conquest.ChooseTechnologyToTake(participantIdx, personIdx);
				RestoreScreenStyle();

				CivilopediaWithoutPictures = 1;
				this.Owner.Conquest.ChooseTechnologyToTake(personIdx, participantIdx);
				this.Owner.Helpers.HaltHueCycling();
				RestoreScreenStyle();

				this.audienceHostile = 0;
				KingMood = 1;

				this.Owner.Startup.ConcealMouseCursor();
				this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);
				this.Owner.Startup.DisplayMouseCursor();
			}

			// A friendly leader at peace with a human player strong on its continents doesn't negotiate
			return !(HostileValue == 0 && occasion == 0 && (Relation(participantIdx) & 2) == 2 && (short)this.audienceDanger > 2);
		}

		/// <summary>The style of screen 0 (word 0x10 of it) back from 0x67ec</summary>
		private void RestoreScreenStyle()
		{
			ushort screen = ScreenBox;
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, (ushort)(screen + 0x10), SavedScreenStyle);
		}

		/// <summary>The leader asks for an alliance against the common enemy (0x30): accepted, the human player breaks the peace with it</summary>
		private void AskForAlliance(short participantIdx, int horizLoc, int vertLoc)
		{
			if (this.audienceCommonEnemy == 0xffff || WelcomeEmissary(participantIdx, horizLoc, vertLoc) == 0xffff || this.audienceDemand == 0)
				return;

			this.Owner.Words.Clear();
			this.Owner.Codes.Set(1, CivilizationPlural(this.audienceCommonEnemy));
			this.Owner.Codes.Set(3, (ushort)(HostileValue != 0 ? 0x3780 : 0x3787));
			this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicCancel);

			KingMood = (ushort)(HostileValue != 0 ? 2 : 1);
			KingSpeaking = 0xc;

			if (HostileValue != 0)
				this.Owner.Services.PerformSound(0x21, 0);

			if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
				return;

			this.audienceHostile = 0;

			if (this.Owner.Libc.Dice.DrawInt(2) != 0)
				this.audienceTribute = 0;

			short enemyIdx = (short)this.audienceCommonEnemy;
			this.Owner.Politics.WipeDiplomacyFlags(this.Owner.World.PersonParticipantIdx, enemyIdx, 2);
			this.Owner.World.Participants[(ushort)enemyIdx].Relations[this.Owner.World.PersonParticipantIdx] |= 8;
		}

		/// <summary>A hostile leader demands the tribute (0x6) from a human player who has more gold; after the answer 0x6 is 999</summary>
		private void DemandTribute(short participantIdx, int horizLoc, int vertLoc)
		{
			if (this.audienceTribute == 0 || this.audienceDemand != 2 || Person.Gold <= (short)this.audienceTribute ||
				WelcomeEmissary(participantIdx, horizLoc, vertLoc) == 0xffff)
			{
				return;
			}

			EmptyMessage()
				.Number((short)this.audienceTribute);
			this.Owner.Codes.Set(2, this.Owner.Words.ToString());
			this.Owner.Words.Clear();
			this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicTribute);

			AudienceWelcomed = 0;
			KingMood = 2;
			KingSpeaking = 0xc;
			this.Owner.Services.PerformSound(0x21, 0);

			if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) == 1)
			{
				short tribute = (short)this.audienceTribute;
				Person.Gold -= tribute;
				Leader(participantIdx).Gold += tribute;
				this.audienceHostile = 0;
			}

			this.audienceTribute = 0x3e7;
		}

		/// <summary>A leader with demands asks for the most valuable technology it lacks (0xc); after the answer 0x6 is 999</summary>
		private void DemandTechnology(short participantIdx, int horizLoc, int vertLoc)
		{
			if (this.audienceDemand == 0 || this.audienceOfferedTechnology == 0xffff || this.audienceTribute == 0x3e7 ||
				WelcomeEmissary(participantIdx, horizLoc, vertLoc) == 0xffff)
			{
				return;
			}

			this.Owner.Words.Clear();
			this.Owner.Codes.Set(3, TechnologyName(this.audienceOfferedTechnology));
			this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicTakeciv);

			AudienceWelcomed = 0;
			KingMood = 2;
			KingSpeaking = 0xc;
			this.Owner.Services.PerformSound(0x21, 0);

			if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) == 1)
			{
				this.Owner.Advisors.EarnTechnology(participantIdx, (short)this.audienceOfferedTechnology, this.Owner.World.PersonParticipantIdx);
				this.audienceHostile = 0;
			}

			this.audienceTribute = 0x3e7;
		}

		/// <summary>
		/// A hostile leader threatens: at peace and with a big tribute left, after a refused demand, or (at war: only when it
		/// came) otherwise; with an emissary the threat is a dialog, without one a message; the peace treaty ends
		/// </summary>
		private void Threaten(short participantIdx, int horizLoc, int vertLoc, ushort occasion)
		{
			ushort text;
			bool welcome;

			if (HostileValue != 0 && (Relation(participantIdx) & 2) != 0 && (short)this.audienceTribute > 0x32)
			{
				welcome = WelcomeEmissary(participantIdx, horizLoc, vertLoc) != 0xffff;
				text = (ushort)(welcome ? 0x37a9 : 0x37b2);
			}
			else if (HostileValue != 0 && this.audienceTribute == 0x3e7)
			{
				if (WelcomeEmissary(participantIdx, horizLoc, vertLoc) == 0xffff)
					return;

				welcome = true;
				text = 0x37bb;
			}
			else
			{
				if (HostileValue == 0 || ((Relation(participantIdx) & 1) != 0 && occasion == 0))
					return;

				welcome = WelcomeEmissary(participantIdx, horizLoc, vertLoc) != 0xffff;
				text = (ushort)(welcome ? 0x37c3 : 0x37c8);
			}

			if (welcome)
				this.Owner.Services.PerformSound(0x21, 0);

			this.Owner.Words.Clear();
			this.Owner.BlockText.DisplayTextDialog(text);
			this.Owner.BlockText.ArrangeTextRow(0x28);

			if (welcome)
			{
				AudienceWelcomed = 1;
				KingMood = 2;
				KingSpeaking = 0xc;
				DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
			}
			else
			{
				this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 40, 100);
			}

			this.Owner.Politics.WipeDiplomacyFlags(this.Owner.World.PersonParticipantIdx, participantIdx, 2);
		}

		/// <summary>
		/// A calm leader (not at peace, or the human player asked for the audience) offers peace: accepted, the treaty is
		/// signed for 16 turns; under a democracy or communism the senate signs anyway; else the peace ends, and a leader in
		/// danger offers a technology, gold for peace, or (in its last city) everything it has for peace. True: done.
		/// </summary>
		private bool OfferTreaty(short participantIdx, int horizLoc, int vertLoc, ushort occasion)
		{
			short personIdx = this.Owner.World.PersonParticipantIdx;

			if (((Relation(participantIdx) & 2) != 0 && occasion == 0) || WelcomeEmissary(participantIdx, horizLoc, vertLoc) == 0xffff)
				return true;

			this.Owner.Words.Clear();
			this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicPeace);
			AudienceWelcomed = 0;
			KingMood = 0;
			KingSpeaking = 0xc;

			if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) == 1)
			{
				DisplayKingMessage(participantIdx);
				LogAudience(participantIdx);
				Leader(participantIdx).TouchParticipantTimeleft = (short)(this.Owner.World.RoundTally + 16);
				return true;
			}

			if (Person.RegimeKind >= 4)
			{
				this.Owner.Words.Set("You are overruled by the Senate.\nTreaty signed.\n");
				DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
				LogAudience(participantIdx);
				return true;
			}

			if ((Relation(participantIdx) & 2) != 0)
				this.Owner.Politics.WipeDiplomacyFlags(personIdx, participantIdx, 2);

			if ((short)this.audienceDanger >= 4 && this.audienceWantedTechnology != 0xffff)
			{
				if ((Relation(participantIdx) & 0xa) == 0)
					OfferTechnologyForPeace(participantIdx);
			}

			if ((Relation(participantIdx) & 0xa) == 0)
				OfferGoldForPeace(participantIdx);

			if (this.audienceDanger != 0 && Leader(participantIdx).TownTally <= 1)
			{
				if ((Relation(participantIdx) & 0xa) == 0)
					OfferEverythingForPeace(participantIdx);
			}

			return false;
		}

		private void OfferTechnologyForPeace(short participantIdx)
		{
			this.Owner.Codes.Set(3, TechnologyName(this.audienceWantedTechnology));
			this.Owner.Words.Clear();
			this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicGiveciv);
			KingMood = 0;
			KingSpeaking = 0xc;

			if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
				return;

			this.Owner.Startup.ConcealMouseCursor();
			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);
			this.Owner.Startup.DisplayMouseCursor();
			this.Owner.Helpers.HaltHueCycling();

			KingMood = 0xffff;
			CivilopediaWithoutPictures = 1;
			this.Owner.Advisors.EarnTechnology(this.Owner.World.PersonParticipantIdx, (short)this.audienceWantedTechnology, participantIdx);
			RestoreScreenStyle();
			this.Owner.Helpers.HaltHueCycling();
			KingMood = 0;

			this.Owner.Startup.ConcealMouseCursor();
			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);
			this.Owner.Startup.DisplayMouseCursor();

			DisplayKingMessage(participantIdx);
			LogAudience(participantIdx);
			this.audienceLastAudience = (ushort)this.Owner.World.RoundTally;
		}

		/// <summary>Gold for peace (0x22: twice the danger - 4, in fifties, up to the leader's gold)</summary>
		private void OfferGoldForPeace(short participantIdx)
		{
			ushort amount = (ushort)((ushort)this.Owner.Helpers.VerifyValueSpan(((short)this.audienceDanger * 2) - 4, 0, Leader(participantIdx).Gold / 50) * 0x32);

			if (amount == 0)
				return;

			EmptyMessage()
				.Number((short)amount);
			this.Owner.Codes.Set(2, this.Owner.Words.ToString());
			this.Owner.Words.Clear();
			this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicGivecash);
			KingMood = 0;
			KingSpeaking = 0xc;

			if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
				return;

			DisplayKingMessage(participantIdx);
			LogAudience(participantIdx);
			Person.Gold += (short)amount;

			// A translation slip: the gold is taken from the word at SI (the leader << 4), not from the leader
			ushort taken = (ushort)(participantIdx << 4);
			this.Processor.EmitUInt16(this.Processor.SegData.Value16, taken, (ushort)(this.Processor.PullUInt16(this.Processor.SegData.Value16, taken) - amount));
			this.audienceLastAudience = (ushort)this.Owner.World.RoundTally;
		}

		/// <summary>A leader in danger in its last city gives all its gold and technologies for peace</summary>
		private void OfferEverythingForPeace(short participantIdx)
		{
			short personIdx = this.Owner.World.PersonParticipantIdx;

			EmptyMessage()
				.Number(Leader(participantIdx).Gold);
			this.Owner.Codes.Set(2, this.Owner.Words.ToString());
			this.Owner.Words.Clear();
			this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicGrovel);
			KingMood = 0;
			KingSpeaking = 0xc;

			if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
				return;

			DisplayKingMessage(participantIdx);
			LogAudience(participantIdx);

			Person.Gold += Leader(participantIdx).Gold;
			Leader(participantIdx).Gold = 0;

			this.Owner.Startup.ConcealMouseCursor();
			this.Owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);
			this.Owner.Startup.DisplayMouseCursor();
			this.Owner.Helpers.HaltHueCycling();
			KingMood = 0xffff;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{
				if (this.Owner.Advisors.PlayerKnowsTechnology(personIdx, scienceIdx) == 0 &&
					this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
				{
					CivilopediaWithoutPictures = 1;
					this.Owner.Advisors.EarnTechnology(personIdx, scienceIdx, participantIdx);
				}
			}

			RestoreScreenStyle();
			this.Owner.Helpers.HaltHueCycling();
			KingMood = 0;

			this.Owner.Startup.ConcealMouseCursor();
			this.Owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);
			this.Owner.Startup.DisplayMouseCursor();

			this.audienceLastAudience = (ushort)this.Owner.World.RoundTally;
		}

		/// <summary>Without a peace treaty the leader declares war</summary>
		private void DeclareWar(short participantIdx)
		{
			if ((Relation(participantIdx) & 2) != 0)
				return;

			this.Owner.Services.PerformSound(0x21, 0);
			this.Owner.Words.Clear();
			this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicMobilize);
			AudienceWelcomed = 1;
			KingMood = 2;
			KingSpeaking = 4;
			DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
		}

		/// <summary>A calm leader at peace welcomes the human player's friendship (or reminds of the demand when one is open)</summary>
		private void WelcomeFriendship(short participantIdx, int horizLoc, int vertLoc)
		{
			if (HostileValue != 0 || (Relation(participantIdx) & 2) != 2 || WelcomeEmissary(participantIdx, horizLoc, vertLoc) == 0xffff)
				return;

			if (this.audienceDemand != 0 && this.audienceTribute != 0x3e7)
			{
				KingMood = 1;
				KingSpeaking = 4;
				this.Owner.Words.Clear();
				this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicAttitude);
			}
			else
			{
				short personIdx = this.Owner.World.PersonParticipantIdx;

				KingMood = 0;
				KingSpeaking = 0xc;
				Message("We welcome the friendship of the\n")
					.Text(CivilizationName(personIdx))
					.Text(" people and their most\nwise and munificent leader:\n")
					.Text(GovernmentTitle(Person.RegimeKind))
					.Text(" ")
					.Text(LeaderName(personIdx))
					.Text(".\n");
			}

			DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
		}

		/// <summary>
		/// At peace the human player may talk: 2 demands gold (or the leader ends the peace, or ignores the threat), 1 asks
		/// the leader to attack someone (an enemy of the human player: it may join for gold)
		/// </summary>
		private void NegotiateUnderPeace(short participantIdx)
		{
			short personIdx = this.Owner.World.PersonParticipantIdx;

			while (true)
			{
				this.Owner.Codes.Set(3, CivilizationPlural(participantIdx));
				this.Owner.Words.Clear();
				this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicThreat);

				// Choices disabled: 2 (a demand) shortly after the last audience or under a democracy, 1 (an attack) with only two players left
				if ((short)(this.Owner.World.RoundTally - this.audienceLastAudience) < 0x10 || Person.RegimeKind >= 4)
					DisabledLines = 4;

				if (((1 << participantIdx) | (1 << (byte)personIdx)) == (ushort)this.Owner.World.LiveRealms)
					DisabledLinesLow = (byte)((byte)DisabledLines | 2);

				ushort choice = DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);

				if (choice == 2)
					DemandGoldUnderPeace(participantIdx);

				if (choice != 1 || !AskToAttack(participantIdx))
					return;
			}
		}

		private void DemandGoldUnderPeace(short participantIdx)
		{
			short personIdx = this.Owner.World.PersonParticipantIdx;

			short amount = (short)((ushort)this.Owner.Helpers.VerifyValueSpan((short)this.audienceDanger, 0, Leader(participantIdx).Gold / 50) * 0x32);

			if (amount != 0)
			{
				EmptyMessage()
					.Number(amount);
				this.Owner.Codes.Set(2, this.Owner.Words.ToString());
				this.Owner.Words.Clear();
				this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicGivemore);
				KingMood = 0;
				KingSpeaking = 0xc;

				// The human player gets the gold amount (classic translation slip gave the dialog button answer 0 or 1)
				ushort answer = DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
				short goldGiven = this.Owner.TurnHooks.ClassicTributeDemand ? (short)answer : amount;
				Person.Gold += goldGiven;
				Leader(participantIdx).Gold -= amount;
				Leader(participantIdx).TouchParticipantTimeleft = this.Owner.World.RoundTally;
				return;
			}

			if (this.audienceWasHostile != 0)
			{
				this.Owner.Services.PerformSound(0x21, 0);
				this.Owner.Words.Clear();
				this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicProvoke);
				this.Owner.Politics.WipeDiplomacyFlags(personIdx, participantIdx, 2);
				KingMood = 2;
				KingSpeaking = 0xc;
			}
			else
			{
				this.Owner.Words.Set("We ignore your feeble threats.\n");
				KingMood = 1;
				KingSpeaking = 4;
			}

			DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
		}

		/// <summary>
		/// The human player names a living player to attack: not at war with it, the leader refuses; not at peace with it,
		/// the leader says so and the talk starts again (true); else it joins for gold, if the human player has it
		/// </summary>
		private bool AskToAttack(short participantIdx)
		{
			short personIdx = this.Owner.World.PersonParticipantIdx;

			this.Owner.Words.Set("You must attack the evil:\n ");
			short[] enemyTable = new short[8];
			int enemyCount = 0;

			for (short otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				if (otherIdx == personIdx || otherIdx == participantIdx || (this.Owner.World.LiveRealms & (1 << (byte)otherIdx)) == 0)
					continue;

				enemyTable[enemyCount++] = otherIdx;

				this.Owner.Words.Append(CivilizationPlural(otherIdx));
				this.Owner.Words.Append("\n ");
			}

			ushort choice = DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
			KingMood = 1;

			if (choice == 0xffff)
				return false;

			short enemyIdx = enemyTable[choice];

			this.Owner.Codes.Set(3, CivilizationPlural(enemyIdx));
			KingSpeaking = 4;

			if ((Leader(participantIdx).Relations[enemyIdx] & 1) == 0)
			{
				this.Owner.Words.Clear();
				this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicNocontact);
				DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
				return false;
			}

			if ((Leader(participantIdx).Relations[enemyIdx] & 2) == 0)
			{
				this.Owner.Words.Clear();
				this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicAtwar);
				DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
				return true;
			}

			short amount = (short)((ushort)this.Owner.Helpers.VerifyValueSpan(((Leader(enemyIdx).MartialMight / 2) * (Leader(participantIdx).Gold / 100)) / 50, 2, 9999) * 0x32);

			EmptyMessage()
				.Number(amount);
			this.Owner.Codes.Set(2, this.Owner.Words.ToString());
			this.Owner.Words.Clear();
			this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicMercenary);

			if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
				return false;

			if (Person.Gold < amount)
			{
				this.Owner.Words.Clear();
				this.Owner.BlockText.DisplayTextDialog(GameTexts.TopicUnfortunate);
			}
			else
			{
				Leader(participantIdx).Gold += amount;
				Person.Gold -= amount;

				Message(CivilizationPlural(participantIdx))
					.Text(" declare war\non the ")
					.Text(CivilizationPlural(enemyIdx))
					.Text(".\n");
			}

			DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
			return false;
		}

		/// <summary>After an audience the advisor warns of a big leader without a treaty (once per audience)</summary>
		private void AdviseAboutLeader(short participantIdx)
		{
			if (KingAnswer == 0xffff || Leader(participantIdx).TownTally <= 4 ||
				Person.Standing + 1 >= Leader(participantIdx).Standing ||
				(Person.Relations[participantIdx] & 2) != 0)
			{
				return;
			}

			this.Owner.Codes.Set(1, CivilizationPlural(participantIdx));
			this.Owner.HelpSystem.DisplayHelpPopup(GameTexts.TopicSchism);
		}

		/// <summary>The question of WelcomeEmissary: 0x3934 becomes minus the chosen line (-2 shows the intelligence report first)</summary>
		private void AskToReceiveEmissary(short participantIdx, int horizLoc, int vertLoc)
		{
			if (horizLoc != -1)
			{
				this.Owner.MapOps.FocusMapView(this.Owner.World.PersonParticipantIdx, horizLoc - 8, vertLoc - 2);
			}

			while (true)
			{
				Message("An emissary from the\n")
					.Text(CivilizationPlural(participantIdx))
					.Text(" wishes to\nspeak with you.\n")
					.Text("Will you receive him?\n Yes.\n No.\n");

				if ((this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].Relations[participantIdx] & 0x40) != 0)
				{
					this.Owner.Words.Append(" Check Intelligence\n");
				}

				this.Owner.Services.PerformSound(LeaderWord(this.Owner.World.Participants[participantIdx].ChiefVisuals, LeaderTable.ShortTune), 3);

				this.Owner.Startup.DisplayMouseCursorFully();

				KingAnswer = (ushort)-this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80);

				this.Owner.Startup.ConcealMouseCursorToSavedLevel();

				if (KingAnswer != 0xfffe)
					break;

				this.Owner.Briefings.DisplayIntelligenceReport(participantIdx);
			}
		}

		/// <summary>The audience of WelcomeEmissary: the king's screen, his greeting and dialog</summary>
		private void ReceiveEmissary(short participantIdx)
		{
			this.Owner.Startup.ConcealMouseCursor();

			DisplayKingScreen(participantIdx);

			this.Owner.Services.PerformSound(LeaderWord(this.Owner.World.Participants[participantIdx].ChiefVisuals, LeaderTable.Tune), 3);

			this.Owner.Startup.DisplayMouseCursor();

			// The font of the leader (table at 0x36fa), font 3 instead of 10
			ushort typeface = (ushort)((sbyte)this.Processor.PullUInt8(LeaderFontTableSegment, (ushort)this.Owner.World.Participants[participantIdx].ChiefVisuals) + 8);
			SavedScreenStyle = typeface == 0xa ? (ushort)3 : typeface;

			DrawingBox.TypefaceIdx = SavedScreenStyle;
			AudienceInitialPrompt = 1;
			KingMood = 1;
			AdvisorPoseShown = 0xffff;
			KingPose = 0;
			KingSpeaking = 0xc;

			ObtainKingSalutation(participantIdx);

			DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
		}

		private ushort CivilizationNameAddr(short participantIdx)
		{
			return CivilizationName(participantIdx);
		}

		/// <summary>The entry of table 0x3ab8 for the king's step 0x67f4</summary>
		private sbyte KingGestureStep()
		{
			return (sbyte)this.Processor.PullGlobalByte((ushort)(KingStep + 0x3ab8));
		}

		/// <summary>The next step of the king's gesture (see ChoreographKingSpeaking)</summary>
		private void AdvanceKingGesture()
		{
			sbyte next = KingGestureStep();

			if (next > 0)
			{
				KingStep = (ushort)(KingStep + 1);
				return;
			}

			if (next < 0)
			{
				switch (next)
				{
					case -3:
						KingStep = 0xe;
						break;

					case -2:
						KingStep = 6;
						break;

					case -1:
						KingStep = 0;
						break;
				}

				return;
			}

			short gesture = -1;

			switch (KingMood)
			{
				case 0:
					this.Processor.EmitGlobalByte(KingEyesHueAddress, AudienceWelcomed != 0 ? (byte)4 : (byte)2);

					if (KingStep != 6)
						gesture = 3;
					break;

				case 1:
					if (KingStep != 0)
						gesture = 0;
					break;

				case 2:
					this.Processor.EmitGlobalByte(KingGestureHue1Address, AudienceWelcomed != 0 ? (byte)0xb : (byte)7);
					this.Processor.EmitGlobalByte(KingGestureHue2Address, AudienceWelcomed != 0 ? (byte)0xb : (byte)7);

					if (KingStep != 0xe)
						gesture = 0xc;
					break;
			}

			if (KingSpeaking != 0 && KingStep != 0)
				gesture = 0;

			if (gesture == -1)
			{
				ushort step = KingStep;

				KingPose = step == 0xe ? (ushort)1 : step == 6 ? (ushort)2 : (ushort)0;
				return;
			}

			if (KingStep == 0)
				KingStep = (ushort)gesture;

			if (KingStep is 6 or 0xe)
				KingStep = (ushort)(KingStep + 1);
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
				KingModernPictures = this.Owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x14);
			}

			char regimeChar = regime == 3 ? '3' : (char)((regime / 2) + '0');
			char techChar = regime != 3 && KingModernPictures != 0 ? 'm' : 'a';
			return $"govt{regimeChar}{techChar}.pic";
		}

		/// <summary>The audience without pictures: "&lt;title&gt;: &lt;government&gt; of the &lt;civilization&gt;." over a motif</summary>
		private void DisplayKingLine(short participantIdx)
		{
			AudienceWithoutPictures = 1;

			this.Owner.Startup.ConcealMouseCursor();

			this.Owner.Helpers.FloodBoxWithMotif(0, 128, 320, 72);

			this.Owner.Dialogs.RenderShadowBox(0, 128, 319, 71, Hues.White, Hues.DarkGray);

			Message(LeaderName((ushort)participantIdx))
				.Text(": ")
				.Text(GovernmentTitle((ushort)this.Owner.World.Participants[participantIdx].RegimeKind))
				.Text(" of the ")
				.Text(CivilizationPlural((ushort)participantIdx))
				.Text(".");

			this.Owner.TextPainter.RenderTextToScreen0(this.Owner.Words.ToString(), 8, 130, Hues.White);

			this.Owner.Startup.DisplayMouseCursor();
		}
	}
}
