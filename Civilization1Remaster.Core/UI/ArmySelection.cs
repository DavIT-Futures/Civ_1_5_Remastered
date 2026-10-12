using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 10.
	/// </summary>
	public class ArmySelection
	{
		private readonly GameEngine owner;
		private readonly CPU processor;
		private readonly UnitMemory troops;

		public ArmySelection(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>
		/// Asks which civilization's army to show (the names at 0x1982 + 2 * player, players 1 - 7) and shows its list;
		/// the screen is kept in the back screen 0x19d4 and put back
		/// </summary>
		public void DisplayWhichArmy()
		{
			this.processor.Journal.OpenScope("DisplayWhichArmy()");

			this.owner.Libc.CopyText(0xba06, "Which army?\n ");

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)));

				this.owner.Libc.AppendText(0xba06, "\n ");
			}

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			short chosenIdx = (short)(this.processor.RegAcc.Value16 + 1);

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4), 0, 0);

			DisplayArmyList(chosenIdx);

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa), 0, 0);

			this.processor.Journal.CloseScope("DisplayWhichArmy");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void DisplayArmyList(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayArmyList({participantIdx})");

			// function body
			this.processor.EnterFrame(0x14);
			this.processor.PreserveSource();

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x8, 0x0);
			goto L01c1;

		L00b3:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xe),
				4, 4, 2);

		L00b6:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0x8));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L00ff;

			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0x8));

			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946));
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xe),
				4, 4,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946)));

			goto L0161;

		L00ff:
			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0x8));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0161;

			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0x8));

			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1946));
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xa) + 1,
				this.processor.PullLocalSignedWord(0xe) + 1,
				3, 3, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xe),
				3, 3,
				this.processor.PullLocalWord(0x12));

		L0161:
			this.processor.EmitLocalWord(0x4, this.processor.IncrementWord(this.processor.PullLocalWord(0x4)));

		L0164:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x50);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L01be;

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x4),
				this.processor.PullLocalWord(0x8)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x10));
			if (this.processor.FlagSet.IsNotEqual) goto L018b;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0161;

		L018b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x4),
				this.processor.PullLocalSignedWord(0x8));

			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsEqual) goto L01b8;
			goto L00b3;

		L01b8:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xe),
				4, 4, 1);

			goto L00b6;

		L01be:
			this.processor.EmitLocalWord(0x8, this.processor.IncrementWord(this.processor.PullLocalWord(0x8)));

		L01c1:
			this.processor.CompareWord(this.processor.PullLocalWord(0x8), 0x32);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L01ce;
			this.processor.EmitLocalWord(0x4, 0x0);
			goto L0164;

		L01ce:
			this.owner.Libc.ReadKey();

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x2);
			this.processor.EmitLocalWord(0x6, 0x0);
			goto L0224;

		L01e3:
			this.owner.Libc.CopyText(0xba06, "S");

		L01f3:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.PullLocalWord(0x6));

			this.owner.TextPainter.RenderTextWithShadow(0xba06,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xe598)) << 2,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0xe798)) << 2,
				13);

		L0221:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));

		L0224:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x20);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0264;
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0xe2c2));
			this.processor.EmitLocalByte(0x14, this.processor.RegAcc.LowByte);
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L0221;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L01e3;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0255;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L025a;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsEqual) goto L025f;
			goto L01f3;

		L0255:
			this.owner.Libc.CopyText(0xba06, "A");
			goto L01f3;

		L025a:
			this.owner.Libc.CopyText(0xba06, "D");
			goto L01f3;

		L025f:
			this.owner.Libc.CopyText(0xba06, "N");
			goto L01f3;

		L0264:
			this.processor.EmitLocalWord(0x6, 0x0);
			goto L02ac;

		L026b:
			this.processor.RegAcc.Value16 = 0x417f;

		L026e:
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

		L027b:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.PullLocalWord(0x6));

			this.owner.TextPainter.RenderTextWithShadow(0xba06,
				this.owner.World.Participants[participantIdx].TacticalSites[this.processor.PullLocalWord(0x6)].Place.X << 2,
				this.owner.World.Participants[participantIdx].TacticalSites[this.processor.PullLocalWord(0x6)].Place.Y << 2,
				14);

		L02a9:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));

		L02ac:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x10);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L02e2;
			
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x6);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.Participants[participantIdx].TacticalSites[this.processor.PullLocalWord(0x6)].Live;
			this.processor.EmitLocalByte(0x14, this.processor.RegAcc.LowByte);

			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L02a9;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L026b;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L02d8;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L02dd;
			goto L027b;

		L02d8:
			this.processor.RegAcc.Value16 = 0x4181;
			goto L026e;

		L02dd:
			this.processor.RegAcc.Value16 = 0x4183;
			goto L026e;

		L02e2:
			this.owner.Libc.ReadKey();

			this.processor.EmitLocalWord(0x8, 0x1);
			this.processor.EmitLocalWord(0x6, 0x1);

		L02f1:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x6)].Assault == 0)
				goto L0453;

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x6), 10));

			this.owner.Libc.AppendText(0xba06, "/");

			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x6)].Approach == 0)
			{
				this.owner.Libc.AppendText(0xba06, "S");
			}
		
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x6)].Approach == 2)
			{
				this.owner.Libc.AppendText(0xba06, "D");
			}
		
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x6)].Approach == 1)
			{
				this.owner.Libc.AppendText(0xba06, "A");
			}
		
			if (this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x6)].Approach == 5)
			{
				this.owner.Libc.AppendText(0xba06, "T");
			}

			this.owner.Libc.AppendText(0xba06, " ");

			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegCount.LowByte = 0x5;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x6)].Guard, 10));

			this.owner.Libc.AppendText(0xba06, "/");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].Landmasses[this.processor.PullLocalWord(0x6)].Assault, 10));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 2, this.processor.PullLocalSignedWord(0x8), 7);

			this.processor.EmitLocalWord(0x8, this.processor.SumWord(this.processor.PullLocalWord(0x8), 0x6));

		L0453:
			this.processor.EmitLocalWord(0x6, this.processor.IncrementWord(this.processor.PullLocalWord(0x6)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0xf);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L045f;
			goto L02f1;

		L045f:
			this.owner.Libc.ReadKey();

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayArmyList");
		}

		/// <summary>
		/// A window about the unit: civilization, number, unit type, home city, position, the target of its "go to" order, then
		/// the types of the other units of its stack (following the ring, while the text is shorter than 512)
		/// </summary>
		public void DisplayUnitInfo(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"DisplayUnitInfo({participantIdx}, {troopIdx})");

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)));
			this.owner.Libc.AppendText(0xba06, "\n");
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(troopIdx, 10));
			this.owner.Libc.AppendText(0xba06, KindLabel(participantIdx, troopIdx));
			this.owner.Libc.AppendText(0xba06, "\n");

			this.owner.Conquest.ObtainCityLabel(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown));

			this.owner.Libc.AppendText(0xba06, "\n");
			this.owner.Libc.AppendText(0xba06, "Loc:");
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz), 10));
			this.owner.Libc.AppendText(0xba06, ",");
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert), 10));
			this.owner.Libc.AppendText(0xba06, "\n");

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
			{
				this.owner.Libc.AppendText(0xba06, "To:");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz), 10));
				this.owner.Libc.AppendText(0xba06, ",");
				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelVert), 10));
				this.owner.Libc.AppendText(0xba06, "\n");
			}

			this.owner.Libc.AppendText(0xba06, "*");

			for (short current = this.troops.Following(participantIdx, troopIdx);
				current != -1 && current != troopIdx && (short)this.owner.Libc.TextLength(0xba06) < 0x200;
				current = this.troops.Following(participantIdx, current))
			{
				this.owner.Libc.AppendText(0xba06, KindLabel(participantIdx, current));
				this.owner.Libc.AppendText(0xba06, "\n*");
			}

			this.owner.TurnHooks.DisplayDialog(0xba06, 10, 10);

			this.processor.Journal.CloseScope("DisplayUnitInfo");
		}

		/// <summary>The name of the unit's type: the start of its record in the unit type table (0x112a + 0x22 * type)</summary>
		private ushort KindLabel(short participantIdx, short troopIdx)
		{
			return (ushort)(UnitKindTable.Origin + (UnitKindTable.Stride * this.troops.TroopKind(participantIdx, troopIdx)));
		}
	}
}
