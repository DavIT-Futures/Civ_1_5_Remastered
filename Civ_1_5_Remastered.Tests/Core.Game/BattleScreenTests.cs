using Civ_1_5_Remastered.Core.Shared.State;
using Civ_1_5_Remastered.Core.Shared.Texts;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>
	/// After a battle the winner stays on the screen. The animation restores tiles from a copy of the screen that held
	/// only the 256 left columns of a 320 x 200 screen, so a winner in the right part of the map window (or anywhere
	/// beyond the original window) vanished until the map was drawn again. On an enlarged map the battle must also keep
	/// the map layout: a copy of the whole screen from its corner starts a centred 320 x 200 screen, which drew the
	/// fight and the left panel shifted by the difference of the sizes.
	/// </summary>
	public class BattleScreenTests
	{
		private const short Barbarians = 0;
		private const short Militia = 1;
		private const short Armor = 11;
		private const int Ocean = 10;
		private const int WinnerColumn = 13;
		private const int TileSize = 16;

		[Theory]
		[InlineData(15, 12)]
		[InlineData(30, 20)]
		public Task TheWinnerOfABattleNearTheRightEdgeStaysOnTheScreen(int columns, int rows) => FittingSaves.OnFirstFittingSave(TimeSpan.FromMinutes(1), (game, save) =>
		{
			var engine = game.Engine;
			engine.MapViewport.Request(columns, rows);
			engine.World.ParticipantMarkers = (short)(1 << engine.World.PersonParticipantIndex);
			short person = engine.World.PersonParticipantIndex;
			var map = engine.MapOperations;

			bool Free(int horizontal, int vertical) =>
				map.ObtainTerrainType(horizontal, vertical) != Ocean && map.ObtainUnitAtTile(horizontal, vertical) == 0xffff &&
				(map.ObtainTileImprovements(horizontal, vertical) & 1) == 0;

			// Two free land tiles side by side: the attacker on the right, in column 13 of the window.
			(int Horizontal, int Vertical)? spot = null;

			for (int vertical = 8; vertical < 40 && spot == null; vertical++)
			{
				for (int horizontal = WinnerColumn + 1; horizontal < 70 && spot == null; horizontal++)
				{
					if (Free(horizontal, vertical) && Free(horizontal - 1, vertical))
					{
						spot = (horizontal, vertical);
					}
				}
			}

			if (spot == null)
			{
				return false;
			}

			(int column, int row) = spot.Value;

			// A loaded test game skips the start screens that cut the unit pictures: cut them here.
			engine.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileSpaceshipPictureSecond, 0);

			for (int participant = 0; participant < 8; participant++)
			{
				engine.NewGameWizard.CutPlayerCitySprites(participant);
			}

			engine.Globals.MapView.Horizontal = (ushort)(column - WinnerColumn);
			engine.Globals.MapView.Vertical = (ushort)(row - 5);

			// The battle is left to chance: fight again (with new units) until the attacker wins, the case where the
			// winner vanished.
			ushort won = 0;

			for (int attempt = 0; attempt < 20 && won != 1; attempt++)
			{
				ushort attacker = engine.UnitOperations.CreateUnit(person, Armor, column, row);
				ushort defender = engine.UnitOperations.CreateUnit(Barbarians, Militia, column - 1, row);

				if (attacker == 0xffff || defender == 0xffff)
				{
					return false;
				}

				// Full moves: an attack with fewer is weaker.
				new UnitMemory(engine.Processor).Assign(person, attacker, UnitMemory.LeftoverSteps, 9);
				engine.TurnHooks.RepaintMainScreen();

				won = engine.Combat.SettleBattle(person, (short)attacker, Barbarians, (short)defender, 1);
			}

			Assert.Equal(1, won);
			Assert.True(engine.VideoCard.Wide, $"{save}: the battle left the map layout");
			int winnerHorizontal = column;

			byte[] afterBattle = Tile(engine, winnerHorizontal, row);
			map.RepaintMapTile(winnerHorizontal, row);
			byte[] redrawn = Tile(engine, winnerHorizontal, row);

			// Below the health bar (its first three rows), where the unit is drawn.
			Assert.True(afterBattle.Skip(3 * TileSize).SequenceEqual(redrawn.Skip(3 * TileSize)),
				$"{save}: the winner's tile differs from the same tile drawn again");
			Assert.Contains(redrawn.Skip(3 * TileSize), dot => dot != 0);

			return true;
		});

		private static byte[] Tile(Civ_1_5_Remastered.Core.Composition.GameEngine engine, int horizontal, int vertical)
		{
			var display = engine.VideoCard.Displays[0];
			int breadth = display.Extent.Width;
			int left = ((horizontal - engine.Globals.MapView.Horizontal) * TileSize) + 0x50;
			int top = ((vertical - engine.Globals.MapView.Vertical) * TileSize) + 8;

			return Enumerable.Range(0, TileSize).SelectMany(row => Enumerable.Range(0, TileSize).Select(column => display.PictureStorage[((top + row) * breadth) + left + column])).ToArray();
		}
	}
}
