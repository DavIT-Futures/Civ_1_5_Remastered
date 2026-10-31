using System.Linq;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The production choice of AI cities, the replay names, the city view pictures, the tutorial pointer, the king screen,
	/// the Civilopedia menu and the government picture against their original code
	/// </summary>
	public class BatchEightDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void PickNewCityProductionMatchesOriginalCode()
		{
			Check(10, 200, (chance, pair) =>
			{
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx)
					.Where(idx => pair.Original.Engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short townIdx = towns[chance.Next(towns.Length)];
				sbyte making = (sbyte)chance.Next(-25, 28);

				return new Scenario($"PickNewCityProduction({townIdx}), making {making}",
					game => game.World.Towns[townIdx].PresentMakingIdx = making,
					game => new AIPlayerReference(game).PickNewCityProduction(townIdx),
					game => game.Bot.PickNewCityProduction(townIdx),
					Register.Kept,
					game => (making >= 0 ? "from a unit" : "from a building") + (game.World.Towns[townIdx].PresentMakingIdx >= 0 ? " to a unit" : " to a building"));
			}, output, ["from a unit to a unit", "from a unit to a building", "from a building to a unit", "from a building to a building"]);
		}

		[Fact]
		public void CityViewAndReplayNamesMatchOriginalCode()
		{
			Check(3, 300, (chance, pair) =>
			{
				switch (chance.Next(3))
				{
					case 0:
						short participantIdx = (short)chance.Next(8);
						bool noPlural = chance.Next(2) == 0;

						return new Scenario($"ObtainReplayPlayerLabel({participantIdx}), no plural {noPlural}",
							game =>
							{
								if (noPlural)
									game.Processor.EmitGlobalByte((ushort)(0x14f2 + (0x3a * game.World.Participants[participantIdx].ChiefVisuals)), 0);
							},
							game => new GameReplayReference(game).ObtainReplayPlayerLabel(participantIdx),
							game => game.Replayer.ObtainReplayPlayerLabel(participantIdx),
							Register.Kept, game => noPlural ? "name with s" : "plural name");

					case 1:
						ushort column = (ushort)chance.Next(20);
						ushort row = (ushort)chance.Next(20);
						ushort picture = (ushort)chance.Next(4);
						ushort kind = (ushort)chance.Next(8);

						return new Scenario($"RenderCityPictureRegion({column}, {row}, {picture}, {kind})", game => { },
							game => new CityViewReference(game).RenderCityPictureRegion(column, row, picture, kind),
							game => game.CityScene.RenderCityPictureRegion(column, row, picture, kind),
							Register.Kept, game => "city picture");

					default:
						ushort box = (ushort)chance.Next(22);
						bool firstTime = chance.Next(2) == 0;

						// 22 sensible rectangles: as corners the first time (the right of rectangle 1 above 300), else as sizes
						ushort[] boxes = new ushort[22 * 4];

						for (int idx = 0; idx < 22; idx++)
						{
							ushort left = (ushort)(idx == 1 && firstTime ? chance.Next(280, 300) : chance.Next(0, 200));
							ushort top = (ushort)chance.Next(0, 150);
							ushort width = (ushort)chance.Next(1, 20);
							ushort height = (ushort)chance.Next(1, 40);

							(boxes[idx * 4], boxes[(idx * 4) + 1]) = (left, top);
							(boxes[(idx * 4) + 2], boxes[(idx * 4) + 3]) = firstTime ?
								((ushort)(left + width - 1), (ushort)(top + height - 1)) : (width, height);
						}

						return new Scenario($"SeizeCityScenery({box}), first time {firstTime}",
							game =>
							{
								for (int idx = 0; idx < boxes.Length; idx++)
									game.Processor.EmitUInt16(0x3767, (ushort)(idx << 1), boxes[idx]);
							},
							game => new CityViewReference(game).SeizeCityScenery(box),
							game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.CityScene.SeizeCityScenery(box)),
							Register.Kept, game => firstTime ? "first time" : "boxes ready",
							[(0x3767, 0, 0xb0)]);
				}
			}, output, ["name with s", "plural name", "city picture", "first time", "boxes ready"]);
		}

		[Fact]
		public void KingAndTutorialMatchOriginalCode()
		{
			Check(3, 120, (chance, pair) =>
			{
				switch (chance.Next(3))
				{
					case 0:
						ushort loaded = (ushort)chance.Next(2);

						return new Scenario($"ReleaseKingResources(), loaded {loaded}",
							game => game.Processor.EmitGlobalWord(0x67ea, loaded),
							game => new MeetWithKingReference(game).ReleaseKingResources(),
							game => game.Audience.ReleaseKingResources(),
							Register.Kept, game => loaded != 0 ? "release" : "nothing loaded");

					case 1:
						ushort horizLoc = (ushort)(chance.Next(2) == 0 ? 0x14 : chance.Next(100));
						ushort vertLoc = (ushort)chance.Next(130, 180);

						return new Scenario($"DisplayKingDialog(0xba06, {horizLoc}, {vertLoc})",
							game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Libc.CopyText(0xba06, "We meet again.\n")),
							game => new MeetWithKingReference(game).DisplayKingDialog(0xba06, horizLoc, vertLoc),
							game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Audience.DisplayKingDialog(0xba06, horizLoc, vertLoc)),
							Register.Acc | Register.Kept, game => horizLoc == 0x14 ? "moved dialog" : "dialog");

					default:
						int fromHoriz = chance.Next(320), fromVert = chance.Next(200), toHoriz = chance.Next(200), toVert = chance.Next(150);

						return new Scenario($"RenderTutorialPointer({fromHoriz}, {fromVert}, {toHoriz}, {toVert})",
							game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Libc.CopyText(0xba06, "Hint")),
							game => new MainIntroReference(game).RenderTutorialPointer(fromHoriz, fromVert, toHoriz, toVert),
							game => game.Prologue.RenderTutorialPointer(fromHoriz, fromVert, toHoriz, toVert),
							Register.Kept, game => "tutorial");
				}
			}, output, ["release", "nothing loaded", "moved dialog", "dialog", "tutorial"]);
		}

		[Fact]
		public void CivilopediaMenuMatchesOriginalCode()
		{
			// Escape leaves the menu or the Civilopedia, the arrows and Enter choose a part
			int[] keys = { 0x1b, 0x5000, 0x0d, 0x1b, 0x1b, 0x0d, 0x1b, 0x1b, 0x5000, 0x5000, 0x0d, 0x1b, 0x1b };

			Check(3, 30, (chance, pair) => new Scenario("DisplayCivilopediaMenu()", game => { },
				game => new GameMenusReference(game).DisplayCivilopediaMenu(),
				game => game.MenuSystem.DisplayCivilopediaMenu(),
				Register.Kept, game => game.Processor.PullGlobalWord(0x654a) == 1 ? "escape" : "part shown"),
				output, ["escape", "part shown"], keys);
		}

		[Fact]
		public void ImportGovernmentGraphicsMatchesOriginalCode()
		{
			Check(10, 60, (chance, pair) =>
			{
				short regime = (short)chance.Next(6);
				bool flip = chance.Next(2) == 0;

				return new Scenario($"ImportGovernmentGraphics(), government {regime}, technology 20 changed {flip}",
					game =>
					{
						game.World.Participants[game.World.PersonParticipantIdx].RegimeKind = regime;

						if (flip)
							game.World.Participants[game.World.PersonParticipantIdx].FoundScienceMarkers[1] ^= 0x10;
					},
					game => new StartGameMenuReference(game).ImportGovernmentGraphics(),
					game => game.NewGameWizard.ImportGovernmentGraphics(),
					Register.Kept,
					game => regime == 3 ? "government 3" :
						game.Advisors.PlayerKnowsTechnology(game.World.PersonParticipantIdx, 0x14) != 0 ? "with technology 20" : "without technology 20");
			}, output, ["government 3", "with technology 20", "without technology 20"]);
		}
	}
}
