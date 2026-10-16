using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;

namespace Civilization1Remaster.UI
{
	public class MainIntro
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public MainIntro(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// ?
		/// </summary>
		public void DisplayMainIntro()
		{
			this.processor.Journal.OpenScope("DisplayMainIntro()");

			this.processor.EnterFrame(0x4e);
			this.processor.PreserveSource();

			this.processor.EmitGlobalWord(0xdeba, 0x1);

			for (ushort disk = 1; disk < 8; disk++)
			{
				this.owner.Startup.QueryForDisk(disk, 0);
				if (this.processor.RegAcc.Value16 != 0)
				{
					this.processor.EmitGlobalWord(0xdeba, (ushort)(disk + 1));
				}
				else
				{
					break;
				}
			}

			while (true)
			{
				this.owner.Services.PathPresent(0x324a);
				if (this.processor.RegAcc.Value16 != 0xffff)
				{
					break;
				}
			}

			byte driverFirstChar = this.processor.PullGlobalByte(0x1a22);
			ushort isVgaOrMcga = (driverFirstChar == (byte)'m' || driverFirstChar == (byte)'M') ? (ushort)1 : (ushort)0;
			this.processor.EmitGlobalWord(0xd762, isVgaOrMcga);

			ushort isEga = (driverFirstChar == (byte)'e' || driverFirstChar == (byte)'E') ? (ushort)1 : (ushort)0;
			this.processor.EmitGlobalWord(0xb20e, isEga);

			ushort screen0Ptr = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(screen0Ptr + 0x10), 1);
			this.processor.EmitGlobalWord(0x6dfc, 0);

			ushort maxScreen = isVgaOrMcga != 0 ? (ushort)3 : (ushort)4;
			for (ushort s = 0; s < maxScreen; s++)
			{
				this.owner.VideoCard.ReserveScreen(s);
				if (this.processor.RegAcc.Value16 == 0)
				{
					this.processor.EmitGlobalWord(0x6dfc, 1);
				}
			}

			this.owner.Services.PrepareSoundDriver();
			this.owner.Services.LaunchTimer();
			this.owner.Helpers.SeedRandomizer();

			this.owner.Services.FloodBox(screen0Ptr, 0, 0, 320, 200, 0);

			if (isVgaOrMcga == 0)
			{
				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0x19fc), 0, 0, 320, 200, 0);
			}

			this.owner.VideoCard.AssignHuesByIndexArray(0x19fe);
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x328a, 1);
			ushort titleLogoHandle = this.owner.VideoCard.ScreenToPicture(1, 0, 64, 320, 80);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x3293, 1);
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0x4c, 0x140, 0x18,
				this.processor.PullGlobalWord(0x19d4),
				0, 0xb0);

			this.owner.ImageToolkit.ImportPictureOrColors(2, 0, 0, 0x329e, 1);
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19e8),
				0, 0x4c, 0x140, 0x18,
				this.processor.PullGlobalWord(0x19e8),
				0, 0xb0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x32a9, 1);
			this.owner.Services.FloodBox(screen0Ptr, 0, 12, 320, 176, 0);
			this.owner.Services.AssignHueCycleSpan(1, 14, 146, 152);

			if (isVgaOrMcga != 0)
			{
				for (ushort cycle = 1; cycle <= 3; cycle++)
				{
					this.owner.Services.ActivateHueCycle(cycle);
				}
			}

			this.owner.World.SessionPrefMarkers |= 0x10;
			this.owner.Libc.OpenStream("credits.txt", "rt");
			this.owner.GlobD768 = this.processor.RegAcc.Value16;

			this.owner.Services.RestoreTimer();
			this.owner.Services.PerformSound(3, 0);

			this.processor.EmitGlobalByte(0xba06, 0);

			this.AnimateIntroPhase1(screen0Ptr, isVgaOrMcga);
			this.AnimateIntroPhase2(screen0Ptr, isVgaOrMcga);
			this.HandleIntroSelection(screen0Ptr, isVgaOrMcga, titleLogoHandle);

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			this.processor.Journal.CloseScope("DisplayMainIntro");
		}

		private void AnimateIntroPhase1(ushort screen0Ptr, ushort isVgaOrMcga)
		{
			ushort savedFlag = 0;
			ushort scrollCol = 0;
			while (scrollCol < 320)
			{
				ushort remCol = (ushort)(320 - scrollCol);
				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0x19d4), 0, 176, (short)remCol, 24, 0);

				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0, 0x40, scrollCol, 0x18,
					this.processor.PullGlobalWord(0x19d4),
					remCol, 0xb0);

				switch (scrollCol)
				{
					case 10:
						this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(screen0Ptr + 0x10), 5);
						this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);
						break;
					case 50:
						this.processor.EmitGlobalByte(0xba06, 0);
						break;
					case 70:
					case 120:
						this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);
						break;
					case 160:
						this.processor.EmitGlobalByte(0xba06, 0);
						break;
					case 170:
					case 190:
					case 210:
					case 230:
					case 250:
					case 270:
					case 290:
					case 310:
						this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);
						break;
				}

				if (this.owner.Libc.TextLength(0xba06) != 0)
				{
					savedFlag = this.processor.PullUInt16(this.processor.SegData.Value16, screen0Ptr);
					this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, 1);

					ushort textColor = isVgaOrMcga != 0 ? (ushort)0xfc : (ushort)0x7;
					this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 184, textColor);

					ushort shadowColor1 = isVgaOrMcga != 0 ? (ushort)0xf8 : (ushort)0xf;
					this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 182, shadowColor1);

					ushort shadowColor2 = isVgaOrMcga != 0 ? (ushort)0xfa : (ushort)0xf;
					this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 183, shadowColor2);

					this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, savedFlag);
				}

				if (isVgaOrMcga == 0)
				{
					ushort curVal = this.processor.PullUInt16(this.processor.SegData.Value16, screen0Ptr);
					this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, curVal == 0 ? (ushort)3 : (ushort)0);
				}

				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0, 0, scrollCol, 0x40,
					screen0Ptr,
					remCol, 0xc);

				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0, 0xb0, 0x140, 0x18,
					screen0Ptr,
					0, 0x4c);

				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0, 0x58, scrollCol, 0x58,
					screen0Ptr,
					remCol, 0x64);

				if (isVgaOrMcga == 0)
				{
					savedFlag = this.processor.PullUInt16(this.processor.SegData.Value16, screen0Ptr);
					this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, 0);
					this.owner.Services.AssignDot(0, 0, 0);
					this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, savedFlag);
					this.owner.Services.DisplayScreen(savedFlag);
				}

				this.owner.Services.ObtainSongPosition();
				ushort songPos = this.processor.RegAcc.Value16;
				ushort scaledIdx = (ushort)(0x15 * scrollCol);
				if ((short)songPos >= (short)(scaledIdx >> 2) || (scrollCol & 1) != 0)
				{
					scrollCol++;
				}

				while (true)
				{
					this.owner.Services.ObtainSongPosition();
					if ((short)this.processor.RegAcc.Value16 <= (short)(scaledIdx >> 2))
					{
						break;
					}
				}

				if (this.owner.Libc.KeyAvailable() != 0)
				{
					scrollCol = 999;
				}

				scrollCol++;
			}
		}

		private void AnimateIntroPhase2(ushort screen0Ptr, ushort isVgaOrMcga)
		{
			ushort savedFlag = 0;
			ushort scrollCol = 0;
			while (scrollCol < 320)
			{
				ushort remCol = (ushort)(320 - scrollCol);
				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					scrollCol, 0x40, remCol, 0x18,
					this.processor.PullGlobalWord(0x19d4),
					0, 0xb0);

				if (scrollCol != 0)
				{
					this.owner.VideoCard.RenderImage(
						this.processor.PullGlobalWord(0x19e8),
						0, 0x40, scrollCol, 0x18,
						this.processor.PullGlobalWord(0x19d4),
						remCol, 0xb0);
				}

				switch (scrollCol)
				{
					case 10:
					case 30:
						this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);
						break;
					case 50:
						this.processor.EmitGlobalByte(0xba06, 0);
						break;
					case 64:
					case 84:
						this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);
						break;
					case 116:
						this.processor.EmitGlobalByte(0xba06, 0);
						break;
					case 130:
					case 150:
					case 170:
					case 190:
					case 210:
					case 230:
					case 250:
					case 270:
					case 290:
						this.owner.Libc.ScanStream((short)this.owner.GlobD768, "%[^\n]\n", 0xba06);
						break;
					case 310:
						this.processor.EmitGlobalByte(0xba06, 0);
						break;
				}

				if (this.owner.Libc.TextLength(0xba06) != 0)
				{
					savedFlag = this.processor.PullUInt16(this.processor.SegData.Value16, screen0Ptr);
					this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, 1);

					ushort textColor = isVgaOrMcga != 0 ? (ushort)0xfc : (ushort)0x7;
					this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 184, textColor);

					ushort shadowColor1 = isVgaOrMcga != 0 ? (ushort)0xf8 : (ushort)0xf;
					this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 182, shadowColor1);

					ushort shadowColor2 = isVgaOrMcga != 0 ? (ushort)0xfa : (ushort)0xf;
					this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 183, shadowColor2);

					this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, savedFlag);
				}

				if (isVgaOrMcga == 0)
				{
					ushort curVal = this.processor.PullUInt16(this.processor.SegData.Value16, screen0Ptr);
					this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, curVal == 0 ? (ushort)3 : (ushort)0);
				}

				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					scrollCol, 0, remCol, 0x40,
					screen0Ptr,
					0, 0xc);

				if (scrollCol != 0)
				{
					this.owner.VideoCard.RenderImage(
						this.processor.PullGlobalWord(0x19e8),
						0, 0, scrollCol, 0x40,
						screen0Ptr,
						remCol, 0xc);
				}

				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0, 0xb0, 0x140, 0x18,
					screen0Ptr,
					0, 0x4c);

				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					scrollCol, 0x58, remCol, 0x58,
					screen0Ptr,
					0, 0x64);

				if (scrollCol != 0)
				{
					this.owner.VideoCard.RenderImage(
						this.processor.PullGlobalWord(0x19e8),
						0, 0x58, scrollCol, 0x58,
						screen0Ptr,
						remCol, 0x64);
				}

				if (isVgaOrMcga == 0)
				{
					savedFlag = this.processor.PullUInt16(this.processor.SegData.Value16, screen0Ptr);
					this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, 0);
					this.owner.Services.AssignDot(0, 0, 0);
					this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, savedFlag);
					this.owner.Services.DisplayScreen(savedFlag);
				}

				this.owner.Services.ObtainSongPosition();
				ushort songPos = this.processor.RegAcc.Value16;
				ushort scaledIdx = (ushort)(0x15 * scrollCol);
				if ((short)(songPos + 0x1a40) >= (short)(scaledIdx >> 2) || (scrollCol & 1) != 0)
				{
					scrollCol++;
				}

				if (this.owner.Libc.KeyAvailable() == 0)
				{
					while (true)
					{
						this.owner.Services.ObtainSongPosition();
						if ((short)(this.processor.RegAcc.Value16 + 0x1a40) <= (short)(scaledIdx >> 2))
						{
							break;
						}
					}
				}

				if (this.owner.Libc.KeyAvailable() != 0)
				{
					scrollCol = 999;
				}

				scrollCol++;
			}
		}

		private void HandleIntroSelection(ushort screen0Ptr, ushort isVgaOrMcga, ushort titleLogoHandle)
		{
			this.processor.EmitGlobalWord(0x6b32, 0xfffe);

			if (this.owner.Libc.KeyAvailable() != 0)
			{
				this.processor.EmitGlobalWord(0x6b32, 0xffff);
				ushort navKey = this.owner.Dialogs.ObtainNavigationKey();
				switch (navKey)
				{
					case 0x4e:
					case 0x6e:
						this.processor.EmitGlobalWord(0x6b32, 0);
						break;
					case 0x4c:
					case 0x6c:
						this.processor.EmitGlobalWord(0x6b32, 1);
						break;
					case 0x45:
					case 0x65:
						this.processor.EmitGlobalWord(0x6b32, 2);
						break;
					case 0x43:
					case 0x63:
						this.processor.EmitGlobalWord(0x6b32, 3);
						break;
				}
			}

			if (this.processor.PullGlobalWord(0x6b32) == 0xfffe)
			{
				while (true)
				{
					this.owner.Services.ObtainSongPosition();
					if ((short)this.processor.RegAcc.Value16 >= 0xd00)
					{
						break;
					}
				}

				this.owner.VideoCard.RenderImage(
					screen0Ptr,
					0, 0, 0x140, 0xc8,
					this.processor.PullGlobalWord(0x19d4),
					0, 0);

				this.owner.Services.RenderPictureToScreen(
					this.processor.PullGlobalWord(0x19d4),
					0, 64,
					titleLogoHandle);

				if (isVgaOrMcga != 0)
				{
					this.owner.Services.AssignHueCycleSpan(4, 14, 224, 239);
					this.owner.Services.ActivateHueCycle(4);
				}

				for (ushort col = 0; col < 320; col += 4)
				{
					this.owner.VideoCard.RenderImage(
						this.processor.PullGlobalWord(0x19d4),
						col, 0x40, 4, 0x50,
						screen0Ptr,
						col, 0x40);

					this.owner.TextPainter.AwaitTime(1);
				}

				for (ushort delay = 0; delay < 100; delay++)
				{
					if (this.owner.Libc.KeyAvailable() != 0)
					{
						break;
					}
					this.owner.TextPainter.AwaitTime(5);
				}

				this.owner.TurnLogic.AwaitForKey();
				this.processor.EmitGlobalWord(0x6b32, 0xffff);
			}

			if (isVgaOrMcga != 0)
			{
				for (ushort cycle = 1; cycle <= 4; cycle++)
				{
					this.owner.Services.DeactivateHueCycle(cycle);
				}
			}
			else
			{
				this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, 0);
			}

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0x19d4), 0, 0, 320, 200, 0);
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0x19d4),
				0, 64,
				titleLogoHandle);

			this.owner.VideoCard.RenderScreenToMainScreen(1, 0x14);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				screen0Ptr,
				0, 0);

			this.owner.Services.DisplayScreen(0);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(screen0Ptr + 0x10), 1);
			this.owner.Helpers.ReleaseResource(titleLogoHandle, 0);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void DisplayCivilizationScreen(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayCivilizationScreen({participantIdx})");

			this.processor.EnterFrame(0x246);
			this.processor.PreserveSource();

			this.owner.Startup.QueryForDisk(6, 1);
			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x32d8, 0);

			ushort[] pictures = new ushort[24];
			for (ushort i = 0; i < 24; i++)
			{
				pictures[i] = this.owner.VideoCard.ScreenToPicture(
					1,
					(ushort)(((i % 6) * 50) + 1),
					(ushort)(((i / 6) * 50) + 1),
					0x31, 0x31);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + (i << 1) - 0x238), pictures[i]);
			}

			this.owner.Helpers.HaltHueCycling();

			ushort bgColor = 0;
			if (this.processor.PullGlobalWord(0xd762) != 0)
			{
				bgColor = 236;
				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 236);
			}
			else
			{
				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);
			}

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x32e4, 1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0x18, 0x140, 0xb0,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0x20, 0x140, 0x18,
				this.processor.PullGlobalWord(0x19d4),
				0, 0xb0);

			this.owner.Startup.QueryForDisk(0, 1);

			if (this.processor.PullGlobalWord(0xd762) == 0)
			{
				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0x19fc), 0, 0, 320, 200, 0);
			}

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, bgColor);
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 12, 320, 176, bgColor);

			ushort regimeKind = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind;
			ushort personParticipantIdx = (ushort)this.owner.World.PersonParticipantIdx;
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((regimeKind << 1) + 0x19b2)));
			this.owner.Libc.AppendText(0xba06, " ");
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((personParticipantIdx << 1) + 0x19a2)));
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30b8), 0xba06);

			this.owner.Libc.CopyText(
				this.processor.PullGlobalWord(0x30ba),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((participantIdx << 1) + 0x1982)));

			this.processor.EmitGlobalByte(0xba06, 0);
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].StarshipInhabitants, 10));
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30bc), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0);
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.owner.World.Participants[participantIdx].StarshipLiftoffSeason, 10));
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0);
			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(this.owner.World.Season, 10));
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30c0), 0xba06);

			this.owner.BlockText.ImportTextFromFile(0x3301, participantIdx != this.owner.World.PersonParticipantIdx ? (ushort)0x32f9 : (ushort)0x32f2);
			this.owner.BlockText.ReflowText();

			ushort textBuf = (ushort)(this.processor.RegFrame.Value16 - 0x200);
			this.owner.Libc.CopyText(textBuf, 0xba06);

			ushort screen0Ptr = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(screen0Ptr + 0x10), 0x6);

			short starX = -10;
			short starY = 92;
			short charIdx = -1;
			short textStart = -1;
			ushort textLen = (ushort)this.owner.Libc.TextLength(0xba06);

			this.owner.Services.PerformSound(3, 0);

			this.processor.EmitGlobalByte(0xba06, 0);
			ushort scrollCol = 0;
			ushort animFrame = 0;
			ushort savedScreen0Flag = 0;

			while (scrollCol < 320)
			{
				this.owner.Services.RestoreTimer();

				if (scrollCol < 320)
				{
					this.owner.Services.FloodBox(this.processor.PullGlobalWord(0x19d4), 0, 176, (short)(320 - scrollCol), 24, bgColor);
				}

				if (scrollCol != 0)
				{
					this.owner.VideoCard.RenderImage(
						this.processor.PullGlobalWord(0x19d4),
						0, 0x14, scrollCol, 0x18,
						this.processor.PullGlobalWord(0x19d4),
						(ushort)(320 - scrollCol), 0xb0);
				}

				if ((scrollCol % 22) == 0)
				{
					charIdx++;
					textStart = charIdx;

					while (true)
					{
						charIdx++;
						byte ch = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(charIdx + 0xba06));
						if (ch == (byte)'^' || charIdx >= (short)textLen)
						{
							break;
						}
					}

					if (charIdx < (short)textLen)
					{
						this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(charIdx + 0xba06), 0);
					}
				}

				if (charIdx < (short)textLen)
				{
					savedScreen0Flag = this.processor.PullUInt16(this.processor.SegData.Value16, screen0Ptr);
					this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, 1);

					ushort textColor = this.processor.PullGlobalWord(0xd762) != 0 ? (ushort)0x38 : (ushort)0x7;
					this.owner.TextPainter.RenderCenteredTextToScreen0((ushort)(0xba06 + textStart), 160, 184, textColor);

					ushort shadowColor = this.processor.PullGlobalWord(0xd762) != 0 ? (ushort)0x3c : (ushort)0xf;
					this.owner.TextPainter.RenderCenteredTextToScreen0((ushort)(0xba06 + textStart), 160, 183, shadowColor);

					this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, savedScreen0Flag);
				}

				animFrame++;
				if (animFrame >= 23)
				{
					animFrame = 0;
				}

				this.owner.Services.RenderPictureToScreen(
					this.processor.PullGlobalWord(0x19d4),
					135, 51,
					pictures[animFrame]);

				if (this.processor.PullGlobalWord(0xd762) == 0)
				{
					ushort currentVal = this.processor.PullUInt16(this.processor.SegData.Value16, screen0Ptr);
					this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, currentVal == 0 ? (ushort)3 : (ushort)0);
				}

				if (scrollCol < 320)
				{
					this.owner.Services.FloodBox(screen0Ptr, 0, 12, 319, 20, bgColor);
				}

				if (scrollCol != 0)
				{
					this.owner.VideoCard.RenderImage(
						this.processor.PullGlobalWord(0x19d4),
						0, 0, scrollCol, 0x14,
						screen0Ptr,
						(ushort)(320 - scrollCol), 0xc);
				}

				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0, 0xb0, 0x140, 0x18,
					screen0Ptr,
					0, 0x20);

				if (scrollCol < 320)
				{
					this.owner.Services.FloodBox(screen0Ptr, 0, 56, (short)(320 - scrollCol), 132, bgColor);
				}

				if (scrollCol != 0)
				{
					this.owner.VideoCard.RenderImage(
						this.processor.PullGlobalWord(0x19d4),
						0, 0x2c, scrollCol, 0x84,
						screen0Ptr,
						(ushort)(320 - scrollCol), 0x38);
				}

				if ((scrollCol & 1) != 0)
				{
					starX++;
					this.owner.Services.AssignDot(starX - (short)scrollCol, starY, 15);
				}

				starX++;
				if (this.processor.PullGlobalWord(0xd762) == 0)
				{
					savedScreen0Flag = this.processor.PullUInt16(this.processor.SegData.Value16, screen0Ptr);
					this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, 0);
					this.owner.Services.AssignDot(0, 0, 0);
					this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, savedScreen0Flag);
					this.owner.Services.DisplayScreen(savedScreen0Flag);
				}

				if (this.processor.PullGlobalWord(0x5c) > 0xc && (scrollCol & 1) == 0)
				{
					scrollCol++;
				}

				while (this.processor.PullGlobalWord(0x5c) < 8)
				{
					// wait
				}
				scrollCol++;
			}

			this.owner.Helpers.ReleaseResource(pictures[0], 0);

			this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, 0);
			this.owner.Services.DisplayScreen(0);

			this.owner.Libc.CopyText(textBuf, (ushort)(charIdx + 0xba07));
			textLen = (ushort)(textLen - (charIdx + 1));
			charIdx = -1;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.Startup.QueryForDisk(6, 1);
				this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x330a, 0);
				this.owner.Services.PerformSound(1, 0);

				if (this.processor.PullGlobalWord(0xd762) != 0)
				{
					this.owner.Services.FloodColors(5, 0, 0, 0);
				}

				this.owner.Services.FloodBox(screen0Ptr, 0, 0, 320, 200, 0);
				this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x3316, 1);
				this.owner.Startup.QueryForDisk(0, 1);
				this.owner.Services.PerformSound(0x22, 0);

				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0, 0, 0x140, 0xc8,
					screen0Ptr,
					0, 0);

				if (this.processor.PullGlobalWord(0xd762) != 0)
				{
					this.owner.Services.AssignHueCycleSpan(1, 15, 16, 31);
					this.owner.Services.AssignHueCycleSpan(2, 100, 32, 34);
					this.owner.Services.AssignHueCycleSpan(3, 42, 35, 40);
					this.owner.Services.AssignHueCycleSpan(4, 300, 41, 47);
					this.owner.Services.AssignHueCycleSpan(5, 60, 48, 53);
					this.owner.Services.AssignHueCycleSpan(6, 37, 54, 63);

					for (ushort cycle = 1; cycle <= 6; cycle++)
					{
						this.owner.Services.ActivateHueCycle(cycle);
					}
				}
			}
			else
			{
				this.owner.Throne.ImportPalaceGraphics(1);
				this.owner.VideoCard.RenderImage(
					this.processor.PullGlobalWord(0x19d4),
					0, 0, 0x140, 0xc8,
					screen0Ptr,
					0, 0);
			}

			while (charIdx < (short)textLen)
			{
				charIdx++;
				textStart = charIdx;

				while (true)
				{
					charIdx++;
					byte ch = this.processor.PullUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + charIdx - 0x200));
					if (ch == (byte)'^' || charIdx >= (short)textLen)
					{
						break;
					}
				}

				if (charIdx < (short)textLen)
				{
					this.processor.EmitUInt8(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + charIdx - 0x200), 0);
				}

				if (charIdx < (short)textLen)
				{
					this.owner.VideoCard.RenderImage(
						this.processor.PullGlobalWord(0x19d4),
						0, 0, 0x140, 0x20,
						screen0Ptr,
						0, 0);

					ushort textAddr = (ushort)(this.processor.RegFrame.Value16 + textStart - 0x200);
					this.owner.TextPainter.RenderCenteredTextToScreen0(textAddr, 160, 5, 11);
					this.owner.TextPainter.RenderCenteredTextToScreen0(textAddr, 160, 4, 15);

					this.owner.Services.AssignDot(280, 32, 15);
					this.owner.TextPainter.AwaitTime(80);
					this.owner.Services.AssignDot(280, 32, 0);
					this.owner.TextPainter.AwaitTime(80);
				}
			}

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			if (participantIdx == this.owner.World.PersonParticipantIdx && this.processor.PullGlobalWord(0xd762) != 0)
			{
				for (ushort cycle = 1; cycle <= 6; cycle++)
				{
					this.owner.Services.DeactivateHueCycle(cycle);
				}
			}

			this.processor.EmitUInt16(this.processor.SegData.Value16, screen0Ptr, 0);
			this.owner.Services.PerformSound(1, 0);
			this.owner.Libc.CloseStream((short)this.owner.GlobD768);

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(screen0Ptr + 0x10), 1);

			if (this.processor.PullGlobalWord(0xd762) == 0)
			{
				this.owner.Startup.QueryForDisk(7, 1);
				this.owner.ImageToolkit.ImportPictureOrColors(3, 0, 0, 0x3322, 0);
				this.owner.Startup.QueryForDisk(0, 1);
			}

			this.owner.Services.FloodBox(screen0Ptr, 0, 0, 320, 200, 0);
			this.owner.TurnHooks.DisplayCivilizationScenery();
			this.owner.Helpers.SetupHueCycling();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			this.processor.Journal.CloseScope("DisplayCivilizationScreen");
		}

		/// <summary>
		/// The opening news of a new game: the picture 0x332e and, with the human player's leader name in capitals (code text
		/// 0x30b8) and the last technology the player knows ("Irrigation" without any, code text 0x30be), the text 0x3342 of
		/// file 0x3348 shown part after part ("^" ends a part; the last part is not shown), each in a band at row 8 (colour 14
		/// with a shadow when 0xd762 is set, else 9), waiting 200 between parts unless a part ends with "!" or a key is waiting;
		/// then the leader's sound and the screen of the civilization
		/// </summary>
		public void DisplayTerrainTutorial()
		{
			this.processor.Journal.OpenScope("DisplayTerrainTutorial()");

			this.owner.Helpers.HaltHueCycling();

			this.owner.Startup.QueryForDisk(6, 1);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.Services.PerformSound(0x23, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x332e, 1);

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 0, 0x140, 0xc8, this.processor.PullGlobalWord(0xaa), 0, 0);

			short humanIdx = this.owner.World.PersonParticipantIdx;

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((humanIdx << 1) + 0x19a2)));
			this.owner.Libc.UppercaseText(0xba06);
			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30b8), 0xba06);

			this.owner.Libc.CopyText(0xba06, "Irrigation");

			for (short technologyIdx = 0; technologyIdx < 0x48; technologyIdx++)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(humanIdx, technologyIdx) != 0)
				{
					this.owner.Libc.CopyText(0xba06, (ushort)((technologyIdx * 0x16) + 0x4da));
				}
			}

			this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30be), 0xba06);

			this.processor.EmitGlobalByte(0xba06, 0);

			this.owner.BlockText.ImportTextFromFile(0x3348, 0x3342);

			this.owner.BlockText.ReflowText();

			this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 6);

			short end = (short)this.owner.Libc.TextLength(0xba06);
			short place = -1;

			do
			{
				ushort partAddr = (ushort)(0xba06 + place + 1);

				do
				{
					place++;
				} while (this.processor.PullGlobalByte((ushort)(0xba06 + place)) != 0x5e && place < end);

				if (place >= end)
					break;

				this.processor.EmitGlobalByte((ushort)(0xba06 + place), 0);

				this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19d4), 0, 6, 0x140, 0x14, this.processor.PullGlobalWord(0xaa), 0, 6);

				ushort hue = 9;

				if (this.processor.PullGlobalWord(0xd762) != 0)
				{
					this.owner.TextPainter.RenderCenteredTextToScreen0(partAddr, 160, 7, 15);
					this.owner.TextPainter.RenderCenteredTextToScreen0(partAddr, 160, 9, 13);

					hue = 14;
				}
				else
				{
					this.owner.TextPainter.RenderCenteredTextToScreen0(partAddr, 160, 9, 1);
				}

				this.owner.TextPainter.RenderCenteredTextToScreen0(partAddr, 160, 8, hue);

				if (this.processor.PullGlobalByte((ushort)(0xba05 + place)) != 0x21 && this.owner.Libc.KeyAvailable() == 0)
				{
					this.owner.TextPainter.AwaitTime(200);
				}
			} while (place < end);

			this.owner.Services.PerformSound(1, 0);

			this.owner.Services.PerformSound(this.processor.PullGlobalWord(
				(ushort)((this.owner.World.Participants[humanIdx].ChiefVisuals * 0x3a) + 0x1518)), 0);

			this.owner.TextPainter.AwaitTime(300);

			this.owner.Services.PerformSound(1, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.TurnHooks.DisplayCivilizationScenery();

			this.processor.EmitGlobalWord((ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 1);

			this.processor.Journal.CloseScope("DisplayTerrainTutorial");
		}

		/// <summary>
		/// The tutorial of the screen: points at the 8 tiles around the centre of the map window (0xd4cc + 7, 0xd75e + 6) with
		/// the names of their terrain (of the special resource, or "Village"), then at the menu bar, the map window and the
		/// active unit, and waits for a key or a click. SI holds 2 * direction at the pointers, like in the original: DisplayMenu
		/// leaves a value made from it in DI, which goes back to the caller.
		/// </summary>
		public void DisplayInterfaceTutorial()
		{
			this.processor.Journal.OpenScope("DisplayInterfaceTutorial()");

			this.processor.PreserveSource();

			this.owner.Startup.ConcealMouseCursor();

			short centreHoriz = (short)(this.processor.PullGlobalWord(0xd4cc) + 7);
			short centreVert = (short)(this.processor.PullGlobalWord(0xd75e) + 6);
			MapLayers chart = this.owner.MapOps.Chart;

			for (int heading = 1; heading <= 8; heading++)
			{
				int horizLoc = this.owner.Routing.TunePosition(chart.DirectionHoriz(heading) + centreHoriz);
				short vertLoc = (short)(chart.DirectionVert(heading) + centreVert);
				ushort terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);

				// Terrain names of 0x13 bytes at 0x282, names of their special resources at 0x366
				this.owner.Libc.CopyText(0xba06, (ushort)((terrain * 0x13) + 0x282));

				if (this.owner.MapOps.ContainsSpecialResource(horizLoc, vertLoc) != 0)
				{
					this.owner.Libc.CopyText(0xba06, (ushort)((terrain * 0x13) + 0x366));
				}

				if (this.owner.MapOps.ContainsHut(terrain, horizLoc, vertLoc) != 0)
				{
					this.owner.Libc.CopyText(0xba06, "Village");
				}

				this.processor.RegSource.Value16 = (ushort)(heading << 1);

				RenderTutorialPointer((chart.DirectionHoriz(heading) << 4) + 200, (chart.DirectionVert(heading) << 4) + 112,
					(chart.DirectionHoriz(heading) << 6) + 170, (chart.DirectionVert(heading) * 48) + 100);
			}

			this.owner.Libc.CopyText(0xba06, "Menu Bar");

			RenderTutorialPointer(160, 6, 200, 16);

			this.owner.Libc.CopyText(0xba06, "Map Window");

			RenderTutorialPointer(48, 32, 88, 24);

			this.owner.Libc.CopyText(0xba06, "Active Unit");

			RenderTutorialPointer(40, 128, 88, 170);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.processor.RestoreSource();

			this.processor.Journal.CloseScope("DisplayInterfaceTutorial");
		}

		/// <summary>
		/// A tutorial hint: a line (colour 15) from the pointed place to the text, then the text at 0xba06 as a dialog
		/// </summary>
		public void RenderTutorialPointer(int horizSpot1, int vertSpot1, int horizSpot2, int vertSpot2)
		{
			this.processor.Journal.OpenScope($"RenderTutorialPointer({horizSpot1}, {vertSpot1}, {horizSpot2}, {vertSpot2})");

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), horizSpot1, vertSpot1, horizSpot2, vertSpot2 + 6, 15);

			this.owner.Libc.AppendText(0xba06, "\n");

			this.processor.EmitGlobalWord(0xdb38, 0x1);

			this.owner.Dialogs.DisplayMenu(0xba06, horizSpot2, vertSpot2, 1);

			this.processor.Journal.CloseScope("RenderTutorialPointer");
		}
	}
}
