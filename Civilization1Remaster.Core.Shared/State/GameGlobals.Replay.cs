namespace Civilization1Remaster.Core.Shared.State
{
	public sealed partial class GameGlobals
	{
		public sealed class ReplayVariables : VariableGroup
		{
			private readonly GlobalWord mode = new GlobalWord(0x680a);
			private readonly GlobalWord actor = new GlobalWord(0x6810);
			private readonly GlobalWord cityDistance = new GlobalWord(0x6812);
			private readonly GlobalWord file = new GlobalWord(0x680c);
			private readonly GlobalWord atGameEnd = new GlobalWord(0x680e);

			public ushort Mode { get => this.mode.Value; set => this.mode.Value = value; }

			public ushort Actor { get => this.actor.Value; set => this.actor.Value = value; }

			public ushort CityDistance { get => this.cityDistance.Value; set => this.cityDistance.Value = value; }

			public ushort File { get => this.file.Value; set => this.file.Value = value; }

			public ushort AtGameEnd { get => this.atGameEnd.Value; set => this.atGameEnd.Value = value; }
		}
	}
}
