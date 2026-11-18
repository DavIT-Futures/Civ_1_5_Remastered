using Civ_1_5_Remastered.Tests.Core.Game;

namespace Civ_1_5_Remastered.Tests.Core.UI
{
	/// <summary>
	/// The menus of the menu bar: one click opens a menu and it stays open (before, it had to be held), and over an
	/// enlarged map window it opens under the bar (before, it was drawn as a 320 x 200 screen centred over the map).
	/// </summary>
	public class MenuBarTests
	{
		private const short GameMenu = 0;
		private const ushort SaveGameOrder = 0x53;
		private const ushort DownKey = 0x5000;
		private const int LineHeight = 8;
		private const int SaveGameLine = 4;

		[Theory]
		[InlineData(15, 12)]
		[InlineData(30, 20)]
		public Task TheGameMenuOpensUnderTheBar(int columns, int rows) => FittingSaves.OnFirstFittingSave(TimeSpan.FromMinutes(1), (game, save) =>
		{
			var engine = game.Engine;
			engine.MapViewport.Request(columns, rows);
			engine.World.ParticipantMarkers = (short)(1 << engine.World.PersonParticipantIndex);
			engine.TurnHooks.RepaintMainScreen();

			var display = engine.VideoCard.Displays[0];
			int breadth = display.Extent.Width;
			byte[] before = (byte[])display.PictureStorage.Clone();
			byte[]? open = null;

			game.Feeding = false;
			game.StepKey = 0x1b;
			game.Waiting = step => open ??= (byte[])display.PictureStorage.Clone();

			lock (engine.VideoCard.Tokens)
			{
				engine.VideoCard.Tokens.Clear();
			}

			engine.MenuSystem.ManageMenuBar(engine.World.PersonParticipantIndex, -1, GameMenu);
			game.Waiting = null;

			Assert.NotNull(open);

			// The top left corner of what the menu changed on the screen: the menu box at (16, 8), under GAME.
			int first = Enumerable.Range(0, before.Length).First(place => open![place] != before[place]);
			int left = Enumerable.Range(0, before.Length).Where(place => open![place] != before[place]).Min(place => place % breadth);

			Assert.Equal(8, first / breadth);
			Assert.Equal(16, left);

			return true;
		});

		[Fact]
		public Task OneClickOpensTheGameMenuAndTheNextChoosesALine() => FittingSaves.OnFirstFittingSave(TimeSpan.FromMinutes(1), (game, save) =>
		{
			var engine = game.Engine;
			engine.World.ParticipantMarkers = (short)(1 << engine.World.PersonParticipantIndex);
			engine.TurnHooks.RepaintMainScreen();

			// The button pressed on GAME in the menu bar, as when the menu opens.
			engine.Globals.Mouse.Present = 1;
			engine.Pointer.Horizontal = 20;
			engine.Pointer.Vertical = 3;
			engine.Pointer.Buttons = 1;
			engine.Pointer.Clicks = 1;

			game.Feeding = false;
			game.StepKey = DownKey;
			game.Waiting = step =>
			{
				if (step == 0)
				{
					// Let go on the bar, off the lines of the menu: the menu stays open.
					engine.Pointer.Buttons = 0;
				}
				else if (step == 1)
				{
					// A click on "Save Game".
					engine.Pointer.Horizontal = 40;
					engine.Pointer.Vertical = (ushort)(8 + 4 + (SaveGameLine * LineHeight) + 3);
					engine.Pointer.Clicks = 1;
				}
			};

			lock (engine.VideoCard.Tokens)
			{
				engine.VideoCard.Tokens.Clear();
			}

			engine.MenuSystem.ManageMenuBar(engine.World.PersonParticipantIndex, -1, GameMenu);
			game.Waiting = null;
			engine.Globals.Mouse.Present = 0;

			Assert.Equal(SaveGameOrder, engine.Globals.Turn.ChosenOrder);

			return true;
		});
	}
}
