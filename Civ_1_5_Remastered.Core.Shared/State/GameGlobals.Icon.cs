namespace Civ_1_5_Remastered.Core.Shared.State
{
	public sealed partial class GameGlobals
	{
		public sealed class IconVariables : VariableGroup
		{
			private readonly GlobalWord birthPicture = new GlobalWord(0x3b64);
			private readonly GlobalWord governmentPicture = new GlobalWord(0x804e);
			private readonly GlobalWord newspaperCrowdPicture = new GlobalWord(0xdf0c);
			private readonly GlobalWord tileFortressPicture = new GlobalWord(0xd4da);
			private readonly GlobalWord miniSymbolPicture = new GlobalWord(0xb880);
			private readonly GlobalWord unhappyCitizenPicture = new GlobalWord(0x6e9e);
			private readonly GlobalWord pollutionIconPicture = new GlobalWord(0xb2ba);

			public ushort BirthPicture { get => this.birthPicture.Value; set => this.birthPicture.Value = value; }

			public ushort GovernmentPicture { get => this.governmentPicture.Value; set => this.governmentPicture.Value = value; }

			public ushort NewspaperCrowdPicture { get => this.newspaperCrowdPicture.Value; set => this.newspaperCrowdPicture.Value = value; }

			public ushort TileFortressPicture { get => this.tileFortressPicture.Value; set => this.tileFortressPicture.Value = value; }

			public ushort MiniSymbolPicture { get => this.miniSymbolPicture.Value; set => this.miniSymbolPicture.Value = value; }

			public ushort UnhappyCitizenPicture { get => this.unhappyCitizenPicture.Value; set => this.unhappyCitizenPicture.Value = value; }

			public ushort PollutionIconPicture { get => this.pollutionIconPicture.Value; set => this.pollutionIconPicture.Value = value; }
		}
	}
}
