using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;

namespace Civilization1Remaster.UI
{
	public class MeetWithKing : GameModule
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

		public MeetWithKing(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>
		/// An audience of the human player with the leader of a computer player (arg4 0: the player asked for it, the
		/// computer player came otherwise, 0xffff from a diplomat). The leader weighs the strength on the continents, nuclear
		/// weapons, wonders and enemies, then may exchange technologies, ask for an alliance against a common enemy, demand
		/// tribute or a technology, threaten, offer peace or a cease-fire, or ask the human player to declare war on someone.
		/// The locals stay in the frame (functions called later from the same depth may read what is left there).
		/// </summary>
		public void LaunchAudience(short participantIdx, int horizLoc, int vertLoc, ushort arg4)
		{
			this.processor.Journal.OpenScope($"LaunchAudience({participantIdx}, {horizLoc}, {vertLoc}, {arg4})");

			this.owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);
			this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);

			this.audienceLastAudience = (ushort)Leader(participantIdx).TouchParticipantTimeleft;
			NameEmpress(participantIdx);

			if (arg4 == 0)
				Leader(participantIdx).TouchParticipantTimeleft = this.owner.World.RoundTally;

			EmissaryAudience = 0;
			this.audienceHostile = 0;
			KingAnswer = arg4;
			this.audienceDanger = 0;
			this.audienceTribute = 0;
			this.audienceDemand = 1;
			AudienceWelcomed = 0;

			WeighContinents(participantIdx);
			WeighNuclearWeapons(participantIdx);
			WeighEnemies(participantIdx);

			bool ended = !ExchangeTechnologies(participantIdx, horizLoc, vertLoc, arg4);

			if (!ended)
			{
				if ((this.owner.World.DiagMarkers & 1) == 0)
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
				Threaten(participantIdx, horizLoc, vertLoc, arg4);

				if (HostileValue == 0 && !OfferTreaty(participantIdx, horizLoc, vertLoc, arg4))
					DeclareWar(participantIdx);

				NameEmpressBack(participantIdx);
				AudienceWelcomed = 0;

				WelcomeFriendship(participantIdx, horizLoc, vertLoc);
				AudienceWelcomed = 1;

				if ((Relation(participantIdx) & 2) != 0 && WelcomeEmissary(participantIdx, horizLoc, vertLoc) != 0xffff)
					NegotiateUnderPeace(participantIdx);

				if (this.audiencePeaceBroken != 0)
					this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] |= 0x100;

				this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] &= 0xffdf;
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx] &= 0xffdf;

				AdviseAboutLeader(participantIdx);
			}

			if (KingAnswer != 0xffff)
				FinishAudience();

			NameEmpressBack(participantIdx);

			EmissaryAudience = 0;
			KingAnswer = 0xffff;
			KingMood = 0xffff;
			this.processor.Journal.CloseScope("LaunchAudience");
		}

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

		private ushort HostileValue => this.audienceHostile;

		private Game.State.Player Leader(int participantIdx) => this.owner.World.Participants[participantIdx];

		private Game.State.Player Person => this.owner.World.Participants[this.owner.World.PersonParticipantIdx];

		/// <summary>The relation of the computer player to the human player</summary>
		private ushort Relation(short participantIdx) => Leader(participantIdx).Relations[this.owner.World.PersonParticipantIdx];

		/// <summary>The leader 14 is an empress: her titles for the audience</summary>
		private void NameEmpress(short participantIdx)
		{
			if (Leader(participantIdx).ChiefVisuals == 14)
			{
				this.owner.Libc.CopyText(GovernmentTitleAt(2), "Empress");
				this.owner.Libc.CopyText(GovernmentTitleAt(4), "Empress");
			}
		}

		private void NameEmpressBack(short participantIdx)
		{
			if (Leader(participantIdx).ChiefVisuals == 14)
			{
				this.owner.Libc.CopyText(GovernmentTitleAt(2), SavedGovernmentTitleMonarchy);
				this.owner.Libc.CopyText(GovernmentTitleAt(4), SavedGovernmentTitleCommunism);
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
					short factor = (short)(this.owner.World.HardnessTier == 0 ? 4 : 2);
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
			short personIdx = this.owner.World.PersonParticipantIdx;

			bool threat = person.Standing == 7 && person.TownTally > 4 && leader.TownTally > 1 && person.LiveTroops[25] == 0 &&
				this.owner.World.RoundTally > 200;
			this.audienceNuclearThreat = (ushort)(threat ? 1 : 0);

			if ((threat || ((Relation(participantIdx) & 8) != 0 && (short)(3 * leader.MartialMight) > person.MartialMight)) &&
				person.LiveTroops[25] == 0)
			{
				this.audienceHostile = 1;
				this.audienceTribute = (ushort)this.owner.Helpers.VerifyValueSpan((short)this.audienceTribute, 10 * leader.Standing, 9999);
				this.audienceDanger = 0;
			}

			if (leader.LiveTroops[25] != 0)
				leader.Relations[personIdx] |= 0x80;

			if (leader.LiveTroops[25] != 0 && person.LiveTroops[25] == 0)
			{
				this.audienceHostile = 1;
				this.audienceTribute = (ushort)this.owner.Helpers.VerifyValueSpan((short)this.audienceTribute, 100, 9999);
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

			if (this.owner.CityOps.PlayerEnjoysWonderEffect(personIdx, 7) != 0 || this.owner.CityOps.PlayerEnjoysWonderEffect(personIdx, 0x12) != 0)
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

			this.audienceTribute = (ushort)((ushort)this.owner.Helpers.VerifyValueSpan(((short)this.audienceTribute * (this.owner.World.HardnessTier + 1)) / 32, 0, 20) * 0x32);

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
			short personIdx = this.owner.World.PersonParticipantIdx;

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

			ushort character = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(0x1512 + 0x3a * leader.ChiefVisuals));
			attitude = (short)(attitude - character);

			if (Person.MartialMight < leader.MartialMight)
				attitude--;

			short calm = (short)((Relation(participantIdx) & 2) == 0 ? 1 : 0);

			if (calm < attitude && this.audienceNuclearThreat == 0)
			{
				this.audienceHostile = 0;

				if (this.owner.Libc.Dice.DrawInt(2) != 0)
					this.audienceTribute = 0;
			}

			if (leader.RegimeKind >= 4 && HostileValue != 0 && (Relation(participantIdx) & 8) == 0)
			{
				if ((short)(3 * Person.MartialMight) > (short)(leader.MartialMight << 1) && this.audienceNuclearThreat == 0)
					this.audienceHostile = 0;
				else
					this.owner.Politics.SwitchGovernment(participantIdx, 2);
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

		/// <summary>The name of a technology (records of 0x16 bytes from 0x4da)</summary>
		private static ushort TechnologyName(ushort scienceIdx) => TechnologyText(scienceIdx);

		/// <summary>
		/// The technologies each side knows (0xe, 0x32), one the leader knows and the human player doesn't (0xa), the most
		/// valuable one the human player knows and the leader doesn't (0xc, value 0x2c); a friendly leader with fewer
		/// technologies offers an exchange, again after each one. False: the audience ends here (a friendly leader at peace
		/// in danger).
		/// </summary>
		private bool ExchangeTechnologies(short participantIdx, int horizLoc, int vertLoc, ushort arg4)
		{
			short personIdx = this.owner.World.PersonParticipantIdx;

			while (true)
			{
				this.audienceOfferedTechnology = 0xffff;
				this.audienceWantedTechnology = 0xffff;
				short offeredValue = 0;
				short leaderTechnologies = 0;
				short personTechnologies = 0;

				for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
				{
					if (this.owner.Advisors.PlayerKnowsTechnology(personIdx, scienceIdx) != 0)
						personTechnologies++;

					if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
						leaderTechnologies++;

					if (this.owner.Advisors.PlayerKnowsTechnology(personIdx, scienceIdx) == 0 &&
						this.owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
					{
						this.audienceWantedTechnology = (ushort)scienceIdx;
					}

					if (this.owner.Advisors.PlayerKnowsTechnology(personIdx, scienceIdx) != 0 &&
						this.owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) == 0 &&
						(short)this.owner.Advisors.ObtainTechnologyInfo(participantIdx, (ushort)scienceIdx) >= offeredValue)
					{
						this.audienceOfferedTechnology = (ushort)scienceIdx;
						offeredValue = (short)this.owner.Advisors.ObtainTechnologyInfo(participantIdx, (ushort)scienceIdx);
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
				this.owner.Words.Clear();
				this.owner.Libc.CopyText(CodeText4, TechnologyName(this.audienceWantedTechnology));
				this.owner.BlockText.DisplayTextDialog(0x3776);
				KingSpeaking = 0xc;

				if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
					break;

				this.owner.Startup.ConcealMouseCursor();
				this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);
				this.owner.Startup.DisplayMouseCursor();

				KingMood = 0xffff;
				CivilopediaWithoutPictures = 1;
				this.owner.Conquest.ChooseTechnologyToTake(participantIdx, personIdx);
				RestoreScreenStyle();

				CivilopediaWithoutPictures = 1;
				this.owner.Conquest.ChooseTechnologyToTake(personIdx, participantIdx);
				this.owner.Helpers.HaltHueCycling();
				RestoreScreenStyle();

				this.audienceHostile = 0;
				KingMood = 1;

				this.owner.Startup.ConcealMouseCursor();
				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);
				this.owner.Startup.DisplayMouseCursor();
			}

			// A friendly leader at peace with a human player strong on its continents doesn't negotiate
			return !(HostileValue == 0 && arg4 == 0 && (Relation(participantIdx) & 2) == 2 && (short)this.audienceDanger > 2);
		}

		/// <summary>The style of screen 0 (word 0x10 of it) back from 0x67ec</summary>
		private void RestoreScreenStyle()
		{
			ushort screen = ScreenBox;
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(screen + 0x10), SavedScreenStyle);
		}

		/// <summary>The leader asks for an alliance against the common enemy (0x30): accepted, the human player breaks the peace with it</summary>
		private void AskForAlliance(short participantIdx, int horizLoc, int vertLoc)
		{
			if (this.audienceCommonEnemy == 0xffff || WelcomeEmissary(participantIdx, horizLoc, vertLoc) == 0xffff || this.audienceDemand == 0)
				return;

			this.owner.Words.Clear();
			this.owner.Libc.CopyText(CodeText2,
				CivilizationPlural(this.audienceCommonEnemy));
			this.owner.Libc.CopyText(CodeText4, (ushort)(HostileValue != 0 ? 0x3780 : 0x3787));
			this.owner.BlockText.DisplayTextDialog(0x378f);

			KingMood = (ushort)(HostileValue != 0 ? 2 : 1);
			KingSpeaking = 0xc;

			if (HostileValue != 0)
				this.owner.Services.PerformSound(0x21, 0);

			if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
				return;

			this.audienceHostile = 0;

			if (this.owner.Libc.Dice.DrawInt(2) != 0)
				this.audienceTribute = 0;

			short enemyIdx = (short)this.audienceCommonEnemy;
			this.owner.Politics.WipeDiplomacyFlags(this.owner.World.PersonParticipantIdx, enemyIdx, 2);
			this.owner.World.Participants[(ushort)enemyIdx].Relations[this.owner.World.PersonParticipantIdx] |= 8;
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
			this.owner.Libc.CopyText(CodeText3, TextBuffer.Address);
			this.owner.Words.Clear();
			this.owner.BlockText.DisplayTextDialog(0x3797);

			AudienceWelcomed = 0;
			KingMood = 2;
			KingSpeaking = 0xc;
			this.owner.Services.PerformSound(0x21, 0);

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

			this.owner.Words.Clear();
			this.owner.Libc.CopyText(CodeText4, TechnologyName(this.audienceOfferedTechnology));
			this.owner.BlockText.DisplayTextDialog(0x37a0);

			AudienceWelcomed = 0;
			KingMood = 2;
			KingSpeaking = 0xc;
			this.owner.Services.PerformSound(0x21, 0);

			if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) == 1)
			{
				this.owner.Advisors.EarnTechnology(participantIdx, (short)this.audienceOfferedTechnology, this.owner.World.PersonParticipantIdx);
				this.audienceHostile = 0;
			}

			this.audienceTribute = 0x3e7;
		}

		/// <summary>
		/// A hostile leader threatens: at peace and with a big tribute left, after a refused demand, or (at war: only when it
		/// came) otherwise; with an emissary the threat is a dialog, without one a message; the peace treaty ends
		/// </summary>
		private void Threaten(short participantIdx, int horizLoc, int vertLoc, ushort arg4)
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
				if (HostileValue == 0 || ((Relation(participantIdx) & 1) != 0 && arg4 == 0))
					return;

				welcome = WelcomeEmissary(participantIdx, horizLoc, vertLoc) != 0xffff;
				text = (ushort)(welcome ? 0x37c3 : 0x37c8);
			}

			if (welcome)
				this.owner.Services.PerformSound(0x21, 0);

			this.owner.Words.Clear();
			this.owner.BlockText.DisplayTextDialog(text);
			this.owner.BlockText.ArrangeTextRow(0x28);

			if (welcome)
			{
				AudienceWelcomed = 1;
				KingMood = 2;
				KingSpeaking = 0xc;
				DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
			}
			else
			{
				this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 40, 100);
			}

			this.owner.Politics.WipeDiplomacyFlags(this.owner.World.PersonParticipantIdx, participantIdx, 2);
		}

		/// <summary>
		/// A calm leader (not at peace, or the human player asked for the audience) offers peace: accepted, the treaty is
		/// signed for 16 turns; under a democracy or communism the senate signs anyway; else the peace ends, and a leader in
		/// danger offers a technology, gold for peace, or (in its last city) everything it has for peace. True: done.
		/// </summary>
		private bool OfferTreaty(short participantIdx, int horizLoc, int vertLoc, ushort arg4)
		{
			short personIdx = this.owner.World.PersonParticipantIdx;

			if (((Relation(participantIdx) & 2) != 0 && arg4 == 0) || WelcomeEmissary(participantIdx, horizLoc, vertLoc) == 0xffff)
				return true;

			this.owner.Words.Clear();
			this.owner.BlockText.DisplayTextDialog(0x37cd);
			AudienceWelcomed = 0;
			KingMood = 0;
			KingSpeaking = 0xc;

			if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) == 1)
			{
				DisplayKingMessage(participantIdx);
				LogAudience(participantIdx);
				Leader(participantIdx).TouchParticipantTimeleft = (short)(this.owner.World.RoundTally + 16);
				return true;
			}

			if (Person.RegimeKind >= 4)
			{
				this.owner.Words.Set("You are overruled by the Senate.\nTreaty signed.\n");
				DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
				LogAudience(participantIdx);
				return true;
			}

			if ((Relation(participantIdx) & 2) != 0)
				this.owner.Politics.WipeDiplomacyFlags(personIdx, participantIdx, 2);

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
			this.owner.Libc.CopyText(CodeText4, TechnologyName(this.audienceWantedTechnology));
			this.owner.Words.Clear();
			this.owner.BlockText.DisplayTextDialog(0x3805);
			KingMood = 0;
			KingSpeaking = 0xc;

			if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
				return;

			this.owner.Startup.ConcealMouseCursor();
			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);
			this.owner.Startup.DisplayMouseCursor();
			this.owner.Helpers.HaltHueCycling();

			KingMood = 0xffff;
			CivilopediaWithoutPictures = 1;
			this.owner.Advisors.EarnTechnology(this.owner.World.PersonParticipantIdx, (short)this.audienceWantedTechnology, participantIdx);
			RestoreScreenStyle();
			this.owner.Helpers.HaltHueCycling();
			KingMood = 0;

			this.owner.Startup.ConcealMouseCursor();
			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);
			this.owner.Startup.DisplayMouseCursor();

			DisplayKingMessage(participantIdx);
			LogAudience(participantIdx);
			this.audienceLastAudience = (ushort)this.owner.World.RoundTally;
		}

		/// <summary>Gold for peace (0x22: twice the danger - 4, in fifties, up to the leader's gold)</summary>
		private void OfferGoldForPeace(short participantIdx)
		{
			ushort amount = (ushort)((ushort)this.owner.Helpers.VerifyValueSpan(((short)this.audienceDanger * 2) - 4, 0, Leader(participantIdx).Gold / 50) * 0x32);

			if (amount == 0)
				return;

			EmptyMessage()
				.Number((short)amount);
			this.owner.Libc.CopyText(CodeText3, TextBuffer.Address);
			this.owner.Words.Clear();
			this.owner.BlockText.DisplayTextDialog(0x380e);
			KingMood = 0;
			KingSpeaking = 0xc;

			if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
				return;

			DisplayKingMessage(participantIdx);
			LogAudience(participantIdx);
			Person.Gold += (short)amount;

			// A translation slip: the gold is taken from the word at SI (the leader << 4), not from the leader
			ushort taken = (ushort)(participantIdx << 4);
			this.processor.EmitUInt16(this.processor.SegData.Value16, taken, (ushort)(this.processor.PullUInt16(this.processor.SegData.Value16, taken) - amount));
			this.audienceLastAudience = (ushort)this.owner.World.RoundTally;
		}

		/// <summary>A leader in danger in its last city gives all its gold and technologies for peace</summary>
		private void OfferEverythingForPeace(short participantIdx)
		{
			short personIdx = this.owner.World.PersonParticipantIdx;

			EmptyMessage()
				.Number(Leader(participantIdx).Gold);
			this.owner.Libc.CopyText(CodeText3, TextBuffer.Address);
			this.owner.Words.Clear();
			this.owner.BlockText.DisplayTextDialog(0x3818);
			KingMood = 0;
			KingSpeaking = 0xc;

			if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
				return;

			DisplayKingMessage(participantIdx);
			LogAudience(participantIdx);

			Person.Gold += Leader(participantIdx).Gold;
			Leader(participantIdx).Gold = 0;

			this.owner.Startup.ConcealMouseCursor();
			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);
			this.owner.Startup.DisplayMouseCursor();
			this.owner.Helpers.HaltHueCycling();
			KingMood = 0xffff;

			for (short scienceIdx = 0; scienceIdx < 0x48; scienceIdx++)
			{

				if (this.owner.Advisors.PlayerKnowsTechnology(personIdx, scienceIdx) == 0 &&
					this.owner.Advisors.PlayerKnowsTechnology(participantIdx, scienceIdx) != 0)
				{
					CivilopediaWithoutPictures = 1;
					this.owner.Advisors.EarnTechnology(personIdx, scienceIdx, participantIdx);
				}
			}

			RestoreScreenStyle();
			this.owner.Helpers.HaltHueCycling();
			KingMood = 0;

			this.owner.Startup.ConcealMouseCursor();
			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);
			this.owner.Startup.DisplayMouseCursor();

			this.audienceLastAudience = (ushort)this.owner.World.RoundTally;
		}

		/// <summary>Without a peace treaty the leader declares war</summary>
		private void DeclareWar(short participantIdx)
		{
			if ((Relation(participantIdx) & 2) != 0)
				return;

			this.owner.Services.PerformSound(0x21, 0);
			this.owner.Words.Clear();
			this.owner.BlockText.DisplayTextDialog(0x3820);
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
				this.owner.Words.Clear();
				this.owner.BlockText.DisplayTextDialog(0x382a);
			}
			else
			{
				short personIdx = this.owner.World.PersonParticipantIdx;

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
			short personIdx = this.owner.World.PersonParticipantIdx;

			while (true)
			{
				this.owner.Libc.CopyText(CodeText4,
					CivilizationPlural(participantIdx));
				this.owner.Words.Clear();
				this.owner.BlockText.DisplayTextDialog(0x388f);

				// Choices disabled: 2 (a demand) shortly after the last audience or under a democracy, 1 (an attack) with only two players left
				if ((short)(this.owner.World.RoundTally - this.audienceLastAudience) < 0x10 || Person.RegimeKind >= 4)
					DisabledLines = 4;

				if (((1 << participantIdx) | (1 << (byte)personIdx)) == (ushort)this.owner.World.LiveRealms)
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
			short personIdx = this.owner.World.PersonParticipantIdx;

			short amount = (short)((ushort)this.owner.Helpers.VerifyValueSpan((short)this.audienceDanger, 0, Leader(participantIdx).Gold / 50) * 0x32);

			if (amount != 0)
			{
				EmptyMessage()
					.Number(amount);
				this.owner.Libc.CopyText(CodeText3, TextBuffer.Address);
				this.owner.Words.Clear();
				this.owner.BlockText.DisplayTextDialog(0x3897);
				KingMood = 0;
				KingSpeaking = 0xc;

				// The human player gets the gold amount (classic translation slip gave the dialog button answer 0 or 1)
				ushort answer = DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
				short goldGiven = this.owner.TurnHooks.ClassicTributeDemand ? (short)answer : amount;
				Person.Gold += goldGiven;
				Leader(participantIdx).Gold -= amount;
				Leader(participantIdx).TouchParticipantTimeleft = this.owner.World.RoundTally;
				return;
			}

			if (this.audienceWasHostile != 0)
			{
				this.owner.Services.PerformSound(0x21, 0);
				this.owner.Words.Clear();
				this.owner.BlockText.DisplayTextDialog(0x38a1);
				this.owner.Politics.WipeDiplomacyFlags(personIdx, participantIdx, 2);
				KingMood = 2;
				KingSpeaking = 0xc;
			}
			else
			{
				this.owner.Words.Set("We ignore your feeble threats.\n");
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
			short personIdx = this.owner.World.PersonParticipantIdx;

			this.owner.Words.Set("You must attack the evil:\n ");
			short[] enemyTable = new short[8];
			int enemyCount = 0;

			for (short otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				if (otherIdx == personIdx || otherIdx == participantIdx || (this.owner.World.LiveRealms & (1 << (byte)otherIdx)) == 0)
					continue;

				enemyTable[enemyCount++] = otherIdx;

				this.owner.Words.Append(CivilizationPlural(otherIdx));
				this.owner.Words.Append("\n ");
			}

			ushort choice = DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
			KingMood = 1;

			if (choice == 0xffff)
				return false;

			short enemyIdx = enemyTable[choice];

			this.owner.Libc.CopyText(CodeText4,
				CivilizationPlural(enemyIdx));
			KingSpeaking = 4;

			if ((Leader(participantIdx).Relations[enemyIdx] & 1) == 0)
			{
				this.owner.Words.Clear();
				this.owner.BlockText.DisplayTextDialog(0x38e9);
				DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
				return false;
			}

			if ((Leader(participantIdx).Relations[enemyIdx] & 2) == 0)
			{
				this.owner.Words.Clear();
				this.owner.BlockText.DisplayTextDialog(0x38f4);
				DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);
				return true;
			}

			short amount = (short)((ushort)this.owner.Helpers.VerifyValueSpan(((Leader(enemyIdx).MartialMight / 2) * (Leader(participantIdx).Gold / 100)) / 50, 2, 9999) * 0x32);

			EmptyMessage()
				.Number(amount);
			this.owner.Libc.CopyText(CodeText3, TextBuffer.Address);
			this.owner.Words.Clear();
			this.owner.BlockText.DisplayTextDialog(0x38fb);

			if (DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b) != 1)
				return false;

			if (Person.Gold < amount)
			{
				this.owner.Words.Clear();
				this.owner.BlockText.DisplayTextDialog(0x391e);
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

			this.owner.Libc.CopyText(CodeText2,
				CivilizationPlural(participantIdx));
			this.owner.HelpSystem.DisplayHelpPopup(0x392b);
		}

		/// <summary>
		/// ?
		/// </summary>
		public void FinishAudience()
		{
			this.processor.Journal.OpenScope("FinishAudience()");

			// function body
			ReleaseKingResources();

			DrawingBox.TypefaceIdx = 0x1;

			this.owner.TurnHooks.RepaintMainScreen();

			KingMood = 0xffff;
			KingAnswer = 0;
			EmissaryAudience = KingAnswer;

			// Far return
			this.processor.Journal.CloseScope("FinishAudience");
		}

		/// <summary>
		/// The audience with the player's emissary, once a turn (0xd804): unless the answer 0x3934 is known, shows the map at
		/// the position (horizLoc -1: not moved) and asks whether to receive the emissary (with "Check Intelligence" when the human player
		/// has relations bit 0x40 towards the player, which shows the report and asks again); 0x3934 is minus the choice. Unless
		/// refused (-1), shows the king and his greeting in the player's font (0x36fa).
		/// </summary>
		/// <returns>0x3934, also in AX</returns>
		public ushort WelcomeEmissary(short participantIdx, int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"WelcomeEmissary({participantIdx}, {horizLoc}, {vertLoc})");

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

			this.processor.Journal.CloseScope("WelcomeEmissary");

			return answer;
		}

		/// <summary>The question of WelcomeEmissary: 0x3934 becomes minus the chosen line (-2 shows the intelligence report first)</summary>
		private void AskToReceiveEmissary(short participantIdx, int horizLoc, int vertLoc)
		{
			if (horizLoc != -1)
			{
				this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx, horizLoc - 8, vertLoc - 2);
			}

			while (true)
			{
				Message("An emissary from the\n")
					.Text(CivilizationPlural(participantIdx))
					.Text(" wishes to\nspeak with you.\n")
					.Text("Will you receive him?\n Yes.\n No.\n");

				if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx] & 0x40) != 0)
				{
					this.owner.Words.Append(" Check Intelligence\n");
				}

				this.owner.Services.PerformSound(LeaderWord(this.owner.World.Participants[participantIdx].ChiefVisuals, LeaderTable.ShortTune), 3);

				this.owner.Startup.DisplayMouseCursorFully();

				KingAnswer = (ushort)-this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80);

				this.owner.Startup.ConcealMouseCursorToSavedLevel();

				if (KingAnswer != 0xfffe)
					break;

				this.owner.Briefings.DisplayIntelligenceReport(participantIdx);
			}
		}

		/// <summary>The audience of WelcomeEmissary: the king's screen, his greeting and dialog</summary>
		private void ReceiveEmissary(short participantIdx)
		{
			this.owner.Startup.ConcealMouseCursor();

			DisplayKingScreen(participantIdx);

			this.owner.Services.PerformSound(LeaderWord(this.owner.World.Participants[participantIdx].ChiefVisuals, LeaderTable.Tune), 3);

			this.owner.Startup.DisplayMouseCursor();

			// The font of the leader (table at 0x36fa), font 3 instead of 10
			ushort typeface = (ushort)((sbyte)this.processor.PullUInt8(LeaderFontTableSegment, (ushort)this.owner.World.Participants[participantIdx].ChiefVisuals) + 8);
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

		/// <summary>
		/// The greeting of the king at TextBuffer.Address: the opening (0x39b1 with relations bit 8 towards the human player, else 0x39e8),
		/// one of 4 random titles with the government (0x19b2) and the leader's name (0x19a2), "of the" civilization (0x1992),
		/// and the nuclear threat when the player has nuclear units (type 25)
		/// </summary>
		public void ObtainKingSalutation(short participantIdx)
		{
			this.processor.Journal.OpenScope($"ObtainKingSalutation({participantIdx})");

			ushort leader = (ushort)((participantIdx << 1) + 0x19a2);
			ushort Regime() => GovernmentTitle(this.owner.World.Participants[participantIdx].RegimeKind);

			this.owner.Words.Set((this.owner.World.Participants[participantIdx].Relations[this.owner.World.PersonParticipantIdx] & 8) != 0 ? (ushort)0x39b1 : (ushort)0x39e8);

			switch (this.owner.Libc.Dice.DrawInt(4))
			{
				case 0:
					this.owner.Words.Append("the most exalted\n");
					this.owner.Words.Append(Regime());
					this.owner.Words.Append(": ");
					this.owner.Words.Append(this.processor.PullGlobalWord(leader));
					this.owner.Words.Append(" ");
					break;

				case 1:
					this.owner.Words.Append(this.processor.PullGlobalWord(leader));
					this.owner.Words.Append(", ruler\nand ");
					this.owner.Words.Append(Regime());
					this.owner.Words.Append(" ");
					break;

				case 2:
					this.owner.Words.Append("our most wise\n");
					this.owner.Words.Append(Regime());
					this.owner.Words.Append(": ");
					this.owner.Words.Append(this.processor.PullGlobalWord(leader));
					this.owner.Words.Append(" ");
					break;

				case 3:
					this.owner.Words.Append("he who makes mortals\ntremble: ");
					this.owner.Words.Append(this.processor.PullGlobalWord(leader));
					this.owner.Words.Append(" ");
					break;
			}

			this.owner.Words.Append("of the ");
			this.owner.Words.Append(CivilizationPlural(participantIdx));
			this.owner.Words.Append("...\n");

			if (this.owner.World.Participants[participantIdx].LiveTroops[25] != 0)
			{
				this.owner.Words.Append("Our words are backed\nwith NUCLEAR WEAPONS!\n");
			}

			this.processor.Journal.CloseScope("ObtainKingSalutation");
		}

		/// <summary>
		/// The message of the leader of the player to the human player (text 0x3a8c, with the names of both civilizations from
		/// the table at 0x1982 put into the text parameters 0x30be and 0x30b8)
		/// </summary>
		public void DisplayKingMessage(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayKingMessage({participantIdx})");

			this.owner.Libc.CopyText(CodeText4, CivilizationNameAddr(participantIdx));

			this.owner.Libc.CopyText(CodeText1, CivilizationNameAddr(this.owner.World.PersonParticipantIdx));

			KingSpeaking = 0xc;
			this.owner.Words.Clear();

			this.owner.BlockText.DisplayTextDialog(0x3a8c);

			DisplayKingDialog(TextBuffer.Address, 0x14, 0x8b);

			this.processor.Journal.CloseScope("DisplayKingMessage");
		}

		private ushort CivilizationNameAddr(short participantIdx)
		{
			return CivilizationName(participantIdx);
		}

		/// <summary>
		/// After an audience: the player and the human have met (diplomacy flag 2), the player waits 16 turns before the next
		/// contact. At the easiest level the units of the player whose nearest city is one of the human's, at most 2 away, are
		/// removed.
		/// </summary>
		public void LogAudience(short participantIdx)
		{
			this.processor.Journal.OpenScope($"LogAudience({participantIdx})");

			this.owner.Politics.AssignDiplomacyFlags(this.owner.World.PersonParticipantIdx, participantIdx, 2);

			this.owner.World.Participants[participantIdx].TouchParticipantTimeleft = (short)(this.owner.World.RoundTally + 16);

			if (this.owner.World.HardnessTier == 0)
			{
				for (short troopIdx = 0; troopIdx < UnitMemory.TroopTally; troopIdx++)
				{
					if (!this.troops.Exists(participantIdx, troopIdx))
						continue;

					ushort townIdx = this.owner.Helpers.LocateNearestCity(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
						this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

					if (this.owner.World.Towns[townIdx].ParticipantIdx == this.owner.World.PersonParticipantIdx &&
						(short)FoundDistance <= 2)
					{
						this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);
					}
				}
			}

			this.processor.Journal.CloseScope("LogAudience");
		}

		/// <summary>
		/// One frame of the king's animation. While he speaks (0xd76c counts down) and stands still (step 0x67f4 = 0), the mouth
		/// moves: eyes 5 or -1, mouth 0, 9 or (every 4th frame) 8, then a pause of 4 frames (0x67f6). Otherwise the step goes on
		/// along table 0x3ab8 (positive: next step, -1, -2, -3: back to step 0, 6, 14); at the end of a gesture (0) the mood
		/// 0x3936 picks the next one (0: step 3, the eyes colour 0x3a9a; 1: step 0; 2: step 12, the colours 0x3ab4 / 0x3ab7)
		/// and 0x7ef4 says which pose he holds (0, 1 at step 14, 2 at step 6); without a pause one time in 12 a still pose
		/// starts a short gesture. The eyes and the mouth come from the tables 0x3a94 and 0x3aa6.
		/// </summary>
		public void ChoreographKingSpeaking()
		{
			this.processor.Journal.OpenScope("ChoreographKingSpeaking()");

			// The final values of the locals stay in the stack frame, like in the original

			KingPose = 0xffff;

			short eyes, mouth;

			if (KingSpeaking != 0 && KingStep == 0)
			{
				eyes = KingStep != 0 ? (short)5 : (short)-1;

				mouth = this.owner.Libc.Dice.DrawInt(2) != 0 ? (short)9 : (short)0;

				if (((byte)KingSpeaking & 3) == 0)
					mouth = 8;

				KingSpeaking = (ushort)(KingSpeaking - 1);
				KingPause = 4;
			}
			else
			{
				AdvanceKingGesture();

				if (KingPause == 0 && this.owner.Libc.Dice.DrawInt(12) == 0 && KingGestureStep() == 0)
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

				eyes = (sbyte)this.processor.PullGlobalByte((ushort)(step + 0x3a94));
				mouth = (sbyte)this.processor.PullGlobalByte((ushort)(step + 0x3aa6));
			}

			if (KingPause != 0)
				KingPause = (ushort)(KingPause - 1);

			ChoreographKingLikeness((ushort)eyes, (ushort)mouth, KingPose);

			this.owner.TextPainter.AwaitTime(10);

			// Far return
			this.processor.Journal.CloseScope("ChoreographKingSpeaking");
		}

		/// <summary>The entry of table 0x3ab8 for the king's step 0x67f4</summary>
		private sbyte KingGestureStep()
		{
			return (sbyte)this.processor.PullGlobalByte((ushort)(KingStep + 0x3ab8));
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
					this.processor.EmitGlobalByte(KingEyesHueAddress, AudienceWelcomed != 0 ? (byte)4 : (byte)2);

					if (KingStep != 6)
						gesture = 3;
					break;

				case 1:
					if (KingStep != 0)
						gesture = 0;
					break;

				case 2:
					this.processor.EmitGlobalByte(KingGestureHue1Address, AudienceWelcomed != 0 ? (byte)0xb : (byte)7);
					this.processor.EmitGlobalByte(KingGestureHue2Address, AudienceWelcomed != 0 ? (byte)0xb : (byte)7);

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
		/// Opens the audience with the player's king (0x67f2): without the pictures (disk 2 missing and no preference bit 8) only
		/// a line "&lt;title&gt;: &lt;government&gt; of the &lt;civilization&gt;." over a motif (0x67f0 = 1). Otherwise the advisors of
		/// govtNa.pic (N the government / 2, 3 for government 3, "m" with technology 0x14, 0x67ee) into 0x804e, the king of
		/// kingNN.pic (NN = 0x3938 + ChiefVisuals, 0x67f8; kink on EGA for kings 0 and 3) into 0xb1e6, the throne room
		/// (ImportKingScenery), the 14 faces and mouths into 0x7efe / 0x7f00 and, when disk 2 answers, the 12 small advisors
		/// (0x67fa = 1). The file name is in the stack frame, like in the original.
		/// </summary>
		public void DisplayKingScreen(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayKingScreen({participantIdx})");


			AudiencePlayer = (ushort)participantIdx;
			AudienceWithoutPictures = 0;

			if ((this.owner.World.SessionPrefMarkers & 8) != 0)
			{
				this.owner.Startup.QueryForDisk(2, 1);
			}
			else
			{
				if (this.owner.Startup.QueryForDisk(2, 0) == 0)
				{
					DisplayKingLine(participantIdx);

					// Far return
					this.processor.Journal.CloseScope("DisplayKingScreen");
					return;
				}
			}

			string advisorsPic = NameAdvisorsPicture(participantIdx, true);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, advisorsPic, 0);

			for (int advisorIdx = 0; advisorIdx < 4; advisorIdx++)
			{
				this.processor.EmitGlobalWord((ushort)((6 * advisorIdx) + 0x804e), this.owner.VideoCard.ScreenToPicture(1, (ushort)((0x50 * advisorIdx) + 1), 0x65, 0x4e, 0x63));
			}

			short king = (sbyte)this.processor.PullGlobalByte((ushort)((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals + 0x3938));
			KingPicture = (ushort)king;

			this.owner.Startup.QueryForDisk((ushort)((KingPicture / 7) + 4), 1);

			king = (short)KingPicture;
			char tens = (char)('0' + (king / 10));
			char ones = (char)('0' + (king % 10));
			string kingPic = (VgaMode == 0 && (KingPicture == 0 || KingPicture == 3))
				? $"kink{tens}{ones}.pic"
				: $"king{tens}{ones}.pic";

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, kingPic, 0);

			KingScenePicture = this.owner.VideoCard.ScreenToPicture(1, 0xb5, 0x43, 0x8b, 0x85);

			ImportKingScenery(participantIdx);

			this.owner.Helpers.ReleaseResource(GovernmentPicture, 0x3aee);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, kingPic, 0);

			// The 14 faces with their mouths, 5 a row (the 9th and the 12th skip two places)
			short place = 0;
			ushort faceIdx;

			for (faceIdx = 0; faceIdx < 0xe; faceIdx++)
			{
				if (faceIdx == 8 || faceIdx == 0xb)
					place += 2;

				ushort horizLoc = (ushort)(((place % 5) * 0x3c) + 1);
				ushort vertLoc = (ushort)((place / 5) * 0x32);

				this.processor.EmitGlobalWord((ushort)((faceIdx << 2) + 0x7efe), this.owner.VideoCard.ScreenToPicture(1, horizLoc, (ushort)(vertLoc + 1), 0x3b, 0x14));

				this.processor.EmitGlobalWord((ushort)((faceIdx << 2) + 0x7f00), this.owner.VideoCard.ScreenToPicture(1, horizLoc, (ushort)(vertLoc + 0x15), 0x3b, 0x1d));

				place++;
			}

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

			if (this.owner.Startup.QueryForDisk(2, 0) != 0)
			{
				string secondAdvisorsPic = NameAdvisorsPicture(participantIdx, false);

				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, secondAdvisorsPic, 0);

				// The 12 small advisors, 3 a row
				ushort row, column = 0;

				for (row = 0; row < 4; row++)
				{
					for (column = 0; column < 3; column++)
					{
						this.processor.EmitGlobalWord((ushort)((column << 1) + (6 * row) + 0x804e), this.owner.VideoCard.ScreenToPicture(1, (ushort)((0x1d * column) + 0xa1), (ushort)((0x19 * row) + 1), 0x1c, 0x18));
					}
				}

				KingScreenKept = 1;

				this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8, BackScreenBox, 0, 0);

				this.owner.Startup.QueryForDisk(0, 1);

				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);
			}
			else
			{
				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

				KingScreenKept = 0;
			}

			KingPictureLoaded = 1;

			// Far return
			this.processor.Journal.CloseScope("DisplayKingScreen");
		}

		/// <summary>
		/// "govtNa.pic" in the buffer: N the government / 2 ('3' for government 3), "m" instead of "a" with technology 0x14
		/// (0x67ee, read from the player the first time)
		/// </summary>
		private string NameAdvisorsPicture(short participantIdx, bool first)
		{
			short regime = this.owner.World.Participants[participantIdx].RegimeKind;

			if (first)
			{
				KingModernPictures = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x14);
			}

			char regimeChar = regime == 3 ? '3' : (char)((regime / 2) + '0');
			char techChar = regime != 3 && KingModernPictures != 0 ? 'm' : 'a';
			return $"govt{regimeChar}{techChar}.pic";
		}

		/// <summary>The audience without pictures: "&lt;title&gt;: &lt;government&gt; of the &lt;civilization&gt;." over a motif</summary>
		private void DisplayKingLine(short participantIdx)
		{
			AudienceWithoutPictures = 1;

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Helpers.FloodBoxWithMotif(0, 128, 320, 72);

			this.owner.Dialogs.RenderShadowBox(0, 128, 319, 71, 15, 8);

			Message(LeaderName((ushort)participantIdx))
				.Text(": ")
				.Text(GovernmentTitle((ushort)this.owner.World.Participants[participantIdx].RegimeKind))
				.Text(" of the ")
				.Text(CivilizationPlural((ushort)participantIdx))
				.Text(".");

			this.owner.TextPainter.RenderTextToScreen0(TextBuffer.Address, 8, 130, 15);

			this.owner.Startup.DisplayMouseCursor();
		}

		/// <summary>
		/// Unless 0x67f0 is set: shows the throne room of the player, the background "backNa.pic" (N is the government / 2, 3 for
		/// government 3, "m" instead of "a" when 0x67ee is set) with (Standing + 1) / 2 figures (columns at 0x3afe, pictures at
		/// 0x804e) and the king (0xb1e6), with the colours of "kingNN.pal" (NN is 0x67f8) and of the background. The file names are
		/// made in buffers in the stack frame, like in the original.
		/// </summary>
		public void ImportKingScenery(short participantIdx)
		{
			this.processor.Journal.OpenScope($"ImportKingScenery({participantIdx})");

			if (AudienceWithoutPictures == 0)
			{
				this.owner.Startup.ConcealMouseCursor();

				short regime = this.owner.World.Participants[participantIdx].RegimeKind;
				char regimeChar = regime == 3 ? '3' : (char)((regime / 2) + '0');
				char techChar = regime != 3 && KingModernPictures != 0 ? 'm' : 'a';
				string sceneryPic = $"back{regimeChar}{techChar}.pic";

				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, sceneryPic, 0);

				for (int figureIdx = 0; (short)((this.owner.World.Participants[participantIdx].Standing + 1) >> 1) > figureIdx; figureIdx++)
				{
					this.owner.Services.RenderPictureToScreen(BackScreenBox,
						(short)this.processor.PullGlobalWord((ushort)((figureIdx << 1) + 0x3afe)), 34,
						this.processor.PullGlobalWord((ushort)((((figureIdx + participantIdx) & 3) * 6) + 0x804e)));
				}

				this.owner.Services.RenderPictureToScreen(BackScreenBox, 90, 0, KingScenePicture);

				KingStep = 0;

				// The background's colours: "backNa.pal"
				string sceneryPal = $"back{regimeChar}{techChar}.pal";

				short king = (short)KingPicture;
				char tens = (char)('0' + (king / 10));
				char ones = (char)('0' + (king % 10));
				string hueName = $"king{tens}{ones}.pal";

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, hueName, 0xc1d6);

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, sceneryPal, 0xbdee);

				// The colours 0x40 - 0x8f of the king over the background's
				for (ushort place = 0xc0; place < 0x1b0; place++)
				{
					this.processor.EmitGlobalByte((ushort)(place + 0xbdf4), this.processor.PullGlobalByte((ushort)(place + 0xc1dc)));
				}

				this.owner.Helpers.HaltHueCycling();

				this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);

				this.owner.VideoCard.AssignHuesFromHueStruct(0xbdee);

				this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8, ScreenBox, 0, 0);

				this.owner.Startup.DisplayMouseCursor();
			}

			this.processor.Journal.CloseScope("'LoadKingBackground'");
		}

		/// <summary>
		/// Ends the meeting music and frees the picture of the king (0x7efe), when one is loaded (0x67ea)
		/// </summary>
		public void ReleaseKingResources()
		{
			this.processor.Journal.OpenScope("ReleaseKingResources()");

			this.owner.Services.PerformSound(1, 0);

			if (KingPictureLoaded != 0)
			{
				this.owner.Helpers.ReleaseResource(KingFacesPicture, 0);
			}

			KingPictureLoaded = 0x0;

			this.processor.Journal.CloseScope("ReleaseKingResources");
		}

		/// <summary>
		/// The king's picture (unless 0x67f0 is set): with the advisors' pose arg3 (not -1, not the one shown, 0x67e8, and with
		/// 0x67fa) the advisors, one for every two ranks of the standing, by the style of the government (6 for the monarchy,
		/// +3 with 0x67ee); then the face (arg1) and the mouth (arg2) of the king, placed by the table of the king (0x67f8) in the
		/// segment 0x36fa.
		/// </summary>
		public void ChoreographKingLikeness(ushort arg1, ushort arg2, ushort arg3)
		{
			this.processor.Journal.OpenScope($"ChoreographKingLikeness({arg1}, {arg2}, {arg3})");

			if (AudienceWithoutPictures == 0)
			{
				this.owner.Startup.ConcealMouseCursor();

				short regime = this.owner.World.Participants[AudiencePlayer].RegimeKind;
				short style = (short)((regime / 2) % 3);

				if (regime == 3)
				{
					style = 6;
				}
				else if (KingModernPictures != 0)
				{
					style += 3;
				}

				if (arg3 != 0xffff && arg3 != AdvisorPoseShown && KingScreenKept != 0)
				{
					for (short advisorIdx = 0; advisorIdx < (short)((this.owner.World.Participants[AudiencePlayer].Standing + 1) >> 1); advisorIdx++)
					{
						ushort seat = (ushort)((advisorIdx + AudiencePlayer) & 3);
						ushort place = (ushort)((style << 4) + (seat << 2));

						// The tables of the advisors are in the segment 0x36fa (ES is left there, like in the original)
						this.processor.SegExtra.Value16 = AdvisorKingVisualsSegment;

						short horizLoc = (short)(this.processor.PullGlobalWord((ushort)((advisorIdx << 1) + AdvisorHorizOffsetsTableAddress)) +
							(this.processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(place + 0x80)) - (0x50 * seat)) -
							(this.processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(place + 0x82)) - 0xa0));
						short vertLoc = (short)((this.processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(place + 0xf0)) - 0x64) -
							(this.processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(place + 0xf2)) - (0x19 * seat)));

						this.owner.Services.RenderPictureToScreen(ScreenBox, horizLoc, vertLoc + 34,
							this.processor.PullGlobalWord((ushort)((arg3 << 1) + (0x6 * seat) + AdvisorSpritesTableAddress)));

						AdvisorPoseShown = arg3;
					}
				}

				ushort king = (ushort)(KingPicture << 2);

				this.processor.SegExtra.Value16 = AdvisorKingVisualsSegment;

				short faceHoriz = (short)(0x5a + (this.processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(king + 0x10)) - 0xb4) - this.processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(king + 0x12)));
				short faceVert = (short)((this.processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(king + 0x48)) - 0x40) - this.processor.PullUInt16(AdvisorKingVisualsSegment, (ushort)(king + 0x4a)));

				if (arg1 != 0xffff)
				{
					this.owner.Services.RenderPictureToScreen(ScreenBox, faceHoriz, faceVert - 2,
						this.processor.PullGlobalWord((ushort)((arg1 << 2) + KingEyesPicturesTableAddress)));
				}

				if (arg2 != 0xffff)
				{
					this.owner.Services.RenderPictureToScreen(ScreenBox, faceHoriz, faceVert + 18,
						this.processor.PullGlobalWord((ushort)((arg2 << 2) + 0x7f00)));
				}

				this.owner.Startup.DisplayMouseCursor();
			}

			// Far return
			this.processor.Journal.CloseScope("ChoreographKingLikeness");
		}

		/// <summary>
		/// A dialog over the bottom part of the king screen (rows 128 - 199, kept in the back screen 0x19d4 and put back);
		/// column 20 is moved to 36
		/// </summary>
		/// <returns>The answer of DisplayMenu, also in AX</returns>
		public ushort DisplayKingDialog(ushort charsAddr, ushort horizLoc, ushort vertLoc)
		{
			this.processor.Journal.OpenScope($"DisplayKingDialog(0x{charsAddr:x4}, {horizLoc}, {vertLoc})");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0x80, 0x140, 0x48,
				BackScreenBox, 0, 0x80);

			if (horizLoc == 0x14)
			{
				horizLoc = 0x24;
			}

			this.owner.Startup.DisplayMouseCursorFully();

			ushort answer = this.owner.Dialogs.DisplayMenu(charsAddr, (short)horizLoc, (short)vertLoc, 1);

			this.owner.Startup.ConcealMouseCursorToSavedLevel();

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0x80, 0x140, 0x48,
				ScreenBox, 0, 0x80);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("DisplayKingDialog");

			return answer;
		}
	}
}
