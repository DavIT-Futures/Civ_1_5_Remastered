namespace Civ_1_5_Remastered.Core.Shared.Tables
{
	public static partial class ExtraBuildings
	{
		private sealed class Building
		{
			public Building(string name, int standIn, int price, int upkeep, int technology, string history, string note, string effect,
				string advice, string[] icon, string[] civilopediaPicture)
			{
				this.Name = name;
				this.StandIn = standIn;
				this.History = history;
				this.Note = note;
				this.EffectLine = effect;
				this.Advice = advice;
				this.IconPicture = icon;
				this.CivilopediaPicture = civilopediaPicture;

				for (int place = 0; place < name.Length && place < BuildingTable.Price - 1; place++)
				{
					this.Record[place] = (byte)name[place];
				}

				Put(BuildingTable.Price, price);
				Put(BuildingTable.Upkeep, upkeep);
				Put(BuildingTable.Technology, technology);
			}

			public string Name { get; }

			public int StandIn { get; }

			public string History { get; }

			public string Note { get; }

			public string EffectLine { get; }

			public string Advice { get; }

			public string[] IconPicture { get; }

			public string[] CivilopediaPicture { get; }

			public byte[] Record { get; } = new byte[BuildingTable.Stride];

			private void Put(int field, int value)
			{
				this.Record[field] = (byte)value;
				this.Record[field + 1] = (byte)(value >> 8);
			}
		}
	}
}
