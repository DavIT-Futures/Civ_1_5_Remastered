namespace Civ_1_5_Remastered.Core.UI.Meetings
{
	public partial class RulerAudience
	{
		private void WeighContinents(short participantIndex)
		{
			GameModel.State.Player leader = Leader(participantIndex);

			for (int continentIndex = 1; continentIndex < 0xf; continentIndex++)
			{
				var own = leader.Landmasses[continentIndex];
				var human = Person.Landmasses[continentIndex];
				short cities = (short)(leader.TownTally > 1 ? 1 : 0);

				if (own.TownTally > cities && human.Guard > own.Assault)
				{
					short factor = (short)(this.Owner.World.HardnessTier == 0 ? 4 : 2);
					short danger = CentralProcessor.SignedQuotient((short)(factor * human.Guard), (short)(own.Assault + 1));
					this.audienceDanger = (ushort)(this.audienceDanger + danger);
					continue;
				}

				if (human.Assault != 0 && own.Assault != 0)
				{
					this.audienceHostile = 1;
				}

				short tribute;

				if (human.TownTally != 0)
				{
					tribute = own.TownTally > 1 ? (short)(own.Guard - human.Guard) : own.Guard;
				}
				else if (own.TownTally != 0)
				{
					tribute = CentralProcessor.SignedQuotient((short)(own.Assault - human.Guard), 2);
				}
				else
				{
					tribute = CentralProcessor.SignedQuotient((short)(own.Guard - human.Guard), 4);
				}

				this.audienceTribute = (ushort)(this.audienceTribute + tribute);
			}
		}

		private void WeighNuclearWeapons(short participantIndex)
		{
			GameModel.State.Player leader = Leader(participantIndex);
			GameModel.State.Player person = Person;
			short personIndex = this.Owner.World.PersonParticipantIndex;

			bool threat = person.Standing == 7 && person.TownTally > 4 && leader.TownTally > 1 && person.LiveTroops[25] == 0 &&
				this.Owner.World.RoundTally > 200;

			// Remastered: a long peace eases the envy of the world leader (DiplomacyRules.EnvyEase).
			this.audienceEnvyEase = (ushort)DiplomacyRules.EnvyEase(
				DiplomacyRules.AtPeace(this.Owner.World.Participants, participantIndex, personIndex),
				DiplomacyRules.PeaceUnits(this.Owner.World.Participants, participantIndex, personIndex));

			if (this.audienceEnvyEase >= 2)
			{
				threat = false;
			}

			this.audienceNuclearThreat = (ushort)(threat ? 1 : 0);

			if ((threat || ((Relation(participantIndex) & 8) != 0 && (short)(3 * leader.MartialMight) > person.MartialMight)) &&
				person.LiveTroops[25] == 0)
			{
				this.audienceHostile = 1;
				this.audienceTribute = (ushort)this.Owner.Helpers.ClampValue((short)this.audienceTribute, 10 * leader.Standing, 9999);
				this.audienceDanger = 0;
			}

			if (leader.LiveTroops[25] != 0)
			{
				leader.Relations[personIndex] |= 0x80;
			}

			if (leader.LiveTroops[25] != 0 && person.LiveTroops[25] == 0)
			{
				this.audienceHostile = 1;
				this.audienceTribute = (ushort)this.Owner.Helpers.ClampValue((short)this.audienceTribute, 100, 9999);
			}

			if (person.LiveTroops[25] != 0)
			{
				if (leader.LiveTroops[25] == 0)
				{
					this.audienceDanger = 4;
				}
				else
				{
					short half = CentralProcessor.SignedQuotient((short)this.audienceTribute, 2);
					this.audienceTribute = (ushort)CentralProcessor.SignedQuotient((short)(leader.LiveTroops[25] * half), person.LiveTroops[25]);
				}
			}

			if (this.audienceDanger != 0)
			{
				this.audienceHostile = 0;
				this.audienceDemand = 0;
			}

			if (this.Owner.CityOperations.PlayerEnjoysWonderEffect(personIndex, 7) != 0 || this.Owner.CityOperations.PlayerEnjoysWonderEffect(personIndex, 0x12) != 0)
			{
				this.audienceNuclearThreat = 0;
				this.audienceHostile = 0;

				if (this.audienceLastAudience == 0xfffe)
				{
					this.audienceLastAudience = 0xffff;
				}
			}

			if ((Relation(participantIndex) & 8) != 0)
			{
				this.audienceTribute = (ushort)(this.audienceTribute << 1);
			}
			else if (this.audienceLastAudience == 0xfffe)
			{
				this.audienceTribute = (ushort)CentralProcessor.SignedQuotient((short)this.audienceTribute, 4);
			}

			this.audienceTribute = (ushort)((ushort)this.Owner.Helpers.ClampValue(((short)this.audienceTribute * (this.Owner.World.HardnessTier + 1)) / 32, 0, 20) * 0x32);

			short gold = person.Gold;

			if ((short)this.audienceTribute > gold && (short)this.audienceTribute <= (short)(gold << 1) && gold >= 0x32)
			{
				this.audienceTribute = (ushort)(CentralProcessor.SignedQuotient(gold, 0x32) * 0x32);
			}

			if (leader.LiveTroops[25] != 0)
			{
				if ((Relation(participantIndex) & 0x80) == 0 && person.Gold < (short)this.audienceTribute)
				{
					this.audienceTribute = (ushort)(CentralProcessor.SignedQuotient(person.Gold, 0x32) * 0x32);
				}

				leader.Relations[personIndex] |= 0x80;
			}

			if (this.audienceTribute == 0 || (short)(3 * leader.MartialMight) < person.MartialMight)
			{
				this.audienceHostile = 0;
			}

			if (this.audienceHostile != 0)
			{
				this.audienceDemand = 2;
			}

			if (this.audienceHostile == 0 && (Relation(participantIndex) & 4) != 0)
			{
				this.audienceDemand = 0;
			}
		}

		private void WeighEnemies(short participantIndex)
		{
			GameModel.State.Player leader = Leader(participantIndex);
			short personIndex = this.Owner.World.PersonParticipantIndex;

			this.audienceWasHostile = HostileValue;
			short attitude = 0;

			if ((Relation(participantIndex) & 8) != 0)
			{
				attitude = -2;
			}

			this.audienceCommonEnemy = 0xffff;

			for (short otherIndex = 1; otherIndex < 8; otherIndex++)
			{
				if (otherIndex == personIndex || (leader.Relations[otherIndex] & 3) != 1)
				{
					continue;
				}

				if (leader.MartialMight < (short)(Leader(otherIndex).MartialMight << 2))
				{
					attitude++;
				}

				if (Leader(otherIndex).MartialMight > leader.MartialMight)
				{
					attitude++;
				}

				if ((Person.Relations[otherIndex] & 2) != 0)
				{
					this.audienceCommonEnemy = (ushort)otherIndex;
				}
			}

			ushort character = Tables.LeaderWord(leader.ChiefVisuals, LeaderTable.Aggression);
			attitude = (short)(attitude - character);

			if (Person.MartialMight < leader.MartialMight)
			{
				attitude--;
			}

			if (DiplomacyRules.AtPeace(this.Owner.World.Participants, participantIndex, personIndex))
			{
				attitude = (short)(attitude + DiplomacyRules.PeaceAttitudeBonus(DiplomacyRules.PeaceUnits(this.Owner.World.Participants, participantIndex, personIndex)));
			}

			short calm = (short)((Relation(participantIndex) & 2) == 0 ? 1 : 0);

			if (calm < attitude && (this.audienceNuclearThreat == 0 || this.audienceEnvyEase >= 1))
			{
				this.audienceHostile = 0;

				if (this.Owner.Runtime.Dice.DrawInteger(2) != 0)
				{
					this.audienceTribute = 0;
				}
			}

			if (leader.RegimeKind >= 4 && HostileValue != 0 && (Relation(participantIndex) & 8) == 0)
			{
				if ((short)(3 * Person.MartialMight) > (short)(leader.MartialMight << 1) && (this.audienceNuclearThreat == 0 || this.audienceEnvyEase >= 1))
				{
					this.audienceHostile = 0;
				}
				else
				{
					this.Owner.Politics.SwitchGovernment(participantIndex, 2);
				}
			}

			this.audiencePeaceBroken = 0;

			if ((Relation(participantIndex) & 2) != 0 && HostileValue != 0 &&
				(leader.MartialMight < Person.MartialMight || (Relation(participantIndex) & 8) != 0))
			{
				this.audienceHostile = 0;
				this.audiencePeaceBroken = 1;
				this.audienceDemand = 0;
			}
		}
	}
}
