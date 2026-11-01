namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
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

			public ushort ArrivedSpaceship { get => this.arrivedSpaceship.Value; set => this.arrivedSpaceship.Value = value; }

			public ushort Colonists { get => this.colonists.Value; set => this.colonists.Value = value; }

			public ushort Propulsion { get => this.propulsion.Value; set => this.propulsion.Value = value; }

			public ushort Fuel { get => this.fuel.Value; set => this.fuel.Value = value; }

			public ushort Support { get => this.support.Value; set => this.support.Value = value; }

			public ushort Energy { get => this.energy.Value; set => this.energy.Value = value; }

			public ushort ComponentsComplete { get => this.componentsComplete.Value; set => this.componentsComplete.Value = value; }

			public ushort ModulesComplete { get => this.modulesComplete.Value; set => this.modulesComplete.Value = value; }

			public ushort Picture { get => this.picture.Value; set => this.picture.Value = value; }

			public ushort HabitatPicture { get => this.habitatPicture.Value; set => this.habitatPicture.Value = value; }

			public ushort StarFieldFrame { get => this.starFieldFrame.Value; set => this.starFieldFrame.Value = value; }
		}
	}
}
