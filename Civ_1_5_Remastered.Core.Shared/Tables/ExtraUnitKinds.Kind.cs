namespace Civ_1_5_Remastered.Core.Shared.Tables
{
	public static partial class ExtraUnitKinds
	{
		private sealed class Kind
		{
			public Kind(string name, int standIn, int shieldPrice, int obsoleteBy, int landGroup, int moves, int attack, int defence, int role, int technology,
				string civilopedia, string advice)
			{
				this.Name = name;
				this.CivilopediaText = civilopedia;
				this.Advice = advice;
				this.StandIn = standIn;
				this.ShieldPrice = shieldPrice;

				for (int place = 0; place < name.Length && place < NameLength - 1; place++)
				{
					this.Record[place] = (byte)name[place];
				}

				Put(UnitKindTable.ObsoleteBy, obsoleteBy);
				Put(UnitKindTable.LandGroup, landGroup);
				Put(UnitKindTable.ShiftTally, moves);
				Put(UnitKindTable.AssaultPower, attack);
				Put(UnitKindTable.GuardPower, defence);

				// The table counts in tens of shields: rounded up for the places that only compare prices.
				Put(UnitKindTable.Price, (shieldPrice + 9) / 10);
				Put(UnitKindTable.TroopGroup, role);
				Put(UnitKindTable.Technology, technology);
			}

			public string Name { get; }

			public string CivilopediaText { get; }

			public string Advice { get; }

			public int StandIn { get; }

			public int ShieldPrice { get; }

			public byte[] Record { get; } = new byte[UnitKindTable.Stride];

			private void Put(int field, int value)
			{
				this.Record[field] = (byte)value;
				this.Record[field + 1] = (byte)(value >> 8);
			}
		}
	}
}
