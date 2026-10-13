using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.UI
{
	public class GameReplay
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public GameReplay(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayReplay()
		{
			this.processor.Journal.OpenScope("DisplayReplay()");

			// function body
			this.processor.EnterFrame(0x24);
			this.processor.PreserveSource();

		L0007:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x680e), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0027;

			this.owner.TurnHooks.DisplayDialog(0x3fb0, 100, 80);

			this.processor.EmitGlobalWord(0x680a, this.processor.RegAcc.Value16);
			goto L002d;

		L0027:
			this.processor.EmitGlobalWord(0x680a, 0x4);

		L002d:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x680a), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L0037;
			goto L089e;

		L0037:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x680a), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L0085;

			this.owner.Libc.OpenHandle("replay.txt", 0x8301, 0x80);

			this.processor.EmitGlobalWord(0x680c, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0081;

			this.owner.Libc.CopyText(0xba06, "File Error\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			goto L089e;

		L0081:
			DumpReplayCities();

		L0085:
			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.processor.EmitLocalWord(0xe, 0x0);

		L0094:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.SegExtra.Value16 = 0x3725; // segment
			this.processor.EmitUInt16(this.processor.SegExtra.Value16, (ushort)(this.processor.RegBase.Value16 + 0x0), 0xffff);
			this.processor.IncrementLocalWord(0xe);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x80);
			if (this.processor.FlagSet.IsLess) goto L0094;
			this.processor.EmitLocalWord(0xe, 0x1);

		L00b3:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullLocalWord(0xe)].ChiefVisuals;
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x20), this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0xe);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x8);
			if (this.processor.FlagSet.IsLess) goto L00b3;
			this.processor.EmitLocalWord(0xe, 0x1);
			goto L00e8;

		L00cf:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);

		L00d2:
			this.owner.World.Participants[this.processor.PullLocalWord(0xe)].ChiefVisuals = (short)this.processor.RegAcc.Value16;

			ObtainReplayPlayerLabel(this.processor.PullLocalSignedWord(0xe));

		L00e5:
			this.processor.EmitLocalWord(0xe, this.processor.IncrementWord(this.processor.PullLocalWord(0xe)));

		L00e8:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L010c;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L00e5;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xe);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantIdentMarkers);
			if (this.processor.FlagSet.IsEqual) goto L00cf;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
			goto L00d2;

		L010c:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x680a), 0x4);
			if (this.processor.FlagSet.IsEqual) goto L0191;

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.processor.EmitLocalWord(0xc, 0x1);
			goto L0184;

		L0135:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xa) * 4,
				(this.processor.PullLocalSignedWord(0xc) * 4) + 4,
				4, 4, 8);

		L0138:
			this.processor.EmitLocalWord(0xa, this.processor.IncrementWord(this.processor.PullLocalWord(0xa)));

		L0160:
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x50);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0181;

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0xa),
				this.processor.PullLocalSignedWord(0xc));

			this.processor.EmitLocalWord(0x22, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L0135;

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xa) * 4,
				(this.processor.PullLocalSignedWord(0xc) * 4) + 4,
				4, 4, 1);

			goto L0138;

		L0181:
			this.processor.EmitLocalWord(0xc, this.processor.IncrementWord(this.processor.PullLocalWord(0xc)));

		L0184:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x31);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0191;
			this.processor.EmitLocalWord(0xa, 0x0);
			goto L0160;

		L0191:
			this.processor.EmitLocalWord(0x2, 0x0);

		L0196:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.RerunContentSpan;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L01a1;
			goto L083d;

		L01a1:
			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0xf);
			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegCount.HighByte = this.processor.PullLocalByte(0x24);
			this.processor.RegCount.LowByte = 0;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			ObtainReplayYear(this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x6810, 0xffff);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(Math.Abs((short)this.processor.PullLocalWord(0x4)), 10));

			if (this.processor.PullLocalSignedWord(0x4) < 0)
			{
				this.owner.Libc.AppendText(0xba06, " BC");
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, " AD");
			}

			this.owner.Libc.AppendText(0xba06, ": ");

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xc);
			if (this.processor.FlagSet.IsBelowOrEqual) goto L0255;
			goto L0826;

		L0255:
			switch(this.processor.RegAcc.Value16)
			{
				case 0:
					goto L025d;
				case 1:
					goto L03ae;
				case 2:
					goto L0414;
				case 3:
					goto L0826;
				case 4:
					goto L0453;
				case 5:
					goto L04a8;
				case 6:
					goto L0826;
				case 7:
					goto L04fb;
				case 8:
					goto L025d;
				case 9:
					goto L054f;
				case 10:
					goto L05b9;
				case 11:
					goto L065c;
				case 12:
					goto L0779;
			}

		L025d:
			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitGlobalWord(0x6810, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.EmitLocalWord(0x8, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.CompareWord(this.processor.PullGlobalWord(0x6810), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L02a6;
			goto L0336;

		L02a6:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x6810);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L02e2;

			this.owner.Libc.AppendText(0xba06, " found the city of ");

			AssignReplayCityContent(
				this.processor.PullGlobalWord(0x6810),
				this.processor.PullLocalWord(0xa),
				this.processor.PullLocalWord(0xc));

			goto L0300;

		L02e2:
			this.owner.Libc.AppendText(0xba06, " capture ");

			AssignReplayCityContentAlt(
				this.processor.PullGlobalWord(0x6810),
				this.processor.PullLocalWord(0xa),
				this.processor.PullLocalWord(0xc));

		L0300:
			uint settlementTitleIdent = this.processor.PullLocalWord(0x8);
			ushort charsDisplacement = (ushort)(0xba06 + this.owner.Libc.TextLength(0xba06));

			for (int idx = 0; idx < 0xd; idx++)
			{
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(charsDisplacement + idx), (byte)this.owner.World.TownLabels[settlementTitleIdent][idx]);
			}

			RenderReplayPreamble();

			goto L0388;

		L0336:
			settlementTitleIdent = this.processor.PullLocalWord(0x8);
			charsDisplacement = (ushort)(0xba06 + this.owner.Libc.TextLength(0xba06));

			for (int idx = 0; idx < 0xd; idx++)
			{
				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(charsDisplacement + idx), (byte)this.owner.World.TownLabels[settlementTitleIdent][idx]);
			}

			this.owner.Libc.AppendText(0xba06, " destroyed");

			RenderReplayPreamble();

			AssignReplayCityContentAlt(
				0xffff,
				this.processor.PullLocalWord(0xa),
				this.processor.PullLocalWord(0xc));

		L0388:
			RenderReplayMapTile(
				this.processor.PullLocalWord(0xa),
				this.processor.PullLocalWord(0xc));

			this.processor.CompareWord(this.processor.PullGlobalWord(0x680a), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L039f;
			goto L0826;

		L039f:
			this.owner.TextPainter.AwaitTime(30);

			goto L0826;

		L03ae:
			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0xf);
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " declare war on ");

		L03eb:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

		L040d:
			RenderReplayPreamble();

			goto L0826;

		L0414:
			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegAcc.Value16 = this.processor.ShiftRightWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);

			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0xf);
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);

			this.processor.IncrementLocalWord(0x2);

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " make peace with ");

			goto L03eb;

		L0453:
			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitGlobalWord(0x6810, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x6810);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " discover ");

			this.processor.ScaleIndex(0x16, this.processor.PullLocalWord(0xc));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			goto L040d;

		L04a8:
			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitGlobalWord(0x6810, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x6810);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " produce first ");

			this.processor.ScaleIndex(0x22, this.processor.PullLocalWord(0xc));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			goto L040d;

		L04fb:
			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitGlobalWord(0x6810, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x6810);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " form ");

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xc);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1966)));
			goto L040d;

		L054f:
			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitGlobalWord(0x6810, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x6810);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " build ");

			this.processor.CompareWord(this.processor.PullLocalWord(0xc), 0x7);
			if (this.processor.FlagSet.IsGreater) goto L05ad;

			this.owner.Libc.AppendText(0xba06, "the ");

		L05ad:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0xc));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xe6a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			goto L040d;

		L05b9:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.EmitGlobalWord(0x6810, this.processor.RegAcc.Value16);

			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.EmitLocalWord(0xc, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, ": ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0xa), 10));

			this.owner.Libc.AppendText(0xba06, " cities; ");

			this.owner.Helpers.PopulationToText(
				this.processor.PullLocalSignedByte(0xc) + this.processor.PullLocalSignedWord(0x10));

			this.owner.Libc.AppendText(0xba06, " population");

			goto L040d;

		L065c:
			this.owner.Libc.CopyText(0xba06, "*** ");

			this.processor.EmitLocalWord(0xa, 0x0);

		L0671:
			this.processor.TestBitsByte(this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)], 0xf0);
			if (this.processor.FlagSet.IsEqual) goto L06e8;

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)(this.processor.PullLocalWord(0xa) << 1) + 1), 10));

			this.owner.Libc.AppendText(0xba06, ":");

			this.processor.RegBase.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.RegBase.HighByte = 0;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftRightWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " ");

		L06e8:
			this.processor.TestBitsByte(this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)], 0xf);
			if (this.processor.FlagSet.IsEqual) goto L075d;

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((short)(this.processor.PullLocalWord(0xa) << 1) + 2), 10));

			this.owner.Libc.AppendText(0xba06, ":");

			this.processor.RegBase.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16, 0xf);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " ");

		L075d:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));
			this.processor.IncrementLocalWord(0xa);
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), 0x4);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L076c;
			goto L0671;

		L076c:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x680a), 0x3);
			if (this.processor.FlagSet.IsEqual) goto L0776;
			goto L0826;

		L0776:
			goto L040d;

		L0779:
			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitGlobalWord(0x6810, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegAcc.LowByte = this.owner.World.RerunContent[this.processor.PullLocalWord(0x2)];
			this.processor.EmitLocalWord(0xa, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x2);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x6810);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " civilization destroyed by ");

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.processor.CompareWord(this.processor.PullGlobalWord(0x680a), 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L07f3;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0xa), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L07f3;

			DisplayCivilizationDestroyedScreen(this.processor.PullGlobalSignedWord(0x6810),
				this.processor.PullLocalWord(0x4));

		L07f3:
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6810)].ChiefVisuals ^= 8;

			ObtainReplayPlayerLabel(this.processor.PullGlobalSignedWord(0x6810));
			
			goto L040d;

		L0826:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x680a), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0830;
			goto L0196;

		L0830:
			this.owner.Libc.ReadKey();

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1b);
			if (this.processor.FlagSet.IsEqual) goto L083d;
			goto L0196;

		L083d:
			this.processor.EmitLocalWord(0xe, 0x1);

		L0842:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xe);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x20));
			this.owner.World.Participants[this.processor.PullLocalWord(0xe)].ChiefVisuals = (short)this.processor.RegAcc.Value16;

			this.processor.IncrementLocalWord(0xe);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x8);
			if (this.processor.FlagSet.IsLess) goto L0842;
			this.processor.EmitLocalWord(0xe, 0x1);

		L085c:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L086e;

			ObtainReplayPlayerLabel(this.processor.PullLocalSignedWord(0xe));

		L086e:
			this.processor.EmitLocalWord(0xe, this.processor.IncrementWord(this.processor.PullLocalWord(0xe)));
			this.processor.CompareWord(this.processor.PullLocalWord(0xe), 0x8);
			if (this.processor.FlagSet.IsLess) goto L085c;

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.CompareWord(this.processor.PullGlobalWord(0x680a), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L0894;
			
			this.owner.Libc.CloseHandle((short)this.processor.PullGlobalWord(0x680c));

		L0894:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x680a), 0x3);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L089e;
			goto L0007;

		L089e:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayReplay");
		}

		/// <summary>
		/// Shows the line of a replay event (the text at 0xba06) at the top of the screen, in capitals for the human's events
		/// (0x6810), and writes it with CR LF to the replay file (0x680c) in mode 3 of 0x680a; nothing in mode 4. The text is
		/// drawn, then drawn again shortened to 312 pixels.
		/// </summary>
		public void RenderReplayPreamble()
		{
			this.processor.Journal.OpenScope("RenderReplayPreamble()");

			if (this.processor.PullGlobalWord(0x680a) != 4)
			{
				if (this.processor.PullGlobalWord(0x6810) == (ushort)this.owner.World.PersonParticipantIdx)
				{
					this.owner.Libc.UppercaseText(0xba06);
				}

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 8, 15);

				this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, 1, 0);

				if (this.processor.PullGlobalWord(0x680a) == 3)
				{
					ushort length = this.owner.Libc.TextLength(0xba06);

					this.processor.EmitGlobalByte((ushort)(length + 0xba06), 0xd);
					this.processor.EmitGlobalByte((ushort)(length + 0xba07), 0xa);
					this.processor.EmitGlobalByte((ushort)(length + 0xba08), 0x0);

					this.owner.Libc.WriteHandle((short)this.processor.PullGlobalWord(0x680c), 0xba06, (ushort)(length + 2));
				}

				this.owner.BlockText.ShrinkTextToWidth(0xba06, 312);

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 8, 15);

				this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, 1, 0);
			}

			this.processor.Journal.CloseScope("RenderReplayPreamble");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <returns></returns>
		public ushort ObtainReplayYear(ushort arg1)
		{
			this.processor.Journal.OpenScope($"ObtainReplayYear({arg1})");

			// function body
			if (arg1 > 250)
			{
				arg1 -= 250;

				if (arg1 > 50)
				{
					arg1 -= 50;

					if (arg1 > 50)
					{
						arg1 -= 50;

						if (arg1 > 50)
						{
							arg1 -= 50;

							this.processor.RegAcc.Value16 = (ushort)(arg1 + 1850);
						}
						else
						{
							this.processor.RegAcc.Value16 = (ushort)((arg1 << 1) + 1750);
						}
					}
					else
					{
						this.processor.RegAcc.Value16 = (ushort)((arg1 * 5) + 1500);
					}
				}
				else
				{
					this.processor.RegAcc.Value16 = (ushort)((arg1 * 10) + 1000);
				}
			}
			else
			{
				this.processor.RegAcc.Value16 = (ushort)((arg1 * 20) - 4000);
			}

			// Far return
			this.processor.Journal.CloseScope("ObtainReplayYear");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Puts the city into the first free entry (first column 0xffff) of the replay city table (segment 0x3725: three word
		/// columns of 128 entries at 0, 0x100 and 0x200)
		/// </summary>
		public void AssignReplayCityContent(ushort arg1, ushort arg2, ushort arg3)
		{
			this.processor.Journal.OpenScope($"AssignReplayCityContent({arg1}, {arg2}, {arg3})");

			this.processor.SegExtra.Value16 = ReplayCitySegment;

			for (int idx = 0; idx < 0x80; idx++)
			{
				if (PullReplayCity(idx, 0) != 0xffff)
					continue;

				EmitReplayCity(idx, 0, arg1);
				EmitReplayCity(idx, 0x100, arg2);
				EmitReplayCity(idx, 0x200, arg3);
				break;
			}

			this.processor.Journal.CloseScope("AssignReplayCityContent");
		}

		private const ushort ReplayCitySegment = 0x3725;

		private ushort PullReplayCity(int idx, int column)
		{
			return this.processor.PullUInt16(ReplayCitySegment, (ushort)((idx << 1) + column));
		}

		private void EmitReplayCity(int idx, int column, ushort amount)
		{
			this.processor.EmitUInt16(ReplayCitySegment, (ushort)((idx << 1) + column), amount);
		}

		/// <summary>
		/// Sets the first column of the first entry of the replay city table whose second and third columns are arg2 and arg3
		/// </summary>
		public void AssignReplayCityContentAlt(ushort arg1, ushort arg2, ushort arg3)
		{
			this.processor.Journal.OpenScope($"AssignReplayCityContentAlt({arg1}, {arg2}, {arg3})");

			this.processor.SegExtra.Value16 = ReplayCitySegment;

			for (int idx = 0; idx < 0x80; idx++)
			{
				if (PullReplayCity(idx, 0x100) != arg2 || PullReplayCity(idx, 0x200) != arg3)
					continue;

				EmitReplayCity(idx, 0, arg1);
				break;
			}

			this.processor.Journal.CloseScope("AssignReplayCityContentAlt");
		}

		/// <summary>
		/// The entry of the replay city table nearest to the position; its distance goes to 0x6812 (999 when none is nearer).
		/// Like the original, without an entry nearer than 999 the result is what was left in the stack frame (local 4), so the
		/// frame stays, and its locals get the values the original leaves there.
		/// </summary>
		/// <returns>Entry, also in AX</returns>
		public ushort ObtainDistanceToReplayCity(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainDistanceToReplayCity({horizLoc}, {vertLoc})");

			this.processor.EnterFrame(0x6);
			this.processor.PreserveSource();

			ushort nearest = this.processor.PullLocalWord(0x4);
			ushort? lastDistance = null;

			this.processor.EmitGlobalWord(0x6812, 999);
			this.processor.SegExtra.Value16 = ReplayCitySegment;

			for (int idx = 0; idx < 0x80; idx++)
			{
				if (PullReplayCity(idx, 0) == 0xffff)
					continue;

				short distance = (short)this.owner.Helpers.ObtainDistance(horizLoc, (short)PullReplayCity(idx, 0x100),
					vertLoc, (short)PullReplayCity(idx, 0x200));
				lastDistance = (ushort)distance;

				if (distance < this.processor.PullGlobalSignedWord(0x6812))
				{
					this.processor.EmitGlobalWord(0x6812, (ushort)distance);
					nearest = (ushort)idx;
				}
			}

			if (lastDistance.HasValue)
			{
				this.processor.EmitLocalWord(0x2, lastDistance.Value);
			}

			this.processor.EmitLocalWord(0x4, nearest);
			this.processor.EmitLocalWord(0x6, 0x80);
			this.processor.RegAcc.Value16 = nearest;

			this.processor.RestoreSource();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("ObtainDistanceToReplayCity");

			return nearest;
		}

		/// <summary>
		/// Unless 0x680a is 4: colours the land tiles of the replay map around the position (5 tiles in every direction, columns
		/// 0 - 79, rows 2 - 48) by the nearest replay city: grey (8) when it is 6 or more tiles away or on another continent,
		/// else 7 or 15 (the city's player is 1 or not) on its own tile, else the colour of its player (0x1946). The stack frame
		/// stays: ObtainDistanceToReplayCity reads what is left below it, so it must be called at the same depth as in the original.
		/// </summary>
		public void RenderReplayMapTile(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"RenderReplayMapTile({horizLoc}, {vertLoc})");

			this.processor.EnterFrame(0xc);
			this.processor.PreserveIndexRegisters();

			if (this.processor.PullGlobalWord(0x680a) != 4)
			{
				int left = this.owner.Helpers.VerifyValueSpan(horizLoc - 5, 0, 80);
				int right = this.owner.Helpers.VerifyValueSpan(horizLoc + 5, 0, 80);
				int top = this.owner.Helpers.VerifyValueSpan(vertLoc - 5, 2, 49);
				int bottom = this.owner.Helpers.VerifyValueSpan(vertLoc + 5, 2, 49);

				for (int tileVert = top; tileVert < bottom; tileVert++)
				{
					for (int tileHoriz = left; tileHoriz < right; tileHoriz++)
					{
						if (this.owner.MapOps.ObtainTerrainType(tileHoriz, tileVert) == MapRules.OceanTerrain)
							continue;

						ushort cityIdx = ObtainDistanceToReplayCity(tileHoriz, tileVert);
						ushort hue = 8;

						if (this.processor.PullGlobalSignedWord(0x6812) < 6 &&
							this.owner.MapOps.ObtainContinentID((short)PullReplayCity(cityIdx, 0x100), (short)PullReplayCity(cityIdx, 0x200)) ==
							this.owner.MapOps.ObtainContinentID(tileHoriz, tileVert))
						{
							ushort cityOwner = PullReplayCity(cityIdx, 0);

							hue = this.processor.PullGlobalWord(0x6812) == 0 ? (ushort)(cityOwner == 1 ? 7 : 15) :
								this.processor.PullGlobalWord((ushort)((cityOwner << 1) + 0x1946));
						}

						this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), tileHoriz * 4, (tileVert * 4) + 4, 4, 4, hue);
					}
				}
			}

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("RenderReplayMapTile");
		}

		/// <summary>
		/// Writes the map into the replay file (handle 0x680c) as text, a line of 80 characters and CR LF for each of the 50 rows:
		/// " " ocean, "." land, "+" a city ("*" with city walls, improvement bit 1) followed by its name (as far as the line goes)
		/// </summary>
		public void DumpReplayCities()
		{
			this.processor.Journal.OpenScope("DumpReplayCities()");

			for (int vertLoc = 0; vertLoc < 50; vertLoc++)
			{
				this.processor.EmitGlobalByte(0xba56, 0xd);
				this.processor.EmitGlobalByte(0xba57, 0xa);
				this.processor.EmitGlobalByte(0xba58, 0);

				for (int horizLoc = 0; horizLoc < 80; horizLoc++)
				{
					bool ocean = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain;
					this.processor.EmitGlobalByte((ushort)(0xba06 + horizLoc), (byte)(ocean ? 0x20 : 0x2e));
				}

				for (int horizLoc = 0; horizLoc < 80; horizLoc++)
				{
					if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain)
						continue;

					for (int labelIdx = 0; labelIdx < 0x100; labelIdx++)
					{
						if ((sbyte)this.owner.World.TownPlaces[labelIdx].X != horizLoc || (sbyte)this.owner.World.TownPlaces[labelIdx].Y != vertLoc)
							continue;

						bool walls = (this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0;
						this.processor.EmitGlobalByte((ushort)(0xba06 + horizLoc), (byte)(walls ? 0x2a : 0x2b));

						// The name after it: at least its first character, then while the line goes on and the name does
						int place = 0;

						do
						{
							this.processor.EmitGlobalByte((ushort)(0xba07 + horizLoc + place),
								(byte)this.owner.World.TownLabels[labelIdx][place]);
							place++;
						} while (horizLoc + place + 1 < 0x50 && this.owner.World.TownLabels[labelIdx][place] != '\0');

						break;
					}
				}

				this.owner.Libc.WriteHandle((short)this.processor.PullGlobalWord(0x680c), 0xba06, 0x52);
			}

			this.processor.Journal.CloseScope("DumpReplayCities");
		}

		/// <summary>
		/// The names of the civilization of the player for the replay: its name (leader record 0x1502 + 0x3a * leader) into the
		/// text at 0x1982 + 2 * player, its plural (0x14f2 + ..., or the name with "s" when empty) into the text at 0x1992 + 2 * player.
		/// The plural is made in a buffer in the stack frame, like in the original.
		/// </summary>
		public void ObtainReplayPlayerLabel(short participantIdx)
		{
			this.processor.Journal.OpenScope($"ObtainReplayPlayerLabel({participantIdx})");

			this.processor.EnterFrame(0x10);
			this.processor.PreserveSource();

			ushort buffer = (ushort)(this.processor.RegFrame.Value16 - 0x10);
			ushort leader = (ushort)(0x3a * this.owner.World.Participants[participantIdx].ChiefVisuals);

			this.owner.Libc.CopyText(this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)), (ushort)(leader + 0x1502));
			this.owner.Libc.CopyText(buffer, (ushort)(leader + 0x14f2));

			if (this.processor.PullGlobalByte(buffer) == 0)
			{
				this.owner.Libc.CopyText(buffer, (ushort)(leader + 0x1502));
				this.owner.Libc.AppendText(buffer, "s");
			}

			this.owner.Libc.CopyText(this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1992)), buffer);

			this.processor.RestoreSource();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("ObtainReplayPlayerLabel");
		}

		/// <summary>
		/// The end of a conquest victory: the replay of the game (0x680e set meanwhile, the 14 bytes at 0x3772:0x37c4 cleared to
		/// 0xff), the leader's music, "The entire world hails ... the CONQUEROR!" until a key or click, then the map screen
		/// again. The texts have a shadow, in other colours with the colour cycles (0xd762).
		/// The stack frame stays: a called function leaves BP changed, and the original ends with the stack that gives.
		/// </summary>
		public void DisplayConquerorScreen()
		{
			this.processor.Journal.OpenScope("DisplayConquerorScreen()");

			this.processor.EnterFrame(0x2);

			this.processor.EmitGlobalWord(0x680e, 0x1);

			this.owner.Helpers.HaltHueCycling();

			this.owner.Startup.ConcealMouseCursor();

			this.processor.EmitGlobalByte(0xb1d4, 0x0);

			for (int idx = 0; idx < 0xe; idx++)
			{
				this.processor.EmitUInt8(0x3772, (ushort)(idx + 0x37c4), 0xff);
			}

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x6);

			DisplayReplay();

			ushort leader = (ushort)(0x3a * this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ChiefVisuals);
			this.owner.Services.PerformSound(this.processor.PullGlobalWord((ushort)(leader + 0x151a)), 3);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 18, 150, 284, 32,
				(ushort)(this.processor.PullGlobalWord(0xd762) != 0 ? 34 : 7));

			this.owner.Libc.CopyText(0xba06, "The entire world hails");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 152, 20);
			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 151, (ushort)(this.processor.PullGlobalWord(0xd762) != 0 ? 23 : 15));

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((this.owner.World.PersonParticipantIdx << 1) + 0x19a2)));
			this.owner.Libc.AppendText(0xba06, " the CONQUEROR!");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 168, 20);
			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 167, (ushort)(this.processor.PullGlobalWord(0xd762) != 0 ? 23 : 15));

			this.owner.TurnLogic.AwaitForKey();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.Services.PerformSound(1, 0);

			this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x1);

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.TurnHooks.RepaintMainScreen();

			this.processor.EmitGlobalWord(0x680e, 0x0);

			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("DisplayConquerorScreen");
		}

		/// <summary>
		/// The screen of a destroyed civilization: its leader (king00.pic with the number 0x3938 + ChiefVisuals, kink on EGA
		/// for leaders 0 and 3) looks out of the wall of the fallen leaders (0x3772:0x37c4, 14 places from slam2.pic, slag2.pic on
		/// EGA) and says his last words; the leader is put into the wall at place 0xb1d4 (the places of the two rows taken in
		/// turn), then "&lt;year&gt;: &lt;the human civilization&gt; destroy the &lt;civilization&gt;!" (the second line overwrites "the ").
		/// The file names and pictures are in the stack frame, like in the original.
		/// </summary>
		public void DisplayCivilizationDestroyedScreen(short participantIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"DisplayCivilizationDestroyedScreen({participantIdx}, {arg2})");

			// The file names are in the stack frame, like in the original, and the final values of the locals stay there
			this.processor.EnterFrame(0x36);
			this.processor.PreserveSource();

			ushort portraitName = (ushort)(this.processor.RegFrame.Value16 - 0x10);
			ushort wallName = (ushort)(this.processor.RegFrame.Value16 - 0x24);

			short leaderIdx = (sbyte)this.processor.PullGlobalByte((ushort)((ushort)this.owner.World.Participants[participantIdx].ChiefVisuals + 0x3938));

			this.owner.Libc.CopyText(portraitName, "king00.pic");

			this.processor.EmitGlobalByte((ushort)(portraitName + 5), (byte)(this.processor.PullGlobalByte((ushort)(portraitName + 5)) + (leaderIdx % 10)));
			this.processor.EmitGlobalByte((ushort)(portraitName + 4), (byte)(this.processor.PullGlobalByte((ushort)(portraitName + 4)) + (leaderIdx / 10)));

			if (this.processor.PullGlobalWord(0xd762) == 0 && (leaderIdx == 0 || leaderIdx == 3))
			{
				this.processor.EmitGlobalByte((ushort)(portraitName + 3), 0x6b);
			}

			this.owner.Startup.QueryForDisk((ushort)(((ushort)leaderIdx / 7) + 4), 1);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, portraitName, 0);

			this.owner.VideoCard.ScreenToPicture(1, 0xb5, 0x43, 0x8b, 0x85);
			ushort portrait = this.processor.RegAcc.Value16;

			this.owner.VideoCard.ScreenToPicture(1, 1, 0x33, 0x3b, 0x31);
			ushort speakingMouth = this.processor.RegAcc.Value16;

			this.owner.VideoCard.ScreenToPicture(1, 1, 0x97, 0x3b, 0x31);
			ushort closedMouth = this.processor.RegAcc.Value16;

			this.processor.EmitLocalWord(0x12, portrait);
			this.processor.EmitLocalWord(0x30, closedMouth);
			this.processor.EmitLocalWord(0x32, (ushort)leaderIdx);
			this.processor.EmitLocalWord(0x36, speakingMouth);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4112, 0);

			if (this.processor.PullGlobalWord(0xd762) != 0)
			{
				this.owner.Services.FloodColors(5, 0, 0, 0);
			}

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0xaa), 0, 0);

			this.owner.Libc.CopyText(wallName, "slam2.pic");

			if (this.processor.PullGlobalWord(0xd762) == 0)
			{
				this.processor.EmitGlobalByte((ushort)(wallName + 3), 0x67);
			}

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0x85, wallName, 0);

			RenderWallOfLeaders(this.processor.PullGlobalWord(0xaa));

			short mouthHoriz = 0x5a;
			short mouthVert = 0;
			ushort mouths = (ushort)(leaderIdx << 2);

			mouthHoriz += (short)(this.processor.PullUInt16(0x36fa, (ushort)(mouths + 0x10)) - 0xb4);
			mouthHoriz -= (short)this.processor.PullUInt16(0x36fa, (ushort)(mouths + 0x12));
			mouthVert += (short)(this.processor.PullUInt16(0x36fa, (ushort)(mouths + 0x48)) - 0x40);
			mouthVert -= (short)this.processor.PullUInt16(0x36fa, (ushort)(mouths + 0x4a));

			this.processor.EmitLocalWord(0x14, (ushort)leaderIdx);
			this.processor.EmitLocalWord(0x28, (ushort)mouthHoriz);
			this.processor.EmitLocalWord(0x2c, (ushort)mouthVert);

			this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), 90, 0, portrait);

			this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), mouthHoriz, mouthVert - 2, speakingMouth);

			this.owner.Services.PerformSound(this.processor.PullGlobalWord((ushort)((ushort)(0x3a * this.owner.World.Participants[participantIdx].ChiefVisuals) + 0x151a)), 3);

			if (this.processor.PullGlobalWord(0xd762) != 0)
			{
				this.owner.Libc.CopyText((ushort)(portraitName + 7), "pal");

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, portraitName, 0xc1d6);

				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x412a, 0xbdee);

				for (ushort colourIdx = 0xc0; colourIdx < 0x1b0; colourIdx++)
				{
					this.processor.EmitGlobalByte((ushort)(colourIdx + 0xbdf4), this.processor.PullGlobalByte((ushort)(colourIdx + 0xc1dc)));
				}

				this.processor.EmitLocalWord(0x2e, 0x1b0);

				this.owner.Services.ImportColorsFromBuffer(8, 0xbdee);
			}

			sbyte fallen = (sbyte)this.processor.PullGlobalByte(0xb1d4);
			short place = (short)(fallen % 7);

			place = (place & 1) != 0 ? (short)-((place / 2) - 6) : (short)(place / 2);

			this.processor.EmitGlobalByte(0xb1d4, (byte)(fallen + 1));

			if (fallen >= 7)
				place += 7;

			this.processor.EmitLocalWord(0x26, (ushort)place);

			this.processor.EmitUInt8(0x3772, (ushort)(place + 0x37c4), (byte)leaderIdx);

			this.owner.Services.RestoreTimer();

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4134, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0x85, wallName, 0);

			RenderWallOfLeaders(this.processor.PullGlobalWord(0x19d4));

			while ((short)this.processor.PullGlobalWord(0x5c) < 0xf0)
			{
				this.processor.EmitGlobalByte(0xba06, 0);
			}

			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(Math.Abs((short)arg2), 10));

			this.owner.Libc.AppendText(0xba06, (short)arg2 < 0 ? " BC" : " AD");

			this.owner.Libc.AppendText(0xba06, ": ");

			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)(((ushort)this.owner.World.PersonParticipantIdx << 1) + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " destroy");

			RenderShadowedCentredLine(152);

			this.owner.Libc.CopyText(0xba06, "the ");

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)(((ushort)participantIdx << 1) + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " civilization!");

			RenderShadowedCentredLine(168);

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0x85, 0x140, 0x43, this.processor.PullGlobalWord(0x19d4), 0, 0x85);

			this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), mouthHoriz, mouthVert - 2, closedMouth);

			this.owner.Helpers.ReleaseResource(portrait, 0);

			this.owner.Services.PerformSound(1, 0);

			this.owner.TextPainter.AwaitTime(60);

			this.owner.Services.PerformSound(this.processor.PullGlobalWord((ushort)((ushort)(0x3a * this.owner.World.Participants[this.owner.World.PersonParticipantIdx].ChiefVisuals) + 0x151a)), 3);

			this.owner.VideoCard.RenderScreenToMainScreen(1, 0x1e);

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0xaa), 0, 0);

			this.owner.Services.DisplayScreen(0);

			this.owner.Services.PerformSound(1, 0);

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayCivilizationDestroyedScreen");
		}

		/// <summary>
		/// The 14 places of the wall of fallen leaders (0x3772:0x37c4, 0xff empty) on the screen: the leaders' pictures from the
		/// back screen 0x19d4 (7 a row from y 0x85) at 0x2e apart, the second row 0x2a higher
		/// </summary>
		private void RenderWallOfLeaders(ushort screen)
		{
			ushort placeIdx;

			for (placeIdx = 0; (short)placeIdx < 0xe; placeIdx++)
			{
				byte leaderIdx = this.processor.PullUInt8(0x3772, (ushort)(placeIdx + 0x37c4));

				if (leaderIdx == 0xff)
					continue;

				this.processor.EmitLocalWord(0x2a, leaderIdx);

				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					(ushort)(((ushort)leaderIdx % 7 * 0x1c) + 1),
					(ushort)(((ushort)leaderIdx / 7 * 0x22) + 0x85),
					0x1b, 0x21,
					screen,
					(ushort)((placeIdx % 7 * 0x2e) + 8),
					(ushort)((-(placeIdx / 7) * 0x2a) + 0x31));
			}

			this.processor.EmitLocalWord(0x2e, placeIdx);
		}

		/// <summary>The text 0xba06 centred at x 160 in colour 20, with the text in colour 23 (VGA) or 15 one line higher</summary>
		private void RenderShadowedCentredLine(int vertLoc)
		{
			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, vertLoc, 20);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, vertLoc - 1, this.processor.PullGlobalWord(0xd762) != 0 ? (ushort)23 : (ushort)15);
		}
	}
}
