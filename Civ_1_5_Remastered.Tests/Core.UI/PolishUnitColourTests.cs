using Civ_1_5_Remastered.Core.Shared.Tables;
using Civ_1_5_Remastered.GameModel.Rules;
using Civ_1_5_Remastered.Tests.Core.Game;

namespace Civ_1_5_Remastered.Tests.Core.UI
{
	/// <summary>
	/// A slot that takes the colours of Poland (light red and white) gets its units cut again in them at once. Before,
	/// the cut was skipped (a box address was taken for a display number) and the units kept the colours of the slot's
	/// original civilization until the game cut all units again.
	/// </summary>
	public class PolishUnitColourTests
	{
		private const int PlayerColours = 0x1946;
		private const int PlayerSecondaryColours = 0x1956;
		private const byte LightRed = 12;
		private const byte White = 15;
		private const byte Green = 2;
		private const byte LightGreen = 10;

		[Fact]
		public Task UnitsOfASlotTakeItsNewColoursAtOnce() => FittingSaves.OnFirstFittingSave(TimeSpan.FromMinutes(1), (game, save) =>
		{
			var engine = game.Engine;
			const int Slot = 3;

			engine.Tables.Content[PlayerColours - GameTables.Begin + (Slot << 1)] = LightRed;
			engine.Tables.Content[PlayerSecondaryColours - GameTables.Begin + (Slot << 1)] = White;

			engine.NewGameWizard.CutPolishUnitSprites(Slot);

			foreach (int kind in new[] { UnitKinds.Phalanx, UnitKinds.Pikemen })
			{
				ushort handle = engine.Tables.SpritePicture(0x40 + (Slot << 5) + kind);
				Assert.True(handle != 0 && engine.VideoCard.Pictures.ContainsKey(handle), $"{save}: unit {kind} of the slot was not cut");

				// The badge colours of the sheet are gone: white and red stripes instead.
				var picture = engine.VideoCard.Pictures[handle];
				byte[] dots = Enumerable.Range(0, 15).SelectMany(vertical => Enumerable.Range(0, 15).Select(horizontal => picture.ObtainDot(horizontal, vertical))).ToArray();

				Assert.DoesNotContain(LightGreen, dots);
				Assert.DoesNotContain(Green, dots);
				Assert.Contains(LightRed, dots);
				Assert.Contains(White, dots);
			}

			return true;
		});
	}
}
