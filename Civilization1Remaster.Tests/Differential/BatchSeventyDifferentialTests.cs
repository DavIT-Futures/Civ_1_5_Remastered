using System;
using System.Drawing;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchSeventyDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void RenderMapTileMatchesOriginalCode()
	{
		int step = 0;

		Check(1, 14, (chance, pair) =>
		{
			int mode = (step++) % 7;
			int tileX = mode == 1 ? 2 : 10;
			int tileY = mode == 1 ? 2 : 10;

			return new Scenario(
				$"RenderMapTile(mode={mode}, x={tileX}, y={tileY})",
				game =>
				{
					ushort dataSeg = game.Processor.SegData.Value16;
					game.World.PersonParticipantIdx = 1;
					game.Processor.EmitGlobalWord(0xd4cc, 8);
					game.Processor.EmitGlobalWord(0xd75e, 8);
					game.Processor.EmitGlobalWord(0xdcfc, 0);
					game.Processor.EmitGlobalWord(0xd806, (ushort)(mode == 4 ? 1 : 0));
					game.Processor.EmitGlobalWord(0xd762, (ushort)(mode & 1));
					game.Processor.EmitGlobalWord(0xb278, (ushort)(mode == 0 ? 1 : 0));
					game.VideoCard.ReserveScreen(3);

					for (int x = 6; x <= 18; x++)
					{
						for (int y = 6; y <= 18; y++)
						{
							game.VideoCard.AssignDot(2, x, y, 2, 0);
							game.VideoCard.AssignDot(2, x, y + 100, 0, 0);
							game.VideoCard.AssignDot(2, x + 80, y + 100, 0, 0);
							game.VideoCard.AssignDot(2, x + 160, y, 0, 0);
							game.World.ChartSight[x, y] = 0x2;
						}
					}

					if (mode == 0)
					{
						game.VideoCard.AssignDot(2, 10, 10, 1, 0);
						game.VideoCard.AssignDot(2, 11, 10, 2, 0);
						game.VideoCard.AssignDot(2, 11, 10 + 100, 0x1, 0);
						game.VideoCard.AssignDot(2, 11 + 160, 10, 1, 0);
						game.VideoCard.AssignDot(2, 12, 10, 4, 0);
					}
					else if (mode == 2)
					{
						game.VideoCard.AssignDot(2, 10, 10, 1, 0);
						game.VideoCard.AssignDot(2, 10, 9, 0xb, 0);
						game.VideoCard.AssignDot(2, 11, 10, 1, 0);
						game.VideoCard.AssignDot(2, 10, 11, 2, 0);
						game.VideoCard.AssignDot(2, 9, 10, 0xb, 0);
						game.Processor.EmitGlobalWord(0xd762, 1);
					}
					else if (mode == 3)
					{
						game.VideoCard.AssignDot(2, 10, 10, 0xb, 0);
						game.VideoCard.AssignDot(2, 10, 9, 0xb, 0);
						game.VideoCard.AssignDot(2, 11, 10, 1, 0);
					}
					else if (mode == 4)
					{
						game.VideoCard.AssignDot(2, 10, 10, 2, 0);
						game.VideoCard.AssignDot(2, 10, 10 + 100, 0x40, 0);
						game.VideoCard.AssignDot(2, 10 + 80, 10 + 100, 0x40, 0);
						game.VideoCard.AssignDot(2, 11, 10, 4, 0);
						game.VideoCard.AssignDot(2, 11, 9, 4, 0);
					}
					else if (mode == 5)
					{
						game.VideoCard.AssignDot(2, 10, 10, 4, 0);
						game.VideoCard.AssignDot(2, 10, 10 + 100, 0x2e, 0);
						game.VideoCard.AssignDot(2, 10 + 80, 10 + 100, 0x2e, 0);
						game.VideoCard.AssignDot(2, 11, 10 + 100, 0x08, 0);
						game.VideoCard.AssignDot(2, 11 + 80, 10 + 100, 0x08, 0);
						game.World.ChartSight[10, 9] = 0;

						game.VideoCard.AssignDot(2, 12, 10, 2, 0);
						game.VideoCard.AssignDot(2, 12, 10 + 100, 0x1a, 0);
						game.VideoCard.AssignDot(2, 12 + 80, 10 + 100, 0x1a, 0);
					}
					else if (mode == 6)
					{
						game.VideoCard.AssignDot(2, 10, 10, 2, 0);
						game.VideoCard.AssignDot(2, 10, 10 + 100, 0x19, 0);
						game.VideoCard.AssignDot(2, 10 + 80, 10 + 100, 0x19, 0);
						game.VideoCard.AssignDot(2, 10 + 160, 10, 1 | MapLayers.UnitsPresent, 0);

						game.World.Towns[0].ConditionMarker = 0x01;
						game.World.Towns[0].StructureMarkers0 = 0x80;
						game.World.Towns[0].Place = new Point(10, 10);
						game.World.Towns[0].ShownExtent = 12;
						game.World.Towns[0].TrueExtent = 12;
						game.World.Towns[0].ParticipantIdx = 1;
						game.World.Towns[0].LabelIdx = 0;
						game.Processor.EmitGlobalWord(0x6c96, 0);

						ushort unitAddr = (ushort)(1 * 0x600 + 0 * 0xc);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unitAddr + 0x81d4), 0);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unitAddr + 0x81d5), 10);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unitAddr + 0x81d6), 10);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unitAddr + 0x81d7), 2);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unitAddr + 0x81de), 0xff);
					}
				},
				game =>
				{
					var reference = new MapManagementReference(game);
					if (mode == 0)
					{
						reference.RenderMapTile(10, 10);
						reference.RenderMapTile(11, 10);
						reference.RenderMapTile(12, 10);
					}
					else if (mode == 2)
					{
						reference.RenderMapTile(10, 10);
						game.Processor.EmitGlobalWord(0xd762, 0);
						reference.RenderMapTile(10, 10);
					}
					else if (mode == 4)
					{
						reference.RenderMapTile(10, 10);
						game.Processor.EmitGlobalWord(0xd762, 0);
						reference.RenderMapTile(11, 10);
					}
					else if (mode == 5)
					{
						reference.RenderMapTile(10, 10);
						reference.RenderMapTile(12, 10);
					}
					else if (mode == 6)
					{
						reference.RenderMapTile(10, 10);
						game.World.Towns[0].ConditionMarker = 0;
						reference.RenderMapTile(10, 10);
					}
					else
					{
						reference.RenderMapTile(tileX, tileY);
					}
				},
				game =>
				{
					if (mode == 0)
					{
						game.MapOps.RenderMapTile(10, 10);
						game.MapOps.RenderMapTile(11, 10);
						game.MapOps.RenderMapTile(12, 10);
					}
					else if (mode == 2)
					{
						game.MapOps.RenderMapTile(10, 10);
						game.Processor.EmitGlobalWord(0xd762, 0);
						game.MapOps.RenderMapTile(10, 10);
					}
					else if (mode == 4)
					{
						game.MapOps.RenderMapTile(10, 10);
						game.Processor.EmitGlobalWord(0xd762, 0);
						game.MapOps.RenderMapTile(11, 10);
					}
					else if (mode == 5)
					{
						game.MapOps.RenderMapTile(10, 10);
						game.MapOps.RenderMapTile(12, 10);
					}
					else if (mode == 6)
					{
						game.MapOps.RenderMapTile(10, 10);
						game.World.Towns[0].ConditionMarker = 0;
						game.MapOps.RenderMapTile(10, 10);
					}
					else
					{
						game.MapOps.RenderMapTile(tileX, tileY);
					}
				},
				Register.Kept | Register.Acc,
				game => $"mode{mode}");
		}, output, ["mode0", "mode1", "mode2", "mode3", "mode4", "mode5", "mode6"]);
	}

	[Fact]
	public void ManageBarbarianUnitMatchesOriginalCode()
	{
		int step = 0;

		Check(2, 16, (chance, pair) =>
		{
			int mode = (step++) % 8;
			ushort rngSeed = (ushort)chance.Next(1, 0x7fff);

			return new Scenario(
				$"ManageBarbarianUnit(mode={mode})",
				game =>
				{
					ushort dataSeg = game.Processor.SegData.Value16;
					game.Processor.EmitGlobalWord(0x5386, rngSeed);
					game.World.PersonParticipantIdx = 1;
					game.World.ParticipantMarkers = 0x07;
					game.World.RoundTally = 4;

					for (int i = 0; i < 128; i++)
					{
						game.World.Towns[i].ConditionMarker = 0xff;
					}

					for (int x = 8; x <= 22; x++)
					{
						for (int y = 8; y <= 22; y++)
						{
							game.VideoCard.AssignDot(2, x, y, 2, 0);
							game.VideoCard.AssignDot(2, x, y + 100, 0, 0);
							game.VideoCard.AssignDot(2, x + 80, y, 1, 0);
							game.VideoCard.AssignDot(2, x + 160, y, 0, 0);
							game.World.ChartSight[x, y] = 0x2;
						}
					}

					game.World.Towns[0].ConditionMarker = 0;
					game.World.Towns[0].Place = new Point(14, 12);
					game.World.Towns[0].TrueExtent = 4;
					game.World.Towns[0].ParticipantIdx = 1;
					game.World.Towns[0].LabelIdx = 0;
					game.VideoCard.AssignDot(2, 14, 12 + 100, 0x1, 0);
					game.VideoCard.AssignDot(2, 14 + 160, 12, 1, 0);

					game.World.Participants[1].TownTally = 2;
					game.World.Participants[1].Gold = 150;

					ushort unit0Addr = (ushort)(0 * 0x600 + 0 * 0xc);
					ushort unit1Addr = (ushort)(0 * 0x600 + 1 * 0xc);
					for (int u = 0; u < 4; u++)
					{
						game.Processor.EmitUInt8(dataSeg, (ushort)(u * 0xc + 0x81d7), 0xff);
					}

					if (mode == 0)
					{
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d4), 0);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d5), 10);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d6), 1);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d7), 2);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81de), 0xff);
					}
					else if (mode == 1)
					{
						game.VideoCard.AssignDot(2, 12, 12, 1, 0);
						game.VideoCard.AssignDot(2, 12 + 160, 12, MapLayers.UnitsPresent, 0);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d4), 0);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d5), 12);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d6), 12);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d7), 0x10);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d8), 0);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81da), 0xff);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81dd), 0);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81de), 1);

						game.Processor.EmitUInt8(dataSeg, (ushort)(unit1Addr + 0x81d5), 12);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit1Addr + 0x81d6), 12);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit1Addr + 0x81d7), 2);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit1Addr + 0x81de), 0);
					}
					else if (mode == 2)
					{
						for (int x = 8; x <= 14; x++)
						{
							for (int y = 8; y <= 14; y++)
							{
								game.VideoCard.AssignDot(2, x, y, 1, 0);
							}
						}

						game.World.Towns[0].Place = new Point(20, 20);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d5), 10);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d6), 10);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d7), 0x10);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81da), 0xff);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81de), 1);
					}
					else if (mode == 3)
					{
						game.World.Towns[0].ParticipantIdx = 0;
						game.VideoCard.AssignDot(2, 14 + 160, 12, 0, 0);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d5), 14);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d6), 12);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d7), 2);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81de), 0xff);
					}
					else if (mode == 4)
					{
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d5), 12);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d6), 12);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d7), 0x1a);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81de), 0xff);

						game.Processor.EmitUInt8(dataSeg, (ushort)(unit1Addr + 0x81d5), 13);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit1Addr + 0x81d6), 12);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit1Addr + 0x81d7), 2);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit1Addr + 0x81de), 0xff);
					}
					else if (mode == 5)
					{
						game.World.Towns[0].Place = new Point(18, 12);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d5), 12);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d6), 12);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d7), 0x1a);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81de), 0xff);
					}
					else if (mode == 6)
					{
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d5), 13);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d6), 12);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d7), 6);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81dc), 3);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81de), 0xff);
					}
					else
					{
						game.World.Towns[0].Place = new Point(16, 12);
						game.VideoCard.AssignDot(2, 14, 12 + 100, 0x2, 0);
						game.VideoCard.AssignDot(2, 13 + 160, 12, 1 | MapLayers.UnitsPresent, 0);

						ushort enemyUnitAddr = (ushort)(1 * 0x600 + 0 * 0xc);
						game.Processor.EmitUInt8(dataSeg, (ushort)(enemyUnitAddr + 0x81d5), 13);
						game.Processor.EmitUInt8(dataSeg, (ushort)(enemyUnitAddr + 0x81d6), 12);
						game.Processor.EmitUInt8(dataSeg, (ushort)(enemyUnitAddr + 0x81d7), 2);
						game.Processor.EmitUInt8(dataSeg, (ushort)(enemyUnitAddr + 0x81de), 0xff);

						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d5), 12);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d6), 12);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81d7), 6);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81dc), 3);
						game.Processor.EmitUInt8(dataSeg, (ushort)(unit0Addr + 0x81de), 0xff);
					}
				},
				game => new AIPlayerReference(game).ManageBarbarianUnit(0, 0),
				game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Bot.ManageBarbarianUnit(0, 0)),
				Register.Kept | Register.Acc,
				game => $"mode{mode}");
		}, output, ["mode0", "mode1", "mode2", "mode3", "mode4", "mode5", "mode6", "mode7"],
		[0x0d, 0x0d]);
	}
}
