namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The pictures loaded for the icons (B6.4: C# fields over the memory of the data segment)</summary>
		public sealed class IconVariables : VariableGroup
		{
			private readonly GlobalWord birthPicture = new GlobalWord(0x3b64);
			private readonly GlobalWord governmentPicture = new GlobalWord(0x804e);
			private readonly GlobalWord newspaperCrowdPicture = new GlobalWord(0xdf0c);
			private readonly GlobalWord tileFortressPicture = new GlobalWord(0xd4da);
			private readonly GlobalWord miniSymbolPicture = new GlobalWord(0xb880);
			private readonly GlobalWord unhappyCitizenPicture = new GlobalWord(0x6e9e);
			private readonly GlobalWord pollutionIconPicture = new GlobalWord(0xb2ba);

			/// <summary>0x3b64: the next picture of the story (birthN.pic)</summary>
			public ushort BirthPicture { get => this.birthPicture.Value; set => this.birthPicture.Value = value; }

			/// <summary>0x804e: the picture of the government in the audience (govtNa.pic)</summary>
			public ushort GovernmentPicture { get => this.governmentPicture.Value; set => this.governmentPicture.Value = value; }

			/// <summary>0xdf0c: picture handle of the cheering crowd in the newspaper</summary>
			public ushort NewspaperCrowdPicture { get => this.newspaperCrowdPicture.Value; set => this.newspaperCrowdPicture.Value = value; }

			/// <summary>0xd4da: picture handle of the tile fortress</summary>
			public ushort TileFortressPicture { get => this.tileFortressPicture.Value; set => this.tileFortressPicture.Value = value; }

			/// <summary>0xb880: picture handle of the mini peace symbol on the map</summary>
			public ushort MiniSymbolPicture { get => this.miniSymbolPicture.Value; set => this.miniSymbolPicture.Value = value; }

			/// <summary>0x6e9e: picture handle of the unhappy citizen</summary>
			public ushort UnhappyCitizenPicture { get => this.unhappyCitizenPicture.Value; set => this.unhappyCitizenPicture.Value = value; }

			/// <summary>0xb2ba: picture handle of the pollution skull icon</summary>
			public ushort PollutionIconPicture { get => this.pollutionIconPicture.Value; set => this.pollutionIconPicture.Value = value; }
		}
	}
}
