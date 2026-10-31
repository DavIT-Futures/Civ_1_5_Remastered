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
	/// Loading a saved game and its line in the list, the sections of the palace, the strategic locations of the AI and the
	/// texts of the text files against their original code
	/// </summary>
	public class BatchThirtyDifferentialTests(ITestOutputHelper output)
	{
		/// <summary>The file name at 0x41c6 (drive, colon, name of 6 letters) gets the name</summary>
		private static void AssignLabel(GameEngine game, string label)
		{
			for (int idx = 0; idx < label.Length; idx++)
				game.Processor.EmitGlobalByte((ushort)(0x41c8 + idx), (byte)label[idx]);
		}

		[Fact]
		public void ImportGameMatchesOriginalCode()
		{
			Check(3, 24, (chance, pair) =>
			{
				ushort marker = (ushort)(chance.Next(5) == 0 ? 0 : 1);
				int kind = marker == 0 ? 0 : chance.Next(4);
				// AUTOnn, a copy with a year BC, a missing file or a manual save
				string label = kind switch { 0 => $"AUTO{chance.Next(15):00}", 1 => "BCSAVE", 2 => "NOFILE", _ => $"CIVIL{chance.Next(10)}" };
				short year = (short)-chance.Next(1, 4000);
				bool backup = chance.Next(2) == 0;

				return new Scenario($"ImportGame(0x41c6, {marker}) of {label}, autosave {backup}",
					game =>
					{
						AssignLabel(game, label);
						game.Processor.EmitGlobalByte(0xba06, 0);

						if (kind == 1)
						{
							byte[] content = File.ReadAllBytes(Path.Combine(game.Processor.StandardFolder, "AUTO00.SVE"));
							(content[8], content[9]) = ((byte)year, (byte)(year >> 8));
							File.WriteAllBytes(Path.Combine(game.Processor.StandardFolder, "BCSAVE.SVE"), content);
						}
						game.SaveLoad.FetchingBackup = backup;
					},
					game => new GameLoadAndSaveReference(game).ImportGame(0x41c6, marker),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.SaveLoad.ImportGame(0x41c6, marker)),
					Register.Acc | Register.Kept,
					game => marker == 0 ? (backup ? "loaded autosave" : "loaded") :
						game.Processor.RegAcc.Value16 == 0 ? "empty" : (short)game.Processor.PullGlobalWord(0x6826) < 0 ? "listed BC" : "listed AD");
			}, output, ["loaded autosave", "loaded", "empty", "listed BC", "listed AD"]);
		}

		[Fact]
		public void RenderPalaceSectionMatchesOriginalCode()
		{
			Check(3, 300, (chance, pair) =>
			{
				ushort section = (ushort)chance.Next(8);
				ushort marker = (ushort)chance.Next(2);
				ushort[] levels = Enumerable.Range(0, 11).Select(_ => chance.Next(3) == 0 ? (ushort)0xffff : (ushort)chance.Next(0, 4)).ToArray();

				// levels[idx] is the word at 0xd91a + 2 * idx, the section's own level is levels[section + 2]
				string place = section < 4 ? (levels[section + 1] == 0xffff || section == 1 ? "left end" : "inner left") :
					section > 4 ? (levels[section + 3] == 0xffff || section == 7 ? "right end" : "inner right") : "middle";
				bool higher = (short)levels[section + 1] > (short)levels[section + 2];

				return new Scenario($"RenderPalaceSection({section}, {marker}), levels {string.Join(" ", levels.Select(level => (short)level))}",
					game =>
					{
						for (int idx = 0; idx < levels.Length; idx++)
							game.Processor.EmitGlobalWord((ushort)(0xd91a + (idx << 1)), levels[idx]);
					},
					game => new PalaceReference(game).RenderPalaceSection(section, marker),
					game => game.Throne.RenderPalaceSection(section, marker),
					Register.Kept,
					game => $"{place}{(higher ? ", left higher" : "")}{(marker == 0 ? ", marker 0" : "")}");
			}, output, ["left end", "left end, marker 0", "inner left", "inner left, left higher", "middle", "right end",
				"right end, left higher, marker 0", "right end, marker 0", "inner right", "inner right, left higher"]);
		}

		[Fact]
		public void SendUnitToStrategicLocationMatchesOriginalCode()
		{
			Check(3, 120, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				UnitMemory troops = new UnitMemory(engine.Processor);
				short participantIdx = (short)chance.Next(1, 8);
				(short, short)? unit = RandomUnit(engine, chance, participantIdx, (player, troopIdx) =>
					troops.KindField(player, troopIdx, UnitKindTable.TroopGroup) is 3 or 4);

				short live, horizLoc, vertLoc;
				short troopIdx = -1;

				if (unit != null && chance.Next(4) != 0)
				{
					troopIdx = unit.Value.Item2;
					live = (short)troops.KindField(participantIdx, troopIdx, UnitKindTable.TroopGroup);
					horizLoc = (short)Math.Clamp(troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) + chance.Next(-4, 5), 0, 79);
					vertLoc = (short)Math.Clamp(troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert) + chance.Next(-4, 5), 1, 48);
				}
				else
				{
					(int horiz, int vert) = RandomTile(engine, chance);
					(horizLoc, vertLoc, live) = ((short)horiz, (short)vert, (short)chance.Next(0, 8));
				}

				short strategy = (short)chance.Next(0, 8);
				byte steps = (byte)chance.Next(0, 12);
				bool aiTurn = chance.Next(5) != 0;
				int used = chance.Next(4) == 0 ? 32 : chance.Next(0, 33);
				bool listed = chance.Next(5) == 0;
				byte[] roles = Enumerable.Range(0, 32).Select(idx => idx < used ? (byte)chance.Next(0, 8) : (byte)0xff).ToArray();
				byte[] strategies = Enumerable.Range(0, 32).Select(_ => (byte)chance.Next(0, 8)).ToArray();
				byte[] horizs = Enumerable.Range(0, 32).Select(_ => (byte)chance.Next(0, 80)).ToArray();
				byte[] verts = Enumerable.Range(0, 32).Select(_ => (byte)chance.Next(0, 50)).ToArray();
				int listedIdx = chance.Next(32);

				if (listed)
				{
					(horizs[listedIdx], verts[listedIdx], roles[listedIdx]) = ((byte)horizLoc, (byte)vertLoc, (byte)live);
					strategies[listedIdx] = (byte)chance.Next(0, strategy + 2);
				}

				bool already = Enumerable.Range(0, 32).Any(idx => horizs[idx] == horizLoc && verts[idx] == vertLoc &&
					(sbyte)roles[idx] == live && (sbyte)strategies[idx] <= strategy);
				bool full = Enumerable.Range(0, 32).All(idx => roles[idx] != 0xff && (sbyte)strategies[idx] >= strategy);

				return new Scenario($"SendUnitToStrategicLocation({participantIdx}, {horizLoc}, {vertLoc}, {live}, {strategy}), unit {troopIdx} steps {steps}, " +
					$"AI turn {aiTurn}, {used} used",
					game =>
					{
						ushort places = (ushort)(participantIdx << 5);

						for (int idx = 0; idx < 32; idx++)
						{
							game.Processor.EmitGlobalByte((ushort)(places + idx + 0xe598), horizs[idx]);
							game.Processor.EmitGlobalByte((ushort)(places + idx + 0xe798), verts[idx]);
							game.Processor.EmitGlobalByte((ushort)(places + idx + 0xe2c2), roles[idx]);
							game.Processor.EmitGlobalByte((ushort)(places + idx + 0xb2be), strategies[idx]);
						}

						game.Processor.EmitGlobalWord(0x6b90, (ushort)(aiTurn ? participantIdx : (participantIdx + 1) % 8));

						if (troopIdx >= 0)
						{
							UnitMemory memory = new UnitMemory(game.Processor);
							memory.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, steps);
							memory.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
						}
					},
					game => new AIPlayerReference(game).SendUnitToStrategicLocation(participantIdx, horizLoc, vertLoc, live, strategy),
					game => game.Bot.SendUnitToStrategicLocation(participantIdx, horizLoc, vertLoc, live, strategy),
					Register.Kept,
					game => already ? "already listed" :
						troopIdx >= 0 && new UnitMemory(game.Processor).Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == (byte)horizLoc ?
							(full ? "unit sent, list full" : "unit sent") :
						full ? "list full" : "listed");
			}, output, ["already listed", "unit sent", "list full", "listed"]);
		}

		[Fact]
		public void ImportTextFromFileMatchesOriginalCode()
		{
			// The text files the game reads (their names in the data segment), and a made one (0xbb40)
			ushort[] docnames = { 0x34de, 0x34b2, 0x2284, 0x22a3, 0x30a5, 0xbb40 };
			string[][]? topics = null;

			Check(3, 80, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				topics ??= docnames.Take(5).Select(docname => File.ReadAllLines(Path.Combine(engine.Processor.StandardFolder, DocName(engine, docname)))
					.Skip(1).Where(line => line.StartsWith('*') && line.Length > 1 && line != "*END").Distinct().ToArray())
					.Append(MadeTopics).ToArray();
				int fileIdx = chance.Next(2) == 0 ? 5 : chance.Next(5);
				ushort docname = docnames[fileIdx];
				string topic = chance.Next(6) == 0 ? $"*NOTOPIC{chance.Next(100)}" : topics[fileIdx][chance.Next(topics[fileIdx].Length)];
				// The index of the made file: 0 none ("00"), 1 the topic line, 2 another topic line (the next hash has the topic), 3 zero,
				// 4 another topic line only
				int[] indexes = MadeTopics.Select(_ => chance.Next(5)).ToArray();
				// The index entry of the topic in a file of the game cleared: the search from 0x200 stops at the empty line after the index
				bool cleared = fileIdx < 5 && chance.Next(4) == 0;
				ushort sp = engine.Processor.RegStack.Value16;

				return new Scenario($"ImportTextFromFile({(fileIdx < 5 ? DocName(engine, docname) : "made")}, \"{topic}\"), " +
					$"index {string.Join("", indexes)}, entry cleared {cleared}",
					game =>
					{
						game.Libc.CopyText(0xbb80, topic);
						game.Libc.CopyText(0xbb40, "MADE.TXT");
						game.Processor.EmitGlobalByte(0xba06, 0);

						if (fileIdx == 5)
						{
							File.WriteAllBytes(Path.Combine(game.Processor.StandardFolder, "MADE.TXT"), MadeFile(indexes));
						}

						if (cleared)
						{
							string route = Path.Combine(game.Processor.StandardFolder, DocName(game, docname));
							byte[] content = File.ReadAllBytes(route);
							int place = ((Hash(topic) >> 5) + Hash(topic)) << 1;
							(content[place], content[place + 1]) = (0, 0);
							File.WriteAllBytes(route, content);
						}
					},
					game =>
					{
						new TextBlockRendererReference(game).ImportTextFromFile(docname, 0xbb80);
						ShowFile(game, DocName(game, docname));
					},
					game =>
					{
						game.BlockText.ImportTextFromFile(docname, 0xbb80);
						ShowFile(game, DocName(game, docname));
					},
					Register.Acc | Register.Kept,
					game =>
					{
						// The locals of the original stay in its frame under the stack: 0x10e indexed, 0x102 the last hash
						bool indexed = pair.Original.Engine.Processor.PullGlobalWord((ushort)(sp - 2 - 0x10e)) != 0;
						bool rehashed = pair.Original.Engine.Processor.PullGlobalWord((ushort)(sp - 2 - 0x102)) != Hash(topic);
						return game.Processor.RegAcc.Value16 == 0xffff ? $"missing{(rehashed ? ", next hash" : "")}" :
							$"{(indexed ? "indexed" : "searched")}{(rehashed ? ", next hash" : "")}";
					});
			}, output, ["missing", "missing, next hash", "indexed", "indexed, next hash", "searched", "searched, next hash"]);
		}

		private static readonly string[] MadeTopics = { "*ALPHA", "*BETA", "*GAMMA", "*DELTA", "*EPSILON", "*ZETA", "*ETA" };

		/// <summary>
		/// A text file like those of the game, without empty lines: the index of 256 words, then the topics with 1 - 3 lines
		/// each (the last ones end with '^'). Index entries outside the index (hash above 0xfc) are left out.
		/// </summary>
		private static byte[] MadeFile(int[] indexes)
		{
			System.Text.StringBuilder text = new System.Text.StringBuilder();
			int[] offsets = new int[MadeTopics.Length];

			for (int idx = 0; idx < MadeTopics.Length; idx++)
			{
				offsets[idx] = 0x200 + text.Length;
				text.Append(MadeTopics[idx]).Append("\r\n");

				for (int lineIdx = 0; lineIdx <= idx % 3; lineIdx++)
					text.Append($"Line {lineIdx} of {MadeTopics[idx].Substring(1).ToLowerInvariant()}{(lineIdx == idx % 3 ? " ^ " : "")}\r\n");
			}

			text.Append("*END\r\n");

			byte[] content = Enumerable.Repeat((byte)'0', 0x200).Concat(System.Text.Encoding.ASCII.GetBytes(text.ToString())).ToArray();

			for (int idx = 0; idx < MadeTopics.Length; idx++)
			{
				int hash = Hash(MadeTopics[idx]);
				int other = offsets[(idx + 1) % MadeTopics.Length];

				(int, int)[] entries = indexes[idx] switch
				{
					1 => [(hash, offsets[idx])],
					2 => [(hash, other), ((hash + 1) & 0xff, offsets[idx])],
					3 => [(hash, 0)],
					4 => [(hash, other)],
					_ => [],
				};

				foreach ((int slot, int offset) in entries)
				{
					int place = ((slot >> 5) + slot) << 1;

					if (place < 0x1fe)
						(content[place], content[place + 1]) = ((byte)offset, (byte)(offset >> 8));
				}
			}

			return content;
		}

		private static string DocName(GameEngine game, ushort docname)
		{
			return game.Processor.PullText(CPU.AsLinearAddress(game.Processor.SegData.Value16, docname));
		}

		/// <summary>The low byte of the sum of character * position</summary>
		private static ushort Hash(string topic)
		{
			int hash = 0;

			for (int idx = 0; idx < topic.Length; idx++)
				hash += (sbyte)topic[idx] * idx;

			return (ushort)(hash & 0xff);
		}

		/// <summary>The snapshot does not see files: the length and a hash of the text file go into the data segment (0xbc00)</summary>
		private static void ShowFile(GameEngine game, string docname)
		{
			byte[] content = File.ReadAllBytes(Path.Combine(game.Processor.StandardFolder, docname));
			uint hash = 2166136261;

			foreach (byte item in content)
				hash = (hash ^ item) * 16777619;

			game.Processor.EmitGlobalWord(0xbc00, (ushort)content.Length);
			game.Processor.EmitGlobalWord(0xbc02, (ushort)hash);
			game.Processor.EmitGlobalWord(0xbc04, (ushort)(hash >> 16));
		}
	}
}
