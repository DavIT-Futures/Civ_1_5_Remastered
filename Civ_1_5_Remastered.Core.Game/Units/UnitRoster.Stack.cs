using System;

namespace Civ_1_5_Remastered.Core.Game.Units
{
	public partial class UnitRoster
	{
		public ushort FindStackLeader(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"LocateStackLeader({participantIndex}, {troopIndex})");

			ushort leader = (ushort)troopIndex;

			if (this.troops.Following(participantIndex, troopIndex) != -1)
			{
				Stack.Leader = (ushort)troopIndex;
				Stack.LeaderScore = 0;

				for (short firstIndex = 0; firstIndex < UnitMemory.TroopTally; firstIndex++)
				{
					if (this.troops.Pull(participantIndex, firstIndex, UnitMemory.Kind) != 0xff &&
						this.troops.Pull(participantIndex, firstIndex, UnitMemory.Horizontal) == this.troops.Pull(participantIndex, troopIndex, UnitMemory.Horizontal) &&
						this.troops.Pull(participantIndex, firstIndex, UnitMemory.Vertical) == this.troops.Pull(participantIndex, troopIndex, UnitMemory.Vertical))
					{
						VisitEachUnitInStack(participantIndex, firstIndex, 0x1169);
						break;
					}
				}

				leader = Stack.Leader;
			}

			return leader;
		}

		public ushort FindBestGuardianInStack(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"LocateBestGuardianInStack({participantIndex}, {troopIndex})");

			ushort guardian = (ushort)troopIndex;

			if (this.troops.Following(participantIndex, troopIndex) != -1)
			{
				Stack.Leader = (ushort)troopIndex;
				Stack.LeaderScore = 0xffff;

				VisitEachUnitInStack(participantIndex, troopIndex, 0x1169);

				guardian = Stack.Leader;
			}

			return guardian;
		}

		public void FindBestGuardianStep(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"LocateBestGuardianStep({participantIndex}, {troopIndex})");

			int unitKind = this.Troops.PullSigned(participantIndex, troopIndex, UnitMemory.Kind);
			int horizontal = this.Troops.PullSigned(participantIndex, troopIndex, UnitMemory.Horizontal);
			int vertical = this.Troops.PullSigned(participantIndex, troopIndex, UnitMemory.Vertical);
			int terrainType = this.Owner.MapOperations.ObtainTerrainType(horizontal, vertical);

			int landGroup = (short)Tables.UnitKindWord(unitKind, UnitKindTable.LandGroup);
			int baseGuardPower = (short)Tables.UnitKindWord(unitKind, UnitKindTable.GuardPower);
			int terrainDefenseFactor = (sbyte)this.Owner.Tables.Byte((ushort)((TerrainTable.Stride * terrainType) + TerrainTable.Origin + TerrainTable.DefenseBonus));

			byte condition = this.Troops.Pull(participantIndex, troopIndex, UnitMemory.Status);
			bool isFortified = (condition & 0x8) != 0;
			bool isVeteran = (condition & 0x20) != 0;

			int score = CombatRules.ComputeGuardianScore(landGroup, baseGuardPower, terrainType, terrainDefenseFactor, isFortified, isVeteran);

			if (score >= 0)
			{
				int bestScore = (short)Stack.LeaderScore;
				if (score > bestScore)
				{
					Stack.LeaderScore = (ushort)score;
					Stack.Leader = (ushort)troopIndex;
				}
			}
		}

		public ushort CountUnitsByDomainInStack(short participantIndex, short troopIndex, ushort mode)
		{
			using var _ = this.Processor.Journal.Scope($"TallyUnitsByDomainInStack({participantIndex}, {troopIndex}, {mode})");

			Stack.DomainTally = 0x0;
			Stack.DomainMode = mode;
			Stack.IndexLimit = (ushort)troopIndex;

			VisitEachUnitInStack(participantIndex, troopIndex, 0x1280);

			ushort result = Stack.DomainTally;

			return result;
		}

		public void CountUnitsByDomainStep(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"TallyUnitsByDomainStep({participantIndex}, {troopIndex})");

			switch (Stack.DomainMode)
			{
				case 0:
					AddToGlobal(0x6532, this.troops.KindField(participantIndex, troopIndex, UnitKindTable.Price));
					break;

				case 1:
					AddToGlobal(0x6532, this.troops.KindField(participantIndex, troopIndex, UnitKindTable.GuardPower));
					break;

				case 2:
					AddToGlobal(0x6532, 1);
					break;

				case 3:
					AddToGlobal(0x6532, this.troops.KindField(participantIndex, troopIndex, UnitKindTable.AssaultPower));
					break;

				case 4:
					if (troopIndex < (short)Stack.IndexLimit &&
						this.troops.KindField(participantIndex, troopIndex, UnitKindTable.TroopGroup) == UnitKindTable.DefenceTroops)
					{
						AddToGlobal(0x6532, 1);
					}

					break;
			}
		}

		public ushort CountUnitsOfTypeInStack(short participantIndex, short troopIndex, ushort kind)
		{
			using var _ = this.Processor.Journal.Scope($"TallyUnitsOfTypeInStack({participantIndex}, {troopIndex}, {kind})");

			Stack.Kind = kind;
			Stack.Tally = 0x0;

			VisitEachUnitInStack(participantIndex, troopIndex, 0x135a);

			ushort result = Stack.Tally;

			return result;
		}

		public void CountUnitsOfTypeStep(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"TallyUnitsOfTypeStep({participantIndex}, {troopIndex})");

			if ((ushort)this.troops.TroopKind(participantIndex, troopIndex) == Stack.Kind)
			{
				AddToGlobal(0x653c, 1);
			}
		}

		public ushort CountUnitsWithFlagInStack(short participantIndex, short troopIndex, ushort group)
		{
			using var _ = this.Processor.Journal.Scope($"TallyUnitsWithFlagInStack({participantIndex}, {troopIndex}, {group})");

			Stack.Kind = group;
			Stack.Tally = 0x0;

			VisitEachUnitInStack(participantIndex, troopIndex, 0x13a9);

			ushort result = Stack.Tally;

			return result;
		}

		public void CountUnitsWithFlagStep(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"TallyUnitsWithFlagStep({participantIndex}, {troopIndex})");

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.TroopGroup) == Stack.Kind)
			{
				AddToGlobal(0x653c, 1);
			}
		}

		public ushort CountSpecialUnitsInStack(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"TallySpecialUnitsInStack({participantIndex}, {troopIndex})");

			Stack.Tally = 0x0;

			VisitEachUnitInStack(participantIndex, troopIndex, 0x13f8);

			ushort result = Stack.Tally;

			return result;
		}

		public void CountSpecialUnitsStep(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"TallySpecialUnitsStep({participantIndex}, {troopIndex})");

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				AddToGlobal(0x653c, -1);
			}

			if (this.troops.KindField(participantIndex, troopIndex, UnitKindTable.TroopGroup) == UnitKindTable.TransportTroops)
			{
				AddToGlobal(0x653c, this.troops.KindField(participantIndex, troopIndex, UnitKindTable.FerryRoom));
			}
		}

		public void VisitEachUnitInStack(short participantIndex, short troopIndex, int functionAddress)
		{
			using var _ = this.Processor.Journal.Scope($"VisitEachUnitInStack({participantIndex}, {troopIndex}, 0x{functionAddress:x4})");

			Action<short, short> step = StackStep(functionAddress);

			ForEachUnitInStack(participantIndex, troopIndex, (stepParticipantIndex, stepTroopIndex) =>
			{
				step(stepParticipantIndex, stepTroopIndex);
			});
		}

		public void ForEachUnitInStack(short participantIndex, short troopIndex, Action<short, short> visit)
		{
			short present = troopIndex;

			for (int index = 0; index < 10; index++)
			{
				short following = this.troops.Following(participantIndex, present);

				visit(participantIndex, present);

				present = following;

				if (present == -1 || present == troopIndex)
				{
					break;
				}
			}
		}

		public ushort ShowBattleScreen(short participantIndex, short troopIndex)
		{
			using var _ = this.Processor.Journal.Scope($"DisplayBattleScreen({participantIndex}, {troopIndex})");

			ushort screen = Screen.Box;
			ushort backScreen = Screen.BackScreenBox;

			this.Owner.Startup.HideMouseCursor();

			this.Owner.VideoCard.RenderImage(screen, 0, 0, 0x140, 0xc8, backScreen, 0, 0);

			ushort picked = FindStackLeader(participantIndex, troopIndex);
			short[] stack = new short[StackListTally];
			int stackTally = 0;
			short present = troopIndex;

			do
			{
				stack[stackTally] = present;
				present = this.troops.Following(participantIndex, present);
				stackTally++;
			}
			while (stackTally < StackListTally && present != -1 && present != troopIndex);

			short listTop = (short)(0x60 - (stackTally << 3));
			int selected = 0;

			while (true)
			{
				stackTally = DrawStackList(participantIndex, troopIndex, listTop, stackTally);

				int choice = ChooseFromStackList(listTop, stackTally, ref selected);

				if (choice < 0 || choice >= stackTally)
				{
					break;
				}

				picked = (ushort)stack[choice];

				if ((this.troops.Pull(participantIndex, picked, UnitMemory.Status) & (int)UnitStatus.SettlerBuildMask) != 0)
				{
					this.troops.Assign(participantIndex, picked, UnitMemory.LeftoverSteps,
						(byte)(3 * this.troops.KindField(participantIndex, picked, UnitKindTable.ShiftTally)));
				}

				this.troops.Assign(participantIndex, picked, UnitMemory.Status,
					(byte)(this.troops.Pull(participantIndex, picked, UnitMemory.Status) & 0x30));
				ClearTravel(participantIndex, (short)picked);

				if (stackTally <= 1)
				{
					break;
				}
			}

			this.Owner.VideoCard.RenderImage(backScreen, 0, 0, 0x140, 0xc8, screen, 0, 0);

			this.Owner.Startup.ShowMouseCursor();

			this.Owner.TurnLogic.WaitForKey();

			return picked;
		}

		private Action<short, short> StackStep(int functionAddress)
		{
			return functionAddress switch
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

		private int DrawStackList(short participantIndex, short troopIndex, short listTop, int boxTally)
		{
			this.Owner.Dialogs.FillBoxWithDoubleShadow(100, listTop, 120, (boxTally << 4) + 5, Hues.Cyan);

			int stackTally = 0;
			short present = troopIndex;
			int lineTop = listTop + 5;

			do
			{
				this.Owner.MapOperations.DrawUnitSprite(participantIndex, present, 106, lineTop);

				this.Owner.Words.Set((ushort)(UnitKindTable.Origin + (UnitKindTable.Stride * this.troops.TroopKind(participantIndex, present))));

				this.Owner.Words.Append(UnitRankRules.Suffix(
					(this.troops.Pull(participantIndex, present, UnitMemory.Status) & (int)UnitStatus.Veteran) != 0,
					this.Owner.World.Vitals.VerifyElite(participantIndex, present, this.troops.TroopKind(participantIndex, present))));

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 128, lineTop, Hues.White);

				EmptyMessage()
					.City(this.troops.PullSigned(participantIndex, present, UnitMemory.BaseTown));

				this.Owner.TextPainter.DrawTextToPrimaryScreen(this.Owner.Words.ToString(), 128, lineTop + 8, Hues.Yellow);

				present = this.troops.Following(participantIndex, present);
				lineTop += 0x10;
				stackTally++;
			}
			while (stackTally < StackListTally && present != -1 && present != troopIndex);

			return stackTally;
		}

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

					if (this.Owner.MenuPointerState != 0)
					{
						break;
					}
				}
				while (this.Owner.Runtime.KeyAvailable() == 0);
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
						{
							selected--;
						}
					}
					else if (key == KeyCodes.Down)
					{
						if (stackTally - 1 > selected)
						{
							selected++;
						}
					}

					this.Owner.VideoCard.SubstituteHue(screen, 105, (highlighted * 16) + listTop + 4, 112, 17, Hues.LightCyan, Hues.Cyan);

					if (key == KeyCodes.Enter || key == KeyCodes.Space)
					{
						return selected;
					}

					if (key == KeyCodes.Escape)
					{
						break;
					}
				}
			}

			this.Owner.Startup.HideMouseCursor();

			if (this.Owner.MenuPointerState == 1 && (short)this.Owner.MenuPointerHorizontal > 0x64 && (short)this.Owner.MenuPointerHorizontal < 0xdc)
			{
				return (short)(this.Owner.MenuPointerVertical - listTop - 5) / 16;
			}

			return -1;
		}
	}
}
