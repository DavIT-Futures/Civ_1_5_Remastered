using System.IO;
using System.Linq;
using System.Text;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The mouse pointer, the name question, the army list question, the floppy check, the text codes and the Civilopedia
	/// against their original code
	/// </summary>
	public class BatchNineDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void MouseCursorMatchesOriginalCode()
		{
			Check(3, 300, (chance, pair) =>
			{
				ushort picture = (ushort)(chance.Next(3) == 0 ? 0 : chance.Next(1, 0x400));
				byte drawn = (byte)chance.Next(2);
				ushort mouse = (ushort)chance.Next(2);
				ushort level = (ushort)chance.Next(-2, 3);
				ushort horizLoc = (ushort)chance.Next(320), vertLoc = (ushort)chance.Next(200);
				ushort hotHoriz = (ushort)chance.Next(16), hotVert = (ushort)chance.Next(16);
				byte shift = (byte)chance.Next(2);

				void Setup(Civilization1Remaster.GameEngine game)
				{
					game.Processor.EmitGlobalWord(0x5876, picture);
					game.Processor.EmitGlobalByte(0x5403, drawn);
					game.Processor.EmitGlobalWord(0x1a3c, mouse);
					game.Processor.EmitGlobalWord(0xdeea, level);
					game.Processor.EmitGlobalWord(0x586e, horizLoc);
					game.Processor.EmitGlobalWord(0x5870, vertLoc);
					game.Processor.EmitGlobalWord(0x5878, hotHoriz);
					game.Processor.EmitGlobalWord(0x587a, hotVert);
					game.Processor.EmitGlobalByte(0x587d, shift);
				}

				string state = $"picture {picture}, drawn {drawn}, mouse {mouse}, level {(short)level}";
				int renders = Calls(pair.Rewritten.Engine, pair, "RenderCursorToScreen");
				int recovers = Calls(pair.Rewritten.Engine, pair, "RecoverScreenUnderCursor");
				string Drawn(Civilization1Remaster.GameEngine game) =>
					Calls(game, pair, "RenderCursorToScreen") > renders ? "drawn" :
					Calls(game, pair, "RecoverScreenUnderCursor") > recovers ? "blanked" : "unchanged";

				// The mouse state is left out of the snapshot (GameSnapshot.IgnoredDataRanges)
				(ushort, ushort, int)[] mouseState = [(0x3b01, 0x586e, 0x10)];

				switch (chance.Next(4))
				{
					case 0:
						return new Scenario($"RenderMouseCursor(), {state}", Setup,
							game => new SystemServicesReference(game).RenderMouseCursor(),
							game => game.Services.RenderMouseCursor(),
							Register.Kept, game => "render " + Drawn(game), mouseState);

					case 1:
						return new Scenario($"BlankMouseCursor(), {state}", Setup,
							game => new SystemServicesReference(game).BlankMouseCursor(),
							game => game.Services.BlankMouseCursor(),
							Register.Kept, game => "blank " + Drawn(game), mouseState);

					case 2:
						ushort toHoriz = (ushort)chance.Next(320), toVert = (ushort)chance.Next(200);

						return new Scenario($"AssignMousePosition({toHoriz}, {toVert}), {state}", Setup,
							game => new SystemServicesReference(game).AssignMousePosition(toHoriz, toVert),
							game => game.Services.AssignMousePosition(toHoriz, toVert),
							Register.Kept, game => "position", mouseState);

					default:
						return new Scenario($"DisplayMouseCursor(), {state}", Setup,
							game => new GameStartupReference(game).DisplayMouseCursor(),
							game => game.Startup.DisplayMouseCursor(),
							Register.Kept, game => "display " + Drawn(game), mouseState);
				}
			}, output, ["render drawn", "render unchanged", "blank blanked", "blank unchanged", "position", "display drawn", "display unchanged"]);
		}

		[Fact]
		public void NameAndArmyQuestionsMatchOriginalCode()
		{
			// A name typed (with a backspace) and accepted; the arrows and Enter choose an army, any key closes its list
			int[] keys = { 0x41, 0x62, 0x08, 0x63, 0x0d, 0x5000, 0x0d, 0x20, 0x4800, 0x0d, 0x0d, 0x0d, 0x1b, 0x31, 0x0d };

			Check(10, 40, (chance, pair) =>
			{
				if (chance.Next(2) == 0)
				{
					short personIdx = (short)chance.Next(1, 8);

					return new Scenario($"DisplayFoundCityMessage(), human {personIdx}",
						game => game.World.PersonParticipantIdx = personIdx,
						game => new NewCityScreenReference(game).DisplayFoundCityMessage(),
						game => game.CityNaming.DisplayFoundCityMessage(),
						Register.Kept, game => "name");
				}

				return new Scenario("DisplayWhichArmy()", game => { },
					game => new ArmySelectionReference(game).DisplayWhichArmy(),
					game => game.ArmyPicker.DisplayWhichArmy(),
					Register.Acc | Register.Kept, game => "army");
			}, output, ["name", "army"], keys);
		}

		[Fact]
		public void VerifyDriveMatchesOriginalCode()
		{
			// The ready branch needs a floppy drive A: or B:
			bool floppy = DriveInfo.GetDrives().Any(drive => (drive.Name[0] == 'A' || drive.Name[0] == 'B') && drive.IsReady);

			ushort[] drives = { 0, 1, 2, 3, 0x80, 0xffff, 0x8000 };
			int scenarioIdx = 0;

			Check(3, drives.Length * 2, (chance, pair) =>
			{
				ushort driveIdx = drives[scenarioIdx++ % drives.Length];

				return new Scenario($"VerifyDrive({driveIdx})", game => { },
					game => new GameLoadAndSaveReference(game).VerifyDrive(driveIdx),
					game => game.SaveLoad.VerifyDrive(driveIdx),
					Register.Acc | Register.Kept,
					game => (short)driveIdx > 1 ? "not a floppy" : game.Processor.RegAcc.Value16 != 0 ? "ready" : "no disk");
			}, output, floppy ? ["not a floppy", "ready"] : ["not a floppy", "no disk"]);
		}

		[Fact]
		public void ReflowTextMatchesOriginalCode()
		{
			Check(3, 200, (chance, pair) =>
			{
				CPU processor = pair.Original.Engine.Processor;
				string[] codes = Enumerable.Range(0, 5)
					.Select(idx => processor.PullText(CPU.AsLinearAddress(0x3b01, processor.PullGlobalWord((ushort)(0x30ae + (idx << 1))))))
					.ToArray();
				string[] words = { "The ", "city ", "of ", "x", "\n", "", "Rome. ", "%" };
				StringBuilder text = new StringBuilder();
				int codeTally = 0;

				for (int pieceIdx = chance.Next(12); pieceIdx > 0; pieceIdx--)
				{
					if (chance.Next(3) == 0)
					{
						text.Append(codes[chance.Next(codes.Length)]);
						codeTally++;
					}
					else
					{
						text.Append(words[chance.Next(words.Length)]);
					}
				}

				string composed = text.ToString();

				return new Scenario($"ReflowText(), text \"{composed}\"",
					game => game.Libc.CopyText(0xba06, composed),
					game => new TextBlockRendererReference(game).ReflowText(),
					game => game.BlockText.ReflowText(),
					Register.Kept, game => codeTally == 0 ? "no code" : codeTally == 1 ? "one code" : "several codes");
			}, output, ["no code", "one code", "several codes"]);
		}

		[Fact]
		public void DisplayCivilopediaMatchesOriginalCode()
		{
			// 'm' turns the page, Escape leaves, the arrows and Enter show an entry
			int[] keys = { 0x6d, 0x6d, 0x6d, 0x1b, 0x1b, 0x5000, 0x0d, 0x0d, 0x1b, 0x6d, 0x4800, 0x0d, 0x20, 0x1b, 0x1b };

			Check(3, 30, (chance, pair) =>
			{
				ushort part = (ushort)chance.Next(-1, 4);
				int pages = Calls(pair.Rewritten.Engine, pair, "DisplayCivilopediaIndex");

				return new Scenario($"DisplayCivilopedia({part})", game => { },
					game => new CivilopediaReference(game).DisplayCivilopedia(part),
					game => game.Encyclopedia.DisplayCivilopedia(part),
					Register.Kept, game => Calls(game, pair, "DisplayCivilopediaIndex") - pages > 1 ? "several pages" : "one page",
					[(0x3772, 0, 0x8000)]);
			}, output, ["several pages", "one page"], keys);
		}
	}
}
