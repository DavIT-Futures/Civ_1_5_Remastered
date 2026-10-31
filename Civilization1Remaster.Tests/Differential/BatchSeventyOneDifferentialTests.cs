using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchSeventyOneDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void DisplayPalaceMatchesOriginalCode()
	{
		int[] keys = [0x0d, 0x31];
		int step = 0;

		Check(1, 4, (chance, pair) =>
		{
			int mode = (step++) % 2;

			return new Scenario(
				$"DisplayPalace(mode={mode})",
				game =>
				{
					ushort dataSeg = game.Processor.SegData.Value16;
					game.World.PersonParticipantIdx = 1;
					game.World.Participants[1].ChiefVisuals = 0;
					game.VideoCard.ReserveScreen(3);

					game.Processor.EmitGlobalWord(0xd762, (ushort)(mode & 1));
					game.Processor.EmitGlobalWord(0xaa, 0);
					game.Processor.EmitGlobalWord(0x19d4, 0);

					if (mode == 0)
					{
						// All maxed out: no options available
						for (ushort sec = 0; sec < 9; sec++)
						{
							game.Processor.EmitUInt16(dataSeg, (ushort)((sec << 1) + 0xd91e), 4);
							game.Processor.EmitUInt16(dataSeg, (ushort)((sec << 1) + 0xb222), 0);
						}

						for (ushort garden = 0; garden < 3; garden++)
						{
							game.Processor.EmitUInt16(dataSeg, (ushort)((garden << 1) + 0xd930), 3);
						}
					}
					else
					{
						// Section 1 upgradable
						for (ushort sec = 0; sec < 9; sec++)
						{
							game.Processor.EmitUInt16(dataSeg, (ushort)((sec << 1) + 0xd91e), 0xffff);
							game.Processor.EmitUInt16(dataSeg, (ushort)((sec << 1) + 0xb222), 0);
						}

						game.Processor.EmitUInt16(dataSeg, (ushort)((4 << 1) + 0xd91e), 1);
						game.Processor.EmitUInt16(dataSeg, (ushort)((3 << 1) + 0xd91e), 1);
						game.Processor.EmitUInt16(dataSeg, (ushort)((2 << 1) + 0xd91e), 1);
						game.Processor.EmitUInt16(dataSeg, (ushort)((1 << 1) + 0xd91e), 0);

						for (ushort garden = 0; garden < 3; garden++)
						{
							game.Processor.EmitUInt16(dataSeg, (ushort)((garden << 1) + 0xd930), 0);
						}
					}
				},
				game => new PalaceBuilderReference(game).DisplayPalace(),
				game => game.Throne.DisplayPalace(),
				Register.Kept,
				_ => mode == 0 ? "maxed" : "upgrade-section");
		}, output, ["maxed", "upgrade-section"], keys);
	}

	[Fact]
	public void DisplayCivilopediaRecordMatchesOriginalCode()
	{
		int[] keys = [0x20, 0x20, 0x0d, 0x0d];
		int step = 0;

		Check(1, 10, (chance, pair) =>
		{
			ushort category = (ushort)((step++) % 5);
			ushort index = category switch
			{
				0 => (ushort)(step % 20),
				1 => (ushort)(1 + (step % 20)),
				2 => (ushort)(step % 15),
				3 => (ushort)(step % 10),
				_ => (ushort)(step % 5)
			};

			return new Scenario(
				$"DisplayCivilopediaRecord(idx={index}, cat={category})",
				game =>
				{
					game.World.PersonParticipantIdx = 1;
					game.World.SessionPrefMarkers = 0;
					game.Processor.EmitGlobalWord(0xe17c, 3);
					game.Processor.EmitGlobalWord(0xd762, (ushort)(step & 1));
					game.Processor.EmitGlobalWord(0x3934, 0xffff);
					game.VideoCard.ReserveScreen(3);
				},
				game => new EncyclopediaReference(game).DisplayCivilopediaRecord(index, category),
				game => game.Encyclopedia.DisplayCivilopediaRecord(index, category),
				Register.Kept,
				_ => category switch
				{
					0 => "tech",
					1 => "improvement",
					2 => "unit",
					3 => "terrain",
					_ => "concept"
				});
		}, output, ["tech", "improvement", "unit", "terrain", "concept"], keys);
	}
}
