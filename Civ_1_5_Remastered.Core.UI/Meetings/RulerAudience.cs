namespace Civ_1_5_Remastered.Core.UI.Meetings
{
	public partial class RulerAudience : GameComponent, IRulerAudience
	{
		private const ushort KingEyesHueAddress = 0x3a9a;
		private const ushort KingGestureHueFirstAddress = 0x3ab4;
		private const ushort KingGestureHueSecondAddress = 0x3ab7;
		private const ushort LeaderFontTableSegment = 0x36fa;
		private const ushort AdvisorKingVisualsSegment = 0x36fa;
		private const ushort AdvisorHorizontalOffsetsTableAddress = 0x3afe;
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
		private ushort audienceAppeased;

		public RulerAudience(IGameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		private ushort HostileValue => this.audienceHostile;
		private GameModel.State.Player Person => this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex];

		public void LaunchAudience(short participantIndex, int horizontalLocation, int verticalLocation, ushort occasion)
		{
			using var _ = this.Processor.Journal.Scope($"LaunchAudience({participantIndex}, {horizontalLocation}, {verticalLocation}, {occasion})");

			this.Owner.MapOperations.KeepKnownTileImprovements(horizontalLocation, verticalLocation);
			this.Owner.MapOperations.RepaintMapTile(horizontalLocation, verticalLocation);

			this.audienceLastAudience = (ushort)Leader(participantIndex).TouchParticipantTimeLeft;
			NameEmpress(participantIndex);

			if (occasion == 0)
			{
				Leader(participantIndex).TouchParticipantTimeLeft = this.Owner.World.RoundTally;
			}

			King.EmissaryAudience = 0;
			this.audienceHostile = 0;
			King.Answer = occasion;
			this.audienceDanger = 0;
			this.audienceTribute = 0;
			this.audienceDemand = 1;
			King.AudienceWelcomed = 0;
			this.audienceAppeased = 0;

			OfferReparations(participantIndex, horizontalLocation, verticalLocation);

			WeighContinents(participantIndex);
			WeighNuclearWeapons(participantIndex);
			WeighEnemies(participantIndex);

			bool ended = !ExchangeTechnologies(participantIndex, horizontalLocation, verticalLocation, occasion);

			if (!ended)
			{
				if ((this.Owner.World.DiagonalMarkers & 1) == 0)
				{
					this.audienceHostile = 1;
					this.audienceDemand = 0;
					this.audienceDanger = 0;
					this.audiencePeaceBroken = 0;
				}

				AskForAlliance(participantIndex, horizontalLocation, verticalLocation);
				DemandTribute(participantIndex, horizontalLocation, verticalLocation);
				DemandTechnology(participantIndex, horizontalLocation, verticalLocation);
				Threaten(participantIndex, horizontalLocation, verticalLocation, occasion);

				if (HostileValue == 0 && !OfferTreaty(participantIndex, horizontalLocation, verticalLocation, occasion))
				{
					DeclareWar(participantIndex);
				}

				NameEmpressBack(participantIndex);
				King.AudienceWelcomed = 0;

				WelcomeFriendship(participantIndex, horizontalLocation, verticalLocation);
				King.AudienceWelcomed = 1;

				if ((Relation(participantIndex) & 2) != 0 && WelcomeEmissary(participantIndex, horizontalLocation, verticalLocation) != 0xffff)
				{
					NegotiateUnderPeace(participantIndex);
				}

				if (this.audiencePeaceBroken != 0 && this.audienceAppeased == 0 && WouldBreakPeace(participantIndex))
				{
					this.Owner.World.Participants[participantIndex].Relations[this.Owner.World.PersonParticipantIndex] |= 0x100;
				}

				this.Owner.World.Participants[participantIndex].Relations[this.Owner.World.PersonParticipantIndex] &= 0xffdf;
				this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].Relations[participantIndex] &= 0xffdf;

				AdviseAboutLeader(participantIndex);
			}

			if (King.Answer != 0xffff)
			{
				FinishAudience();
			}

			NameEmpressBack(participantIndex);

			King.EmissaryAudience = 0;
			King.Answer = 0xffff;
			King.Mood = 0xffff;
		}

		public void FinishAudience()
		{
			using var _ = this.Processor.Journal.Scope("FinishAudience()");

			ReleaseKingResources();

			DrawingBox.TypefaceIndex = 0x1;

			this.Owner.TurnHooks.RepaintMainScreen();

			King.Mood = 0xffff;
			King.Answer = 0;
			King.EmissaryAudience = King.Answer;
		}

		public ushort WelcomeEmissary(short participantIndex, int horizontalLocation, int verticalLocation)
		{
			using var _ = this.Processor.Journal.Scope($"WelcomeEmissary({participantIndex}, {horizontalLocation}, {verticalLocation})");

			if (King.EmissaryAudience == 0)
			{
				King.EmissaryAudience = 1;

				if (King.Answer == 0)
				{
					AskToReceiveEmissary(participantIndex, horizontalLocation, verticalLocation);
				}

				if (King.Answer != 0xffff)
				{
					ReceiveEmissary(participantIndex);
				}
			}

			ushort answer = King.Answer;

			return answer;
		}

		public void ObtainKingSalutation(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainKingSalutation({participantIndex})");

			ushort leader = (ushort)((participantIndex << 1) + 0x19a2);
			ushort Regime() => Tables.GovernmentTitle(this.Owner.World.Participants[participantIndex].RegimeKind);

			this.Owner.Words.Set((this.Owner.World.Participants[participantIndex].Relations[this.Owner.World.PersonParticipantIndex] & 8) != 0 ? (ushort)0x39b1 : (ushort)0x39e8);

			switch (this.Owner.Runtime.Dice.DrawInteger(4))
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
			this.Owner.Words.Append(Tables.CivilizationPlural(participantIndex));
			this.Owner.Words.Append("...\n");

			if (this.Owner.World.Participants[participantIndex].LiveTroops[25] != 0)
			{
				this.Owner.Words.Append("Our words are backed\nwith NUCLEAR WEAPONS!\n");
			}
		}

		public void LogAudience(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"LogAudience({participantIndex})");

			this.Owner.Politics.AssignDiplomacyFlags(this.Owner.World.PersonParticipantIndex, participantIndex, 2);

			this.audienceAppeased = 1;

			this.Owner.World.Participants[participantIndex].TouchParticipantTimeLeft = (short)(this.Owner.World.RoundTally + 16);

			if (this.Owner.World.HardnessTier == 0)
			{
				for (short troopIndex = 0; troopIndex < UnitMemory.TroopTally; troopIndex++)
				{
					if (!this.troops.Exists(participantIndex, troopIndex))
					{
						continue;
					}

					ushort townIndex = this.Owner.Helpers.FindNearestCity(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal),
						this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical));

					if (this.Owner.World.Towns[townIndex].ParticipantIndex == this.Owner.World.PersonParticipantIndex &&
						(short)Search.FoundDistance <= 2)
					{
						this.Owner.UnitOperations.DestroyUnit(participantIndex, troopIndex);
					}
				}
			}
		}

		private static ushort TechnologyName(ushort scienceIndex) => GameTables.TechnologyText(scienceIndex);

		private GameModel.State.Player Leader(int participantIndex) => this.Owner.World.Participants[participantIndex];

		private ushort Relation(short participantIndex) => Leader(participantIndex).Relations[this.Owner.World.PersonParticipantIndex];

		/// <summary>
		/// A hostile ruler under a treaty only starts a secret plan to break it if he would act on it: he drops such
		/// a plan at once when the player is much stronger and he holds no vendetta (see DiplomacyRules.WeighBreakingPeace).
		/// </summary>
		private bool WouldBreakPeace(short participantIndex)
		{
			short personIndex = this.Owner.World.PersonParticipantIndex;
			GameModel.State.Player leader = Leader(participantIndex);

			return DiplomacyRules.WeighBreakingPeace(
				DiplomacyRules.PeaceUnits(this.Owner.World.Participants, participantIndex, personIndex),
				leader.MartialMight,
				Person.MartialMight,
				(Relation(participantIndex) & DiplomacyRules.VendettaRelation) != 0) >= 0;
		}

		private void NameEmpress(short participantIndex)
		{
			if (Leader(participantIndex).ChiefVisuals == 14)
			{
				this.Owner.Names.Set(Tables.GovernmentTitleAt(2), "Empress");
				this.Owner.Names.Set(Tables.GovernmentTitleAt(4), "Empress");
			}
		}

		private void NameEmpressBack(short participantIndex)
		{
			if (Leader(participantIndex).ChiefVisuals == 14)
			{
				this.Owner.Names.Set(Tables.GovernmentTitleAt(2), Turn.SavedGovernmentTitleMonarchy);
				this.Owner.Names.Set(Tables.GovernmentTitleAt(4), Turn.SavedGovernmentTitleCommunism);
			}
		}

		private ushort CivilizationNameAddress(short participantIndex)
		{
			return Tables.CivilizationName(participantIndex);
		}
	}
}
