using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// Help topic popup (DisplayHelpTopic), technology capture/exchange (ChooseTechnologyToTake), and menu layout/interaction
	/// (OperateMenu, DisplayMenu) against their original code
	/// </summary>
	public class BatchSixtyEightDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void DisplayHelpTopicMatchesOriginalCode()
		{
			string[] topics = ["TERRAIN", "DIPLOMAT", "CARAVAN", "MISSING_TOPIC", "orders"];

			Check(2, 15, (chance, pair) =>
			{
				string topic = topics[chance.Next(topics.Length)];

				return new Scenario($"DisplayHelpTopic(\"{topic}\")",
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Libc.CopyText(0xbc00, topic)),
					game => new HelpPopupsReference(game).DisplayHelpTopic(0xbc00),
					game => game.HelpSystem.DisplayHelpTopic(0xbc00),
					Register.Kept,
					game => topic == "MISSING_TOPIC" ? "missing" : "found");
			}, output, ["found", "missing"]);
		}

		[Fact]
		public void ChooseTechnologyToTakeMatchesOriginalCode()
		{
			Check(3, 35, (chance, pair) =>
			{
				Civilization1Remaster.GameEngine engine = pair.Original.Engine;
				short person = engine.World.PersonParticipantIdx;

				int mode = chance.Next(5);
				short p1, p2;

				if (mode == 0)
				{
					p1 = 0;
					p2 = (short)chance.Next(1, 8);
				}
				else if (mode == 1)
				{
					p1 = person;
					p2 = (short)(person == 1 ? 2 : 1);
				}
				else if (mode == 2)
				{
					p1 = (short)(person == 1 ? 2 : 1);
					p2 = person;
				}
				else
				{
					p1 = (short)(person == 1 ? 2 : 1);
					p2 = (short)(person <= 2 ? 3 : 2);
				}

				bool kingDialog = chance.Next(2) == 0;
				bool noneAvailable = mode == 4;
				ushort[] masks1 = new ushort[5];
				ushort[] masks2 = new ushort[5];

				for (int i = 0; i < 5; i++)
				{
					if (noneAvailable)
					{
						masks1[i] = 0xffff;
						masks2[i] = 0;
					}
					else
					{
						masks1[i] = (ushort)chance.Next(0, 0x10000);
						masks2[i] = (ushort)(chance.Next(0, 0x10000) | (i == 0 ? 3 : 0));
						masks1[i] &= (ushort)~(i == 0 ? 3 : 0);
					}
				}

				return new Scenario($"ChooseTechnologyToTake({p1}, {p2}), mode {mode}, king {kingDialog}",
					game =>
					{
						if (p1 > 0 && p2 > 0)
						{
							for (int i = 0; i < 5; i++)
							{
								game.World.Participants[p1].FoundScienceMarkers[i] = masks1[i];
								game.World.Participants[p2].FoundScienceMarkers[i] = masks2[i];
							}
						}

						game.Processor.EmitGlobalWord(0x3934, (ushort)(kingDialog ? 1 : 0xffff));
					},
					game => new CityCaptureReference(game).ChooseTechnologyToTake(p1, p2),
					game => game.Conquest.ChooseTechnologyToTake(p1, p2),
					Register.Kept,
					game =>
					{
						if (p1 == 0 || p2 == 0) return "barbarian";
						if (noneAvailable) return "none";
						if (p1 == person) return kingDialog ? "human-king" : "human-dialog";
						if (p2 == person) return kingDialog ? "from-human-king" : "from-human-dialog";
						return "ai-ai";
					});
			}, output, ["barbarian", "none", "human-king", "human-dialog", "from-human-king", "from-human-dialog", "ai-ai"],
				[0x5000, 0x0d, 0x0d, 0x1b, 0x0d]);
		}

		[Fact]
		public void OperateAndDisplayMenuMatchOriginalCode()
		{
			Check(3, 45, (chance, pair) =>
			{
				int horizLoc = chance.Next(48, 120);
				int vertLoc = chance.Next(4) == 0 ? 139 : chance.Next(3) == 0 ? 41 : 40;
				ushort marker = (ushort)(chance.Next(3) == 0 ? 1 : 0);
				ushort defaultChoice = (ushort)(chance.Next(3) == 0 ? 0xffff : chance.Next(0, 3));
				ushort advisor = (ushort)(chance.Next(3) == 0 ? 0xffff : chance.Next(0, 6));
				ushort helpHint = (ushort)(chance.Next(3) == 0 ? 1 : 0);
				ushort moreBox = (ushort)(chance.Next(4) == 0 ? 1 : 0);
				ushort disabledMask = (ushort)(chance.Next(3) == 0 ? 2 : 0);
				ushort checkedMask = (ushort)(chance.Next(3) == 0 ? 5 : 0);
				ushort noShadow = (ushort)(chance.Next(4) == 0 ? 1 : 0);
				ushort motifBox = (ushort)(chance.Next(2) == 0 ? 1 : 0);
				bool indentTokens = chance.Next(4) == 0;
				bool noTrailingNewline = chance.Next(4) == 0;

				string menuText = noTrailingNewline
					? "Header line\n First option\n_Second option\n Third option"
					: "Header line\n First option\n_Second option\n Third option\n";

				int keyKind = chance.Next(7);
				ushort[] keys = keyKind switch
				{
					0 => [0x5000, 0x0d], // Down, Enter
					1 => [0x5000, 0x4800, 0x20], // Down, Up, Space
					2 => [0x1b], // Escape
					3 => [0x2300], // Alt+H help
					4 => [0x2f00, 0x0d], // Alt+V sound toggle, Enter
					5 => [(ushort)'t', 0x0d], // Hotkey 'T' for Third option, Enter
					_ => [0x09, 0x0d] // Tab (closes when ChoicesIndentTokens, else searches hotkey)
				};

				return new Scenario($"DisplayMenu({horizLoc}, {vertLoc}, {marker}), advisor {advisor}, keyKind {keyKind}",
					game =>
					{
						game.Libc.CopyText(0xba06, menuText);
						game.Processor.EmitGlobalWord(0x2f98, motifBox);
						game.Processor.EmitGlobalWord(0x2f9a, defaultChoice);
						game.Processor.EmitGlobalWord(0x2f9e, advisor);
						game.Processor.EmitGlobalWord(0x2fa0, helpHint);
						game.Processor.EmitGlobalWord(0x8066, moreBox);
						game.Processor.EmitGlobalWord(0xb276, disabledMask);
						game.Processor.EmitGlobalWord(0xd7f2, checkedMask);
						game.Processor.EmitGlobalWord(0xd762, noShadow);
						game.Dialogs.ChoicesIndentTokens = indentTokens;
						game.Dialogs.ChoicesIndentTokenDown = false;

						lock (game.VideoCard.VideoLatch)
						{
							game.VideoCard.Tokens.Clear();

							if (marker != 0)
								game.VideoCard.Tokens.Enqueue(0x0d);

							foreach (ushort k in keys)
								game.VideoCard.Tokens.Enqueue(k);
						}
					},
					game => new DialogPainterReference(game).DisplayMenu(0xba06, horizLoc, vertLoc, marker),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Dialogs.DisplayMenu(0xba06, horizLoc, vertLoc, marker)),
					Register.Kept | Register.Acc,
					game =>
					{
						if (advisor != 0xffff && advisor <= 2) return "advisor-pic";
						if (advisor != 0xffff) return "advisor-img";
						if ((vertLoc & 1) != 0) return "odd-vert";
						return $"choice-{game.Processor.RegAcc.Value16}";
					});
			}, output, ["advisor-pic", "advisor-img", "odd-vert", "choice-65535"]);
		}
	}
}
