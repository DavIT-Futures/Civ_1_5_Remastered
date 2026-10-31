using System.Linq;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The Civilopedia lists, the replay distance, the colour cycles, the Hall of Fame, the palette, the city view and
	/// the palace pictures against their original code
	/// </summary>
	public class BatchSevenDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void CivilopediaListsMatchOriginalCode()
		{
			Check(3, 300, (chance, pair) =>
			{
				ushort scienceIdx = (ushort)(chance.Next(-2, 76));
				int list = chance.Next(3);
				string name = new[] { "ObtainFirstPrerequisite", "ObtainSecondPrerequisite", "ObtainTechnologyCategory" }[list];

				return new Scenario($"{name}({scienceIdx})", game => { },
					game =>
					{
						CivilopediaReference reference = new CivilopediaReference(game);
						_ = list == 0 ? reference.ObtainFirstPrerequisite(scienceIdx) :
							list == 1 ? reference.ObtainSecondPrerequisite(scienceIdx) : reference.ObtainTechnologyCategory(scienceIdx);
					},
					game =>
					{
						game.Processor.RegAcc.Value16 = list == 0 ? game.Encyclopedia.ObtainFirstPrerequisite(scienceIdx) :
							list == 1 ? game.Encyclopedia.ObtainSecondPrerequisite(scienceIdx) : game.Encyclopedia.ObtainTechnologyCategory(scienceIdx);
					},
					Register.Acc | Register.Base | Register.Kept,
					game => $"{name} {(game.Processor.RegAcc.Value16 != 0xffff ? "found" : "not found")}");
			}, output, ["ObtainFirstPrerequisite found", "ObtainFirstPrerequisite not found", "ObtainSecondPrerequisite found",
				"ObtainSecondPrerequisite not found", "ObtainTechnologyCategory found", "ObtainTechnologyCategory not found"]);
		}

		[Fact]
		public void ObtainDistanceToReplayCityMatchesOriginalCode()
		{
			Check(3, 200, (chance, pair) =>
			{
				// At least one city: without one the original returns what was left on the stack
				ushort[] firsts = Enumerable.Range(0, 0x80).Select(idx => (ushort)(idx == 0 || chance.Next(4) != 0 ? chance.Next(100) : 0xffff)).ToArray();
				short[] horizs = Enumerable.Range(0, 0x80).Select(_ => (short)chance.Next(80)).ToArray();
				short[] verts = Enumerable.Range(0, 0x80).Select(_ => (short)chance.Next(50)).ToArray();
				(int horizLoc, int vertLoc) = RandomTile(pair.Original.Engine, chance);
				int shuffle = chance.Next(0x80);

				return new Scenario($"ObtainDistanceToReplayCity({horizLoc}, {vertLoc})",
					game =>
					{
						for (int idx = 0; idx < 0x80; idx++)
						{
							int entry = (idx + shuffle) % 0x80;
							game.Processor.EmitUInt16(0x3725, (ushort)(entry << 1), firsts[idx]);
							game.Processor.EmitUInt16(0x3725, (ushort)((entry << 1) + 0x100), (ushort)horizs[idx]);
							game.Processor.EmitUInt16(0x3725, (ushort)((entry << 1) + 0x200), (ushort)verts[idx]);
						}
					},
					game => new GameReplayReference(game).ObtainDistanceToReplayCity(horizLoc, vertLoc),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Replayer.ObtainDistanceToReplayCity(horizLoc, vertLoc)),
					Register.Acc | Register.Kept,
					game => game.Processor.PullGlobalWord(0x6812) == 0 ? "on a city" : "near a city",
					[ScreenAndMapDifferentialTests.ReplayCities]);
			}, output, ["on a city", "near a city"]);
		}

		[Fact]
		public void ColourCyclesMatchOriginalCode()
		{
			Check(3, 200, (chance, pair) =>
			{
				ushort cycle = chance.Next(10) == 0 ? (ushort)0x8000 : (ushort)chance.Next(-10, 11);
				bool start = chance.Next(2) == 0;
				bool valid = (ushort)(System.Math.Abs((int)(short)cycle) - 1) < 8;

				return new Scenario($"{(start ? "Activate" : "Deactivate")}HueCycle({(short)cycle})", game => { },
					game =>
					{
						if (start)
							new SystemServicesReference(game).ActivateHueCycle(cycle);
						else
							new SystemServicesReference(game).DeactivateHueCycle(cycle);
					},
					game =>
					{
						if (start)
							game.Services.ActivateHueCycle(cycle);
						else
							game.Services.DeactivateHueCycle(cycle);
					},
					Register.Kept | Register.Base | (start ? Register.Data : Register.None),
					game => (start ? "start " : "stop ") + ((start ? valid : (ushort)(cycle - 1) < 8) ? "cycle" : "ignored"),
					[(0x35cf, 0, 0x200)]);
			}, output, ["start cycle", "start ignored", "stop cycle", "stop ignored"]);
		}

		[Fact]
		public void PicturesAndTablesMatchOriginalCode()
		{
			Check(3, 300, (chance, pair) =>
			{
				switch (chance.Next(4))
				{
					case 0:
						return new Scenario("WipeHallOfFame()", game => { },
							game => new HallOfFameReference(game).WipeHallOfFame(),
							game => game.Legends.WipeHallOfFame(),
							Register.Kept, game => "hall of fame", [(0x3772, 0x37d2, 6 * 0x2e)]);

					case 1:
						ushort vga = (ushort)chance.Next(2);

						return new Scenario($"PullAndAssignColors(SP257.PAL), VGA {vga}",
							game => game.Processor.EmitGlobalWord(0xd762, vga),
							game => new CommonUtilitiesReference(game).PullAndAssignColors(0x1c0c),
							game => game.Helpers.PullAndAssignColors(0x1c0c),
							Register.Kept, game => vga != 0 ? "palette" : "no palette (EGA)");

					case 2:
						ushort column = (ushort)chance.Next(20);
						ushort row = (ushort)chance.Next(20);
						ushort picture = (ushort)chance.Next(16);

						return new Scenario($"RenderCityPicture({column}, {row}, {picture})", game => { },
							game => new CityViewReference(game).RenderCityPicture(column, row, picture),
							game => game.CityScene.RenderCityPicture(column, row, picture),
							Register.Kept, game => "city picture");

					default:
						ushort level = (ushort)chance.Next(4);
						ushort part = (ushort)chance.Next(13);
						ushort horizLoc = (ushort)chance.Next(300);
						ushort shift = (ushort)chance.Next(100);
						ushort style = (ushort)chance.Next(3);

						return new Scenario($"RenderPalacePart({level}, {part}, {horizLoc}, {shift}, {style})", game => { },
							game => new PalaceReference(game).RenderPalacePart(level, part, horizLoc, shift, style),
							game => game.Throne.RenderPalacePart(level, part, horizLoc, shift, style),
							Register.Kept, game => "palace part");
				}
			}, output, ["hall of fame", "palette", "no palette (EGA)", "city picture", "palace part"]);
		}
	}
}
