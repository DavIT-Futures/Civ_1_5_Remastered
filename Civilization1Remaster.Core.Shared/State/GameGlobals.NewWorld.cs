namespace Civilization1Remaster.Core.Shared.State
{
	public sealed partial class GameGlobals
	{
		public sealed class NewWorldVariables : VariableGroup
		{
			private readonly GlobalWord earthGame = new GlobalWord(0xd76a);
			private readonly GlobalWord climate = new GlobalWord(0x7efa);
			private readonly GlobalWord landMass = new GlobalWord(0x7ef6);
			private readonly GlobalWord temperature = new GlobalWord(0x7ef8);
			private readonly GlobalWord worldAge = new GlobalWord(0x7efc);

			public ushort EarthGame { get => this.earthGame.Value; set => this.earthGame.Value = value; }

			public ushort Climate { get => this.climate.Value; set => this.climate.Value = value; }

			public ushort LandMass { get => this.landMass.Value; set => this.landMass.Value = value; }

			public ushort Temperature { get => this.temperature.Value; set => this.temperature.Value = value; }

			public ushort WorldAge { get => this.worldAge.Value; set => this.worldAge.Value = value; }
		}
	}
}
