using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;

namespace Civilization1Remaster.UI
{
	/// <summary>
	/// Translated from the original CIV.EXE overlay 10.
	/// </summary>
	public class ArmySelection : GameModule
	{
		public const ushort StrategicLocationRoleTableAddress = 0xe2c2;
		public const ushort StrategicLocationHorizTableAddress = 0xe598;
		public const ushort StrategicLocationVertTableAddress = 0xe798;
		public const ushort StrategicLocationPriorityTableAddress = 0xb2be;

		private const ushort TacticalSiteRoleFortAddress = 0x417f;
		private const ushort TacticalSiteRoleCityAddress = 0x4181;
		private const ushort TacticalSiteRolePassAddress = 0x4183;

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

			this.owner.Words.Set("Which army?\n ");

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				this.owner.Words.Append(CivilizationName(participantIdx));

				this.owner.Words.Append("\n ");
			}

			short chosenIdx = (short)(this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 100, 80) + 1);

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

			this.owner.Services.FloodBox(ScreenBox, 0, 0, 320, 200, Hues.Black);

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

			DrawingBox.TypefaceIdx = 0x2;

			// The strategic locations; another role keeps the text of the one before
			for (int placeIdx = 0; placeIdx < 0x20; placeIdx++)
			{
				ushort place = (ushort)((participantIdx << 5) + placeIdx);
				byte live = this.processor.PullGlobalByte((ushort)(place + StrategicLocationRoleTableAddress));

				if (live == 0xff)
					continue;

				switch ((sbyte)live)
				{
					case 0:
						this.owner.Words.Set("S");
						break;

					case 1:
						this.owner.Words.Set("A");
						break;

					case 2:
						this.owner.Words.Set("D");
						break;

					case 3:
						this.owner.Words.Set("N");
						break;
				}

				this.owner.TextPainter.RenderTextWithShadow(this.owner.Words.ToString(), this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + StrategicLocationHorizTableAddress)) << 2,
					this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(place + StrategicLocationVertTableAddress)) << 2, Hues.LightMagenta);
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
						this.owner.Words.Set(TacticalSiteRoleFortAddress);
						break;

					case 1:
						this.owner.Words.Set(TacticalSiteRoleCityAddress);
						break;

					case 2:
						this.owner.Words.Set(TacticalSiteRolePassAddress);
						break;
				}

				this.owner.TextPainter.RenderTextWithShadow(this.owner.Words.ToString(), this.owner.World.Participants[participantIdx].TacticalSites[siteIdx].Place.X << 2,
					this.owner.World.Participants[participantIdx].TacticalSites[siteIdx].Place.Y << 2, Hues.Yellow);
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
						this.owner.Words.Append(letter);
					}
				}

				this.owner.Words.Append(" ");

				this.owner.Words.Append(this.owner.Libc.IntegerToText(continent.Guard, 10));

				this.owner.Words.Append("/");

				this.owner.Words.Append(this.owner.Libc.IntegerToText(continent.Assault, 10));

				this.owner.TextPainter.RenderTextToScreen0(this.owner.Words.ToString(), 2, row, Hues.LightGray);

				row += 6;
			}

			this.owner.Libc.ReadKey();

			DrawingBox.TypefaceIdx = 0x1;

			this.owner.Startup.DisplayMouseCursor();

			this.processor.Journal.CloseScope("DisplayArmyList");
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
				this.owner.Words.Append("To:");
				this.owner.Words.Append(this.owner.Libc.IntegerToText(this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz), 10));
				this.owner.Words.Append(",");
				this.owner.Words.Append(this.owner.Libc.IntegerToText(this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelVert), 10));
				this.owner.Words.Append("\n");
			}

			this.owner.Words.Append("*");

			for (short current = this.troops.Following(participantIdx, troopIdx);
				current != -1 && current != troopIdx && (short)this.owner.Words.Length < 0x200;
				current = this.troops.Following(participantIdx, current))
			{
				this.owner.Words.Append(KindLabel(participantIdx, current));
				this.owner.Words.Append("\n*");
			}

			this.owner.TurnHooks.DisplayDialog(TextBuffer.Address, 10, 10);

			this.processor.Journal.CloseScope("DisplayUnitInfo");
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

					this.owner.Services.FloodBox(ScreenBox, boxHoriz + 1, boxVert + 1, 3, 3, Hues.Black);

					this.owner.Services.FloodBox(ScreenBox, boxHoriz, boxVert, 3, 3, hue);
				}
			}
		}

		/// <summary>The name of the unit's type: the start of its record in the unit type table (0x112a + 0x22 * type)</summary>
		private ushort KindLabel(short participantIdx, short troopIdx)
		{
			return (ushort)(UnitKindTable.Origin + (UnitKindTable.Stride * this.troops.TroopKind(participantIdx, troopIdx)));
		}
	}
}
