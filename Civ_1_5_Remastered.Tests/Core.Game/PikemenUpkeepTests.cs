using Civ_1_5_Remastered.Core.Shared.State;
using Civ_1_5_Remastered.GameModel.Rules;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>
	/// A city pays a shield for its Pikemen as for any military unit (before, the rule ended at the Diplomat, kind 26, and
	/// the extra kinds after it were kept for free).
	/// </summary>
	public class PikemenUpkeepTests
	{
		private const short Monarchy = 2;
		private const short Communism = 3;
		private const short Republic = 4;
		private const short Democracy = 5;

		[Fact]
		public void PikemenAreMilitaryAndTheDiplomatAndCaravanAreNot()
		{
			Assert.True(UnitKinds.IsMilitary(UnitKinds.Pikemen));
			Assert.True(UnitKinds.IsMilitary(UnitKinds.Phalanx));
			Assert.True(UnitKinds.IsMilitary(0));
			Assert.False(UnitKinds.IsMilitary(UnitKinds.Diplomat));
			Assert.False(UnitKinds.IsMilitary(UnitKinds.Diplomat + 1));
			Assert.False(UnitKinds.IsMilitary(-1));
			Assert.False(UnitKinds.IsMilitary(UnitKinds.Count));
		}

		[Theory]
		[InlineData(Monarchy)]
		[InlineData(Communism)]
		[InlineData(Republic)]
		[InlineData(Democracy)]
		public Task ACityPaysAShieldForItsPikemen(short regime) => FittingSaves.OnFirstFittingSave(TimeSpan.FromMinutes(1), (game, save) =>
		{
			var engine = game.Engine;
			short person = engine.World.PersonParticipantIndex;
			short townIndex = (short)Array.FindIndex(engine.World.Towns, city => city.ConditionMarker != 0xff && city.ParticipantIndex == person);

			if (townIndex < 0)
			{
				return false;
			}

			City town = engine.World.Towns[townIndex];
			engine.World.Participants[person].RegimeKind = regime;
			engine.World.DiagonalMarkers |= 2;

			int Upkeep()
			{
				engine.CityOperations.HandleCity(townIndex, -1);
				return engine.Globals.CityTurn.UnitUpkeep;
			}

			int before = Upkeep();

			if (engine.UnitOperations.CreateUnit(person, UnitKinds.Diplomat, town.Place.X, town.Place.Y) == 0xffff)
			{
				return false;
			}

			Assert.Equal(before, Upkeep());

			ushort created = engine.UnitOperations.CreateUnit(person, UnitKinds.Pikemen, town.Place.X, town.Place.Y);

			if (created == 0xffff)
			{
				return false;
			}

			Assert.Equal(townIndex, new UnitMemory(engine.Processor).PullSigned(person, (short)created, UnitMemory.BaseTown));
			Assert.Equal(before + 1, Upkeep());

			return true;
		});
	}
}
