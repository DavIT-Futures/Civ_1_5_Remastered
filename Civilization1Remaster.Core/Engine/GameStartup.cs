using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Engine
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x11a8.
	/// </summary>
	public class GameStartup
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public GameStartup(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// Main game entry
		/// </summary>
		public void RunGame()
		{
			this.processor.Journal.OpenScope("RunGame()");

			// function body
			this.processor.EnterFrame(0xa);

			// Main menu selection
			// '1' - VGA; first letter of driver
			this.processor.EmitGlobalByte(0x1a22, 0x4d);
			// '1' - No sound 0x4e, '4' - Sound blaster 0x41, '5' - Roland MIDI board 0x52; first letter of driver
			this.processor.EmitGlobalByte(0x1a30, 0x4e);
			// '1' - Mouse and Keyboard 0x1, '2' - Keyboard only 0x0
			this.processor.EmitGlobalWord(0x1a3c, 0x1);

			this.owner.Prologue.DisplayMainIntro();

			QueryForDisk(0, 1);

			this.processor.StackWord(0); // stack management - push return segment, ignored
			this.processor.StackWord(0x0055); // stack management - push return offset
			this.owner.VideoCard.ObtainFreeStorage();
			this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment
			this.processor.EmitLocalWord(0x4, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6dfc), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0064;
			this.processor.EmitLocalWord(0x4, 0x0);

		L0064:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0070;
			this.processor.RegAcc.Value16 = 0x1f40;
			goto L0073;

		L0070:
			this.processor.RegAcc.Value16 = 0xfa0;

		L0073:
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x4));
			if (this.processor.FlagSet.IsLessOrEqual) goto L00ee;

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 12);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0x4), 10));

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x270);

			this.owner.Dialogs.DisplayMenu(0xba06, 64, 49, 1);

		L00ee:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0102;
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x23be);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L0102;
			this.processor.EmitGlobalWord(0x1a3e, 0x1);

		L0102:
			this.processor.StackWord(0); // stack management - push return segment, ignored
			this.processor.StackWord(0x0110); // stack management - push return offset
			this.owner.ImageToolkit.ImportIcon(0x277);
			this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment

			this.processor.EmitGlobalWord(0x6e92, this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.PullGlobalWord(0x1a3c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L012a;

			this.owner.Services.PrepareMouse();

			this.processor.EmitGlobalWord(0x1a3c, this.processor.RegAcc.Value16);

		L012a:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x1a3c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0145;

			this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullGlobalWord(0x6e92));
			
			DisplayMouseCursor();

		L0145:
			// Game type, load, etc. menu
			// And then after menu Intro
			TitleLogoAndMainChoices();

			// Start Game menu, level, tribe, name
			FreshWorldChoices();

			this.processor.CompareWord(this.processor.PullGlobalWord(0x6b32), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L016f;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].HorizBegin;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.EmitGlobalWord(0xd4cc, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xd75e, 0x13);

			this.owner.TurnHooks.RepaintMainScreen();

		L016f:
			// Mouse pointer show/hide level at the start of the game, a game loaded during play starts with the same level
			ushort arrowTier = this.processor.PullGlobalWord(0xdeea);

		LoadedGame:
			this.processor.EmitGlobalWord(0xdc48, 0x0);

		L0175:
			this.owner.TurnHooks.HandleGameEnd();

			this.processor.CompareWord(this.processor.PullGlobalWord(0xdc48), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0175;

			if (this.processor.PullGlobalWord(0xdc48) == GameLoadAndSave.FetchSessionAsk)
			{
				ImportGameDuringPlay(arrowTier);
				goto LoadedGame;
			}
			this.processor.CompareWord(this.processor.PullGlobalWord(0x1a3c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L018d;

			this.owner.Services.DeactivateMouse();

		L018d:
			this.owner.Services.PerformSound(0, 0);

			this.owner.Services.HaltSound();

			this.owner.Services.HaltTimer();
			
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RunGame");
		}

		/// <summary>
		/// Loads the game chosen with Load Game in the GAME menu, after the main loop has left the turn.
		/// It does the same steps as Load a Saved Game in the start menu.
		/// </summary>
		/// <param name="pointerLevel">Mouse pointer show/hide level at the start of the game</param>
		private void ImportGameDuringPlay(ushort arrowTier)
		{
			this.processor.Journal.OpenScope($"ImportGameDuringPlay({arrowTier})");

			this.processor.EmitGlobalWord(0xdc48, 0x0);
			this.processor.EmitGlobalWord(0xd4ca, 0x0);

			// The turn was left in the middle, restore the pointer level
			while ((short)this.processor.PullGlobalWord(0xdeea) > (short)arrowTier)
				ConcealMouseCursor();

			while ((short)this.processor.PullGlobalWord(0xdeea) < (short)arrowTier)
				DisplayMouseCursor();

			ushort bay = this.owner.SaveLoad.FetchBay;
			this.owner.SaveLoad.FetchBay = 0xffff;
			this.owner.SaveLoad.ImportGameFromSlot(bay);

			// From here the same as after Load a Saved Game in LogoAndMainGameMenu
			ConcealMouseCursor();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 7);

			this.owner.NewGameWizard.DisplayTribeSelection();

			this.processor.EmitGlobalWord(0x2f98, 0x1);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x1c0c, 1);

			this.owner.Helpers.SetupHueCycling();

			DisplayMouseCursor();

			FreshWorldChoices();

			this.processor.EmitGlobalWord(0x6b32, 0x1);
			this.processor.EmitGlobalWord(0xd4cc, (ushort)(this.owner.World.Participants[this.owner.World.PersonParticipantIdx].HorizBegin - 7));
			this.processor.EmitGlobalWord(0xd75e, 0x13);

			this.owner.TurnHooks.RepaintMainScreen();

			this.processor.Journal.CloseScope("ImportGameDuringPlay");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void ObtainMouseState()
		{
			// function body
			if (this.processor.PullGlobalWord(0x1a3c) != 0)
			{
				this.owner.Services.ObtainAndWipeMouseClick();

				this.processor.RegAcc.Value16 = this.processor.BitOrWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x5872));
				this.owner.GlobDb3a = this.processor.RegAcc.Value16;
				this.owner.GlobDb3c = this.processor.PullGlobalWord(0x586e);
				this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x5870);
				this.owner.GlobDb3e = this.processor.RegAcc.Value16;
			}
			else
			{
				this.processor.RegAcc.Value16 = 0;
				this.owner.GlobDb3e = 0;
				this.owner.GlobDb3c = 0;
				this.owner.GlobDb3a = 0;
			}
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayMouseCursor()
		{
			this.processor.Journal.OpenScope("DisplayMouseCursor()");

			// function body
			this.processor.EmitGlobalWord(0xdeea, this.processor.IncrementWord(this.processor.PullGlobalWord(0xdeea)));
			this.processor.CompareWord(this.processor.PullGlobalWord(0x1a3c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0267;
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdeea), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L0267;

			this.owner.Services.RenderMouseCursor();

		L0267:
			// Far return
			this.processor.Journal.CloseScope("DisplayMouseCursor");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void ConcealMouseCursor()
		{
			this.processor.Journal.OpenScope("ConcealMouseCursor()");

			// function body
			if (this.processor.PullGlobalWord(0x1a3c) != 0 && this.processor.PullGlobalWord(0xdeea) == 1)
			{
				this.owner.Services.BlankMouseCursor();
			}
		
			this.processor.EmitGlobalWord(0xdeea, this.processor.DecrementWord(this.processor.PullGlobalWord(0xdeea)));

			// Far return
			this.processor.Journal.CloseScope("ConcealMouseCursor");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayMouseCursorFully()
		{
			this.processor.Journal.OpenScope("DisplayMouseCursorFully()");

			// function body
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xdeea);
			this.processor.EmitGlobalWord(0x652e, this.processor.RegAcc.Value16);
			goto L028c;

		L0288:
			DisplayMouseCursor();

		L028c:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdeea), 0x1);
			if (this.processor.FlagSet.IsLess) goto L0288;
			// Far return
			this.processor.Journal.CloseScope("DisplayMouseCursorFully");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void ConcealMouseCursorToSavedLevel()
		{
			this.processor.Journal.OpenScope("ConcealMouseCursorToSavedLevel()");

			// function body
			goto L029a;

		L0296:
			ConcealMouseCursor();

		L029a:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x652e);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdeea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L0296;
			// Far return
			this.processor.Journal.CloseScope("ConcealMouseCursorToSavedLevel");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="flag"></param>
		public void QueryForDisk(ushort arg1, ushort marker)
		{
			this.processor.Journal.OpenScope($"QueryForDisk({arg1}, {marker})");

			// function body
			this.processor.EnterFrame(0x102);
			this.processor.CompareWord(marker, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L02d9;

			this.processor.RegBase.Value16 = arg1;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.Services.PathPresent(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1ace)));
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L02d4;
			this.processor.RegAcc.Value16 = arg1;
			this.processor.EmitGlobalWord(0xe17c, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x1;
			goto L03d5;

		L02d4:
			arg1 = 0;

		L02d9:
			this.processor.RegBase.Value16 = arg1;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.Services.PathPresent(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1ace)));
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L02f9;
			this.processor.CompareWord(marker, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L02f9;
			goto L03cc;

		L02f9:
			this.owner.Services.DisplayScreen(this.processor.PullUInt16(this.processor.SegData.Value16, this.processor.PullGlobalWord(0xaa)));
			
			this.processor.CompareWord(marker, 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L0312;

			marker = 1;

		L0312:
			ConcealMouseCursor();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 15);

			this.processor.RegAcc.Value16 = arg1;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0xdeba);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, 0x31);
			this.processor.EmitGlobalByte(0x1ade, this.processor.RegAcc.LowByte);

			this.owner.Libc.CopyText((ushort)(this.processor.RegFrame.Value16 - 0x100), "Please insert Disk ");

			this.owner.Libc.AppendText((ushort)(this.processor.RegFrame.Value16 - 0x100), "A");

			this.owner.Libc.AppendText((ushort)(this.processor.RegFrame.Value16 - 0x100), ".\n \n    Press Return\n    to continue.\n");

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x3936);
			this.processor.EmitLocalWord(0x102, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x3936, 0xffff);
			this.processor.EmitGlobalWord(0xd206, 0x1);

			this.owner.Dialogs.DisplayMenu((ushort)(this.processor.RegFrame.Value16 - 0x100), 100, 81, 1);

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x102);
			this.processor.EmitGlobalWord(0x3936, this.processor.RegAcc.Value16);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 15);

			this.processor.EmitGlobalWord(0x1ae0, 0x1);

			DisplayMouseCursor();

			goto L02d9;

		L03cc:
			this.processor.RegAcc.Value16 = arg1;
			this.processor.EmitGlobalWord(0xe17c, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = marker;

		L03d5:
			this.processor.RegStack.Value16 = this.processor.RegFrame.Value16;
			this.processor.RegFrame.Value16 = this.processor.UnstackWord();
			// Far return
			this.processor.Journal.CloseScope("QueryForDisk");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void TitleLogoAndMainChoices()
		{
			this.processor.Journal.OpenScope("TitleLogoAndMainChoices()");

			// function body
			this.processor.EnterFrame(0x6);
			this.processor.EmitGlobalWord(0xd76a, 0x0);
			goto L04bb;

		L0494:
			this.owner.Libc.CopyText(0xba06, " Start a New Game\n Load a Saved Game\n EARTH\n Customize World\n View Hall of Fame\n");

			this.owner.Dialogs.DisplayMenu(0xba06, 100, 140, 1);

			this.processor.EmitGlobalWord(0x6b32, this.processor.RegAcc.Value16);

		L04bb:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6b32), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0494;

		L04c2:
			this.owner.Services.ObtainSongPosition();

			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1495);
			if (this.processor.FlagSet.IsLessOrEqual) goto L04dc;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x1495);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x120;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L04c2;

			L04dc:
			this.owner.Services.PerformSound(1, 0);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6b32);
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual) goto L0511;
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L04f6;
			goto L07bd;

		L04f6:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L04fe;
			goto L0795;

		L04fe:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L0506;
			goto L0591;

		L0506:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L050e;
			goto L07ee;

		L050e:
			goto L0810;

		L0511:
			ConcealMouseCursor();

			this.processor.EmitLocalWord(0x4, 0x0);

		L051a:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x4);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x7ef6), 0x1);
			this.processor.IncrementLocalWord(0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x4);
			if (this.processor.FlagSet.IsLess) goto L051a;

			L052e:
			// Intro...
			this.processor.Journal = this.owner.PrologueJournal;
			this.processor.Journal.OpenScope("'Intro start'");
			this.owner.VideoJournal.OpenScope("'Intro start'");
			this.owner.IrqJournal.OpenScope("'Intro start'");

			this.owner.WorldSetup.OpeningSequence();

			this.owner.IrqJournal.CloseScope("'Intro end'");
			this.owner.VideoJournal.CloseScope("'Intro end'");
			this.processor.Journal.CloseScope("'Intro end'");
			this.processor.Journal = this.owner.Journal;

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);
			
			this.owner.Services.DisplayScreen(0);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x1b33, 0);

		L0549:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0xa0, 0x32, 0xa0, 0x96,
				this.processor.PullGlobalWord(0x19e8),
				0xa0, 0x32);

			QueryForDisk(0, 1);

			goto L0810;

		L0591:
			ConcealMouseCursor();

			QueryForDisk(7, 1);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L05be;

			this.owner.Services.FloodColors(5, 0, 0, 0);

		L05be:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x1b3d, 1);

			QueryForDisk(0, 1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			DisplayMouseCursor();

			this.processor.EmitGlobalWord(0x2f9a, 0x1);

		L0623:
			this.owner.Services.AssignMousePosition(210, 11);

			this.owner.Libc.CopyText(0xba06, "LAND MASS:\n Small\n Normal\n Large\n");

			this.owner.Dialogs.DisplayMenu(0xba06, 200, 1, 1);

			this.processor.EmitGlobalWord(0x7ef6, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0623;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2f9c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L067a;

			this.owner.HelpSystem.DisplayHelpTopic(0x1b6a);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x7ef6);
			this.processor.EmitGlobalWord(0x2f9a, this.processor.RegAcc.Value16);
			goto L0623;

		L067a:
			this.processor.EmitGlobalWord(0x2f9a, 0x1);

		L0680:
			this.owner.Services.AssignMousePosition(210, 61);

			this.owner.Libc.CopyText(0xba06, "TEMPERATURE:\n Cool\n Temperate\n Warm\n");

			this.owner.Dialogs.DisplayMenu(0xba06, 200, 51, 1);

			this.processor.EmitGlobalWord(0x7ef8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L0680;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2f9c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L06d7;

			this.owner.HelpSystem.DisplayHelpTopic(0x1b94);
			
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x7ef8);
			this.processor.EmitGlobalWord(0x2f9a, this.processor.RegAcc.Value16);
			goto L0680;

		L06d7:
			this.processor.EmitGlobalWord(0x2f9a, 0x1);

		L06dd:
			this.owner.Services.AssignMousePosition(210, 111);

			this.owner.Libc.CopyText(0xba06, "CLIMATE:\n Arid\n Normal\n Wet\n");

			this.owner.Dialogs.DisplayMenu(0xba06, 200, 101, 1);

			this.processor.EmitGlobalWord(0x7efa, this.processor.RegAcc.Value16);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L06dd;

			this.processor.CompareWord(this.processor.PullGlobalWord(0x2f9c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0734;

			this.owner.HelpSystem.DisplayHelpTopic(0x1bbd);
			
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x7efa);

		L072f:
			this.processor.EmitGlobalWord(0x2f9a, this.processor.RegAcc.Value16);
			goto L06dd;

		L0734:
			this.processor.EmitGlobalWord(0x2f9a, 0x1);

		L073a:
			this.owner.Services.AssignMousePosition(210, 161);

			this.owner.Libc.CopyText(0xba06, "AGE:\n 3 billion years\n 4 billion years\n 5 billion years\n");

			this.owner.Dialogs.DisplayMenu(0xba06, 200, 151, 1);

			this.processor.EmitGlobalWord(0x7efc, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L073a;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2f9c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L078e;

			this.owner.HelpSystem.DisplayHelpTopic(0x1bfe);
			
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x7efc);
			goto L072f;

		L078e:
			ConcealMouseCursor();

			goto L052e;

		L0795:
			ConcealMouseCursor();

			this.processor.EmitGlobalWord(0xd76a, 0x1);

			this.owner.WorldSetup.OpeningSequence();

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);

			this.owner.Services.DisplayScreen(0);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x1c02, 0);

			goto L0549;

		L07bd:
			this.owner.SaveLoad.ImportGameFromMenu(0xffff);
			
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0807;

			ConcealMouseCursor();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 7);
			goto L0810;

		L07ee:
			ConcealMouseCursor();

			this.owner.Legends.ImportHallOfFame();

			this.owner.Legends.DisplayHallOfFame(0xffff);

			DisplayMouseCursor();

		L0807:
			this.processor.EmitGlobalWord(0x6b32, 0xffff);
			goto L04bb;

		L0810:
			this.owner.NewGameWizard.DisplayTribeSelection();

			this.processor.EmitGlobalWord(0x2f98, 0x1);
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd762), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0848;

			this.processor.CompareWord(this.processor.PullGlobalWord(0x6b32), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0837;

			this.processor.CompareWord(this.processor.PullGlobalWord(0x6b32), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L0837;

			this.processor.CompareWord(this.processor.PullGlobalWord(0x6b32), 0x3);
			if (this.processor.FlagSet.IsNotEqual) goto L0848;

		L0837:
			this.owner.Services.FloodColors(5, 0, 0, 0);

		L0848:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x1c0c, 1);

			this.owner.Helpers.SetupHueCycling();

			DisplayMouseCursor();

			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("TitleLogoAndMainChoices");
		}

		/// <summary>
		/// ?
		/// </summary>
		public void FreshWorldChoices()
		{
			this.processor.Journal.OpenScope("FreshWorldChoices()");

			// function body
			this.processor.EnterFrame(0x6);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x1a3c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L0899;

			this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullGlobalWord(0xd4dc));

		L0899:
			if (this.owner.World.RoundTally != 0) goto L08c5;

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x1c16, 1);

			QueryForDisk(0, 1);

			this.owner.NewGameWizard.ConfigureFreshGame();

		L08c5:
			this.owner.NewGameWizard.ImportGovernmentGraphics();

			QueryForDisk(0, 1);

			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("FreshWorldChoices");
		}
	}
}
