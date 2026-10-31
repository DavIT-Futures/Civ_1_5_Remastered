namespace Civilization1Remaster.Core.Engine
{
	public sealed partial class GameGlobals
	{
		/// <summary>The map view (B6.4: C# fields over the memory of the data segment)</summary>
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

			/// <summary>0xd806: the whole map is shown</summary>
			public ushort Revealed { get => this.revealed.Value; set => this.revealed.Value = value; }

			/// <summary>0xd4cc: the column of the map view (its top left tile)</summary>
			public ushort Horiz { get => this.horiz.Value; set => this.horiz.Value = value; }

			/// <summary>0xd75e: the row of the map view (its top left tile)</summary>
			public ushort Vert { get => this.vert.Value; set => this.vert.Value = value; }

			/// <summary>0x6b64: the main screen and the map are drawn again after the city screen</summary>
			public ushort RedrawRequest { get => this.redrawRequest.Value; set => this.redrawRequest.Value = value; }

			/// <summary>0xb278: 1 when the map is drawn as the world map (a tile in 4 x 4 pixels)</summary>
			public ushort WorldMapView => this.worldMapView.Value;

			/// <summary>0xd20a: the player of the unit found last at a tile (also in 0xd7f0)</summary>
			public ushort UnitAtTilePlayer { get => this.unitAtTilePlayer.Value; set => this.unitAtTilePlayer.Value = value; }

			/// <summary>0xdcfc: the units on the map are not drawn</summary>
			public ushort UnitsHidden { get => this.unitsHidden.Value; set => this.unitsHidden.Value = value; }

			/// <summary>0x6c96: the names of the cities written on the map</summary>
			public ushort CityLabelCount { get => this.cityLabelCount.Value; set => this.cityLabelCount.Value = value; }

			/// <summary>0x6ed6: the left offset of the map view focus</summary>
			public ushort FocusLeft { get => this.focusLeft.Value; set => this.focusLeft.Value = value; }

			/// <summary>0x70ea: the top offset of the map view focus</summary>
			public ushort FocusTop { get => this.focusTop.Value; set => this.focusTop.Value = value; }
		}
	}
}
