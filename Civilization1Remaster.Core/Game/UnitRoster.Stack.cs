using System;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>The units of a stack: the leader, the best defender, the counts, the walks over the stack and the list to pick a unit from</summary>
	public partial class UnitRoster
	{
		/// <inheritdoc/>
		public ushort FindStackLeader(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"LocateStackLeader({participantIdx}, {troopIdx})");

			ushort leader = (ushort)troopIdx;

			if (this.troops.Following(participantIdx, troopIdx) != -1)
			{
				Stack.Leader = (ushort)troopIdx;
				Stack.LeaderScore = 0;

				for (short firstIdx = 0; firstIdx < UnitMemory.TroopTally; firstIdx++)
				{
					if (this.troops.Pull(participantIdx, firstIdx, UnitMemory.Kind) != 0xff &&
						this.troops.Pull(participantIdx, firstIdx, UnitMemory.Horiz) == this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz) &&
						this.troops.Pull(participantIdx, firstIdx, UnitMemory.Vert) == this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert))
					{
						VisitEachUnitInStack(participantIdx, firstIdx, 0x1169);
						break;
					}
				}

				leader = Stack.Leader;
			}


			return leader;
		}

		/// <inheritdoc/>
		public ushort FindBestGuardianInStack(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"LocateBestGuardianInStack({participantIdx}, {troopIdx})");

			ushort guardian = (ushort)troopIdx;

			if (this.troops.Following(participantIdx, troopIdx) != -1)
			{
				Stack.Leader = (ushort)troopIdx;
				Stack.LeaderScore = 0xffff;

				VisitEachUnitInStack(participantIdx, troopIdx, 0x1169);

				guardian = Stack.Leader;
			}


			return guardian;
		}

		/// <inheritdoc/>
		public void FindBestGuardianStep(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"LocateBestGuardianStep({participantIdx}, {troopIdx})");

			// Extract unit parameters
			int unitKind = this.Troops.PullSigned(participantIdx, troopIdx, UnitMemory.Kind);
			int horiz = this.Troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
			int vert = this.Troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);
			int terrainType = this.Owner.MapOps.ObtainTerrainType(horiz, vert);

			int landGroup = (short)UnitKindWord(unitKind, UnitKindTable.LandGroup);
			int baseGuardPower = (short)UnitKindWord(unitKind, UnitKindTable.GuardPower);
			int terrainDefenseFactor = (sbyte)this.Owner.Tables.Byte((ushort)(TerrainTable.Stride * terrainType + TerrainTable.Origin + TerrainTable.DefenseBonus));

			byte condition = this.Troops.Pull(participantIdx, troopIdx, UnitMemory.Status);
			bool isFortified = (condition & 0x8) != 0;
			bool isVeteran = (condition & 0x20) != 0;

			int score = CombatRules.ComputeGuardianScore(
				unitKind, landGroup, baseGuardPower, terrainType, terrainDefenseFactor, isFortified, isVeteran);

			if (score >= 0)
			{
				int bestScore = (short)Stack.LeaderScore;
				if (score > bestScore)
				{
					Stack.LeaderScore = (ushort)score;
					Stack.Leader = (ushort)troopIdx;
				}
			}
		}

		/// <inheritdoc/>
		public ushort CountUnitsByDomainInStack(short participantIdx, short troopIdx, ushort mode)
		{
			using var _ = this.Processor.Journal.Scope($"TallyUnitsByDomainInStack({participantIdx}, {troopIdx}, {mode})");

			Stack.DomainTally = 0x0;
			Stack.DomainMode = mode;
			Stack.IndexLimit = (ushort)troopIdx;

			VisitEachUnitInStack(participantIdx, troopIdx, 0x1280);

			ushort result = Stack.DomainTally;

			return result;
		}

		/// <inheritdoc/>
		public void CountUnitsByDomainStep(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"TallyUnitsByDomainStep({participantIdx}, {troopIdx})");

			switch (Stack.DomainMode)
			{
				case 0:
					AddToGlobal(0x6532, this.troops.KindField(participantIdx, troopIdx, UnitKindTable.Price));
					break;

				case 1:
					AddToGlobal(0x6532, this.troops.KindField(participantIdx, troopIdx, UnitKindTable.GuardPower));
					break;

				case 2:
					AddToGlobal(0x6532, 1);
					break;

				case 3:
					AddToGlobal(0x6532, this.troops.KindField(participantIdx, troopIdx, UnitKindTable.AssaultPower));
					break;

				case 4:
					if (troopIdx < (short)Stack.IndexLimit &&
						this.troops.KindField(participantIdx, troopIdx, UnitKindTable.TroopGroup) == UnitKindTable.DefenceTroops)
					{
						AddToGlobal(0x6532, 1);
					}
					break;
			}
		}

		/// <inheritdoc/>
		public ushort CountUnitsOfTypeInStack(short participantIdx, short troopIdx, ushort kind)
		{
			using var _ = this.Processor.Journal.Scope($"TallyUnitsOfTypeInStack({participantIdx}, {troopIdx}, {kind})");

			Stack.Kind = kind;
			Stack.Tally = 0x0;

			VisitEachUnitInStack(participantIdx, troopIdx, 0x135a);

			ushort result = Stack.Tally;

			return result;
		}

		/// <inheritdoc/>
		public void CountUnitsOfTypeStep(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"TallyUnitsOfTypeStep({participantIdx}, {troopIdx})");

			if ((ushort)this.troops.TroopKind(participantIdx, troopIdx) == Stack.Kind)
			{
				AddToGlobal(0x653c, 1);
			}
		}

		/// <inheritdoc/>
		public ushort CountUnitsWithFlagInStack(short participantIdx, short troopIdx, ushort group)
		{
			using var _ = this.Processor.Journal.Scope($"TallyUnitsWithFlagInStack({participantIdx}, {troopIdx}, {group})");

			Stack.Kind = group;
			Stack.Tally = 0x0;

			VisitEachUnitInStack(participantIdx, troopIdx, 0x13a9);

			ushort result = Stack.Tally;

			return result;
		}

		/// <inheritdoc/>
		public void CountUnitsWithFlagStep(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"TallyUnitsWithFlagStep({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.TroopGroup) == Stack.Kind)
			{
				AddToGlobal(0x653c, 1);
			}
		}

		/// <inheritdoc/>
		public ushort CountSpecialUnitsInStack(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"TallySpecialUnitsInStack({participantIdx}, {troopIdx})");

			Stack.Tally = 0x0;

			VisitEachUnitInStack(participantIdx, troopIdx, 0x13f8);

			ushort result = Stack.Tally;

			return result;
		}

		/// <inheritdoc/>
		public void CountSpecialUnitsStep(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"TallySpecialUnitsStep({participantIdx}, {troopIdx})");

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				AddToGlobal(0x653c, -1);
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.TroopGroup) == UnitKindTable.TransportTroops)
			{
				AddToGlobal(0x653c, this.troops.KindField(participantIdx, troopIdx, UnitKindTable.FerryRoom));
			}
		}

		/// <inheritdoc/>
		public void VisitEachUnitInStack(short participantIdx, short troopIdx, int funcAddr)
		{
			using var _ = this.Processor.Journal.Scope($"VisitEachUnitInStack({participantIdx}, {troopIdx}, 0x{funcAddr:x4})");

			Action<short, short> step = StackStep(funcAddr);

			ForEachUnitInStack(participantIdx, troopIdx, (stepParticipantIdx, stepTroopIdx) =>
			{
				step(stepParticipantIdx, stepTroopIdx);
			});
		}

		/// <inheritdoc/>
		public void ForEachUnitInStack(short participantIdx, short troopIdx, Action<short, short> visit)
		{
			short present = troopIdx;

			for (int idx = 0; idx < 10; idx++)
			{
				short following = this.troops.Following(participantIdx, present);

				visit(participantIdx, present);

				present = following;

				if (present == -1 || present == troopIdx)
					break;
			}
		}

		/// <inheritdoc/>
		public ushort ShowBattleScreen(short participantIdx, short troopIdx)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayBattleScreen({participantIdx}, {troopIdx})");

			ushort screen = Screen.Box;
			ushort backScreen = Screen.BackScreenBox;

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(screen, 0, 0, 0x140, 0xc8, backScreen, 0, 0);

			ushort picked = FindStackLeader(participantIdx, troopIdx);
			short[] stack = new short[StackListTally];
			int stackTally = 0;
			short present = troopIdx;

			do
			{
				stack[stackTally] = present;
				present = this.troops.Following(participantIdx, present);
				stackTally++;
			}
			while (stackTally < StackListTally && present != -1 && present != troopIdx);

			short listTop = (short)(0x60 - (stackTally << 3));
			int selected = 0;

			while (true)
			{
				stackTally = DrawStackList(participantIdx, troopIdx, listTop, stackTally);

				int choice = ChooseFromStackList(listTop, stackTally, ref selected);

				if (choice < 0 || choice >= stackTally)
					break;

				picked = (ushort)stack[choice];

				if ((this.troops.Pull(participantIdx, picked, UnitMemory.Status) & (int)UnitStatus.SettlerBuildMask) != 0)
				{
					this.troops.Assign(participantIdx, picked, UnitMemory.LeftoverSteps,
						(byte)(3 * this.troops.KindField(participantIdx, picked, UnitKindTable.ShiftTally)));
				}

				// Only Unknown1 and Veteran stay
				this.troops.Assign(participantIdx, picked, UnitMemory.Status,
					(byte)(this.troops.Pull(participantIdx, picked, UnitMemory.Status) & 0x30));
				ClearTravel(participantIdx, (short)picked);

				if (stackTally <= 1)
					break;
			}

			this.Owner.VideoCard.RenderImage(backScreen, 0, 0, 0x140, 0xc8, screen, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.TurnLogic.WaitForKey();


			return picked;
		}

		/// <summary>The step functions of VisitEachUnitInStack by their address in the original code</summary>
		private Action<short, short> StackStep(int funcAddr)
		{
			return funcAddr switch
			{
				0xf10 => DestroyUnit,
				0x1169 => FindBestGuardianStep,
				0x1280 => CountUnitsByDomainStep,
				0x135a => CountUnitsOfTypeStep,
				0x13a9 => CountUnitsWithFlagStep,
				0x13f8 => CountSpecialUnitsStep,
				0x14a2 => CheckUnitTerrainFlag,
				0x14f6 => ClearUnitOrders,
				0x1560 => RestoreUnitOrders,
				0x1593 => RestoreUnitStatus,
				0x1610 => DestroyLandUnit,
				0x1643 => DestroyNonLandUnit,
				0x1676 => DestroySeaUnit,
				_ => throw new Exception("Unknown function address"),
			};
		}

		/// <summary>
		/// The box with the units of the stack: picture, name with " (V)" for a veteran, home city. The units are counted again
		/// while drawing, the box has the size of the count before.
		/// </summary>
		/// <returns>Number of units in the list</returns>
		private int DrawStackList(short participantIdx, short troopIdx, short listTop, int boxTally)
		{
			this.Owner.Dialogs.FillBoxWithDoubleShadow(100, listTop, 120, (boxTally << 4) + 5, Hues.Cyan);

			int stackTally = 0;
			short present = troopIdx;
			int lineTop = listTop + 5;

			do
			{
				this.Owner.MapOps.DrawUnitSprite(participantIdx, present, 106, lineTop);

				this.Owner.Words.Set((ushort)(UnitKindTable.Origin + (UnitKindTable.Stride * this.troops.TroopKind(participantIdx, present))));

				if ((this.troops.Pull(participantIdx, present, UnitMemory.Status) & (int)UnitStatus.Veteran) != 0)
				{
					this.Owner.Words.Append(" (V)");
				}

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 128, lineTop, Hues.White);

				EmptyMessage()
					.City(this.troops.PullSigned(participantIdx, present, UnitMemory.BaseTown));

				this.Owner.TextPainter.DrawTextToScreen0(this.Owner.Words.ToString(), 128, lineTop + 8, Hues.Yellow);

				present = this.troops.Following(participantIdx, present);
				lineTop += 0x10;
				stackTally++;
			}
			while (stackTally < StackListTally && present != -1 && present != troopIdx);

			return stackTally;
		}

		/// <summary>
		/// Waits for the choice in the list: with the mouse (0x1a3c) a click on a line or a key; without it the cursor keys move
		/// the highlight, Enter or Space choose, Escape ends. Like the original, the mouse pointer is hidden again except after
		/// Enter or Space.
		/// </summary>
		/// <returns>Line of the list, a negative number or a number past the list when none was chosen</returns>
		private int ChooseFromStackList(short listTop, int stackTally, ref int selected)
		{
			ushort screen = Screen.Box;

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.TurnLogic.WaitForKey();

			if (Mouse.Present != 0)
			{
				do
				{
					this.Owner.Startup.ReadMouseState();

					if (this.Owner.GlobDb3a != 0)
						break;
				}
				while (this.Owner.Libc.KeyAvailable() == 0);
			}
			else
			{
				while (true)
				{
					this.Owner.VideoCard.SubstituteHue(screen, 105, (selected * 16) + listTop + 4, 112, 17, Hues.Cyan, Hues.LightCyan);

					int highlighted = selected;
					ushort key = this.Owner.Dialogs.ReadNavigationKey();

					if (key == KeyCodes.Up)
					{
						if (selected != 0)
							selected--;
					}
					else if (key == KeyCodes.Down)
					{
						if (stackTally - 1 > selected)
							selected++;
					}

					this.Owner.VideoCard.SubstituteHue(screen, 105, (highlighted * 16) + listTop + 4, 112, 17, Hues.LightCyan, Hues.Cyan);

					if (key == KeyCodes.Enter || key == KeyCodes.Space)
						return selected;

					if (key == KeyCodes.Escape)
						break;
				}
			}

			this.Owner.Startup.HideMouseCursor();

			if (this.Owner.GlobDb3a == 1 && (short)this.Owner.GlobDb3c > 0x64 && (short)this.Owner.GlobDb3c < 0xdc)
				return (short)(this.Owner.GlobDb3e - listTop - 5) / 16;

			return -1;
		}
	}
}
