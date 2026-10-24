using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 10.
	/// </summary>
	public class ArmySelection : GameModule
	{
		private readonly UnitMemory troops;

		public ArmySelection(GameEngine owner)
			: base(owner)
		{
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
				this.owner.Libc.AppendText(0xba06, CivilizationName(participantIdx));

				this.owner.Libc.AppendText(0xba06, "\n ");
			}

			short chosenIdx = (short)(this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80) + 1);

			this.owner.VideoCard.RenderImage(ScreenBox, 0, 0, 0x140, 0xc8,
				BackScreenBox, 0, 0);

			DisplayArmyList(chosenIdx);

			this.owner.VideoCard.RenderImage(BackScreenBox, 0, 0, 0x140, 0xc8,
				ScreenBox, 0, 0);

			this.processor.Journal.CloseScope("DisplayWhichArmy");
		}

		/// <summary>
		/// The strategic map of a player (diagnostics), each step after a key: the tiles it sees (all with 0xd806) as 4 x 4
		/// boxes (sea 1, land 2), cities and the owners of the tiles in their colours; then its strategic locations (S, A, D, N by
		/// the role) and its tactical sites; then its strategy on each continent (S, D, A, T) with defence / attack.
		/// </summary>
		public void DisplayArmyList(short participantIdx)
		{
			this.processor.Journal.OpenScope($"DisplayArmyList({participantIdx})");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, 0);

			ushort sight = (ushort)(1 << (byte)participantIdx);

			for (short vertLoc = 0; vertLoc < 0x32; vertLoc++)
			{
				for (short horizLoc = 0; horizLoc < 0x50; horizLoc++)
				{
					if ((this.owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc] & sight) == 0 && MapRevealed == 0)
						continue;

					RenderStrategicTile(horizLoc, vertLoc);
				}
			}

			this.owner.Libc.ReadKey();

			this.processor.EmitGlobalWord((ushort)(ScreenBox + 0x10), 0x2);

			// The strategic locations; another role keeps the text of the one before
			for (int placeIdx = 0; placeIdx < 0x20; placeIdx++)
			{
				ushort place = (ushort)((participantIdx << 5) + placeIdx);
				byte live = this.processor.PullGlobalByte((ushort)(place + 0xe2c2));

				if (live == 0xff)
					continue;

				switch ((sbyte)live)
				{
					case 0:
						this.owner.Libc.CopyText(0xba06, "S");
						break;

					case 1:
						this.owner.Libc.CopyText(0xba06, "A");
						break;

					case 2:
						this.owner.Libc.CopyText(0xba06, "D");
						break;

					case 3:
						this.owner.Libc.CopyText(0xba06, "N");
						break;
				}

				this.owner.TextPainter.RenderTextWithShadow(0xba06, this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + 0xe598)) << 2,
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + 0xe798)) << 2, 13);
			}

			// The tactical sites; another kind keeps the text of the one before
			for (int siteIdx = 0; siteIdx < 0x10; siteIdx++)
			{
				byte live = (byte)this.owner.World.Participants[participantIdx].TacticalSites[siteIdx].Live;

				if (live == 0xff)
					continue;

				switch ((sbyte)live)
				{
					case 0:
						this.owner.Libc.CopyText(0xba06, 0x417f);
						break;

					case 1:
						this.owner.Libc.CopyText(0xba06, 0x4181);
						break;

					case 2:
						this.owner.Libc.CopyText(0xba06, 0x4183);
						break;
				}

				this.owner.TextPainter.RenderTextWithShadow(0xba06, this.owner.World.Participants[participantIdx].TacticalSites[siteIdx].Place.X << 2,
					this.owner.World.Participants[participantIdx].TacticalSites[siteIdx].Place.Y << 2, 14);
			}

			this.owner.Libc.ReadKey();

			short row = 1;

			for (short continentIdx = 1; continentIdx < 0xf; continentIdx++)
			{
				PlayerContinent continent = this.owner.World.Participants[participantIdx].Landmasses[continentIdx];

				if (continent.Assault == 0)
					continue;

				EmptyMessage()
					.Number(continentIdx)
					.Text("/");

				foreach ((short approach, string letter) in new[] { ((short)0, "S"), ((short)2, "D"), ((short)1, "A"), ((short)5, "T") })
				{
					if (continent.Approach == approach)
					{
						this.owner.Libc.AppendText(0xba06, letter);
					}
				}

				this.owner.Libc.AppendText(0xba06, " ");

				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(continent.Guard, 10));

				this.owner.Libc.AppendText(0xba06, "/");

				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(continent.Assault, 10));

				this.owner.TextPainter.RenderTextToScreen0(0xba06, 2, row, 7);

				row += 6;
			}

			this.owner.Libc.ReadKey();

			this.processor.EmitGlobalWord((ushort)(ScreenBox + 0x10), 0x1);

			this.owner.Startup.DisplayMouseCursor();

			// Far return
			this.processor.Journal.CloseScope("DisplayArmyList");
		}

		/// <summary>A tile of the strategic map: sea or land, then a city in the colour of its owner or the owner of the tile</summary>
		private void RenderStrategicTile(short horizLoc, short vertLoc)
		{
			short boxHoriz = (short)(horizLoc << 2);
			short boxVert = (short)(vertLoc << 2);

			this.owner.Services.FloodBox(ScreenBox, boxHoriz, boxVert, 4, 4,
				(ushort)(this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == 0xa ? 1 : 2));

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				this.owner.Services.FloodBox(ScreenBox, boxHoriz, boxVert, 4, 4,
					PlayerColour(this.owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc)));
			}
			else
			{
				if (this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc) != 0xffff)
				{
					ushort hue = PlayerColour(this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc));

					this.owner.Services.FloodBox(ScreenBox, boxHoriz + 1, boxVert + 1, 3, 3, 0);

					this.owner.Services.FloodBox(ScreenBox, boxHoriz, boxVert, 3, 3, hue);
				}
			}
		}

		/// <summary>
		/// A window about the unit: civilization, number, unit type, home city, position, the target of its "go to" order, then
		/// the types of the other units of its stack (following the ring, while the text is shorter than 512)
		/// </summary>
		public void DisplayUnitInfo(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"DisplayUnitInfo({participantIdx}, {troopIdx})");

			Message(CivilizationName(participantIdx))
				.Text("\n")
				.Number(troopIdx)
				.Text(KindLabel(participantIdx, troopIdx))
				.Text("\n")
				.City(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown))
				.Text("\n")
				.Text("Loc:")
				.Number(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz))
				.Text(",")
				.Number(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert))
				.Text("\n");

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
