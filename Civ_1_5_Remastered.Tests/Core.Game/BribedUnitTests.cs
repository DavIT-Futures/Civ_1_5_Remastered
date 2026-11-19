using Civ_1_5_Remastered.Core.Shared.State;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>A unit bribed by a diplomat moves from one civilization's tally to the other's once (it was counted twice).</summary>
	public class BribedUnitTests
	{
		[Fact]
		public Task ABribedUnitIsCountedOnceByBothCivilizations() => FittingSaves.OnFirstFittingSave(TimeSpan.FromMinutes(1), (game, save) =>
		{
			var engine = game.Engine;
			short person = engine.World.PersonParticipantIndex;
			UnitMemory troops = new UnitMemory(engine.Processor);
			short[] computers = Enumerable.Range(1, 7).Select(participant => (short)participant)
				.Where(participant => participant != person && (engine.World.LiveRealms & (1 << participant)) != 0).ToArray();

			if (computers.Length < 2)
			{
				return false;
			}

			short seller = computers[0];
			short buyer = computers[1];
			Player selling = engine.World.Participants[seller];
			Player buying = engine.World.Participants[buyer];

			// Keep the price low and the buyer rich, so the computer buyer pays without a question.
			selling.Gold = 0;

			for (short troop = 0; troop < UnitMemory.TroopTally; troop++)
			{
				sbyte kind = troops.TroopKind(seller, troop);

				if (kind < 0)
				{
					continue;
				}

				buying.Gold = 30000;
				int sold = selling.LiveTroops[kind];
				int bought = buying.LiveTroops[kind];

				engine.Diplomats.BuyUnit(seller, troop, buyer);

				if (troops.TroopKind(seller, troop) >= 0)
				{
					// Not alone on its tile: not for sale.
					continue;
				}

				Assert.Equal(sold - 1, selling.LiveTroops[kind]);
				Assert.Equal(bought + 1, buying.LiveTroops[kind]);

				return true;
			}

			return false;
		});
	}
}
