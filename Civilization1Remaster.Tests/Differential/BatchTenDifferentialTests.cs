using System;
using System.IO;
using System.Linq;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The Hall of Fame file, the civilization name question, the end of an audience and the plunder of a city against their
	/// original code
	/// </summary>
	public class BatchTenDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void HallOfFameFileMatchesOriginalCode()
		{
			(ushort, ushort, int)[] hallOfFame = [(0x3772, 0x37d2, 0x114)];

			Check(3, 30, (chance, pair) =>
			{
				byte[] content = new byte[0x114];
				chance.NextBytes(content);
				bool missing = chance.Next(4) == 0;

				void Setup(GameEngine game)
				{
					for (int idx = 0; idx < content.Length; idx++)
						game.Processor.EmitUInt8(0x3772, (ushort)(0x37d2 + idx), content[idx]);

					if (missing)
						File.Delete(Path.Combine(game.Processor.StandardFolder, "fame.dta"));
				}

				if (chance.Next(2) == 0)
				{
					return new Scenario($"StoreHallOfFame(), file missing {missing}", Setup,
						game => new HallOfFameReference(game).StoreHallOfFame(),
						game => game.Legends.StoreHallOfFame(),
						Register.Kept, game => "store", hallOfFame);
				}

				return new Scenario($"ImportHallOfFame(), file missing {missing}", Setup,
					game => new HallOfFameReference(game).ImportHallOfFame(),
					game => game.Legends.ImportHallOfFame(),
					Register.Kept, game => missing ? "import without file" : "import", hallOfFame);
			}, output, ["store", "import", "import without file"]);
		}

		[Fact]
		public void CivilizationNameQuestionMatchesOriginalCode()
		{
			// The letters overwrite the name from its start: a plural with "s", one without, nothing, 11 letters. An empty name
			// (the last character is then the one before the text) never comes back: PullTextInput keeps at least a space.
			int[] keys = new[] { "Romans", "Zulu", "", "Abcdefghijk", "Mongolians", "" }
				.SelectMany(name => name.Select(letter => (int)letter).Append(0x0d)).ToArray();

			Check(10, 30, (chance, pair) =>
			{
				short personIdx = (short)chance.Next(1, 8);
				bool empty = chance.Next(3) == 0; // the field starts empty

				return new Scenario($"DisplayCityFoundedMessage(), human {personIdx}, empty plural {empty}",
					game =>
					{
						game.World.PersonParticipantIdx = personIdx;

						if (empty)
							game.Processor.EmitGlobalByte(game.Processor.PullGlobalWord((ushort)((personIdx << 1) + 0x1992)), 0);
					},
					game => new NewCityScreenReference(game).DisplayCityFoundedMessage(),
					game => game.CityNaming.DisplayCityFoundedMessage(),
					Register.Kept,
					game =>
					{
						ushort plural = game.Processor.PullGlobalWord((ushort)((personIdx << 1) + 0x1992));
						string typed = game.Processor.PullText(CPU.AsLinearAddress(0x3b01, plural));

						return typed.Length == 0 ? "empty" : typed.EndsWith('s') ? "plural with s" : typed.Length > 10 ? "cut to 10" : "kept";
					});
			}, output, ["plural with s", "cut to 10", "kept"], keys);
		}

		[Fact]
		public void LogAudienceMatchesOriginalCode()
		{
			Check(3, 60, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				short participantIdx = (short)chance.Next(1, 8);
				short hardness = (short)(chance.Next(3) == 0 ? chance.Next(1, 5) : 0);
				short personIdx = engine.World.PersonParticipantIdx;
				short[] humanTowns = Enumerable.Range(0, 128).Select(idx => (short)idx)
					.Where(idx => engine.World.Towns[idx].ConditionMarker != 0xff && engine.World.Towns[idx].ParticipantIdx == personIdx).ToArray();

				// Some units of the player are moved near cities of the human (at most 3 away)
				var moves = Enumerable.Range(0, chance.Next(4)).Select(_ =>
				{
					if (humanTowns.Length == 0 || RandomUnit(engine, chance, participantIdx) is not (short, short) unit)
						return ((short)-1, 0, 0);

					var town = engine.World.Towns[humanTowns[chance.Next(humanTowns.Length)]];

					return (unit.Item2, (town.Place.X + chance.Next(-3, 4) + 80) % 80, Math.Clamp(town.Place.Y + chance.Next(-3, 4), 1, 48));
				}).Where(move => move.Item1 >= 0).ToArray();

				int eliminations = Calls(pair.Rewritten.Engine, pair, "EliminateUnit");

				return new Scenario($"LogAudience({participantIdx}), level {hardness}, {moves.Length} units moved",
					game =>
					{
						game.World.HardnessTier = hardness;
						UnitMemory troops = new UnitMemory(game.Processor);

						foreach ((short troopIdx, int horizLoc, int vertLoc) in moves)
						{
							troops.Assign(participantIdx, troopIdx, UnitMemory.Horiz, (byte)horizLoc);
							troops.Assign(participantIdx, troopIdx, UnitMemory.Vert, (byte)vertLoc);
						}
					},
					game => new MeetWithKingReference(game).LogAudience(participantIdx),
					game => game.Audience.LogAudience(participantIdx),
					Register.Kept,
					game => hardness != 0 ? "not the easiest level" : Calls(game, pair, "EliminateUnit") > eliminations ? "units removed" : "none near");
			}, output, ["not the easiest level", "units removed", "none near"]);
		}

		[Fact]
		public void ComputeCityPlunderMatchesOriginalCode()
		{
			Check(10, 300, (chance, pair) =>
			{
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx)
					.Where(idx => pair.Original.Engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short townIdx = towns[chance.Next(towns.Length)];
				short ownerIdx = pair.Original.Engine.World.Towns[townIdx].ParticipantIdx;
				sbyte size = (sbyte)(chance.Next(10) == 0 ? chance.Next(-128, 128) : chance.Next(1, 30));
				short gold = (short)(chance.Next(5) == 0 ? chance.Next(-32768, 32768) : chance.Next(0, 3000));
				short extent = (short)(chance.Next(10) == 0 ? chance.Next(-2, 2) : chance.Next(1, 200));

				// The edge between the two ways: the gold is just 32000 / size
				if (size != 0 && chance.Next(4) == 0)
					gold = (short)(32000 / size + chance.Next(-1, 2));

				// The original leaves its frame and registers behind when it fails (the game stops there), they are put back
				void Original(GameEngine game)
				{
					CPU processor = game.Processor;
					(ushort stack, ushort frame, ushort source, ushort dest) = (processor.RegStack.Value16, processor.RegFrame.Value16,
						processor.RegSource.Value16, processor.RegDest.Value16);

					try
					{
						new TradeAndConquestReference(game).ComputeCityPlunder(townIdx);
					}
					catch
					{
						(processor.RegStack.Value16, processor.RegFrame.Value16, processor.RegSource.Value16, processor.RegDest.Value16) =
							(stack, frame, source, dest);
						throw;
					}
				}

				bool fails = size == 0 || extent == -1;

				return new Scenario($"ComputeCityPlunder({townIdx}), size {size}, gold {gold}, total size {extent}",
					game =>
					{
						game.World.Towns[townIdx].TrueExtent = size;
						game.World.Participants[ownerIdx].Gold = gold;
						game.World.Participants[ownerIdx].GrandTownExtent = extent;
					},
					Original,
					game => game.Conquest.ComputeCityPlunder(townIdx),
					fails ? Register.Kept : Register.Acc | Register.Kept,
					game => fails ? "division by zero" : 32000 / size > gold ? "multiplied first" : 32000 / size == gold ? "edge" : "divided first");
			}, output, ["division by zero", "multiplied first", "edge", "divided first"]);
		}
	}
}
