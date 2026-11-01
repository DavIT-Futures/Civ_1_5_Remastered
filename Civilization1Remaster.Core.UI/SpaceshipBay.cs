namespace Civilization1Remaster.Core.UI
{
	public class SpaceshipBay : GameComponent, ISpaceshipBay
	{
		private const ushort SpaceshipPartPictureSpecificationsAddress = 0x4be2;
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
		private const ushort FirstPaletteBufferAddress = 0xc1d6;
		private const ushort SecondPaletteBufferAddress = 0xc5be;
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

		public SpaceshipBay(IGameEngine owner)
			: base(owner)
		{
		}

		public void DrawSpaceScenery()
		{
			using var _ = this.Processor.Journal.Scope("RenderSpaceScenery()");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.Helpers.HaltHueCycling();

			this.Owner.Startup.AskForDisk(1, 1);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			this.Owner.ImageToolkit.LoadPictureOrColors(1, 0, 0, SpaceshipBackgroundPictureAddress, 1);

			for (int partIndex = 0; partIndex < 0xa; partIndex++)
			{
				ushort part = (ushort)(0x1a * partIndex);

				ushort picture = this.Owner.VideoCard.ScreenToPicture(1,
					this.Processor.PullGlobalWord((ushort)(part + SpaceshipPartPictureSpecificationsAddress)),
					this.Processor.PullGlobalWord((ushort)(part + SpaceshipPartPictureSpecificationsAddress + 2)),
					this.Processor.PullGlobalWord((ushort)(part + SpaceshipPartPictureSpecificationsAddress + 4)),
					this.Processor.PullGlobalWord((ushort)(part + SpaceshipPartPictureSpecificationsAddress + 6)));

				this.Processor.EmitGlobalWord((ushort)((partIndex << 1) + SpaceshipPictureRules.Pictures), picture);
			}

			for (int starIndex = 0; starIndex < 0x14; starIndex++)
			{
				AssignStarColumn(starIndex, (byte)this.Owner.Runtime.Dice.DrawInteger(240));
				AssignStarRow(starIndex, (byte)(this.Owner.Runtime.Dice.DrawInteger(192) + 8));
				AssignStarDrawn(starIndex, 0x0);
			}

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.Services.AssignHueCycleSpan(1, 30, 208, 223);
			this.Owner.Services.AssignHueCycleSpan(2, 11, 192, 207);
			this.Owner.Services.AssignHueCycleSpan(3, 20, 224, 239);
			this.Owner.Services.AssignHueCycleSpan(4, 8, 144, 159);
			this.Owner.Services.AssignHueCycleSpan(5, 150, 128, 129);

			if (Screen.VideoGraphicsMode != 0)
			{
				for (ushort cycleIndex = 1; cycleIndex <= 5; cycleIndex++)
				{
					this.Owner.Services.ActivateHueCycle(cycleIndex);
				}
			}
		}

		public void LoadSpaceScenery()
		{
			using var _ = this.Processor.Journal.Scope("ImportSpaceScenery()");

			this.Owner.Helpers.ReleaseResource(Spaceship.Picture, SpaceshipResourceBlockAddress);

			if (Screen.VideoGraphicsMode != 0)
			{
				for (ushort cycleIndex = 1; cycleIndex <= 5; cycleIndex++)
				{
					this.Owner.Services.DeactivateHueCycle(cycleIndex);
				}
			}

			this.Owner.Startup.AskForDisk(0, 1);

			short regime = this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].RegimeKind;
			char regimeCharacter = (char)((regime / 2) + '0');
			char technologyCharacter = this.Owner.Advisors.PlayerKnowsTechnology(this.Owner.World.PersonParticipantIndex, 0x14) != 0 ? 'm' : 'a';
			string documentName = $"back{regimeCharacter}{technologyCharacter}.pal";

			this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, documentName, FirstPaletteBufferAddress);

			this.Owner.ImageToolkit.LoadPictureOrColors(-1, 0, 0, SpaceshipPaletteAddress, SecondPaletteBufferAddress);

			this.Owner.Runtime.CopyBytes(PaletteCopySourceAddress, PaletteCopyTargetAddress, 0x180);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			this.Owner.VideoCard.AssignHuesFromHueStructure(SecondPaletteBufferAddress);

			this.Owner.Helpers.SetupHueCycling();

			this.Owner.TurnHooks.RepaintMainScreen();
		}

		public ushort DrawSpaceship(short participantIndex, short frames, short launchable)
		{
			using var _ = this.Processor.Journal.Scope($"RenderSpaceship({participantIndex}, {frames}, {launchable})");

			this.Owner.Startup.HideMouseCursor();

			bool shown = participantIndex == this.Owner.World.PersonParticipantIndex || (this.Owner.World.SessionPreferenceMarkers & 0x20) != 0;

			if (shown)
			{
				DrawSpaceshipTitle(participantIndex);
			}

			CountSpaceshipParts(participantIndex, shown);

			ushort mass = 0;

			for (int kind = 0; kind < 0xa; kind++)
			{
				mass += (ushort)(this.Processor.PullGlobalWord((ushort)((0x1a * kind) + SpaceshipPartMassTableAddress)) *
					SpaceshipPartCount(kind));
			}

			if ((short)mass >= 0x50 && shown)
			{
				this.Owner.Services.DrawPictureToScreen(Screen.Box, 152, 88, Spaceship.HabitatPicture);
			}

			if (shown)
			{
				ushort colour = Tables.PlayerColour(participantIndex);

				this.Owner.Services.FloodBox(Screen.Box, 240, 8, 80, 192, colour);

				this.Owner.VideoCard.RenderLine(Screen.Box, 0, 8, 0, 199, colour);

				this.Owner.VideoCard.RenderLine(Screen.Box, 0, 199, 240, 199, colour);
			}

			(short success, short flightTime, short verticalLocation) = RenderSpaceshipFigures(shown, mass);

			if (Spaceship.Colonists == 0)
			{
				launchable = 0;
			}

			if ((this.Owner.World.StarshipMarkers & (1 << (byte)participantIndex)) != 0)
			{
				if (shown)
				{
					Player player = this.Owner.World.Participants[participantIndex];

					this.Owner.TextPainter.DrawTextToPrimaryScreen((ushort)(player.StarshipArrivalSeason < this.Owner.World.Season ? ArrivalPassedStringAddress : ArrivalExpectedStringAddress),
						242, verticalLocation, Hues.Black);
				}
			}
			else
			{
				this.Owner.World.Participants[participantIndex].StarshipArrivalSeason =
					(short)(CentralProcessor.SignedQuotient(flightTime, 0xa) + this.Owner.World.Season);

				if (participantIndex == this.Owner.World.PersonParticipantIndex && frames < 0 && launchable != 0)
				{
					this.Owner.TextPainter.DrawTextToPrimaryScreen(SpaceshipLaunchInstructionsStringAddress, 256, 190, Hues.Black);

					this.Owner.Dialogs.DrawBox(242, 188, 76, 9, Hues.Black);
				}
			}

			if (shown)
			{
				this.Owner.TurnLogic.WaitForKey();
			}

			this.Owner.Startup.ShowMouseCursor();

			if (shown)
			{
				MoveSpaceshipStars(participantIndex, frames, launchable, success);
			}

			this.Owner.World.Participants[participantIndex].StarshipInhabitants = (short)Spaceship.Colonists;

			ushort result = (ushort)success;

			return result;
		}

		public ushort ObtainSpaceshipPartValue(short participantIndex, ushort row, ushort column)
		{
			using var _ = this.Processor.Journal.Scope($"ObtainSpaceshipPartValue({participantIndex}, {row}, {column})");

			this.Processor.SegmentExtra.Word = SpaceshipGridSegment;

			sbyte part = (sbyte)this.Processor.PullByte(this.Processor.SegmentExtra.Word,
				SpaceshipGridCellAddress(participantIndex, (short)row, (short)column));

			ushort taken = (ushort)(part != -1 && part < 3 ? 1 : 0);

			return taken;
		}

		public void DrawStarField()
		{
			using var _ = this.Processor.Journal.Scope("RenderStarField()");

			for (int starIndex = 0; starIndex < 0x14; starIndex++)
			{
				if (StarDrawn(starIndex) != 0)
				{
					this.Owner.Services.AssignDot((sbyte)StarColumn(starIndex), (sbyte)StarRow(starIndex), 0);
				}

				ushort hue;

				switch (starIndex & 3)
				{
					case 0:
						AssignStarColumn(starIndex, (byte)(StarColumn(starIndex) + 2));
						hue = 7;
						break;

					case 1:
						AssignStarColumn(starIndex, (byte)(StarColumn(starIndex) + 1));
						hue = 3;
						break;

					default:
						if ((Spaceship.StarFieldFrame & 0x1) != 0)
						{
							AssignStarColumn(starIndex, (byte)(StarColumn(starIndex) + 1));
						}

						hue = 8;
						break;
				}

				if (StarColumn(starIndex) >= 0xf0)
				{
					AssignStarColumn(starIndex, 0x1);
					AssignStarRow(starIndex, (byte)(this.Owner.Runtime.Dice.DrawInteger(192) + 8));
					AssignStarDrawn(starIndex, 0x0);
				}

				if (this.Owner.VideoCard.ObtainDot(0, (sbyte)StarColumn(starIndex), (sbyte)StarRow(starIndex)) == 0)
				{
					AssignStarDrawn(starIndex, 0x1);

					this.Owner.Services.AssignDot((sbyte)StarColumn(starIndex), (sbyte)StarRow(starIndex), hue);
				}
				else
				{
					AssignStarDrawn(starIndex, 0x0);
				}
			}

			Spaceship.StarFieldFrame++;
		}

		public ushort ComputeSpaceshipSuccess(short participantIndex, short partKind)
		{
			using var _ = this.Processor.Journal.Scope($"ComputeSpaceshipSuccess({participantIndex}, {partKind})");

			this.Owner.World.ComputerStarshipVictoryRatio = 0;

			ushort placeIndex = (ushort)(partKind == 2 || ShipArrivesFirstSoon(participantIndex) ? 0x21 : 0);
			ushort firstFree = 0x3e7;
			bool found = false;

			while (true)
			{
				if (SpaceshipPlaceCell(participantIndex, placeIndex) == SpaceshipSlotEmptyMarker)
				{
					sbyte slotKind = LayoutSlotKind(placeIndex);

					if (firstFree == 0x3e7 && slotKind >= (short)((3 * partKind) + 3))
					{
						firstFree = placeIndex;
					}

					if (slotKind / 3 == partKind)
					{
						found = true;
						break;
					}
				}

				placeIndex++;

				if (LayoutSlotMarker(placeIndex) == SpaceshipSlotEndMarker)
				{
					break;
				}

				if ((short)(placeIndex - firstFree) < 4 || partKind == 2)
				{
					continue;
				}

				if (this.Owner.Advisors.PlayerKnowsTechnology(participantIndex,
					(short)Tables.BuildingWord(23 + partKind, BuildingTable.Technology)) != 0)
				{
					break;
				}
			}

			ushort result;
			bool atEnd = LayoutSlotMarker(placeIndex) == SpaceshipSlotEndMarker;

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
				BuildSpaceshipPart(participantIndex, placeIndex);

				result = 0;
			}

			return result;
		}

		public void AttachSpaceshipPart(short participantIndex, ushort partKind)
		{
			using var _ = this.Processor.Journal.Scope($"AttachSpaceshipPart({participantIndex}, {partKind})");

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			DrawSpaceScenery();

			DrawSpaceship(participantIndex, 15, 0);

			this.Owner.TurnLogic.WaitForKey();

			bool attach = false;

			while (true)
			{
				short kind = 2;

				this.Owner.Startup.ShowMouseCursor();

				if (partKind == 1)
				{
					ushort complete = Spaceship.ComponentsComplete;
					Menu.DisabledLines = complete;

					if (complete == 3)
					{
						break;
					}

					kind = (short)(this.Owner.TurnHooks.ShowDialog(SpaceshipComponentChoiceDialogAddress, 100, 80) + 3);

					if (kind == 2)
					{
						LeaveAttachSpaceshipPart();
						return;
					}
				}

				if (partKind == 2)
				{
					ushort complete = Spaceship.ModulesComplete;
					Menu.DisabledLines = complete;

					if (complete == 7)
					{
						break;
					}

					kind = (short)(this.Owner.TurnHooks.ShowDialog(SpaceshipModuleChoiceDialogAddress, 100, 80) + 6);

					if (kind == 5)
					{
						LeaveAttachSpaceshipPart();
						return;
					}
				}

				this.Owner.Startup.HideMouseCursor();

				ushort slotIndex = partKind == 0 ? ChooseSpaceshipStructureSlot(participantIndex) : FindSpaceshipSlot(participantIndex, kind);

				if (slotIndex != 0xffff)
				{
					this.Processor.EmitByte(SpaceshipGridSegment, SpaceshipGridOffset(participantIndex, slotIndex), (byte)LayoutSlotKind(slotIndex));

					attach = true;
					break;
				}

				if (partKind == 0)
				{
					attach = true;
					break;
				}

				ushort completeAddress = (ushort)((partKind << 1) + SpaceshipCompleteMasksAddress);

				this.Processor.EmitGlobalWord(completeAddress, (ushort)(this.Processor.PullGlobalWord(completeAddress) |
					(1 << SpaceshipSlotRules.CompleteBit(kind, partKind))));

				Message("No further ")
					.Text(SpaceshipSlotRules.PartName(kind))
					.Text(" required.\n")
					.Dialog(120, 80);
			}

			if (attach && (this.Owner.World.StarshipMarkers & (1 << (byte)(participantIndex + 8))) == 0)
			{
				short rivalIndex;

				for (rivalIndex = 1; rivalIndex < 8; rivalIndex++)
				{
					if ((this.Owner.World.StarshipMarkers & (1 << (rivalIndex + 8))) != 0)
					{
						this.Owner.World.Participants[rivalIndex].TouchParticipantTimeLeft = 0;
					}
				}

				this.Owner.World.StarshipMarkers |= (short)(ushort)(1 << (byte)(participantIndex + 8));
			}

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].StarshipVictoryRatio = (short)DrawSpaceship(participantIndex, -1, 1);

			LeaveAttachSpaceshipPart();
		}

		public void ChooseNationToView()
		{
			using var _ = this.Processor.Journal.Scope("ChooseNationToView()");

			if ((this.Owner.World.StarshipMarkers & 0xfe00) != 0)
			{
				this.Owner.Words.Set("Which nation?\n ");

				for (int participantIndex = 1; participantIndex < 8; participantIndex++)
				{
					this.Owner.Words.Append(Tables.CivilizationName(participantIndex));

					this.Owner.Words.Append("\n ");
				}

				Menu.DisabledLines = (ushort)((ushort)(this.Owner.World.StarshipMarkers >> 9) ^ 0xff);

				short chosenIndex = (short)(this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80) + 1);

				if (chosenIndex > 0)
				{
					DrawSpaceScenery();

					DrawSpaceship(chosenIndex, -1, 1);

					LoadSpaceScenery();
				}
			}
		}

		public void FireSpaceship(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"FireSpaceship({participantIndex})");

			this.Owner.World.StarshipMarkers |= (short)(1 << participantIndex);

			this.Owner.Startup.HideMouseCursor();

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			this.Owner.TurnLogic.WaitForKey();

			Message(Tables.CivilizationPlural(participantIndex))
				.Text(" LAUNCH space ship!");

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 92, Hues.White);

			Message("Estimate arrival in ")
				.Number(this.Owner.World.Participants[participantIndex].StarshipArrivalSeason)
				.Text(".\n");

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 100, Hues.White);

			this.Owner.World.Participants[participantIndex].StarshipLiftoffSeason = this.Owner.World.Season;

			if (participantIndex != this.Owner.World.PersonParticipantIndex)
			{
				DrawSpaceScenery();
			}

			this.Owner.TurnLogic.WaitForKey();

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			DrawSpaceship(participantIndex, -1, 0);

			this.Owner.Startup.ShowMouseCursor();

			if (participantIndex != this.Owner.World.PersonParticipantIndex)
			{
				LoadSpaceScenery();
			}
		}

		private sbyte LayoutSlotColumn(int slotIndex) => (sbyte)this.Processor.PullByte(SpaceshipLayoutSegment, (ushort)((slotIndex * SpaceshipSlotStride) + SpaceshipSlotColumnOffset));

		private sbyte LayoutSlotRow(int slotIndex) => (sbyte)this.Processor.PullByte(SpaceshipLayoutSegment, (ushort)((slotIndex * SpaceshipSlotStride) + SpaceshipSlotRowOffset));

		private sbyte LayoutSlotKind(int slotIndex) => (sbyte)this.Processor.PullByte(SpaceshipLayoutSegment, (ushort)((slotIndex * SpaceshipSlotStride) + SpaceshipSlotKindOffset));

		private byte LayoutSlotMarker(int slotIndex) => this.Processor.PullByte(SpaceshipLayoutSegment, (ushort)(slotIndex * SpaceshipSlotStride));

		private ushort SpaceshipGridCellAddress(short participantIndex, int column, int row)
		{
			return (ushort)((SpaceshipGridRowStride * column) + row + (SpaceshipGridParticipantStride * participantIndex) + SpaceshipGridBaseAddress);
		}

		private byte StarColumn(int index) => this.Processor.PullGlobalByte((ushort)(index + StarColumnsAddress));

		private void AssignStarColumn(int index, byte value) => this.Processor.EmitGlobalByte((ushort)(index + StarColumnsAddress), value);

		private byte StarRow(int index) => this.Processor.PullGlobalByte((ushort)(index + StarRowsAddress));

		private void AssignStarRow(int index, byte value) => this.Processor.EmitGlobalByte((ushort)(index + StarRowsAddress), value);

		private byte StarDrawn(int index) => this.Processor.PullGlobalByte((ushort)(index + StarDrawnAddress));

		private void AssignStarDrawn(int index, byte value) => this.Processor.EmitGlobalByte((ushort)(index + StarDrawnAddress), value);

		private ushort SpaceshipPartCount(int kind) => this.Processor.PullGlobalWord((ushort)((kind << 1) + SpaceshipPartCountsAddress));

		private void AssignSpaceshipPartCount(int kind, ushort value) => this.Processor.EmitGlobalWord((ushort)((kind << 1) + SpaceshipPartCountsAddress), value);

		private void DrawSpaceshipTitle(short participantIndex)
		{
			ushort names = (ushort)(participantIndex << 1);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 8, Tables.PlayerColourAt(names));

			Message(Tables.CivilizationNameAt(names))
				.Text(" SpaceShip: X.S.S. ");

			ushort length = this.Owner.Words.Length;

			this.Owner.Words.Append(Tables.LeaderNameAt(names));

			this.Owner.Words[length - 7] = this.Processor.PullGlobalByte(Tables.CivilizationNameAt(names));

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 1, Hues.Black);
		}

		private void CountSpaceshipParts(short participantIndex, bool shown)
		{
			for (int kind = 0; kind < 0xa; kind++)
			{
				AssignSpaceshipPartCount(kind, 0x0);
			}

			int placeIndex = 0;

			do
			{
				short column = LayoutSlotColumn(placeIndex);
				short row = LayoutSlotRow(placeIndex);
				short part = (sbyte)this.Processor.PullByte(SpaceshipGridSegment, SpaceshipGridCellAddress(participantIndex, column, row));

				if (part != -1)
				{
					if (shown)
					{
						this.Owner.Services.DrawPictureToScreen(Screen.Box, (column * 16) - 16, (row * 16) + 8,
							this.Processor.PullGlobalWord(SpaceshipPictureRules.PictureAddress(part)));
					}

					short breadth = (short)(part >= 6 || part == 3 ? 2 : 1);
					short altitude = (short)(part >= 6 ? 2 : 1);
					short neighbours = (short)(part >= 3 ? 0 : 1);

					for (short across = 0; across < breadth; across++)
					{
						for (short down = 0; down < altitude; down++)
						{
							ushort horizontal = (ushort)(column + across);
							ushort vertical = (ushort)(row + down);

							neighbours += (short)ObtainSpaceshipPartValue(participantIndex, (ushort)(horizontal + 1), vertical);
							neighbours += (short)ObtainSpaceshipPartValue(participantIndex, (ushort)(horizontal - 1), vertical);
							neighbours += (short)ObtainSpaceshipPartValue(participantIndex, horizontal, (ushort)(vertical + 1));
							neighbours += (short)ObtainSpaceshipPartValue(participantIndex, horizontal, (ushort)(vertical - 1));
						}
					}

					if (neighbours != 0)
					{
						AssignSpaceshipPartCount(part, (ushort)(SpaceshipPartCount(part) + 1));
					}
					else if (shown)
					{
						this.Owner.Dialogs.DrawBox((column << 4) - 16, (row << 4) + 8, (breadth << 4) - 1, (altitude << 4) - 1, Hues.LightRed);
					}
				}
				else
				{
					_ = LayoutSlotKind(placeIndex);
				}

				placeIndex++;
			}
			while (LayoutSlotMarker(placeIndex) != SpaceshipSlotEndMarker);
		}

		private (short Success, short FlightTime, short StatusRow) RenderSpaceshipFigures(bool shown, ushort mass)
		{
			short success = 0x64;
			short verticalLocation = 0x10;

			this.Owner.TextPainter.DrawTextToPrimaryScreen(SpaceshipPopulationHeaderAddress, 242, 16, Hues.Black);

			verticalLocation += 8;

			EmptyMessage()
				.Number((short)Spaceship.Colonists)
				.Text("0,000");

			if (shown)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 242, verticalLocation, Hues.Black);
			}

			verticalLocation += 8;

			short colonists = (short)this.Owner.Helpers.ClampValue((short)Spaceship.Colonists, 1, 99);
			short support = CentralProcessor.SignedQuotient((short)(0x64 * Spaceship.Support), colonists);

			Message("Support: ")
				.Number(support)
				.Text("%");

			this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 242, verticalLocation, (ushort)(support < 0x64 ? 4 : 0));

			verticalLocation += 8;

			if (shown)
			{
				success = CentralProcessor.SignedQuotient((short)(this.Owner.Helpers.ClampValue(support, 0, 100) * success), 0x64);
			}

			short modules = (short)this.Owner.Helpers.ClampValue(
				(short)Spaceship.Colonists + (short)Spaceship.Support, 1, 99);
			short energy = CentralProcessor.SignedQuotient((short)(0xc8 * Spaceship.Energy), modules);

			Message("Energy: ")
				.Number(energy)
				.Text("%");

			success = CentralProcessor.SignedQuotient((short)(this.Owner.Helpers.ClampValue(energy, 0, 100) * success), 0x64);

			if (shown)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 242, verticalLocation, (ushort)(energy < 0x64 ? 4 : 0));
			}

			verticalLocation += 0x10;

			if (shown)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(SpaceshipMassHeaderAddress, 242, verticalLocation, Hues.Black);
			}

			verticalLocation += 8;

			EmptyMessage()
				.Number(TenthsRules.Whole((short)mass))
				.Text(",")
				.Number(TenthsRules.Tenth((short)mass))
				.Text("00 tons");

			if (shown)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 242, verticalLocation, Hues.Black);
			}

			verticalLocation += 8;

			short propulsion = (short)this.Owner.Helpers.ClampValue((short)Spaceship.Propulsion, 1, 99);
			short fuel = CentralProcessor.SignedQuotient((short)(0x64 * Spaceship.Fuel), propulsion);

			Message("Fuel: ")
				.Number(fuel)
				.Text("%");

			if (shown)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 242, verticalLocation, (ushort)(fuel < 0x64 ? 4 : 0));
			}

			verticalLocation += 8;

			if (shown)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(SpaceshipFlightTimeHeaderAddress, 242, verticalLocation, Hues.Black);
			}

			verticalLocation += 8;

			short thrust = (short)this.Owner.Helpers.ClampValue(10 * (short)Spaceship.Propulsion, 0,
				10 * (short)Spaceship.Fuel);
			short flightTime = CentralProcessor.SignedQuotient((short)(0x32 * mass), (short)(thrust + 1));

			if (flightTime > 0x96)
			{
				success -= CentralProcessor.SignedQuotient((short)(flightTime - 0x96), 0xa);
			}

			EmptyMessage()
				.Number(TenthsRules.Whole((short)flightTime))
				.Text(".")
				.Number(TenthsRules.Tenth((short)flightTime))
				.Text(" Years");

			if (shown)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 242, verticalLocation, Hues.Black);
			}

			verticalLocation += 0x10;

			success = (short)this.Owner.Helpers.ClampValue(success, 0, 100);

			if (shown)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(SpaceshipSuccessRateHeaderAddress, 242, verticalLocation, Hues.Black);
			}

			verticalLocation += 8;

			Message("--- ")
				.Number(success)
				.Text("% ---");

			if (shown)
			{
				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 242, verticalLocation, Hues.Black);
			}

			return (success, flightTime, (short)(verticalLocation + 0x10));
		}

		private void MoveSpaceshipStars(short participantIndex, short frames, short launchable, short success)
		{
			do
			{
				this.Owner.Startup.HideMouseCursor();

				DrawStarField();

				this.Owner.Startup.ShowMouseCursor();

				this.Owner.TextPainter.WaitForTime((short)((this.Owner.World.StarshipMarkers & (1 << (byte)participantIndex)) != 0 ? 2 : 4));

				this.Owner.Startup.ReadMouseState();

				if (this.Owner.Runtime.KeyAvailable() != 0)
				{
					break;
				}

				frames--;
			}
			while (frames != 0 && this.Owner.MenuPointerState == 0);

			ushort key = 0;

			if (frames != 0 && this.Owner.MenuPointerState == 0)
			{
				key = this.Owner.Dialogs.ReadNavigationKey();
			}

			if (participantIndex != this.Owner.World.PersonParticipantIndex || launchable == 0 || frames >= 0)
			{
				return;
			}

			bool launchClick = this.Owner.MenuPointerState != 0 && (short)this.Owner.MenuPointerHorizontal > 0xf2 && (short)this.Owner.MenuPointerVertical >= 0xbe;

			if (!launchClick && key != 'l')
			{
				return;
			}

			if ((this.Owner.World.StarshipMarkers & (1 << (byte)this.Owner.World.PersonParticipantIndex)) != 0)
			{
				return;
			}

			ushort launch = Message("Confirm spaceship launch.\n(")
				.Number(success)
				.Text("% chance of success)\n No Launch\n Launch CONFIRMED!\n")
				.Dialog(80, 100);

			if (launch == 1)
			{
				FireSpaceship(this.Owner.World.PersonParticipantIndex);
			}
		}

		private bool ShipArrivesFirstSoon(short participantIndex)
		{
			ushort arrival = (ushort)this.Owner.World.Participants[participantIndex].StarshipArrivalSeason;

			if (arrival == 0xffff || (short)(arrival - this.Owner.World.Season) >= 0xf)
			{
				return false;
			}

			ushort humanArrival = (ushort)this.Owner.World.Participants[this.Owner.World.PersonParticipantIndex].StarshipArrivalSeason;

			return (short)arrival < (short)humanArrival || humanArrival == 0xffff;
		}

		private ushort SpaceshipPlaceAddress(short participantIndex, ushort placeIndex)
		{
			return SpaceshipGridCellAddress(participantIndex, LayoutSlotColumn(placeIndex), LayoutSlotRow(placeIndex));
		}

		private byte SpaceshipPlaceCell(short participantIndex, ushort placeIndex)
		{
			return this.Processor.PullByte(SpaceshipGridSegment, SpaceshipPlaceAddress(participantIndex, placeIndex));
		}

		private void BuildSpaceshipPart(short participantIndex, ushort placeIndex)
		{
			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			Message(Tables.CivilizationPlural(participantIndex))
				.Text(" build space ship.\n");

			this.Owner.TextPainter.DrawCenteredTextToPrimaryScreen(this.Owner.Words.ToString(), 160, 96, Hues.White);

			this.Owner.Conquest.WaitForKeyPressOrMouseClick();

			DrawSpaceScenery();

			this.Owner.TurnLogic.WaitForKey();

			byte kind = (byte)LayoutSlotKind(placeIndex);

			this.Processor.EmitByte(SpaceshipGridSegment, SpaceshipPlaceAddress(participantIndex, placeIndex), kind);

			this.Owner.World.ComputerStarshipVictoryRatio = (short)DrawSpaceship(participantIndex, -1, 0);

			this.Owner.World.StarshipMarkers |= (short)(1 << (byte)(participantIndex + 8));

			this.Owner.TurnLogic.WaitForKey();

			LoadSpaceScenery();
		}

		private void LeaveAttachSpaceshipPart()
		{
			this.Owner.Startup.HideMouseCursor();

			LoadSpaceScenery();
		}

		private ushort SpaceshipGridOffset(short participantIndex, ushort slotIndex)
		{
			return SpaceshipPlaceAddress(participantIndex, slotIndex);
		}

		private bool SpaceshipSlotFree(short participantIndex, ushort slotIndex, out sbyte kind)
		{
			bool free = this.Processor.PullByte(SpaceshipGridSegment, SpaceshipGridOffset(participantIndex, slotIndex)) == SpaceshipSlotEmptyMarker;

			kind = LayoutSlotKind(slotIndex);

			return free;
		}

		private ushort FindSpaceshipSlot(short participantIndex, short kind)
		{
			ushort slotIndex = 0;

			do
			{
				if (SpaceshipSlotFree(participantIndex, slotIndex, out sbyte slotKind) && slotKind == kind)
				{
					return slotIndex;
				}

				slotIndex++;
			}
			while (LayoutSlotMarker(slotIndex) != SpaceshipSlotEndMarker);

			return SpaceshipSlotRules.NoSlot;
		}

		private ushort ChooseSpaceshipStructureSlot(short participantIndex)
		{
			ushort innerFree = 0x63, innerBuilt = 0xffff;
			ushort slotIndex;

			for (slotIndex = 0; (short)slotIndex < 0x21; slotIndex++)
			{
				NoteSpaceshipStructureSlot(participantIndex, slotIndex, ref innerFree, ref innerBuilt);
			}

			ushort outerFree = 0x63, outerBuilt = 0xffff;

			slotIndex = 0x21;

			do
			{
				NoteSpaceshipStructureSlot(participantIndex, slotIndex, ref outerFree, ref outerBuilt);

				slotIndex++;
			}
			while (LayoutSlotMarker(slotIndex) != SpaceshipSlotEndMarker);

			ushort chosen = (short)(outerBuilt - outerFree) < (short)(innerBuilt - innerFree) ? innerFree : outerFree;

			return SpaceshipSlotRules.StructureSlot(chosen, chosen == innerFree ? outerFree : innerFree);
		}

		private void NoteSpaceshipStructureSlot(short participantIndex, ushort slotIndex, ref ushort firstFree, ref ushort lastBuilt)
		{
			if (SpaceshipSlotFree(participantIndex, slotIndex, out sbyte slotKind))
			{
				if (slotKind < 3 && firstFree == 0x63)
				{
					firstFree = slotIndex;
				}
			}
			else if (slotKind >= 3)
			{
				lastBuilt = slotIndex;
			}
		}
	}
}
