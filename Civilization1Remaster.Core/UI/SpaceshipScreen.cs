using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 18.
	/// </summary>
	public class SpaceshipScreen : GameModule
	{
		public SpaceshipScreen(GameEngine owner)
			: base(owner)
		{
		}

		/// <summary>
		/// The space background: a black screen with the picture 0x4bc4, its 10 parts cut out (rectangles of 13 words at 0x4be2,
		/// the pictures go to 0x682a), 20 stars at random places (0x683e, 0x6852, 0x6866), the colour cycles 1 - 5 set up and,
		/// when 0xd762 is set, started
		/// </summary>
		public void RenderSpaceScenery()
		{
			this.processor.Journal.OpenScope("RenderSpaceScenery()");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Helpers.HaltHueCycling();

			this.owner.Startup.QueryForDisk(1, 1);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(1, 0, 0, 0x4bc4, 1);

			for (int partIdx = 0; partIdx < 0xa; partIdx++)
			{
				ushort part = (ushort)(0x1a * partIdx);

				ushort picture = this.owner.VideoCard.ScreenToPicture(1, this.processor.PullGlobalWord((ushort)(part + 0x4be2)),
					this.processor.PullGlobalWord((ushort)(part + 0x4be4)), this.processor.PullGlobalWord((ushort)(part + 0x4be6)),
					this.processor.PullGlobalWord((ushort)(part + 0x4be8)));

				this.processor.EmitGlobalWord((ushort)((partIdx << 1) + 0x682a), picture);
			}

			for (int starIdx = 0; starIdx < 0x14; starIdx++)
			{
				this.processor.EmitGlobalByte((ushort)(starIdx + 0x683e), (byte)this.owner.Libc.Dice.DrawInt(240));
				this.processor.EmitGlobalByte((ushort)(starIdx + 0x6852), (byte)(this.owner.Libc.Dice.DrawInt(192) + 8));
				this.processor.EmitGlobalByte((ushort)(starIdx + 0x6866), 0x0);
			}

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Services.AssignHueCycleSpan(1, 30, 208, 223);
			this.owner.Services.AssignHueCycleSpan(2, 11, 192, 207);
			this.owner.Services.AssignHueCycleSpan(3, 20, 224, 239);
			this.owner.Services.AssignHueCycleSpan(4, 8, 144, 159);
			this.owner.Services.AssignHueCycleSpan(5, 150, 128, 129);

			if (this.processor.PullGlobalWord(0xd762) != 0)
			{
				for (ushort cycleIdx = 1; cycleIdx <= 5; cycleIdx++)
				{
					this.owner.Services.ActivateHueCycle(cycleIdx);
				}
			}

			this.processor.Journal.CloseScope("RenderSpaceScenery");
		}

		/// <summary>
		/// Leaves the spaceship screen: frees its picture (0x682a), stops the colour cycles 1 - 5 when 0xd762 is set, loads the
		/// palette of the human player's government, "backNa.pal" (N is the government / 2, "m" instead of "a" with technology
		/// 20), and the palette 0x4cea, clears the screen and draws the map screen again. The file name is made in a buffer in
		/// the stack frame, like in the original.
		/// </summary>
		public void ImportSpaceScenery()
		{
			this.processor.Journal.OpenScope("ImportSpaceScenery()");

			this.processor.EnterFrame(0x12);

			this.owner.Helpers.ReleaseResource(this.processor.PullGlobalWord(0x682a), 0x4cda);

			if (this.processor.PullGlobalWord(0xd762) != 0)
			{
				for (ushort cycleIdx = 1; cycleIdx <= 5; cycleIdx++)
				{
					this.owner.Services.DeactivateHueCycle(cycleIdx);
				}
			}

			this.owner.Startup.QueryForDisk(0, 1);

			ushort docname = (ushort)(this.processor.RegFrame.Value16 - 0x10);
			this.owner.Libc.CopyText(docname, "back0a.pal");

			short regime = this.owner.World.Participants[this.owner.World.PersonParticipantIdx].RegimeKind;
			this.processor.EmitGlobalByte((ushort)(docname + 4), (byte)((regime / 2) + 0x30));

			if (this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x14) != 0)
			{
				this.processor.EmitGlobalByte((ushort)(docname + 5), 0x6d);
			}

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, docname, 0xc1d6);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x4cea, 0xc5be);

			this.owner.Libc.CopyBytes(0xc744, 0xc35c, 0x180);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.VideoCard.AssignHuesFromHueStruct(0xc5be);

			this.owner.Helpers.SetupHueCycling();

			this.owner.TurnHooks.RepaintMainScreen();

			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("ImportSpaceScenery");
		}

		/// <summary>
		/// The spaceship of the player: its parts on the layout (segment 0x3772, 0x16e0 + 0xb4 * player; places in the table
		/// 0x375a), counted per kind in 0xdea2 when connected to a neighbour (structures always), the mass (part masses 0x4be0),
		/// the colonists (0xdeae), support, energy, fuel, flight time and the chance of success; the arrival year of a ship
		/// not launched yet. Shown (with a key awaited and the stars moving until a key, a click or arg2 frames) for the human
		/// player, or for all with the spaceship option (0x20); the human player may launch it ("l" or the button) when arg3
		/// and arg2 &lt; 0.
		/// </summary>
		/// <returns>The chance of success in percent, also in AX</returns>
		public ushort RenderSpaceship(short participantIdx, short arg2, short arg3)
		{
			this.processor.Journal.OpenScope($"RenderSpaceship({participantIdx}, {arg2}, {arg3})");

			// The frame keeps the key of the star animation (bp - 0x16) like the original: without a key (a click) the old
			// stack value is read
			this.processor.EnterFrame(0x20);
			this.processor.PreserveIndexRegisters();

			this.owner.Startup.ConcealMouseCursor();

			bool shown = participantIdx == this.owner.World.PersonParticipantIdx || (this.owner.World.SessionPrefMarkers & 0x20) != 0;

			if (shown)
			{
				RenderSpaceshipTitle(participantIdx);
			}

			CountSpaceshipParts(participantIdx, shown);

			ushort mass = 0;

			for (int kind = 0; kind < 0xa; kind++)
			{
				mass += (ushort)(this.processor.PullGlobalWord((ushort)((0x1a * kind) + 0x4be0)) *
					this.processor.PullGlobalWord((ushort)((kind << 1) + 0xdea2)));
			}

			if ((short)mass >= 0x50 && shown)
			{
				this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), 152, 88, this.processor.PullGlobalWord(0x6834));
			}

			if (shown)
			{
				ushort colour = (ushort)((participantIdx << 1) + 0x1946);

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 240, 8, 80, 192, this.processor.PullGlobalWord(colour));

				this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 0, 8, 0, 199, this.processor.PullGlobalWord(colour));

				this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa), 0, 199, 240, 199, this.processor.PullGlobalWord(colour));
			}

			(short success, short flightTime, short vertLoc) = RenderSpaceshipFigures(shown, mass);

			if (this.processor.PullGlobalWord(0xdeae) == 0)
			{
				arg3 = 0;
			}

			if ((this.owner.World.StarshipMarkers & (1 << (byte)participantIdx)) != 0)
			{
				if (shown)
				{
					Player player = this.owner.World.Participants[participantIdx];

					this.owner.TextPainter.RenderTextToScreen0((ushort)(player.StarshipArrivalSeason < this.owner.World.Season ? 0x4d7c : 0x4d84),
						242, vertLoc, 0);
				}
			}
			else
			{
				this.owner.World.Participants[participantIdx].StarshipArrivalSeason =
					(short)(CPU.SignedQuotient(flightTime, 0xa) + this.owner.World.Season);

				if (participantIdx == this.owner.World.PersonParticipantIdx && arg2 < 0 && arg3 != 0)
				{
					this.owner.TextPainter.RenderTextToScreen0(0x4d8e, 256, 190, 0);

					this.owner.Dialogs.RenderBox(242, 188, 76, 9, 0);
				}
			}

			if (shown)
			{
				this.owner.TurnLogic.AwaitForKey();
			}

			this.owner.Startup.DisplayMouseCursor();

			if (shown)
			{
				MoveSpaceshipStars(participantIdx, arg2, arg3, success);
			}

			this.owner.World.Participants[participantIdx].StarshipInhabitants = (short)this.processor.PullGlobalWord(0xdeae);

			this.processor.RegAcc.Value16 = (ushort)success;

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("RenderSpaceship");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// The title: "civilization SpaceShip: X.S.S. leader" (X = the first letter of the civilization) on a band of the
		/// player's colour
		/// </summary>
		private void RenderSpaceshipTitle(short participantIdx)
		{
			ushort names = (ushort)(participantIdx << 1);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 8, this.processor.PullGlobalWord((ushort)(names + 0x1946)));

			Message(this.processor.PullGlobalWord((ushort)(names + 0x1982)))
				.Text(" SpaceShip: X.S.S. ");

			ushort length = this.owner.Libc.TextLength(0xba06);

			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)(names + 0x19a2)));

			// The "X" of "X.S.S." becomes the first letter of the civilization
			this.processor.EmitGlobalByte((ushort)(length + 0xb9ff),
				this.processor.PullGlobalByte(this.processor.PullGlobalWord((ushort)(names + 0x1982))));

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 1, 0);
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
				this.processor.EmitGlobalWord((ushort)((kind << 1) + 0xdea2), 0x0);
			}

			int placeIdx = 0;

			do
			{
				ushort place = (ushort)(3 * placeIdx);
				short column = (sbyte)this.processor.PullUInt8(0x375a, place);
				short row = (sbyte)this.processor.PullUInt8(0x375a, (ushort)(place + 1));
				short part = (sbyte)this.processor.PullUInt8(0x3772, (ushort)((0xb4 * participantIdx) + (0xc * column) + row + 0x16e0));

				if (part != -1)
				{
					if (shown)
					{
						// The original computes the picture address as part << 1 + 0x682a, which C# reads as part << 0x682b,
						// a shift by 11 (the count is masked to 5 bits)
						this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), (column * 16) - 16, (row * 16) + 8,
							this.processor.PullGlobalWord((ushort)((ushort)part << 0xb)));
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
						ushort tally = (ushort)((part << 1) + 0xdea2);

						this.processor.EmitGlobalWord(tally, (ushort)(this.processor.PullGlobalWord(tally) + 1));
					}
					else if (shown)
					{
						this.owner.Dialogs.RenderBox((column << 4) - 16, (row << 4) + 8, (breadth << 4) - 1, (altitude << 4) - 1, 12);
					}
				}
				else
				{
					// The original reads the kind of the empty place here (and forgets it)
					this.processor.PullUInt8(0x375a, (ushort)(place + 2));
				}

				placeIdx++;
			}
			while (this.processor.PullUInt8(0x375a, (ushort)(3 * placeIdx)) != 0xff);
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

			this.owner.TextPainter.RenderTextToScreen0(0x4d08, 242, 16, 0);

			vertLoc += 8;

			EmptyMessage()
				.Number((short)this.processor.PullGlobalWord(0xdeae))
				.Text("0,000");

			if (shown)
			{
				this.owner.TextPainter.RenderTextToScreen0(0xba06, 242, vertLoc, 0);
			}

			vertLoc += 8;

			// Support: life support per colonist module
			short colonists = (short)this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0xdeae), 1, 99);
			short support = CPU.SignedQuotient((short)(0x64 * this.processor.PullGlobalWord(0xdeb0)), colonists);

			Message("Support: ")
				.Number(support)
				.Text("%");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 242, vertLoc, (ushort)(support < 0x64 ? 4 : 0));

			vertLoc += 8;

			if (shown)
			{
				success = CPU.SignedQuotient((short)(this.owner.Helpers.VerifyValueSpan(support, 0, 100) * success), 0x64);
			}

			// Energy: power per support and colonist module
			short modules = (short)this.owner.Helpers.VerifyValueSpan(
				this.processor.PullGlobalSignedWord(0xdeae) + this.processor.PullGlobalSignedWord(0xdeb0), 1, 99);
			short energy = CPU.SignedQuotient((short)(0xc8 * this.processor.PullGlobalWord(0xdeb2)), modules);

			Message("Energy: ")
				.Number(energy)
				.Text("%");

			success = CPU.SignedQuotient((short)(this.owner.Helpers.VerifyValueSpan(energy, 0, 100) * success), 0x64);

			if (shown)
			{
				this.owner.TextPainter.RenderTextToScreen0(0xba06, 242, vertLoc, (ushort)(energy < 0x64 ? 4 : 0));
			}

			vertLoc += 0x10;

			if (shown)
			{
				this.owner.TextPainter.RenderTextToScreen0(0x4d31, 242, vertLoc, 0);
			}

			vertLoc += 8;

			// Mass: the original writes mass / 10 twice ("x,x00 tons")
			EmptyMessage()
				.Number((short)((short)mass / 10))
				.Text(",")
				.Number((short)((short)mass / 10))
				.Text("00 tons");

			if (shown)
			{
				this.owner.TextPainter.RenderTextToScreen0(0xba06, 242, vertLoc, 0);
			}

			vertLoc += 8;

			// Fuel: fuel per propulsion
			short propulsion = (short)this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0xdea8), 1, 99);
			short fuel = CPU.SignedQuotient((short)(0x64 * this.processor.PullGlobalWord(0xdeaa)), propulsion);

			Message("Fuel: ")
				.Number(fuel)
				.Text("%");

			if (shown)
			{
				this.owner.TextPainter.RenderTextToScreen0(0xba06, 242, vertLoc, (ushort)(fuel < 0x64 ? 4 : 0));
			}

			vertLoc += 8;

			if (shown)
			{
				this.owner.TextPainter.RenderTextToScreen0(0x4d4a, 242, vertLoc, 0);
			}

			vertLoc += 8;

			// Flight time (tenths of years): 50 * mass / (thrust + 1), over 15 years less success
			short thrust = (short)this.owner.Helpers.VerifyValueSpan(10 * this.processor.PullGlobalSignedWord(0xdea8), 0,
				10 * this.processor.PullGlobalSignedWord(0xdeaa));
			short flightTime = CPU.SignedQuotient((short)(0x32 * mass), (short)(thrust + 1));

			if (flightTime > 0x96)
			{
				success -= CPU.SignedQuotient((short)(flightTime - 0x96), 0xa);
			}

			// The original writes the time / 10 twice ("x.x Years")
			EmptyMessage()
				.Number((short)(flightTime / 10))
				.Text(".")
				.Number((short)(flightTime / 10))
				.Text(" Years");

			if (shown)
			{
				this.owner.TextPainter.RenderTextToScreen0(0xba06, 242, vertLoc, 0);
			}

			vertLoc += 0x10;

			success = (short)this.owner.Helpers.VerifyValueSpan(success, 0, 100);

			if (shown)
			{
				this.owner.TextPainter.RenderTextToScreen0(0x4d60, 242, vertLoc, 0);
			}

			vertLoc += 8;

			Message("--- ")
				.Number(success)
				.Text("% ---");

			if (shown)
			{
				this.owner.TextPainter.RenderTextToScreen0(0xba06, 242, vertLoc, 0);
			}

			return (success, flightTime, (short)(vertLoc + 0x10));
		}

		/// <summary>
		/// The stars move (every 2 beats after the launch, else 4) until a key, a click or arg2 frames; the key is taken. The
		/// human player launches a ship not launched yet with "l" or a click on the launch button (when arg3 and arg2 &lt; 0),
		/// after confirming it.
		/// </summary>
		private void MoveSpaceshipStars(short participantIdx, short arg2, short arg3, short success)
		{
			do
			{
				this.owner.Startup.ConcealMouseCursor();

				RenderStarField();

				this.owner.Startup.DisplayMouseCursor();

				this.owner.TextPainter.AwaitTime((short)((this.owner.World.StarshipMarkers & (1 << (byte)participantIdx)) != 0 ? 2 : 4));

				this.owner.Startup.ObtainMouseState();

				if (this.owner.Libc.KeyAvailable() != 0)
					break;

				arg2--;
			}
			while (arg2 != 0 && this.owner.GlobDb3a == 0);

			if (arg2 != 0 && this.owner.GlobDb3a == 0)
			{
				this.processor.EmitLocalWord(0x16, this.owner.Dialogs.ObtainNavigationKey());
			}

			if (participantIdx != this.owner.World.PersonParticipantIdx || arg3 == 0 || arg2 >= 0)
				return;

			bool launchClick = this.owner.GlobDb3a != 0 && (short)this.owner.GlobDb3c > 0xf2 && (short)this.owner.GlobDb3e >= 0xbe;

			if (!launchClick && this.processor.PullLocalWord(0x16) != 0x6c)
				return;

			if ((this.owner.World.StarshipMarkers & (1 << (byte)this.owner.World.PersonParticipantIdx)) != 0)
				return;

			Message("Confirm spaceship launch.\n(")
				.Number(success)
				.Text("% chance of success)\n No Launch\n Launch CONFIRMED!\n")
				.Dialog(80, 100);

			if (this.processor.RegAcc.Value16 == 1)
			{
				FireSpaceship(this.owner.World.PersonParticipantIdx);
			}
		}

		/// <summary>
		/// Is the place of the spaceship of the player taken by a part: the byte of the spaceship layout (segment 0x3772,
		/// 0x16e0 + 0xb4 * player + 0xc * row + column) is not empty (0xff) and below 3
		/// </summary>
		/// <returns>1 or 0, also in AX</returns>
		public ushort ObtainSpaceshipPartValue(short participantIdx, ushort arg2, ushort arg3)
		{
			this.processor.Journal.OpenScope($"ObtainSpaceshipPartValue({participantIdx}, {arg2}, {arg3})");

			this.processor.SegExtra.Value16 = 0x3772;

			sbyte part = (sbyte)this.processor.PullUInt8(this.processor.SegExtra.Value16,
				(ushort)((0xb4 * participantIdx) + (0xc * arg2) + arg3 + 0x16e0));

			ushort taken = (ushort)(part != -1 && part < 3 ? 1 : 0);

			this.processor.RegAcc.Value16 = taken;

			this.processor.Journal.CloseScope("ObtainSpaceshipPartValue");

			return taken;
		}

		/// <summary>
		/// Moves the 20 stars of the space background one step to the right (0x683e: column, 0x6852: row, 0x6866: drawn):
		/// every 4th star 2 pixels (colour 7), the next 1 pixel (colour 3), the others 1 pixel every second frame (colour 8,
		/// frame counter 0x687a); a star past column 239 starts again at column 1 on a random row. A star is drawn only on
		/// black. The positions are read as signed bytes, like in the original.
		/// </summary>
		public void RenderStarField()
		{
			this.processor.Journal.OpenScope("RenderStarField()");

			for (int starIdx = 0; starIdx < 0x14; starIdx++)
			{
				ushort column = (ushort)(starIdx + 0x683e), row = (ushort)(starIdx + 0x6852), drawn = (ushort)(starIdx + 0x6866);

				if (this.processor.PullGlobalByte(drawn) != 0)
				{
					this.owner.Services.AssignDot((sbyte)this.processor.PullGlobalByte(column), (sbyte)this.processor.PullGlobalByte(row), 0);
				}

				ushort hue;

				switch (starIdx & 3)
				{
					case 0:
						this.processor.EmitGlobalByte(column, (byte)(this.processor.PullGlobalByte(column) + 2));
						hue = 7;
						break;

					case 1:
						this.processor.EmitGlobalByte(column, (byte)(this.processor.PullGlobalByte(column) + 1));
						hue = 3;
						break;

					default:
						if ((this.processor.PullGlobalByte(0x687a) & 0x1) != 0)
						{
							this.processor.EmitGlobalByte(column, (byte)(this.processor.PullGlobalByte(column) + 1));
						}

						hue = 8;
						break;
				}

				if (this.processor.PullGlobalByte(column) >= 0xf0)
				{
					this.processor.EmitGlobalByte(column, 0x1);
					this.processor.EmitGlobalByte(row, (byte)(this.owner.Libc.Dice.DrawInt(192) + 8));
					this.processor.EmitGlobalByte(drawn, 0x0);
				}

				if (this.owner.VideoCard.ObtainDot(0, (sbyte)this.processor.PullGlobalByte(column), (sbyte)this.processor.PullGlobalByte(row)) == 0)
				{
					this.processor.EmitGlobalByte(drawn, 0x1);

					this.owner.Services.AssignDot((sbyte)this.processor.PullGlobalByte(column), (sbyte)this.processor.PullGlobalByte(row), hue);
				}
				else
				{
					this.processor.EmitGlobalByte(drawn, 0x0);
				}
			}

			this.processor.EmitGlobalWord(0x687a, (ushort)(this.processor.PullGlobalWord(0x687a) + 1));

			this.processor.Journal.CloseScope("RenderStarField");
		}

		/// <summary>
		/// The AI decides about a spaceship part of the kind arg2 (0 structure, 1 component, 2 module): the places of the table
		/// 0x375a are searched from the first one (from 0x21 for modules, or when the player's ship arrives within 15 years and
		/// before the human player's) for an empty place of a part of this kind; the first empty place that would take a part
		/// of the kind is remembered, and the search gives up 4 places after it (not for modules) when the player knows the
		/// technology of the part (0xe68 + 0x1e * kind). An empty place of this kind is built (shown with the space scenery,
		/// the ship redrawn, its chance of success in BotStarshipVictoryRatio, the ship bit 8 + player set).
		/// </summary>
		/// <returns>0 when a part was built, 1 when not (none found but a later place exists), 2 when no place at all; also in AX</returns>
		public ushort ComputeSpaceshipSuccess(short participantIdx, short arg2)
		{
			this.processor.Journal.OpenScope($"ComputeSpaceshipSuccess({participantIdx}, {arg2})");

			// The frame like the original (the functions called see the stack at the same depth)
			this.processor.EnterFrame(0x6);
			this.processor.PreserveIndexRegisters();

			this.owner.World.BotStarshipVictoryRatio = 0;

			ushort placeIdx = (ushort)(arg2 == 2 || ShipArrivesFirstSoon(participantIdx) ? 0x21 : 0);
			ushort firstFree = 0x3e7;
			bool found = false;

			while (true)
			{
				if (SpaceshipPlaceCell(participantIdx, placeIdx) == 0xff)
				{
					ushort place = (ushort)(3 * placeIdx);

					if (firstFree == 0x3e7 && (sbyte)this.processor.PullUInt8(0x375a, (ushort)(place + 2)) >= (short)((3 * arg2) + 3))
					{
						firstFree = placeIdx;
					}

					if ((sbyte)this.processor.PullUInt8(0x375a, (ushort)(place + 2)) / 3 == arg2)
					{
						found = true;
						break;
					}
				}

				placeIdx++;

				if (this.processor.PullUInt8(0x375a, (ushort)(3 * placeIdx)) == 0xff)
					break;

				if ((short)(placeIdx - firstFree) < 4 || arg2 == 2)
					continue;

				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx,
					(short)this.processor.PullGlobalWord((ushort)((0x1e * arg2) + 0xe68))) != 0)
					break;
			}

			ushort result;
			bool atEnd = this.processor.PullUInt8(0x375a, (ushort)(3 * placeIdx)) == 0xff;

			if (atEnd && firstFree == 0x3e7)
			{
				result = 2;
			}
			else if (!found)
			{
				result = 1;
			}
			else if (this.processor.PullUInt8(0x375a, (ushort)(3 * placeIdx)) == 0xff && firstFree != 0x3e7)
			{
				result = 1;
			}
			else
			{
				BuildSpaceshipPart(participantIdx, placeIdx);

				result = 0;
			}

			this.processor.RegAcc.Value16 = result;

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();

			this.processor.Journal.CloseScope("ComputeSpaceshipSuccess");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// The player's ship arrives within 15 years, and before the human player's (or the human player has none)
		/// </summary>
		private bool ShipArrivesFirstSoon(short participantIdx)
		{
			ushort arrival = (ushort)this.owner.World.Participants[participantIdx].StarshipArrivalSeason;

			if (arrival == 0xffff || (short)(arrival - this.owner.World.Season) >= 0xf)
				return false;

			ushort humanArrival = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].StarshipArrivalSeason;

			return (short)arrival < (short)humanArrival || humanArrival == 0xffff;
		}

		/// <summary>The address of the cell of the place (table 0x375a: column, row) in the player's spaceship (0x3772:0x16e0)</summary>
		private ushort SpaceshipPlaceAddress(short participantIdx, ushort placeIdx)
		{
			ushort place = (ushort)(3 * placeIdx);
			short column = (sbyte)this.processor.PullUInt8(0x375a, place);
			short row = (sbyte)this.processor.PullUInt8(0x375a, (ushort)(place + 1));

			return (ushort)((0xc * column) + row + (0xb4 * participantIdx) + 0x16e0);
		}

		/// <summary>The cell of the place in the player's spaceship (0xff: empty)</summary>
		private byte SpaceshipPlaceCell(short participantIdx, ushort placeIdx)
		{
			return this.processor.PullUInt8(0x3772, SpaceshipPlaceAddress(participantIdx, placeIdx));
		}

		/// <summary>
		/// The AI builds a part at the place: the news on the space scenery, the part of the place's kind, the ship redrawn
		/// (its chance of success into BotStarshipVictoryRatio), the ship bit (8 + player)
		/// </summary>
		private void BuildSpaceshipPart(short participantIdx, ushort placeIdx)
		{
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			Message(this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1992)))
				.Text(" build space ship.\n");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 96, 15);

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			RenderSpaceScenery();

			this.owner.TurnLogic.AwaitForKey();

			byte kind = this.processor.PullUInt8(0x375a, (ushort)((3 * placeIdx) + 2));

			this.processor.EmitUInt8(0x3772, SpaceshipPlaceAddress(participantIdx, placeIdx), kind);

			this.owner.World.BotStarshipVictoryRatio = (short)RenderSpaceship(participantIdx, -1, 0);

			this.owner.World.StarshipMarkers |= (short)(1 << (byte)(participantIdx + 8));

			this.owner.TurnLogic.AwaitForKey();

			ImportSpaceScenery();
		}

		/// <summary>
		/// A new part of the player's spaceship: arg2 0 a structure, 1 a component (dialog 0x4df9: the kind 0 or 1 becomes 3 or 4,
		/// unless all 3 are there, 0x687c), 2 a module (dialog 0x4e12: kind 6 - 8, unless all 7 are there, 0x687e); a cancelled
		/// dialog leaves without a part. The part goes into a free slot of its kind in the grid 0x3772:0x16e0 + 0xb4 * player
		/// (slots: table 0x375a of row, column and kind, ending with row 0xff); a structure into the first free one of the inner
		/// 0x21 slots or of the outer ones, whichever side has the shorter span to the last built part. The first spaceship
		/// part of the player (StarshipMarkers bit 8 + player) clears TouchParticipantTimeleft of every player 1 - 7 with one.
		/// </summary>
		public void AttachSpaceshipPart(short participantIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"AttachSpaceshipPart({participantIdx}, {arg2})");

			// The final values of the locals stay in the stack frame, like in the original
			this.processor.EnterFrame(0x16);
			this.processor.PreserveSource();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			RenderSpaceScenery();

			RenderSpaceship(participantIdx, 15, 0);

			this.owner.TurnLogic.AwaitForKey();

			bool attach = false;

			while (true)
			{
				short kind = 2;

				this.processor.EmitLocalWord(0x16, (ushort)kind);

				this.owner.Startup.DisplayMouseCursor();

				if (arg2 == 1)
				{
					ushort complete = this.processor.PullGlobalWord(0x687c);
					this.processor.EmitGlobalWord(0xb276, complete);

					if (complete == 3)
						break;

					this.owner.TurnHooks.DisplayDialog(0x4df9, 100, 80);

					kind = (short)(this.processor.RegAcc.Value16 + 3);
					this.processor.EmitLocalWord(0x16, (ushort)kind);

					if (kind == 2)
					{
						LeaveAttachSpaceshipPart();
						return;
					}
				}

				if (arg2 == 2)
				{
					ushort complete = this.processor.PullGlobalWord(0x687e);
					this.processor.EmitGlobalWord(0xb276, complete);

					if (complete == 7)
						break;

					this.owner.TurnHooks.DisplayDialog(0x4e12, 100, 80);

					kind = (short)(this.processor.RegAcc.Value16 + 6);
					this.processor.EmitLocalWord(0x16, (ushort)kind);

					if (kind == 5)
					{
						LeaveAttachSpaceshipPart();
						return;
					}
				}

				this.owner.Startup.ConcealMouseCursor();

				ushort slotIdx = arg2 == 0 ? ChooseSpaceshipStructureSlot(participantIdx) : FindSpaceshipSlot(participantIdx, kind);

				this.processor.EmitLocalWord(0xc, slotIdx);

				if (slotIdx != 0xffff)
				{
					this.processor.EmitUInt8(0x3772, SpaceshipGridOffset(participantIdx, slotIdx), this.processor.PullUInt8(0x375a, (ushort)((3 * slotIdx) + 2)));

					attach = true;
					break;
				}

				if (arg2 == 0)
				{
					attach = true;
					break;
				}

				// Never reached: a full kind ends at the end of the table, not at 0xffff (see the backlog)
				this.processor.EmitGlobalWord((ushort)((arg2 << 1) + 0x687a), (ushort)(this.processor.PullGlobalWord((ushort)((arg2 << 1) + 0x687a)) | (1 << (byte)kind)));

				Message("No further ")
					.Text((ushort)((((3 * arg2) + kind) * 0x1a) + 0x4bd0))
					.Text(" required.\n")
					.Dialog(120, 80);
			}

			if (attach && (this.owner.World.StarshipMarkers & (1 << (byte)(participantIdx + 8))) == 0)
			{
				short rivalIdx;

				for (rivalIdx = 1; rivalIdx < 8; rivalIdx++)
				{
					if ((this.owner.World.StarshipMarkers & (1 << (rivalIdx + 8))) != 0)
					{
						this.owner.World.Participants[rivalIdx].TouchParticipantTimeleft = 0;
					}
				}

				this.processor.EmitLocalWord(0x4, (ushort)rivalIdx);

				this.owner.World.StarshipMarkers |= (short)(ushort)(1 << (byte)(participantIdx + 8));
			}

			this.owner.Startup.DisplayMouseCursor();

			RenderSpaceship(participantIdx, -1, 1);

			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].StarshipVictoryRatio = (short)this.processor.RegAcc.Value16;

			LeaveAttachSpaceshipPart();
		}

		private void LeaveAttachSpaceshipPart()
		{
			this.owner.Startup.ConcealMouseCursor();

			ImportSpaceScenery();

			this.processor.RestoreSource();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("AttachSpaceshipPart");
		}

		/// <summary>The offset of the slot slotIdx (table 0x375a) in the player's spaceship grid, segment 0x3772</summary>
		private ushort SpaceshipGridOffset(short participantIdx, ushort slotIdx)
		{
			ushort slot = (ushort)(3 * slotIdx);

			return (ushort)((12 * (sbyte)this.processor.PullUInt8(0x375a, slot)) + (sbyte)this.processor.PullUInt8(0x375a, (ushort)(slot + 1)) + (0xb4 * participantIdx) + 0x16e0);
		}

		/// <summary>Whether the slot is free (0xff) and the kind of the slot of table 0x375a</summary>
		private bool SpaceshipSlotFree(short participantIdx, ushort slotIdx, out sbyte kind)
		{
			bool free = this.processor.PullUInt8(0x3772, SpaceshipGridOffset(participantIdx, slotIdx)) == 0xff;

			kind = (sbyte)this.processor.PullUInt8(0x375a, (ushort)((3 * slotIdx) + 2));

			return free;
		}

		/// <summary>The first free slot of the kind, else the end of table 0x375a</summary>
		private ushort FindSpaceshipSlot(short participantIdx, short kind)
		{
			ushort slotIdx = 0;

			do
			{
				if (SpaceshipSlotFree(participantIdx, slotIdx, out sbyte slotKind) && slotKind == kind)
					break;

				slotIdx++;
			}
			while (this.processor.PullUInt8(0x375a, (ushort)(3 * slotIdx)) != 0xff);

			return slotIdx;
		}

		/// <summary>
		/// The slot of a new structure (kinds 0 - 2): the first free one of the inner 0x21 slots or of the outer ones (else 0x63),
		/// on the side where it is closer to the last built part (kinds 3 and more)
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
			while (this.processor.PullUInt8(0x375a, (ushort)(3 * slotIdx)) != 0xff);

			this.processor.EmitLocalWord(0x2, outerFree);
			this.processor.EmitLocalWord(0x6, innerBuilt);
			this.processor.EmitLocalWord(0x8, outerBuilt);
			this.processor.EmitLocalWord(0x14, innerFree);

			return (short)(outerBuilt - outerFree) < (short)(innerBuilt - innerFree) ? innerFree : outerFree;
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

		/// <summary>
		/// When some player 1 - 7 has a spaceship (StarshipMarkers bits 9 - 15), asks which nation's spaceship to show (the
		/// names at 0x1982 + 2 * player; 0xb276 masks the ones without a spaceship) and shows it
		/// </summary>
		public void ChooseNationToView()
		{
			this.processor.Journal.OpenScope("ChooseNationToView()");

			if ((this.owner.World.StarshipMarkers & 0xfe00) != 0)
			{
				this.owner.Libc.CopyText(0xba06, "Which nation?\n ");

				for (int participantIdx = 1; participantIdx < 8; participantIdx++)
				{
					this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)));

					this.owner.Libc.AppendText(0xba06, "\n ");
				}

				this.processor.EmitGlobalWord(0xb276, (ushort)((ushort)(this.owner.World.StarshipMarkers >> 9) ^ 0xff));

				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

				short chosenIdx = (short)(this.processor.RegAcc.Value16 + 1);

				if (chosenIdx > 0)
				{
					RenderSpaceScenery();

					RenderSpaceship(chosenIdx, -1, 1);

					ImportSpaceScenery();
				}
			}

			this.processor.Journal.CloseScope("ChooseNationToView");
		}

		/// <summary>
		/// The player launches its spaceship (StarshipMarkers bit player): a black screen with "... LAUNCH space ship!" and the
		/// estimated arrival, the launch year is noted, then the spaceship is shown (on the space background, which is left
		/// again afterwards, for another player than the human)
		/// </summary>
		public void FireSpaceship(short participantIdx)
		{
			this.processor.Journal.OpenScope($"FireSpaceship({participantIdx})");

			this.owner.World.StarshipMarkers |= (short)(1 << participantIdx);

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.TurnLogic.AwaitForKey();

			Message(this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1992)))
				.Text(" LAUNCH space ship!");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 92, 15);

			Message("Estimate arrival in ")
				.Number(this.owner.World.Participants[participantIdx].StarshipArrivalSeason)
				.Text(".\n");

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 160, 100, 15);

			this.owner.World.Participants[participantIdx].StarshipLiftoffSeason = this.owner.World.Season;

			if (participantIdx != this.owner.World.PersonParticipantIdx)
			{
				RenderSpaceScenery();
			}

			this.owner.TurnLogic.AwaitForKey();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			RenderSpaceship(participantIdx, -1, 0);

			this.owner.Startup.DisplayMouseCursor();

			if (participantIdx != this.owner.World.PersonParticipantIdx)
			{
				ImportSpaceScenery();
			}

			this.processor.Journal.CloseScope("FireSpaceship");
		}
	}
}
