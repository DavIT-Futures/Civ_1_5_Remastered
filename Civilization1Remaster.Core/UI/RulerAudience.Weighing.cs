namespace Civilization1Remaster.Core.UI
{
	public partial class RulerAudience
	{
		private void WeighContinents(short participantIdx)
		{
			GameModel.State.Player leader = Leader(participantIdx);

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
					tribute = CPU.SignedQuotient((short)(own.Assault - human.Guard), 2);
				}
				else
				{
					tribute = CPU.SignedQuotient((short)(own.Guard - human.Guard), 4);
				}

				this.audienceTribute = (ushort)(this.audienceTribute + tribute);
			}
		}

		private void WeighNuclearWeapons(short participantIdx)
		{
			GameModel.State.Player leader = Leader(participantIdx);
			GameModel.State.Player person = Person;
			short personIdx = this.Owner.World.PersonParticipantIdx;

			bool threat = person.Standing == 7 && person.TownTally > 4 && leader.TownTally > 1 && person.LiveTroops[25] == 0 &&
				this.Owner.World.RoundTally > 200;
			this.audienceNuclearThreat = (ushort)(threat ? 1 : 0);

			if ((threat || ((Relation(participantIdx) & 8) != 0 && (short)(3 * leader.MartialMight) > person.MartialMight)) &&
				person.LiveTroops[25] == 0)
			{
				this.audienceHostile = 1;
				this.audienceTribute = (ushort)this.Owner.Helpers.ClampValue((short)this.audienceTribute, 10 * leader.Standing, 9999);
				this.audienceDanger = 0;
			}

			if (leader.LiveTroops[25] != 0)
			{
				leader.Relations[personIdx] |= 0x80;
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
				{
					this.audienceLastAudience = 0xffff;
				}
			}

			if ((Relation(participantIdx) & 8) != 0)
			{
				this.audienceTribute = (ushort)(this.audienceTribute << 1);
			}
			else if (this.audienceLastAudience == 0xfffe)
			{
				this.audienceTribute = (ushort)CPU.SignedQuotient((short)this.audienceTribute, 4);
			}

			this.audienceTribute = (ushort)((ushort)this.Owner.Helpers.ClampValue(((short)this.audienceTribute * (this.Owner.World.HardnessTier + 1)) / 32, 0, 20) * 0x32);

			short gold = person.Gold;

			if ((short)this.audienceTribute > gold && (short)this.audienceTribute <= (short)(gold << 1) && gold >= 0x32)
			{
				this.audienceTribute = (ushort)(CPU.SignedQuotient(gold, 0x32) * 0x32);
			}

			if (leader.LiveTroops[25] != 0)
			{
				if ((Relation(participantIdx) & 0x80) == 0 && person.Gold < (short)this.audienceTribute)
				{
					this.audienceTribute = (ushort)(CPU.SignedQuotient(person.Gold, 0x32) * 0x32);
				}

				leader.Relations[personIdx] |= 0x80;
			}

			if (this.audienceTribute == 0 || (short)(3 * leader.MartialMight) < person.MartialMight)
			{
				this.audienceHostile = 0;
			}

			if (this.audienceHostile != 0)
			{
				this.audienceDemand = 2;
			}

			if (this.audienceHostile == 0 && (Relation(participantIdx) & 4) != 0)
			{
				this.audienceDemand = 0;
			}
		}

		private void WeighEnemies(short participantIdx)
		{
			GameModel.State.Player leader = Leader(participantIdx);
			short personIdx = this.Owner.World.PersonParticipantIdx;

			this.audienceWasHostile = HostileValue;
			short attitude = 0;

			if ((Relation(participantIdx) & 8) != 0)
			{
				attitude = -2;
			}

			this.audienceCommonEnemy = 0xffff;

			for (short otherIdx = 1; otherIdx < 8; otherIdx++)
			{
				if (otherIdx == personIdx || (leader.Relations[otherIdx] & 3) != 1)
				{
					continue;
				}

				if (leader.MartialMight < (short)(Leader(otherIdx).MartialMight << 2))
				{
					attitude++;
				}

				if (Leader(otherIdx).MartialMight > leader.MartialMight)
				{
					attitude++;
				}

				if ((Person.Relations[otherIdx] & 2) != 0)
				{
					this.audienceCommonEnemy = (ushort)otherIdx;
				}
			}

			ushort character = this.Processor.PullUInt16(this.Processor.SegData.Value16, (ushort)(0x1512 + (0x3a * leader.ChiefVisuals)));
			attitude = (short)(attitude - character);

			if (Person.MartialMight < leader.MartialMight)
			{
				attitude--;
			}

			short calm = (short)((Relation(participantIdx) & 2) == 0 ? 1 : 0);

			if (calm < attitude && this.audienceNuclearThreat == 0)
			{
				this.audienceHostile = 0;

				if (this.Owner.Libc.Dice.DrawInt(2) != 0)
				{
					this.audienceTribute = 0;
				}
			}

			if (leader.RegimeKind >= 4 && HostileValue != 0 && (Relation(participantIdx) & 8) == 0)
			{
				if ((short)(3 * Person.MartialMight) > (short)(leader.MartialMight << 1) && this.audienceNuclearThreat == 0)
				{
					this.audienceHostile = 0;
				}
				else
				{
					this.Owner.Politics.SwitchGovernment(participantIdx, 2);
				}
			}

			this.audiencePeaceBroken = 0;

			if ((Relation(participantIdx) & 2) != 0 && HostileValue != 0 &&
				(leader.MartialMight < Person.MartialMight || (Relation(participantIdx) & 8) != 0))
			{
				this.audienceHostile = 0;
				this.audiencePeaceBroken = 1;
				this.audienceDemand = 0;
			}
		}
	}
}
