namespace Civilization1Remaster.Core.UI
{
	public class UnitPicker : GameComponent, IUnitPicker
	{
		public const ushort StrategicLocationRoleTableAddress = 0xe2c2;
		public const ushort StrategicLocationHorizTableAddress = 0xe598;
		public const ushort StrategicLocationVertTableAddress = 0xe798;
		public const ushort StrategicLocationPriorityTableAddress = 0xb2be;

		private const ushort TacticalSiteRoleFortAddress = 0x417f;
		private const ushort TacticalSiteRoleCityAddress = 0x4181;
		private const ushort TacticalSiteRolePassAddress = 0x4183;

		private readonly UnitMemory troops;

		public UnitPicker(GameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		public void ShowWhichArmy()
		{
			using var _ = this.Processor.Journal.Scope("DisplayWhichArmy()");

			this.Owner.Words.Set("Which army?\n ");

			for (int participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				this.Owner.Words.Append(Tables.CivilizationName(participantIdx));

				this.Owner.Words.Append("\n ");
			}

			short chosenIdx = (short)(this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80) + 1);

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8,
				Screen.BackScreenBox, 0, 0);

			ShowArmyList(chosenIdx);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
				Screen.Box, 0, 0);
		}

		public void ShowArmyList(short participantIdx)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayArmyList({participantIdx})");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			ushort sight = (ushort)(1 << (byte)participantIdx);

			for (short vertLoc = 0; vertLoc < 0x32; vertLoc++)
			{
				for (short horizLoc = 0; horizLoc < 0x50; horizLoc++)
				{
					if ((this.Owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc] & sight) == 0 && MapView.Revealed == 0)
						continue;

					DrawStrategicTile(horizLoc, vertLoc);
				}
			}

			this.Owner.Libc.ReadKey();

			DrawingBox.TypefaceIdx = 0x2;

			for (int placeIdx = 0; placeIdx < 0x20; placeIdx++)
			{
				ushort place = (ushort)((participantIdx << 5) + placeIdx);
				byte live = this.Processor.PullGlobalByte((ushort)(place + StrategicLocationRoleTableAddress));

				if (live == 0xff)
					continue;

				switch ((sbyte)live)
				{
					case 0:
						this.Owner.Words.Set("S");
						break;

					case 1:
						this.Owner.Words.Set("A");
						break;

					case 2:
						this.Owner.Words.Set("D");
						break;

					case 3:
						this.Owner.Words.Set("N");
						break;
				}

				this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), this.Processor.PullInt8(this.Processor.SegData.Value16, (ushort)(place + StrategicLocationHorizTableAddress)) << 2,
					this.Processor.PullInt8(this.Processor.SegData.Value16, (ushort)(place + StrategicLocationVertTableAddress)) << 2, Hues.LightMagenta);
			}

			for (int siteIdx = 0; siteIdx < 0x10; siteIdx++)
			{
				byte live = (byte)this.Owner.World.Participants[participantIdx].TacticalSites[siteIdx].Live;

				if (live == 0xff)
					continue;

				switch ((sbyte)live)
				{
					case 0:
						this.Owner.Words.Set(TacticalSiteRoleFortAddress);
						break;

					case 1:
						this.Owner.Words.Set(TacticalSiteRoleCityAddress);
						break;

					case 2:
						this.Owner.Words.Set(TacticalSiteRolePassAddress);
						break;
				}

				this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), this.Owner.World.Participants[participantIdx].TacticalSites[siteIdx].Place.X << 2,
					this.Owner.World.Participants[participantIdx].TacticalSites[siteIdx].Place.Y << 2, Hues.Yellow);
			}

			this.Owner.Libc.ReadKey();

			short row = 1;

			for (short continentIdx = 1; continentIdx < 0xf; continentIdx++)
			{
				PlayerContinent continent = this.Owner.World.Participants[participantIdx].Landmasses[continentIdx];

				if (continent.Assault == 0)
					continue;

				EmptyMessage()
					.Number(continentIdx)
					.Text("/");

				foreach ((short approach, string letter) in new[] { ((short)0, "S"), ((short)2, "D"), ((short)1, "A"), ((short)5, "T") })
				{
					if (continent.Approach == approach)
					{
						this.Owner.Words.Append(letter);
					}
				}

				this.Owner.Words.Append(" ");

				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(continent.Guard, 10));

				this.Owner.Words.Append("/");

				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(continent.Assault, 10));

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 2, row, Hues.LightGray);

				row += 6;
			}

			this.Owner.Libc.ReadKey();

			DrawingBox.TypefaceIdx = 0x1;

			this.Owner.Startup.ShowMouseCursor();
		}

		public void ShowUnitInfo(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayUnitInfo({participantIdx}, {troopIdx})");

			Message(Tables.CivilizationName(participantIdx))
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
				this.Owner.Words.Append("To:");
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz), 10));
				this.Owner.Words.Append(",");
				this.Owner.Words.Append(this.Owner.Libc.IntegerToText(this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelVert), 10));
				this.Owner.Words.Append("\n");
			}

			this.Owner.Words.Append("*");

			for (short current = this.troops.Following(participantIdx, troopIdx);
				current != -1 && current != troopIdx && (short)this.Owner.Words.Length < 0x200;
				current = this.troops.Following(participantIdx, current))
			{
				this.Owner.Words.Append(KindLabel(participantIdx, current));
				this.Owner.Words.Append("\n*");
			}

			this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 10, 10);
		}

		private void DrawStrategicTile(short horizLoc, short vertLoc)
		{
			short boxHoriz = (short)(horizLoc << 2);
			short boxVert = (short)(vertLoc << 2);

			this.Owner.Services.FloodBox(Screen.Box, boxHoriz, boxVert, 4, 4,
				(ushort)(this.Owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == 0xa ? 1 : 2));

			if ((this.Owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				this.Owner.Services.FloodBox(Screen.Box, boxHoriz, boxVert, 4, 4,
					Tables.PlayerColour(this.Owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc)));
			}
			else
			{
				if (this.Owner.MapOps.ObtainTileOwner(horizLoc, vertLoc) != 0xffff)
				{
					ushort hue = Tables.PlayerColour(this.Owner.MapOps.ObtainTileOwner(horizLoc, vertLoc));

					this.Owner.Services.FloodBox(Screen.Box, boxHoriz + 1, boxVert + 1, 3, 3, Hues.Black);

					this.Owner.Services.FloodBox(Screen.Box, boxHoriz, boxVert, 3, 3, hue);
				}
			}
		}

		private ushort KindLabel(short participantIdx, short troopIdx)
		{
			return (ushort)(UnitKindTable.Origin + (UnitKindTable.Stride * this.troops.TroopKind(participantIdx, troopIdx)));
		}
	}
}
