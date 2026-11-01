namespace Civ_1_5_Remastered.Core.UI.Units
{
	public class UnitPicker : GameComponent, IUnitPicker
	{
		private const ushort TacticalSiteRoleFortAddress = 0x417f;
		private const ushort TacticalSiteRoleCityAddress = 0x4181;
		private const ushort TacticalSiteRolePassAddress = 0x4183;

		private readonly UnitMemory troops;

		public UnitPicker(IGameEngine owner)
			: base(owner)
		{
			this.troops = new UnitMemory(owner.Processor);
		}

		public void ShowWhichArmy()
		{
			using var _ = this.Processor.Journal.Scope("DisplayWhichArmy()");

			this.Owner.Words.Set("Which army?\n ");

			for (int participantIndex = 1; participantIndex < 8; participantIndex++)
			{
				this.Owner.Words.Append(Tables.CivilizationName(participantIndex));

				this.Owner.Words.Append("\n ");
			}

			short chosenIndex = (short)(this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 100, 80) + 1);

			this.Owner.VideoCard.RenderImage(Screen.Box, 0, 0, 0x140, 0xc8,
				Screen.BackScreenBox, 0, 0);

			ShowArmyList(chosenIndex);

			this.Owner.VideoCard.RenderImage(Screen.BackScreenBox, 0, 0, 0x140, 0xc8,
				Screen.Box, 0, 0);
		}

		public void ShowArmyList(short participantIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayArmyList({participantIndex})");

			this.Owner.Startup.HideMouseCursor();

			this.Owner.Services.FloodBox(Screen.Box, 0, 0, 320, 200, Hues.Black);

			ushort sight = (ushort)(1 << (byte)participantIndex);

			for (short verticalLocation = 0; verticalLocation < 0x32; verticalLocation++)
			{
				for (short horizontalLocation = 0; horizontalLocation < 0x50; horizontalLocation++)
				{
					if ((this.Owner.World.ChartSight[(ushort)horizontalLocation, (ushort)verticalLocation] & sight) == 0 && MapView.Revealed == 0)
					{
						continue;
					}

					DrawStrategicTile(horizontalLocation, verticalLocation);
				}
			}

			this.Owner.Runtime.ReadKey();

			DrawingBox.TypefaceIndex = 0x2;

			for (int placeIndex = 0; placeIndex < 0x20; placeIndex++)
			{
				ushort place = (ushort)((participantIndex << 5) + placeIndex);
				byte live = this.Processor.PullGlobalByte((ushort)(place + IUnitPicker.StrategicLocationRoleTableAddress));

				if (live == 0xff)
				{
					continue;
				}

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

				this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), this.Processor.PullSignedByte(this.Processor.SegmentData.Word, (ushort)(place + IUnitPicker.StrategicLocationHorizontalTableAddress)) << 2,
					this.Processor.PullSignedByte(this.Processor.SegmentData.Word, (ushort)(place + IUnitPicker.StrategicLocationVerticalTableAddress)) << 2, Hues.LightMagenta);
			}

			for (int siteIndex = 0; siteIndex < 0x10; siteIndex++)
			{
				byte live = (byte)this.Owner.World.Participants[participantIndex].TacticalSites[siteIndex].Live;

				if (live == 0xff)
				{
					continue;
				}

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

				this.Owner.TextPainter.DrawTextWithShadow(this.Owner.Words.ToString(), this.Owner.World.Participants[participantIndex].TacticalSites[siteIndex].Place.X << 2,
					this.Owner.World.Participants[participantIndex].TacticalSites[siteIndex].Place.Y << 2, Hues.Yellow);
			}

			this.Owner.Runtime.ReadKey();

			short row = 1;

			for (short continentIndex = 1; continentIndex < 0xf; continentIndex++)
			{
				PlayerContinent continent = this.Owner.World.Participants[participantIndex].Landmasses[continentIndex];

				if (continent.Assault == 0)
				{
					continue;
				}

				EmptyMessage()
					.Number(continentIndex)
					.Text("/");

				foreach ((short approach, string letter) in new[] { ((short)0, "S"), ((short)2, "D"), ((short)1, "A"), ((short)5, "T") })
				{
					if (continent.Approach == approach)
					{
						this.Owner.Words.Append(letter);
					}
				}

				this.Owner.Words.Append(" ");

				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(continent.Guard, 10));

				this.Owner.Words.Append("/");

				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(continent.Assault, 10));

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 2, row, Hues.LightGray);

				row += 6;
			}

			this.Owner.Runtime.ReadKey();

			DrawingBox.TypefaceIndex = 0x1;

			this.Owner.Startup.ShowMouseCursor();
		}

		public void ShowUnitInformation(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayUnitInfo({participantIndex}, {troopIndex})");

			Message(Tables.CivilizationName(participantIndex))
				.Text("\n")
				.Number(troopIndex)
				.Text(KindLabel(participantIndex, troopIndex))
				.Text("\n")
				.City(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.BaseTown))
				.Text("\n")
				.Text("Loc:")
				.Number(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal))
				.Text(",")
				.Number(this.troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical))
				.Text("\n");

			if (this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal) != 0xff)
			{
				this.Owner.Words.Append("To:");
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelHorizontal), 10));
				this.Owner.Words.Append(",");
				this.Owner.Words.Append(this.Owner.Runtime.IntegerToText(this.troops.Pull(participantIndex, troopIndex, UnitMemory.TravelVertical), 10));
				this.Owner.Words.Append("\n");
			}

			this.Owner.Words.Append("*");

			for (short current = this.troops.Following(participantIndex, troopIndex);
				current != -1 && current != troopIndex && (short)this.Owner.Words.Length < 0x200;
				current = this.troops.Following(participantIndex, current))
			{
				this.Owner.Words.Append(KindLabel(participantIndex, current));
				this.Owner.Words.Append("\n*");
			}

			this.Owner.TurnHooks.ShowDialog(TextBuffer.Address, 10, 10);
		}

		private void DrawStrategicTile(short horizontalLocation, short verticalLocation)
		{
			short boxHorizontal = (short)(horizontalLocation << 2);
			short boxVertical = (short)(verticalLocation << 2);

			this.Owner.Services.FloodBox(Screen.Box, boxHorizontal, boxVertical, 4, 4,
				(ushort)(this.Owner.MapOperations.ObtainTerrainType(horizontalLocation, verticalLocation) == 0xa ? 1 : 2));

			if ((this.Owner.MapOperations.ObtainTileImprovements(horizontalLocation, verticalLocation) & 1) != 0)
			{
				this.Owner.Services.FloodBox(Screen.Box, boxHorizontal, boxVertical, 4, 4,
					Tables.PlayerColour(this.Owner.MapOperations.ObtainTileOwnerBits(horizontalLocation, verticalLocation)));
			}
			else
			{
				if (this.Owner.MapOperations.ObtainTileOwner(horizontalLocation, verticalLocation) != 0xffff)
				{
					ushort hue = Tables.PlayerColour(this.Owner.MapOperations.ObtainTileOwner(horizontalLocation, verticalLocation));

					this.Owner.Services.FloodBox(Screen.Box, boxHorizontal + 1, boxVertical + 1, 3, 3, Hues.Black);

					this.Owner.Services.FloodBox(Screen.Box, boxHorizontal, boxVertical, 3, 3, hue);
				}
			}
		}

		private ushort KindLabel(short participantIndex, short troopIndex)
		{
			return (ushort)(UnitKindTable.Origin + (UnitKindTable.Stride * this.troops.TroopKind(participantIndex, troopIndex)));
		}
	}
}
