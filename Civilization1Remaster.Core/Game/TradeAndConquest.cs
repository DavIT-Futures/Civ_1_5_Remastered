using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x2459.
	/// </summary>
	public class TradeAndConquest
	{
		private readonly GameEngine owner;
		private readonly CPU processor;

		public TradeAndConquest(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
		}

		/// <summary>
		/// The player takes the city (marker: by a diplomat, without damage). A captured capital of a big civilization of a
		/// better standing than the human player may split it (the new capital stops its production); a capital ends the
		/// spaceship. The plunder (up to the owner's gold) goes from the owner to the player. Barbarians with a land unit
		/// (role up to 2, unless 0x28bc) destroy the city; otherwise the city loses its production, some buildings (half of
		/// them by chance unless marker) and one size (unless marker and size 1). A city that remains: replay, strategic
		/// location, the power map marks around it. The human player hears of it (capture message, city screen or newspaper),
		/// else spies of known players report. Then technology, owner of the tiles, the owner's units of this city outside or
		/// in a destroyed one disappear, the production of the city moves to the new owner and the old one may be finished.
		/// SI and DI get the values of the original before the calls.
		/// </summary>
		public void SeizeCity(short participantIdx, short townIdx, ushort marker)
		{
			this.processor.Journal.OpenScope($"SeizeCity({participantIdx}, {townIdx}, {marker})");

			// The final values of the locals stay in the stack frame, like in the original
			this.processor.EnterFrame(0x12);
			this.processor.PreserveIndexRegisters();

			short person = this.owner.World.PersonParticipantIdx;
			City town = this.owner.World.Towns[townIdx];
			ushort townOffset = (ushort)(0x1c * townIdx);

			this.processor.RegSource.Value16 = townOffset;

			ushort formerIdx = (ushort)town.ParticipantIdx;
			ushort horizLoc = (ushort)(short)town.Place.X;
			ushort vertLoc = (ushort)(short)town.Place.Y;

			this.processor.EmitLocalWord(0x12, formerIdx);
			this.processor.EmitLocalWord(0x6, horizLoc);
			this.processor.EmitLocalWord(0x8, vertLoc);

			if (participantIdx == person)
				this.owner.World.Participants[formerIdx].TouchParticipantTimeleft = -2;

			if (formerIdx != (ushort)person)
			{
				this.processor.RegSource.Value16 = townOffset;

				if (((town.StructureMarkers0 & 1) != 0 || (byte)town.PresentMakingIdx == 0xff) && marker == 0 && (town.StructureMarkers0 & 1) != 0)
				{
					this.processor.RegDest.Value16 = (ushort)(formerIdx << 1);

					if (this.owner.World.Participants[formerIdx].TownTally > 4 &&
						this.owner.World.Participants[person].Standing < this.owner.World.Participants[formerIdx].Standing)
					{
						SplitCapturedCivilization(participantIdx, town, (short)formerIdx);
					}
				}
			}

			if ((town.StructureMarkers0 & 1) != 0)
			{
				EliminateSpaceship((short)formerIdx);
			}

			if (formerIdx == (ushort)person)
			{
				this.owner.World.Participants[participantIdx].TouchParticipantTimeleft = -1;
			}

			ComputeCityPlunder(townIdx);

			this.owner.Helpers.VerifyValueSpan((short)this.processor.RegAcc.Value16, 0, this.owner.World.Participants[formerIdx].Gold);

			short plunder = (short)this.processor.RegAcc.Value16;
			this.processor.EmitLocalWord(0x4, (ushort)plunder);

			if (!BarbariansRazeCity(participantIdx, townIdx, horizLoc, vertLoc))
			{
				this.processor.RegSource.Value16 = townOffset;

				town.MaterialsTally = 0;
				town.ConditionMarker &= 0xae;
				town.StructureMarkers0 &= 0xfbb6;

				if (marker == 0)
				{
					int half = this.owner.Libc.Dice.DrawInt(2);

					town.StructureMarkers0 &= (ushort)(0xaaaa >> (byte)half);
					town.StructureMarkers1 &= 0;
				}

				if (marker == 0 || town.TrueExtent > 1)
				{
					town.TrueExtent--;

					if (town.TrueExtent < 1)
					{
						this.owner.Advisors.FinalizeProduction(townIdx, horizLoc, vertLoc);
					}
				}
			}

			this.owner.World.Participants[formerIdx].Gold -= plunder;

			if (participantIdx != 0)
				this.owner.World.Participants[participantIdx].Gold += plunder;

			this.processor.RegSource.Value16 = townOffset;

			if (town.TrueExtent != 0)
			{
				MarkCapturedCity(participantIdx, town, horizLoc, vertLoc);
			}

			town.ParticipantIdx = (sbyte)participantIdx;

			if (participantIdx == person || formerIdx == (ushort)person)
			{
				AnnounceCapture(participantIdx, townIdx, (short)formerIdx, horizLoc, vertLoc, plunder);
			}
			else
			{
				ReportCaptureBySpies(participantIdx, townIdx, (short)formerIdx);
			}

			ChooseTechnologyToTake(participantIdx, (short)formerIdx);

			this.owner.MapOps.MapAssignCityOwner(participantIdx, (short)horizLoc, (short)vertLoc);

			ushort troopIdx;

			for (troopIdx = 0; troopIdx < 0x80; troopIdx++)
			{
				ushort troop = (ushort)((ushort)(0x600 * formerIdx) + (ushort)(0xc * troopIdx));
				this.processor.RegSource.Value16 = troop;

				if (this.processor.PullGlobalByte((ushort)(troop + 0x81d7)) == 0xff ||
					(sbyte)this.processor.PullGlobalByte((ushort)(troop + 0x81df)) != townIdx)
					continue;

				this.processor.RegDest.Value16 = townOffset;

				if (this.processor.PullGlobalByte((ushort)(troop + 0x81d5)) == (byte)(sbyte)town.Place.X &&
					this.processor.PullGlobalByte((ushort)(troop + 0x81d6)) == (byte)(sbyte)town.Place.Y &&
					town.TrueExtent != 0)
					continue;

				this.owner.UnitOps.EliminateUnit((short)formerIdx, (short)troopIdx);
			}

			this.processor.EmitLocalWord(0x10, troopIdx);

			if (participantIdx == person && this.owner.World.Participants[person].RegimeKind < 4)
			{
				town.ConditionMarker |= 1;
			}

			this.processor.RegSource.Value16 = townOffset;

			if (town.TrueExtent != 0)
			{
				if (town.PresentMakingIdx >= 0)
				{
					this.processor.RegDest.Value16 = (ushort)(town.PresentMakingIdx << 1);

					this.owner.World.Participants[formerIdx].TroopsInsideMaking[town.PresentMakingIdx]--;
				}

				if (participantIdx == person)
				{
					this.owner.CityOps.HandleCity(townIdx, 1);

					this.owner.TurnHooks.RepaintMainScreen();
				}
				else
				{
					this.processor.RegSource.Value16 = townOffset;

					town.PresentMakingIdx = 0;

					this.owner.Advisors.PickCityProduction(participantIdx, townIdx);

					town.PresentMakingIdx = (sbyte)this.processor.RegAcc.LowByte;

					if (town.PresentMakingIdx >= 0)
					{
						this.processor.RegDest.Value16 = (ushort)(0x38 * participantIdx);

						this.owner.World.Participants[participantIdx].TroopsInsideMaking[town.PresentMakingIdx]++;
					}
				}
			}

			this.owner.NewGameWizard.EliminateCivilization((short)formerIdx, participantIdx);

			this.processor.RestoreIndexRegisters();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("SeizeCity");
		}

		/// <summary>
		/// The captured capital may split the old civilization; when it does, the new capital (ObtainDistanceToCapital, in the
		/// frame 0x2) stops its production (the city belongs to the player meanwhile)
		/// </summary>
		private void SplitCapturedCivilization(short participantIdx, City town, short formerIdx)
		{
			this.owner.Secession.DivideCivilization(formerIdx);

			if (this.processor.RegAcc.Value16 != 0)
				return;

			town.ParticipantIdx = (sbyte)participantIdx;

			this.processor.StackWord(0); // stack management - push return segment, ignored
			this.processor.StackWord(0x00ab); // stack management - push return offset
			this.owner.Secession.ObtainDistanceToCapital(formerIdx);
			this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment

			ushort capitalIdx = this.processor.RegAcc.Value16;
			this.processor.EmitLocalWord(0x2, capitalIdx);

			this.owner.World.Towns[capitalIdx].PresentMakingIdx = -1;

			town.ParticipantIdx = (sbyte)formerIdx;
		}

		/// <summary>
		/// Barbarians with a land unit (role 0x113e of its kind up to 2) on the city destroy it, unless 0x28bc; true when they did
		/// </summary>
		private bool BarbariansRazeCity(short participantIdx, short townIdx, ushort horizLoc, ushort vertLoc)
		{
			if (participantIdx != 0)
				return false;

			this.owner.MapOps.ObtainUnitAtTile((short)horizLoc, (short)vertLoc);

			ushort troop = (ushort)(0xc * this.processor.RegAcc.Value16);
			this.processor.RegSource.Value16 = troop;

			sbyte kind = (sbyte)this.processor.PullGlobalByte((ushort)((ushort)(0x600 * participantIdx) + troop + 0x81d7));

			if ((short)this.processor.PullGlobalWord((ushort)((ushort)(0x22 * kind) + 0x113e)) > 2 || this.processor.PullGlobalWord(0x28bc) != 0)
				return false;

			this.owner.Advisors.FinalizeProduction(townIdx, horizLoc, vertLoc);

			this.owner.World.Towns[townIdx].TrueExtent = 0;

			return true;
		}

		/// <summary>The replay, the strategic location and the 20 power map marks around the captured city (screen 2)</summary>
		private void MarkCapturedCity(short participantIdx, City town, ushort horizLoc, ushort vertLoc)
		{
			this.owner.UnitOps.AttachReplayContent(9, (byte)participantIdx, town.LabelIdx, (byte)horizLoc, (byte)vertLoc);

			this.owner.Bot.DisableStrategicLocation(participantIdx, 0, (short)horizLoc, (short)vertLoc, 4);

			town.ConditionMarker &= 0x9b;

			ushort markIdx;

			for (markIdx = 0; markIdx < 0x14; markIdx++)
			{
				this.processor.RegSource.Value16 = (ushort)(markIdx << 1);

				this.owner.Routing.TunePosition((short)this.processor.PullGlobalWord((ushort)(this.processor.RegSource.Value16 + 0x1882)) + (short)horizLoc);

				ushort markHoriz = this.processor.RegAcc.Value16;
				ushort markVert = (ushort)(this.processor.PullGlobalWord((ushort)(this.processor.RegSource.Value16 + 0x18e4)) + vertLoc);

				this.processor.EmitLocalWord(0xa, markHoriz);
				this.processor.EmitLocalWord(0xc, markVert);

				this.owner.Services.AssignDot(2, (short)markHoriz + 0x50, (short)markVert, (ushort)participantIdx);
			}

			this.processor.EmitLocalWord(0x10, markIdx);
		}

		/// <summary>The human player takes or loses the city: the map, the capture message, the city screen or the newspaper</summary>
		private void AnnounceCapture(short participantIdx, short townIdx, short formerIdx, ushort horizLoc, ushort vertLoc, short plunder)
		{
			short person = this.owner.World.PersonParticipantIdx;
			City town = this.owner.World.Towns[townIdx];

			this.owner.MapOps.KeepKnownTileImprovements((short)horizLoc, (short)vertLoc);

			if (formerIdx == person)
			{
				this.owner.UnitOps.PanMapToPosition(person, (short)horizLoc, (short)vertLoc);
			}

			this.owner.MapOps.RenderMapTile((short)horizLoc, (short)vertLoc);

			this.processor.RegSource.Value16 = (ushort)(participantIdx << 1);

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)(this.processor.RegSource.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " capture\n");

			ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, ". ");

			this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(plunder, 10));

			this.owner.Libc.AppendText(0xba06, " gold\npieces plundered.\n");

			this.owner.Services.PerformSound(this.processor.PullGlobalWord((ushort)((ushort)(0x3a * this.owner.World.Participants[participantIdx].ChiefVisuals) + 0x1518)), 0);

			if ((this.owner.World.SessionPrefMarkers & 8) != 0)
			{
				this.owner.Startup.ConcealMouseCursor();

				this.owner.CityScene.DisplayCityScreen(townIdx, -2);

				this.owner.Startup.DisplayMouseCursor();

				this.owner.Startup.QueryForDisk(1, 1);

				this.owner.CityScene.RenderCitySceneryForPlayer(participantIdx);

				this.owner.Startup.QueryForDisk(0, 1);

				this.owner.TurnHooks.RepaintMainScreen();
			}
			else
			{
				this.owner.Paper.DisplayNewspaper(townIdx);
			}

			this.owner.Services.PerformSound(1, 0);

			town.ShownExtent = participantIdx == person ? (sbyte)0 : town.TrueExtent;
		}

		/// <summary>Spies report the capture when the human player knows one of the two players</summary>
		private void ReportCaptureBySpies(short participantIdx, short townIdx, short formerIdx)
		{
			ushort[] known = this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations;

			this.processor.RegSource.Value16 = (ushort)(this.owner.World.PersonParticipantIdx << 4);

			if ((known[participantIdx] & 0x40) == 0 && (known[formerIdx] & 0x40) == 0)
				return;

			this.processor.EmitGlobalWord(0x2f9e, 0x0);

			this.owner.Libc.CopyText(0xba06, "Spies report:\n");

			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)(((ushort)participantIdx << 1) + 0x1992)));

			this.owner.Libc.AppendText(0xba06, this.owner.World.Towns[townIdx].TrueExtent != 0 ? (ushort)0x2893 : (ushort)0x28a1);

			this.owner.Libc.AppendText(0xba06, this.processor.PullGlobalWord((ushort)(((ushort)formerIdx << 1) + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " city\nof ");

			ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
		}

		/// <summary>
		/// The player loses its spaceship: when it was launched (StarshipMarkers bit 8 + player) a message "... spaceship" with
		/// the text 0x28ca (also built: bit player) or 0x28dd; both bits are cleared and the parts wiped
		/// </summary>
		public void EliminateSpaceship(short participantIdx)
		{
			this.processor.Journal.OpenScope($"EliminateSpaceship({participantIdx})");

			ushort built = (ushort)(1 << participantIdx);
			ushort launched = (ushort)(1 << (participantIdx + 8));

			this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)((participantIdx << 1) + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " spaceship\n");

			if ((this.owner.World.StarshipMarkers & launched) != 0)
			{
				this.owner.Libc.AppendText(0xba06, (this.owner.World.StarshipMarkers & built) != 0 ? (ushort)0x28ca : (ushort)0x28dd);

				this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
			}

			this.owner.World.StarshipMarkers &= (short)~built;
			this.owner.World.StarshipMarkers &= (short)~launched;

			this.owner.NewGameWizard.WipeSpaceshipContent(participantIdx);

			this.processor.Journal.CloseScope("EliminateSpaceship");
		}

		/// <summary>
		/// The gold taken with a city: the owner's gold * city size / (total size of the owner's cities + 1), divided first
		/// when the product would pass 32000. Signed word arithmetic like in the original (also its division errors).
		/// </summary>
		/// <returns>The gold, also in AX</returns>
		public ushort ComputeCityPlunder(short townIdx)
		{
			this.processor.Journal.OpenScope($"ComputeCityPlunder({townIdx})");

			short size = this.owner.World.Towns[townIdx].TrueExtent;
			Player victim = this.owner.World.Participants[(ushort)this.owner.World.Towns[townIdx].ParticipantIdx];
			short divisor = (short)(victim.GrandTownExtent + 1);

			short plunder = CPU.SignedQuotient(32000, size) > victim.Gold ?
				CPU.SignedQuotient((short)(victim.Gold * size), divisor) :
				(short)(CPU.SignedQuotient(victim.Gold, divisor) * size);

			this.processor.RegAcc.Value16 = (ushort)plunder;

			this.processor.Journal.CloseScope("ComputeCityPlunder");

			return (ushort)plunder;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="playerID1"></param>
		/// <param name="playerID2"></param>
		public void ChooseTechnologyToTake(short participantIdx1, short participantIdx2)
		{
			this.processor.Journal.OpenScope($"ChooseTechnologyToTake({participantIdx1}, {participantIdx2})");

			// function body
			this.processor.EnterFrame(0x9a);
			this.processor.PreserveSource();
			this.processor.CompareWord((ushort)participantIdx1, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L0703;
			goto L08c1;

		L0703:
			this.processor.CompareWord((ushort)participantIdx2, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L070c;
			goto L08c1;

		L070c:
			this.processor.EmitLocalWord(0x2, 0x0);

			this.owner.Libc.CopyText(0xba06, "Select one...\n ");

			this.processor.EmitLocalWord(0x96, 0xffff);
			this.processor.EmitLocalWord(0x98, 0x47);

		L072d:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx1,
				this.processor.PullLocalSignedWord(0x98));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsNotEqual)
				goto L07b9;

			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(participantIdx2,
				this.processor.PullLocalSignedWord(0x98));
			this.processor.TestAccumulator();
			if (this.processor.FlagSet.IsEqual)
				goto L07b9;

			this.processor.ScaleIndex(0x16, this.processor.PullLocalWord(0x98));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, "\n ");

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x2);
			this.processor.IncrementLocalWord(0x2);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x98);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x94), this.processor.RegAcc.Value16);

			this.owner.Advisors.ObtainTechnologyInfo(participantIdx1, this.processor.PullLocalWord(0x98));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x96));
			if (this.processor.FlagSet.IsLessOrEqual) goto L07b9;

			this.owner.Advisors.ObtainTechnologyInfo(participantIdx1, this.processor.PullLocalWord(0x98));

			this.processor.EmitLocalWord(0x96, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x98, 0x4);

		L07b9:
			this.processor.EmitLocalWord(0x98, this.processor.DecrementWord(this.processor.PullLocalWord(0x98)));
			if (this.processor.FlagSet.IsSigned) goto L07c2;
			goto L072d;

		L07c2:
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L07cb;
			goto L08c1;

		L07cb:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx1, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L0823;

		L07d3:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x3934), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L07ed;

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 32);

			goto L07fe;

		L07ed:
			this.processor.StackWord(0); // stack management - push return segment, ignored
			this.processor.StackWord(0x07fe); // stack management - push return offset
			this.owner.Audience.DisplayKingDialog(0xba06, 0x14, 0x8b);
			this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment

		L07fe:
			this.processor.EmitLocalWord(0x9a, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L07d3;

			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.Advisors.EarnTechnology(participantIdx1,
				this.processor.PullInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x94)),
				participantIdx2);
			
			goto L08c1;

		L0823:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord((ushort)participantIdx2, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L08a1;

			this.processor.RegBase.Value16 = (ushort)participantIdx1;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.CopyText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1992)));

			this.owner.Libc.AppendText(0xba06, " take\n");

			this.processor.ScaleIndex(0x16, this.processor.PullLocalWord(0x4));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4da);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.processor.CompareWord(this.processor.PullGlobalWord(0x3934), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L088d;

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

			goto L08a1;

		L088d:
			this.processor.StackWord(0); // stack management - push return segment, ignored
			this.processor.StackWord(0x089e); // stack management - push return offset
			this.owner.Audience.DisplayKingDialog(0xba06, 0x14, 0x8b);
			this.processor.UnstackDoubleWord(); // stack management - pop return offset and segment

		L08a1:
			this.owner.Advisors.EarnTechnology(participantIdx1,
				this.processor.PullLocalSignedWord(0x4),
				participantIdx2);
			
			this.owner.Bot.RefreshCityProduction(participantIdx1, 0xffff);

		L08c1:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("ChooseTechnologyToTake");
		}

		/// <summary>
		/// Appends the name of the city (13 characters of GameState.TownLabels) to the text at 0xba06, "NONE" for city -1
		/// </summary>
		public void ObtainCityLabel(short townIdx)
		{
			this.processor.Journal.OpenScope($"ObtainCityLabel({townIdx})");

			if (townIdx != -1)
			{
				byte townLabelIdx = this.owner.World.Towns[townIdx].LabelIdx;
				ushort charsDisplacement = (ushort)(0xba06 + this.owner.Libc.TextLength(0xba06));

				for (int idx = 0; idx < 0xd; idx++)
				{
					this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(charsDisplacement + idx), (byte)this.owner.World.TownLabels[townLabelIdx][idx]);
				}
			}
			else
			{
				this.owner.Libc.AppendText(0xba06, "NONE");
			}

			this.processor.Journal.CloseScope("ObtainCityLabel");
		}

		/// <summary>
		/// Waits for a key or a mouse click; a key is taken (with the second code of an extended key)
		/// </summary>
		public void AwaitForKeyPressOrMouseClick()
		{
			this.processor.Journal.OpenScope("AwaitForKeyPressOrMouseClick()");

			this.owner.TurnLogic.AwaitForKey();

			do
			{
				this.owner.Startup.ObtainMouseState();

				if (this.owner.GlobDb3a != 0)
					break;
			}
			while (this.owner.Libc.KeyAvailable() == 0);

			if (this.owner.GlobDb3a == 0 && this.owner.Libc.ReadKey() == 0)
			{
				this.owner.Libc.ReadKey();
			}

			this.processor.Journal.CloseScope("AwaitForKeyPressOrMouseClick");
		}

		/// <summary>
		/// A caravan enters a city: the revenue ((distance + 10) * (trade of both cities) / 24, halved on the same continent and
		/// for an own city, less a third each with the technologies 0x31 and 0x26 of the city's owner) goes to the gold and the
		/// science of the player (a message for the human player), the caravan is removed and the city becomes a trade route of
		/// its home city, in place of the route with the least trade (an empty slot first), unless that one has more.
		/// </summary>
		/// <remarks>The continent of the home city is looked up at (x, x), like in the original.</remarks>
		/// <returns>1, also in AX</returns>
		public ushort FoundTradeRoute(short participantIdx, short troopIdx, short townIdx)
		{
			this.processor.Journal.OpenScope($"FoundTradeRoute({participantIdx}, {troopIdx}, {townIdx})");

			// DisplayDialog leaves SI in DI: both are given back to the caller as they were
			ushort source = this.processor.RegSource.Value16;
			ushort dest = this.processor.RegDest.Value16;

			UnitMemory troops = new UnitMemory(this.processor);
			ushort homeIdx = (ushort)troops.PullSigned(participantIdx, troopIdx, UnitMemory.BaseTown);
			City town = this.owner.World.Towns[townIdx];
			City home = this.owner.World.Towns[homeIdx];

			int distance = this.owner.Helpers.ObtainDistance(town.Place.X, home.Place.X, town.Place.Y, home.Place.Y);
			short revenue = (short)((home.RootCommerce + town.RootCommerce) * (short)(distance + 0xa));
			revenue = (short)(revenue / 0x18);

			short homeHoriz = (short)home.Place.X;

			if (this.owner.MapOps.ObtainContinentID(homeHoriz, homeHoriz) == this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y))
			{
				revenue = (short)(revenue / 2);
			}

			if (town.ParticipantIdx == participantIdx)
			{
				revenue = (short)(revenue / 2);
			}

			if (this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, 0x31) != 0)
			{
				revenue = (short)(revenue - (revenue / 3));
			}

			if (this.owner.Advisors.PlayerKnowsTechnology(town.ParticipantIdx, 0x26) != 0)
			{
				revenue = (short)(revenue - (revenue / 3));
			}

			this.owner.World.Participants[participantIdx].Gold += revenue;
			this.owner.World.Participants[participantIdx].InquiryHeadway += revenue;

			if (participantIdx == this.owner.World.PersonParticipantIdx)
			{
				this.owner.Libc.CopyText(0xba06, this.processor.PullGlobalWord((ushort)(((troopIdx & 7) << 1) + 0x1972)));

				this.owner.Libc.AppendText(0xba06, " caravan from ");

				ObtainCityLabel((short)homeIdx);

				this.owner.Libc.AppendText(0xba06, "\narrives in ");

				ObtainCityLabel(townIdx);

				this.owner.Libc.AppendText(0xba06, "\nTrade route established\nRevenue: $");

				this.owner.Libc.AppendText(0xba06, this.owner.Libc.IntegerToText(revenue, 10));

				this.owner.Libc.AppendText(0xba06, ".\n");

				this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

				this.owner.TurnHooks.RenderStatusBar();
			}

			this.owner.UnitOps.EliminateUnit(participantIdx, troopIdx);

			short trade = RouteTrade(townIdx, participantIdx);
			int replacedIdx = -1;

			for (int routeIdx = 0; routeIdx < 3; routeIdx++)
			{
				ushort routeTownIdx = (byte)home.CommerceTownIndices[routeIdx];

				if (routeTownIdx == (ushort)townIdx)
				{
					replacedIdx = -1;
					break;
				}

				short routeTrade = routeTownIdx == 0xff ? (short)-1 : RouteTrade((short)routeTownIdx, participantIdx);

				if (routeTrade < trade)
				{
					trade = routeTrade;
					replacedIdx = routeIdx;
				}
			}

			if (replacedIdx != -1)
			{
				home.CommerceTownIndices[replacedIdx] = (sbyte)townIdx;
			}

			this.processor.RegSource.Value16 = source;
			this.processor.RegDest.Value16 = dest;
			this.processor.RegAcc.Value16 = 1;

			// Far return
			this.processor.Journal.CloseScope("FoundTradeRoute");

			return 1;
		}

		/// <summary>The trade of the city as a trade route of the player: doubled for a foreign city</summary>
		private short RouteTrade(short townIdx, short participantIdx)
		{
			short trade = this.owner.World.Towns[townIdx].RootCommerce;

			return this.owner.World.Towns[townIdx].ParticipantIdx == participantIdx ? trade : (short)(trade << 1);
		}
	}
}
