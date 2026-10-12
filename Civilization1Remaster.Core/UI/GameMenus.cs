using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2c84.
	/// </summary>
	public class GameMenus
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public GameMenus(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// Opens a menu of the menu bar (marker 0 - 4: Game, Orders, Advisors, World, Civilopedia; -1: the one under the mouse,
		/// column / 60), with the screen kept in the back screen 0x19d4. Afterwards, by the menu's result in 0x654a: 1 draws the
		/// map screen again, 0 puts the screen back.
		/// </summary>
		public void ManageMenuBar(short participantIdx, short troopIdx, short marker)
		{
			this.processor.Journal.OpenScope($"ManageMenuBar({participantIdx}, {troopIdx}, {marker})");

			this.processor.EmitGlobalWord(0xd4ca, 0xffff);
			this.processor.EmitGlobalWord(0x654a, 0x0);

			if (marker == -1)
			{
				marker = (short)this.owner.Helpers.VerifyValueSpan((short)this.owner.GlobDb3c / 60, 0, 4);
			}

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200,
				this.processor.PullGlobalWord(0x19d4), 0, 0);

			this.owner.Startup.DisplayMouseCursor();

			switch (marker)
			{
				case 0:
					DisplayGameMenu();
					break;

				case 1:
					DisplayOrdersMenu(participantIdx, troopIdx);
					break;

				case 2:
					DisplayAdvisorMenu();
					break;

				case 3:
					DisplayWorldMenu();
					break;

				case 4:
					DisplayCivilopediaMenu();
					break;
			}

			this.owner.Startup.ConcealMouseCursor();

			if (this.processor.PullGlobalWord(0x654a) == 1)
			{
				this.owner.TurnHooks.RepaintMainScreen();
			}

			if (this.processor.PullGlobalWord(0x654a) == 0)
			{
				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8,
					this.processor.PullGlobalWord(0xaa), 0, 0);
			}

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("ManageMenuBar");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayGameMenu()
		{
			this.processor.Journal.OpenScope("DisplayGameMenu()");

			// function body
			this.processor.EnterFrame(0x6);

			if (owner.World.RoundTally != 0) goto L00c0;
			this.processor.EmitGlobalWord(0xb276, 0x10);

		L00c0:
			this.owner.Libc.CopyText(0xba06, " Tax Rate\n Luxuries Rate\n FindCity\n Options\n Save Game\n Load Game\n REVOLUTION!\n \n Retire\n QUIT to DOS\n");

			this.processor.TestBitsWord((ushort)this.owner.World.StarshipMarkers, 0x100);
			if (this.processor.FlagSet.IsEqual) goto L00e8;

			this.owner.Libc.AppendText(0xba06, " View Replay\n");

		L00e8:
			this.owner.Dialogs.DisplayMenu(0xba06, 16, 8, 0);

			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);

			this.owner.TurnLogic.AwaitForKey();

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsBelowOrEqual) goto L010f;
			goto L01d4;

		L010f:
			// Load Game (5) is new, the items after it moved down by one
			switch(this.processor.RegAcc.Value16)
			{
				case 0:
					goto L0120;
				case 1:
					goto L0129;
				case 2:
					goto L0132;
				case 3:
					goto L013b;
				case 4:
					goto L019b;
				case 5:
					goto LoadGame;
				case 6:
					goto L0117;
				case 7:
					goto L01d4;
				case 8:
					goto L01a3;
				case 9:
					goto L01b1;
				case 10:
					goto L01b9;
			}

		LoadGame:
			// The saved game is chosen now and loaded by the main loop after the turn is left,
			// the same way Quit leaves the turn
			this.owner.SaveLoad.FetchBay = this.owner.SaveLoad.ChooseGameToImport();

			if (this.owner.SaveLoad.FetchBay != 0xffff)
			{
				this.processor.EmitGlobalWord(0xdc48, GameLoadAndSave.FetchSessionAsk);
				this.processor.EmitGlobalWord(0xd4ca, 0x1000);
			}
			goto L01d4;

		L0117:
			this.processor.EmitGlobalWord(0xd4ca, 0xfffe);
			goto L01d4;

		L0120:
			this.processor.EmitGlobalWord(0xd4ca, 0x3d);
			goto L01d4;

		L0129:
			this.processor.EmitGlobalWord(0xd4ca, 0x2d);
			goto L01d4;

		L0132:
			this.processor.EmitGlobalWord(0xd4ca, 0x3f);
			goto L01d4;

		L013b:
			this.owner.Libc.CopyText(0xba06, "Options:\n Instant Advice\n AutoSave\n End of Turn\n Animations\n Sound\n Enemy Moves\n Civilopedia Text\n Palace\n");

		L014b:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.SessionPrefMarkers;
			this.processor.EmitGlobalWord(0xd7f2, this.processor.RegAcc.Value16);

			this.owner.Dialogs.DisplayMenu(0xba06, 24, 16, 0);

			this.processor.EmitLocalWord(0x6, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0180;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2f9c), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0180;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x6);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			owner.World.SessionPrefMarkers ^= (short)this.processor.RegAcc.Value16;

		L0180:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L018c;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x6);
			this.processor.EmitGlobalWord(0x2f9a, this.processor.RegAcc.Value16);

		L018c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L014b;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2f9c), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L014b;
			goto L01d4;

		L019b:
			this.processor.EmitGlobalWord(0xd4ca, 0x53);
			goto L01d4;

		L01a3:
			this.processor.EmitGlobalWord(0xdc48, 0x2);

		L01a9:
			this.processor.EmitGlobalWord(0xd4ca, 0x1000);
			goto L01d4;

		L01b1:
			this.processor.EmitGlobalWord(0xdc48, 0x1);
			goto L01a9;

		L01b9:
			this.owner.Replayer.DisplayReplay();

		L01d4:
			this.processor.RegStack.Value16 = this.processor.RegFrame.Value16;
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("DisplayGameMenu");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		/// <param name="unitID"></param>
		public void DisplayOrdersMenu(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"DisplayOrdersMenu({participantIdx}, {troopIdx})");

			// function body
			this.processor.EnterFrame(0x30);
			this.processor.PreserveIndexRegisters();
			
			if (troopIdx != -1) goto L01e9;
			goto L060f;

		L01e9:
			if (troopIdx != 128) goto L01f3;
			goto L060f;

		L01f3:
			this.processor.EmitLocalWord(0x2, 0x0);
			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, (ushort)troopIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x24, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x24),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x2c, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0x24),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x2a, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, " No Orders \x008fspace\n");

			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x22), 0x20);
			this.processor.EmitGlobalWord(0xb276, 0x0);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0267;
			goto L042d;

		L0267:
			if ((this.processor.PullLocalByte(0x2c) & 1) != 0)
			{
				this.owner.Libc.AppendText(0xba06, 0x2cbc);
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, 0x2ccd);
			}

			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x22), 0x62);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x2c), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L02ae;

			this.owner.Libc.AppendText(0xba06, " Build Road \x008fr\n");

			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x22), 0x72);
			goto L02e1;

		L02ae:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x2c), 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L02e1;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x31);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L02e1;

			this.owner.Libc.AppendText(0xba06, " Build RailRoad \x008fr\n");

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x22), 0x72);

		L02e1:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x2c), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L02ea;
			goto L038e;

		L02ea:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x2a));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44a)), 0xfffe);
			if (this.processor.FlagSet.IsNotEqual) goto L0329;

			this.owner.Libc.AppendText(0xba06, " Build Irrigation");

			this.owner.TurnLogic.VerifyCoastalTile(this.processor.PullLocalSignedWord(0x24),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual) goto L0365;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x2);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitGlobalWord(0xb276, this.processor.BitOrWord(this.processor.PullGlobalWord(0xb276), this.processor.RegAcc.Value16));
			goto L0365;

		L0329:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2a));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x44a);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x0);
			if (this.processor.FlagSet.IsLess) goto L0365;

			this.owner.Libc.AppendText(0xba06, " Change to ");

			this.processor.RegBase.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2ba6)));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x282);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

		L0365:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x2a));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44a)), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L038e;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x22), 0x69);

			this.owner.Libc.AppendText(0xba06, " \x008fi\n");

		L038e:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x2c), 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L040d;
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x2a));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44e)), 0xfffe);
			if (this.processor.FlagSet.IsGreater) goto L03a8;
			
			this.owner.Libc.AppendText(0xba06, 0x2d28);

			goto L03e4;

		L03a8:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x2a));
			this.processor.RegSource.Value16 = this.processor.SumWord(this.processor.RegSource.Value16, 0x44e);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16), 0x0);
			if (this.processor.FlagSet.IsLess) goto L03e4;

			this.owner.Libc.AppendText(0xba06, " Change to ");

			this.processor.RegBase.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.RegSource.Value16);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0x13;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x2ba6)));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x282);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

		L03e4:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x2a));
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44e)), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L040d;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x22), 0x6d);

			this.owner.Libc.AppendText(0xba06, " \x008fm\n");

		L040d:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x2c), 0x40);
			if (this.processor.FlagSet.IsEqual) goto L042d;

			this.owner.Libc.AppendText(0xba06, " Clean up Pollution \x008fp\n");

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x22), 0x70);

		L042d:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0475;

			this.owner.Libc.AppendText(0xba06, " Build Fortress \x008ff\n");

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2d);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L04a4;

			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x2);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.EmitGlobalWord(0xb276, this.processor.BitOrWord(this.processor.PullGlobalWord(0xb276), this.processor.RegAcc.Value16));
			goto L04a4;

		L0475:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L04a4;

			this.owner.Libc.AppendText(0xba06, " Fortify \x008ff\n");

		L04a4:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L04cd;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x22), 0x66);

		L04cd:
			this.owner.Libc.AppendText(0xba06, " Wait \x008fw\n Sentry \x008fs\n GoTo\n");

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x22), 0x77);
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x22), 0x73);
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x22), 0x67);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x2c), 0x1e);
			if (this.processor.FlagSet.IsEqual) goto L053a;
			this.processor.ScaleIndexToBase(0xc, (ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7));
			this.processor.EmitLocalByte(0x30, this.processor.RegAcc.LowByte);
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0x1a);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L053a;
			this.processor.CompareByte(this.processor.RegAcc.LowByte, 0xe);
			if (this.processor.FlagSet.IsEqual) goto L053a;

			this.owner.Libc.AppendText(0xba06, " Pillage \x008fP\n");

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x22), 0x50);

		L053a:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x2c), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L055a;

			this.owner.Libc.AppendText(0xba06, " Home City \x008fh\n");

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x22), 0x68);

		L055a:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7));
			this.processor.EmitLocalByte(0x30, this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullLocalByte(0x30));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148)), 0x5);
			if (this.processor.FlagSet.IsEqual) goto L0585;
			this.processor.CompareByte(this.processor.PullLocalByte(0x30), 0x17);
			if (this.processor.FlagSet.IsNotEqual) goto L05b6;

		L0585:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase((ushort)troopIdx);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L05b6;

			this.owner.Libc.AppendText(0xba06, " Unload \x008fu\n");

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x22), 0x75);

		L05b6:
			this.owner.Libc.AppendText(0xba06, " \n Disband Unit \x008fD\n");

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x22), 0x0);
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x22), 0x44);

			this.owner.Dialogs.DisplayMenu(0xba06, 72, 8, 0);

			this.processor.EmitLocalWord(0x2e, this.processor.RegAcc.Value16);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsLess) goto L0609;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x2e), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0609;
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2e);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x22));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitGlobalWord(0xd4ca, this.processor.RegAcc.Value16);
			goto L060f;

		L0609:
			this.processor.EmitGlobalWord(0xd4ca, 0xffff);

		L060f:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("DisplayOrdersMenu");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayAdvisorMenu()
		{
			this.processor.Journal.OpenScope("DisplayAdvisorMenu()");

			// function body
			this.processor.EnterFrame(0x4);

			this.owner.Libc.CopyText(0xba06, " City Status (F1)\n Military Advisor (F2)\n Intelligence Advisor (F3)\n Attitude Advisor (F4)\n");

			this.owner.Libc.AppendText(0xba06, " Trade Advisor (F5)\n Science Advisor (F6)\n");

			this.owner.Dialogs.DisplayMenu(0xba06, 112, 8, 0);

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.EmitGlobalWord(0x654a, 0xffff);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L06ca;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L0694;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L06b4;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsEqual) goto L06bf;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsEqual) goto L0689;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5);
			if (this.processor.FlagSet.IsEqual) goto L06d5;

			goto L06e0;

		L0689:
			this.owner.Briefings.DisplayTradeReport(this.owner.World.PersonParticipantIdx);

			goto L06e0;

		L0694:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L06a9;

			this.owner.RealmInfo.DisplayCivilizationScoreList();

			goto L06e0;

		L06a9:
			this.owner.Briefings.RenderUnitCountsChart(this.owner.World.PersonParticipantIdx);

			goto L06e0;

		L06b4:
			this.owner.Briefings.DisplayMilitaryReport();

			goto L06e0;

		L06bf:
			this.owner.Briefings.DisplayAttitudeReport(this.owner.World.PersonParticipantIdx);

			goto L06e0;

		L06ca:
			this.owner.Briefings.DisplayCityStatusReport(this.owner.World.PersonParticipantIdx);

			goto L06e0;

		L06d5:
			this.owner.Briefings.DisplayScienceReport(this.owner.World.PersonParticipantIdx);

		L06e0:
			this.processor.RegStack.Value16 = this.processor.RegFrame.Value16;
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("DisplayAdvisorMenu");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayWorldMenu()
		{
			this.processor.Journal.OpenScope("DisplayWorldMenu()");

			// function body
			this.processor.EnterFrame(0x4);
			this.processor.TestBitsWord((ushort)this.owner.World.StarshipMarkers, 0xfe00);
			if (this.processor.FlagSet.IsNotEqual) goto L06f8;
			this.processor.EmitGlobalWord(0xb276, 0x20);

		L06f8:
			this.owner.Libc.CopyText(0xba06, " Wonders of the World (F7)\n Top 5 Cities (F8)\n Civilization Score (F9)\n");

			this.owner.Libc.AppendText(0xba06, " World Map (F10)\n Demographics\n SpaceShips\n");

			this.owner.Dialogs.DisplayMenu(0xba06, 144, 8, 0);

			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.EmitGlobalWord(0x654a, 0xffff);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0766;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L078f;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L077b;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsEqual) goto L0796;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsEqual) goto L076d;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5);
			if (this.processor.FlagSet.IsEqual) goto L07a6;
			goto L07ab;

		L0766:
			this.owner.Atlas.DisplayWondersOfTheWorld();

			goto L07ab;

		L076d:
			this.owner.Atlas.DisplayCensus(this.owner.World.PersonParticipantIdx);

			goto L07ab;

		L077b:
			this.owner.Founding.DisplayFinalScore(this.owner.World.PersonParticipantIdx, true);

			goto L07ab;

		L078f:
			this.owner.Legends.DisplayTopCities();

			goto L07ab;

		L0796:
			this.owner.Atlas.DisplayWorldMap(1);
			
			goto L07ab;

		L07a6:
			this.owner.RocketScreen.ChooseNationToView();

		L07ab:
			this.processor.RegStack.Value16 = this.processor.RegFrame.Value16;
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("DisplayWorldMenu");
		}

		/// <summary>
		/// The Civilopedia menu: the chosen part is shown (0x654a = -1 afterwards), Escape sets 0x654a to 1
		/// </summary>
		public void DisplayCivilopediaMenu()
		{
			this.processor.Journal.OpenScope("DisplayCivilopediaMenu()");

			this.owner.Libc.CopyText(0xba06, " Complete\n Civilization Advances\n City Improvements\n Military Units\n Terrain Types\n");
			this.owner.Libc.AppendText(0xba06, " Miscellaneous\n");

			this.owner.Dialogs.DisplayMenu(0xba06, 182, 8, 0);

			ushort choice = this.processor.RegAcc.Value16;

			if (choice != 0xffff)
			{
				this.owner.Encyclopedia.DisplayCivilopedia((ushort)(choice - 1));

				this.processor.EmitGlobalWord(0x654a, 0xffff);
			}
			else
			{
				this.processor.EmitGlobalWord(0x654a, 0x1);
			}

			this.processor.Journal.CloseScope("DisplayCivilopediaMenu");
		}
	}
}
