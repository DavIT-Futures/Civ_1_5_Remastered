using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 18.
	/// </summary>
	public class SpaceshipScreen : GameModule, ISpaceshipScreen
	{
		private const ushort SpaceshipPartPictureSpecsAddress = 0x4be2;
		private const ushort SpaceshipPartMassTableAddress = 0x4be0;
		private const ushort SpaceshipPartCountsAddress = 0xdea2;
		private const ushort StarColumnsAddress = 0x683e;
		private const ushort StarRowsAddress = 0x6852;
		private const ushort StarDrawnAddress = 0x6866;
		private const ushort SpaceshipCompleteMasksAddress = 0x687a;
		private const ushort SpaceshipLayoutSegment = 0x375a;
		private const ushort SpaceshipGridSegment = 0x3772;
		private const ushort SpaceshipGridBaseAddress = 0x16e0;
		private const ushort SpaceshipGridParticipantStride = 0xb4;
		private const ushort SpaceshipGridRowStride = 12;
		private const ushort SpaceshipSlotStride = 3;
		private const ushort SpaceshipSlotColumnOffset = 0;
		private const ushort SpaceshipSlotRowOffset = 1;
		private const ushort SpaceshipSlotKindOffset = 2;
		private const byte SpaceshipSlotEndMarker = 0xff;
		private const byte SpaceshipSlotEmptyMarker = 0xff;
		private const ushort SpaceshipResourceBlockAddress = 0x4cda;
		private const ushort PaletteBufferAddress1 = 0xc1d6;
		private const ushort PaletteBufferAddress2 = 0xc5be;
		private const ushort PaletteCopySourceAddress = 0xc744;
		private const ushort PaletteCopyTargetAddress = 0xc35c;
		private const ushort SpaceshipComponentChoiceDialogAddress = 0x4df9;
		private const ushort SpaceshipModuleChoiceDialogAddress = 0x4e12;

		private static readonly GameText SpaceshipBackgroundPictureAddress = new(0x4bc4, "docker.pic");
		private static readonly GameText SpaceshipPaletteAddress = new(0x4cea, "sp256.pal");
		private static readonly GameText SpaceshipPopulationHeaderAddress = new(0x4d08, "Population:");
		private static readonly GameText SpaceshipMassHeaderAddress = new(0x4d31, "Mass:");
		private static readonly GameText SpaceshipFlightTimeHeaderAddress = new(0x4d4a, "Flight Time:");
		private static readonly GameText SpaceshipSuccessRateHeaderAddress = new(0x4d60, "Prob. of Success");
		private static readonly GameText ArrivalPassedStringAddress = new(0x4d7c, "Landed!");
		private static readonly GameText ArrivalExpectedStringAddress = new(0x4d84, "Launched!");
		private static readonly GameText SpaceshipLaunchInstructionsStringAddress = new(0x4d8e, "LAUNCH");

		/// <summary>The words of the frame of RenderSpaceship, shared by its helpers (B3: in C#, new for every call)</summary>

		public SpaceshipScreen(GameEngine owner)
			: base(owner)
		{
		}

		/// <inheritdoc/>
		public void RenderSpaceScenery()
		{
			this.Processor.Journal.OpenScope("RenderSpaceScenery()");

			this.Owner.Startup.ConcealMouseCursor();

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.Startup.QueryForDisk(1, 1);

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

			this.Owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, SpaceshipBackgroundPictureAddress, 1);

			for (int partIdx = 0; partIdx < 0xa; partIdx++)
			{
				ushort part = (ushort)(0x1a * partIdx);

				ushort picture = this.Owner.VideoCard.ScreenToPicture(1,
					this.Processor.PullGlobalWord((ushort)(part + SpaceshipPartPictureSpecsAddress)),
					this.Processor.PullGlobalWord((ushort)(part + SpaceshipPartPictureSpecsAddress + 2)),
					this.Processor.PullGlobalWord((ushort)(part + SpaceshipPartPictureSpecsAddress + 4)),
					this.Processor.PullGlobalWord((ushort)(part + SpaceshipPartPictureSpecsAddress + 6)));

				this.Processor.EmitGlobalWord((ushort)((partIdx << 1) + SpaceshipPictureRules.Pictures), picture);
			}

			for (int starIdx = 0; starIdx < 0x14; starIdx++)
			{
				AssignStarColumn(starIdx, (byte)this.Owner.Libc.Dice.DrawInt(240));
				AssignStarRow(starIdx, (byte)(this.Owner.Libc.Dice.DrawInt(192) + 8));
				AssignStarDrawn(starIdx, 0x0);
			}

			this.Owner.Startup.DisplayMouseCursor();

			this.Owner.Services.AssignHueCycleSpan(1, 30, 208, 223);
			this.Owner.Services.AssignHueCycleSpan(2, 11, 192, 207);
			this.Owner.Services.AssignHueCycleSpan(3, 20, 224, 239);
			this.Owner.Services.AssignHueCycleSpan(4, 8, 144, 159);
			this.Owner.Services.AssignHueCycleSpan(5, 150, 128, 129);

			if (VgaMode != 0)
			{
				for (ushort cycleIdx = 1; cycleIdx <= 5; cycleIdx++)
				{
					this.Owner.Services.ActivateHueCycle(cycleIdx);
				}
			}

			this.Processor.Journal.CloseScope("RenderSpaceScenery");
		}

		/// <inheritdoc/>
		public void ImportSpaceScenery()
		{
			this.Processor.Journal.OpenScope("ImportSpaceScenery()");

			this.Owner.Helpers.ReleaseResource(SpaceshipPicture, SpaceshipResourceBlockAddress);

			if (VgaMode != 0)
			{
				for (ushort cycleIdx = 1; cycleIdx <= 5; cycleIdx++)
				{
					this.Owner.Services.DeactivateHueCycle(cycleIdx);
				}
			}

			this.Owner.Startup.QueryForDisk(0, 1);

			short regime = this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].RegimeKind;
			char regimeChar = (char)((regime / 2) + '0');
			char techChar = this.Owner.Advisors.PlayerKnowsTechnology(this.Owner.World.PersonParticipantIdx, 0x14) != 0 ? 'm' : 'a';
			string docname = $"back{regimeChar}{techChar}.pal";

			this.Owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, docname, PaletteBufferAddress1);

			this.Owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, SpaceshipPaletteAddress, PaletteBufferAddress2);

			this.Owner.Libc.CopyBytes(PaletteCopySourceAddress, PaletteCopyTargetAddress, 0x180);

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

			this.Owner.VideoCard.AssignHuesFromHueStruct(PaletteBufferAddress2);

			this.Owner.Helpers.SetupHueCycling();

			this.Owner.TurnHooks.RepaintMainScreen();

			this.Processor.Journal.CloseScope("ImportSpaceScenery");
		}

		/// <inheritdoc/>
		public ushort RenderSpaceship(short participantIdx, short frames, short launchable)
		{
			this.Processor.Journal.OpenScope($"RenderSpaceship({participantIdx}, {frames}, {launchable})");



			this.Owner.Startup.ConcealMouseCursor();

			bool shown = participantIdx == this.Owner.World.PersonParticipantIdx || (this.Owner.World.SessionPrefMarkers & 0x20) != 0;

			if (shown)
			{
				RenderSpaceshipTitle(participantIdx);
			}

			CountSpaceshipParts(participantIdx, shown);

			ushort mass = 0;

			for (int kind = 0; kind < 0xa; kind++)
			{
				mass += (ushort)(this.Processor.PullGlobalWord((ushort)((0x1a * kind) + SpaceshipPartMassTableAddress)) *
					SpaceshipPartCount(kind));
			}

			if ((short)mass >= 0x50 && shown)
			{
				this.Owner.Services.RenderPictureToScreen(ScreenBox, 152, 88, SpaceshipHabitatPicture);
			}

			if (shown)
			{
				ushort colour = PlayerColour(participantIdx);

				this.Owner.Services.FloodBox(ScreenBox, 240, 8, 80, 192, colour);

				this.Owner.VideoCard.RenderLine(ScreenBox, 0, 8, 0, 199, colour);

				this.Owner.VideoCard.RenderLine(ScreenBox, 0, 199, 240, 199, colour);
			}

			(short success, short flightTime, short vertLoc) = RenderSpaceshipFigures(shown, mass);

			if (SpaceshipColonists == 0)
			{
				launchable = 0;
			}

			if ((this.Owner.World.StarshipMarkers & (1 << (byte)participantIdx)) != 0)
			{
				if (shown)
				{
					Player player = this.Owner.World.Participants[participantIdx];

					this.Owner.TextPainter.RenderTextToScreen0((ushort)(player.StarshipArrivalSeason < this.Owner.World.Season ? ArrivalPassedStringAddress : ArrivalExpectedStringAddress),
						242, vertLoc, Hues.Black);
				}
			}
			else
			{
				this.Owner.World.Participants[participantIdx].StarshipArrivalSeason =
					(short)(CPU.SignedQuotient(flightTime, 0xa) + this.Owner.World.Season);

				if (participantIdx == this.Owner.World.PersonParticipantIdx && frames < 0 && launchable != 0)
				{
					this.Owner.TextPainter.RenderTextToScreen0(SpaceshipLaunchInstructionsStringAddress, 256, 190, Hues.Black);

					this.Owner.Dialogs.RenderBox(242, 188, 76, 9, Hues.Black);
				}
			}

			if (shown)
			{
				this.Owner.TurnLogic.AwaitForKey();
			}

			this.Owner.Startup.DisplayMouseCursor();

			if (shown)
			{
				MoveSpaceshipStars(participantIdx, frames, launchable, success);
			}

			this.Owner.World.Participants[participantIdx].StarshipInhabitants = (short)SpaceshipColonists;

			ushort result = (ushort)success;

			this.Processor.Journal.CloseScope("RenderSpaceship");

			return result;
		}

		/// <inheritdoc/>
		public ushort ObtainSpaceshipPartValue(short participantIdx, ushort row, ushort column)
		{
			this.Processor.Journal.OpenScope($"ObtainSpaceshipPartValue({participantIdx}, {row}, {column})");

			this.Processor.SegExtra.Value16 = SpaceshipGridSegment;

			sbyte part = (sbyte)this.Processor.PullUInt8(this.Processor.SegExtra.Value16,
				SpaceshipGridCellAddress(participantIdx, (short)row, (short)column));

			ushort taken = (ushort)(part != -1 && part < 3 ? 1 : 0);

			this.Processor.Journal.CloseScope("ObtainSpaceshipPartValue");

			return taken;
		}

		/// <inheritdoc/>
		public void RenderStarField()
		{
			this.Processor.Journal.OpenScope("RenderStarField()");

			for (int starIdx = 0; starIdx < 0x14; starIdx++)
			{
				if (StarDrawn(starIdx) != 0)
				{
					this.Owner.Services.AssignDot((sbyte)StarColumn(starIdx), (sbyte)StarRow(starIdx), 0);
				}

				ushort hue;

				switch (starIdx & 3)
				{
					case 0:
						AssignStarColumn(starIdx, (byte)(StarColumn(starIdx) + 2));
						hue = 7;
						break;

					case 1:
						AssignStarColumn(starIdx, (byte)(StarColumn(starIdx) + 1));
						hue = 3;
						break;

					default:
						if ((SpaceshipStarFieldFrame & 0x1) != 0)
						{
							AssignStarColumn(starIdx, (byte)(StarColumn(starIdx) + 1));
						}

						hue = 8;
						break;
				}

				if (StarColumn(starIdx) >= 0xf0)
				{
					AssignStarColumn(starIdx, 0x1);
					AssignStarRow(starIdx, (byte)(this.Owner.Libc.Dice.DrawInt(192) + 8));
					AssignStarDrawn(starIdx, 0x0);
				}

				if (this.Owner.VideoCard.ObtainDot(0, (sbyte)StarColumn(starIdx), (sbyte)StarRow(starIdx)) == 0)
				{
					AssignStarDrawn(starIdx, 0x1);

					this.Owner.Services.AssignDot((sbyte)StarColumn(starIdx), (sbyte)StarRow(starIdx), hue);
				}
				else
				{
					AssignStarDrawn(starIdx, 0x0);
				}
			}

			SpaceshipStarFieldFrame++;

			this.Processor.Journal.CloseScope("RenderStarField");
		}

		/// <inheritdoc/>
		public ushort ComputeSpaceshipSuccess(short participantIdx, short partKind)
		{
			this.Processor.Journal.OpenScope($"ComputeSpaceshipSuccess({participantIdx}, {partKind})");

			// The frame like the original (the functions called see the stack at the same depth)

			this.Owner.World.BotStarshipVictoryRatio = 0;

			ushort placeIdx = (ushort)(partKind == 2 || ShipArrivesFirstSoon(participantIdx) ? 0x21 : 0);
			ushort firstFree = 0x3e7;
			bool found = false;

			while (true)
			{
				if (SpaceshipPlaceCell(participantIdx, placeIdx) == SpaceshipSlotEmptyMarker)
				{
					sbyte slotKind = LayoutSlotKind(placeIdx);

					if (firstFree == 0x3e7 && slotKind >= (short)((3 * partKind) + 3))
					{
						firstFree = placeIdx;
					}

					if (slotKind / 3 == partKind)
					{
						found = true;
						break;
					}
				}

				placeIdx++;

				if (LayoutSlotMarker(placeIdx) == SpaceshipSlotEndMarker)
					break;

				if ((short)(placeIdx - firstFree) < 4 || partKind == 2)
					continue;

				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIdx,
					(short)BuildingWord(23 + partKind, BuildingTable.Technology)) != 0)
					break;
			}

			ushort result;
			bool atEnd = LayoutSlotMarker(placeIdx) == SpaceshipSlotEndMarker;

			if (atEnd && firstFree == 0x3e7)
			{
				result = 2;
			}
			else if (!found)
			{
				result = 1;
			}
			else if (atEnd && firstFree != 0x3e7)
			{
				result = 1;
			}
			else
			{
				BuildSpaceshipPart(participantIdx, placeIdx);

				result = 0;
			}

			this.Processor.Journal.CloseScope("ComputeSpaceshipSuccess");

			return result;
		}

		/// <inheritdoc/>
		public void AttachSpaceshipPart(short participantIdx, ushort partKind)
		{
			this.Processor.Journal.OpenScope($"AttachSpaceshipPart({participantIdx}, {partKind})");


			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

			RenderSpaceScenery();

			RenderSpaceship(participantIdx, 15, 0);

			this.Owner.TurnLogic.AwaitForKey();

			bool attach = false;

			while (true)
			{
				short kind = 2;

				this.Owner.Startup.DisplayMouseCursor();

				if (partKind == 1)
				{
					ushort complete = SpaceshipComponentsComplete;
					DisabledLines = complete;

					if (complete == 3)
						break;

					kind = (short)(this.Owner.TurnHooks.DisplayDialog(SpaceshipComponentChoiceDialogAddress, 100, 80) + 3);

					if (kind == 2)
					{
						LeaveAttachSpaceshipPart();
						return;
					}
				}

				if (partKind == 2)
				{
					ushort complete = SpaceshipModulesComplete;
					DisabledLines = complete;

					if (complete == 7)
						break;

					kind = (short)(this.Owner.TurnHooks.DisplayDialog(SpaceshipModuleChoiceDialogAddress, 100, 80) + 6);

					if (kind == 5)
					{
						LeaveAttachSpaceshipPart();
						return;
					}
				}

				this.Owner.Startup.ConcealMouseCursor();

				ushort slotIdx = partKind == 0 ? ChooseSpaceshipStructureSlot(participantIdx) : FindSpaceshipSlot(participantIdx, kind);

				if (slotIdx != 0xffff)
				{
					this.Processor.EmitUInt8(SpaceshipGridSegment, SpaceshipGridOffset(participantIdx, slotIdx), (byte)LayoutSlotKind(slotIdx));

					attach = true;
					break;
				}

				if (partKind == 0)
				{
					attach = true;
					break;
				}

				// A full kind (classic: never reached, a full kind ends at the end of the table, not at 0xffff): marked complete
				// (its line of the dialog disabled from now on) and told
				bool classic = this.Owner.TurnHooks.ClassicSpaceshipSlots;
				ushort completeAddress = (ushort)((partKind << 1) + SpaceshipCompleteMasksAddress);

				this.Processor.EmitGlobalWord(completeAddress, (ushort)(this.Processor.PullGlobalWord(completeAddress) |
					(1 << SpaceshipSlotRules.CompleteBit(kind, partKind, classic))));

				Message("No further ")
					.Text(SpaceshipSlotRules.PartName(kind, partKind, classic))
					.Text(" required.\n")
					.Dialog(120, 80);
			}

			if (attach && (this.Owner.World.StarshipMarkers & (1 << (byte)(participantIdx + 8))) == 0)
			{
				short rivalIdx;

				for (rivalIdx = 1; rivalIdx < 8; rivalIdx++)
				{
					if ((this.Owner.World.StarshipMarkers & (1 << (rivalIdx + 8))) != 0)
					{
						this.Owner.World.Participants[rivalIdx].TouchParticipantTimeleft = 0;
					}
				}

				this.Owner.World.StarshipMarkers |= (short)(ushort)(1 << (byte)(participantIdx + 8));
			}

			this.Owner.Startup.DisplayMouseCursor();

			this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].StarshipVictoryRatio = (short)RenderSpaceship(participantIdx, -1, 1);

			LeaveAttachSpaceshipPart();
		}

		/// <inheritdoc/>
		public void ChooseNationToView()
		{
			this.Processor.Journal.OpenScope("ChooseNationToView()");

			if ((this.Owner.World.StarshipMarkers & 0xfe00) != 0)
			{
				this.Owner.Words.Set("Which nation?\n ");

				for (int participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					this.Owner.Words.Append(CivilizationName(participantIdx));

					this.Owner.Words.Append("\n ");
				}

				DisabledLines = (ushort)((ushort)(this.Owner.World.StarshipMarkers >> 9) ^ 0xff);

				short chosenIdx = (short)(this.Owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80) + 1);

				if (chosenIdx > 0)
				{
					RenderSpaceScenery();

					RenderSpaceship(chosenIdx, -1, 1);

					ImportSpaceScenery();
				}
			}

			this.Processor.Journal.CloseScope("ChooseNationToView");
		}

		/// <inheritdoc/>
		public void FireSpaceship(short participantIdx)
		{
			this.Processor.Journal.OpenScope($"FireSpaceship({participantIdx})");

			this.Owner.World.StarshipMarkers |= (short)(1 << participantIdx);

			this.Owner.Startup.ConcealMouseCursor();

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

			this.Owner.TurnLogic.AwaitForKey();

			Message(CivilizationPlural(participantIdx))
				.Text(" LAUNCH space ship!");

			this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 92, Hues.White);

			Message("Estimate arrival in ")
				.Number(this.Owner.World.Participants[participantIdx].StarshipArrivalSeason)
				.Text(".\n");

			this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 100, Hues.White);

			this.Owner.World.Participants[participantIdx].StarshipLiftoffSeason = this.Owner.World.Season;

			if (participantIdx != this.Owner.World.PersonParticipantIdx)
			{
				RenderSpaceScenery();
			}

			this.Owner.TurnLogic.AwaitForKey();

			this.Owner.Conquest.AwaitForKeyPressOrMouseClick();

			RenderSpaceship(participantIdx, -1, 0);

			this.Owner.Startup.DisplayMouseCursor();

			if (participantIdx != this.Owner.World.PersonParticipantIdx)
			{
				ImportSpaceScenery();
			}

			this.Processor.Journal.CloseScope("FireSpaceship");
		}

		private sbyte LayoutSlotColumn(int slotIdx) => (sbyte)this.Processor.PullUInt8(SpaceshipLayoutSegment, (ushort)((slotIdx * SpaceshipSlotStride) + SpaceshipSlotColumnOffset));

		private sbyte LayoutSlotRow(int slotIdx) => (sbyte)this.Processor.PullUInt8(SpaceshipLayoutSegment, (ushort)((slotIdx * SpaceshipSlotStride) + SpaceshipSlotRowOffset));

		private sbyte LayoutSlotKind(int slotIdx) => (sbyte)this.Processor.PullUInt8(SpaceshipLayoutSegment, (ushort)((slotIdx * SpaceshipSlotStride) + SpaceshipSlotKindOffset));

		private byte LayoutSlotMarker(int slotIdx) => this.Processor.PullUInt8(SpaceshipLayoutSegment, (ushort)(slotIdx * SpaceshipSlotStride));

		private ushort SpaceshipGridCellAddress(short participantIdx, int column, int row)
		{
			return (ushort)((SpaceshipGridRowStride * column) + row + (SpaceshipGridParticipantStride * participantIdx) + SpaceshipGridBaseAddress);
		}

		private byte StarColumn(int index) => this.Processor.PullGlobalByte((ushort)(index + StarColumnsAddress));

		private void AssignStarColumn(int index, byte val) => this.Processor.EmitGlobalByte((ushort)(index + StarColumnsAddress), val);

		private byte StarRow(int index) => this.Processor.PullGlobalByte((ushort)(index + StarRowsAddress));

		private void AssignStarRow(int index, byte val) => this.Processor.EmitGlobalByte((ushort)(index + StarRowsAddress), val);

		private byte StarDrawn(int index) => this.Processor.PullGlobalByte((ushort)(index + StarDrawnAddress));

		private void AssignStarDrawn(int index, byte val) => this.Processor.EmitGlobalByte((ushort)(index + StarDrawnAddress), val);

		private ushort SpaceshipPartCount(int kind) => this.Processor.PullGlobalWord((ushort)((kind << 1) + SpaceshipPartCountsAddress));

		private void AssignSpaceshipPartCount(int kind, ushort val) => this.Processor.EmitGlobalWord((ushort)((kind << 1) + SpaceshipPartCountsAddress), val);

		/// <summary>
		/// The title: "civilization SpaceShip: X.S.S. leader" (X = the first letter of the civilization) on a band of the
		/// player's colour
		/// </summary>
		private void RenderSpaceshipTitle(short participantIdx)
		{
			ushort names = (ushort)(participantIdx << 1);

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 8, PlayerColourAt(names));

			Message(CivilizationNameAt(names))
				.Text(" SpaceShip: X.S.S. ");

			ushort length = this.Owner.Words.Length;

			this.Owner.Words.Append(LeaderNameAt(names));

			// The "X" of "X.S.S." becomes the first letter of the civilization
			this.Owner.Words[length - 7] = this.Processor.PullGlobalByte(CivilizationNameAt(names));

			this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 1, Hues.Black);
		}

		/// <summary>
		/// The parts on the layout (the places of table 0x375a, column, row, kind of place; 0xff ends it): each part is drawn
		/// (when shown) and counted in 0xdea2 by kind when a neighbouring place of it is taken (structures 0 - 2 always);
		/// a part without a neighbour gets a red frame
		/// </summary>
		private void CountSpaceshipParts(short participantIdx, bool shown)
		{
			for (int kind = 0; kind < 0xa; kind++)
			{
				AssignSpaceshipPartCount(kind, 0x0);
			}

			int placeIdx = 0;

			do
			{
				short column = LayoutSlotColumn(placeIdx);
				short row = LayoutSlotRow(placeIdx);
				short part = (sbyte)this.Processor.PullUInt8(SpaceshipGridSegment, SpaceshipGridCellAddress(participantIdx, column, row));

				if (part != -1)
				{
					if (shown)
					{
						// The picture of the part (the translated code read it from part << 11, SpaceshipPictureRules)
						this.Owner.Services.RenderPictureToScreen(ScreenBox, (column * 16) - 16, (row * 16) + 8,
							this.Processor.PullGlobalWord(SpaceshipPictureRules.PictureAddress(part, this.Owner.TurnHooks.ClassicSpaceshipPictures)));
					}

					short breadth = (short)(part >= 6 || part == 3 ? 2 : 1);
					short altitude = (short)(part >= 6 ? 2 : 1);
					short neighbours = (short)(part >= 3 ? 0 : 1);

					for (short across = 0; across < breadth; across++)
					{
						for (short down = 0; down < altitude; down++)
						{
							ushort horiz = (ushort)(column + across);
							ushort vert = (ushort)(row + down);

							neighbours += (short)ObtainSpaceshipPartValue(participantIdx, (ushort)(horiz + 1), vert);
							neighbours += (short)ObtainSpaceshipPartValue(participantIdx, (ushort)(horiz - 1), vert);
							neighbours += (short)ObtainSpaceshipPartValue(participantIdx, horiz, (ushort)(vert + 1));
							neighbours += (short)ObtainSpaceshipPartValue(participantIdx, horiz, (ushort)(vert - 1));
						}
					}

					if (neighbours != 0)
					{
						AssignSpaceshipPartCount(part, (ushort)(SpaceshipPartCount(part) + 1));
					}
					else if (shown)
					{
						this.Owner.Dialogs.RenderBox((column << 4) - 16, (row << 4) + 8, (breadth << 4) - 1, (altitude << 4) - 1, Hues.LightRed);
					}
				}
				else
				{
					// The original reads the kind of the empty place here (and forgets it)
					_ = LayoutSlotKind(placeIdx);
				}

				placeIdx++;
			}
			while (LayoutSlotMarker(placeIdx) != SpaceshipSlotEndMarker);
		}

		/// <summary>
		/// The figures of the spaceship on the right (each line drawn only when shown, except the title and the support):
		/// colonists (0xdeae) * 10,000, support (0xdeb0 per colonist module), energy (0xdeb2 per support and colonist module),
		/// mass, fuel (0xdeaa per propulsion 0xdea8), flight time, the chance of success from them
		/// </summary>
		/// <returns>The chance of success, the flight time (in tenths of years) and the row for the status line</returns>
		private (short Success, short FlightTime, short StatusRow) RenderSpaceshipFigures(bool shown, ushort mass)
		{
			short success = 0x64;
			short vertLoc = 0x10;

			this.Owner.TextPainter.RenderTextToScreen0(SpaceshipPopulationHeaderAddress, 242, 16, Hues.Black);

			vertLoc += 8;

			EmptyMessage()
				.Number((short)SpaceshipColonists)
				.Text("0,000");

			if (shown)
			{
				this.Owner.TextPainter.RenderTextToScreen0(this.Owner.Words.ToString(), 242, vertLoc, Hues.Black);
			}

			vertLoc += 8;

			// Support: life support per colonist module
			short colonists = (short)this.Owner.Helpers.VerifyValueSpan((short)SpaceshipColonists, 1, 99);
			short support = CPU.SignedQuotient((short)(0x64 * SpaceshipSupport), colonists);

			Message("Support: ")
				.Number(support)
				.Text("%");

			this.Owner.TextPainter.RenderTextToScreen0(this.Owner.Words.ToString(), 242, vertLoc, (ushort)(support < 0x64 ? 4 : 0));

			vertLoc += 8;

			if (shown)
			{
				success = CPU.SignedQuotient((short)(this.Owner.Helpers.VerifyValueSpan(support, 0, 100) * success), 0x64);
			}

			// Energy: power per support and colonist module
			short modules = (short)this.Owner.Helpers.VerifyValueSpan(
				(short)SpaceshipColonists + (short)SpaceshipSupport, 1, 99);
			short energy = CPU.SignedQuotient((short)(0xc8 * SpaceshipEnergy), modules);

			Message("Energy: ")
				.Number(energy)
				.Text("%");

			success = CPU.SignedQuotient((short)(this.Owner.Helpers.VerifyValueSpan(energy, 0, 100) * success), 0x64);

			if (shown)
			{
				this.Owner.TextPainter.RenderTextToScreen0(this.Owner.Words.ToString(), 242, vertLoc, (ushort)(energy < 0x64 ? 4 : 0));
			}

			vertLoc += 0x10;

			if (shown)
			{
				this.Owner.TextPainter.RenderTextToScreen0(SpaceshipMassHeaderAddress, 242, vertLoc, Hues.Black);
			}

			vertLoc += 8;

			// Mass in hundreds of tons ("x,x00 tons"; the translated code writes mass / 10 twice, TenthsRules)
			EmptyMessage()
				.Number(TenthsRules.Whole((short)mass))
				.Text(",")
				.Number(TenthsRules.Tenth((short)mass, this.Owner.TurnHooks.ClassicTenths))
				.Text("00 tons");

			if (shown)
			{
				this.Owner.TextPainter.RenderTextToScreen0(this.Owner.Words.ToString(), 242, vertLoc, Hues.Black);
			}

			vertLoc += 8;

			// Fuel: fuel per propulsion
			short propulsion = (short)this.Owner.Helpers.VerifyValueSpan((short)SpaceshipPropulsion, 1, 99);
			short fuel = CPU.SignedQuotient((short)(0x64 * SpaceshipFuel), propulsion);

			Message("Fuel: ")
				.Number(fuel)
				.Text("%");

			if (shown)
			{
				this.Owner.TextPainter.RenderTextToScreen0(this.Owner.Words.ToString(), 242, vertLoc, (ushort)(fuel < 0x64 ? 4 : 0));
			}

			vertLoc += 8;

			if (shown)
			{
				this.Owner.TextPainter.RenderTextToScreen0(SpaceshipFlightTimeHeaderAddress, 242, vertLoc, Hues.Black);
			}

			vertLoc += 8;

			// Flight time (tenths of years): 50 * mass / (thrust + 1), over 15 years less success
			short thrust = (short)this.Owner.Helpers.VerifyValueSpan(10 * (short)SpaceshipPropulsion, 0,
				10 * (short)SpaceshipFuel);
			short flightTime = CPU.SignedQuotient((short)(0x32 * mass), (short)(thrust + 1));

			if (flightTime > 0x96)
			{
				success -= CPU.SignedQuotient((short)(flightTime - 0x96), 0xa);
			}

			// The flight time in tenths of years ("x.x Years"; the translated code writes the time / 10 twice, TenthsRules)
			EmptyMessage()
				.Number(TenthsRules.Whole((short)flightTime))
				.Text(".")
				.Number(TenthsRules.Tenth((short)flightTime, this.Owner.TurnHooks.ClassicTenths))
				.Text(" Years");

			if (shown)
			{
				this.Owner.TextPainter.RenderTextToScreen0(this.Owner.Words.ToString(), 242, vertLoc, Hues.Black);
			}

			vertLoc += 0x10;

			success = (short)this.Owner.Helpers.VerifyValueSpan(success, 0, 100);

			if (shown)
			{
				this.Owner.TextPainter.RenderTextToScreen0(SpaceshipSuccessRateHeaderAddress, 242, vertLoc, Hues.Black);
			}

			vertLoc += 8;

			Message("--- ")
				.Number(success)
				.Text("% ---");

			if (shown)
			{
				this.Owner.TextPainter.RenderTextToScreen0(this.Owner.Words.ToString(), 242, vertLoc, Hues.Black);
			}

			return (success, flightTime, (short)(vertLoc + 0x10));
		}

		/// <summary>
		/// The stars move (every 2 beats after the launch, else 4) until a key, a click or frames frames; the key is taken. The
		/// human player launches a ship not launched yet with "l" or a click on the launch button (when launchable and frames &lt; 0),
		/// after confirming it.
		/// </summary>
		private void MoveSpaceshipStars(short participantIdx, short frames, short launchable, short success)
		{
			do
			{
				this.Owner.Startup.ConcealMouseCursor();

				RenderStarField();

				this.Owner.Startup.DisplayMouseCursor();

				this.Owner.TextPainter.AwaitTime((short)((this.Owner.World.StarshipMarkers & (1 << (byte)participantIdx)) != 0 ? 2 : 4));

				this.Owner.Startup.ObtainMouseState();

				if (this.Owner.Libc.KeyAvailable() != 0)
					break;

				frames--;
			}
			while (frames != 0 && this.Owner.GlobDb3a == 0);

			ushort key = 0;

			if (frames != 0 && this.Owner.GlobDb3a == 0)
			{
				key = this.Owner.Dialogs.ObtainNavigationKey();
			}

			if (participantIdx != this.Owner.World.PersonParticipantIdx || launchable == 0 || frames >= 0)
				return;

			bool launchClick = this.Owner.GlobDb3a != 0 && (short)this.Owner.GlobDb3c > 0xf2 && (short)this.Owner.GlobDb3e >= 0xbe;

			if (!launchClick && key != 'l')
				return;

			if ((this.Owner.World.StarshipMarkers & (1 << (byte)this.Owner.World.PersonParticipantIdx)) != 0)
				return;

			ushort launch = Message("Confirm spaceship launch.\n(")
				.Number(success)
				.Text("% chance of success)\n No Launch\n Launch CONFIRMED!\n")
				.Dialog(80, 100);

			if (launch == 1)
			{
				FireSpaceship(this.Owner.World.PersonParticipantIdx);
			}
		}

		/// <summary>
		/// The player's ship arrives within 15 years, and before the human player's (or the human player has none)
		/// </summary>
		private bool ShipArrivesFirstSoon(short participantIdx)
		{
			ushort arrival = (ushort)this.Owner.World.Participants[participantIdx].StarshipArrivalSeason;

			if (arrival == 0xffff || (short)(arrival - this.Owner.World.Season) >= 0xf)
				return false;

			ushort humanArrival = (ushort)this.Owner.World.Participants[this.Owner.World.PersonParticipantIdx].StarshipArrivalSeason;

			return (short)arrival < (short)humanArrival || humanArrival == 0xffff;
		}

		/// <summary>The address of the cell of the place (table 0x375a: column, row) in the player's spaceship (0x3772:0x16e0)</summary>
		private ushort SpaceshipPlaceAddress(short participantIdx, ushort placeIdx)
		{
			return SpaceshipGridCellAddress(participantIdx, LayoutSlotColumn(placeIdx), LayoutSlotRow(placeIdx));
		}

		/// <summary>The cell of the place in the player's spaceship (0xff: empty)</summary>
		private byte SpaceshipPlaceCell(short participantIdx, ushort placeIdx)
		{
			return this.Processor.PullUInt8(SpaceshipGridSegment, SpaceshipPlaceAddress(participantIdx, placeIdx));
		}

		/// <summary>
		/// The AI builds a part at the place: the news on the space scenery, the part of the place's kind, the ship redrawn
		/// (its chance of success into BotStarshipVictoryRatio), the ship bit (8 + player)
		/// </summary>
		private void BuildSpaceshipPart(short participantIdx, ushort placeIdx)
		{
			this.Owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

			Message(CivilizationPlural(participantIdx))
				.Text(" build space ship.\n");

			this.Owner.TextPainter.RenderCenteredTextToScreen0(this.Owner.Words.ToString(), 160, 96, Hues.White);

			this.Owner.Conquest.AwaitForKeyPressOrMouseClick();

			RenderSpaceScenery();

			this.Owner.TurnLogic.AwaitForKey();

			byte kind = (byte)LayoutSlotKind(placeIdx);

			this.Processor.EmitUInt8(SpaceshipGridSegment, SpaceshipPlaceAddress(participantIdx, placeIdx), kind);

			this.Owner.World.BotStarshipVictoryRatio = (short)RenderSpaceship(participantIdx, -1, 0);

			this.Owner.World.StarshipMarkers |= (short)(1 << (byte)(participantIdx + 8));

			this.Owner.TurnLogic.AwaitForKey();

			ImportSpaceScenery();
		}

		private void LeaveAttachSpaceshipPart()
		{
			this.Owner.Startup.ConcealMouseCursor();

			ImportSpaceScenery();

			this.Processor.Journal.CloseScope("AttachSpaceshipPart");
		}

		/// <summary>The offset of the slot slotIdx (table 0x375a) in the player's spaceship grid, segment 0x3772</summary>
		private ushort SpaceshipGridOffset(short participantIdx, ushort slotIdx)
		{
			return SpaceshipPlaceAddress(participantIdx, slotIdx);
		}

		/// <summary>Whether the slot is free (0xff) and the kind of the slot of table 0x375a</summary>
		private bool SpaceshipSlotFree(short participantIdx, ushort slotIdx, out sbyte kind)
		{
			bool free = this.Processor.PullUInt8(SpaceshipGridSegment, SpaceshipGridOffset(participantIdx, slotIdx)) == SpaceshipSlotEmptyMarker;

			kind = LayoutSlotKind(slotIdx);

			return free;
		}

		/// <summary>The first free slot of the kind, else none (0xffff; classic: the end of table 0x375a)</summary>
		private ushort FindSpaceshipSlot(short participantIdx, short kind)
		{
			ushort slotIdx = 0;

			do
			{
				if (SpaceshipSlotFree(participantIdx, slotIdx, out sbyte slotKind) && slotKind == kind)
					return slotIdx;

				slotIdx++;
			}
			while (LayoutSlotMarker(slotIdx) != SpaceshipSlotEndMarker);

			// No free place of the kind: the end of the table (classic), or none
			return this.Owner.TurnHooks.ClassicSpaceshipSlots ? slotIdx : SpaceshipSlotRules.NoSlot;
		}

		/// <summary>
		/// The slot of a new structure (kinds 0 - 2): the first free one of the inner 0x21 slots or of the outer ones, on the side
		/// where it is closer to the last built part (kinds 3 and more); a full side gives the other one, both full none
		/// (classic: 0x63, past the table, SpaceshipSlotRules.StructureSlot)
		/// </summary>
		private ushort ChooseSpaceshipStructureSlot(short participantIdx)
		{
			ushort innerFree = 0x63, innerBuilt = 0xffff;
			ushort slotIdx;

			for (slotIdx = 0; (short)slotIdx < 0x21; slotIdx++)
			{
				NoteSpaceshipStructureSlot(participantIdx, slotIdx, ref innerFree, ref innerBuilt);
			}

			ushort outerFree = 0x63, outerBuilt = 0xffff;

			slotIdx = 0x21;

			do
			{
				NoteSpaceshipStructureSlot(participantIdx, slotIdx, ref outerFree, ref outerBuilt);

				slotIdx++;
			}
			while (LayoutSlotMarker(slotIdx) != SpaceshipSlotEndMarker);

			ushort chosen = (short)(outerBuilt - outerFree) < (short)(innerBuilt - innerFree) ? innerFree : outerFree;

			return SpaceshipSlotRules.StructureSlot(chosen, chosen == innerFree ? outerFree : innerFree, this.Owner.TurnHooks.ClassicSpaceshipSlots);
		}

		private void NoteSpaceshipStructureSlot(short participantIdx, ushort slotIdx, ref ushort firstFree, ref ushort lastBuilt)
		{
			if (SpaceshipSlotFree(participantIdx, slotIdx, out sbyte slotKind))
			{
				if (slotKind < 3 && firstFree == 0x63)
					firstFree = slotIdx;
			}
			else if (slotKind >= 3)
			{
				lastBuilt = slotIdx;
			}
		}
	}
}
