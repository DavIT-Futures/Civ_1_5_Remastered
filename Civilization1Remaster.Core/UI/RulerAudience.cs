namespace Civilization1Remaster.Core.UI
{
	public partial class RulerAudience : GameComponent, IRulerAudience
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
		private GameModel.State.Player Person => this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx];

		public void LaunchAudience(short participantIdx, int horizLoc, int vertLoc, ushort occasion)
		{
			using var _ = this.Processor.Journal.Scope($"LaunchAudience({participantIdx}, {horizLoc}, {vertLoc}, {occasion})");

			this.Owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);
			this.Owner.MapOps.RepaintMapTile(horizLoc, vertLoc);

			this.audienceLastAudience = (ushort)Leader(participantIdx).TouchParticipantTimeleft;
			NameEmpress(participantIdx);

			if (occasion == 0)
				Leader(participantIdx).TouchParticipantTimeleft = this.Owner.World.RoundTally;

			King.EmissaryAudience = 0;
			this.audienceHostile = 0;
			King.Answer = occasion;
			this.audienceDanger = 0;
			this.audienceTribute = 0;
			this.audienceDemand = 1;
			King.AudienceWelcomed = 0;

			WeighContinents(participantIdx);
			WeighNuclearWeapons(participantIdx);
			WeighEnemies(participantIdx);

			bool ended = !ExchangeTechnologies(participantIdx, horizLoc, vertLoc, occasion);

			if (!ended)
			{
				if ((this.Owner.World.DiagMarkers & 1) == 0)
				{
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
				King.AudienceWelcomed = 0;

				WelcomeFriendship(participantIdx, horizLoc, vertLoc);
				King.AudienceWelcomed = 1;

				if ((Relation(participantIdx) & 2) != 0 && WelcomeEmissary(participantIdx, horizLoc, vertLoc) != 0xffff)
					NegotiateUnderPeace(participantIdx);

				if (this.audiencePeaceBroken != 0)
					this.Owner.World.Participants[participantIdx].Relations[this.Owner.World.PersonParticipantIdx] |= 0x100;

				this.Owner.World.Participants[participantIdx].Relations[this.Owner.World.PersonParticipantIdx] &= 0xffdf;
				this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].Relations[participantIdx] &= 0xffdf;

				AdviseAboutLeader(participantIdx);
			}

			if (King.Answer != 0xffff)
				FinishAudience();

			NameEmpressBack(participantIdx);

			King.EmissaryAudience = 0;
			King.Answer = 0xffff;
			King.Mood = 0xffff;
		}

		public void FinishAudience()
		{
			using var _ = this.Processor.Journal.Scope("FinishAudience()");

			ReleaseKingResources();

			DrawingBox.TypefaceIdx = 0x1;

			this.Owner.TurnHooks.RepaintMainScreen();

			King.Mood = 0xffff;
			King.Answer = 0;
			King.EmissaryAudience = King.Answer;
		}

		public ushort WelcomeEmissary(short participantIdx, int horizLoc, int vertLoc)
		{
			using var _ = this.Processor.Journal.Scope($"WelcomeEmissary({participantIdx}, {horizLoc}, {vertLoc})");

			if (King.EmissaryAudience == 0)
			{
				King.EmissaryAudience = 1;

				if (King.Answer == 0)
				{
					AskToReceiveEmissary(participantIdx, horizLoc, vertLoc);
				}

				if (King.Answer != 0xffff)
				{
					ReceiveEmissary(participantIdx);
				}
			}

			ushort answer = King.Answer;

			return answer;
		}

		public void ObtainKingSalutation(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainKingSalutation({participantIdx})");

			ushort leader = (ushort)((participantIdx << 1) + 0x19a2);
			ushort Regime() => Tables.GovernmentTitle(this.Owner.World.Participants[participantIdx].RegimeKind);

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
			this.Owner.Words.Append(Tables.CivilizationPlural(participantIdx));
			this.Owner.Words.Append("...\n");

			if (this.Owner.World.Participants[participantIdx].LiveTroops[25] != 0)
			{
				this.Owner.Words.Append("Our words are backed\nwith NUCLEAR WEAPONS!\n");
			}
		}

		public void LogAudience(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"LogAudience({participantIdx})");

			this.Owner.Politics.AssignDiplomacyFlags(this.Owner.World.PersonParticipantIdx, participantIdx, 2);

			this.Owner.World.Participants[participantIdx].TouchParticipantTimeleft = (short)(this.Owner.World.RoundTally + 16);

			if (this.Owner.World.HardnessTier == 0)
			{
				for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (!this.troops.Exists(participantIdx, troopIdx))
						continue;

					ushort townIdx = this.Owner.Helpers.FindNearestCity(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

					if (this.Owner.World.Towns[townIdx].ParticipantIdx == this.Owner.World.PersonParticipantIdx &&
						(short)Search.FoundDistance <= 2)
					{
						this.Owner.UnitOps.DestroyUnit(participantIdx, troopIdx);
					}
				}
			}
		}

		private static ushort TechnologyName(ushort scienceIdx) => GameTables.TechnologyText(scienceIdx);

		private GameModel.State.Player Leader(int participantIdx) => this.Owner.World.Participants[participantIdx];

		private ushort Relation(short participantIdx) => Leader(participantIdx).Relations[this.Owner.World.PersonParticipantIdx];

		private void NameEmpress(short participantIdx)
		{
			if (Leader(participantIdx).ChiefVisuals == 14)
			{
				this.Owner.Names.Set(Tables.GovernmentTitleAt(2), "Empress");
				this.Owner.Names.Set(Tables.GovernmentTitleAt(4), "Empress");
			}
		}

		private void NameEmpressBack(short participantIdx)
		{
			if (Leader(participantIdx).ChiefVisuals == 14)
			{
				this.Owner.Names.Set(Tables.GovernmentTitleAt(2), Turn.SavedGovernmentTitleMonarchy);
				this.Owner.Names.Set(Tables.GovernmentTitleAt(4), Turn.SavedGovernmentTitleCommunism);
			}
		}

		private ushort CivilizationNameAddr(short participantIdx)
		{
			return Tables.CivilizationName(participantIdx);
		}
	}
}
