namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The spaceship (B6.4: C# fields over the memory of the data segment)</summary>
		public sealed class SpaceshipVariables : VariableGroup
		{
			private readonly GlobalWord arrivedSpaceship = new GlobalWord(0x1cf6);
			private readonly GlobalWord colonists = new GlobalWord(0xdeae);
			private readonly GlobalWord propulsion = new GlobalWord(0xdea8);
			private readonly GlobalWord fuel = new GlobalWord(0xdeaa);
			private readonly GlobalWord support = new GlobalWord(0xdeb0);
			private readonly GlobalWord energy = new GlobalWord(0xdeb2);
			private readonly GlobalWord componentsComplete = new GlobalWord(0x687c);
			private readonly GlobalWord modulesComplete = new GlobalWord(0x687e);
			private readonly GlobalWord picture = new GlobalWord(0x682a);
			private readonly GlobalWord habitatPicture = new GlobalWord(0x6834);
			private readonly GlobalWord starFieldFrame = new GlobalWord(0x687a);

			/// <summary>0x1cf6: the player whose spaceship arrived this year (-1 none)</summary>
			public ushort ArrivedSpaceship { get => this.arrivedSpaceship.Value; set => this.arrivedSpaceship.Value = value; }

			/// <summary>0xdeae: the colonists of the spaceship</summary>
			public ushort Colonists { get => this.colonists.Value; set => this.colonists.Value = value; }

			/// <summary>0xdea8: the propulsion components of the spaceship</summary>
			public ushort Propulsion { get => this.propulsion.Value; set => this.propulsion.Value = value; }

			/// <summary>0xdeaa: the fuel components of the spaceship</summary>
			public ushort Fuel { get => this.fuel.Value; set => this.fuel.Value = value; }

			/// <summary>0xdeb0: the support modules of the spaceship</summary>
			public ushort Support { get => this.support.Value; set => this.support.Value = value; }

			/// <summary>0xdeb2: the energy modules of the spaceship</summary>
			public ushort Energy { get => this.energy.Value; set => this.energy.Value = value; }

			/// <summary>0x687c: all components of the spaceship are there</summary>
			public ushort ComponentsComplete { get => this.componentsComplete.Value; set => this.componentsComplete.Value = value; }

			/// <summary>0x687e: all modules of the spaceship are there</summary>
			public ushort ModulesComplete { get => this.modulesComplete.Value; set => this.modulesComplete.Value = value; }

			/// <summary>0x682a: the picture of the spaceship parts</summary>
			public ushort Picture { get => this.picture.Value; set => this.picture.Value = value; }

			/// <summary>0x6834: picture handle of the spaceship habitat module</summary>
			public ushort HabitatPicture { get => this.habitatPicture.Value; set => this.habitatPicture.Value = value; }

			/// <summary>0x687a: frame counter of the spaceship star field</summary>
			public ushort StarFieldFrame { get => this.starFieldFrame.Value; set => this.starFieldFrame.Value = value; }
		}
	}
}
