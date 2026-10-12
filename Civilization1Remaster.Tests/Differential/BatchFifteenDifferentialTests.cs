using System.Linq;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The most central city, the star field, the intro clouds and the conqueror screen against their original code
	/// </summary>
	public class BatchFifteenDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void ObtainDistanceToCapitalMatchesOriginalCode()
		{
			Check(3, 40, (chance, pair) =>
			{
				short participantIdx = (short)chance.Next(8);
				short[] own = Enumerable.Range(0, 128).Select(idx => (short)idx)
					.Where(idx => pair.Original.Engine.World.Towns[idx].ConditionMarker != 0xff &&
						pair.Original.Engine.World.Towns[idx].ParticipantIdx == participantIdx).ToArray();

				// Two cities have equal sums: the first one wins
				bool tie = own.Length > 2 && chance.Next(3) == 0;

				return new Scenario($"ObtainDistanceToCapital({participantIdx}), only two cities left {tie}",
					game =>
					{
						if (tie)
						{
							foreach (short townIdx in own.Skip(2))
								game.World.Towns[townIdx].ConditionMarker = 0xff;
						}
					},
					game => new CivilizationSchismReference(game).ObtainDistanceToCapital(participantIdx),
					game => game.Secession.ObtainDistanceToCapital(participantIdx),
					Register.Acc | Register.Kept, game => game.Processor.RegAcc.Value16 == 0xffff ? "no city" : tie ? "tie" : "central city");
			}, output, ["no city", "tie", "central city"]);
		}

		[Fact]
		public void RenderStarFieldMatchesOriginalCode()
		{
			Check(3, 200, (chance, pair) =>
			{
				byte[] columns = new byte[20], rows = new byte[20], drawn = new byte[20];

				for (int starIdx = 0; starIdx < 20; starIdx++)
				{
					columns[starIdx] = (byte)(chance.Next(4) == 0 ? chance.Next(0xec, 0x100) : chance.Next(0xf0));
					rows[starIdx] = (byte)chance.Next(8, 200);
					drawn[starIdx] = (byte)chance.Next(2);
				}

				ushort frame = (ushort)chance.Next(0x10000);
				bool wraps = false;

				for (int starIdx = 0; starIdx < 20; starIdx++)
					wraps |= columns[starIdx] + 2 >= 0xf0;

				return new Scenario($"RenderStarField(), frame {frame}",
					game =>
					{
						for (int starIdx = 0; starIdx < 20; starIdx++)
						{
							game.Processor.EmitGlobalByte((ushort)(0x683e + starIdx), columns[starIdx]);
							game.Processor.EmitGlobalByte((ushort)(0x6852 + starIdx), rows[starIdx]);
							game.Processor.EmitGlobalByte((ushort)(0x6866 + starIdx), drawn[starIdx]);
						}

						game.Processor.EmitGlobalWord(0x687a, frame);
					},
					game => new SpaceshipScreenReference(game).RenderStarField(),
					game => game.RocketScreen.RenderStarField(),
					Register.Kept, game => (frame & 1) != 0 ? "odd frame" : wraps ? "even frame, stars start again" : "even frame");
			}, output, ["odd frame", "even frame, stars start again", "even frame"]);
		}

		[Fact]
		public void ProduceCloudLayerMatchesOriginalCode()
		{
			Check(3, 20, (chance, pair) => new Scenario("ProduceCloudLayer()", game => { },
				game => new GameInitAndIntroReference(game).ProduceCloudLayer(),
				game => game.WorldSetup.ProduceCloudLayer(),
				Register.Kept, game => "clouds"), output, ["clouds"]);
		}

		[Fact]
		public void DisplayConquerorScreenMatchesOriginalCode()
		{
			Check(3, 4, (chance, pair) =>
			{
				ushort cycling = (ushort)chance.Next(2);

				return new Scenario($"DisplayConquerorScreen(), colour cycles {cycling}",
					game => game.Processor.EmitGlobalWord(0xd762, cycling),
					game => new GameReplayReference(game).DisplayConquerorScreen(),
					game => game.Replayer.DisplayConquerorScreen(),
					Register.Kept, game => cycling != 0 ? "with colour cycles" : "without colour cycles",
					[ScreenAndMapDifferentialTests.ReplayCities, (0x3772, 0x37c4, 0xe)]);
			}, output, ["with colour cycles", "without colour cycles"], [0x0d, 0x20, 0x1b]);
		}
	}
}
