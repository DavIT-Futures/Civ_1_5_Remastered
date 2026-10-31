namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The choices of the new world (B6.4: C# fields over the memory of the data segment)</summary>
		public sealed class NewWorldVariables : VariableGroup
		{
			private readonly GlobalWord earthGame = new GlobalWord(0xd76a);
			private readonly GlobalWord climate = new GlobalWord(0x7efa);
			private readonly GlobalWord landMass = new GlobalWord(0x7ef6);
			private readonly GlobalWord temperature = new GlobalWord(0x7ef8);
			private readonly GlobalWord worldAge = new GlobalWord(0x7efc);

			/// <summary>0xd76a: the game is played on the map of the Earth</summary>
			public ushort EarthGame { get => this.earthGame.Value; set => this.earthGame.Value = value; }

			/// <summary>0x7efa: the climate of the new world</summary>
			public ushort Climate { get => this.climate.Value; set => this.climate.Value = value; }

			/// <summary>0x7ef6: the land mass of the new world</summary>
			public ushort LandMass { get => this.landMass.Value; set => this.landMass.Value = value; }

			/// <summary>0x7ef8: the temperature of the new world</summary>
			public ushort Temperature { get => this.temperature.Value; set => this.temperature.Value = value; }

			/// <summary>0x7efc: the age of the new world</summary>
			public ushort WorldAge { get => this.worldAge.Value; set => this.worldAge.Value = value; }
		}
	}
}
