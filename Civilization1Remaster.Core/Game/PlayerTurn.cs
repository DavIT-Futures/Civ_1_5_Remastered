using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1403.
	/// </summary>
	public class PlayerTurn
	{
		private readonly GameEngine owner;
		private readonly CPU processor;
		private readonly UnitMemory troops;

		public PlayerTurn(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID"></param>
		public void HandlePlayerTurn(short participantIdx)
		{
			this.processor.Journal.OpenScope($"HandlePlayerTurn({participantIdx})");

			// function body
			this.processor.EnterFrame(0x52);
			this.processor.PreserveIndexRegisters();
			// The player, the help of some turns, the moves of the units
			PrepareTurn(participantIdx);

		L013c:
			this.processor.EmitLocalWord(0x1e, 0x1);
			this.processor.EmitLocalWord(0x32, 0x0);
			goto L014c;

		L0149:
			this.processor.EmitLocalWord(0x32, this.processor.IncrementWord(this.processor.PullLocalWord(0x32)));

		L014c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsLess) goto L0156;
			goto L3e4e;

		L0156:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x1ae0), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0160;
			goto L0165;

		L0160:
			this.owner.TurnHooks.RepaintMainScreen();

		L0165:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdc48), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L016f;
			goto L0177;

		L016f:
			this.processor.EmitLocalWord(0x32, 0x80);
			goto L0149;

		L0177:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x46);
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L0182;
			goto L0187;

		L0182:
			this.processor.EmitLocalWord(0x2e, 0x1);

		L0187:
			// Empty slots, sentried and fortified units and units without moves are skipped; fortifying ones are fortified now
			if (!StartUnitMove(participantIdx))
				goto L0149;
		L0356:
			// A unit of the human player without orders off the map view waits for the second pass, else the view moves to it
			if (!FocusOnUnit(participantIdx))
				goto L0149;

		L043b:
			if ((this.owner.World.ParticipantMarkers & (1 << participantIdx)) != 0)
				AwaitForKey();

			this.processor.EmitLocalWord(0xa, 0);

			if (this.processor.PullLocalWord(0x36) != 0)
				this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullGlobalWord(0xd4dc));

			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0xc, 0);
			this.processor.EmitLocalWord(0x36, 0);
			this.processor.EmitLocalWord(0x28, 0);

		L047f:
			// A computer unit gets its order from ShiftAIUnit, a unit with orders goes on with them, else the human chooses
			if ((this.owner.World.ParticipantMarkers & (1 << participantIdx)) == 0)
			{
				ObtainAIOrder(participantIdx);
				goto L0d0a;
			}

			if (ObtainStandingOrder(participantIdx))
				goto L0d00;
			// The human chooses: a key or a click
			switch (TakeHumanOrder(participantIdx))
			{
				case HumanOrder.MenuOrder:
					goto L0d0a;

				case HumanOrder.MenuWithoutUnit:
					goto L3365;

				case HumanOrder.NextLook:
					goto L3c40;

				case HumanOrder.EndOfTurn:
					goto L3ea1;
			}
		L0d00:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsLess) goto L0d0a;
			goto L1b5e;

		L0d0a:
			// The order 0x44 of the unit (a plane flying home moves at once)
			if (CarryOutOrder(participantIdx))
				goto L1c10;
		L1b5e:
			// Without a unit only the marked tile moves
			if ((short)this.processor.PullLocalWord(0x32) >= 0x80 && this.processor.PullLocalWord(0x4) == 0)
				goto L3365;

			// A move: the direction of the order (1 - 8 from GoTo and the computer, the arrows, the keypad); 'c' centers the view
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x44);

			if (this.processor.PullLocalWord(0x44) == 'c')
			{
				CenterView(participantIdx);
				goto L3365;
			}

			if (KeyDirection(this.processor.PullLocalWord(0x44)) == 0)
				goto L3365;

			this.processor.EmitLocalWord(0x24, KeyDirection(this.processor.PullLocalWord(0x44)));

		L1c10:
			// The marked tile moves, or a barbarian pillages a tile of the human on its way, or the unit moves
			if (this.processor.PullLocalWord(0x4) != 0)
			{
				MoveMarkedTile(participantIdx);
				goto L3365;
			}

			if (BarbarianPillages(participantIdx))
				goto L3365;
		L1d96:
			// The tile of the move; a land unit may be stopped by a zone of control, other units go on below (L219c)
			if (!LocateMoveTarget(participantIdx))
				goto L219c;

			if (StoppedByZoneOfControl(participantIdx))
				goto L3365;
		L1ed2:
			// A diplomat or caravan at a city
			switch (EnterCityAsDiplomatOrCaravan(participantIdx))
			{
				case MoveStep.GoesOn:
					goto L219c;

				case MoveStep.Done:
					goto L3365;
			}
		L2157:
			BribeUnitOnTheWay(participantIdx);

		L219c:
			// The tile of the move: boarding a ship, the cases without an attack, an attack below (L2329)
			switch (CheckMoveTile(participantIdx))
			{
				case MoveCheck.Moves:
					goto L267b;

				case MoveCheck.Done:
					goto L3365;
			}
		L2329:
			// The attack (the unit may be lost)
			if (!ConfirmAttack(participantIdx))
				goto L3365;

			if (FightAt(participantIdx))
				goto L3c40;

			goto L3365;
		L267b:
			// The cost of the move (a ship on land, a plane or ship into a foreign city, no moves: no move)
			if (!PayForMove(participantIdx))
				goto L3365;
		L2884:
			// A computer unit does not turn back nor crowd an own stack
			if (!ComputerMayMove(participantIdx))
				goto L3365;
		L29a0:
			// The territory of the tile: a treaty may stop the move
			if (!EnterTerritory(participantIdx))
				goto L3365;
		L2b06:
			this.owner.MapOps.ObtainTileOwnerBits(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x48, this.processor.RegAcc.Value16);
			
			if (participantIdx != 0)
				goto L2b9d;

			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L2b29;
			goto L2b9d;

		L2b29:
			this.processor.RegSource.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x2c),
				this.processor.PullLocalWord(0x30)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L2b54;
			goto L2b9d;

		L2b54:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, 0x81dd);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2b72;
			goto L2b9d;

		L2b72:
			this.owner.Helpers.LocateNearestCity(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			if (this.owner.World.Towns[this.processor.RegAcc.Value16].ParticipantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x2c),
					this.processor.PullLocalWord(0x30)];
				this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16, this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16), this.processor.RegAcc.LowByte));
			}

		L2b9d:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2ba6;
			goto L2c18;

		L2ba6:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L2bb1;
			goto L2c18;

		L2bb1:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c),
				this.processor.PullLocalWord(0x26)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L2bd6;
			goto L2c18;

		L2bd6:
			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)participantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[participantIdx] & 0x40) != 0)
				goto L2bfc;

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x48);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[this.processor.PullLocalWord(0x48)] & 0x40) == 0)
				goto L2c18;

		L2bfc:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

		L2c18:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L2c21;
			goto L2c51;

		L2c21:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L2c36;
			goto L2c51;

		L2c36:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x48);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

		L2c51:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2c76;
			goto L2c94;

		L2c76:
			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L2c94;

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2c8b;
			goto L2cdd;

		L2c8b:
			if (participantIdx == 0)
				goto L2cdd;

		L2c94:
			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x20);
			if (this.processor.FlagSet.IsEqual) goto L2c9e;
			goto L2ca9;

		L2c9e:
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L2ccf;

		L2ca9:
			this.owner.UnitOps.PanMapToPosition(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.owner.UnitOps.ChoreographUnitCombat(participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				this.processor.PullLocalWord(0x24));

			goto L2cdd;

		L2ccf:
			this.owner.MapOps.RenderMapTile(this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

		L2cdd:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81de));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);

			this.owner.MapOps.DiscardUnitFromTile(participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.processor.CopyLocalWord(0x2c, 0x22);
			this.processor.CopyLocalWord(0x30, 0x2a);

			this.owner.MapOps.SituateUnitOnTile(participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x1c);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x26);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd), 0x0);

			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L2d6e;

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2d69;
			goto L2d6e;

		L2d69:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0x30));

		L2d6e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2d77;
			goto L2da5;

		L2d77:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x8);
			if (this.processor.FlagSet.IsLess) goto L2d80;
			goto L2da5;

		L2d80:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L2d8b;
			goto L2da5;

		L2d8b:
			this.owner.Services.AssignDot(2, this.processor.PullLocalSignedWord(0x1c) + 80,
				this.processor.PullLocalSignedWord(0x26),
				0);

		L2da5:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da));
			this.processor.RegAcc.HighByte = 0;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x1c));
			if (this.processor.FlagSet.IsEqual) goto L2dc3;
			goto L2deb;

		L2dc3:
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81db));
			this.processor.RegAcc.HighByte = 0;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x26));
			if (this.processor.FlagSet.IsEqual) goto L2dd1;
			goto L2deb;

		L2dd1:
			this.processor.RegAcc.LowByte = 0xff;
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), this.processor.RegAcc.LowByte);

			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L2deb;

			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);

		L2deb:
			this.processor.CompareWord(this.processor.PullLocalWord(0x34), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L2df4;
			goto L2e24;

		L2df4:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L2dfd;
			goto L2e24;

		L2dfd:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2e1f;
			goto L2e24;

		L2e1f:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);

		L2e24:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7));
			this.processor.EmitLocalByte(0x52, this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullLocalByte(0x52));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1146)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2e4c;
			goto L2e55;

		L2e4c:
			this.processor.CompareByte(this.processor.PullLocalByte(0x52), 0x17);
			if (this.processor.FlagSet.IsEqual) goto L2e55;
			goto L2ff4;

		L2e55:
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L2e5e;
			goto L2ff4;

		L2e5e:
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0x4a, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x3c, this.processor.RegAcc.Value16);

		L2e67:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x14));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegSource.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81de));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0x4c, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1146)), 0x1);
			this.processor.RegAcc.Value16 = this.processor.SubtractBorrowWord(this.processor.RegAcc.Value16, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)));
			if (this.processor.FlagSet.IsEqual) goto L2ead;
			goto L2f9d;

		L2ead:
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d4)), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2eb7;
			goto L2eef;

		L2eb7:
			this.processor.CompareWord(this.processor.PullLocalWord(0x34), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L2ec0;
			goto L2eef;

		L2ec0:
			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L2f9d;

			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d4)), 0xc);
			if (this.processor.FlagSet.IsEqual) goto L2ed5;
			goto L2f9d;

		L2ed5:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x14));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L2eef;
			goto L2f9d;

		L2eef:
			this.owner.MapOps.DiscardUnitFromTile(participantIdx,
				this.processor.PullLocalSignedWord(0x14),
				this.processor.PullLocalSignedWord(0x22),
				this.processor.PullLocalSignedWord(0x2a));

			this.owner.MapOps.SituateUnitOnTile(participantIdx,
				this.processor.PullLocalSignedWord(0x14),
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x14));
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x1c);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5), this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x26);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6), this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dd), 0x0);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81dc), 0xff);
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.ScaleIndexToBase(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1146)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2f61;
			goto L2f6e;

		L2f61:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x14));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x1));

		L2f6e:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x14));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegDest.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113c));
			this.processor.CompareWord(this.processor.RegDest.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2f92;
			goto L2f9a;

		L2f92:
			this.processor.RegAcc.Value16 = this.processor.RegDest.Value16;
			this.processor.RegAcc.LowByte = this.processor.DecrementByte(this.processor.RegAcc.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), this.processor.RegAcc.LowByte);

		L2f9a:
			this.processor.EmitLocalWord(0x3c, this.processor.IncrementWord(this.processor.PullLocalWord(0x3c)));

		L2f9d:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4c);
			this.processor.EmitLocalWord(0x14, this.processor.RegAcc.Value16);
			this.processor.IncrementLocalWord(0x4a);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4a), 0x14);
			if (this.processor.FlagSet.IsLess) goto L2faf;
			goto L2ff4;

		L2faf:
			this.processor.CompareWord(this.processor.PullLocalWord(0x14), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L2fb8;
			goto L2ff4;

		L2fb8:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7));
			this.processor.EmitLocalByte(0x52, this.processor.RegAcc.LowByte);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullLocalByte(0x52));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x3c);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1146)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2fe2;
			goto L2e67;

		L2fe2:
			this.processor.CompareByte(this.processor.PullLocalByte(0x52), 0x17);
			if (this.processor.FlagSet.IsNotEqual) goto L2feb;
			goto L2e67;

		L2feb:
			if (participantIdx == 0)
				goto L2e67;

		L2ff4:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2ffd;
			goto L3054;

		L2ffd:
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.CompareWord(this.processor.PullLocalWord(0x48), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L3008;
			goto L3054;

		L3008:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0x48);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd),
				this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd)), this.processor.RegAcc.LowByte));

			this.owner.Helpers.LocateCityAtPosition(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.EmitLocalWord(0x10, this.processor.RegAcc.Value16);

			this.owner.Conquest.SeizeCity(participantIdx, this.processor.PullLocalSignedWord(0x10), 0);

			this.owner.MapOps.TagTileOwned(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

		L3054:
			this.owner.MapOps.MapAssignCityOwner(participantIdx,
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.owner.UnitOps.ShiftUnit(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26),
				participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				1);
			
			this.owner.MapOps.ContainsHut(
				this.processor.PullLocalWord(0x3e),
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L309a;
			goto L30bd;

		L309a:
			this.owner.UnitOps.VisitHut(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c),
				this.processor.PullLocalWord(0x26)] |= 1;

		L30bd:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xa);
			if (this.processor.FlagSet.IsEqual) goto L30c6;
			goto L30fe;

		L30c6:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L30e8;
			goto L30fe;

		L30e8:
			this.owner.UnitOps.VisitEachUnitInStack(participantIdx, this.processor.PullLocalSignedWord(0x32), 0x1560);

		L30fe:
			if (participantIdx != 0)
				goto L3184;

			this.processor.CompareWord(this.processor.PullLocalWord(0x3e), 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L3110;
			goto L3184;

		L3110:
			this.processor.RegSource.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c),
				this.processor.PullLocalWord(0x26)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L313b;
			goto L3184;

		L313b:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, 0x81dd);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16);
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			if (this.processor.FlagSet.IsEqual) goto L3159;
			goto L3184;

		L3159:
			this.owner.Helpers.LocateNearestCity(
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			if (this.owner.World.Towns[this.processor.RegAcc.Value16].ParticipantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c),
					this.processor.PullLocalWord(0x26)];

				this.processor.EmitUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16, this.processor.BitOrByte(this.processor.PullUInt8(this.processor.SegData.Value16, this.processor.RegDest.Value16), this.processor.RegAcc.LowByte));
			}

		L3184:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81dd));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L31a9;
			goto L31c7;

		L31a9:
			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L31c7;

			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L31be;
			goto L3242;

		L31be:
			if (participantIdx == 0)
				goto L3242;

		L31c7:
			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L31dc;

			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x20);
			if (this.processor.FlagSet.IsNotEqual) goto L31dc;
			goto L3220;

		L31dc:
			this.owner.UnitOps.PanMapToPosition(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0x1c),
				this.processor.PullLocalSignedWord(0x26));

			this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, this.processor.PullLocalSignedWord(0x32));

			if (participantIdx == this.owner.World.PersonParticipantIdx)
				goto L321d;

			this.processor.CompareWord(this.processor.PullGlobalWord(0xb278), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L3211;
			goto L321d;

		L3211:
			this.owner.TextPainter.AwaitTime(30);

		L321d:
			goto L3242;

		L3220:
			this.owner.MapOps.RenderUnitOnWorldMap(participantIdx, this.processor.PullLocalSignedWord(0x32));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3236;
			goto L3242;

		L3236:
			this.owner.TextPainter.AwaitTime(10);

		L3242:
			this.processor.EmitLocalWord(0xa, 0x0);
			this.processor.TestBitsByte(this.processor.PullLocalByte(0x40), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L3250;
			goto L3281;

		L3250:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113c)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3272;
			goto L3281;

		L3272:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xfd));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81da), 0xff);

		L3281:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x113c)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L32a3;
			goto L32e1;

		L32a3:
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L32ad;
			goto L32e1;

		L32ad:
			this.owner.UnitOps.TallyUnitsOfTypeInStack(participantIdx, this.processor.PullLocalSignedWord(0x32), 23);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L32c7;
			goto L32e1;

		L32c7:
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d4)), 0xfd));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), 0x0);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113c));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), this.processor.RegAcc.LowByte);

		L32e1:
			goto L3365;


		L3365:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x44);
			goto L3abd;

		L336b:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3375;
			goto L337d;

		L3375:
			this.owner.ArmyPicker.DisplayWhichArmy();

			goto L3c40;

		L337d:
			this.owner.Briefings.DisplayCityStatusReport(this.owner.World.PersonParticipantIdx);

			goto L3c40;

		L338c:
			this.owner.Briefings.RenderUnitCountsChart(this.owner.World.PersonParticipantIdx);
			
			goto L3c40;

		L339b:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L33a5;
			goto L33cf;

		L33a5:
			this.owner.Startup.ConcealMouseCursor();
			
			this.owner.Services.DisplayScreen(2);
			
			this.owner.Libc.ReadKey();

			this.owner.Services.DisplayScreen(0);
			
			this.owner.Startup.DisplayMouseCursor();

			goto L33db;

		L33cf:
			this.owner.Briefings.DisplayMilitaryReport();

		L33db:
			goto L3c40;

		L33de:
			this.owner.Briefings.DisplayAttitudeReport(this.owner.World.PersonParticipantIdx);

			goto L3c40;

		L33ed:
			this.owner.Briefings.DisplayTradeReport(this.owner.World.PersonParticipantIdx);
			
			goto L3c40;

		L33fc:
			this.owner.Briefings.DisplayScienceReport(this.owner.World.PersonParticipantIdx);

			goto L3c40;

		L340b:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3415;
			goto L343a;

		L3415:
			this.processor.EmitLocalWord(0x3c, 0x1);
			goto L3420;

		L341d:
			this.processor.EmitLocalWord(0x3c, this.processor.IncrementWord(this.processor.PullLocalWord(0x3c)));

		L3420:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3c), 0x8);
			if (this.processor.FlagSet.IsLess) goto L3429;
			goto L3437;

		L3429:
			this.owner.RealmInfo.DisplayCivilizationOverview(this.processor.PullLocalSignedWord(0x3c));
			
			goto L341d;

		L3437:
			goto L343f;

		L343a:
			this.owner.Atlas.DisplayWondersOfTheWorld();

		L343f:
			goto L3c40;

		L3442:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L344c;
			goto L3459;

		L344c:
			this.owner.Atlas.DisplayCivilizationGraph();

			this.owner.Replayer.DisplayReplay();

			goto L345e;

		L3459:
			this.owner.Legends.DisplayTopCities();

		L345e:
			goto L3c40;

		L3461:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd806), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L346b;
			goto L3473;

		L346b:
			this.owner.Atlas.DisplayGameFigures();

			goto L3c40;

		L3473:
			this.owner.Founding.DisplayFinalScore(this.owner.World.PersonParticipantIdx, true);

			goto L3c40;

		L348a:
			this.owner.Atlas.DisplayWorldMap(1);

			goto L3c40;

		L349d:
			this.processor.EmitLocalWord(0x24, 0x1);
			goto L34dd;

		L34a5:
			this.processor.EmitLocalWord(0x24, 0x2);
			goto L34dd;

		L34ad:
			this.processor.EmitLocalWord(0x24, 0x3);
			goto L34dd;

		L34b5:
			this.processor.EmitLocalWord(0x24, 0x4);
			goto L34dd;

		L34bd:
			this.processor.EmitLocalWord(0x24, 0x5);
			goto L34dd;

		L34c5:
			this.processor.EmitLocalWord(0x24, 0x6);
			goto L34dd;

		L34cd:
			this.processor.EmitLocalWord(0x24, 0x7);
			goto L34dd;

		L34d5:
			this.processor.EmitLocalWord(0x24, 0x8);

		L34dd:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x24);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitGlobalWord(0xd4cc, this.processor.SumWord(this.processor.PullGlobalWord(0xd4cc), this.processor.RegAcc.Value16));

			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4));
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitGlobalWord(0xd75e, this.processor.SumWord(this.processor.PullGlobalWord(0xd75e), this.processor.RegAcc.Value16));

			this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx,
				this.processor.PullGlobalSignedWord(0xd4cc),
				this.processor.PullGlobalSignedWord(0xd75e));

			goto L3c40;

		L3511:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsEqual) goto L351b;
			goto L351e;

		L351b:
			goto L3ea1;

		L351e:
			goto L3c40;

		L3521:
			this.owner.CityNaming.DisplayNewCityLocation();

			goto L3c40;

		L3529:
			if (this.owner.World.RoundTally != 0) goto L3533;
			goto L353f;

		L3533:
			this.owner.SaveLoad.DisplayStoreGameDialog(0xffff);

		L353f:
			goto L3c40;

		L3542:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.BlockText.DisplayTextDialog(0x1f6a);

			this.owner.TurnHooks.DisplayDialog(0xba06, 64, 80);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L356f;
			goto L3572;

		L356f:
			goto L3c40;

		L3572:
			this.processor.RegSource.Value16 = (ushort)participantIdx;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.owner.World.Participants[participantIdx].RegimeKind = 0;

			this.owner.Libc.CopyText(0xba06, "The ");

			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " are\nrevolting! Citizens\ndemand new govt.\n");

			this.owner.Paper.DisplayNewspaper(-1);
			
			this.owner.NewGameWizard.ImportGovernmentGraphics();

			this.owner.TurnHooks.RepaintMainScreen();

			goto L3c40;

		L35c6:
			this.owner.Services.PrepareMouse();

			goto L3c40;

		L35ce:
			this.owner.Libc.CopyText(0xba06, "Select new Tax rate:\n ");

			this.processor.EmitLocalWord(0x3a, 0x0);
			goto L35e9;

		L35e6:
			this.processor.EmitLocalWord(0x3a, this.processor.IncrementWord(this.processor.PullLocalWord(0x3a)));

		L35e9:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].LevyRatio;
			this.processor.RegAcc.Value16 += (ushort)this.owner.World.Participants[participantIdx].ResearchLevyRatio;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x3a));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L35fe;
			goto L3673;

		L35fe:
			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(10 * (short)this.processor.PullLocalWord(0x3a)), 10));

			this.owner.Libc.AppendText(0xba06, "% Tax, (");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)((this.owner.World.Participants[participantIdx].LevyRatio +
					this.owner.World.Participants[participantIdx].ResearchLevyRatio -
					(short)this.processor.PullLocalWord(0x3a)) * 10), 10));

			this.owner.Libc.AppendText(0xba06, "% Science)\n ");
			goto L35e6;

		L3673:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].LevyRatio;
			this.processor.EmitGlobalWord(0x2f9a, this.processor.RegAcc.Value16);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L369f;
			goto L36b6;

		L369f:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].LevyRatio;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x3a));
			this.owner.World.Participants[participantIdx].ResearchLevyRatio += 
				(short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x3a);
			this.owner.World.Participants[participantIdx].LevyRatio = (short)this.processor.RegAcc.Value16;

			this.owner.TurnHooks.RenderStatusBar();

		L36b6:
			goto L3c40;

		L36b9:
			this.owner.Libc.CopyText(0xba06, "Select new Luxuries rate:\n ");

			this.processor.EmitLocalWord(0x3a, 0x0);
			goto L36d4;

		L36d1:
			this.processor.EmitLocalWord(0x3a, this.processor.IncrementWord(this.processor.PullLocalWord(0x3a)));

		L36d4:
			this.processor.RegAcc.Value16 = 10;
			this.processor.RegAcc.Value16 -= (ushort)this.owner.World.Participants[participantIdx].LevyRatio;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x3a));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L36ea;
			goto L375e;

		L36ea:
			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(10 * (short)this.processor.PullLocalWord(0x3a)), 10));

			this.owner.Libc.AppendText(0xba06, "% Luxuries, (");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(-((short)this.processor.PullLocalWord(0x3a) +
				this.owner.World.Participants[participantIdx].LevyRatio - 10) * 10), 10));

			this.owner.Libc.AppendText(0xba06, "% Science)\n ");
			goto L36d1;

		L375e:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].LevyRatio;
			this.processor.RegAcc.Value16 += (ushort)this.owner.World.Participants[participantIdx].ResearchLevyRatio;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xa);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x2f9a, this.processor.RegAcc.Value16);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L3793;
			goto L37a8;

		L3793:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[participantIdx].LevyRatio;
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x3a));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 10);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.owner.World.Participants[participantIdx].ResearchLevyRatio = (short)this.processor.RegAcc.Value16;

			this.owner.TurnHooks.RenderStatusBar();

		L37a8:
			goto L3c40;

		L37ab:
			this.processor.EmitGlobalWord(0xdcfc, 0x1);

			this.owner.MapOps.FocusMapView(participantIdx,
				this.processor.PullGlobalSignedWord(0xd4cc),
				this.processor.PullGlobalSignedWord(0xd75e));

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.processor.EmitGlobalWord(0xdcfc, 0x0);

			this.owner.MapOps.FocusMapView(participantIdx,
				this.processor.PullGlobalSignedWord(0xd4cc),
				this.processor.PullGlobalSignedWord(0xd75e));

			goto L3c40;

		L37e5:
			this.processor.EmitLocalWord(0x3a, 0x0);
			goto L37f0;

		L37ed:
			this.processor.EmitLocalWord(0x3a, this.processor.IncrementWord(this.processor.PullLocalWord(0x3a)));

		L37f0:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3a), 0x10);
			if (this.processor.FlagSet.IsLess) goto L37f9;
			goto L3837;

		L37f9:
			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x3a));

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(3));

			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1512), this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(3));

			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1514), this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(3));

			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1516), this.processor.RegAcc.Value16);
			goto L37ed;

		L3837:
			this.owner.TurnHooks.DisplayDialog(0x2003, 100, 80);

			goto L3c40;

		L384e:
			this.owner.World.SessionPrefMarkers ^= 0x10;

			this.owner.Libc.CopyText(0xba06, "Sounds ");

			if ((this.owner.World.SessionPrefMarkers & 0x10) != 0)
			{
				this.owner.Libc.AppendText(0xba06, 0x2020);
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, 0x2024);
			}
		
			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			goto L3c40;

		L389b:
			this.owner.HelpSystem.DisplayHelpPopup(0x2029);
			
			this.owner.HelpSystem.DisplayHelpPopup(0x2030);
			
			goto L3c40;

		L38b6:
			// Load Game from the GAME menu leaves the turn without asking
			if (this.processor.PullGlobalWord(0xdc48) == GameLoadAndSave.FetchSessionAsk) goto L38fb;

			this.owner.Libc.CopyText(0xba06, "Are you sure you\nwant to Quit?\n Keep Playing\n Yes, Quit\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L38e2;
			goto L38eb;

		L38e2:
			this.processor.EmitGlobalWord(0xdc48, 0x0);
			goto L3c40;

		L38eb:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdc48), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L38f5;
			goto L38fb;

		L38f5:
			this.processor.EmitGlobalWord(0xdc48, 0x1);

		L38fb:
			this.processor.EmitLocalWord(0x32, 0x80);
			goto L3c40;

		L3903:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x1a3c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L390d;
			goto L3952;

		L390d:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3916;
			goto L3952;

		L3916:
			this.processor.EmitLocalWord(0x4, 0x0);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x12)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L393f;
			goto L394d;

		L393f:
			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

		L394d:
			this.processor.EmitLocalWord(0xe, 0xffff);

		L3952:
			goto L3c40;

		L3955:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x1a3c), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L395f;
			goto L3a38;

		L395f:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3968;
			goto L39a2;

		L3968:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x12)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L398c;
			goto L399a;

		L398c:
			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

		L399a:
			this.processor.EmitLocalWord(0xe, 0xffff);
			goto L3a33;

		L39a2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsNotEqual) goto L39ac;
			goto L39ef;

		L39ac:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));

			VerifyPositionOnScreen(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L39da;
			goto L39ef;

		L39da:
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);
			goto L3a01;

		L39ef:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd4cc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x7);
			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd75e);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x6);
			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

		L3a01:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x12)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L3a25;
			goto L3a33;

		L3a25:
			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0xe),
				this.processor.PullLocalSignedWord(0x12));

		L3a33:
			this.processor.EmitLocalWord(0x4, this.processor.BitXorWord(this.processor.PullLocalWord(0x4), 0x1));

		L3a38:
			goto L3c40;

		L3a3b:
			this.owner.MenuSystem.ManageMenuBar(participantIdx, this.processor.PullLocalSignedWord(0x32), 0);

			goto L3aa4;

		L3a50:
			this.owner.MenuSystem.ManageMenuBar(participantIdx, this.processor.PullLocalSignedWord(0x32), 1);

			goto L3aa4;

		L3a65:
			this.owner.MenuSystem.ManageMenuBar(participantIdx, this.processor.PullLocalSignedWord(0x32), 2);

			goto L3aa4;

		L3a7a:
			this.owner.MenuSystem.ManageMenuBar(participantIdx, this.processor.PullLocalSignedWord(0x32), 3);

			goto L3aa4;

		L3a8f:
			this.owner.MenuSystem.ManageMenuBar(participantIdx, this.processor.PullLocalSignedWord(0x32), 4);

			goto L3aa4;

		ToggleDebugMode:
			this.processor.EmitGlobalWord(0xd806, (ushort)(~this.processor.PullGlobalWord(0xd806)));
			this.processor.EmitLocalWord(0x24, 0x0);
			goto L34dd;

		L3aa4:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd4ca), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L3aae;
			goto L3ab7;

		L3aae:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd4ca);
			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			goto L0d0a;

		L3ab7:
			goto L3c40;

		L3abd:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2300); // Alt + H
			if (this.processor.FlagSet.IsNotEqual) goto L3ac5;
			goto L389b;

		L3ac5:
			if (this.processor.FlagSet.IsLessOrEqual) goto L3aca;
			goto L3b7d;

		L3aca:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3d); // '='
			if (this.processor.FlagSet.IsNotEqual) goto L3ad2;
			goto L35ce;

		L3ad2:
			if (this.processor.FlagSet.IsLessOrEqual) goto L3ad7;
			goto L3b22;

		L3ad7:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1b); // ESC
			if (this.processor.FlagSet.IsNotEqual) goto L3adf;
			goto L3903;

		L3adf:
			if (this.processor.FlagSet.IsLessOrEqual) goto L3ae4;
			goto L3aff;

		L3ae4:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xfffe); // ??
			if (this.processor.FlagSet.IsNotEqual) goto L3aec;
			goto L3542;

		L3aec:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x9); // Tab
			if (this.processor.FlagSet.IsNotEqual) goto L3af4;
			goto L3955;

		L3af4:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xd); // Enter
			if (this.processor.FlagSet.IsNotEqual) goto L3afc;
			goto L3511;

		L3afc:
			goto L3c40;

		L3aff:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x20); // Space
			if (this.processor.FlagSet.IsNotEqual) goto L3b07;
			goto L3511;

		L3b07:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2b); // '+'
			if (this.processor.FlagSet.IsNotEqual) goto L3b0f;
			goto L35ce;

		L3b0f:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2d); // '-'
			if (this.processor.FlagSet.IsNotEqual) goto L3b17;
			goto L36b9;

		L3b17:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2f); // '/'
			if (this.processor.FlagSet.IsNotEqual) goto L3b1f;
			goto L3521;

		L3b1f:
			goto L3c40;

		L3b22:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1000); // Alt + Q
			if (this.processor.FlagSet.IsNotEqual) goto L3b2a;
			goto L38b6;

		L3b2a:
			if (this.processor.FlagSet.IsLessOrEqual) goto L3b2f;
			goto L3b52;

		L3b2f:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3f); // '?'
			if (this.processor.FlagSet.IsNotEqual) goto L3b37;
			goto L3521;

		L3b37:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x53); // 'S'
			if (this.processor.FlagSet.IsNotEqual) goto L3b3f;
			goto L3529;

		L3b3f:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5f); // '_'
			if (this.processor.FlagSet.IsNotEqual) goto L3b47;
			goto L36b9;

		L3b47:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x74); // 't'
			if (this.processor.FlagSet.IsNotEqual) goto L3b4f;
			goto L37ab;

		L3b4f:
			goto L3c40;

		L3b52:
			if (this.processor.RegAcc.Value16 == 0x2000) // Alt + D
				goto ToggleDebugMode;

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1100); // Alt + W
			if (this.processor.FlagSet.IsNotEqual) goto L3b5a;
			goto L3a7a;

		L3b5a:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1300); // Alt + R
			if (this.processor.FlagSet.IsNotEqual) goto L3b62;
			goto L37e5;

		L3b62:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1800); // Alt + O
			if (this.processor.FlagSet.IsNotEqual) goto L3b6a;
			goto L3a50;

		L3b6a:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1e00); // Alt + A
			if (this.processor.FlagSet.IsNotEqual) goto L3b72;
			goto L3a65;

		L3b72:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2200); // Alt + G
			if (this.processor.FlagSet.IsNotEqual) goto L3b7a;
			goto L3a3b;

		L3b7a:
			goto L3c40;

		L3b7d:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4100); // F7
			if (this.processor.FlagSet.IsNotEqual) goto L3b85;
			goto L340b;

		L3b85:
			if (this.processor.FlagSet.IsLessOrEqual) goto L3b8a;
			goto L3bdd;

		L3b8a:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3b00); // F1
			if (this.processor.FlagSet.IsNotEqual) goto L3b92;
			goto L336b;

		L3b92:
			if (this.processor.FlagSet.IsLessOrEqual) goto L3b97;
			goto L3bb2;

		L3b97:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2e00); // Alt + C
			if (this.processor.FlagSet.IsNotEqual) goto L3b9f;
			goto L3a8f;

		L3b9f:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2f00); // Alt + V
			if (this.processor.FlagSet.IsNotEqual) goto L3ba7;
			goto L384e;

		L3ba7:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3200); // Alt + M
			if (this.processor.FlagSet.IsNotEqual) goto L3baf;
			goto L35c6;

		L3baf:
			goto L3c40;

		L3bb2:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3c00); // F2
			if (this.processor.FlagSet.IsNotEqual) goto L3bba;
			goto L338c;

		L3bba:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3d00); // F3
			if (this.processor.FlagSet.IsNotEqual) goto L3bc2;
			goto L339b;

		L3bc2:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3e00); // F4
			if (this.processor.FlagSet.IsNotEqual) goto L3bca;
			goto L33de;

		L3bca:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x3f00); // F5
			if (this.processor.FlagSet.IsNotEqual) goto L3bd2;
			goto L33ed;

		L3bd2:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4000); // F6
			if (this.processor.FlagSet.IsNotEqual) goto L3bda;
			goto L33fc;

		L3bda:
			goto L3c40;

		L3bdd:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4838); // Shift + Up
			if (this.processor.FlagSet.IsNotEqual) goto L3be5;
			goto L349d;

		L3be5:
			if (this.processor.FlagSet.IsLessOrEqual) goto L3bea;
			goto L3c0d;

		L3bea:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4200); // F8
			if (this.processor.FlagSet.IsNotEqual) goto L3bf2;
			goto L3442;

		L3bf2:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4300); // F9
			if (this.processor.FlagSet.IsNotEqual) goto L3bfa;
			goto L3461;

		L3bfa:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4400); // F10
			if (this.processor.FlagSet.IsNotEqual) goto L3c02;
			goto L348a;

		L3c02:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4737); // Shift + Home
			if (this.processor.FlagSet.IsNotEqual) goto L3c0a;
			goto L34d5;

		L3c0a:
			goto L3c40;

		L3c0d:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4939); // Shift + PageUp
			if (this.processor.FlagSet.IsNotEqual) goto L3c15;
			goto L34a5;

		L3c15:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4b34); // Shift + Left
			if (this.processor.FlagSet.IsNotEqual) goto L3c1d;
			goto L34cd;

		L3c1d:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4d36); // Shift + Right
			if (this.processor.FlagSet.IsNotEqual) goto L3c25;
			goto L34ad;

		L3c25:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4f31); // Shift + End
			if (this.processor.FlagSet.IsNotEqual) goto L3c2d;
			goto L34c5;

		L3c2d:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5032); // Shift + Down
			if (this.processor.FlagSet.IsNotEqual) goto L3c35;
			goto L34bd;

		L3c35:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5133); // Shift + PageDown
			if (this.processor.FlagSet.IsNotEqual) goto L3c40;
			goto L34b5;

		L3c40:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsLess) goto L3c4a;
			goto L3c77;

		L3c4a:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L3c64;
			goto L3c77;

		L3c64:
			this.processor.CompareWord(this.processor.PullLocalWord(0x28), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3c6d;
			goto L3c77;

		L3c6d:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdc48), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3c77;
			goto L047f;

		L3c77:
			this.processor.CompareWord(this.processor.PullLocalWord(0x32), 0x80);
			if (this.processor.FlagSet.IsLess) goto L3c81;
			goto L3e2c;

		L3c81:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsEqual) goto L3c9b;
			goto L3c9e;

		L3c9b:
			goto L0149;

		L3c9e:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d8)), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L3cb8;
			goto L3cc0;

		L3cb8:
			this.processor.EmitLocalWord(0x1e, 0x0);
			goto L3e2c;

		L3cc0:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113c)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3ce2;
			goto L3d71;

		L3ce2:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L3cff;
			goto L3d19;

		L3cff:
			this.owner.UnitOps.TallyUnitsOfTypeInStack(participantIdx, this.processor.PullLocalSignedWord(0x32), 23);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3d19;
			goto L3d39;

		L3d19:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToSource((ushort)participantIdx);
			this.processor.AddScaledToSource(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113c));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d9), this.processor.RegAcc.LowByte);

		L3d39:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d9),
				this.processor.DecrementByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d9))));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d9)), 0x0);
			if (this.processor.FlagSet.IsLess) goto L3d57;
			goto L3d71;

		L3d57:
			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			DisplayHelpForHuman(0x2070);

		L3d71:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x32));
			this.processor.ScaleIndexToSource(0x600, (ushort)participantIdx);
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x10);
			if (this.processor.FlagSet.IsEqual) goto L3d8b;
			goto L3e2c;

		L3d8b:
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L3e2c;

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(2));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3daa;
			goto L3e2c;

		L3daa:
			this.processor.EmitLocalWord(0x38, 0x0);
			this.processor.EmitLocalWord(0x3c, 0x1);
			goto L3dba;

		L3db7:
			this.processor.EmitLocalWord(0x3c, this.processor.IncrementWord(this.processor.PullLocalWord(0x3c)));

		L3dba:
			this.processor.CompareWord(this.processor.PullLocalWord(0x3c), 0x8);
			if (this.processor.FlagSet.IsLessOrEqual) goto L3dc3;
			goto L3e09;

		L3dc3:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x3c);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.ScaleIndex(0x600, (ushort)participantIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x32));
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)) + this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)) + this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L3dfe;
			goto L3e06;

		L3dfe:
			this.processor.EmitLocalWord(0x38, 0x1);
			goto L3e09;

		L3e06:
			goto L3db7;

		L3e09:
			this.processor.CompareWord(this.processor.PullLocalWord(0x38), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3e12;
			goto L3e2c;

		L3e12:
			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));
			
			DisplayHelpForHuman(0x2076);

		L3e2c:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L3e3d;
			goto L3e4b;

		L3e3d:
			RenderUnitInfo(participantIdx,
				this.processor.PullLocalWord(0x32));

		L3e4b:
			goto L0149;

		L3e4e:
			this.processor.CompareWord(this.processor.PullLocalWord(0x1e), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3e57;
			goto L3e61;

		L3e57:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdc48), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3e61;
			goto L013c;

		L3e61:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xdc48), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3e6b;
			goto L3ea1;

		L3e6b:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)participantIdx;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L3e7c;
			goto L3ea1;

		L3e7c:
			this.processor.CompareWord(this.processor.PullLocalWord(0x6), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3e85;
			goto L3e8f;

		L3e85:
			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L3e8f;
			goto L3ea1;

		L3e8f:
			this.processor.EmitLocalWord(0x32, 0x80);
			this.processor.EmitLocalWord(0x1e, 0x0);
			this.processor.EmitLocalWord(0x8, 0x1);
			goto L043b;

		L3ea1:
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				goto L3ece;

			this.owner.Startup.ConcealMouseCursor();

			this.owner.TurnHooks.FloodBoxWithShadow(0, 97, 80, 103);

			this.owner.Startup.DisplayMouseCursor();

		L3ece:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("HandlePlayerTurn");
		}

		/// <summary>
		/// The start of the turn of the player: 0x6b90 the player, 0x20 the players who were played by humans, 0xe -1, 0x4 0;
		/// for the human player only he is played by a human now, with the help about the turns 20 / 60 and 40 / 80. Every unit
		/// gets its moves (3 per tile of its type, ships 3 more with Magellan's Expedition or the Lighthouse), unless 0xdf60 is 1.
		/// Then 0x6, 0x8, 0x28, 0x2e are 0 and 0x46 is 0x7f. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void PrepareTurn(short participantIdx)
		{
			this.processor.RegAcc.Value16 = (ushort)participantIdx;
			this.processor.EmitGlobalWord(0x6b90, (ushort)participantIdx);
			this.processor.EmitLocalWord(0x20, (ushort)this.owner.World.ParticipantMarkers);
			this.processor.EmitLocalWord(0xe, 0xffff);
			this.processor.EmitLocalWord(0x4, 0);

			short person = this.owner.World.PersonParticipantIdx;

			if (participantIdx == person)
			{
				this.owner.World.ParticipantMarkers = (short)(1 << (person & 0xff));

				if (this.owner.World.RoundTally == 20 || this.owner.World.RoundTally == 60)
					this.owner.HelpSystem.DisplayHelpPopup(0x1e60);

				if (this.owner.World.RoundTally == 40 || this.owner.World.RoundTally == 80)
					this.owner.HelpSystem.DisplayHelpPopup(0x1e67);
			}

			for (this.processor.EmitLocalWord(0x32, 0); (short)this.processor.PullLocalWord(0x32) < UnitMemory.TroopTally;
				this.processor.EmitLocalWord(0x32, (ushort)(this.processor.PullLocalWord(0x32) + 1)))
			{
				short troopIdx = (short)this.processor.PullLocalWord(0x32);

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff || this.processor.PullGlobalWord(0xdf60) == 1)
					continue;

				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps,
					(byte)(3 * this.troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally)));

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits &&
					(this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 4) != 0 || this.owner.CityOps.PlayerEnjoysWonderEffect(participantIdx, 8) != 0))
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps,
						(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) + 3));
				}
			}

			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0x6, 0);
			this.processor.EmitLocalWord(0x8, 0);
			this.processor.EmitLocalWord(0x28, 0);
			this.processor.EmitLocalWord(0x2e, 0);
			this.processor.EmitLocalWord(0x46, 0x7f);
		}

		/// <summary>
		/// The start of the move of the unit 0x32: none (false) for an empty slot, a sentried or fortified unit (status bits 0, 3)
		/// or one without moves. A unit of a kind from 0x1c becomes a militia; in the first pass (0x8 = 0) the unit is shifted in
		/// place (ShiftUnit). A unit that was fortifying (bit 2) is fortified now and has no moves: a computer unit in a city of
		/// size 3 or more makes it its home city, and in its home city without a foreign unit around it is kept in the list of
		/// the defenders of the city (false). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool StartUnitMove(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			// (SI as the original leaves it: the player's units, or the unit when it goes on or was fortifying)
			ushort player = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			ushort unit = (ushort)(player + troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = player;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff)
				return false;

			if (this.troops.TroopKind(participantIdx, troopIdx) >= 0x1c)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Kind, 1);

			if (this.processor.PullLocalWord(0x8) == 0)
			{
				// (ShiftUnit keeps SI on the stack, where ShiftAIUnit later finds it)
				this.processor.RegSource.Value16 = unit;
				this.owner.UnitOps.ShiftUnit(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert), participantIdx, troopIdx, 0);
			}

			this.processor.RegSource.Value16 = player;

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 9) != 0 ||
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) == 0)
			{
				return false;
			}

			this.processor.RegSource.Value16 = unit;

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 4) == 0)
				return true;

			// Fortifying: fortified
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfb) | 8));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIdx, troopIdx);

			sbyte horizLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz);
			sbyte vertLoc = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert);

			if (participantIdx != this.owner.World.PersonParticipantIdx && (this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				this.processor.EmitLocalWord(0x3a, this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc));

				if ((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x3a)].TrueExtent >= 3)
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, this.processor.PullLocalByte(0x3a));
			}

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				this.processor.EmitLocalWord(0x3a, this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc));

				if ((ushort)this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown) == this.processor.PullLocalWord(0x3a) &&
					this.owner.UnitOps.ContainsAdjacentForeignUnit(participantIdx, horizLoc, vertLoc) == 0)
				{
					this.owner.UnitOps.KeepUnitsInCityList((short)this.processor.PullLocalWord(0x3a));
				}
			}

			return false;
		}

		/// <summary>
		/// A unit of the human player without orders (status bits 1, 6, 7) that is not on the map view (0xd4cc / 0xd75e, 15 x 11
		/// tiles) waits for the second pass (false, 0x1e = 0) while 0x2e is 0; else the view moves to it (after a short wait
		/// unless 0xb278 is 1), 0x46 remembers the unit before it and 0x2e is 0 again (the unit panel in the first turn).
		/// The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool FocusOnUnit(short participantIdx)
		{
			if (participantIdx != this.owner.World.PersonParticipantIdx)
				return true;

			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xc2) != 0)
				return true;

			// On the map view (the unsigned comparisons of the original)
			ushort horizLoc = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz);
			this.processor.RegDest.Value16 = horizLoc;
			ushort viewHoriz = this.processor.PullGlobalWord(0xd4cc);

			if (viewHoriz < horizLoc)
			{
				this.processor.EmitLocalWord(0x4e, this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert));
				ushort viewVert = this.processor.PullGlobalWord(0xd75e);

				if (this.processor.PullLocalWord(0x4e) > viewVert && (ushort)(viewHoriz + 0xe) >= horizLoc &&
					(ushort)(viewVert + 0xa) > this.processor.PullLocalWord(0x4e))
				{
					return true;
				}
			}

			if (this.processor.PullLocalWord(0x2e) == 0)
			{
				this.processor.EmitLocalWord(0x1e, 0);
				return false;
			}

			if (this.processor.PullGlobalWord(0xb278) != 1)
				this.owner.TextPainter.AwaitTime(30);

			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			this.owner.MapOps.FocusMapView(participantIdx,
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz) - 7,
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert) - 6);

			this.processor.EmitLocalWord(0x46, (ushort)((this.processor.PullLocalWord(0x32) - 1) & 0x7f));
			this.processor.EmitLocalWord(0x2e, 0);

			if (this.owner.World.RoundTally == 0)
				RenderUnitInfo(participantIdx, this.processor.PullLocalWord(0x32));

			return true;
		}

		/// <summary>
		/// The order of a computer unit (0x44) is the answer of ShiftAIUnit: with one the unit forgets its destination; after
		/// more than 4 tries (0xa) it is 0x20 and the unit forgets its heading and destination. The locals of HandlePlayerTurn
		/// are used as in the original, SI as the original leaves it.
		/// </summary>
		private void ObtainAIOrder(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.EmitLocalWord(0x44, this.owner.Bot.ShiftAIUnit(participantIdx, troopIdx));

			if (this.processor.PullLocalWord(0x44) != 0)
			{
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			}

			this.processor.EmitLocalWord(0xa, (ushort)(this.processor.PullLocalWord(0xa) + 1));

			if ((short)this.processor.PullLocalWord(0xa) > 4)
			{
				this.processor.EmitLocalWord(0x44, 0x20);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			}
		}

		/// <summary>
		/// A unit of a player played by a human with orders (status bits 1, 6, 7) or a destination goes on with them (true, the
		/// key 0x44): 'r' for a destination, 'i' / 'm' for settlers / others with bit 6, 'm' with bit 7 ('f' with bits 6 and 7),
		/// 'p' with bits 7 and 1, 'h' for a plane. False: the human chooses. The locals of HandlePlayerTurn are used as in the
		/// original, SI as the original leaves it.
		/// </summary>
		private bool ObtainStandingOrder(short participantIdx)
		{
			if ((short)this.processor.PullLocalWord(0x32) >= UnitMemory.TroopTally)
				return false;

			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xc2) == 0 &&
				this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == 0xff)
			{
				return false;
			}

			this.processor.EmitLocalWord(0x44, 0x72);

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x40) != 0)
				this.processor.EmitLocalWord(0x44, (ushort)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0 ? 0x6d : 0x69));

			this.processor.EmitLocalByte(0x4e, this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status));

			if ((this.processor.PullLocalByte(0x4e) & 0x80) != 0)
			{
				this.processor.EmitLocalWord(0x44, 0x6d);

				if ((this.processor.PullLocalByte(0x4e) & 0x40) != 0)
					this.processor.EmitLocalWord(0x44, 0x66);

				if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 2) != 0)
					this.processor.EmitLocalWord(0x44, 0x70);
			}

			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
				this.processor.EmitLocalWord(0x44, 0x68);

			return true;
		}

		/// <summary>Where HandlePlayerTurn goes on after the human chose (TakeHumanOrder)</summary>
		private enum HumanOrder
		{
			/// <summary>The order 0x44 of the unit, or none (L0d00)</summary>
			Order,

			/// <summary>A menu chose the order 0x44 of the unit (L0d0a)</summary>
			MenuOrder,

			/// <summary>A menu chose without a unit (L3365)</summary>
			MenuWithoutUnit,

			/// <summary>The menu bar chose nothing, or a click picked another unit (L3c40)</summary>
			NextLook,

			/// <summary>The palace area clicked without a unit: the end of the turn (L3ea1)</summary>
			EndOfTurn,
		}

		/// <summary>
		/// The human chooses what the unit 0x32 does: its panel (with the help of the first turn and for settlers), the
		/// blinking unit, then a key (0x44; Enter with a marked tile 0x4 acts as a click on it) or a click: the menu bar, the
		/// world map (moves the view), the palace, a click held (2) on the map is a destination (a neighbouring tile, also across
		/// the date line) or, on a seen tile, the Civilopedia of its terrain; a click (1) with a GoTo pointer (0x36) picks a
		/// destination the unit may reach, on a city of the human the city screen, on an own unit that unit (with its panel when
		/// everything is shown, 0xd806), else the view moves there. At the end 0x44 is -1 and the keys wait unless 0xc.
		/// The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private HumanOrder TakeHumanOrder(short participantIdx)
		{
			short person = this.owner.World.PersonParticipantIdx;

			RenderUnitInfo(participantIdx, this.processor.PullLocalWord(0x32));

			if (this.owner.World.RoundTally == 0 && this.processor.PullLocalWord(0x8) == 0)
			{
				this.processor.RegBase.Value16 = (ushort)(participantIdx << 1);
				this.owner.Libc.CopyText(this.processor.PullGlobalWord(0x30b8),
					this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

				this.owner.HelpSystem.DisplayHelpPopup(0x1e6e);

				this.processor.EmitLocalWord(0x8, 1);
			}

			if (this.processor.PullLocalWord(0x32) < UnitMemory.TroopTally)
			{
				short troopIdx = (short)this.processor.PullLocalWord(0x32);
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0 && this.processor.PullLocalWord(0x8) == 0)
				{
					this.owner.HelpSystem.DisplayTileInfo(participantIdx, this.processor.PullLocalWord(0x32));
					this.processor.EmitLocalWord(0x8, 1);
				}
			}

			this.owner.UnitOps.ChoreographUnitMove(participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				this.processor.PullLocalWord(0xe),
				this.processor.PullLocalWord(0x12));

			if (this.processor.PullLocalWord(0x32) != 0x80)
				this.processor.EmitLocalWord(0x6, 1);

			if (this.owner.GlobDb3a == 0)
			{
				this.processor.EmitLocalWord(0x44, this.owner.Dialogs.ObtainNavigationKey());

				if (this.processor.PullLocalWord(0x44) != 0xd || this.processor.PullLocalWord(0x4) == 0)
					return HumanOrder.Order;

				// Enter on the marked tile: a click there
				this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe);
				this.processor.EmitLocalWord(0x1c, this.processor.PullLocalWord(0xe));
				this.processor.EmitLocalWord(0x26, this.processor.PullLocalWord(0x12));
				this.owner.GlobDb3c = 0x50;
				this.owner.GlobDb3e = 0x8;
				this.owner.GlobDb3a = 0x1;

				return TakeClick(participantIdx);
			}

			LocateClickedTile();

			if ((short)this.owner.GlobDb3e < 8)
			{
				// The menu bar
				this.owner.MenuSystem.ManageMenuBar(participantIdx, this.processor.PullLocalSignedWord(0x32), -1);

				AwaitForKey();

				if (this.processor.PullGlobalWord(0xd4ca) == 0xffff)
					return HumanOrder.NextLook;

				this.processor.EmitLocalWord(0x44, this.processor.PullGlobalWord(0xd4ca));

				return (short)this.processor.PullLocalWord(0x32) < 0x80 ? HumanOrder.MenuOrder : HumanOrder.MenuWithoutUnit;
			}

			if ((short)this.owner.GlobDb3c < 0x50)
			{
				if ((short)this.owner.GlobDb3e < 0x3a)
				{
					// The world map: the view moves there
					this.owner.Routing.TunePosition(
						(short)this.owner.GlobDb3c + this.processor.PullGlobalSignedWord(0x6ed6) - 7);

					this.owner.MapOps.FocusMapView(participantIdx,
						(short)this.processor.RegAcc.Value16,
						this.owner.Helpers.VerifyValueSpan((short)this.owner.GlobDb3e + this.processor.PullGlobalSignedWord(0x70ea) - 14, 0, 49));
				}
				else if ((short)this.owner.GlobDb3e < 0x48)
				{
					ShowPalace();
				}
				else if (this.processor.PullLocalWord(0x32) == 0x80)
				{
					return HumanOrder.EndOfTurn;
				}

				this.owner.GlobDb3a = 0;
			}

			if (this.owner.GlobDb3a == 2 && (short)this.processor.PullLocalWord(0x32) < 0x80)
				TakeHeldClick(participantIdx);

			if (this.owner.GlobDb3a == 2 &&
				(((sbyte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c), this.processor.PullLocalWord(0x26)] & (1 << (person & 0xff))) != 0 ||
					this.processor.PullGlobalWord(0xd806) != 0))
			{
				// The Civilopedia of the terrain of a seen tile
				this.owner.Startup.ConcealMouseCursor();

				this.owner.MapOps.ObtainTerrainType(
					this.processor.PullLocalSignedWord(0x1c),
					this.processor.PullLocalSignedWord(0x26));

				this.owner.Encyclopedia.DisplayCivilopediaRecord(this.processor.RegAcc.Value16, 3);

				this.owner.TurnHooks.RepaintMainScreen();

				this.owner.Startup.DisplayMouseCursor();
			}

			return TakeClick(participantIdx);
		}

		/// <summary>The tile clicked on the map view: 0x1c / 0x26</summary>
		private void LocateClickedTile()
		{
			this.owner.Routing.TunePosition(
				(((short)this.owner.GlobDb3c - 80) / 16) + this.processor.PullGlobalSignedWord(0xd4cc));

			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)(this.owner.GlobDb3e - 8);
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);
			this.processor.RegAcc.Value16 = (ushort)(this.processor.RegAcc.Value16 + this.processor.PullGlobalWord(0xd75e));
			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);
		}

		/// <summary>The palace of the human player, until a key or a click</summary>
		private void ShowPalace()
		{
			this.owner.Startup.ConcealMouseCursor();

			this.owner.Throne.ImportPalaceGraphics(0);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, this.processor.RegBase.Value16, 0x0);

			this.owner.Helpers.HaltHueCycling();

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.owner.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x1e79, 1);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 0x140, 0xc8,
				this.processor.PullGlobalWord(0xaa),
				0, 0);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.TurnHooks.RepaintMainScreen();

			this.owner.Helpers.LaunchHueCycling();
		}

		/// <summary>
		/// A click held on the map (GlobDb3a 2): when the button is let go on a tile next to the unit 0x32 (also across the date
		/// line, 0x18) it is the destination of the unit (GlobDb3a 0, the normal pointer), else the click stays held (2).
		/// </summary>
		private void TakeHeldClick(short participantIdx)
		{
			do
			{
				this.owner.Startup.ObtainMouseState();
			}
			while (this.owner.GlobDb3a != 0);

			LocateClickedTile();

			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);

			this.owner.Helpers.MergeCoordinates(
				(short)(this.processor.PullLocalSignedWord(0x1c) - this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz)),
				(short)(this.processor.PullLocalSignedWord(0x26) - this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert)));

			this.processor.EmitLocalWord(0x18, this.processor.RegAcc.Value16);

			// Across the date line
			if (this.owner.Libc.AbsoluteValue((short)(this.processor.PullLocalWord(0x1c) - this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz))) == 0x4f &&
				this.owner.Libc.AbsoluteValue((short)(this.processor.PullLocalWord(0x26) - this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert))) <= 1)
			{
				this.processor.EmitLocalWord(0x18, 1);
			}

			if (this.processor.PullLocalWord(0x18) != 1)
			{
				this.owner.GlobDb3a = 2;
				return;
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, this.processor.PullLocalByte(0x1c));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, this.processor.PullLocalByte(0x26));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.owner.GlobDb3a = 0;
			this.processor.EmitLocalWord(0x36, 0);

			this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullGlobalWord(0xd4dc));
		}

		/// <summary>
		/// A click (GlobDb3a 1) on the tile 0x1c / 0x26, see TakeHumanOrder; then 0x44 is -1 and the keys wait unless 0xc.
		/// </summary>
		private HumanOrder TakeClick(short participantIdx)
		{
			short person = this.owner.World.PersonParticipantIdx;
			short horizLoc = (short)this.processor.PullLocalWord(0x1c);
			short vertLoc = (short)this.processor.PullLocalWord(0x26);

			if (this.owner.GlobDb3a == 1)
			{
				if (this.processor.PullLocalWord(0x36) != 0 && (short)this.processor.PullLocalWord(0x32) < 0x80)
				{
					// The GoTo pointer: a destination the unit may reach (any for 2)
					short troopIdx = (short)this.processor.PullLocalWord(0x32);
					this.processor.EmitLocalWord(0x3e, this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc));
					bool ocean = this.processor.PullLocalWord(0x3e) == MapRules.OceanTerrain;
					bool reachable;

					if (this.processor.PullLocalWord(0x36) == 2)
						reachable = true;
					else if (((sbyte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x1c), this.processor.PullLocalWord(0x26)] & (1 << (person & 0xff))) == 0)
						reachable = false;
					else
					{
						ushort group = this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup);

						// A ship on the sea, a land unit on land, any unit into a city, a plane anywhere
						reachable = (group == UnitKindTable.SeaUnits && ocean) ||
							(group == UnitKindTable.LandUnits && !ocean) ||
							(this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0 ||
							this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.AirUnits;
					}

					if (reachable)
					{
						this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, this.processor.PullLocalByte(0x1c));
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelVert, this.processor.PullLocalByte(0x26));
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
					}

					this.processor.EmitLocalWord(0x36, 0);

					this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullGlobalWord(0xd4dc));

					ForgetMarkedTile(participantIdx);
				}
				else if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0 &&
					(this.owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc) == (ushort)person || this.processor.PullGlobalWord(0xd806) != 0))
				{
					// A city of the human (any with everything shown): the city screen
					this.processor.EmitLocalWord(0x48, this.owner.MapOps.ObtainTileOwnerBits(horizLoc, vertLoc));
					this.processor.EmitLocalWord(0x3c, this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc));

					this.owner.Advisors.RenewCityScreen(this.processor.PullLocalSignedWord(0x3c));

					this.owner.TurnHooks.RenderStatusBar();

					this.processor.EmitLocalWord(0x1e, 0);
					ForgetMarkedTile(participantIdx);
				}
				else
				{
					this.processor.EmitLocalWord(0x44, this.owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc));

					if (this.processor.PullGlobalWord(0xd806) != 0 && this.processor.PullLocalWord(0x44) != 0xffff)
					{
						this.owner.ArmyPicker.DisplayUnitInfo(
							this.processor.PullGlobalSignedWord(0xd7f0),
							this.processor.PullLocalSignedWord(0x44));
					}

					if (this.processor.PullLocalWord(0x44) != 0xffff && participantIdx == this.processor.PullGlobalSignedWord(0xd7f0))
					{
						// An own unit (from its stack): it is the unit now, unless sentried or fortified
						this.processor.EmitLocalWord(0x3a, this.owner.UnitOps.DisplayBattleScreen(this.processor.PullGlobalSignedWord(0xd7f0),
							this.processor.PullLocalSignedWord(0x44)));

						short chosen = (short)this.processor.PullLocalWord(0x3a);
						this.processor.RegBase.Value16 = (ushort)(chosen * UnitMemory.TroopStride);
						this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

						if ((this.troops.Pull(participantIdx, chosen, UnitMemory.Status) & 9) == 0)
						{
							this.processor.EmitLocalWord(0x32, this.processor.PullLocalWord(0x3a));
							this.processor.EmitLocalWord(0x46, (ushort)((this.processor.PullLocalWord(0x3a) - 1) & 0x7f));
							this.processor.EmitLocalWord(0x2e, 0);
							this.processor.EmitLocalWord(0x1e, 0);
							ForgetMarkedTile(participantIdx);

							return HumanOrder.NextLook;
						}
					}
					else
					{
						this.owner.MapOps.FocusMapView(participantIdx,
							this.processor.PullLocalSignedWord(0x1c) - 7,
							this.processor.PullLocalSignedWord(0x26) - 6);
					}
				}
			}

			this.processor.EmitLocalWord(0x44, 0xffff);

			if (this.processor.PullLocalWord(0xc) != 0)
				this.processor.EmitLocalWord(0xc, 0);
			else
				AwaitForKey();

			return HumanOrder.Order;
		}

		/// <summary>The marked tile (0x4, at 0xe / 0x12) is forgotten and repainted when the player sees it</summary>
		private void ForgetMarkedTile(short participantIdx)
		{
			if (this.processor.PullLocalWord(0x4) == 0)
				return;

			this.processor.EmitLocalWord(0x4, 0);

			if (((sbyte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe), this.processor.PullLocalWord(0x12)] & (1 << (byte)participantIdx)) != 0)
			{
				this.owner.MapOps.RepaintMapTile(
					this.processor.PullLocalSignedWord(0xe),
					this.processor.PullLocalSignedWord(0x12));
			}

			this.processor.EmitLocalWord(0xe, 0xffff);
		}

		/// <summary>
		/// Before an order: a unit with a destination (or no unit, 0x32 from 128) goes on with GoTo (CarryOutGoto gives the
		/// order 0x44), else it forgets its heading; 0x2c / 0x30 are its place (0 without a unit). The locals of HandlePlayerTurn
		/// are used as in the original, SI as the original leaves it.
		/// </summary>
		private void PrepareOrder(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);

			if (this.processor.PullLocalWord(0x32) > 127 || this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) != 0xff)
			{
				this.processor.EmitLocalWord(0x44, this.owner.Routing.CarryOutGoto(participantIdx, troopIdx));
			}
			else
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			}

			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + this.processor.PullLocalWord(0x32) * UnitMemory.TroopStride);

			if (this.processor.PullLocalWord(0x32) < UnitMemory.TroopTally)
			{
				this.processor.EmitLocalWord(0x2c, this.troops.Pull(participantIdx, troopIdx, UnitMemory.Horiz));
				this.processor.EmitLocalWord(0x30, this.troops.Pull(participantIdx, troopIdx, UnitMemory.Vert));
			}
			else
			{
				this.processor.EmitLocalWord(0x2c, 0);
				this.processor.EmitLocalWord(0x30, 0);
			}
		}

		/// <summary>'D': the unit is disbanded</summary>
		private void DisbandUnit(short participantIdx)
		{
			this.troops.Assign(participantIdx, (short)this.processor.PullLocalWord(0x32), UnitMemory.LeftoverSteps, 0);

			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));
		}

		/// <summary>
		/// 'P': a land unit (not a diplomat, caravan or fighter) outside a city with moves left pillages the tile: irrigation and
		/// mine, else a railroad, else a road go (a human seen tile is remembered, the treaty with the owner of the nearest city
		/// may break and he gets the place as a strategic location); else the help message. The locals of HandlePlayerTurn are
		/// used as in the original.
		/// </summary>
		private void PillageTile(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			short horizLoc = (short)this.processor.PullLocalWord(0x2c);
			short vertLoc = (short)this.processor.PullLocalWord(0x30);

			bool may = (this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) == 0;

			if (may)
			{
				this.processor.EmitLocalByte(0x4e, this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind));
				may = (sbyte)this.processor.PullLocalByte(0x4e) < 0x1a && this.processor.PullLocalByte(0x4e) != 0xe;
			}

			if (!may)
			{
				DisplayHelpForHuman(0x1e83);
				return;
			}

			this.processor.EmitLocalWord(0x10, this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc));

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) == 0)
				return;

			// (no when the human keeps the treaty)
			if (this.owner.Combat.AffirmBreakTreaty(this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx) == 0xffff)
				return;

			this.processor.EmitLocalWord(0x3a, this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc));

			if ((this.processor.PullLocalByte(0x3a) & 6) != 0)
				this.owner.MapOps.DiscardTileImprovements(6, horizLoc, vertLoc);
			else if ((this.processor.PullLocalByte(0x3a) & 0x10) != 0)
				this.owner.MapOps.DiscardTileImprovements(0x10, horizLoc, vertLoc);
			else
				this.owner.MapOps.DiscardTileImprovements(8, horizLoc, vertLoc);

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx == this.owner.World.PersonParticipantIdx)
				this.owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);

			RepaintTileIfVisible(this.processor.PullLocalWord(0x2c), this.processor.PullLocalWord(0x30));

			this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);

			this.owner.Politics.WipeDiplomacyFlags(this.owner.World.PersonParticipantIdx,
				this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx,
				2);

			// (the player of the strategic location is DI as the original leaves it)
			if (participantIdx != this.owner.World.Towns[this.processor.PullLocalWord(0x10)].ParticipantIdx)
				this.owner.Bot.SendUnitToStrategicLocation((short)this.processor.RegDest.Value16, horizLoc, vertLoc, 1, 4);

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
		}

		/// <summary>Space: the unit does nothing this turn</summary>
		private void SkipUnit(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
		}

		/// <summary>'s': the unit is sentried</summary>
		private void SentryUnit(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 1));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIdx, troopIdx);
		}

		/// <summary>
		/// 'u': a ship that carries units (or a carrier) in a stack wakes the units on board; the best defender of the stack
		/// is the next unit (0x32 one before it). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void UnloadUnit(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.EmitLocalByte(0x4e, this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind));

			if (UnitKindTable.Field(this.processor, (sbyte)this.processor.PullLocalByte(0x4e), UnitKindTable.FerryRoom) == 0 &&
				this.processor.PullLocalByte(0x4e) != UnitKindTable.Carrier)
			{
				return;
			}

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.FollowingInStack) == 0xff)
				return;

			this.owner.UnitOps.VisitEachUnitInStack(participantIdx, troopIdx, 0x14f6);

			this.processor.EmitLocalWord(0x1e, 0);
			this.processor.EmitLocalWord(0x28, 1);

			this.processor.EmitLocalWord(0x32, (ushort)(LocateBestGuardian(participantIdx, this.processor.PullLocalWord(0x32)) - 1));
			this.processor.EmitLocalWord(0x46, (ushort)(this.processor.PullLocalWord(0x32) & 0x7f));
			this.processor.EmitLocalWord(0x2e, 0);
		}

		/// <summary>
		/// 'g': GoTo on (0x36), with the square cursor on the unit (0x4 at 0xe / 0x12), or off, the cursor removed; with a
		/// mouse its pointer changes. The original game showed the square cursor (moved with the arrow keys, Enter picks the
		/// destination) only when there was no mouse, with a mouse it only changed the mouse pointer, which was easy to miss.
		/// The square cursor is now shown in both cases, so the destination can be picked with the keyboard or the mouse.
		/// </summary>
		private void ToggleGoTo(short participantIdx)
		{
			if (this.processor.PullLocalWord(0x36) == 0)
			{
				if (this.processor.PullLocalWord(0x4) == 0)
				{
					this.processor.EmitLocalWord(0x4, 0x1);
					this.processor.RegSource.Value16 = (ushort)(participantIdx * 0x600 + this.processor.PullLocalWord(0x32) * 0xc);
					this.processor.EmitLocalWord(0xe, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)));
					this.processor.EmitLocalWord(0x12, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));
				}
			}
			else if (this.processor.PullLocalWord(0x4) != 0)
			{
				// GoTo cancelled, remove the square cursor
				this.processor.EmitLocalWord(0x4, 0x0);

				if ((this.owner.World.ChartSight[this.processor.PullLocalWord(0xe),
					this.processor.PullLocalWord(0x12)] & (1 << participantIdx)) != 0)
				{
					this.owner.MapOps.RepaintMapTile(
						this.processor.PullLocalSignedWord(0xe),
						this.processor.PullLocalSignedWord(0x12));
				}

				this.processor.EmitLocalWord(0xe, 0xffff);
			}

			if (this.processor.PullGlobalWord(0x1a3c) != 0)
			{
				// GoTo mouse pointer, or the normal one when GoTo is cancelled
				this.processor.RegBase.Value16 = (ushort)((this.processor.PullLocalWord(0x36) != 0 ? 7 : 2) << 1);
				this.owner.Services.AssignMouseCursorImage(0, 0, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xd4ce)));
			}

			this.processor.EmitLocalWord(0x36, (ushort)(this.processor.PullLocalWord(0x36) ^ 1));
		}

		/// <summary>
		/// 'h': in a city (distance 0 to the nearest, 0x10) it becomes the home city of the unit; a plane elsewhere flies to the
		/// nearest own city (true: status bit 1, the move follows), without one a human plane forgets that order and a computer
		/// plane is lost. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool OrderHome(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);

			this.processor.EmitLocalWord(0x10, this.owner.Helpers.LocateNearestCity(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30)));

			if (this.processor.PullGlobalWord(0x6c9a) == 0)
			{
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, this.processor.PullLocalByte(0x10));

				return false;
			}

			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.AirUnits)
				return false;

			this.processor.EmitLocalWord(0x24, this.owner.UnitOps.LocateNearestFriendlyCity(participantIdx, this.processor.PullLocalSignedWord(0x32)));

			if (this.processor.PullLocalWord(0x24) != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 2));
				return true;
			}

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfd));

				return false;
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));

			return false;
		}

		/// <summary>
		/// 'p': settlers clean up pollution (status bits 1 and 7, the work counter in byte 5): after 4 turns the pollution is
		/// gone; on a tile without pollution the order is forgotten (and the view moves to the unit). Other units: the help
		/// message. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void CleanPollution(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
			{
				DisplayHelpForHuman(0x1e8c);
				return;
			}

			this.processor.EmitLocalWord(0x40, this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30)));

			this.processor.RegSource.Value16 = unit;

			if ((this.processor.PullLocalByte(0x40) & 0x40) == 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x7d));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);

				this.owner.UnitOps.PanMapToPosition(participantIdx,
					this.processor.PullLocalSignedWord(0x2c),
					this.processor.PullLocalSignedWord(0x30));

				return;
			}

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 0x82));

			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));
			this.processor.RegAcc.LowByte = 0xff;

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps) < 4)
				return;

			this.owner.MapOps.DiscardTileImprovements(0x40,
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			this.owner.MapOps.KeepKnownTileImprovements(
				this.processor.PullLocalSignedWord(0x2c),
				this.processor.PullLocalSignedWord(0x30));

			owner.World.DirtyCellTally--;
			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

			this.processor.RegSource.Value16 = unit;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x7d));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
		}

		/// <summary>
		/// 'f': a land unit fortifies (status bit 2; a computer unit in a city makes it its home city); settlers build a
		/// fortress (bits 6 and 7, the work counter in byte 5 up to the defence of the terrain 0x3e + 4 – the terrain of the
		/// last tile clicked or looked at, as the original reads it) when they know Construction (0x2d) and the tile has no city
		/// or fortress, else the order is forgotten (and the view moves to the unit). The locals of HandlePlayerTurn are used
		/// as in the original.
		/// </summary>
		private void FortifyOrBuildFortress(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = unit;

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.LandUnits)
				return;

			short horizLoc = (short)this.processor.PullLocalWord(0x2c);
			short vertLoc = (short)this.processor.PullLocalWord(0x30);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 4));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

				RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

				if (participantIdx != this.owner.World.PersonParticipantIdx && (this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
				{
					this.processor.RegCount.Value16 = this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc);
					this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
					this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.BaseTown, this.processor.RegCount.LowByte);
				}

				return;
			}

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x21) != 0 ||
				(this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x2d)) == 0)
			{
				this.processor.RegSource.Value16 = unit;
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x3f));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);

				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);

				return;
			}

			this.processor.RegSource.Value16 = unit;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 0xc0));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x3e) * 0x13);
			short work = (short)((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e)) + 4);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));
			this.processor.RegCount.Value16 = (ushort)work;

			if (work > this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps))
				return;

			this.owner.MapOps.AssignTileImprovements(0x20, horizLoc, vertLoc);

			this.owner.UnitOps.ShiftUnit(horizLoc, vertLoc, participantIdx, this.processor.PullLocalSignedWord(0x32), 1);

			this.processor.RegSource.Value16 = unit;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x3f));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
		}

		/// <summary>
		/// 'r': settlers build a road (status bit 1, the work counter in byte 5 up to 2 – 4 for a railroad – times the
		/// byte 0x28e of the terrain), a railroad on a road with Railroad (0x31) outside cities; not on a railroad, not on a
		/// river (0xb) without Bridge Building (0x13); refused: the order is forgotten (and the view moves to the unit). Other
		/// units: the help message. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void BuildRoad(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short horizLoc = (short)this.processor.PullLocalWord(0x2c);
			short vertLoc = (short)this.processor.PullLocalWord(0x30);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
			{
				DisplayHelpForHuman(0x1e96);
				return;
			}

			this.processor.EmitLocalWord(0x40, this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc));
			byte improvements = this.processor.PullLocalByte(0x40);
			bool refused;

			if ((improvements & 8) != 0)
			{
				// A railroad on the road: with Railroad, not in a city
				refused = (this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x31)) == 0 ||
					(improvements & 1) != 0 || (improvements & 0x10) != 0;
			}
			else
			{
				refused = (improvements & 0x10) != 0;
			}

			if (refused)
			{
				this.processor.RegSource.Value16 = unit;
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfd));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);

				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);

				return;
			}

			this.processor.EmitLocalWord(0x3e, this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc));

			if (this.processor.PullLocalWord(0x3e) == 0xb &&
				(this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx, 0x13)) == 0)
			{
				return;
			}

			this.processor.RegSource.Value16 = unit;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | 2));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

			short factor = (short)((this.processor.PullLocalByte(0x40) & 8) != 0 ? 4 : 2);
			short worth = TerrainWorkByte(0x28e);
			this.processor.RegBase.Value16 = (ushort)worth;
			short work = (short)(factor * worth);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));
			this.processor.RegCount.Value16 = (ushort)work;

			if (work > this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps))
				return;

			this.owner.MapOps.AssignTileImprovements(
				(ushort)(((this.processor.PullLocalByte(0x40) & 8) != 0) ? 0x10 : 8),
				horizLoc, vertLoc);

			this.owner.UnitOps.ShiftUnit(horizLoc, vertLoc, participantIdx, this.processor.PullLocalSignedWord(0x32), 1);

			this.processor.RegSource.Value16 = unit;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0xfd));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
		}

		/// <summary>A signed byte of the terrain 0x3e in the table of terrains (0x13 bytes each); BX as the original leaves it</summary>
		private short TerrainWorkByte(ushort offset)
		{
			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x3e) * 0x13);

			return (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + offset));
		}

		/// <summary>
		/// 'i': settlers next to water (0xd7fa bit 1) irrigate a tile not irrigated yet (status bit 6, the work counter in
		/// byte 5 up to the word +2 of the terrain in the table at 0x44a, 12 bytes a terrain): the terrain changes (word +0 from
		/// 0) or the tile is irrigated (a mine goes); not possible (-1) or -2 away from the coast: the help message. Refused: the
		/// view moves to the unit, the work stops (other units: the help message). The locals of HandlePlayerTurn are used as in
		/// the original.
		/// </summary>
		private void IrrigateTile(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short horizLoc = (short)this.processor.PullLocalWord(0x2c);
			short vertLoc = (short)this.processor.PullLocalWord(0x30);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0 || (this.owner.World.DiagMarkers & 2) == 0 ||
				(this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 2) != 0)
			{
				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);

				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
					DisplayHelpForHuman(0x1ea0);
				else
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);

				StopWork(participantIdx, troopIdx, 0xbf);
				return;
			}

			this.processor.EmitLocalWord(0x3e, this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc));
			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x3e) * 0xc);
			this.processor.EmitLocalWord(0x3a, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44a)));

			if (this.processor.PullLocalWord(0x3a) == 0xffff)
			{
				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);
				DisplayHelpForHuman(0x1eaa);
				StopWork(participantIdx, troopIdx, 0xbf);
				return;
			}

			if (this.processor.PullLocalWord(0x3a) == 0xfffe && VerifyCoastalTile(horizLoc, vertLoc) == 0)
			{
				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);
				DisplayHelpForHuman(0x1eb1);
				StopWork(participantIdx, troopIdx, 0xbf);
				return;
			}

			if (!DoTileWork(participantIdx, troopIdx, unit, 0x40, 0x44c))
				return;

			if ((short)this.processor.PullLocalWord(0x3a) >= 0)
			{
				ChangeTerrain(horizLoc, vertLoc);
			}
			else
			{
				this.owner.MapOps.DiscardTileImprovements(4, horizLoc, vertLoc);
				this.owner.MapOps.AssignTileImprovements(2, horizLoc, vertLoc);
			}

			FinishTileWork(participantIdx, troopIdx, unit, 0xbf);
		}

		/// <summary>
		/// 'm': settlers mine a tile without a mine (status bit 7, the work counter in byte 5 up to the word +6 of the terrain
		/// in the table at 0x44a): the terrain changes (word +4 from 0, after a counter over 5) or the tile gets a mine (the
		/// irrigation goes); not possible (-1): the help message. Refused: the view moves to the unit, the work stops (other
		/// units: the help message). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void MineTile(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short horizLoc = (short)this.processor.PullLocalWord(0x2c);
			short vertLoc = (short)this.processor.PullLocalWord(0x30);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0 ||
				(this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 4) != 0)
			{
				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);

				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
					DisplayHelpForHuman(0x1eba);
				else
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);

				StopWork(participantIdx, troopIdx, 0x7f);
				return;
			}

			this.processor.EmitLocalWord(0x3e, this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc));
			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x3e) * 0xc);
			this.processor.EmitLocalWord(0x3a, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x44e)));

			if (this.processor.PullLocalWord(0x3a) == 0xffff)
			{
				this.owner.UnitOps.PanMapToPosition(participantIdx, horizLoc, vertLoc);
				DisplayHelpForHuman(0x1ec4);
				StopWork(participantIdx, troopIdx, 0x7f);
				return;
			}

			if (!DoTileWork(participantIdx, troopIdx, unit, 0x80, 0x450))
				return;

			if ((short)this.processor.PullLocalWord(0x3a) >= 0)
			{
				// (a second step of the counter, the change only over 5)
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));

				if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps) <= 5)
					return;

				ChangeTerrain(horizLoc, vertLoc);
			}
			else
			{
				this.owner.MapOps.DiscardTileImprovements(2, horizLoc, vertLoc);
				this.owner.MapOps.AssignTileImprovements(4, horizLoc, vertLoc);
			}

			FinishTileWork(participantIdx, troopIdx, unit, 0x7f);
		}

		/// <summary>The work of the settlers stops: their status loses the bit (mask), BX / SI as the original leaves them</summary>
		private void StopWork(short participantIdx, short troopIdx, byte mask)
		{
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & mask));
		}

		/// <summary>
		/// A turn of work of the settlers (the status bit, no moves, no destination, the counter one up): true when the counter
		/// reaches the word of the terrain 0x3e at the offset (12 bytes a terrain). The locals of HandlePlayerTurn are used as
		/// in the original.
		/// </summary>
		private bool DoTileWork(short participantIdx, short troopIdx, ushort unit, byte bit, ushort offset)
		{
			this.processor.RegSource.Value16 = unit;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) | bit));

			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
			this.processor.RegAcc.LowByte = 0xff;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, 0xff);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x3e) * 0xc);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) + 1));

			return (short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + offset)) <=
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps);
		}

		/// <summary>The terrain of the tile becomes 0x3a (seen by the human player), irrigation and mine go</summary>
		private void ChangeTerrain(short horizLoc, short vertLoc)
		{
			this.owner.Services.AssignDot(2, horizLoc, vertLoc, this.processor.PullLocalWord(0x3a));
			this.owner.World.ChartSight[this.processor.PullLocalWord(0x2c), this.processor.PullLocalWord(0x30)] |= 1;
			this.owner.MapOps.DiscardTileImprovements(6, horizLoc, vertLoc);
		}

		/// <summary>The work is done: the unit is shown, its status loses the bit (mask) and the counter is 0</summary>
		private void FinishTileWork(short participantIdx, short troopIdx, ushort unit, byte mask)
		{
			RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

			this.processor.RegSource.Value16 = unit;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & mask));
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.UnusualSteps, 0);
		}

		/// <summary>
		/// The order 0x44 of the unit 0x32 (a key of the human, the answer of ShiftAIUnit or a standing order), after the unit
		/// went on to its destination: true when the unit moves now (a plane flying home, L1c10). The locals of
		/// HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool CarryOutOrder(short participantIdx)
		{
			PrepareOrder(participantIdx);

			switch (this.processor.PullLocalWord(0x44))
			{
				case 0x20:
					SkipUnit(participantIdx);
					break;

				case 'D':
					DisbandUnit(participantIdx);
					break;

				case 'P':
					PillageTile(participantIdx);
					break;

				case 'b':
					BuildCity(participantIdx);
					break;

				case 'f':
					FortifyOrBuildFortress(participantIdx);
					break;

				case 'g':
					ToggleGoTo(participantIdx);
					break;

				case 'h':
					return OrderHome(participantIdx);

				case 'i':
					IrrigateTile(participantIdx);
					break;

				case 'm':
					MineTile(participantIdx);
					break;

				case 'p':
					CleanPollution(participantIdx);
					break;

				case 'r':
					BuildRoad(participantIdx);
					break;

				case 's':
					SentryUnit(participantIdx);
					break;

				case 'u':
					UnloadUnit(participantIdx);
					break;

				case 'w':
					// Wait: the unit comes again in the second pass
					this.processor.EmitLocalWord(0x28, 0x1);
					this.processor.EmitLocalWord(0x2e, 0x1);
					break;
			}

			return false;
		}

		/// <summary>
		/// 'b': settlers on land (not in the polar rows) join a city smaller than 10 or found one (EstablishCity, with the
		/// local 0x24 as the original passes it); the settlers are gone. A city of 10 or more: the help message; other units: the
		/// help message. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void BuildCity(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			short horizLoc = (short)this.processor.PullLocalWord(0x2c);
			short vertLoc = (short)this.processor.PullLocalWord(0x30);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0)
			{
				DisplayHelpForHuman(0x1ecc);
				return;
			}

			if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == MapRules.OceanTerrain || vertLoc < 2 || vertLoc >= 0x30)
				return;

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 1) != 0)
			{
				// The settlers join the city
				this.processor.EmitLocalWord(0x10, this.owner.MapOps.LocateCityAtTile(horizLoc, vertLoc));

				if ((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0x10)].TrueExtent >= 0xa)
				{
					DisplayHelpForHuman(0x1ed6);
					return;
				}

				this.owner.World.Towns[this.processor.PullLocalWord(0x10)].TrueExtent++;
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.ScaleIndex(0x600, (ushort)participantIdx);
				this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

				this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));

				RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));

				return;
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			this.processor.EmitLocalWord(0x3a, this.owner.Founding.EstablishCity(participantIdx,
				this.processor.PullLocalWord(0x24),
				horizLoc, vertLoc,
				1));

			if (this.processor.PullLocalWord(0x3a) != 0xffff)
			{
				this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));

				RepaintUnitIfVisible(participantIdx, this.processor.PullLocalSignedWord(0x32));
			}
		}

		/// <summary>
		/// The direction (1 - 8) of a move order: the numbers 1 - 8 (GoTo and the computer), Home / Up / PgUp / Right / PgDn /
		/// Down / End / Left; 0 for other keys (the signed comparisons of the original: no direction from 0x8000 on)
		/// </summary>
		private static ushort KeyDirection(ushort key)
		{
			return (short)key switch
			{
				>= 1 and <= 8 => key,
				0x4700 => 8,
				0x4800 => 1,
				0x4900 => 2,
				0x4d00 => 3,
				0x5100 => 4,
				0x5000 => 5,
				0x4f00 => 6,
				0x4b00 => 7,
				_ => 0,
			};
		}

		/// <summary>'c': the view centers on the marked tile (0x4, at 0xe / 0x12) or on the unit (0x2c / 0x30)</summary>
		private void CenterView(short participantIdx)
		{
			if (this.processor.PullLocalWord(0x4) != 0)
			{
				this.owner.MapOps.FocusMapView(participantIdx,
					this.processor.PullLocalSignedWord(0xe) - 7,
					this.processor.PullLocalSignedWord(0x12) - 6);
			}
			else
			{
				this.owner.MapOps.FocusMapView(participantIdx,
					this.processor.PullLocalSignedWord(0x2c) - 7,
					this.processor.PullLocalSignedWord(0x30) - 6);
			}
		}

		/// <summary>
		/// The marked tile (0xe / 0x12) moves one step in the direction 0x24 (repainted where it was when the player sees it,
		/// kept off the poles); off the map view the view moves. SI as the original leaves it.
		/// </summary>
		private void MoveMarkedTile(short participantIdx)
		{
			if (((sbyte)this.owner.World.ChartSight[this.processor.PullLocalWord(0xe), this.processor.PullLocalWord(0x12)] & (1 << (byte)participantIdx)) != 0)
			{
				this.owner.MapOps.RepaintMapTile(
					this.processor.PullLocalSignedWord(0xe),
					this.processor.PullLocalSignedWord(0x12));
			}

			this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x24) << 1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) +
					this.processor.PullLocalSignedWord(0xe));

			this.processor.EmitLocalWord(0xe, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) +
					this.processor.PullLocalSignedWord(0x12),
				0, 49);

			this.processor.EmitLocalWord(0x12, this.processor.RegAcc.Value16);

			if (VerifyPositionOnScreen(this.processor.PullLocalSignedWord(0xe), this.processor.PullLocalSignedWord(0x12)) == 0)
			{
				this.owner.MapOps.FocusMapView(participantIdx,
					this.processor.PullLocalSignedWord(0xe) - 8,
					this.processor.PullLocalSignedWord(0x12) - 7);
			}
		}

		/// <summary>
		/// A barbarian land unit (not a diplomat, legion or knights) leaving an irrigated or mined tile outside a city nearest
		/// to a city of the human pillages it and has no moves left (true). The locals of HandlePlayerTurn are used as in the
		/// original.
		/// </summary>
		private bool BarbarianPillages(short participantIdx)
		{
			if (participantIdx != 0)
				return false;

			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegSource.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride + UnitMemory.Origin + UnitMemory.Kind);
			byte kind = this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind);

			if (kind == UnitKindTable.Diplomat)
				return false;

			short horizLoc = (short)this.processor.PullLocalWord(0x2c);
			short vertLoc = (short)this.processor.PullLocalWord(0x30);
			this.processor.EmitLocalWord(0x40, this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc));

			if ((this.processor.PullLocalByte(0x40) & 1) != 0 || (this.processor.PullLocalByte(0x40) & 6) == 0 || kind == 3 || kind == 7)
				return false;

			if (this.owner.World.Towns[this.owner.Helpers.LocateNearestCity(horizLoc, vertLoc)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				return false;

			this.owner.MapOps.DiscardTileImprovements(6, horizLoc, vertLoc);

			if (((sbyte)this.owner.World.ChartSight[this.processor.PullLocalWord(0x2c), this.processor.PullLocalWord(0x30)] & (1 << (this.owner.World.PersonParticipantIdx & 0xff))) != 0)
			{
				this.owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);
				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = 0;
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			return true;
		}

		/// <summary>
		/// The tile the unit 0x32 moves to in the direction 0x24 (0x1c / 0x26, off the poles): the terrain where it stands (0x34)
		/// and there (0x3e), the improvements there (0x40), the unit there (0x18); true for a land unit. The locals of
		/// HandlePlayerTurn are used as in the original, BX / DI as it leaves them.
		/// </summary>
		private bool LocateMoveTarget(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x24) << 1);

			this.owner.Routing.TunePosition(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x1882)) +
					this.processor.PullLocalSignedWord(0x2c));

			this.processor.EmitLocalWord(0x1c, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x18e4)) +
					this.processor.PullLocalSignedWord(0x30),
				0, 49);

			this.processor.EmitLocalWord(0x26, this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0x34, this.owner.MapOps.ObtainTerrainType(this.processor.PullLocalSignedWord(0x2c), this.processor.PullLocalSignedWord(0x30)));
			this.processor.EmitLocalWord(0x3e, this.owner.MapOps.ObtainTerrainType(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)));
			this.processor.EmitLocalWord(0x40, this.owner.MapOps.ObtainTileImprovements(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)));
			this.processor.EmitLocalWord(0x18, this.owner.MapOps.ObtainUnitAtTile(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)));

			this.processor.RegDest.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

			return this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits;
		}

		/// <summary>
		/// Zone of control: a land unit (not a diplomat or caravan) on land goes from a tile next to a foreign unit to another
		/// free tile next to one: it stops (true; a beep for the human, the help message with that option, the destination
		/// forgotten). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool StoppedByZoneOfControl(short participantIdx)
		{
			if (this.processor.PullLocalWord(0x18) != 0xffff ||
				this.owner.UnitOps.MayOccupyTile(participantIdx, this.processor.PullLocalSignedWord(0x2c), this.processor.PullLocalSignedWord(0x30)) == 0 ||
				this.owner.UnitOps.MayOccupyTile(participantIdx, this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)) == 0)
			{
				return false;
			}

			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.TroopKind(participantIdx, troopIdx) >= UnitKindTable.Diplomat || this.processor.PullLocalWord(0x34) == MapRules.OceanTerrain)
				return false;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
				this.owner.Services.PerformSound(0x25, 0);

			if ((this.owner.World.SessionPrefMarkers & 1) != 0)
				DisplayHelpForHuman(0x1edf);

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

			return true;
		}

		/// <summary>Where the move of the unit goes on after a step of HandlePlayerTurn</summary>
		private enum MoveStep
		{
			/// <summary>The move goes on (L219c)</summary>
			GoesOn,

			/// <summary>A diplomat or caravan where no city is (L2157)</summary>
			NoCity,

			/// <summary>The order is done (L3365)</summary>
			Done,
		}

		/// <summary>
		/// A diplomat or caravan moves to the tile 0x1c / 0x26: no city there (NoCity); not from the sea into a foreign city
		/// (Done). A diplomat in a foreign city acts there (Done; into a city of the human it is shown, with the view on it and
		/// its move). A caravan asks (in an own city: help to build a wonder; a trade route) unless the city is own, builds no
		/// wonder, is nearer than 10 to its home city and on the same continent; a trade route or the help to build ends the
		/// order (Done), else it moves on. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private MoveStep EnterCityAsDiplomatOrCaravan(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.TroopKind(participantIdx, troopIdx) < UnitKindTable.Diplomat)
				return MoveStep.GoesOn;

			this.processor.EmitLocalWord(0x10, this.owner.Helpers.LocateCityAtPosition(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)));

			if (this.processor.PullLocalWord(0x10) == 0xffff)
				return MoveStep.NoCity;

			Game.State.City town = this.owner.World.Towns[this.processor.PullLocalWord(0x10)];
			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);

			if (town.ParticipantIdx != participantIdx && this.processor.PullLocalWord(0x34) == MapRules.OceanTerrain)
				return MoveStep.Done;

			this.processor.RegSource.Value16 = unit;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == UnitKindTable.Diplomat)
			{
				this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);
				this.processor.RegDest.Value16 = (ushort)town.ParticipantIdx;

				if (participantIdx != town.ParticipantIdx)
				{
					short person = this.owner.World.PersonParticipantIdx;

					if (person == town.ParticipantIdx)
					{
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
							(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | (1 << (person & 0xff))));

						this.owner.UnitOps.PanMapToPosition(person,
							this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
							this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

						this.owner.TextPainter.AwaitTime(30);

						this.owner.UnitOps.ChoreographUnitCombat(participantIdx,
							this.processor.PullLocalSignedWord(0x32),
							this.processor.PullLocalWord(0x24));
					}

					this.owner.Diplomats.DiplomatArrivesAtCity(this.processor.PullLocalSignedWord(0x10),
						participantIdx,
						this.processor.PullLocalSignedWord(0x32));

					return MoveStep.Done;
				}
			}

			this.processor.RegSource.Value16 = unit;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0x1b)
				return MoveStep.GoesOn;

			// The caravan: its distance from its home city (0x3a)
			sbyte home = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown);
			this.processor.RegDest.Value16 = (ushort)(home * 0x1c);
			byte homeIdx = this.troops.Pull(participantIdx, troopIdx, UnitMemory.BaseTown);

			this.owner.Helpers.ObtainDistance(
				this.processor.PullLocalSignedWord(0x1c),
				this.owner.World.Towns[homeIdx].Place.X,
				this.processor.PullLocalSignedWord(0x26),
				this.owner.World.Towns[homeIdx].Place.Y);

			this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);

			if (town.ParticipantIdx == participantIdx && (sbyte)town.PresentMakingIdx >= -24 && (short)this.processor.PullLocalWord(0x3a) < 10)
			{
				// Near its home city, building no wonder: on the same continent it just moves in
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegDest.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.processor.RegSource.Value16 = (ushort)(home * 0x1c);

				this.processor.RegDest.Value16 = this.owner.MapOps.ObtainContinentID(
					this.owner.World.Towns[homeIdx].Place.X,
					this.owner.World.Towns[homeIdx].Place.Y);

				if (this.owner.MapOps.ObtainContinentID(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)) == this.processor.RegDest.Value16)
					return MoveStep.GoesOn;
			}

			this.owner.Libc.CopyText(0xba06, "Will you?\n Keep moving\n Establish trade route\n");
			this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);

			if (town.ParticipantIdx == participantIdx && (sbyte)town.PresentMakingIdx < -24)
			{
				this.owner.Libc.AppendText(0xba06, " Help build WONDER.\n");

				if ((short)this.processor.PullLocalWord(0x3a) < 10)
					this.processor.EmitGlobalWord(0xb276, 0x2);
			}

			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);

			if (town.ParticipantIdx == participantIdx)
			{
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
				this.processor.EmitLocalWord(0x3a, this.processor.RegAcc.Value16);
			}
			else
			{
				this.processor.EmitLocalWord(0x3a, 0x1);
			}

			if (this.processor.PullLocalWord(0x3a) == 1 &&
				this.owner.Conquest.FoundTradeRoute(participantIdx, this.processor.PullLocalSignedWord(0x32), this.processor.PullLocalSignedWord(0x10)) != 0)
			{
				return MoveStep.Done;
			}

			if (this.processor.PullLocalWord(0x3a) != 2)
				return MoveStep.GoesOn;

			// The caravan helps to build the wonder: 10 times its price
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);
			this.processor.RegCount.Value16 = (ushort)(10 * this.troops.KindField(participantIdx, troopIdx, UnitKindTable.Price));
			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);
			this.owner.World.Towns[this.processor.PullLocalWord(0x10)].MaterialsTally += (short)this.processor.RegCount.Value16;

			this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));

			return MoveStep.Done;
		}

		/// <summary>Where the move goes on after CheckMoveTile</summary>
		private enum MoveCheck
		{
			/// <summary>The unit moves (L267b)</summary>
			Moves,

			/// <summary>No move, the order is done (L3365)</summary>
			Done,

			/// <summary>A foreign unit there is attacked (L2329)</summary>
			Attacks,
		}

		/// <summary>
		/// A diplomat next to a foreign unit where no city is bribes it (BuyUnit) and forgets its destination (written through
		/// SI as the original leaves it). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private void BribeUnitOnTheWay(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != UnitKindTable.Diplomat || this.processor.PullLocalWord(0x18) == 0xffff ||
				participantIdx == this.processor.PullGlobalSignedWord(0xd20a))
			{
				return;
			}

			this.owner.Diplomats.BuyUnit(this.processor.PullGlobalSignedWord(0xd20a),
				this.processor.PullLocalSignedWord(0x18),
				participantIdx);

			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);
		}

		/// <summary>
		/// The tile of the move: a land unit to the sea boards an own ship with room (sentried, 3 moves – written through SI as
		/// TallySpecialUnitsInStack leaves it, as the original does) or stays; a foreign unit there: no attack from a ship by a
		/// land unit (help), by a submarine on land, by a computer unit without attack, by a diplomat, on a plane outside a
		/// city except by a fighter (help); else it is attacked. The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private MoveCheck CheckMoveTile(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short owner = this.processor.PullGlobalSignedWord(0xd20a);

			if (this.processor.PullLocalWord(0x3e) == MapRules.OceanTerrain)
			{
				this.processor.RegSource.Value16 = unit;
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
				{
					// A land unit boards an own ship with room
					if (participantIdx != this.processor.PullGlobalSignedWord(0xd20a) || this.processor.PullLocalWord(0x18) == 0xffff ||
						(short)this.owner.UnitOps.TallySpecialUnitsInStack(participantIdx, this.processor.PullLocalSignedWord(0x18)) <= 0)
					{
						return MoveCheck.Done;
					}

					ushort status = (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.Status);
					this.processor.EmitUInt8(this.processor.SegData.Value16, status, (byte)(this.processor.PullUInt8(this.processor.SegData.Value16, status) | 1));
					this.processor.EmitUInt8(this.processor.SegData.Value16, status, (byte)(this.processor.PullUInt8(this.processor.SegData.Value16, status) & 0xf3));
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.LeftoverSteps), 3);

					return MoveCheck.Moves;
				}
			}

			if (this.processor.PullLocalWord(0x18) == 0xffff || participantIdx == this.processor.PullGlobalSignedWord(0xd20a))
				return MoveCheck.Moves;

			if (this.processor.PullLocalWord(0x34) == MapRules.OceanTerrain)
			{
				this.processor.RegSource.Value16 = unit;
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
				{
					// (a land unit on a ship does not attack)
					DisplayHelpForHuman(0x1f28);
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);

					return MoveCheck.Done;
				}
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.processor.PullLocalWord(0x3e) != MapRules.OceanTerrain && this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == UnitKindTable.Submarine)
				return MoveCheck.Done;

			if (participantIdx != this.owner.World.PersonParticipantIdx)
			{
				this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.AssaultPower) == 0)
					return MoveCheck.Done;
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == UnitKindTable.Diplomat)
				return MoveCheck.Done;

			// A plane outside a city is attacked only by a fighter
			short guardian = (short)this.processor.PullLocalWord(0x18);
			this.processor.RegDest.Value16 = (ushort)(guardian * UnitMemory.TroopStride);
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(owner, guardian) * UnitKindTable.Stride);

			if (this.troops.KindField(owner, guardian, UnitKindTable.LandGroup) == UnitKindTable.AirUnits)
			{
				this.processor.RegSource.Value16 = unit;

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0xe && (this.processor.PullLocalByte(0x40) & 1) == 0)
				{
					DisplayHelpForHuman(0x1f30);
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);

					return MoveCheck.Done;
				}
			}

			return MoveCheck.Attacks;
		}

		/// <summary>
		/// An attack with fewer than 3 moves left: the human confirms it ("Attack at x/3 strength?"; cancelled, the destination
		/// is forgotten), a computer unit attacks only with 2 moves. False: no attack. The locals of HandlePlayerTurn are used
		/// as in the original (the unit through SI as it leaves it).
		/// </summary>
		private bool ConfirmAttack(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) >= 3)
				return true;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.Libc.CopyText(0xba06, "Attack at\n");
				this.owner.Libc.AppendText(0xba06,
					this.owner.Libc.IntegerToText((sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8)), 10));
				this.owner.Libc.AppendText(0xba06, "/3 strength?\n Cancel\n Attack\n");
				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 16);

				if (this.processor.RegAcc.Value16 == 1)
					return true;

				this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);
				return false;
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			return this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) >= 2;
		}

		/// <summary>
		/// The attack on the best defender of the stack 0x18 of the player 0xd20a (the view moves to a unit of the human; 0x70d8
		/// while the human is in the battle): the attacker loses 3 moves (a bomber all). An attacked city (0x10) is noticed by
		/// its player (bit 4, not the human), shrinks (no walls, not from the sea, at the easiest level not a human city); a city
		/// of size 0 is destroyed (its player may be eliminated; the units there are shown to the human); the human sees the new
		/// size; a city tile without units of a computer player makes it rethink the production on the continent. True: the
		/// attacker is lost (L3c40). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool FightAt(short participantIdx)
		{
			short person = this.owner.World.PersonParticipantIdx;
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short horizLoc = (short)this.processor.PullLocalWord(0x1c);
			short vertLoc = (short)this.processor.PullLocalWord(0x26);

			this.processor.RegAcc.Value16 = (ushort)person;

			if (this.processor.PullGlobalWord(0xd20a) == (ushort)person)
				this.owner.UnitOps.PanMapToPosition(person, horizLoc, vertLoc);

			this.processor.EmitLocalWord(0x18, this.owner.UnitOps.LocateBestGuardianInStack(this.processor.PullGlobalSignedWord(0xd20a),
				this.processor.PullLocalSignedWord(0x18)));

			this.processor.RegAcc.Value16 = (ushort)person;

			if (participantIdx == person || this.processor.PullGlobalWord(0xd20a) == (ushort)person)
				this.processor.EmitGlobalWord(0x70d8, 0x1);

			this.processor.EmitLocalWord(0x3a, this.owner.Combat.SettleBattle(participantIdx,
				this.processor.PullLocalSignedWord(0x32),
				this.processor.PullGlobalSignedWord(0xd20a),
				this.processor.PullLocalSignedWord(0x18),
				1));

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
			this.processor.EmitGlobalWord(0x70d8, 0x0);

			if (this.processor.PullLocalWord(0x3a) == 0xffff)
				return false;

			this.processor.RegSource.Value16 = unit;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xff)
			{
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
				return true;
			}

			// 3 moves less (no fewer than 0), a bomber has none left
			this.processor.RegSource.Value16 = (ushort)(unit + UnitMemory.Origin + UnitMemory.LeftoverSteps);
			this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) - 3));

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) < 0)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			this.processor.RegSource.Value16 = unit;

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) == 0xf)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			if ((this.processor.PullLocalByte(0x40) & 1) == 0)
			{
				if ((this.processor.PullLocalByte(0x40) & 0x20) == 0)
				{
					this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
					this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				}

				return false;
			}

			// The attacked city
			this.processor.EmitLocalWord(0x10, this.owner.Helpers.LocateCityAtPosition(horizLoc, vertLoc));
			Game.State.City town = this.owner.World.Towns[this.processor.PullLocalWord(0x10)];
			this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);

			if (town.ParticipantIdx != person)
				town.ConditionMarker |= 0x10;

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0xff && (town.StructureMarkers0 & 0x80) == 0 &&
				this.processor.PullLocalWord(0x34) != MapRules.OceanTerrain &&
				(this.owner.World.HardnessTier != 0 || this.processor.PullGlobalWord(0xd20a) != (ushort)person))
			{
				town.TrueExtent--;
			}

			this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);

			if (town.TrueExtent == 0)
			{
				// The city is destroyed
				this.owner.Advisors.FinalizeProduction(
					this.processor.PullLocalSignedWord(0x10),
					this.processor.PullLocalWord(0x1c),
					this.processor.PullLocalWord(0x26));

				this.owner.NewGameWizard.EliminateCivilization(this.processor.PullGlobalSignedWord(0xd20a), participantIdx);

				if (participantIdx == person)
				{
					this.processor.EmitLocalWord(0x18, this.owner.MapOps.ObtainUnitAtTile(horizLoc, vertLoc));

					if (this.processor.PullLocalWord(0x18) != 0xffff)
					{
						short owner = this.processor.PullGlobalSignedWord(0xd20a);
						short left = (short)this.processor.PullLocalWord(0x18);
						this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
						this.processor.RegSource.Value16 = (ushort)(left * UnitMemory.TroopStride);
						this.troops.Assign(owner, left, UnitMemory.ShownToParticipants,
							(byte)(this.troops.Pull(owner, left, UnitMemory.ShownToParticipants) | (1 << (person & 0xff))));
					}
				}
			}

			this.processor.RegAcc.Value16 = (ushort)person;

			if (participantIdx == person || this.processor.PullGlobalWord(0xd20a) == (ushort)person || this.processor.PullGlobalWord(0xd806) != 0)
			{
				// The human sees the new size
				this.processor.RegSource.Value16 = (ushort)(this.processor.PullLocalWord(0x10) * 0x1c);
				this.processor.RegAcc.LowByte = (byte)town.TrueExtent;
				town.ShownExtent = (sbyte)this.processor.RegAcc.LowByte;

				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}

			if (this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc) == 0xffff && this.processor.PullGlobalWord(0xd20a) != (ushort)person)
			{
				this.owner.MapOps.ObtainContinentID(horizLoc, vertLoc);

				this.owner.Bot.RefreshCityProduction(this.processor.PullGlobalSignedWord(0xd20a), this.processor.RegAcc.Value16);
			}

			return false;
		}

		/// <summary>
		/// The cost of the move to 0x1c / 0x26: a ship does not go on land outside a city (the destination is forgotten); a
		/// plane or ship into a foreign city: a nuclear weapon explodes there, others are refused (help); without moves no move.
		/// A land unit pays nothing on a railroad (the human without GoTo), 1 on a road on both tiles, else 3 times the byte
		/// 0x28e of the terrain 0x3e – with fewer moves than full a die decides whether it may enter at all (no moves left);
		/// other units pay 3. False: no move (L3365). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool PayForMove(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short kindOffset = (short)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

			if (this.processor.PullLocalWord(0x3e) != MapRules.OceanTerrain && (this.processor.PullLocalByte(0x40) & 1) == 0)
			{
				this.processor.RegSource.Value16 = unit;
				this.processor.RegBase.Value16 = (ushort)kindOffset;

				if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.SeaUnits)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);
					return false;
				}
			}

			this.processor.RegSource.Value16 = unit;
			this.processor.RegBase.Value16 = (ushort)kindOffset;

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) != UnitKindTable.LandUnits && (this.processor.PullLocalByte(0x40) & 1) != 0 &&
				this.owner.MapOps.ObtainTileOwnerBits(this.processor.PullLocalSignedWord(0x1c), this.processor.PullLocalSignedWord(0x26)) != (ushort)participantIdx)
			{
				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.Kind) != 0x19)
				{
					DisplayHelpForHuman(0x1f62);
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);
					return false;
				}

				this.owner.UnitOps.EliminateUnit(participantIdx, this.processor.PullLocalSignedWord(0x32));

				this.owner.Combat.ExplodeNuclearWeapon(participantIdx,
					this.processor.PullLocalSignedWord(0x1c),
					this.processor.PullLocalSignedWord(0x26));

				return false;
			}

			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) == 0)
				return false;

			this.processor.RegSource.Value16 = unit;
			this.processor.RegBase.Value16 = (ushort)kindOffset;

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits)
			{
				short horizLoc = (short)this.processor.PullLocalWord(0x2c);
				short vertLoc = (short)this.processor.PullLocalWord(0x30);

				if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & this.processor.PullLocalByte(0x40) & 8) != 0 &&
					this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) != 0)
				{
					// A road on both tiles: 1, a railroad where it stands nothing (for the human without GoTo)
					byte cost = 1;

					if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x10) != 0 && participantIdx == this.owner.World.PersonParticipantIdx)
					{
						this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
						this.processor.RegDest.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

						if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHoriz) == 0xff)
							cost = 0;
					}

					this.processor.RegAcc.LowByte = cost;
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) - cost));
				}
				else
				{
					// The terrain: with fewer moves than full a die may stop the unit
					this.processor.RegSource.Value16 = (ushort)(unit + UnitMemory.Origin + UnitMemory.LeftoverSteps);
					this.processor.RegBase.Value16 = (ushort)(this.processor.PullLocalWord(0x3e) * 0x13);
					short terrainCost = (short)(3 * (sbyte)this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x28e)));

					if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) != 3)
					{
						this.processor.RegAcc.Value16 = (ushort)terrainCost;
						this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(this.processor.RegAcc.Value16));
						this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;

						if ((short)this.processor.RegCount.Value16 > this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps))
						{
							this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);
							return false;
						}
					}

					this.processor.RegCount.Value16 = (ushort)terrainCost;
					this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
					this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) - this.processor.RegCount.LowByte));
				}
			}
			else
			{
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, (byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) - 3));
			}

			this.processor.RegSource.Value16 = (ushort)(unit + UnitMemory.Origin + UnitMemory.LeftoverSteps);

			if (this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) < 0)
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

			return true;
		}

		/// <summary>
		/// The moves of a computer unit: one that would turn back (the opposite of its last heading) in a stack of up to 2 units
		/// forgets its heading and destination and stops (written through SI as TallyUnitsByDomainInStack leaves it, as the
		/// original does); else the direction 0x24 becomes its heading. A computer land unit (or a plane with fuel) does not join
		/// an own stack outside a city of 4 units or more (2 or more without moves). False: no move (L3365). The locals of
		/// HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool ComputerMayMove(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short person = this.owner.World.PersonParticipantIdx;

			if (participantIdx != person)
			{
				this.processor.RegSource.Value16 = unit;

				if (this.troops.Pull(participantIdx, troopIdx, UnitMemory.TravelHeading) != 0xff &&
					(ushort)(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.TravelHeading) ^ 4) == this.processor.PullLocalWord(0x24) &&
					(short)this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x32), 2) <= 2)
				{
					this.processor.RegAcc.LowByte = 0xff;
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHeading), 0xff);
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.TravelHoriz), 0xff);
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + UnitMemory.Origin + UnitMemory.LeftoverSteps), 0);

					return false;
				}

				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x24);
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHeading, this.processor.PullLocalByte(0x24));
			}

			if (this.processor.PullLocalWord(0x18) == 0xffff || participantIdx != this.processor.PullGlobalSignedWord(0xd20a) || participantIdx == person ||
				(this.processor.PullLocalByte(0x40) & 1) != 0)
			{
				return true;
			}

			this.processor.RegSource.Value16 = unit;
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);
			this.processor.RegDest.Value16 = this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup);

			if (this.processor.RegDest.Value16 != UnitKindTable.LandUnits &&
				(this.processor.RegDest.Value16 != UnitKindTable.AirUnits || this.troops.Pull(participantIdx, troopIdx, UnitMemory.UnusualSteps) == 0))
			{
				return true;
			}

			this.processor.EmitLocalWord(0x3a, this.owner.UnitOps.TallyUnitsByDomainInStack(participantIdx, this.processor.PullLocalSignedWord(0x18), 2));
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
			this.processor.RegAcc.Value16 = (ushort)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.LeftoverSteps) != 0 ? 4 : 2);

			return (short)this.processor.RegAcc.Value16 > (short)this.processor.PullLocalWord(0x3a);
		}

		/// <summary>
		/// The territory of the tile of the move (0x2: the player on the map layer 2 at x + 80): a land unit (not a diplomat
		/// or caravan) going on land onto an improved tile of another player's territory, between the human and another player,
		/// where no own unit is: the human may break the treaty (the destination forgotten; kept: no move), a computer unit at
		/// peace with the human stops (no moves). On an improved tile of another territory the unit is shown to that player.
		/// False: no move (L3365). The locals of HandlePlayerTurn are used as in the original.
		/// </summary>
		private bool EnterTerritory(short participantIdx)
		{
			short troopIdx = (short)this.processor.PullLocalWord(0x32);
			ushort unit = (ushort)(participantIdx * UnitMemory.ParticipantStride + troopIdx * UnitMemory.TroopStride);
			short person = this.owner.World.PersonParticipantIdx;
			short horizLoc = (short)this.processor.PullLocalWord(0x1c);
			short vertLoc = (short)this.processor.PullLocalWord(0x26);

			this.owner.VideoCard.ObtainDot(2, horizLoc + 80, vertLoc);
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);

			if (this.processor.PullLocalWord(0x2) == 0 || (short)this.processor.PullLocalWord(0x2) >= 8)
				return true;

			short owner = (short)this.processor.PullLocalWord(0x2);

			if (this.processor.PullLocalWord(0x3e) != MapRules.OceanTerrain && this.processor.PullLocalWord(0x34) != MapRules.OceanTerrain &&
				MayTreatyStop(participantIdx, troopIdx, unit, owner, person))
			{
				if (participantIdx == person)
				{
					this.troops.Assign(participantIdx, troopIdx, UnitMemory.TravelHoriz, 0xff);

					if (this.owner.Combat.AffirmBreakTreaty(this.processor.PullLocalSignedWord(0x2)) == 0xffff)
						return false;

					this.owner.Politics.WipeDiplomacyFlags(participantIdx, this.processor.PullLocalSignedWord(0x2), 2);
				}
				else if (this.processor.PullLocalWord(0x2) == (ushort)person)
				{
					this.processor.RegSource.Value16 = (ushort)(owner << 4);
					this.processor.RegBase.Value16 = (ushort)(participantIdx << 1);

					if ((this.owner.World.Participants[this.processor.PullLocalWord(0x2)].Relations[participantIdx] & 2) != 0)
					{
						this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
						this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
						this.troops.Assign(participantIdx, troopIdx, UnitMemory.LeftoverSteps, 0);

						return false;
					}
				}
			}

			if (participantIdx != (short)this.processor.PullLocalWord(0x2) && (this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0xe) != 0)
			{
				this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);
				this.processor.RegAcc.LowByte = (byte)(1 << (this.processor.PullLocalByte(0x2) & 0x1f));
				this.troops.Assign(participantIdx, troopIdx, UnitMemory.ShownToParticipants,
					(byte)(this.troops.Pull(participantIdx, troopIdx, UnitMemory.ShownToParticipants) | this.processor.RegAcc.LowByte));
			}

			return true;
		}

		/// <summary>
		/// The conditions of EnterTerritory after the terrains: a land unit (not a diplomat or caravan) onto an improved tile of
		/// another player's territory, between the human and another player, not onto a tile of its own player
		/// </summary>
		private bool MayTreatyStop(short participantIdx, short troopIdx, ushort unit, short owner, short person)
		{
			this.processor.RegBase.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
			this.processor.RegSource.Value16 = (ushort)(participantIdx * UnitMemory.ParticipantStride);

			if (this.troops.TroopKind(participantIdx, troopIdx) >= UnitKindTable.Diplomat || (this.processor.PullLocalByte(0x40) & 0xf) == 0 ||
				owner == participantIdx || (participantIdx != person && owner != person))
			{
				return false;
			}

			this.processor.RegSource.Value16 = unit;
			this.processor.RegBase.Value16 = (ushort)(this.troops.TroopKind(participantIdx, troopIdx) * UnitKindTable.Stride);

			return this.troops.KindField(participantIdx, troopIdx, UnitKindTable.LandGroup) == UnitKindTable.LandUnits &&
				this.owner.MapOps.ObtainTileOwner((short)this.processor.PullLocalWord(0x1c), (short)this.processor.PullLocalWord(0x26)) != (ushort)participantIdx;
		}

		/// <summary>
		/// Repaints the tile when the human player has seen it (or everything is shown, 0xd806)
		/// </summary>
		public void RepaintTileIfVisible(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"RepaintTileIfVisible({horizLoc}, {vertLoc})");

			if ((this.owner.World.ChartSight[horizLoc, vertLoc] & (ushort)(1 << this.owner.World.PersonParticipantIdx)) != 0 ||
				this.processor.PullGlobalWord(0xd806) != 0)
			{
				this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
			}

			this.processor.Journal.CloseScope("RepaintTileIfVisible");
		}

		/// <summary>
		/// Repaints the tile of the unit when the human player sees it (own unit, or the unit is shown to the human)
		/// </summary>
		public void RepaintUnitIfVisible(short participantIdx, short troopIdx)
		{
			this.processor.Journal.OpenScope($"RepaintUnitIfVisible({participantIdx}, {troopIdx})");

			// The original keeps SI for its caller
			ushort savedSource = this.processor.RegSource.Value16;
			short human = this.owner.World.PersonParticipantIdx;

			if (participantIdx == human ||
				(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.ShownToParticipants) & (ushort)(1 << human)) != 0)
			{
				this.owner.MapOps.RepaintMapTile(
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));
			}

			this.processor.RegSource.Value16 = savedSource;

			this.processor.Journal.CloseScope("RepaintUnitIfVisible");
		}

		/// <summary>
		/// 0 on a tile with a city, irrigation or a mine (improvement bits 0 - 2). Otherwise from the terrain table at 0x44a
		/// (12 bytes a terrain): 2 when the word at +4 is -3 or less, 1 when the word at +0 is -2 and the tile is at the coast
		/// (VerifyCoastalTile), else 0.
		/// </summary>
		public ushort ObtainTileMoveCost(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainTileMoveCost({horizLoc}, {vertLoc})");

			ushort cost = 0;

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x7) == 0)
			{
				int terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
				ushort terrainRecord = (ushort)(0x44a + 0xc * terrain);

				if ((short)this.processor.PullGlobalWord((ushort)(terrainRecord + 4)) <= -3)
				{
					cost = 2;
				}
				else if (this.processor.PullGlobalWord(terrainRecord) == 0xfffe)
				{
					VerifyCoastalTile(horizLoc, vertLoc);

					if (this.processor.RegAcc.Value16 != 0)
						cost = 1;
				}
			}

			this.processor.RegAcc.Value16 = cost;

			this.processor.Journal.CloseScope("ObtainTileMoveCost");

			return cost;
		}

		/// <summary>
		/// Whether the tile is on the coast: it is terrain 11 itself, or one of the 4 tiles next to it in a straight line
		/// (headings 1, 3, 5, 7; the column is not wrapped) is terrain 10 or 11 or has improvement bit 1 - but tiles with a city
		/// do not count
		/// </summary>
		/// <returns>1 on the coast, else 0 (also in AX)</returns>
		public ushort VerifyCoastalTile(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"VerifyCoastalTile({horizLoc}, {vertLoc})");

			MapLayers chart = this.owner.MapOps.Chart;
			ushort coastal = 0;

			if (this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc) == 0xb)
			{
				coastal = 1;
			}
			else
			{
				for (int heading = 1; heading <= 8 && coastal == 0; heading += 2)
				{
					short tileHoriz = (short)(chart.DirectionHoriz(heading) + horizLoc);
					short tileVert = (short)(chart.DirectionVert(heading) + vertLoc);

					ushort terrain = this.owner.MapOps.ObtainTerrainType(tileHoriz, tileVert);
					ushort improvements = this.owner.MapOps.ObtainTileImprovements(tileHoriz, tileVert);

					if ((improvements & 0x1) == 0 && (terrain == 0xb || terrain == 0xa || (improvements & 0x2) != 0))
					{
						coastal = 1;
					}
				}
			}

			this.processor.RegAcc.Value16 = coastal;

			this.processor.Journal.CloseScope("VerifyCoastalTile");

			return coastal;
		}

		/// <summary>
		/// The panel of the active unit (left, under the world map): the civilization, the type (Veteran), the moves (with the
		/// fuel of aircraft), the home city, the terrain and its improvements, then the units of the city on the tile (the two
		/// supported at 0x1c * city ... Unfamiliar, drawn through the unit 127) and the stack (up to row 0xb8, "more" after).
		/// Unit 0x80: no unit.
		/// </summary>
		public void RenderUnitInfo(short participantIdx, ushort arg2)
		{
			this.processor.Journal.OpenScope($"RenderUnitInfo({participantIdx}, {arg2})");

			this.owner.Startup.ConcealMouseCursor();

			this.owner.TurnHooks.FloodBoxWithShadow(0, 97, 80, 103);

			if (arg2 == 0x80)
			{
				this.owner.TextPainter.RenderTextToScreen0(0x207f, 4, 124, 0);

				this.owner.TextPainter.RenderTextToScreen0(0x208b, 4, 136, 0);

				this.owner.TextPainter.RenderTextToScreen0(0x2097, 4, 144, 0);
			}
			else
			{
				RenderUnitPanel(participantIdx, arg2);
			}

			this.owner.Startup.DisplayMouseCursor();

			// Far return
			this.processor.Journal.CloseScope("RenderUnitInfo");
		}

		private void RenderUnitPanel(short participantIdx, ushort arg2)
		{
			short troopIdx = (short)arg2;
			short vertLoc = 0x63;

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, 99, 0);

			vertLoc += 8;

			this.owner.Libc.CopyText(0xba06, (ushort)((0x22 * this.troops.TroopKind(participantIdx, troopIdx)) + 0x112a));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, vertLoc, 0);

			vertLoc += 8;

			if ((this.troops.Pull(participantIdx, troopIdx, UnitMemory.Status) & 0x20) != 0)
			{
				this.owner.TextPainter.RenderTextToScreen0(0x20a3, 8, vertLoc, 0);

				vertLoc += 8;
			}

			sbyte steps = this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps);
			short moves = (short)this.owner.Helpers.VerifyValueSpan(steps / 3, 0, 99);

			this.owner.Libc.CopyText(0xba06, "Moves: ");

			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(moves, 10));

			short thirds = (short)(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.LeftoverSteps) % 3);

			if (thirds != 0)
			{
				this.owner.Libc.AppendText(0xba06, ".");

				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(thirds, 10));
			}

			if (this.troops.KindField(participantIdx, troopIdx, UnitKindTable.RoundsAway) != 0)
			{
				// Aircraft: the moves left with the fuel of the next turns
				this.owner.Libc.AppendText(0xba06, "(");

				moves += (short)(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.UnusualSteps) *
					(short)this.troops.KindField(participantIdx, troopIdx, UnitKindTable.ShiftTally));

				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(moves, 10));

				this.owner.Libc.AppendText(0xba06, ")");
			}

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, vertLoc, 0);

			vertLoc += 8;

			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown));

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, vertLoc, 0);

			vertLoc += 8;

			this.owner.Libc.CopyText(0xba06, "(");

			this.owner.Libc.AppendText(0xba06, (ushort)((0x13 * this.owner.MapOps.ObtainTerrainType(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert))) + 0x282));

			this.owner.Libc.AppendText(0xba06, ")");

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 4, vertLoc, 0);

			vertLoc += 8;

			this.owner.MapOps.ObtainTileImprovements(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			byte improvements = this.processor.RegAcc.LowByte;

			// Railroad or road, irrigation or mines, pollution
			foreach ((byte bit, byte otherBit, ushort text, ushort otherText) in new[] { ((byte)0x10, (byte)0x8, (ushort)0x20bd, (ushort)0x20c8),
				((byte)0x2, (byte)0x4, (ushort)0x20cf, (ushort)0x20dc), ((byte)0x40, (byte)0x0, (ushort)0x20e5, (ushort)0x0) })
			{
				ushort shown = (improvements & bit) != 0 ? text : (improvements & otherBit) != 0 ? otherText : (ushort)0;

				if (shown != 0)
				{
					this.owner.TextPainter.RenderTextToScreen0(shown, 4, vertLoc, 0);

					vertLoc += 8;
				}
			}

			vertLoc += 4;

			short followingIdx = this.troops.Following(participantIdx, troopIdx);
			short column = 8;

			this.owner.MapOps.ObtainTileImprovements(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
				this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

			if ((this.processor.RegAcc.LowByte & 1) != 0)
			{
				ushort townIdx = this.owner.Helpers.LocateCityAtPosition(this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Horiz),
					this.troops.PullSigned(participantIdx, troopIdx, UnitMemory.Vert));

				// The units the city supports elsewhere are drawn through the unit 127 of the player
				for (int unfamiliarIdx = 0; unfamiliarIdx < 2; unfamiliarIdx++)
				{
					byte unfamiliar = (byte)this.owner.World.Towns[townIdx].Unfamiliar[unfamiliarIdx];

					if (unfamiliar == 0xff)
						continue;

					this.troops.Assign(participantIdx, 127, UnitMemory.Kind, (byte)(unfamiliar & 0x3f));
					this.troops.Assign(participantIdx, 127, UnitMemory.Status, 0x8);
					this.troops.Assign(participantIdx, 127, UnitMemory.TravelHoriz, 0xff);

					this.owner.MapOps.RenderUnitSprite(participantIdx, 127, column, vertLoc);

					this.troops.Assign(participantIdx, 127, UnitMemory.Kind, 0xff);

					column += 0x10;
				}
			}

			while (followingIdx != -1)
			{
				if (followingIdx == troopIdx || vertLoc >= 0xb8)
					break;

				this.owner.MapOps.RenderUnitSprite(participantIdx, followingIdx, column, vertLoc);

				column += 0x10;

				if (column > 0x40)
				{
					column = 8;
					vertLoc += 0x10;
				}

				followingIdx = this.troops.Following(participantIdx, followingIdx);
			}

			if (followingIdx != -1 && followingIdx != troopIdx)
			{
				this.owner.TextPainter.RenderTextToScreen0(0x20f1, 74, 192, 0);
			}
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public ushort VerifyPositionOnScreen(int horizLoc, int vertLoc)
		{
			this.owner.Journal.OpenScope($"VerifyPositionOnScreen({horizLoc}, {vertLoc})");

			// function body
			if (horizLoc < 16 && this.processor.PullGlobalSignedWord(0xd4cc) >= 65)
			{
				horizLoc += 80;
			}

			if (horizLoc < this.processor.PullGlobalSignedWord(0xd4cc) ||
				horizLoc >= this.processor.PullGlobalSignedWord(0xd4cc) + 15 ||
				vertLoc < this.processor.PullGlobalSignedWord(0xd75e) ||
				vertLoc >= this.processor.PullGlobalSignedWord(0xd75e) + 12)
			{
				this.processor.RegAcc.Value16 = 0;
			}
			else
			{
				this.processor.RegAcc.Value16 = 1;
			}

			this.owner.Journal.CloseScope("VerifyPositionOnScreen");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Throws away the keys waiting and waits until the mouse buttons are released
		/// </summary>
		public void AwaitForKey()
		{
			this.processor.Journal.OpenScope("AwaitForKey()");

			while (this.owner.Libc.KeyAvailable() != 0)
			{
				this.owner.Dialogs.ObtainNavigationKey();
			}

			do
			{
				this.owner.Startup.ObtainMouseState();
			}
			while (this.owner.GlobDb3a != 0);

			this.processor.Journal.CloseScope("AwaitForKey");
		}

		/// <summary>
		/// The next unit to activate in the stack of the unit: following the ring (at most 16 steps), the non-ship unit with the
		/// smallest index after the unit, counting round past 127
		/// </summary>
		/// <returns>That unit, the unit itself when it stands alone or nothing comes (also in AX)</returns>
		public ushort LocateBestGuardian(short participantIdx, ushort troopIdx)
		{
			this.processor.Journal.OpenScope("LocateBestGuardian()");

			ushort best = troopIdx;
			short current = this.troops.Following(participantIdx, troopIdx);

			if (current != -1)
			{
				short smallestStep = 999;

				for (int stepIdx = 0; stepIdx < 0x10; stepIdx++)
				{
					if (this.troops.KindField(participantIdx, current, UnitKindTable.LandGroup) != UnitKindTable.SeaUnits)
					{
						short step = (short)(current > (short)troopIdx ? current - troopIdx : current - troopIdx + 0x80);

						if (step < smallestStep)
						{
							smallestStep = step;
							best = (ushort)current;
						}
					}

					current = this.troops.Following(participantIdx, current);

					if (current == (short)troopIdx)
						break;
				}
			}

			this.processor.RegAcc.Value16 = best;

			this.processor.Journal.CloseScope("LocateBestGuardian");

			return best;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="stringPtr"></param>
		public void DisplayHelpForHuman(ushort charsAddr)
		{
			this.processor.Journal.OpenScope($"DisplayHelpForHuman(0x{charsAddr:x4})");

			// function body
			if (this.processor.PullGlobalSignedWord(0x6b90) == this.owner.World.PersonParticipantIdx)
			{
				this.owner.HelpSystem.DisplayHelpPopupForHuman(charsAddr);
			}
		
			// Far return
			this.processor.Journal.CloseScope("DisplayHelpForHuman");
		}
	}
}
