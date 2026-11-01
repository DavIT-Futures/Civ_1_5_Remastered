namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		public sealed class MapViewVariables : VariableGroup
		{
			private readonly GlobalWord revealed = new GlobalWord(0xd806);
			private readonly GlobalWord horiz = new GlobalWord(0xd4cc);
			private readonly GlobalWord vert = new GlobalWord(0xd75e);
			private readonly GlobalWord redrawRequest = new GlobalWord(0x6b64);
			private readonly GlobalWord worldMapView = new GlobalWord(0xb278);
			private readonly GlobalWord unitAtTilePlayer = new GlobalWord(0xd20a);
			private readonly GlobalWord unitsHidden = new GlobalWord(0xdcfc);
			private readonly GlobalWord cityLabelCount = new GlobalWord(0x6c96);
			private readonly GlobalWord focusLeft = new GlobalWord(0x6ed6);
			private readonly GlobalWord focusTop = new GlobalWord(0x70ea);

			public ushort Revealed { get => this.revealed.Value; set => this.revealed.Value = value; }

			public ushort Horiz { get => this.horiz.Value; set => this.horiz.Value = value; }

			public ushort Vert { get => this.vert.Value; set => this.vert.Value = value; }

			public ushort RedrawRequest { get => this.redrawRequest.Value; set => this.redrawRequest.Value = value; }

			public ushort WorldMapView => this.worldMapView.Value;

			public ushort UnitAtTilePlayer { get => this.unitAtTilePlayer.Value; set => this.unitAtTilePlayer.Value = value; }

			public ushort UnitsHidden { get => this.unitsHidden.Value; set => this.unitsHidden.Value = value; }

			public ushort CityLabelCount { get => this.cityLabelCount.Value; set => this.cityLabelCount.Value = value; }

			public ushort FocusLeft { get => this.focusLeft.Value; set => this.focusLeft.Value = value; }

			public ushort FocusTop { get => this.focusTop.Value; set => this.focusTop.Value = value; }
		}
	}
}
