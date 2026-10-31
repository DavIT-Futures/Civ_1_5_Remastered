using System;
using System.Linq;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>The founding of a city against its original code</summary>
	public class BatchFortySixDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void EstablishCityMatchesOriginalCode()
		{
			Check(3, 60, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short person = engine.World.PersonParticipantIdx;
				short participantIdx = (short)(chance.Next(3) == 0 ? person : chance.Next(1, 8));
				(int horizLoc, int vertLoc) = RandomTile(engine, chance, true);
				ushort size = (ushort)chance.Next(1, 4);
				bool screen = chance.Next(3) == 0;
				bool wonder = chance.Next(4) == 0;

				return new Scenario($"EstablishCity({participantIdx}, 0, {horizLoc}, {vertLoc}, {size}), city screen {screen}",
					game =>
					{
						game.World.SessionPrefMarkers = (short)(screen ? game.World.SessionPrefMarkers | 8 : game.World.SessionPrefMarkers & ~8);

						if (wonder)
							game.World.MarvelTownIdx[0x13] = -1;
					},
					game => new CityFoundingAndScoreReference(game).EstablishCity(participantIdx, 0, horizLoc, vertLoc, size),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Founding.EstablishCity(participantIdx, 0, horizLoc, vertLoc, size)),
					Register.Acc | Register.Kept,
					game => game.Processor.RegAcc.Value16 == 0xffff ? "no city" : participantIdx == person ? "human city" : "AI city");
			}, output, ["no city", "human city", "AI city"], [0x0d, 0x0d, 0x1b, 0x0d]);
		}
	}
}
