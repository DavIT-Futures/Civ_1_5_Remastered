using Civ_1_5_Remastered.Core.Shared.State;
using Civ_1_5_Remastered.Core.Shared.Texts;
using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>
	/// The light grey box around a fortified unit hugs the frame of its picture: the narrower frame of the Pikemen gets a
	/// narrower box (the box of the original frames left a gap on its right, through which the land showed).
	/// </summary>
	public class UnitStatusBoxTests
	{
		private const byte Fortified = 8 | 0x02;
		private const int Left = 100;
		private const int Top = 60;

		[Theory]
		[InlineData(UnitKinds.Phalanx, -1, 14)]
		[InlineData(UnitKinds.Pikemen, -1, 12)]
		public Task TheBoxOfAFortifiedUnitHugsItsFrame(int kind, int boxLeft, int boxRight) => FittingSaves.OnFirstFittingSave(TimeSpan.FromMinutes(1), (game, save) =>
		{
			var engine = game.Engine;
			short person = engine.World.PersonParticipantIndex;
			UnitMemory troops = new UnitMemory(engine.Processor);
			short troop = (short)Enumerable.Range(0, UnitMemory.TroopTally).FirstOrDefault(index => troops.TroopKind(person, index) < 0, -1);

			if (troop < 0)
			{
				return false;
			}

			// The game cuts the unit pictures on its start screens, which a loaded test game skips: cut them here.
			engine.ImageToolkit.LoadPictureOrColors(1, 0, 0, GameTexts.FileSpaceshipPictureSecond, 0);
			engine.NewGameWizard.CutPlayerCitySprites(person);

			troops.Assign(person, troop, UnitMemory.Kind, (byte)kind);
			troops.Assign(person, troop, UnitMemory.Status, Fortified);

			engine.Services.FloodBox(engine.Globals.Screen.Box, Left - 4, Top - 4, 24, 24, 0);
			engine.MapOperations.DrawUnitSprite(person, troop, Left, Top);
			troops.Assign(person, troop, UnitMemory.Kind, 0xff);

			var display = engine.VideoCard.Displays[0];
			byte Dot(int horizontal) => display.PictureStorage[((Top + 8) * display.Extent.Width) + Left + horizontal];

			Assert.Equal(Hues.LightGray, Dot(boxLeft));
			Assert.Equal(Hues.LightGray, Dot(boxRight));
			Assert.Equal(0, Dot(boxRight + 1));

			return true;
		});
	}
}
