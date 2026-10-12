using System.Linq;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// Small rewritten functions that the golden master doesn't play, against their original code
	/// </summary>
	public class ScreenAndMapDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void AttachSmogMatchesOriginalCode()
		{
			int lastHoriz = 10, lastVert = 10;

			Check(3, 200, (chance, pair) =>
			{
				(int horizLoc, int vertLoc) = chance.Next(3) == 0 ? (lastHoriz, lastVert) : RandomTile(pair.Original.Engine, chance);
				(lastHoriz, lastVert) = (horizLoc, vertLoc);
				bool polluted = (pair.Original.Engine.MapOps.Chart.Improvements(horizLoc, vertLoc) & 0x40) != 0;

				return new Scenario($"AttachSmog({horizLoc}, {vertLoc})", game => { },
					game => new CityManagementReference(game).AttachSmog(horizLoc, vertLoc),
					game => game.CityOps.AttachSmog(horizLoc, vertLoc),
					Register.Kept, game => polluted ? "polluted already" : "new pollution");
			}, output, ["polluted already", "new pollution"]);
		}

		[Fact]
		public void RenderScoreIconMatchesOriginalCode()
		{
			Check(3, 200, (chance, pair) =>
			{
				ushort iconLeft = (ushort)chance.Next(0x140);
				ushort iconTop = (ushort)chance.Next(0xc0);
				ushort rowHeight = (ushort)chance.Next(4, 24);
				ushort picture = (ushort)chance.Next(8);

				return new Scenario($"RenderScoreIcon({picture}) at {iconLeft}, {iconTop}, rows of {rowHeight}",
					game =>
					{
						game.Processor.EmitGlobalWord(0xb25c, iconLeft);
						game.Processor.EmitGlobalWord(0xb2bc, iconTop);
						game.Processor.EmitGlobalWord(0x5116, rowHeight);
					},
					game => new CityFoundingAndScoreReference(game).RenderScoreIcon(picture),
					game => game.Founding.RenderScoreIcon(picture),
					Register.Kept,
					game => (short)(iconLeft + 8) > 0x124 ? "new row" : (short)iconTop <= 0xa0 ? "drawn" : "below the screen");
			}, output, ["new row", "drawn", "below the screen"]);
		}

		[Fact]
		public void MapMakingMatchesOriginalCode()
		{
			Check(3, 300, (chance, pair) =>
			{
				int horizLoc = chance.Next(1, 79);
				int vertLoc = chance.Next(1, 49);

				if (chance.Next(2) == 0)
				{
					byte[] corners = Enumerable.Range(0, 4).Select(_ => (byte)(chance.Next(4) == 0 ? 1 : chance.Next(3) * 2)).ToArray();

					return new Scenario($"BlendTerrainTile({horizLoc}, {vertLoc}), corners {string.Join(" ", corners)}",
						game =>
						{
							game.VideoCard.AssignDot(1, horizLoc - 1, vertLoc - 1, corners[0], 0);
							game.VideoCard.AssignDot(1, horizLoc - 1, vertLoc + 1, corners[1], 0);
							game.VideoCard.AssignDot(1, horizLoc + 1, vertLoc - 1, corners[2], 0);
							game.VideoCard.AssignDot(1, horizLoc + 1, vertLoc + 1, corners[3], 0);
						},
						game => new GameInitAndIntroReference(game).BlendTerrainTile((short)horizLoc, (short)vertLoc),
						game => game.WorldSetup.BlendTerrainTile((short)horizLoc, (short)vertLoc),
						Register.Acc | Register.Kept, game => corners.Contains((byte)1) ? "land corner" : "becomes land");
				}

				short pixelHoriz = (short)chance.Next(320);
				short pixelVert = (short)chance.Next(200);

				return new Scenario($"ObtainTerrainTypeFromScreen({pixelHoriz}, {pixelVert})", game => { },
					game => new GameInitAndIntroReference(game).ObtainTerrainTypeFromScreen(pixelHoriz, pixelVert),
					game => game.WorldSetup.ObtainTerrainTypeFromScreen(pixelHoriz, pixelVert),
					Register.Acc | Register.Kept, game => "terrain");
			}, output, ["land corner", "becomes land", "terrain"]);
		}

		[Fact]
		public void LocateNearestCityOfPlayerMatchesOriginalCode()
		{
			Check(10, 300, (chance, pair) =>
			{
				short participantIdx = (short)chance.Next(8);
				(int horizLoc, int vertLoc) = RandomTile(pair.Original.Engine, chance);

				if (chance.Next(2) == 0)
				{
					City[] towns = pair.Original.Engine.World.Towns.Where(town => town.ConditionMarker != 0xff && town.ParticipantIdx == participantIdx).ToArray();

					if (towns.Length != 0)
					{
						City town = towns[chance.Next(towns.Length)];
						(horizLoc, vertLoc) = (town.Place.X + chance.Next(-6, 7), town.Place.Y + chance.Next(-6, 7));
					}
				}

				return new Scenario($"LocateNearestCityOfPlayer({participantIdx}, {horizLoc}, {vertLoc})", game => { },
					game => new GovernmentAndDiplomacyReference(game).LocateNearestCityOfPlayer(participantIdx, horizLoc, vertLoc),
					game => game.Politics.LocateNearestCityOfPlayer(participantIdx, horizLoc, vertLoc),
					Register.Acc | Register.Kept, game => game.Processor.RegAcc.Value16 < 16 ? "near a palace" : "far");
			}, output, ["near a palace", "far"]);
		}

		[Fact]
		public void SpaceshipMatchesOriginalCode()
		{
			Check(3, 300, (chance, pair) =>
			{
				short participantIdx = (short)chance.Next(8);

				if (chance.Next(10) == 0)
				{
					return new Scenario($"WipeSpaceshipContent({participantIdx})", game => { },
						game => new StartGameMenuReference(game).WipeSpaceshipContent(participantIdx),
						game => game.NewGameWizard.WipeSpaceshipContent(participantIdx),
						Register.Kept, game => "wiped");
				}

				ushort row = (ushort)chance.Next(15);
				ushort column = (ushort)chance.Next(12);
				byte part = new byte[] { 0xff, 0, 1, 2, 3, 7, 0x80, 0xfe }[chance.Next(8)];

				return new Scenario($"ObtainSpaceshipPartValue({participantIdx}, {row}, {column}), part 0x{part:x2}",
					game => game.Processor.EmitUInt8(0x3772, (ushort)(0x16e0 + (0xb4 * participantIdx) + (0xc * row) + column), part),
					game => new SpaceshipScreenReference(game).ObtainSpaceshipPartValue(participantIdx, row, column),
					game => game.RocketScreen.ObtainSpaceshipPartValue(participantIdx, row, column),
					Register.Acc | Register.Kept, game => game.Processor.RegAcc.Value16 != 0 ? "taken" : "free");
			}, output, ["wiped", "taken", "free"]);
		}

		[Fact]
		public void ReplayCityTableMatchesOriginalCode()
		{
			Check(3, 300, (chance, pair) =>
			{
				ushort[] table = Enumerable.Range(0, 0x80).Select(_ => (ushort)(chance.Next(3) == 0 ? 0xffff : chance.Next(4))).ToArray();
				ushort first = (ushort)chance.Next(0x10000);
				ushort second = (ushort)chance.Next(4);
				ushort third = (ushort)chance.Next(4);
				bool full = chance.Next(8) == 0;
				bool alternate = chance.Next(2) == 0;

				return new Scenario($"AssignReplayCityContent{(alternate ? "Alt" : "")}({first}, {second}, {third}), full {full}",
					game =>
					{
						for (int idx = 0; idx < 0x80; idx++)
						{
							game.Processor.EmitUInt16(0x3725, (ushort)(idx << 1), full && table[idx] == 0xffff ? (ushort)0 : table[idx]);
							game.Processor.EmitUInt16(0x3725, (ushort)((idx << 1) + 0x100), (ushort)(table[idx] & 3));
							game.Processor.EmitUInt16(0x3725, (ushort)((idx << 1) + 0x200), (ushort)((table[idx] >> 1) & 3));
						}
					},
					game =>
					{
						if (alternate)
							new GameReplayReference(game).AssignReplayCityContentAlt(first, second, third);
						else
							new GameReplayReference(game).AssignReplayCityContent(first, second, third);
					},
					game =>
					{
						if (alternate)
							game.Replayer.AssignReplayCityContentAlt(first, second, third);
						else
							game.Replayer.AssignReplayCityContent(first, second, third);
					},
					Register.Kept, game => (alternate ? "change" : "add") + (full ? " to a full table" : ""));
			}, output, ["change", "add", "add to a full table"]);
		}
	}

	/// <summary>
	/// Rewritten functions that open screens (the city screen from an advisor, the king, the new game choices), with the
	/// default key script
	/// </summary>
	public class DialogDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void DialogsMatchOriginalCode()
		{
			Check(3, 60, (chance, pair) =>
			{
				switch (chance.Next(3))
				{
					case 0:
						short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx)
							.Where(idx => pair.Original.Engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();

						if (towns.Length == 0)
							return null;

						short townIdx = towns[chance.Next(towns.Length)];

						return new Scenario($"RenewCityScreen({townIdx})", game => { },
							game => new ScienceAndAdvisorsReference(game).RenewCityScreen(townIdx),
							game => game.Advisors.RenewCityScreen(townIdx),
							Register.Kept, game => game.Processor.PullGlobalWord(0x6b64) != 0 ? "main screen" : "advisor screen");

					case 1:
						short participantIdx = (short)chance.Next(1, 8);

						return new Scenario($"DisplayKingMessage({participantIdx})", game => { },
							game => new MeetWithKingReference(game).DisplayKingMessage(participantIdx),
							game => game.Audience.DisplayKingMessage(participantIdx),
							Register.Kept, game => "king");

					default:
						ushort mouse = (ushort)chance.Next(2);

						return new Scenario($"FreshWorldChoices(), mouse {mouse}",
							game => game.Processor.EmitGlobalWord(0x1a3c, mouse),
							game => new GameStartupReference(game).FreshWorldChoices(),
							game => game.Startup.FreshWorldChoices(),
							Register.Kept, game => mouse != 0 ? "choices with mouse" : "choices");
				}
			}, output, ["advisor screen", "king", "choices", "choices with mouse"]);
		}
	}
}
