namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The replay (B6.4: C# fields over the memory of the data segment)</summary>
		public sealed class ReplayVariables : VariableGroup
		{
			private readonly GlobalWord mode = new GlobalWord(0x680a);
			private readonly GlobalWord actor = new GlobalWord(0x6810);
			private readonly GlobalWord cityDistance = new GlobalWord(0x6812);
			private readonly GlobalWord file = new GlobalWord(0x680c);
			private readonly GlobalWord atGameEnd = new GlobalWord(0x680e);

			/// <summary>0x680a: the mode of the replay</summary>
			public ushort Mode { get => this.mode.Value; set => this.mode.Value = value; }

			/// <summary>0x6810: the acting player of the replay event (-1 none)</summary>
			public ushort Actor { get => this.actor.Value; set => this.actor.Value = value; }

			/// <summary>0x6812: the distance of the nearest entry of the replay city table (999 none)</summary>
			public ushort CityDistance { get => this.cityDistance.Value; set => this.cityDistance.Value = value; }

			/// <summary>0x680c: the handle of the replay file</summary>
			public ushort File { get => this.file.Value; set => this.file.Value = value; }

			/// <summary>0x680e: the replay is shown at the end of the game</summary>
			public ushort AtGameEnd { get => this.atGameEnd.Value; set => this.atGameEnd.Value = value; }
		}
	}
}
