using Civ_1_5_Remastered.Core.Shared.Tables;
using Civ_1_5_Remastered.GameModel.State;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>
	/// Units a city keeps (more than one fortified unit) add to the army strength of the civilization. Before, each
	/// of them set the whole strength to its own, so the War Advisor, audiences and the computer's war decisions saw
	/// an army of one unit.
	/// </summary>
	public class ArmyStrengthTests
	{
		private const sbyte Phalanx = 2;

		[Fact]
		public async Task UnitsKeptByACityAddToTheArmy()
		{
			if (GameFiles.Folder == null || GameFiles.Find("*.SVE").FirstOrDefault() is not string save)
			{
				return;
			}

			await Task.Run(() =>
			{
				using ComputerExpansionSimulation game = new ComputerExpansionSimulation(Path.GetFileName(save));

				if (!game.Loaded)
				{
					return;
				}

				var engine = game.Engine;

				// For the human player the computer turn only counts the army; it moves nothing.
				short person = engine.World.PersonParticipantIndex;
				Player player = engine.World.Participants[person];
				City? town = engine.World.Towns.FirstOrDefault(city => city.ConditionMarker != 0xff && city.ParticipantIndex == person);

				if (town == null)
				{
					return;
				}

				town.Unfamiliar[0] = -1;
				town.Unfamiliar[1] = -1;
				engine.Computer.OperateComputerTurn(person);
				int without = player.MartialMight;

				town.Unfamiliar[0] = Phalanx;
				town.Unfamiliar[1] = Phalanx;
				engine.Computer.OperateComputerTurn(person);
				int with = player.MartialMight;

				int phalanx = (short)engine.Tables.UnitKindWord(Phalanx, UnitKindTable.AssaultPower) +
					(short)engine.Tables.UnitKindWord(Phalanx, UnitKindTable.GuardPower);

				Assert.Equal(without + (2 * phalanx), with);
			}).WaitAsync(TimeSpan.FromMinutes(2));
		}
	}
}
