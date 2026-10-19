using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;

namespace Civilization1Remaster.Game
{
	/// <summary>
	/// Translated from the original CIV.EXE segment 0x1d12.
	/// </summary>
	public class CityManagement
	{
		private readonly GameEngine owner;
		private readonly CPU processor;
		private readonly UnitMemory troops;

		public CityManagement(GameEngine owner)
		{
			this.owner = owner;
			this.processor = owner.Processor;
			this.troops = new UnitMemory(owner.Processor);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <returns></returns>
		public ushort ObtainCityTileIndex(short horizLoc, short vertLoc)
		{
			this.processor.Journal.OpenScope($"ObtainCityTileIndex({horizLoc}, {vertLoc})");

			int index = CitizenSpecialistRules.FindCityTileIndex(horizLoc, vertLoc);
			this.processor.RegAcc.Value16 = (index >= 0) ? (ushort)index : (ushort)0xffff;

			this.processor.Journal.CloseScope("ObtainCityTileIndex");
			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// The city screen, first part: the units of the city listed, the screen cleared, the title "NAME (Pop:...)" (0xe8
		/// the population sum 1 + 2 + ... + size), the map of the city (the view 0xd4cc / 0xd75e on it): each of the 0x6540
		/// tiles around that the owner has seen (all for a computer player) is drawn, a tile of another player only repainted,
		/// a tile worked by a neighbouring city (the array at BP-0x8e) framed. The locals of HandleCity and BX, SI, DI are used
		/// as in the original.
		/// </summary>
		private void RenderCityHeaderAndMap(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			this.owner.UnitOps.ConstructUnitsFromCityList(townIdx);
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);
			RenderFrame(2, 1, 208, 21);

			// The population: 1 + 2 + ... + the size
			this.processor.EmitLocalWord(0xe8, 0);
			ushort citizen = 1;

			for (; town.TrueExtent >= (short)citizen; citizen++)
			{
				this.processor.EmitLocalWord(0xea, citizen);
				this.processor.EmitLocalWord(0xe8, (ushort)(this.processor.PullLocalWord(0xe8) + citizen));
			}

			this.processor.EmitLocalWord(0xea, citizen);
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 2);

			this.processor.EmitGlobalByte(0xba06, 0);
			this.owner.Conquest.ObtainCityLabel(townIdx);
			this.owner.Libc.UppercaseText(0xba06);
			this.owner.Libc.AppendText(0xba06, " (Pop:");
			this.owner.Helpers.PopulationToText(this.processor.PullLocalSignedWord(0xe8));
			this.owner.Libc.AppendText(0xba06, ")");
			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 104, 2, 15);

			RenderFrame(127, 23, 208, 104);

			// The map of the city
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.EmitGlobalWord(0xd4cc, (ushort)((short)town.Place.X - 5));
			this.processor.EmitGlobalWord(0xd75e, (ushort)((short)town.Place.Y - 3));
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);

			this.owner.MapOps.RenderMapTile(town.Place.X, town.Place.Y);

			ushort tile = 0;

			for (; (short)tile < (short)this.processor.PullGlobalWord(0x6540); tile++)
			{
				this.processor.EmitLocalWord(0xea, tile);
				short horizLoc = (short)(town.Place.X + this.owner.TownDeltas[tile].X);
				short vertLoc = (short)(town.Place.Y + this.owner.TownDeltas[tile].Y);
				this.processor.EmitLocalWord(0xc6, (ushort)horizLoc);
				this.processor.EmitLocalWord(0xd2, (ushort)vertLoc);

				short owner = (short)this.processor.PullGlobalWord(0x6548);

				if (owner == this.owner.World.PersonParticipantIdx &&
					(this.owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc] & (1 << (owner & 0xff))) == 0)
				{
					continue;
				}

				this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc);
				this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);

				if (this.processor.PullLocalWord(0xe8) != 0xffff && this.processor.PullLocalWord(0xe8) != (ushort)owner)
				{
					// A tile of another player
					this.owner.MapOps.RepaintMapTile(horizLoc, vertLoc);
					continue;
				}

				this.owner.MapOps.RenderMapTile(horizLoc, vertLoc);

				this.processor.RegSource.Value16 = (ushort)(tile << 1);
				this.processor.RegDest.Value16 = (ushort)(short)(10 * this.owner.TownDeltas[tile].X + this.owner.TownDeltas[tile].Y * 2);

				if (this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x8e)) != 0)
				{
					// A tile worked by a neighbouring city
					this.owner.Dialogs.RenderBox(
						(horizLoc - this.processor.PullGlobalSignedWord(0xd4cc)) * 16 + 80,
						(vertLoc - this.processor.PullGlobalSignedWord(0xd75e)) * 16 + 8,
						15, 15, 12);
				}
			}

			this.processor.EmitLocalWord(0xea, tile);
		}

		/// <summary>
		/// The food of the city in its turn: the flag 0x80 cleared; with the food storage below zero a settler of the city
		/// starves (the human is told), else the city is lost (the translated code tests the flags of the earlier comparison
		/// instead of the size, so it is always lost: kept, see the backlog); with the storage at (size + 1) x 0x4a the city
		/// grows (a granary keeps 5 x size + 5), above 10 without an aqueduct it stays (with the bit 4 of DiagMarkers; the human
		/// is told). False when the city is lost. The locals of HandleCity and BX, SI are used as in the original.
		/// </summary>
		private bool FeedCity(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)this.processor.PullGlobalWord(0x6548);

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			town.ConditionMarker &= 0x7f;

			if (town.GrainTally < 0)
			{
				// Starvation: a settler of the city, or the city
				this.processor.EmitLocalWord(0xe8, 0xffff);
				ushort troop = 0;

				for (; troop < UnitMemory.TroopTally; troop++)
				{
					this.processor.EmitLocalWord(0xea, troop);
					this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
					this.processor.RegSource.Value16 = (ushort)(troop * UnitMemory.TroopStride);

					if (this.troops.Pull(owner, troop, UnitMemory.Kind) == 0 && this.troops.PullSigned(owner, troop, UnitMemory.BaseTown) == townIdx)
					{
						this.processor.EmitLocalWord(0xe8, troop);
						break;
					}
				}

				if (troop == UnitMemory.TroopTally)
					this.processor.EmitLocalWord(0xea, troop);

				if (owner == this.owner.World.PersonParticipantIdx)
				{
					this.owner.Libc.CopyText(0xba06, "Food storage exhausted\nin ");
					this.owner.Conquest.ObtainCityLabel(townIdx);
					this.owner.Libc.AppendText(0xba06, (ushort)(this.processor.PullLocalWord(0xe8) != 0xffff ? 0x2510 : 0x2522));
					this.owner.Services.PerformSound(0x24, 0);
					this.owner.Paper.DisplayNewspaper(townIdx);
					this.owner.Services.PerformSound(1, 0);
				}

				if (this.processor.PullLocalWord(0xe8) != 0xffff)
				{
					this.owner.UnitOps.EliminateUnit(owner, this.processor.PullLocalSignedWord(0xe8));
				}
				else
				{
					// The city is lost (whatever its size, as the translated code does)
					this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
					town.TrueExtent--;
					this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
					this.owner.Advisors.FinalizeProduction(townIdx, (ushort)(short)town.Place.X, (ushort)(short)town.Place.Y);
					this.owner.NewGameWizard.EliminateCivilization((short)this.processor.PullGlobalWord(0x6548), 0);
					this.owner.Startup.DisplayMouseCursor();
					return false;
				}

				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
				town.GrainTally = 0;
				this.owner.TurnLogic.RepaintTileIfVisible(this.processor.PullLocalWord(0xd8), this.processor.PullLocalWord(0xe4));
			}

			// Growth
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);

			if ((short)((town.TrueExtent + 1) * (short)this.processor.PullLocalWord(0x4a)) > town.GrainTally)
				return true;

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			town.TrueExtent++;
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);

			// A granary keeps half of the food
			town.GrainTally = (short)((town.StructureMarkers0 & 4) != 0 ? (short)(5 * town.TrueExtent) + 5 : 0);
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.TrueExtent > 10 && (town.StructureMarkers0 & 0x100) == 0 && (this.owner.World.DiagMarkers & 4) != 0)
			{
				// No growth above 10 without an aqueduct
				town.TrueExtent--;

				if (this.processor.PullGlobalWord(0x6548) == (ushort)this.owner.World.PersonParticipantIdx)
				{
					this.processor.EmitGlobalWord(0x2f9e, 4);
					this.processor.EmitGlobalByte(0xba06, 0);
					this.owner.Conquest.ObtainCityLabel(townIdx);
					this.owner.Libc.AppendText(0xba06, " requires an AQUEDUCT\nfor further growth.\n");
					this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
				}
			}

			this.owner.TurnLogic.RepaintTileIfVisible(this.processor.PullLocalWord(0xd8), this.processor.PullLocalWord(0xe4));
			return true;
		}

		/// <summary>
		/// The units of the city: 0x50 the size + 1; 0xdeb8 the special slots of the city in use (Unfamiliar) and the units
		/// it supports (but diplomats and caravans), 0xd2f6 those above the size (and every one for a government above
		/// monarchy with the bit 2 of DiagMarkers), 0x6546 the attackers away from the city (planes always), 0xe3c6 the
		/// settlers. BX, SI are left as in the original.
		/// </summary>
		private void CountCityUnits(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)this.processor.PullGlobalWord(0x6548);

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.EmitLocalWord(0x50, (ushort)(town.TrueExtent + 1));
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitGlobalWord(0x6546, 0);
			this.processor.EmitGlobalWord(0xdeb8, 0);
			this.processor.EmitGlobalWord(0xd2f6, 0);
			this.processor.EmitGlobalWord(0xe3c6, 0);

			// The special slots of the city
			for (ushort slot = 0; slot < 2; slot++)
			{
				this.processor.EmitLocalWord(0xea, slot);
				this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
				this.processor.RegBase.Value16 = slot;

				if ((byte)town.Unfamiliar[slot] != 0xff)
					this.processor.EmitGlobalWord(0xdeb8, (ushort)(this.processor.PullGlobalWord(0xdeb8) + 1));
			}

			this.processor.EmitLocalWord(0xea, 2);
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.TrueExtent < (short)this.processor.PullGlobalWord(0xdeb8))
			{
				this.processor.RegCount.Value16 = (ushort)(this.processor.PullGlobalWord(0xdeb8) - town.TrueExtent);
				this.processor.EmitGlobalWord(0xd2f6, this.processor.RegCount.Value16);
			}

			// The units the city supports
			for (ushort troop = 0; troop < UnitMemory.TroopTally; troop++)
			{
				this.processor.EmitLocalWord(0xea, troop);
				this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
				this.processor.RegSource.Value16 = (ushort)(troop * UnitMemory.TroopStride);

				if (this.troops.Pull(owner, troop, UnitMemory.Kind) == 0xff || this.troops.PullSigned(owner, troop, UnitMemory.BaseTown) != townIdx)
					continue;

				if (this.troops.PullSigned(owner, troop, UnitMemory.Kind) < 0x1a)
				{
					this.processor.EmitGlobalWord(0xdeb8, (ushort)(this.processor.PullGlobalWord(0xdeb8) + 1));

					if (town.TrueExtent < (short)this.processor.PullGlobalWord(0xdeb8) ||
						(this.owner.World.Participants[owner].RegimeKind > 1 && (this.owner.World.DiagMarkers & 2) != 0))
					{
						this.processor.EmitGlobalWord(0xd2f6, (ushort)(this.processor.PullGlobalWord(0xd2f6) + 1));
					}

					// An attacker away from the city (a plane always)
					if (this.troops.KindField(owner, troop, UnitKindTable.AssaultPower) != 0 &&
						(this.troops.KindField(owner, troop, UnitKindTable.LandGroup) == UnitKindTable.AirUnits ||
							this.troops.Pull(owner, troop, UnitMemory.Horiz) != (byte)(sbyte)town.Place.X ||
							this.troops.Pull(owner, troop, UnitMemory.Vert) != (byte)(sbyte)town.Place.Y))
					{
						this.processor.EmitGlobalWord(0x6546, (ushort)(this.processor.PullGlobalWord(0x6546) + 1));
					}

					this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
					this.processor.RegSource.Value16 = (ushort)(troop * UnitMemory.TroopStride);
				}

				if (this.troops.Pull(owner, troop, UnitMemory.Kind) == 0)
					this.processor.EmitGlobalWord(0xe3c6, (ushort)(this.processor.PullGlobalWord(0xe3c6) + 1));
			}

			this.processor.EmitLocalWord(0xea, UnitMemory.TroopTally);
		}

		/// <summary>
		/// The tiles the city cannot work (the array at BP-0x3e, one word per tile around): those the owner has not seen (not
		/// for the barbarians), those of another player (marked also in the array at BP-0x8e; 0x70e8 = 1 when that is the
		/// human) and those worked by a neighbouring city; their workers are taken off. Then the 4 words at 0x70da cleared.
		/// The locals of HandleCity and BX, CX, SI, DI are used as in the original.
		/// </summary>
		private void MarkUnavailableTiles(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];

			this.processor.EmitGlobalWord(0x70e8, 0);
			ushort tile = 0;

			for (; (short)tile < (short)this.processor.PullGlobalWord(0x6540); tile++)
			{
				this.processor.EmitLocalWord(0xea, tile);
				this.processor.RegSource.Value16 = (ushort)(tile << 1);
				ushort unavailable = (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e);
				this.processor.EmitUInt16(this.processor.SegStack.Value16, unavailable, 0);

				short horizLoc = (short)(town.Place.X + this.owner.TownDeltas[tile].X);
				short vertLoc = (short)(town.Place.Y + this.owner.TownDeltas[tile].Y);
				this.processor.EmitLocalWord(0xc6, (ushort)horizLoc);
				this.processor.EmitLocalWord(0xd2, (ushort)vertLoc);

				short owner = (short)this.processor.PullGlobalWord(0x6548);

				// A tile the owner has not seen (the barbarians see all)
				if (owner != 0 && (this.owner.World.ChartSight[(ushort)horizLoc, (ushort)vertLoc] & (1 << (owner & 0xff))) == 0)
					this.processor.EmitUInt16(this.processor.SegStack.Value16, unavailable, 1);

				this.owner.MapOps.ObtainTileOwner(horizLoc, vertLoc);
				this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);

				ushort shared = (ushort)(short)(10 * this.owner.TownDeltas[tile].X + this.owner.TownDeltas[tile].Y * 2);

				if (this.processor.PullLocalWord(0xe8) != 0xffff && this.processor.PullLocalWord(0xe8) != this.processor.PullGlobalWord(0x6548))
				{
					// A tile of another player
					this.processor.RegDest.Value16 = shared;
					this.processor.RegAcc.Value16 = 1;
					this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + shared - 0x8e), 1);
					this.processor.EmitUInt16(this.processor.SegStack.Value16, unavailable, 1);
					this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;

					if (this.processor.PullLocalWord(0xe8) == (ushort)this.owner.World.PersonParticipantIdx)
						this.processor.EmitGlobalWord(0x70e8, 1);
				}

				// A tile worked by a neighbouring city
				this.processor.RegDest.Value16 = shared;

				if (this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + shared - 0x8e)) != 0)
					this.processor.EmitUInt16(this.processor.SegStack.Value16, unavailable, 1);

				if (this.processor.PullUInt16(this.processor.SegStack.Value16, unavailable) == 0)
					continue;

				// Its worker is taken off
				uint mask = ~(1u << (tile & 0xff));
				this.processor.RegCount.Value16 = (ushort)mask;
				this.processor.RegBase.Value16 = (ushort)(mask >> 16);
				this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
				town.LaborerMarkers0 &= this.processor.RegCount.Value16;
				town.LaborerMarkers1 &= this.processor.RegBase.Value16;
			}

			this.processor.EmitLocalWord(0xea, tile);

			for (ushort slot = 0; slot < 4; slot++)
			{
				this.processor.EmitLocalWord(0xea, slot);
				this.processor.RegBase.Value16 = (ushort)(slot << 1);
				this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x70da), 0);
			}

			this.processor.EmitLocalWord(0xea, 4);
		}

		/// <summary>
		/// The workers the city keeps: a computer city every fourth turn (without the flag 1 and with no tile of the human
		/// around, 0x70e8) drops them all and picks anew (true). Else 0x50 = size + 1 less the workers: not too many, they are
		/// kept (drawn, marked in the array at BP-0x3e and in 0xee / 0xec); too many, all go (the human is told in the turn).
		/// True when workers are still to be picked (0x50 neither the specialists 0xe8b8 nor 0). The locals of HandleCity and
		/// AX, BX, CX, DX, SI are used as in the original.
		/// </summary>
		private bool KeepWorkers(short townIdx, short marker)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)this.processor.PullGlobalWord(0x6548);

			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0xec, 0);
			this.processor.EmitLocalWord(0xee, 0);

			if ((this.owner.World.ParticipantMarkers & (1 << (owner & 0xff))) == 0 && ((townIdx + this.owner.World.RoundTally) & 3) == 0)
			{
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

				if ((town.ConditionMarker & 1) == 0 && this.processor.PullGlobalWord(0x70e8) == 0)
					return true;
			}

			// 0x50: the size + 1 less the workers
			ushort tile = 0;

			for (; (short)tile < (short)this.processor.PullGlobalWord(0x6540); tile++)
			{
				this.processor.EmitLocalWord(0xea, tile);

				if (TestWorker(townIdx, tile))
					this.processor.EmitLocalWord(0x50, (ushort)(this.processor.PullLocalWord(0x50) - 1));
			}

			this.processor.EmitLocalWord(0xea, tile);

			if ((short)this.processor.PullLocalWord(0x50) >= 0)
			{
				// Not too many: the workers are kept
				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
				this.processor.EmitLocalWord(0x50, (ushort)(town.TrueExtent + 1));

				for (tile = 0; (short)tile < (short)this.processor.PullGlobalWord(0x6540); tile++)
				{
					this.processor.EmitLocalWord(0xea, tile);

					if (this.processor.PullLocalWord(0x50) == 0 || !TestWorker(townIdx, tile))
						continue;

					this.processor.RegSource.Value16 = (ushort)(tile << 1);
					this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e), 1);

					RenderCityTile(townIdx, tile, marker);

					this.processor.EmitLocalWord(0x50, (ushort)(this.processor.PullLocalWord(0x50) - 1));
					uint mask = 1u << (tile & 0xff);
					this.processor.RegAcc.Value16 = (ushort)mask;
					this.processor.RegData.Value16 = (ushort)(mask >> 16);
					this.processor.EmitLocalWord(0xee, (ushort)(this.processor.PullLocalWord(0xee) | this.processor.RegAcc.Value16));
					this.processor.EmitLocalWord(0xec, (ushort)(this.processor.PullLocalWord(0xec) | this.processor.RegData.Value16));
				}

				this.processor.EmitLocalWord(0xea, tile);
			}
			else
			{
				// Too many: all go
				if (marker == 0 && owner == this.owner.World.PersonParticipantIdx)
				{
					this.owner.Libc.CopyText(0xba06, "Population decrease\nin ");
					this.owner.Conquest.ObtainCityLabel(townIdx);
					this.owner.Libc.AppendText(0xba06, ".\n");
					this.processor.EmitGlobalWord(0x2f9e, 4);
					this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);
					this.processor.EmitGlobalWord(0xb1e8, 1);
				}

				this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
				this.processor.EmitLocalWord(0x50, (ushort)(town.TrueExtent + 1));
			}

			return this.processor.PullLocalWord(0x50) != this.processor.PullGlobalWord(0xe8b8) && this.processor.PullLocalWord(0x50) != 0;
		}

		/// <summary>Whether the tile is worked by the city (CX, BX the masked words of the workers, SI the city, as in the original)</summary>
		private bool TestWorker(short townIdx, ushort tile)
		{
			City town = this.owner.World.Towns[townIdx];
			uint mask = 1u << (tile & 0xff);

			this.processor.RegCount.Value16 = (ushort)(mask & town.LaborerMarkers0);
			this.processor.RegBase.Value16 = (ushort)(((mask >> 16) & town.LaborerMarkers1) | this.processor.RegCount.Value16);
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);

			return this.processor.RegBase.Value16 != 0;
		}

		/// <summary>
		/// The new workers of the city (0x50 the workers still to place): the city tile first (once, 0x16); while the food
		/// (0x70da + half the workers left) does not feed the citizens (2 per size) and the settlers (0x48 each), or the city
		/// is below size 3, the tile with the most food (then the most shields x 2 + trade; none without shields for the last
		/// worker of a city without shields, 0x70dc) is taken while more than the specialists 0xe8b8 are to place and the
		/// last search found one (0x108). Then (a computer city with the flag 1 keeps one more specialist) the tiles with the
		/// best weighted yield: food x 16 / the food left over, shields x (3 x size) / the shields left over (0x70dc less
		/// 0xd2f6), trade x (2 x size) / the trade (0x70de), each divisor kept within 1 - 99. The workers 0xee / 0xec and the
		/// specialists (0x50 in bits 26 - 31) are stored, 0xae = -0x50. The locals of HandleCity and AX, BX, CX, DX, SI are
		/// used as in the original.
		/// </summary>
		private void PickWorkers(short townIdx, short marker, bool pick)
		{
			City town = this.owner.World.Towns[townIdx];

			if (pick)
			{
				if ((short)this.processor.PullLocalWord(0x50) > 0 && this.processor.PullLocalWord(0x16) == 0)
				{
					// The city tile
					this.processor.EmitLocalWord(0x16, 1);
					RenderCityTile(townIdx, 20, marker);
					this.processor.EmitLocalWord(0xee, this.processor.PullLocalWord(0xee));
					this.processor.EmitLocalWord(0xec, (ushort)(this.processor.PullLocalWord(0xec) | 0x10));
					this.processor.EmitLocalWord(0x50, (ushort)(this.processor.PullLocalWord(0x50) - 1));
				}

				// Food first
				while (true)
				{
					this.processor.RegCount.Value16 = (ushort)((short)this.processor.PullLocalWord(0x50) / 2 + this.processor.PullGlobalWord(0x70da));
					this.processor.RegAcc.Value16 = (ushort)((short)this.processor.PullLocalWord(0x48) * (short)this.processor.PullGlobalWord(0xe3c6));
					this.processor.RegBase.Value16 = (ushort)(town.TrueExtent * 2 + this.processor.RegAcc.Value16);

					bool hungry = (short)this.processor.RegBase.Value16 > (short)this.processor.RegCount.Value16 || town.TrueExtent < 3;

					if (!hungry || (short)this.processor.PullLocalWord(0x50) <= (short)this.processor.PullGlobalWord(0xe8b8) ||
						this.processor.PullLocalWord(0x108) == 0xffff)
					{
						break;
					}

					this.processor.EmitLocalWord(0x108, 0xffff);
					this.processor.RegAcc.Value16 = 0;
					this.processor.EmitLocalWord(0xd6, 0);
					this.processor.EmitLocalWord(0xce, 0);
					ushort tile = 0;

					for (; (short)tile < (short)this.processor.PullGlobalWord(0x6540); tile++)
					{
						this.processor.EmitLocalWord(0xea, tile);

						if (!LocateFreeTile(townIdx, tile))
							continue;

						short horizLoc = this.processor.PullLocalSignedWord(0xc6);
						short vertLoc = this.processor.PullLocalSignedWord(0xd2);

						this.processor.EmitLocalWord(0xcc, MayCityWorkTile(horizLoc, vertLoc, 0));
						this.processor.EmitLocalWord(0xd4, (ushort)(MayCityWorkTile(horizLoc, vertLoc, 1) << 1));
						this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xd4);

						if (this.processor.PullLocalWord(0x50) == 1 && this.processor.PullGlobalWord(0x70dc) == 0 && this.processor.PullLocalWord(0xd4) == 0)
							continue;

						this.processor.EmitLocalWord(0xd4, (ushort)(this.processor.PullLocalWord(0xd4) + MayCityWorkTile(horizLoc, vertLoc, 2)));

						short food = (short)this.processor.PullLocalWord(0xcc);
						short best = (short)this.processor.PullLocalWord(0xce);
						this.processor.RegAcc.Value16 = (ushort)best;

						if (food > best || (food == best && (short)this.processor.PullLocalWord(0xd4) > (short)this.processor.PullLocalWord(0xd6)))
						{
							this.processor.EmitLocalWord(0xce, (ushort)food);
							this.processor.CopyLocalWord(0xd4, 0xd6);
							this.processor.CopyLocalWord(0xea, 0x108);
						}
					}

					this.processor.EmitLocalWord(0xea, tile);

					if (this.processor.PullLocalWord(0x108) == 0xffff)
						break;

					TakeWorker(townIdx, marker);
				}

				// A computer city with the flag 1 keeps one more specialist
				if (this.processor.PullGlobalWord(0x6548) != (ushort)this.owner.World.PersonParticipantIdx)
				{
					this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

					if ((town.ConditionMarker & 1) != 0)
						this.processor.EmitGlobalWord(0xe8b8, (ushort)(this.processor.PullGlobalWord(0xe8b8) + 1));
				}

				// The best weighted yield
				while ((short)this.processor.PullLocalWord(0x50) > (short)this.processor.PullGlobalWord(0xe8b8))
				{
					this.processor.EmitLocalWord(0x108, 0xffff);
					this.processor.RegAcc.Value16 = 0;
					this.processor.EmitLocalWord(0xe2, 0);
					this.processor.EmitLocalWord(0xd6, 0);
					this.processor.EmitLocalWord(0xce, 0);
					ushort tile = 0;

					for (; (short)tile < (short)this.processor.PullGlobalWord(0x6540); tile++)
					{
						this.processor.EmitLocalWord(0xea, tile);

						if (!LocateFreeTile(townIdx, tile))
							continue;

						short horizLoc = this.processor.PullLocalSignedWord(0xc6);
						short vertLoc = this.processor.PullLocalSignedWord(0xd2);

						// Food
						short divisor = (short)this.owner.Helpers.VerifyValueSpan(
							this.processor.PullGlobalSignedWord(0x70da) - (town.TrueExtent * 2) - (this.processor.PullLocalSignedWord(0x48) * this.processor.PullGlobalSignedWord(0xe3c6)), 1, 99);
						short weight = (short)(16 / divisor);
						this.processor.RegCount.Value16 = (ushort)weight;
						this.processor.RegSource.Value16 = (ushort)weight;

						short yield = (short)MayCityWorkTile(horizLoc, vertLoc, 0);
						this.processor.EmitLocalWord(0x4, (ushort)yield);
						this.processor.EmitLocalWord(0xd4, (ushort)(yield * weight));

						// Shields
						yield = (short)MayCityWorkTile(horizLoc, vertLoc, 1);
						this.processor.RegSource.Value16 = (ushort)yield;
						this.processor.EmitLocalWord(0xc, (ushort)yield);

						divisor = (short)this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0x70dc) - this.processor.PullGlobalSignedWord(0xd2f6), 1, 99);
						this.processor.RegCount.Value16 = (ushort)divisor;
						this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
						this.processor.EmitLocalWord(0xd4, (ushort)(this.processor.PullLocalWord(0xd4) + (short)(3 * town.TrueExtent) / divisor * yield));

						// Trade
						yield = (short)MayCityWorkTile(horizLoc, vertLoc, 2);
						this.processor.RegSource.Value16 = (ushort)yield;
						this.processor.EmitLocalWord(0x46, (ushort)yield);

						divisor = (short)this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0x70de), 1, 99);
						this.processor.RegCount.Value16 = (ushort)divisor;
						this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
						int product = (short)(town.TrueExtent * 2) / divisor * yield;
						this.processor.RegAcc.Value16 = (ushort)product;
						this.processor.RegData.Value16 = (ushort)(product >> 16);
						this.processor.EmitLocalWord(0xd4, (ushort)(this.processor.PullLocalWord(0xd4) + this.processor.RegAcc.Value16));
						this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xd6);

						if ((short)this.processor.PullLocalWord(0xd4) > (short)this.processor.PullLocalWord(0xd6))
						{
							this.processor.EmitLocalWord(0xd6, this.processor.PullLocalWord(0xd4));
							this.processor.CopyLocalWord(0xea, 0x108);
							this.processor.RegAcc.Value16 = (ushort)(((this.processor.PullLocalWord(0x4) + this.processor.PullLocalWord(0xc)) << 1) + this.processor.PullLocalWord(0x46));
							this.processor.EmitLocalWord(0xe2, this.processor.RegAcc.Value16);
						}
					}

					this.processor.EmitLocalWord(0xea, tile);

					if (this.processor.PullLocalWord(0x108) == 0xffff)
						break;

					TakeWorker(townIdx, marker);
				}
			}

			// The workers and the specialists (bits 26 - 31) stored
			uint workers = (uint)((int)(short)this.processor.PullLocalWord(0x50) << 0x1a) +
				(((uint)this.processor.PullLocalWord(0xec) << 16) | this.processor.PullLocalWord(0xee));
			this.processor.RegCount.Value16 = (ushort)workers;
			this.processor.RegBase.Value16 = (ushort)(workers >> 16);
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			town.LaborerMarkers0 = this.processor.RegCount.Value16;
			town.LaborerMarkers1 = this.processor.RegBase.Value16;
			this.processor.RegAcc.Value16 = (ushort)-(short)this.processor.PullLocalWord(0x50);
			this.processor.EmitLocalWord(0xae, this.processor.RegAcc.Value16);
		}

		/// <summary>A tile around the city still free for a worker (the array at BP-0x3e); 0xc6 / 0xd2 its place</summary>
		private bool LocateFreeTile(short townIdx, ushort tile)
		{
			City town = this.owner.World.Towns[townIdx];

			this.processor.RegSource.Value16 = (ushort)(tile << 1);

			if (this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e)) != 0)
				return false;

			this.processor.EmitLocalWord(0xc6, (ushort)(short)(town.Place.X + this.owner.TownDeltas[tile].X));
			this.processor.EmitLocalWord(0xd2, (ushort)(short)(town.Place.Y + this.owner.TownDeltas[tile].Y));
			return true;
		}

		/// <summary>The tile 0x108 gets a worker: marked, drawn, in 0xee / 0xec; 0x50 one less</summary>
		private void TakeWorker(short townIdx, short marker)
		{
			ushort tile = this.processor.PullLocalWord(0x108);

			this.processor.RegSource.Value16 = (ushort)(tile << 1);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x3e), 1);

			RenderCityTile(townIdx, tile, marker);

			uint mask = 1u << (tile & 0xff);
			this.processor.RegAcc.Value16 = (ushort)mask;
			this.processor.RegData.Value16 = (ushort)(mask >> 16);
			this.processor.EmitLocalWord(0xee, (ushort)(this.processor.PullLocalWord(0xee) | this.processor.RegAcc.Value16));
			this.processor.EmitLocalWord(0xec, (ushort)(this.processor.PullLocalWord(0xec) | this.processor.RegData.Value16));
			this.processor.EmitLocalWord(0x50, (ushort)(this.processor.PullLocalWord(0x50) - 1));
		}

		/// <summary>
		/// After the workers: 0x8 the specialists; the first 0x8 citizen icons without a type become type 3, the rest of the
		/// 8 type 0. The factories: 0xf8 the factory bonus (2 the factory, 4 the manufacturing plant), 0xac the power (2 the
		/// power plant, hydro plant, nuclear plant or the wonder 15 on the same continent), 0x6c98 the pollution factor (1;
		/// 2 for the clean plants, 3 the mass transit); the shields 0x70dc (kept in 0x10a, the trade in 0xa) grow by a
		/// quarter of the factory bonus and of the power within it. The locals of HandleCity and AX - DX, SI, DI are used
		/// as in the original.
		/// </summary>
		private void PrepareCitizensAndFactories(short townIdx, short marker)
		{
			City town = this.owner.World.Towns[townIdx];

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.EmitLocalWord(0xd8, (ushort)(short)town.Place.X);
			this.processor.EmitLocalWord(0xe4, (ushort)(short)town.Place.Y);
			this.processor.CopyLocalWord(0x50, 0x8);

			// The citizen icons
			ushort citizen = 0;

			for (; (short)citizen < (short)this.processor.PullLocalWord(0x8); citizen++)
			{
				this.processor.EmitLocalWord(0xea, citizen);

				if (ObtainCitizenType(citizen) == 0)
					AssignCitizenType(citizen, 3);
			}

			for (citizen = this.processor.PullLocalWord(0x8); (short)citizen < 8; citizen++)
			{
				this.processor.EmitLocalWord(0xea, citizen);
				AssignCitizenType(citizen, 0);
			}

			this.processor.EmitLocalWord(0xea, citizen);

			// The factories and the power
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0xac, 0);
			this.processor.EmitLocalWord(0xf8, 0);
			this.processor.EmitGlobalWord(0x6c98, 1);
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if ((town.StructureMarkers0 & 0x4000) != 0)
				this.processor.EmitLocalWord(0xf8, 2);

			if ((town.StructureMarkers0 & 0x8000) != 0)
				this.processor.EmitLocalWord(0xf8, 4);

			if ((town.StructureMarkers1 & 0x4) != 0)
				this.processor.EmitLocalWord(0xac, 2);

			if ((town.StructureMarkers1 & 0x8) != 0)
			{
				this.processor.EmitGlobalWord(0x6c98, 2);
				this.processor.EmitLocalWord(0xac, 2);
			}

			if ((town.StructureMarkers1 & 0x10) != 0)
			{
				this.processor.EmitGlobalWord(0x6c98, 2);
				this.processor.EmitLocalWord(0xac, 2);
			}

			if (PlayerEnjoysWonderEffect((short)this.processor.PullGlobalWord(0x6548), 15) != 0)
			{
				// The wonder powers the cities on its continent
				short marvelIdx = this.owner.World.MarvelTownIdx[15];
				this.processor.RegSource.Value16 = (ushort)(marvelIdx * 0x1c);
				this.processor.RegDest.Value16 = (ushort)(townIdx * 0x1c);

				this.owner.MapOps.ObtainContinentID(this.owner.World.Towns[marvelIdx].Place.X, this.owner.World.Towns[marvelIdx].Place.Y);
				this.processor.EmitLocalWord(0x10c, this.processor.RegAcc.Value16);

				this.owner.MapOps.ObtainContinentID(town.Place.X, town.Place.Y);

				if (this.processor.RegAcc.Value16 == this.processor.PullLocalWord(0x10c))
				{
					this.processor.EmitGlobalWord(0x6c98, 2);
					this.processor.EmitLocalWord(0xac, 2);
				}
			}

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if ((town.StructureMarkers1 & 0x2) != 0)
				this.processor.EmitGlobalWord(0x6c98, 3);

			this.processor.EmitLocalWord(0xac, (ushort)this.owner.Helpers.VerifyValueSpan(this.processor.PullLocalSignedWord(0xac), 0, this.processor.PullLocalSignedWord(0xf8)));
			this.processor.EmitLocalWord(0x10a, this.processor.PullGlobalWord(0x70dc));
			this.processor.EmitLocalWord(0xa, this.processor.PullGlobalWord(0x70de));

			// The shields grow by a quarter of the power and of the factory bonus
			short shields = (short)this.processor.PullGlobalWord(0x70dc);
			short power = (short)((short)((short)this.processor.PullLocalWord(0xac) * shields) / 4);
			short factory = (short)((short)this.processor.PullLocalWord(0xf8) * shields);
			this.processor.RegBase.Value16 = (ushort)power;
			this.processor.RegCount.Value16 = 2;
			this.processor.RegData.Value16 = (ushort)(factory < 0 ? 0xffff : 0);
			this.processor.RegAcc.Value16 = (ushort)(factory / 4 + power);
			this.processor.EmitGlobalWord(0x70dc, (ushort)(this.processor.PullGlobalWord(0x70dc) + this.processor.RegAcc.Value16));

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.RegData.Value16 = (ushort)((marker < 0 ? 0xffff : 0) | (ushort)marker);
		}

		/// <summary>Where HandleCity goes on after the food and the shields of the turn (ProduceInCity)</summary>
		private enum ProductionStep
		{
			/// <summary>A unit was built (L174b)</summary>
			UnitBuilt,

			/// <summary>Not enough shields for the unit (L1b60)</summary>
			NotEnough,

			/// <summary>The city builds an improvement or a wonder (L1b63)</summary>
			Improvement,

			/// <summary>Settlers of a city of size 1 on the easiest level wait (L2307)</summary>
			SettlersWait,

			/// <summary>The city is gone (L6927)</summary>
			CityLost,
		}

		/// <summary>
		/// The turn of the city, first part: the food stored grows by the surplus (the citizens eat 2, the settlers 0x48),
		/// the shields by 0x70dc less the support of the units (0xd2f6; nothing with the flag 1). A unit (price x 0x4a) is built
		/// when the shields suffice: the replay notes the first one of its kind, the barracks make it veteran; settlers
		/// make the city smaller (a city of size 1 is lost, unless it is the last one of its owner). A computer player builds
		/// no diplomats (the shields go). The locals of HandleCity (0xba the new unit) and BX, SI are used as in the original.
		/// </summary>
		private ProductionStep ProduceInCity(short townIdx)
		{
			City town = this.owner.World.Towns[townIdx];
			short owner = (short)this.processor.PullGlobalWord(0x6548);

			// The food and the shields of the turn
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.RegCount.Value16 = (ushort)(town.TrueExtent * 2);
			this.processor.RegAcc.Value16 = (ushort)((short)this.processor.PullLocalWord(0x48) * (short)this.processor.PullGlobalWord(0xe3c6));
			this.processor.RegData.Value16 = (ushort)(this.processor.PullGlobalWord(0x70da) - this.processor.RegAcc.Value16 - this.processor.RegCount.Value16);
			town.GrainTally += (short)this.processor.RegData.Value16;

			this.processor.EmitLocalWord(0xe8, (ushort)(this.processor.PullGlobalWord(0x70dc) - this.processor.PullGlobalWord(0xd2f6)));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2f6);
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if ((town.ConditionMarker & 1) != 0)
				this.processor.EmitLocalWord(0xe8, 0);

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			town.MaterialsTally += (short)this.processor.RegAcc.Value16;

			if (town.PresentMakingIdx < 0)
				return ProductionStep.Improvement;

			// A unit
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.RegBase.Value16 = (ushort)(town.PresentMakingIdx * UnitKindTable.Stride);
			short cost = (short)((short)this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + UnitKindTable.Origin + UnitKindTable.Price)) *
				(short)this.processor.PullLocalWord(0x4a));

			if (cost > town.MaterialsTally)
				return ProductionStep.NotEnough;

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.PresentMakingIdx == 0 && town.TrueExtent == 1 && this.owner.World.HardnessTier == 0)
				return ProductionStep.SettlersWait;

			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.RegBase.Value16 = (ushort)(town.PresentMakingIdx * UnitKindTable.Stride);
			town.MaterialsTally -= cost;
			this.processor.EmitLocalWord(0xba, 0xffff);

			if (owner == this.owner.World.PersonParticipantIdx || town.PresentMakingIdx != 0x1a)
			{
				// (a computer player builds no diplomats)
				this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
				this.processor.EmitLocalWord(0xba, this.owner.UnitOps.ConstructUnit(owner, town.PresentMakingIdx, town.Place.X, town.Place.Y));
			}

			// The first unit of its kind goes into the replay
			if ((this.owner.World.ScienceLeadingFoundVia[town.PresentMakingIdx] & 8) == 0)
			{
				this.owner.UnitOps.AttachReplayContent(6, this.processor.PullGlobalByte(0x6548), (byte)town.PresentMakingIdx);
				this.owner.World.ScienceLeadingFoundVia[town.PresentMakingIdx] |= 8;
			}

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if ((town.StructureMarkers0 & 2) != 0 && this.processor.PullLocalWord(0xba) != 0xffff)
			{
				// The barracks make it veteran
				short troopIdx = (short)this.processor.PullLocalWord(0xba);
				this.processor.RegBase.Value16 = (ushort)(owner * UnitMemory.ParticipantStride);
				this.processor.RegSource.Value16 = (ushort)(troopIdx * UnitMemory.TroopStride);
				this.troops.Assign(owner, troopIdx, UnitMemory.Status, (byte)(this.troops.Pull(owner, troopIdx, UnitMemory.Status) | 0x20));
			}

			if (this.processor.PullLocalWord(0xba) == 0xffff)
				return ProductionStep.UnitBuilt;

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (town.PresentMakingIdx != 0)
				return ProductionStep.UnitBuilt;

			// Settlers make the city smaller (but the last city of size 1)
			if (town.TrueExtent <= 1 && this.owner.World.Participants[owner].TownTally <= 1)
				return ProductionStep.UnitBuilt;

			sbyte size = town.TrueExtent;
			town.TrueExtent--;

			// (the translated code tests the flags of the comparison of the size with 1: the same as the size becoming 0)
			if (size != 1)
				return ProductionStep.UnitBuilt;

			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			this.owner.Advisors.FinalizeProduction(townIdx, (ushort)(short)town.Place.X, (ushort)(short)town.Place.Y);
			this.processor.RegSource.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.EmitLocalWord(0xba, this.owner.UnitOps.ConstructUnit(owner, town.PresentMakingIdx, town.Place.X, town.Place.Y));
			this.owner.NewGameWizard.EliminateCivilization((short)this.processor.PullGlobalWord(0x6548), 0);
			this.owner.Startup.DisplayMouseCursor();

			return ProductionStep.CityLost;
		}

		/// <summary>
		/// The start of HandleCity: the locals 0xb8, 0xda, 0xfe, 0xca and 0x2496 cleared, the mouse hidden, the 5 x 5 array
		/// of the tiles around (BP-0xa6) cleared; 0x653e the city; the materials outside 0 - 999 cleared. Over the other
		/// cities: 0xb2 the distance to the nearest one (at most 7), 0x6b30 the distance to the nearest palace of the owner
		/// (0 with an own palace, else at most 0x20), the tiles within two of the city worked by a city at most 5 away marked
		/// in the array. Then 0x6548 the owner, 0x6540 0x15, 0xc2 / 0xd0 the map view, 0x48 the waste (1 for despotism and
		/// anarchy, else 2), 0xb882 the advances that make citizens content (without the wonder 0x1000), 0x4a 10 the content
		/// citizens (for a computer player 16 - 2 x the level, 2 less on the levels above 1 for the leader of a later game),
		/// 0xe8b8 the specialists (bits 26 - 31 of the workers), for a computer player in the turn (marker 0) a city without
		/// the flag 1 has none, else all workers are taken off. The locals of HandleCity and SI, BX, CX are used as in the
		/// original.
		/// </summary>
		private void PrepareCity(short townIdx, short marker)
		{
			City town = this.owner.World.Towns[townIdx];

			this.processor.EmitGlobalWord(0x2496, 0);
			this.processor.EmitLocalWord(0xb8, 0);
			this.processor.RegAcc.Value16 = 0;
			this.processor.EmitLocalWord(0xda, 0);
			this.processor.EmitLocalWord(0xfe, 0);
			this.processor.EmitLocalWord(0xca, 0);

			this.owner.Startup.ConcealMouseCursor();

			// The 5 x 5 array of the tiles around
			for (ushort column = 0; column < 5; column++)
			{
				this.processor.EmitLocalWord(0xd8, column);

				for (ushort row = 0; row < 5; row++)
				{
					this.processor.EmitLocalWord(0xe4, row);
					this.processor.RegSource.Value16 = (ushort)(column * 0xa + row * 2);
					this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0xa6), 0);
				}

				this.processor.EmitLocalWord(0xe4, 5);
			}

			this.processor.EmitLocalWord(0xd8, (ushort)(short)town.Place.X);
			this.processor.EmitLocalWord(0xe4, (ushort)(short)town.Place.Y);
			this.processor.EmitGlobalWord(0x653e, (ushort)townIdx);
			this.processor.EmitLocalWord(0xb2, 7);
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.EmitGlobalWord(0x6b30, (ushort)((town.StructureMarkers0 & 1) != 0 ? 0 : 0x20));

			if (town.MaterialsTally > 999 || town.MaterialsTally < 0)
				town.MaterialsTally = 0;

			for (ushort otherIdx = 0; otherIdx < 0x80; otherIdx++)
			{
				this.processor.EmitLocalWord(0x4c, otherIdx);
				this.processor.RegBase.Value16 = (ushort)(otherIdx * 0x1c);
				City other = this.owner.World.Towns[otherIdx];

				if (other.ConditionMarker == 0xff || otherIdx == (ushort)townIdx)
					continue;

				this.processor.RegSource.Value16 = (ushort)(otherIdx * 0x1c);

				this.owner.Helpers.ObtainDistance(this.processor.PullLocalSignedWord(0xd8), other.Place.X, this.processor.PullLocalSignedWord(0xe4), other.Place.Y);

				short distance = (short)this.processor.RegAcc.Value16;
				this.processor.EmitLocalWord(0xb0, (ushort)distance);

				if (distance < (short)this.processor.PullLocalWord(0xb2) && distance != 0)
					this.processor.EmitLocalWord(0xb2, (ushort)distance);

				if (other.ParticipantIdx == town.ParticipantIdx && (other.StructureMarkers0 & 1) != 0 && distance < (short)this.processor.PullGlobalWord(0x6b30))
				{
					// The nearest palace of the owner
					this.processor.RegBase.Value16 = (ushort)(otherIdx * 0x1c);
					this.processor.EmitGlobalWord(0x6b30, (ushort)distance);
				}

				if (distance > 5)
					continue;

				// The tiles near the city worked by the other city (and the other city itself, tile 20)
				for (ushort tile = 0; tile <= 20; tile++)
				{
					this.processor.EmitLocalWord(0xea, tile);
					uint mask = 1u << tile;
					this.processor.RegCount.Value16 = (ushort)(mask & other.LaborerMarkers0);
					this.processor.RegBase.Value16 = (ushort)(((mask >> 16) & other.LaborerMarkers1) | this.processor.RegCount.Value16);
					this.processor.RegSource.Value16 = (ushort)(otherIdx * 0x1c);

					if (this.processor.RegBase.Value16 == 0 && tile != 20)
						continue;

					short horizLoc = (short)(other.Place.X + this.owner.TownDeltas[tile].X);
					short vertLoc = (short)(other.Place.Y + this.owner.TownDeltas[tile].Y);
					this.processor.EmitLocalWord(0xc6, (ushort)horizLoc);
					this.processor.EmitLocalWord(0xd2, (ushort)vertLoc);

					this.processor.RegAcc.Value16 = (ushort)(horizLoc - (short)this.processor.PullLocalWord(0xd8));
					this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

					if ((short)this.processor.RegAcc.Value16 > 2)
						continue;

					this.processor.RegAcc.Value16 = (ushort)(vertLoc - (short)this.processor.PullLocalWord(0xe4));
					this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);

					if ((short)this.processor.RegAcc.Value16 > 2)
						continue;

					this.processor.RegCount.Value16 = 0xa;
					this.processor.RegSource.Value16 = (ushort)((horizLoc - (short)this.processor.PullLocalWord(0xd8)) * 0xa +
						((vertLoc - (short)this.processor.PullLocalWord(0xe4)) << 1));
					this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x8e), 1);
				}

				this.processor.EmitLocalWord(0xea, 21);
			}

			this.processor.EmitLocalWord(0x4c, 0x80);

			// The tile of the city itself
			this.processor.EmitLocalWord(0x8e, 0);
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.EmitGlobalWord(0x6548, (ushort)town.ParticipantIdx);
			this.processor.EmitGlobalWord(0x6540, 0x15);
			this.processor.EmitLocalWord(0xc2, this.processor.PullGlobalWord(0xd4cc));
			this.processor.EmitLocalWord(0xd0, this.processor.PullGlobalWord(0xd75e));

			short owner = (short)this.processor.PullGlobalWord(0x6548);
			this.processor.EmitLocalWord(0x48, (ushort)(this.owner.World.Participants[owner].RegimeKind <= 1 ? 1 : 2));

			// The advances that make citizens content, unless the wonder 0x1000 is in the city
			this.processor.EmitGlobalWord(0xb882, 0);

			if ((town.StructureMarkers0 & 0x1000) == 0)
			{
				foreach (short advance in new short[] { 0x23, 0x3a, 0x29, 0x3c })
				{
					if (this.owner.Advisors.PlayerKnowsTechnology(owner, advance) != 0)
						this.processor.EmitGlobalWord(0xb882, (ushort)(this.processor.PullGlobalWord(0xb882) + 1));
				}
			}

			this.processor.EmitLocalWord(0x4a, 0xa);
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.EmitGlobalWord(0xe8b8, (ushort)((((uint)town.LaborerMarkers1 << 16) | town.LaborerMarkers0) >> 0x1a));
			this.processor.RegData.Value16 = 0;
			this.processor.RegCount.LowByte = 0x1a;

			if (owner == this.owner.World.PersonParticipantIdx)
				return;

			// A computer player: the content citizens by the level
			this.processor.EmitLocalWord(0x4a, (ushort)-(this.owner.World.HardnessTier * 2 - 0x10));

			if (this.owner.World.Season >= 0 &&
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Standing == 7 &&
				this.owner.World.HardnessTier > 1)
			{
				this.processor.EmitLocalWord(0x4a, (ushort)(this.processor.PullLocalWord(0x4a) - 2));
			}

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.RegData.Value16 = (ushort)((marker < 0 ? 0xffff : 0) | (ushort)marker);

			if (marker != 0)
				return;

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if ((town.ConditionMarker & 1) == 0)
			{
				this.processor.EmitGlobalWord(0xe8b8, 0);
			}
			else
			{
				this.processor.RegAcc.Value16 = 0;
				town.LaborerMarkers0 = 0;
				town.LaborerMarkers1 = 0;
			}
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="cityID"></param>
		/// <param name="flag"></param>
		public void HandleCity(short townIdx, short marker)
		{
			this.processor.Journal.OpenScope($"HandleCity({townIdx}, {marker})");

			// function body
			this.processor.EnterFrame(0x10c);
			this.processor.PreserveIndexRegisters();

			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);

			if (this.owner.World.Towns[townIdx].ConditionMarker == 0xff)
				goto L6927;

			// The neighbouring cities, the palace, the governments, the content citizens
			PrepareCity(townIdx, marker);

		L045f:
			this.processor.RegBase.Value16 = (ushort)(townIdx * 0x1c);
			this.processor.EmitLocalWord(0xd8, (ushort)(short)this.owner.World.Towns[townIdx].Place.X);
			this.processor.EmitLocalWord(0xe4, (ushort)(short)this.owner.World.Towns[townIdx].Place.Y);
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.RegData.Value16 = (ushort)(marker < 0 ? 0xffff : 0);

			if (marker != 1)
				goto L0701;

			// The city screen: the title and the map of the city
			RenderCityHeaderAndMap(townIdx);

		L0701:
			// In the turn of the city: starvation and growth
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.RegData.Value16 = (ushort)((marker < 0 ? 0xffff : 0) | (ushort)marker);

			if (marker == 0 && !FeedCity(townIdx))
				goto L6927;

			// The units the city supports
			CountCityUnits(townIdx);

			// The tiles the city cannot work
			MarkUnavailableTiles(townIdx);

			// The workers the city keeps and the new ones, the specialists
			PickWorkers(townIdx, marker, KeepWorkers(townIdx, marker));

		L12c2:
			// The citizen icons, the factories and the power
			PrepareCitizensAndFactories(townIdx, marker);

			if (marker != 0)
				goto L2673;

			// The food and the shields of the turn, a unit built
			switch (ProduceInCity(townIdx))
			{
				case ProductionStep.NotEnough:
					goto L1b60;

				case ProductionStep.Improvement:
					goto L1b63;

				case ProductionStep.SettlersWait:
					goto L2307;

				case ProductionStep.CityLost:
					goto L6927;
			}

		L174b:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1757;
			goto L1ab6;

		L1757:
			this.processor.CompareWord(this.processor.PullLocalWord(0xba), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1761;
			goto L1ab6;

		L1761:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x1b);
			if (this.processor.FlagSet.IsEqual) goto L1773;
			goto L1816;

		L1773:
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.EmitLocalWord(0x108, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xc8, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x4c, 0x0);
			goto L1789;

		L1786:
			this.processor.EmitLocalWord(0x4c, this.processor.IncrementWord(this.processor.PullLocalWord(0x4c)));

		L1789:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x80);
			if (this.processor.FlagSet.IsLess) goto L1793;
			goto L1802;

		L1793:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x6548));
			if (this.processor.FlagSet.IsNotEqual) goto L17a9;
			goto L17ff;

		L17a9:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L17bb;
			goto L17ff;

		L17bb:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			if (this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ParticipantIdx == this.owner.World.PersonParticipantIdx)
				goto L17ff;

			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x4c));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].RootCommerce;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xc8));
			if (this.processor.FlagSet.IsGreater) goto L17e7;
			goto L17ff;

		L17e7:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].RootCommerce;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0xc8, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x4c, 0x108);

		L17ff:
			goto L1786;

		L1802:
			this.owner.Conquest.FoundTradeRoute(
				this.processor.PullGlobalSignedWord(0x6548),
				this.processor.PullLocalSignedWord(0xba),
				this.processor.PullLocalSignedWord(0x108));

		L1816:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x1a);
			if (this.processor.FlagSet.IsEqual) goto L1828;
			goto L1a77;

		L1828:
			this.processor.EmitLocalWord(0x108, 0xffff);
			this.processor.EmitLocalWord(0xc8, 0x7fff);
			this.processor.EmitLocalWord(0x106, 0x0);
			this.processor.EmitLocalWord(0x4c, 0x0);
			goto L1845;

		L1842:
			this.processor.EmitLocalWord(0x4c, this.processor.IncrementWord(this.processor.PullLocalWord(0x4c)));

		L1845:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x80);
			if (this.processor.FlagSet.IsLess) goto L184f;
			goto L19af;

		L184f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));

			if (this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ParticipantIdx != this.owner.World.PersonParticipantIdx)
				goto L1842;

			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x4c));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L1842;

			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x4c));

			this.owner.Helpers.LocateNearestUnit(
				this.processor.PullGlobalSignedWord(0x6548),
				this.processor.PullLocalSignedWord(0xba),
				this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.Y);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6c9a);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x3);
			this.owner.Libc.AbsoluteValue((short)this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x6c9a, this.processor.RegAcc.Value16);

			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x4c));
			this.processor.TestBitsByte(this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ConditionMarker, 0x20);
			if (this.processor.FlagSet.IsNotEqual) goto L18c8;
			goto L18ce;

		L18c8:
			this.processor.EmitGlobalWord(0x6c9a, 0x3);

		L18ce:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6c9a);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L18da;
			goto L18fe;

		L18da:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6c9a);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L18e6;
			goto L19ac;

		L18e6:
			this.processor.EmitLocalWord(0x106, this.processor.IncrementWord(this.processor.PullLocalWord(0x106)));
			
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(this.processor.PullLocalWord(0x106)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L18fe;
			goto L19ac;

		L18fe:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xe8));
			this.processor.AddScaledToSource(0x600, this.processor.PullGlobalWord(0x6548));

			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L192e;
			goto L19ac;

		L192e:
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0xe8));
			this.processor.AddScaledToSource(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndex(0x1c, this.processor.PullLocalWord(0x4c));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0x10c, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.Y);

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x10c));
			if (this.processor.FlagSet.IsEqual) goto L1981;
			goto L19ac;

		L1981:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6c9a);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc8), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L198d;
			goto L1996;

		L198d:
			this.processor.EmitLocalWord(0x106, 0x1);
			goto L199d;

		L1996:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x6c9a);
			this.processor.EmitLocalWord(0xc8, this.processor.RegAcc.Value16);

		L199d:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.EmitLocalWord(0x108, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x4c, 0xb0);

		L19ac:
			goto L1842;

		L19af:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc8), 0x3);
			if (this.processor.FlagSet.IsLessOrEqual) goto L19b9;
			goto L1a5c;

		L19b9:
			this.processor.RegSource.Value16 = this.processor.PullGlobalWord(0x6548);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Relations[this.owner.World.PersonParticipantIdx] & 2) != 0)
				goto L1a5c;

			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x108));
			this.processor.AddScaledToSource(0x600, this.processor.PullGlobalWord(0x6548));

			this.owner.UnitOps.ConstructUnit(
				this.processor.PullGlobalSignedWord(0x6548),
				0x1a,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)));
			
			this.processor.EmitLocalWord(0xba, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xba), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1a0f;
			goto L1a59;

		L1a0f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xb0));

			this.processor.RegCount.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0xb0)].Place.X);
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xba));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), this.processor.RegCount.LowByte);

			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0xb0));

			this.processor.RegCount.LowByte = (byte)((sbyte)this.owner.World.Towns[this.processor.PullLocalWord(0xb0)].Place.Y);
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xba));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81db), this.processor.RegCount.LowByte);

		L1a59:
			goto L1a77;

		L1a5c:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].PresentMakingIdx);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1142));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x4a));
			this.owner.World.Towns[townIdx].MaterialsTally += (short)this.processor.RegAcc.Value16;

		L1a77:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x19);
			if (this.processor.FlagSet.IsEqual) goto L1a89;
			goto L1aa8;

		L1a89:
			this.processor.RegAcc.Value16 = 0x38;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].LiveTroops[25] == 1)
			{
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].TouchParticipantTimeleft = -1;
			}

		L1aa8:
			this.owner.Bot.PickNewCityProduction(townIdx);
			
			goto L1b60;

		L1ab6:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].MaterialsTally = 0;
			this.processor.CompareWord(this.processor.PullLocalWord(0xba), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1ace;
			goto L1b60;

		L1ace:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1ae0;
			goto L1af2;

		L1ae0:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x1a);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1af2;
			goto L1b60;

		L1af2:
			this.processor.EmitGlobalWord(0xb1e8, 0x1);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, " builds ");

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].PresentMakingIdx);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x3);

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

		L1b60:
			goto L220f;

		L1b63:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb2));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x4a));
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Towns[townIdx].MaterialsTally);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1b89;
			goto L220f;

		L1b89:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x18);
			if (this.processor.FlagSet.IsGreater) goto L1ba6;
			goto L1beb;

		L1ba6:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if (this.owner.World.MarvelTownIdx[Math.Abs(this.processor.PullLocalSignedWord(0xe8)) - 24] != -1)
				goto L1be2;

			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			this.owner.World.MarvelTownIdx[Math.Abs(this.processor.PullLocalSignedWord(0xe8)) - 24] = townIdx;

			this.owner.UnitOps.AttachReplayContent(10,
				this.processor.PullGlobalByte(0x6548),
				(byte)(this.processor.PullLocalWord(0xe8) - 0x18));

			goto L1be8;

		L1be2:
			this.processor.EmitLocalWord(0xe8, 0xffff);

		L1be8:
			goto L1c1d;

		L1beb:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xe8);
			this.processor.RegCount.LowByte = this.processor.DecrementByte(this.processor.RegCount.LowByte);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegCount.Value16 = this.processor.BitAndWord(this.processor.RegCount.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers0);
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers1);
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L1c17;
			goto L1c1d;

		L1c17:
			this.processor.EmitLocalWord(0xe8, 0xffff);

		L1c1d:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L1c27;
			goto L220f;

		L1c27:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x29);
			if (this.processor.FlagSet.IsEqual) goto L1c31;
			goto L1c80;

		L1c31:
			this.owner.Libc.CopyText(0xba06, "Sensors report a\nNUCLEAR WEAPONS test\nnear ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "!\n");

			this.owner.Services.PerformSound(0x24, 0);

			this.owner.Paper.DisplayNewspaper(-1);
			
			this.owner.Services.PerformSound(1, 0);

		L1c80:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x16);
			if (this.processor.FlagSet.IsLess) goto L1c8a;
			goto L1caf;

		L1c8a:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xe8);
			this.processor.RegCount.LowByte = this.processor.DecrementByte(this.processor.RegCount.LowByte);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.owner.World.Towns[townIdx].StructureMarkers0 |= this.processor.RegCount.Value16;
			this.owner.World.Towns[townIdx].StructureMarkers1 |= this.processor.RegBase.Value16;

		L1caf:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb2));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x4a));
			this.owner.World.Towns[townIdx].MaterialsTally -= (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsEqual) goto L1ce2;
			goto L1cec;

		L1ce2:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x18);
			if (this.processor.FlagSet.IsGreater) goto L1cec;
			goto L1dfc;

		L1cec:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L1cfe;
			goto L1d07;

		L1cfe:
			this.processor.EmitGlobalWord(0xb1e8, 0x1);
			goto L1d29;

		L1d07:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			this.owner.World.ChartSight[this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[townIdx].Place.Y] = -1;

		L1d29:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, " builds\n");

			this.processor.ScaleIndex(0x1e, this.processor.PullLocalWord(0xe8));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xb9a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1d7c;
			goto L1de0;

		L1d7c:
			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L1d86;
			goto L1de0;

		L1d86:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x15);
			if (this.processor.FlagSet.IsGreater) goto L1d90;
			goto L1d9a;

		L1d90:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x18);
			if (this.processor.FlagSet.IsGreater) goto L1d9a;
			goto L1de0;

		L1d9a:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x10);
			if (this.processor.FlagSet.IsEqual) goto L1dac;
			goto L1de0;

		L1dac:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L1db6;
			goto L1de0;

		L1db6:
			this.owner.Startup.QueryForDisk(1, 0);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1dce;
			goto L1de0;

		L1dce:
			this.owner.CityScene.DisplayCityScreen(townIdx,
				this.processor.PullLocalSignedWord(0xe8));

			goto L1deb;

		L1de0:
			this.owner.Paper.DisplayNewspaper(townIdx);

		L1deb:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].MaterialsTally = 0;
			goto L1e60;

		L1dfc:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L1e06;
			goto L1e13;

		L1e06:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].PresentMakingIdx = -2;

		L1e13:
			this.owner.Advisors.PickCityProduction(this.processor.PullGlobalSignedWord(0x6548), townIdx);
			
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].PresentMakingIdx = (sbyte)this.processor.RegCount.LowByte;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1e42;
			goto L1e60;

		L1e42:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));

			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]++;

		L1e60:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1e6a;
			goto L2010;

		L1e6a:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x18);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1e74;
			goto L2010;

		L1e74:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsEqual) goto L1e86;
			goto L2010;

		L1e86:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L1e92;
			goto L1ea9;

		L1e92:
			this.owner.RocketScreen.AttachSpaceshipPart(
				this.processor.PullGlobalSignedWord(0x6548),
				(ushort)(this.processor.PullLocalWord(0xe8) - 22));
			
			goto L2010;

		L1ea9:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x16);

			this.owner.RocketScreen.ComputeSpaceshipSuccess(this.processor.PullGlobalSignedWord(0x6548), (short)this.processor.RegAcc.Value16);

			this.processor.EmitLocalWord(0xc8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xc8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L1ecb;
			goto L1f2f;

		L1ecb:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x18);
			if (this.processor.FlagSet.IsLess) goto L1ed5;
			goto L1f2f;

		L1ed5:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xe8));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb2));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x4a));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].MaterialsTally += (short)this.processor.RegCount.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0xea);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1f05;
			goto L1f2b;

		L1f05:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0xe8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L1f17;
			goto L1f2b;

		L1f17:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = this.processor.DecrementByte(this.processor.RegAcc.LowByte);
			this.owner.World.Towns[townIdx].PresentMakingIdx = (sbyte)this.processor.RegAcc.LowByte;

		L1f2b:
			this.processor.EmitLocalWord(0xc8, this.processor.DecrementWord(this.processor.PullLocalWord(0xc8)));

		L1f2f:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsEqual) goto L1f41;
			goto L1ffa;

		L1f41:
			if (this.owner.World.BotStarshipVictoryRatio >= 40) goto L1f4b;
			goto L1ffa;

		L1f4b:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L1f55;
			goto L1feb;

		L1f55:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.owner.World.PersonParticipantIdx, 0x28);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L1f6d;

			goto L1ff7;

		L1f6d:
			if (this.owner.World.BotStarshipVictoryRatio <= 75) goto L1f77;
			goto L1feb;

		L1f77:
			this.processor.RegSource.Value16 = this.processor.PullGlobalWord(0x6548);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Standing >
				this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Standing) goto L1feb;

			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsEqual) goto L1fc5;

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].StarshipArrivalSeason;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.Season);
			this.processor.RegCount.Value16 = (ushort)this.owner.World.Participants[this.owner.World.PersonParticipantIdx].StarshipArrivalSeason;
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, (ushort)this.owner.World.Season);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsLessOrEqual) goto L1feb;

		L1fc5:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, 0x8);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsEqual) goto L1ff7;
			if (this.owner.World.Participants[this.owner.World.LiveRealms].Gold <= 1000) goto L1ff7;

		L1feb:
			this.owner.RocketScreen.FireSpaceship(this.processor.PullGlobalSignedWord(0x6548));

		L1ff7:
			goto L2010;

		L1ffa:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2004;
			goto L2010;

		L2004:
			this.owner.RocketScreen.FireSpaceship(this.processor.PullGlobalSignedWord(0x6548));

		L2010:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L201a;
			goto L212c;

		L201a:
			this.processor.EmitLocalWord(0x4c, 0x0);
			goto L2025;

		L2022:
			this.processor.EmitLocalWord(0x4c, this.processor.IncrementWord(this.processor.PullLocalWord(0x4c)));

		L2025:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x80);
			if (this.processor.FlagSet.IsLess) goto L202f;
			goto L205a;

		L202f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x6548));
			if (this.processor.FlagSet.IsEqual) goto L2045;
			goto L2057;

		L2045:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].StructureMarkers0 &= 0xfffe;
			this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].StructureMarkers1 &= 0xffff;

		L2057:
			goto L2022;

		L205a:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L2066;
			goto L207e;

		L2066:
			this.processor.RegSource.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, this.processor.RegCount.LowByte);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x6548);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if ((this.owner.World.Participants[this.owner.World.PersonParticipantIdx].Relations[this.processor.PullGlobalWord(0x6548)] & 0x40) == 0)
				goto L212c;

		L207e:
			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L208f;
			goto L20a5;

		L208f:
			this.owner.Libc.CopyText(0xba06, "Diplomats report:\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x1);

		L20a5:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x6548);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1982)));

			this.owner.Libc.AppendText(0xba06, " capital\nmoved to ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x5);

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 80);

			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].HorizBegin = 
				(short)this.owner.World.Towns[townIdx].Place.X;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].StructureMarkers0 |= 1;
			this.owner.World.Towns[townIdx].StructureMarkers1 |= 0;

		L212c:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x26);
			if (this.processor.FlagSet.IsEqual) goto L2136;
			goto L2160;

		L2136:
			this.owner.Startup.DisplayMouseCursorFully();
			
			this.owner.Advisors.UncoverTechnology(this.processor.PullGlobalSignedWord(0x6548), 0);
			
			this.owner.Advisors.UncoverTechnology(this.processor.PullGlobalSignedWord(0x6548), 0);
			
			this.owner.Startup.ConcealMouseCursorToSavedLevel();

		L2160:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x2b);
			if (this.processor.FlagSet.IsEqual) goto L216a;
			goto L220f;

		L216a:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2176;
			goto L220f;

		L2176:
			this.processor.EmitLocalWord(0x4c, 0x0);
			goto L2181;

		L217e:
			this.processor.EmitLocalWord(0x4c, this.processor.IncrementWord(this.processor.PullLocalWord(0x4c)));

		L2181:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0x80);
			if (this.processor.FlagSet.IsLess) goto L218b;
			goto L220f;

		L218b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			this.processor.CompareByte(this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ConditionMarker, 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L219d;
			goto L220c;

		L219d:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0x4c));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x6548));
			if (this.processor.FlagSet.IsNotEqual) goto L21b3;
			goto L220c;

		L21b3:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x4c));
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].TrueExtent;
			this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ShownExtent = (sbyte)this.processor.RegAcc.LowByte;

			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x4c));
			this.processor.RegAcc.LowByte = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.LowByte = this.processor.ShiftLeftByte(this.processor.RegAcc.LowByte, this.processor.RegCount.LowByte);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.owner.World.ChartSight[this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.Y] |= (sbyte)this.processor.RegCount.LowByte;
			this.processor.ScaleIndexToSource(0x1c, this.processor.PullLocalWord(0x4c));

			this.owner.MapOps.KeepKnownTileImprovements(
				this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.X,
				this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.Y);

		L220c:
			goto L217e;

		L220f:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L221b;
			goto L2307;

		L221b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L222d;
			goto L2307;

		L222d:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L223f;
			goto L225b;

		L223f:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsLess) goto L2251;
			goto L2307;

		L2251:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L225b;
			goto L2307;

		L225b:
			this.owner.Advisors.PickCityProduction(this.processor.PullGlobalSignedWord(0x6548), townIdx);
			
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x63);
			if (this.processor.FlagSet.IsNotEqual) goto L2281;
			goto L22fa;

		L2281:
			this.processor.EmitGlobalWord(0xb1e8, 0x0);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L2299;
			goto L22b7;

		L2299:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));

			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]--;

		L22b7:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0xe8);
			this.owner.World.Towns[townIdx].PresentMakingIdx = (sbyte)this.processor.RegAcc.LowByte;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L22d9;
			goto L22f7;

		L22d9:			
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));
			
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]++;

		L22f7:
			goto L2307;

		L22fa:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker &= 0xef;

		L2307:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsEqual) goto L2319;
			goto L2673;

		L2319:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L232b;
			goto L2336;

		L232b:
			this.owner.Bot.PickNewCityProduction(townIdx);

		L2336:
			this.processor.EmitLocalWord(0xcc, 0x0);

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0xd8),
				this.processor.PullLocalSignedWord(0xe4));

			// ??? Needs further investigation why this happens
			if (this.processor.RegAcc.Value16 >= this.owner.World.Participants.Length)
			{
				this.processor.RegAcc.Value16 = 0;
			}
			else
			{
				this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.RegAcc.Value16].Landmasses[this.processor.PullGlobalWord(0x6548)].Approach;
			}
			this.processor.EmitLocalWord(0x104, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x104), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L236a;
			goto L237e;

		L236a:
			this.processor.CompareWord(this.processor.PullLocalWord(0x104), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L2374;
			goto L237e;

		L2374:
			this.processor.CompareWord(this.processor.PullLocalWord(0x104), 0x5);
			if (this.processor.FlagSet.IsEqual) goto L237e;
			goto L23d3;

		L237e:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2388;
			goto L23d3;

		L2388:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L239a;
			goto L23d3;

		L239a:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].PresentMakingIdx);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x104);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1148)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L23b7;
			goto L23d3;

		L23b7:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold;
			this.processor.DivideAccumulatorByPowerOfTwo(0x6);
			this.processor.EmitLocalWord(0xcc, this.processor.RegAcc.Value16);

		L23d3:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegCount.LowByte = this.processor.SumByte(this.processor.RegCount.LowByte, 0x8);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L23e8;
			goto L2432;

		L23e8:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L23ff;
			goto L2432;

		L23ff:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x18);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2416;
			goto L2432;

		L2416:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold;
			this.processor.DivideAccumulatorByPowerOfTwo(0x7);
			this.processor.EmitLocalWord(0xcc, this.processor.RegAcc.Value16);

		L2432:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2444;
			goto L24b3;

		L2444:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsLess) goto L2456;
			goto L24b3;

		L2456:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2468;
			goto L24b3;

		L2468:
			this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);
			this.owner.Helpers.VerifyValueSpan(
				(this.processor.PullInt16(this.processor.SegData.Value16,
						(ushort)(((-this.owner.World.Towns[townIdx].PresentMakingIdx) * 0x1e) + 0xbb2)) *
						this.processor.PullLocalSignedWord(0x4a)) -
					this.owner.World.Towns[townIdx].MaterialsTally,
				0, this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold / 8);

			this.processor.EmitLocalWord(0xcc, this.processor.RegAcc.Value16);

		L24b3:
			this.owner.MapOps.ObtainTileOwner(
				this.processor.PullLocalSignedWord(0xd8),
				this.processor.PullLocalSignedWord(0xe4));

			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L24c9;
			goto L24db;

		L24c9:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L24db;
			goto L253c;

		L24db:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L24ed;
			goto L253c;

		L24ed:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L24ff;
			goto L253c;

		L24ff:
			this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

			this.owner.Helpers.VerifyValueSpan(
				(this.processor.PullInt16(this.processor.SegData.Value16,
					(ushort)((0x22 * this.owner.World.Towns[townIdx].PresentMakingIdx) + 0x1142)) *
					this.processor.PullLocalSignedWord(0x4a)) -
					this.owner.World.Towns[townIdx].MaterialsTally,
				0, this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold / 3);

			this.processor.EmitLocalWord(0xcc, this.processor.RegAcc.Value16);

		L253c:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0xff);
			if (this.processor.FlagSet.IsEqual) goto L254e;
			goto L259d;

		L254e:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2560;
			goto L259d;

		L2560:
			this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

			this.owner.Helpers.VerifyValueSpan(
				(this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((0x22 *
					this.owner.World.Towns[townIdx].PresentMakingIdx) + 0x1142)) *
					this.processor.PullLocalSignedWord(0x4a)) -
					this.owner.World.Towns[townIdx].MaterialsTally,
				0, this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold / 3);

			this.processor.EmitLocalWord(0xcc, this.processor.RegAcc.Value16);

		L259d:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x19);
			if (this.processor.FlagSet.IsEqual) goto L25af;
			goto L2619;

		L25af:
			this.processor.RegAcc.Value16 = 0x38;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].LiveTroops[25] != 0)
				goto L2619;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L25d4;
			goto L2619;

		L25d4:
			this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);

			this.owner.Helpers.VerifyValueSpan(
				(this.processor.PullInt16(this.processor.SegData.Value16,(ushort)((0x22 *
					this.owner.World.Towns[townIdx].PresentMakingIdx) + 0x1142)) *
					this.processor.PullLocalSignedWord(0x4a)) -
					this.owner.World.Towns[townIdx].MaterialsTally,
				0, this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold / 4);

			this.processor.EmitLocalWord(0xcc, this.processor.RegAcc.Value16);

		L2619:
			this.processor.CompareWord((ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold, 0x7d0);
			if (this.processor.FlagSet.IsGreater) goto L262a;
			goto L2646;

		L262a:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold;
			this.processor.DivideAccumulatorByPowerOfTwo(0x9);
			this.processor.EmitLocalWord(0xcc, this.processor.SumWord(this.processor.PullLocalWord(0xcc), this.processor.RegAcc.Value16));

		L2646:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xcc);
			this.owner.World.Towns[townIdx].MaterialsTally += (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold -= (short)this.processor.RegAcc.Value16;
			this.owner.World.Towns[townIdx].ConditionMarker &= 0xef;

		L2673:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L267f;
			goto L277e;

		L267f:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2687;
			goto L277e;

		L2687:
			RenderFrame(2, 67, 124, 104);

			RenderFrame(95, 106, 227, 197);

			RenderLabelBox(95, 106, 128, 114, 0x25ea, 9);

			RenderLabelBox(129, 106, 160, 114, 0x25ef, 9);

			RenderLabelBox(161, 106, 193, 114, 0x25f5, 9);

			RenderLabelBox(194, 106, 226, 114, 0x25f9, 9);

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 
				(33 * this.processor.PullGlobalSignedWord(0x2496)) + 96, 107, 32, 7, 9, 15);

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x2496);
			goto L2773;

		L2768:
			RenderMiniMap();
			goto L277e;

		L2773:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L277e;
			goto L2768;

		L277e:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.X);
			this.processor.EmitLocalWord(0xd8, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.Y);
			this.processor.EmitLocalWord(0xe4, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xd2e0, 0x0);

			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind == 3)
			{
				this.processor.EmitGlobalWord(0x6b30, 0xa);
			}

			this.processor.RegAcc.Value16 = 0x14;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData,
				(ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, 0x50);
			this.processor.RegBase.Value16 = 0x3;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70de);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0x6b30));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegBase.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitGlobalWord(0xd2e0, this.processor.RegAcc.Value16);

			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x40);
			if (this.processor.FlagSet.IsEqual) goto L27f4;
			goto L2806;

		L27f4:
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L2806;
			goto L2812;

		L2806:
			this.processor.RegCount.Value16 = 0x2;
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2e0);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitGlobalWord(0xd2e0, this.processor.RegAcc.Value16);

		L2812:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind == 5)
			{
				this.processor.EmitGlobalWord(0xd2e0, 0x0);
			}

			this.processor.RegAcc.LowByte = this.processor.PullGlobalByte(0x70de);
			this.processor.RegAcc.LowByte = this.processor.SubtractByte(this.processor.RegAcc.LowByte, this.processor.PullGlobalByte(0xd2e0));
			this.owner.World.Towns[townIdx].RootCommerce = (sbyte)this.processor.RegAcc.LowByte;
			this.processor.EmitLocalWord(0xbc, 0x0);
			goto L2848;

		L2844:
			this.processor.EmitLocalWord(0xbc, this.processor.IncrementWord(this.processor.PullLocalWord(0xbc)));

		L2848:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x3);
			if (this.processor.FlagSet.IsLess) goto L2852;
			goto L28d9;

		L2852:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].CommerceTownIndices[this.processor.PullLocalWord(0xbc)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x4c, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L2871;
			goto L28d6;

		L2871:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x6548));
			if (this.processor.FlagSet.IsNotEqual) goto L2887;
			goto L28b0;

		L2887:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].RootCommerce;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70de));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4);
			this.processor.DivideAccumulatorByPowerOfTwo(0x3);
			this.processor.EmitGlobalWord(0x70de, this.processor.SumWord(this.processor.PullGlobalWord(0x70de), this.processor.RegAcc.Value16));
			goto L28d6;

		L28b0:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].RootCommerce;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70de));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x4);
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);
			this.processor.EmitGlobalWord(0x70de, this.processor.SumWord(this.processor.PullGlobalWord(0x70de), this.processor.RegAcc.Value16));

		L28d6:
			goto L2844;

		L28d9:
			this.processor.RegAcc.Value16 = 0x14;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData,
				(ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, 0x50);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70de);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0x6b30));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, 3);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitGlobalWord(0xd2e0, this.processor.RegAcc.Value16);

			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind == 5)
			{
				this.processor.EmitGlobalWord(0xd2e0, 0x0);
			}

			if ((this.owner.World.Towns[townIdx].StructureMarkers0 & 0x40) != 0)
			{
				this.processor.RegCount.Value16 = 0x2;
				this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2e0);
				this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
				this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
				this.processor.EmitGlobalWord(0xd2e0, this.processor.RegAcc.Value16);
			}

			this.owner.Helpers.VerifyValueSpan(
				((-(this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].ResearchLevyRatio +
					this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].LevyRatio - 10) *
					(this.processor.PullGlobalSignedWord(0x70de) - this.processor.PullGlobalSignedWord(0xd2e0))) + 5) / 10,
				0, this.processor.PullGlobalSignedWord(0x70de));

			this.processor.EmitGlobalWord(0x70e0, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan(
				((this.owner.World.Participants[this.processor.PullGlobalSignedWord(0x6548)].LevyRatio *
					(this.processor.PullGlobalSignedWord(0x70de) - this.processor.PullGlobalSignedWord(0xd2e0))) + 5) / 10,
				0,
				this.processor.PullGlobalSignedWord(0x70de) - 
					this.processor.PullGlobalSignedWord(0x70e0) -
					this.processor.PullGlobalSignedWord(0xd2e0));

			this.processor.EmitGlobalWord(0xe17a, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70de);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e0));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xe17a));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xd2e0));
			this.processor.EmitGlobalWord(0x70e6, this.processor.RegAcc.Value16);

			TallySpecialists(1);

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitGlobalWord(0xe17a, this.processor.SumWord(this.processor.PullGlobalWord(0xe17a), this.processor.RegAcc.Value16));

			TallySpecialists(2);

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitGlobalWord(0x70e6, this.processor.SumWord(this.processor.PullGlobalWord(0x70e6), this.processor.RegAcc.Value16));

			TallySpecialists(3);

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitGlobalWord(0x70e0, this.processor.SumWord(this.processor.PullGlobalWord(0x70e0), this.processor.RegAcc.Value16));
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L29fe;
			goto L2a16;

		L29fe:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e0);
			this.processor.HalveAccumulator();
			this.processor.EmitGlobalWord(0x70e0, this.processor.SumWord(this.processor.PullGlobalWord(0x70e0), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xe17a);
			this.processor.HalveAccumulator();
			this.processor.EmitGlobalWord(0xe17a, this.processor.SumWord(this.processor.PullGlobalWord(0xe17a), this.processor.RegAcc.Value16));

		L2a16:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x200);
			if (this.processor.FlagSet.IsNotEqual) goto L2a29;
			goto L2a41;

		L2a29:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e0);
			this.processor.HalveAccumulator();
			this.processor.EmitGlobalWord(0x70e0, this.processor.SumWord(this.processor.PullGlobalWord(0x70e0), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xe17a);
			this.processor.HalveAccumulator();
			this.processor.EmitGlobalWord(0xe17a, this.processor.SumWord(this.processor.PullGlobalWord(0xe17a), this.processor.RegAcc.Value16));

		L2a41:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e6);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x20);
			if (this.processor.FlagSet.IsNotEqual) goto L2a5a;
			goto L2a8b;

		L2a5a:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e6);
			this.processor.HalveAccumulator();
			this.processor.EmitLocalWord(0xe8, this.processor.SumWord(this.processor.PullLocalWord(0xe8), this.processor.RegAcc.Value16));

			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 0xc);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2a7e;
			goto L2a8b;

		L2a7e:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e6);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.SumWord(this.processor.PullLocalWord(0xe8), this.processor.RegAcc.Value16));

		L2a8b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x800);
			if (this.processor.FlagSet.IsNotEqual) goto L2a9e;
			goto L2acf;

		L2a9e:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e6);
			this.processor.HalveAccumulator();
			this.processor.EmitLocalWord(0xe8, this.processor.SumWord(this.processor.PullLocalWord(0xe8), this.processor.RegAcc.Value16));

			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 0xc);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2ac2;
			goto L2acf;

		L2ac2:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e6);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x3;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.SumWord(this.processor.PullLocalWord(0xe8), this.processor.RegAcc.Value16));

		L2acf:
			ObtainWonderCityID(10);

			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)townIdx);
			if (this.processor.FlagSet.IsEqual) goto L2ae3;
			goto L2aeb;

		L2ae3:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.EmitLocalWord(0xe8, this.processor.SumWord(this.processor.PullLocalWord(0xe8), this.processor.RegAcc.Value16));

		L2aeb:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.EmitGlobalWord(0x70e6, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x70e2, 0x0);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2b04;
			goto L2b79;

		L2b04:
			this.processor.RegAcc.Value16 = (ushort)owner.World.HardnessTier;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xe);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.HalveAccumulator();
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind;
			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.RegData.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)townIdx;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.RegData.Value16;
			this.processor.RegSource.Value16 = this.processor.PullGlobalWord(0x6548);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 += (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TownTally;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xe8));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegBase.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.HardnessTier);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x6);
			this.processor.EmitGlobalWord(0x70e4, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xa8, this.processor.RegAcc.Value16);
			goto L2b90;

		L2b79:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x3);
			this.processor.EmitGlobalWord(0x70e4, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xa8, this.processor.RegAcc.Value16);

		L2b90:
			this.processor.EmitGlobalWord(0x6542, 0x0);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			if (this.processor.FlagSet.IsLess) goto L2bac;
			goto L2bd3;

		L2bac:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0x70e4);
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x6542, this.processor.RegCount.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitGlobalWord(0x70e4, this.processor.RegAcc.Value16);

		L2bd3:
			ReshuffleCitizens(townIdx, this.processor.PullLocalWord(0x8));

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2bed;
			goto L2c3d;

		L2bed:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2bf5;
			goto L2c3d;

		L2bf5:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2bff;
			goto L2c3d;

		L2bff:
			this.processor.EmitLocalWord(0x42, 0x74);

			RenderCitizenIcons(townIdx, 100, this.processor.PullLocalSignedWord(0x42),
				this.processor.PullLocalWord(0x8), 92);

			this.processor.EmitLocalWord(0x42, this.processor.SumWord(this.processor.PullLocalWord(0x42), 0x10));

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				100, this.processor.PullLocalSignedWord(0x42) - 2,
				222, this.processor.PullLocalSignedWord(0x42) - 2, 1);

		L2c3d:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e0);
			this.processor.HalveAccumulator();
			this.processor.EmitGlobalWord(0x70e2, this.processor.RegAcc.Value16);

			ReshuffleCitizens(townIdx, this.processor.PullLocalWord(0x8));

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2c62;
			goto L2cce;

		L2c62:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2c6a;
			goto L2cce;

		L2c6a:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2c74;
			goto L2cce;

		L2c74:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70e2), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2c7e;
			goto L2cce;

		L2c7e:
			RenderCitizenIcons(townIdx, 100, this.processor.PullLocalSignedWord(0x42),
				this.processor.PullLocalWord(0x8), 92);

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				208,
				this.processor.PullLocalSignedWord(0x42) + 4,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xe << 1) + 0xd4ce)));

			this.processor.EmitLocalWord(0x42, this.processor.SumWord(this.processor.PullLocalWord(0x42), 0x10));
			
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				100, this.processor.PullLocalSignedWord(0x42) - 2,
				222, this.processor.PullLocalSignedWord(0x42) - 2, 1);

		L2cce:
			this.processor.EmitLocalWord(0x6, 0xd0);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x2000);
			if (this.processor.FlagSet.IsNotEqual) goto L2ce6;
			goto L2d1f;

		L2ce6:
			this.processor.EmitGlobalWord(0x70e4, this.processor.SubtractWord(this.processor.PullGlobalWord(0x70e4), 0x3));
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2cf7;
			goto L2d1f;

		L2cf7:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2cff;
			goto L2d1f;

		L2cff:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2d09;
			goto L2d1f;

		L2d09:
			RenderResourceIcon(14, this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x42));

			this.processor.EmitLocalWord(0x6, this.processor.SubtractWord(this.processor.PullLocalWord(0x6), 0x10));

		L2d1f:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x1d);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L2d37;

			goto L2dbc;

		L2d37:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x400);
			if (this.processor.FlagSet.IsNotEqual) goto L2d4a;
			goto L2d6e;

		L2d4a:
			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 9);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2d62;
			goto L2d68;

		L2d62:
			this.processor.RegAcc.Value16 = 0x6;
			goto L2d6b;

		L2d68:
			this.processor.RegAcc.Value16 = 0x4;

		L2d6b:
			goto L2d71;

		L2d6e:
			this.processor.RegAcc.Value16 = 0x0;

		L2d71:
			this.processor.EmitGlobalWord(0x70e4, this.processor.SubtractWord(this.processor.PullGlobalWord(0x70e4), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2d81;
			goto L2dbc;

		L2d81:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2d89;
			goto L2dbc;

		L2d89:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2d93;
			goto L2dbc;

		L2d93:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x400);
			if (this.processor.FlagSet.IsNotEqual) goto L2da6;
			goto L2dbc;

		L2da6:
			RenderResourceIcon(11, this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x42));

			this.processor.EmitLocalWord(0x6, this.processor.SubtractWord(this.processor.PullLocalWord(0x6), 0x10));

		L2dbc:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L2dce;
			goto L2e7b;

		L2dce:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x1a);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L2de6;

			goto L2dee;

		L2de6:
			this.processor.EmitGlobalWord(0x70e4, this.processor.SubtractWord(this.processor.PullGlobalWord(0x70e4), 0x2));
			goto L2e0a;

		L2dee:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x19);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L2e06;

			goto L2e0a;

		L2e06:
			this.processor.EmitGlobalWord(0x70e4, this.processor.DecrementWord(this.processor.PullGlobalWord(0x70e4)));

		L2e0a:
			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 6);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2e22;
			goto L2e47;

		L2e22:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x1a);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L2e3a;

			goto L2e40;

		L2e3a:
			this.processor.RegAcc.Value16 = 0x2;
			goto L2e43;

		L2e40:
			this.processor.RegAcc.Value16 = 0x1;

		L2e43:
			this.processor.EmitGlobalWord(0x70e4, this.processor.SubtractWord(this.processor.PullGlobalWord(0x70e4), this.processor.RegAcc.Value16));

		L2e47:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2e53;
			goto L2e7b;

		L2e53:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2e5b;
			goto L2e7b;

		L2e5b:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2e65;
			goto L2e7b;

		L2e65:
			RenderResourceIcon(4, this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x42));

			this.processor.EmitLocalWord(0x6, this.processor.SubtractWord(this.processor.PullLocalWord(0x6), 0x10));

		L2e7b:
			ReshuffleCitizens(townIdx, this.processor.PullLocalWord(0x8));

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2e95;
			goto L2ef3;

		L2e95:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2e9d;
			goto L2ef3;

		L2e9d:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2ea7;
			goto L2ef3;

		L2ea7:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x2408);
			if (this.processor.FlagSet.IsNotEqual) goto L2eba;
			goto L2ef3;

		L2eba:
			RenderCitizenIcons(townIdx, 100, this.processor.PullLocalSignedWord(0x42),
				this.processor.PullLocalWord(0x8), 92);

			this.processor.EmitLocalWord(0x42, this.processor.SumWord(this.processor.PullLocalWord(0x42), 0x10));
			
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				100, this.processor.PullLocalSignedWord(0x42) - 2,
				222, this.processor.PullLocalSignedWord(0x42) - 2, 1);

		L2ef3:
			this.processor.EmitLocalWord(0x6, 0xd0);
			
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind < 4) goto L2f08;
			goto L3088;

		L2f08:
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0x2, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xe6, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainUnitAtTile(
				this.processor.PullLocalSignedWord(0xd8),
				this.processor.PullLocalSignedWord(0xe4));

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xba, this.processor.RegAcc.Value16);

		L2f2a:
			this.processor.CompareWord(this.processor.PullLocalWord(0xba), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L2f34;
			goto L2ffa;

		L2f34:
			this.processor.EmitLocalWord(0x2, this.processor.IncrementWord(this.processor.PullLocalWord(0x2)));
			this.processor.CompareWord(this.processor.PullLocalWord(0x2), 0x20);
			if (this.processor.FlagSet.IsLess) goto L2f40;
			goto L2ffa;

		L2f40:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xba));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113e)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L2f64;
			goto L2fc9;

		L2f64:
			this.processor.EmitLocalWord(0xe6, this.processor.IncrementWord(this.processor.PullLocalWord(0xe6)));
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2f74;
			goto L2fc9;

		L2f74:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L2f7c;
			goto L2fc9;

		L2f7c:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L2f86;
			goto L2fc9;

		L2f86:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe6), 0x3);
			if (this.processor.FlagSet.IsLessOrEqual) goto L2f90;
			goto L2fc9;

		L2f90:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x42) - 1,
				this.processor.PullUInt16(this.processor.SegData.Value16,
						(ushort)(((this.processor.PullUInt8(this.processor.SegData.Value16,
					(ushort)((0x600 * this.processor.PullGlobalWord(0x6548)) +
						(0xc * this.processor.PullLocalWord(0xba)) + 0x81d7)) +
					(this.processor.PullGlobalWord(0x6548) << 5) + 0x40) << 1) + 0xd4ce)));

			this.processor.EmitLocalWord(0x6, this.processor.SubtractWord(this.processor.PullLocalWord(0x6), 0x2));

		L2fc9:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xba));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81de));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitLocalWord(0xba, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.CompareWord(this.processor.PullLocalWord(0xba), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L2ff1;
			goto L2ff7;

		L2ff1:
			this.processor.EmitLocalWord(0xba, 0xffff);

		L2ff7:
			goto L2f2a;

		L2ffa:
			this.processor.EmitLocalWord(0xba, 0x0);
			goto L3007;

		L3003:
			this.processor.EmitLocalWord(0xba, this.processor.IncrementWord(this.processor.PullLocalWord(0xba)));

		L3007:
			this.processor.CompareWord(this.processor.PullLocalWord(0xba), 0x2);
			if (this.processor.FlagSet.IsLess) goto L3011;
			goto L305d;

		L3011:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70e4), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L301b;
			goto L305a;

		L301b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xba);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].Unfamiliar[this.processor.PullLocalWord(0xba)], 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L3031;
			goto L305a;

		L3031:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xba);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].Unfamiliar[this.processor.PullLocalWord(0xba)];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16, 0x3f);
			this.processor.RegCount.Value16 = 0x22;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113e)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3056;
			goto L305a;

		L3056:
			this.processor.EmitLocalWord(0xe6, this.processor.IncrementWord(this.processor.PullLocalWord(0xe6)));

		L305a:
			goto L3003;

		L305d:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe6), 0x3);
			if (this.processor.FlagSet.IsGreater) goto L3067;
			goto L306d;

		L3067:
			this.processor.EmitLocalWord(0xe6, 0x3);

		L306d:
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0xe6),
				0, this.processor.PullGlobalSignedWord(0x70e4));

			this.processor.EmitGlobalWord(0x70e4, this.processor.SubtractWord(this.processor.PullGlobalWord(0x70e4), this.processor.RegAcc.Value16));
			goto L3126;

		L3088:
			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.SubtractBorrowWord(this.processor.RegCount.Value16, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.NegateWord(this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.RegCount.Value16);

			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind == 5)
			{
				this.processor.IncrementLocalWord(0xe8);
			}

			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L30c1;
			goto L3126;

		L30c1:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0x6546));
			this.processor.EmitGlobalWord(0x70e4, this.processor.SumWord(this.processor.PullGlobalWord(0x70e4), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L30d9;
			goto L3126;

		L30d9:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L30e1;
			goto L3126;

		L30e1:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L30eb;
			goto L3126;

		L30eb:
			this.processor.EmitLocalWord(0xba, 0x0);
			goto L30f8;

		L30f4:
			this.processor.EmitLocalWord(0xba, this.processor.IncrementWord(this.processor.PullLocalWord(0xba)));

		L30f8:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0x6546));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xba));
			if (this.processor.FlagSet.IsGreater) goto L3109;
			goto L3126;

		L3109:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0x6),
				this.processor.PullLocalSignedWord(0x42) + 4,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xd << 1) + 0xd4ce)));

			this.processor.EmitLocalWord(0x6, this.processor.SubtractWord(this.processor.PullLocalWord(0x6), 0x2));
			goto L30f4;

		L3126:
			ReshuffleCitizens(townIdx, this.processor.PullLocalWord(0x8));

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L3140;
			goto L318b;

		L3140:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3148;
			goto L318b;

		L3148:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L3152;
			goto L318b;

		L3152:
			RenderCitizenIcons(townIdx, 100, this.processor.PullLocalSignedWord(0x42),
				this.processor.PullLocalWord(0x8), 92);

			this.processor.EmitLocalWord(0x42, this.processor.SumWord(this.processor.PullLocalWord(0x42), 0x10));
			
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				100, this.processor.PullLocalSignedWord(0x42) - 2,
				222, this.processor.PullLocalSignedWord(0x42) - 2, 1);

		L318b:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e2);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);

			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 2);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L31ae;
			goto L31b2;

		L31ae:
			this.processor.EmitGlobalWord(0x70e2, this.processor.IncrementWord(this.processor.PullGlobalWord(0x70e2)));

		L31b2:
			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 0x15);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L31ca;
			goto L31ce;

		L31ca:
			this.processor.EmitGlobalWord(0x70e2, this.processor.IncrementWord(this.processor.PullGlobalWord(0x70e2)));

		L31ce:
			ObtainWonderCityID(11);

			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)townIdx);
			if (this.processor.FlagSet.IsEqual) goto L31e2;
			goto L31e8;

		L31e2:
			this.processor.EmitGlobalWord(0x70e4, 0x0);

		L31e8:
			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 0xd);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3200;
			goto L323d;

		L3200:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)this.owner.World.MarvelTownIdx[13]);

			this.owner.MapOps.ObtainContinentID(
				this.owner.World.Towns[this.owner.World.MarvelTownIdx[13]].Place.X,
				this.owner.World.Towns[this.owner.World.MarvelTownIdx[13]].Place.Y);

			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.ObtainContinentID(
				this.processor.PullLocalSignedWord(0xd8),
				this.processor.PullLocalSignedWord(0xe4));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.RegDest.Value16);
			if (this.processor.FlagSet.IsEqual) goto L3238;
			goto L323d;

		L3238:
			this.processor.EmitGlobalWord(0x70e4, this.processor.SubtractWord(this.processor.PullGlobalWord(0x70e4), 0x2));

		L323d:
			ReshuffleCitizens(townIdx, this.processor.PullLocalWord(0x8));

			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L3257;
			goto L32cd;

		L3257:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L325f;
			goto L32cd;

		L325f:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L3269;
			goto L32cd;

		L3269:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e2);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xe8));
			if (this.processor.FlagSet.IsNotEqual) goto L3279;
			goto L32cd;

		L3279:
			RenderCitizenIcons(townIdx, 100, this.processor.PullLocalSignedWord(0x42),
				this.processor.PullLocalWord(0x8), 92);

			this.owner.TextPainter.RenderTextToScreen0(0x25fe, 190, this.processor.PullLocalSignedWord(0x42) + 5, 15);

			this.processor.EmitLocalWord(0x42, this.processor.SumWord(this.processor.PullLocalWord(0x42), 0x10));

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				100, this.processor.PullLocalSignedWord(0x42) - 2,
				222, this.processor.PullLocalSignedWord(0x42) - 2, 1);

		L32cd:
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L32d7;
			goto L32e0;

		L32d7:
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitGlobalWord(0x70e4, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0x70e2, this.processor.RegAcc.Value16);

		L32e0:
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0x4e, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xdeb8, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xdeb6, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xd2f6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L32fc;

		L32f8:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L32fc:
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0x2);
			if (this.processor.FlagSet.IsLess) goto L3306;
			goto L3323;

		L3306:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].Unfamiliar[this.processor.PullLocalWord(0xea)], 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L331c;
			goto L3320;

		L331c:
			this.processor.EmitGlobalWord(0xdeb8, this.processor.IncrementWord(this.processor.PullGlobalWord(0xdeb8)));

		L3320:
			goto L32f8;

		L3323:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xdeb8));
			if (this.processor.FlagSet.IsLess) goto L3339;
			goto L3350;

		L3339:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0xdeb8);
			this.processor.RegCount.Value16 = this.processor.SubtractWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitGlobalWord(0xd2f6, this.processor.RegCount.Value16);

		L3350:
			this.processor.EmitLocalWord(0xf6, 0x8);
			this.processor.EmitLocalWord(0xfc, 0x45);
			this.processor.EmitLocalWord(0xfa, 0x64);
			this.processor.EmitLocalWord(0x100, 0x74);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L3375;

		L3371:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L3375:
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0x80);
			if (this.processor.FlagSet.IsLess) goto L3380;
			goto L39ef;

		L3380:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L339c;
			goto L38d4;

		L339c:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81df));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)townIdx);
			if (this.processor.FlagSet.IsEqual) goto L33bb;
			goto L38d4;

		L33bb:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsLess) goto L33d7;
			goto L340f;

		L33d7:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.EmitGlobalWord(0xdeb8, this.processor.IncrementWord(this.processor.PullGlobalWord(0xdeb8)));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xdeb8));
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L33f1;
			goto L340b;

		L33f1:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind > 1) goto L3401;
			goto L340f;

		L3401:
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L340b;
			goto L340f;

		L340b:
			this.processor.EmitGlobalWord(0xd2f6, this.processor.IncrementWord(this.processor.PullGlobalWord(0xd2f6)));

		L340f:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L341a;
			goto L3672;

		L341a:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2f6);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70dc), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L3426;
			goto L3463;

		L3426:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L3438;
			goto L3672;

		L3438:
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0xea);
			this.processor.RegAcc.LowByte = this.processor.SumByte(this.processor.RegAcc.LowByte, (byte)(this.owner.World.RoundTally & 0xff));
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x7);
			if (this.processor.FlagSet.IsEqual) goto L3447;
			goto L3672;

		L3447:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L3453;
			goto L3672;

		L3453:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind >= 4) goto L3463;
			goto L3672;

		L3463:
			this.processor.RegAcc.Value16 = 0xffff;
			this.processor.EmitLocalWord(0x108, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xc8, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x106, 0x0);
			goto L347b;

		L3477:
			this.processor.EmitLocalWord(0x106, this.processor.IncrementWord(this.processor.PullLocalWord(0x106)));

		L347b:
			this.processor.CompareWord(this.processor.PullLocalWord(0x106), 0x80);
			if (this.processor.FlagSet.IsLess) goto L3486;
			goto L353f;

		L3486:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x106));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L34a2;
			goto L353c;

		L34a2:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x106));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81df));
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.CompareWord(this.processor.RegAcc.Value16, (ushort)townIdx);
			if (this.processor.FlagSet.IsEqual) goto L34c1;
			goto L353c;

		L34c1:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x106));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsLess) goto L34dd;
			goto L353c;

		L34dd:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			this.processor.ScaleIndex(0xc, this.processor.PullLocalWord(0x106));
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndex(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.RegDest.Value16 = this.processor.SumWord(this.processor.RegDest.Value16, this.processor.RegAcc.Value16);

			this.owner.Helpers.ObtainDistance(
				this.owner.World.Towns[townIdx].Place.X,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d5)),
				this.owner.World.Towns[townIdx].Place.Y,
				this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegDest.Value16 + 0x81d6)));

			this.processor.EmitLocalWord(0xb2, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc8);
			this.processor.CompareWord(this.processor.PullLocalWord(0xb2), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L352c;
			goto L353c;

		L352c:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xb2);
			this.processor.EmitLocalWord(0xc8, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0x106, 0x108);

		L353c:
			goto L3477;

		L353f:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2f6);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70dc), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L354b;
			goto L35e1;

		L354b:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x8078), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3555;
			goto L3577;

		L3555:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].FoundScienceTally < 
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].FoundScienceTally)
				goto L356e;

			goto L3577;

		L356e:
			this.processor.EmitGlobalWord(0xdb42, 0xfc19);
			goto L35de;

		L3577:
			this.processor.CompareWord(this.processor.PullLocalWord(0xc8), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L3581;
			goto L35de;

		L3581:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x108));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L35a5;
			goto L35de;

		L35a5:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x108));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L35c1;
			goto L35de;

		L35c1:
			this.owner.UnitOps.EliminateUnit(this.processor.PullGlobalSignedWord(0x6548),
				this.processor.PullLocalSignedWord(0x108));
			
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker &= 0xfe;

		L35de:
			goto L3672;

		L35e1:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L35ed;
			goto L365f;

		L35ed:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, " can't support\n");

			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0x108));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ".\n Unit Disbanded.\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x3);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

		L365f:
			this.owner.UnitOps.EliminateUnit(this.processor.PullGlobalSignedWord(0x6548),
				this.processor.PullLocalSignedWord(0x108));
			
			goto L32e0;

		L3672:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L367e;
			goto L38d4;

		L367e:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3686;
			goto L38d4;

		L3686:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x1a);
			if (this.processor.FlagSet.IsLess) goto L36a2;
			goto L384a;

		L36a2:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd2f6), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L36ac;
			goto L36c8;

		L36ac:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xf6) + 8,
				this.processor.PullLocalSignedWord(0xfc) + 12,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x9 << 1) + 0xd4ce)));

		L36c8:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L36e4;
			goto L372a;

		L36e4:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xf6),
				this.processor.PullLocalSignedWord(0xfc) + 12,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x8 << 1) + 0xd4ce)));

			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind >= 2)
			{
				this.owner.Services.RenderPictureToScreen(
					this.processor.PullGlobalWord(0xaa),
					this.processor.PullLocalSignedWord(0xf6) + 2,
					this.processor.PullLocalSignedWord(0xfc) + 12,
					this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x8 << 1) + 0xd4ce)));
			}
			goto L384a;

		L372a:
			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 16);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.SubtractBorrowWord(this.processor.RegCount.Value16, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.NegateWord(this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.RegCount.Value16);

			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind == 5)
			{
				this.processor.IncrementLocalWord(0xe8);
			}

			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind >= 4) goto L3769;
			goto L384a;

		L3769:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113e)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L378d;
			goto L384a;

		L378d:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L3797;
			goto L384a;

		L3797:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1138)), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L37bb;
			goto L380d;

		L37bb:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegCount.LowByte = (byte)((sbyte)this.owner.World.Towns[townIdx].Place.X);
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d5)), this.processor.RegCount.LowByte);
			if (this.processor.FlagSet.IsEqual) goto L37e4;
			goto L380d;

		L37e4:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegCount.LowByte = (byte)((sbyte)this.owner.World.Towns[townIdx].Place.Y);
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d6)), this.processor.RegCount.LowByte);
			if (this.processor.FlagSet.IsNotEqual) goto L380d;
			goto L384a;

		L380d:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xf6),
				this.processor.PullLocalSignedWord(0xfc) + 12,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xd << 1) + 0xd4ce)));

			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x1);
			if (this.processor.FlagSet.IsGreater) goto L382f;
			goto L384a;

		L382f:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xf6) + 2,
				this.processor.PullLocalSignedWord(0xfc) + 12,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xd << 1) + 0xd4ce)));

		L384a:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xf6),
				this.processor.PullLocalSignedWord(0xfc),
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(((this.processor.PullUInt8(this.processor.SegData.Value16,
						(ushort)((0x600 * this.processor.PullGlobalWord(0x6548)) +
							(0xc * this.processor.PullLocalWord(0xea)) + 0x81d7)) +
						(this.processor.PullGlobalWord(0x6548) << 5) + 0x40) << 1) + 0xd4ce)));

			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.AddScaledToSource(0x600, this.processor.PullGlobalWord(0x6548));

			RenderMiniMapDot(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d5)),
				this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d6)), 7);

			this.processor.EmitLocalWord(0xf6, this.processor.SumWord(this.processor.PullLocalWord(0xf6), 0x10));
			this.processor.CompareWord(this.processor.PullLocalWord(0xf6), 0x70);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L38ba;
			goto L38d4;

		L38ba:
			this.processor.EmitLocalWord(0xf6, 0x8);
			this.processor.EmitLocalWord(0xfc, this.processor.SumWord(this.processor.PullLocalWord(0xfc), 0x10));
			this.processor.CompareWord(this.processor.PullLocalWord(0xfc), 0x55);
			if (this.processor.FlagSet.IsGreater) goto L38cf;
			goto L38d4;

		L38cf:
			this.processor.EmitLocalWord(0xfc, this.processor.SubtractWord(this.processor.PullLocalWord(0xfc), 0x18));

		L38d4:
			this.processor.CompareWord(this.processor.PullLocalWord(0x4e), 0x12);
			if (this.processor.FlagSet.IsLess) goto L38dd;
			goto L39ec;

		L38dd:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L38e7;
			goto L39ec;

		L38e7:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.CompareByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d7)), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L3903;
			goto L39ec;

		L3903:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L390f;
			goto L39ec;

		L390f:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3917;
			goto L39ec;

		L3917:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d5));
			this.processor.RegAcc.HighByte = 0;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xd8));
			if (this.processor.FlagSet.IsEqual) goto L3938;
			goto L39ec;

		L3938:
			this.processor.RegAcc.Value16 = 0x600;
			this.processor.ScaleAccumulatorToBase(this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));
			this.processor.RegAcc.LowByte = this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d6));
			this.processor.RegAcc.HighByte = 0;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xe4));
			if (this.processor.FlagSet.IsEqual) goto L3959;
			goto L39ec;

		L3959:
			this.owner.MapOps.RenderUnitSprite(this.processor.PullGlobalSignedWord(0x6548),
				this.processor.PullLocalSignedWord(0xea),
				this.processor.PullLocalSignedWord(0xfa),
				this.processor.PullLocalSignedWord(0x100));

			this.processor.EmitGlobalByte(0xba06, 0x0);
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.ScaleIndexToSource(0xc, this.processor.PullLocalWord(0xea));

			this.owner.Conquest.ObtainCityLabel(this.processor.PullInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81df)));
			
			this.processor.EmitGlobalByte(0xba09, 0x2e);
			this.processor.EmitGlobalByte(0xba0a, 0x0);

			this.owner.TextPainter.RenderTextToScreen0(0xba06,
				this.processor.PullLocalSignedWord(0xfa),
				this.processor.PullLocalSignedWord(0x100) + 15,
				0);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x4e);
			this.processor.IncrementLocalWord(0x4e);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.EmitUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74), this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xfa, this.processor.SumWord(this.processor.PullLocalWord(0xfa), 0x12));
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4e);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x6;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L39e1;
			goto L39ec;

		L39e1:
			this.processor.EmitLocalWord(0xfa, 0x64);
			this.processor.EmitLocalWord(0x100, this.processor.SumWord(this.processor.PullLocalWord(0x100), 0x10));

		L39ec:
			goto L3371;

		L39ef:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L39fa;
			goto L3a72;

		L39fa:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xdeb8);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70dc), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L3a06;
			goto L3a16;

		L3a06:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xdeb8);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70dc));
			this.processor.RegCount.Value16 = 0x5;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitGlobalWord(0xe3c2, this.processor.SumWord(this.processor.PullGlobalWord(0xe3c2), this.processor.RegAcc.Value16));

		L3a16:
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullGlobalSignedWord(0xdeb8),
				0, this.owner.World.Towns[townIdx].TrueExtent);

			this.processor.EmitGlobalWord(0xe3c2, this.processor.SumWord(this.processor.PullGlobalWord(0xe3c2), this.processor.RegAcc.Value16));
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L3a4a;
			goto L3a50;

		L3a4a:
			this.processor.RegAcc.Value16 = 0x5;
			goto L3a53;

		L3a50:
			this.processor.RegAcc.Value16 = 0x7;

		L3a53:
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x3a;
			this.processor.ScaleAccumulatorToBase((ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].ChiefVisuals);
			this.processor.RegAcc.Value16 = this.processor.RegCount.Value16;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1516)));
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0x6546));
			this.processor.EmitGlobalWord(0xdb42, this.processor.SubtractWord(this.processor.PullGlobalWord(0xdb42), this.processor.RegAcc.Value16));

		L3a72:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L3a7e;
			goto L5cd0;

		L3a7e:
			this.processor.CompareWord(this.processor.RegData.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3a86;
			goto L5cd0;

		L3a86:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x2);

			RenderFrame(211, 1, 317, 97);

			this.processor.RegAcc.Value16 = 0x2;
			this.processor.EmitLocalWord(0x106, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xe4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0x40, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xb4, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xda), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L3ac6;
			goto L3b80;

		L3ac6:
			this.processor.EmitLocalWord(0xbc, 0x0);
			goto L3ad3;

		L3acf:
			this.processor.EmitLocalWord(0xbc, this.processor.IncrementWord(this.processor.PullLocalWord(0xbc)));

		L3ad3:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x15);
			if (this.processor.FlagSet.IsLess) goto L3add;
			goto L3b80;

		L3add:
			this.processor.RegAcc.Value16 = (ushort)townIdx;
			this.processor.CompareWord((ushort)this.owner.World.MarvelTownIdx[this.processor.PullLocalWord(0xbc) + 1], this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L3aef;
			goto L3b7d;

		L3aef:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xbc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x18);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);

			this.processor.ScaleIndex(0x1e, this.processor.PullLocalWord(0xe8));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xbb8);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.UppercaseText(0xba06);

			this.owner.BlockText.ShrinkTextToWidth(0xba06, 63);

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 253, this.processor.PullLocalSignedWord(0xe4) + 2, 15);

			RenderResourceIcon((short)(this.processor.PullLocalSignedWord(0xe8) + 1),
				(short)(((this.processor.PullLocalByte(0x106) & 1) != 0) ? 213 : 233),
				(short)(this.processor.PullLocalSignedWord(0xe4) - 2));

			this.processor.EmitLocalWord(0xe4, this.processor.SumWord(this.processor.PullLocalWord(0xe4), 0x6));
			this.processor.IncrementLocalWord(0x106);
			this.processor.IncrementLocalWord(0x40);

		L3b7d:
			goto L3acf;

		L3b80:
			this.processor.EmitLocalWord(0xfe, 0x0);
			this.processor.CopyLocalWord(0xda, 0xbc);
			goto L3b95;

		L3b91:
			this.processor.EmitLocalWord(0xbc, this.processor.IncrementWord(this.processor.PullLocalWord(0xbc)));

		L3b95:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x18);
			if (this.processor.FlagSet.IsLess) goto L3b9f;
			goto L3c83;

		L3b9f:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xbc);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegCount.Value16 = this.processor.BitAndWord(this.processor.RegCount.Value16, 
				this.owner.World.Towns[townIdx].StructureMarkers0);
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers1);
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L3bc9;
			goto L3c80;

		L3bc9:
			this.processor.CompareWord(this.processor.PullLocalWord(0x106), 0x10);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L3bd3;
			goto L3be9;

		L3bd3:
			this.processor.EmitLocalWord(0xca, this.processor.BitOrWord(this.processor.PullLocalWord(0xca), 0x2));
			this.processor.EmitLocalWord(0xca, this.processor.BitAndWord(this.processor.PullLocalWord(0xca), 0xfffe));
			this.processor.CopyLocalWord(0xbc, 0xfe);
			goto L3c83;

		L3be9:
			this.processor.EmitLocalWord(0xb4, this.processor.IncrementWord(this.processor.PullLocalWord(0xb4)));

			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				309,
				this.processor.PullLocalSignedWord(0xe4) + 1,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((0xb << 1) + 0xd4ce)));

			this.processor.ScaleIndex(0x1e, this.processor.PullLocalWord(0xbc));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xbb8);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.owner.BlockText.ShrinkTextToWidth(0xba06, 56);

			this.owner.Libc.UppercaseText(0xba06);

			this.owner.TextPainter.RenderTextToScreen0(0xba06, 253, this.processor.PullLocalSignedWord(0xe4) + 2, 15);

			RenderResourceIcon((short)(this.processor.PullLocalSignedWord(0xbc) + 1),
				(short)(((this.processor.PullLocalByte(0x106) & 1) != 0) ? 213 : 233),
				(short)(this.processor.PullLocalSignedWord(0xe4) - 2));

			this.processor.EmitLocalWord(0xe4, this.processor.SumWord(this.processor.PullLocalWord(0xe4), 0x6));
			this.processor.IncrementLocalWord(0x106);

		L3c80:
			goto L3b91;

		L3c83:
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				231, 0, 250, 0, 0);

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				231, 1, 250, 1, 1);

			this.processor.TestBitsByte(this.processor.PullLocalByte(0xca), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L3cc5;
			goto L3ce5;

		L3cc5:
			RenderLabelBox(287, 88, 315, 96, 0x262a, 9);

		L3ce5:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 309, 2, 8, 96, 14, 12);

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L3d1b;
			goto L3d36;

		L3d1b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].PresentMakingIdx);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1142));
			this.processor.EmitLocalWord(0xde, this.processor.RegAcc.Value16);
			goto L3d54;

		L3d36:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb2));
			this.processor.EmitLocalWord(0xde, this.processor.RegAcc.Value16);

		L3d54:
			this.owner.Helpers.VerifyValueSpan(
				((this.processor.PullLocalSignedWord(0xde) - 1) / 10) + 1,
				((this.owner.World.Towns[townIdx].MaterialsTally - 1) / 100) + 1, 99);

			this.processor.EmitLocalWord(0x44, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x50;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x4a);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4a);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0xe8));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x44);
			this.processor.RegData.Value16 = this.processor.RegCount.Value16;
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.RegCount.Value16 = this.processor.RegData.Value16;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0x44);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegBase.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xf6, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xde);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x44);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegCount.LowByte = 0x3;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x8);
			this.processor.EmitLocalWord(0xfc, this.processor.RegAcc.Value16);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				230, 99,
				this.processor.PullLocalSignedWord(0xf6) + 3,
				this.processor.PullLocalSignedWord(0xfc) + 19,
				1);

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L3e01;
			goto L3e59;

		L3e01:
			RenderLabelBox(231, 106, 263, 114, (ushort)(((this.owner.World.Towns[townIdx].ConditionMarker & 0x10) != 0) ? 0x262f : 0x2635), 9);

			RenderLabelBox(294, 106, 311, 114, 0x263c, 9);

		L3e59:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L3e6b;
			goto L3e99;

		L3e6b:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				264, 100,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((((byte)this.owner.World.Towns[townIdx].PresentMakingIdx +
						(this.processor.PullGlobalWord(0x6548) << 5) + 0x40) << 1) + 0xd4ce)));
			goto L3ee5;

		L3e99:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xb9a);
			this.owner.Libc.CopyText(0xba06, this.processor.RegAcc.Value16);

			this.owner.BlockText.ShrinkTextToWidth(0xba06, 0x56);

			this.owner.TextPainter.RenderCenteredTextToScreen0(0xba06, 274, 100, 15);

		L3ee5:
			FloodBoxWithMotif(231, 116,
				this.processor.PullLocalSignedWord(0xf6) + 1,
				this.processor.PullLocalSignedWord(0xfc) + 1);

			this.processor.EmitLocalWord(0xea, 0x0);
			goto L3f0e;

		L3f0a:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L3f0e:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L3f23;
			goto L3f60;

		L3f23:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(((this.processor.PullLocalSignedWord(0xea) %
					(this.processor.PullLocalSignedWord(0x4a) *
						this.processor.PullLocalSignedWord(0x44))) *
					this.processor.PullLocalSignedWord(0xe8)) /
					this.processor.PullLocalSignedWord(0x44)) + 232,
				((this.processor.PullLocalSignedWord(0xea) /
					(this.processor.PullLocalSignedWord(0x4a) *
					this.processor.PullLocalSignedWord(0x44))) * 8) + 117,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x9 << 1) + 0xd4ce)));
			goto L3f0a;

		L3f60:
			if (this.owner.World.Towns[townIdx].TrueExtent != 0)
			{
				this.owner.Helpers.VerifyValueSpan(80 / this.owner.World.Towns[townIdx].TrueExtent, 1, 8);
			}
			else
			{
				this.processor.RegAcc.Value16 = 1;
			}

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x50;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0x4a);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xfc, this.processor.RegAcc.Value16);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				2, 106, 91,
				(this.processor.PullLocalSignedWord(0x4a) *
					this.processor.PullLocalSignedWord(0xfc)) + 12,
				1);

			this.owner.TextPainter.RenderTextToScreen0(0x2640, 8, 108, 15);

			FloodBoxWithMotif(3, 115,
				(this.owner.World.Towns[townIdx].TrueExtent * this.processor.PullLocalSignedWord(0xe8)) + 9,
				(this.processor.PullLocalSignedWord(0x4a) * this.processor.PullLocalSignedWord(0xfc)) + 2);

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L4016;
			goto L4043;

		L4016:
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				5, 155, 
				(this.owner.World.Towns[townIdx].TrueExtent * this.processor.PullLocalSignedWord(0xe8)) + 9, 155, 1);

		L4043:
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L4050;

		L404c:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L4050:
			this.processor.RegSource.Value16 = (ushort)(0x1c * townIdx);
			this.owner.Helpers.VerifyValueSpan(
				this.owner.World.Towns[townIdx].GrainTally,
				0, (this.owner.World.Towns[townIdx].TrueExtent + 1) *
					this.processor.PullLocalSignedWord(0x4a));

			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xea));
			if (this.processor.FlagSet.IsGreater) goto L407b;
			goto L40ba;

		L407b:
			this.processor.RegSource.Value16 = (ushort)(this.owner.World.Towns[townIdx].TrueExtent + 1);
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				((this.processor.PullLocalSignedWord(0xea) % (short)this.processor.RegSource.Value16) *
					this.processor.PullLocalSignedWord(0xe8)) + 4,
				((this.processor.PullLocalSignedWord(0xea) / (short)this.processor.RegSource.Value16) *
					this.processor.PullLocalSignedWord(0xfc)) + 116,
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((0x8 << 1) + 0xd4ce)));
			goto L404c;

		L40ba:
			RenderFrame(2, 23, 124, 65);

			this.processor.EmitLocalWord(0xfc, 0x19);

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 2, 23, 122, 9, 1);

			this.owner.TextPainter.RenderTextToScreen0(0x264d, 8, this.processor.PullLocalSignedWord(0xfc), 15);

			this.processor.EmitLocalWord(0xfc, this.processor.SumWord(this.processor.PullLocalWord(0xfc), 0x8));
			this.processor.EmitLocalWord(0xbc, 0x0);
			goto L4122;

		L411e:
			this.processor.EmitLocalWord(0xbc, this.processor.IncrementWord(this.processor.PullLocalWord(0xbc)));

		L4122:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x3);
			if (this.processor.FlagSet.IsLess) goto L412c;
			goto L427d;

		L412c:
			this.processor.EmitLocalWord(0xd8, 0x4);
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L413c;
			goto L4190;

		L413c:
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullInt16(this.processor.SegData.Value16, (ushort)((this.processor.PullLocalWord(0xbc) << 1) + 0x70da)),
				(this.owner.World.Towns[townIdx].TrueExtent * 2) +
					(this.processor.PullLocalSignedWord(0x48) * 
					this.processor.PullGlobalSignedWord(0xe3c6)), 999);

			this.owner.Helpers.VerifyValueSpan(116 / ((short)this.processor.RegAcc.Value16 + 1), 1, 8);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			goto L41b8;

		L4190:
			this.owner.Helpers.VerifyValueSpan(
				116 / (this.processor.PullInt16(this.processor.SegData.Value16, 
					(ushort)((this.processor.PullLocalWord(0xbc) << 1) + 0x70da)) + 1),
				1, 8);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);

		L41b8:
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L41c5;

		L41c1:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L41c5:
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xbc);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x70da)), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L41d8;
			goto L427a;

		L41d8:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L41e2;
			goto L420a;

		L41e2:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x48);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xe3c6));
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.PullLocalWord(0xea));
			if (this.processor.FlagSet.IsEqual) goto L4205;
			goto L420a;

		L4205:
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), 0x4));

		L420a:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L4214;
			goto L422f;

		L4214:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd2f6), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L421e;
			goto L422f;

		L421e:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2f6);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L422a;
			goto L422f;

		L422a:
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), 0x4));

		L422f:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L4239;
			goto L424e;

		L4239:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70de);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0xd2e0));
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xea));
			if (this.processor.FlagSet.IsEqual) goto L4249;
			goto L424e;

		L4249:
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), 0x2));

		L424e:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xd8),
				(this.processor.PullLocalSignedWord(0xbc) * 8) +
					this.processor.PullLocalSignedWord(0xfc),
				this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)(((this.processor.PullLocalWord(0xbc) + 8) << 1) + 0xd4ce)));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), this.processor.RegAcc.Value16));
			goto L41c1;

		L427a:
			goto L411e;

		L427d:
			this.processor.EmitLocalWord(0xd8, 0x8);

			this.owner.Helpers.VerifyValueSpan(
				224 / (this.processor.PullGlobalSignedWord(0x70e0) + 
					this.processor.PullGlobalSignedWord(0x70e6) +
					this.processor.PullGlobalSignedWord(0xe17a) + 
					this.processor.PullGlobalSignedWord(0xd2e0) + 2),
				1, 16);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L42bf;

		L42bb:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L42bf:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e0);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L42cb;
			goto L42f4;

		L42cb:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xd8) / 2,
				this.processor.PullLocalSignedWord(0xfc) + 24,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xe << 1) + 0xd4ce)));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), this.processor.RegAcc.Value16));
			goto L42bb;

		L42f4:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70e0), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L42fe;
			goto L4303;

		L42fe:
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), 0x8));

		L4303:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xd8);
			this.processor.EmitLocalWord(0xf6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L4318;

		L4314:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L4318:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xe17a);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L4324;
			goto L434d;

		L4324:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xd8) / 2,
				this.processor.PullLocalSignedWord(0xfc) + 24,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xb << 1) + 0xd4ce)));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), this.processor.RegAcc.Value16));
			goto L4314;

		L434d:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xe17a), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L4357;
			goto L435c;

		L4357:
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), 0x8));

		L435c:
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L4369;

		L4365:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L4369:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e6);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L4375;
			goto L439e;

		L4375:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				this.processor.PullLocalSignedWord(0xd8) / 2,
				this.processor.PullLocalSignedWord(0xfc) + 24,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0xc << 1) + 0xd4ce)));

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.EmitLocalWord(0xd8, this.processor.SumWord(this.processor.PullLocalWord(0xd8), this.processor.RegAcc.Value16));
			goto L4365;

		L439e:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x48);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xe3c6));
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.PullGlobalWord(0x70da));
			if (this.processor.FlagSet.IsGreater) goto L43c1;
			goto L4497;

		L43c1:
			this.owner.Helpers.VerifyValueSpan(
				116 / ((this.owner.World.Towns[townIdx].TrueExtent * 2) +
					(this.processor.PullLocalSignedWord(0x48) *
					this.processor.PullGlobalSignedWord(0xe3c6)) + 1),
				1, 8);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70da);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L4407;

		L4403:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L4407:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x48);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xe3c6));
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.PullLocalWord(0xea));
			if (this.processor.FlagSet.IsGreater) goto L442a;
			goto L4449;

		L442a:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(this.processor.PullLocalSignedWord(0xea) *
					this.processor.PullLocalSignedWord(0xe8)) + 8,
				this.processor.PullLocalSignedWord(0xfc),
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x8 << 1) + 0xd4ce)));
			goto L4403;

		L4449:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				(this.processor.PullLocalSignedWord(0xe8) * this.processor.PullGlobalSignedWord(0x70da)) + 8,
				this.processor.PullLocalSignedWord(0xfc),
				(((this.owner.World.Towns[townIdx].TrueExtent * 2) +
					(this.processor.PullLocalSignedWord(0x48) * this.processor.PullGlobalSignedWord(0xe3c6)) -
					this.processor.PullGlobalSignedWord(0x70da)) * this.processor.PullLocalSignedWord(0xe8)) + 4, 
				8, 15, 0);

		L4497:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2f6);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70dc), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L44a3;
			goto L453a;

		L44a3:
			this.owner.Helpers.VerifyValueSpan(
				116 / (this.processor.PullGlobalSignedWord(0x70dc) + 1),
				1, 8);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70dc);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L44d3;

		L44cf:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L44d3:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2f6);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L44df;
			goto L4502;

		L44df:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(this.processor.PullLocalSignedWord(0xea) *
					this.processor.PullLocalSignedWord(0xe8)) + 8,
				this.processor.PullLocalSignedWord(0xfc) + 8,
				this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)((0x9 << 1) + 0xd4ce)));
			goto L44cf;

		L4502:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				(this.processor.PullGlobalSignedWord(0x70dc) * this.processor.PullLocalSignedWord(0xe8)) + 8,
				this.processor.PullLocalSignedWord(0xfc) + 8,
				(this.processor.PullGlobalSignedWord(0xd2f6) - this.processor.PullGlobalSignedWord(0x70dc)) * 
					this.processor.PullLocalSignedWord(0xe8),
				8, 15, 0);

		L453a:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xd2e0), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L4544;
			goto L45a0;

		L4544:
			this.owner.Helpers.VerifyValueSpan(
				116 / (this.processor.PullGlobalSignedWord(0x70de) + 1),
				1, 8);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				((this.processor.PullGlobalSignedWord(0x70de) - this.processor.PullGlobalSignedWord(0xd2e0)) *
					this.processor.PullLocalSignedWord(0xe8)) + 6,
				this.processor.PullLocalSignedWord(0xfc) + 16,
				(this.processor.PullLocalSignedWord(0xe8) * this.processor.PullGlobalSignedWord(0xd2e0)) + 2,
				8, 15, 0);

		L45a0:
			FloodBoxWithMotif(8, 8, 200, 13);

			RenderCitizenIcons(townIdx, 8, 8, this.processor.PullLocalWord(0x8), 192);

			this.processor.EmitLocalWord(0xf4, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan((this.owner.World.Towns[townIdx].TrueExtent * 8) + 24, 0, 128);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xbc, 0x0);
			goto L460c;

		L4608:
			this.processor.EmitLocalWord(0xbc, this.processor.IncrementWord(this.processor.PullLocalWord(0xbc)));

		L460c:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x3);
			if (this.processor.FlagSet.IsLess) goto L4616;
			goto L4770;

		L4616:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xbc);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].CommerceTownIndices[this.processor.PullLocalWord(0xbc)];
			this.processor.RegAcc.HighByte = 0;
			this.processor.EmitLocalWord(0x4c, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0x4c), 0xff);
			if (this.processor.FlagSet.IsNotEqual) goto L4635;
			goto L476d;

		L4635:
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(this.processor.PullLocalSignedWord(0x4c));

			this.owner.Libc.AppendText(0xba06, ":+");

			this.processor.ScaleIndexToBase(0x1c, this.processor.PullLocalWord(0x4c));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].ParticipantIdx;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x6548));
			if (this.processor.FlagSet.IsNotEqual) goto L466b;
			goto L46bb;

		L466b:
			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(((short)this.owner.World.Towns[townIdx].RootCommerce +
					(short)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].RootCommerce + 4) >> 3),
					10));
			goto L4708;

		L46bb:
			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(((short)this.owner.World.Towns[townIdx].RootCommerce +
					(short)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].RootCommerce + 4) >> 4),
					10));

		L4708:
			this.owner.Libc.AppendText(0xba06, "} ");

			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L4722;
			goto L4744;

		L4722:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullLocalWord(0x4c));

			RenderMiniMapDot((ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.X),
				(ushort)((short)this.owner.World.Towns[this.processor.PullLocalWord(0x4c)].Place.Y), 10);

		L4744:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L474e;
			goto L476d;

		L474e:
			this.owner.TextPainter.RenderTextToScreen0(0xba06, 98,
				(this.processor.PullLocalSignedWord(0xbc) * 6) + 179, 10);

		L476d:
			goto L4608;

		L4770:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x2);
			if (this.processor.FlagSet.IsEqual) goto L477a;
			goto L479d;

		L477a:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource(this.processor.PullGlobalWord(0x653e));

			RenderMiniMapDot((ushort)((short)this.owner.World.Towns[this.processor.PullGlobalWord(0x653e)].Place.X),
				(ushort)((short)this.owner.World.Towns[this.processor.PullGlobalWord(0x653e)].Place.Y), 15);

		L479d:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L47a7;
			goto L4852;

		L47a7:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70dc);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0x6c98);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x14);
			this.processor.EmitLocalWord(0xe0, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xb882));
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.EmitLocalWord(0xe0, this.processor.SumWord(this.processor.PullLocalWord(0xe0), this.processor.RegAcc.Value16));

			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0xe0), 1, 99);

			this.owner.Helpers.VerifyValueSpan(100 / (short)this.processor.RegAcc.Value16, 1, 8);

			this.processor.EmitLocalWord(0xfc, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, 0x0);
			goto L481b;

		L4817:
			this.processor.EmitLocalWord(0xea, this.processor.IncrementWord(this.processor.PullLocalWord(0xea)));

		L481b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe0);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L4828;
			goto L4852;

		L4828:
			this.owner.Services.RenderPictureToScreen(
				this.processor.PullGlobalWord(0xaa),
				(this.processor.PullLocalSignedWord(0xea) *
					this.processor.PullLocalSignedWord(0xfc)) + 98,
				(this.processor.PullLocalWord(0xea) & 1) + 161,
				this.processor.PullGlobalWord(0xb2ba));
			goto L4817;

		L4852:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L485e;
			goto L5cbd;

		L485e:
			RenderLabelBox(284, 190, 316, 198, 0x2662, 12);

			RenderLabelBox(231, 190, 272, 198, 0x2667, 9);

			this.processor.EmitLocalWord(0xc0, 0x0);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsLess) goto L48b6;
			goto L48fb;

		L48b6:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0xeb);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L48c8;
			goto L48fb;

		L48c8:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegCount.Value16 = this.processor.NegateWord(this.processor.RegCount.Value16);
			this.processor.RegCount.LowByte = this.processor.DecrementByte(this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegAcc.Value16 = this.processor.BitAndWord(this.processor.RegAcc.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers0);
			this.processor.RegData.Value16 = this.processor.BitAndWord(this.processor.RegData.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers1);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L48f5;
			goto L48fb;

		L48f5:
			this.processor.EmitLocalWord(0xc0, 0x1);

		L48fb:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0xeb);
			if (this.processor.FlagSet.IsLess) goto L490d;
			goto L4937;

		L490d:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0xe8);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L491f;
			goto L4937;

		L491f:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.StarshipMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L4931;
			goto L4937;

		L4931:
			this.processor.EmitLocalWord(0xc0, 0x1);

		L4937:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0xe8);
			if (this.processor.FlagSet.IsLess) goto L4949;
			goto L496c;

		L4949:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);

			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegBase.Value16 = this.processor.NegateWord(this.processor.RegBase.Value16);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);

			if (this.owner.World.MarvelTownIdx[Math.Abs(this.owner.World.Towns[townIdx].PresentMakingIdx) - 24] == -1)
				goto L496c;

			this.processor.EmitLocalWord(0xc0, 0x1);

		L496c:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L497e;
			goto L4994;

		L497e:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopTally >= 127)
			{
				this.processor.EmitLocalWord(0xc0, 0x1);
			}

		L4994:
			this.owner.Startup.DisplayMouseCursor();

		L4999:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L49a3;
			goto L49ab;

		L49a3:
			this.owner.Startup.ObtainMouseState();

			goto L4999;

		L49ab:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L49b5;
			goto L4a4e;

		L49b5:
			this.owner.Libc.KeyAvailable();

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L49c2;
			goto L4a4e;

		L49c2:
			this.processor.CompareWord(this.processor.PullLocalWord(0xb8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L49cc;
			goto L4a4e;

		L49cc:
			this.owner.Startup.ObtainMouseState();

			this.processor.CompareWord(this.processor.PullLocalWord(0xc0), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L49db;
			goto L4a4b;

		L49db:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.IncrementLocalWord(0xc0);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0xc0);
			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L49ef;
			goto L4a16;

		L49ef:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 231, 106, 32, 8, 14, 9);

			goto L4a3a;

		L4a16:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), 231, 106, 32, 8, 9, 14);

		L4a3a:
			this.owner.Startup.DisplayMouseCursor();

			this.owner.TextPainter.AwaitTime(10);

		L4a4b:
			goto L49ab;

		L4a4e:
			this.processor.EmitLocalWord(0xf0, 0xffff);

			this.owner.Libc.KeyAvailable();

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L4a61;
			goto L4a76;

		L4a61:
			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.EmitLocalWord(0xf0, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x0;
			this.owner.GlobDb3e = 0;
			this.owner.GlobDb3c = 0;
			goto L4a86;

		L4a76:
			this.processor.CompareWord(this.processor.PullLocalWord(0xb8), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L4a80;
			goto L4a86;

		L4a80:
			this.processor.EmitLocalWord(0xf0, 0x70);

		L4a86:
			this.owner.Startup.ConcealMouseCursor();

			this.processor.CompareWord(this.owner.GlobDb3a, 0x2);
			if (this.processor.FlagSet.IsEqual) goto L4a95;
			goto L4abf;

		L4a95:
			this.processor.CompareWord(this.owner.GlobDb3c, 0xe6);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4aa0;
			goto L4abf;

		L4aa0:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x10e);
			if (this.processor.FlagSet.IsLess) goto L4aab;
			goto L4abf;

		L4aab:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x6a);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4ab5;
			goto L4abf;

		L4ab5:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x74);
			if (this.processor.FlagSet.IsGreater) goto L4abf;
			goto L4ac9;

		L4abf:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x41);
			if (this.processor.FlagSet.IsEqual) goto L4ac9;
			goto L4bba;

		L4ac9:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker ^= 0x10;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L4ae8;
			goto L4baa;

		L4ae8:
			this.owner.Advisors.PickCityProduction(this.processor.PullGlobalSignedWord(0x6548), townIdx);
			
			this.processor.EmitLocalWord(0x102, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x2);
			this.processor.CompareWord(this.processor.PullLocalWord(0x102), 0x63);
			if (this.processor.FlagSet.IsNotEqual) goto L4b0e;
			goto L4ba7;

		L4b0e:
			this.processor.EmitGlobalWord(0xb1e8, 0x0);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4b26;
			goto L4b44;

		L4b26:			
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));
			
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]--;

		L4b44:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = this.processor.PullLocalByte(0x102);
			this.owner.World.Towns[townIdx].PresentMakingIdx = (sbyte)this.processor.RegAcc.LowByte;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4b66;
			goto L4b84;

		L4b66:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));
			
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]++;

		L4b84:
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 230, 99, 90, 100, 0);

			goto L4baa;

		L4ba7:
			goto L045f;

		L4baa:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			goto L12c2;

		L4bba:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0xffff);
			if (this.processor.FlagSet.IsEqual) goto L4bc4;
			goto L4be2;

		L4bc4:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L4bce;
			goto L4ff5;

		L4bce:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x6a);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4bd8;
			goto L4ff5;

		L4bd8:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x74);
			if (this.processor.FlagSet.IsLessOrEqual) goto L4be2;
			goto L4ff5;

		L4be2:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x128);
			if (this.processor.FlagSet.IsLess) goto L4bed;
			goto L4bf7;

		L4bed:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x62);
			if (this.processor.FlagSet.IsEqual) goto L4bf7;
			goto L4e80;

		L4bf7:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4c09;
			goto L4c66;

		L4c09:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].PresentMakingIdx);
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x1142));
			this.processor.EmitLocalWord(0x106, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan(
				(10 * this.processor.PullLocalSignedWord(0x106)) -
					this.owner.World.Towns[townIdx].MaterialsTally,
				0, 999);

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0xe8));
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x14;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			goto L4cc9;

		L4c66:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase(this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbb2));
			this.processor.EmitLocalWord(0x106, this.processor.RegAcc.Value16);

			this.owner.Helpers.VerifyValueSpan(
				(10 * this.processor.PullLocalSignedWord(0x106)) -
					this.owner.World.Towns[townIdx].MaterialsTally,
				0, 999);

			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x15);
			if (this.processor.FlagSet.IsGreater) goto L4cc5;
			goto L4cc9;

		L4cc5:
			this.processor.EmitLocalWord(0xe8, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0xe8), 0x1));

		L4cc9:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareWord((ushort)this.owner.World.Towns[townIdx].MaterialsTally, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L4cdb;
			goto L4cdf;

		L4cdb:
			this.processor.EmitLocalWord(0xe8, this.processor.ShiftLeftWord(this.processor.PullLocalWord(0xe8), 0x1));

		L4cdf:
			this.owner.Libc.CopyText(0xba06, "Cost to complete\n");

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4d01;
			goto L4d22;

		L4d01:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, (byte)this.owner.World.Towns[townIdx].PresentMakingIdx);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x112a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);
			goto L4d46;

		L4d22:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);
			this.processor.RegCount.Value16 = 0x1e;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xb9a);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

		L4d46:
			this.owner.Libc.AppendText(0xba06, ": $");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)this.processor.PullLocalWord(0xe8), 10));

			this.owner.Libc.AppendText(0xba06, "\nTreasury: $");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText(this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold, 10));

			this.owner.Libc.AppendText(0xba06, "\n");

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4dd0;
			goto L4df5;

		L4dd0:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L4de2;
			goto L4df5;

		L4de2:
			this.owner.Libc.AppendText(0xba06, "CIVIL DISORDER\n");
			goto L4e18;

		L4df5:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.CompareWord((ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4e08;
			goto L4e18;

		L4e08:
			this.owner.Libc.AppendText(0xba06, " Yes\n No\n");

		L4e18:
			this.owner.Startup.DisplayMouseCursor();

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

			this.owner.Dialogs.DisplayMenu(0xba06, 100, 80, 1);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L4e42;
			goto L4e78;

		L4e42:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.CompareWord((ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4e55;
			goto L4e78;

		L4e55:
			this.processor.RegAcc.Value16 = 0xa;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0x106));
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.owner.World.Towns[townIdx].MaterialsTally = (short)this.processor.RegCount.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold -= (short)this.processor.RegAcc.Value16;

		L4e78:
			this.owner.Startup.ConcealMouseCursor();

			goto L045f;

		L4e80:
			this.processor.CompareWord(this.owner.GlobDb3c, 0xe6);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4e8b;
			goto L4e96;

		L4e8b:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x10e);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4e96;
			goto L4ea0;

		L4e96:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x63);
			if (this.processor.FlagSet.IsEqual) goto L4ea0;
			goto L4f37;

		L4ea0:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker &= 0xef;

			this.owner.Startup.DisplayMouseCursor();

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4ec4;
			goto L4ee2;

		L4ec4:			
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));
			
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]--;

		L4ee2:
			this.owner.Advisors.PickCityProduction(this.processor.PullGlobalSignedWord(0x6548), townIdx);
			
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].PresentMakingIdx = (sbyte)this.processor.RegCount.LowByte;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4f11;
			goto L4f2f;

		L4f11:			
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));

			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]++;

		L4f2f:
			this.owner.Startup.ConcealMouseCursor();

			goto L045f;

		L4f37:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x69);
			if (this.processor.FlagSet.IsEqual) goto L4f41;
			goto L4f4a;

		L4f41:
			this.processor.EmitGlobalWord(0x2496, 0x0);
			goto L4faf;

		L4f4a:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x68);
			if (this.processor.FlagSet.IsEqual) goto L4f54;
			goto L4f5d;

		L4f54:
			this.processor.EmitGlobalWord(0x2496, 0x1);
			goto L4faf;

		L4f5d:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x6d);
			if (this.processor.FlagSet.IsEqual) goto L4f67;
			goto L4f70;

		L4f67:
			this.processor.EmitGlobalWord(0x2496, 0x2);
			goto L4faf;

		L4f70:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x76);
			if (this.processor.FlagSet.IsEqual) goto L4f7a;
			goto L4f83;

		L4f7a:
			this.processor.EmitGlobalWord(0x2496, 0x3);
			goto L4faf;

		L4f83:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x60);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L4f8d;
			goto L4ff5;

		L4f8d:
			this.processor.CompareWord(this.owner.GlobDb3c, 0xe0);
			if (this.processor.FlagSet.IsLess) goto L4f98;
			goto L4ff5;

		L4f98:
			this.processor.RegAcc.Value16 = this.owner.GlobDb3c;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x60);
			this.processor.DivideAccumulatorByPowerOfTwo(0x5);
			this.processor.EmitGlobalWord(0x2496, this.processor.RegAcc.Value16);

		L4faf:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullGlobalWord(0x2496), 0x3);
			if (this.processor.FlagSet.IsEqual) goto L4fc6;
			goto L4ff2;

		L4fc6:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.CityScene.DisplayCityScreen(townIdx, -1);
			
			this.processor.EmitGlobalWord(0x6b64, 0x1);
			this.processor.EmitGlobalWord(0x2496, 0x0);
			goto L045f;

		L4ff2:
			goto L12c2;

		L4ff5:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x61);
			if (this.processor.FlagSet.IsEqual) goto L4fff;
			goto L520e;

		L4fff:
			this.processor.EmitLocalWord(0x106, 0x0);

		L5005:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74)));
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0xcf);
			if (this.processor.FlagSet.IsEqual) goto L5026;
			goto L5039;

		L5026:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4e);
			this.processor.CompareWord(this.processor.PullLocalWord(0x106), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L5032;
			goto L5039;

		L5032:
			this.processor.EmitLocalWord(0x106, this.processor.IncrementWord(this.processor.PullLocalWord(0x106)));
			goto L5005;

		L5039:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4e);
			this.processor.CompareWord(this.processor.PullLocalWord(0x106), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L5045;
			goto L504b;

		L5045:
			this.processor.EmitLocalWord(0x106, 0x0);

		L504b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x6;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.RegData.Value16;
			this.processor.RegCount.Value16 = 0x12;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x64);
			this.processor.EmitLocalWord(0xfa, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x6;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x74);
			this.processor.EmitLocalWord(0x100, this.processor.RegAcc.Value16);

		L5078:
			FloodBoxWithMotif(this.processor.PullLocalSignedWord(0xfa),
				this.processor.PullLocalSignedWord(0x100), 16, 16);

			this.owner.TextPainter.AwaitTime(10);

			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.owner.MapOps.RenderUnitSprite(this.processor.PullGlobalSignedWord(0x6548),
				this.processor.PullInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74)),
				this.processor.PullLocalSignedWord(0xfa),
				this.processor.PullLocalSignedWord(0x100));

			this.owner.TextPainter.AwaitTime(10);

			this.owner.Libc.KeyAvailable();

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L50d2;
			goto L5078;

		L50d2:
			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.EmitLocalWord(0xfc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xfc);
			goto L5121;

		L50e2:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L5144;

		L50ee:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L5144;

		L50fa:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x6);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L5144;

		L5108:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x6);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L5144;

		L5116:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L5144;

		L5121:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4800);
			if (this.processor.FlagSet.IsNotEqual) goto L5129;
			goto L5108;

		L5129:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4b00);
			if (this.processor.FlagSet.IsNotEqual) goto L5131;
			goto L50ee;

		L5131:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4d00);
			if (this.processor.FlagSet.IsNotEqual) goto L5139;
			goto L50e2;

		L5139:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5000);
			if (this.processor.FlagSet.IsNotEqual) goto L5141;
			goto L50fa;

		L5141:
			goto L5116;

		L5144:
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0xea),
				0, this.processor.PullLocalSignedWord(0x4e) - 1);

			this.processor.EmitLocalWord(0x106, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xfc), 0xd);
			if (this.processor.FlagSet.IsNotEqual) goto L5167;
			goto L5171;

		L5167:
			this.processor.CompareWord(this.processor.PullLocalWord(0xfc), 0x20);
			if (this.processor.FlagSet.IsEqual) goto L5171;
			goto L51f4;

		L5171:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74)));
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x9);
			if (this.processor.FlagSet.IsNotEqual) goto L5192;
			goto L51bc;

		L5192:
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x74)));
			this.processor.AddScaledToSource(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113a)));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), this.processor.RegAcc.LowByte);

		L51bc:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74)));
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x30));
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74)));
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);

		L51f4:
			this.processor.CompareWord(this.processor.PullLocalWord(0xfc), 0x1b);
			if (this.processor.FlagSet.IsEqual) goto L51fe;
			goto L504b;

		L51fe:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			goto L12c2;

		L520e:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x73);
			if (this.processor.FlagSet.IsEqual) goto L5218;
			goto L532e;

		L5218:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x80);
			if (this.processor.FlagSet.IsEqual) goto L522a;
			goto L532e;

		L522a:
			this.processor.EmitLocalWord(0x106, 0x0);

		L5230:
			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				252,
				((this.processor.PullLocalSignedWord(0x106) +
					this.processor.PullLocalSignedWord(0x40)) * 6) + 3,
				56, 7, 9, 1);

			this.owner.TextPainter.AwaitTime(10);

			this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa),
				252,
				((this.processor.PullLocalSignedWord(0x106) +
					this.processor.PullLocalSignedWord(0x40)) * 6) + 3,
				56, 7, 1, 9);

			this.owner.TextPainter.AwaitTime(10);

			this.owner.Libc.KeyAvailable();

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L52b5;
			goto L5230;

		L52b5:
			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.EmitLocalWord(0xfc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xfc);
			goto L52e0;

		L52c5:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L52f3;

		L52d1:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			goto L52f3;

		L52e0:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4800);
			if (this.processor.FlagSet.IsNotEqual) goto L52e8;
			goto L52d1;

		L52e8:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5000);
			if (this.processor.FlagSet.IsNotEqual) goto L52f0;
			goto L52c5;

		L52f0:

		L52f3:
			this.owner.Helpers.VerifyValueSpan(
				this.processor.PullLocalSignedWord(0xea),
				0, this.processor.PullLocalSignedWord(0xb4) - 1);

			this.processor.EmitLocalWord(0x106, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xfc), 0xd);
			if (this.processor.FlagSet.IsNotEqual) goto L5317;
			goto L5321;

		L5317:
			this.processor.CompareWord(this.processor.PullLocalWord(0xfc), 0x20);
			if (this.processor.FlagSet.IsEqual) goto L5321;
			goto L5324;

		L5321:
			goto L5740;

		L5324:
			this.processor.CompareWord(this.processor.PullLocalWord(0xfc), 0x1b);
			if (this.processor.FlagSet.IsEqual) goto L532e;
			goto L5230;

		L532e:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x31);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5338;
			goto L53c8;

		L5338:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x39);
			if (this.processor.FlagSet.IsLessOrEqual) goto L5342;
			goto L53c8;

		L5342:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xf0);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x31);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L5359;
			goto L53c5;

		L5359:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0x5);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L536b;
			goto L53ad;

		L536b:
			ObtainCitizenType(this.processor.PullLocalWord(0xea));

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x3);
			if (this.processor.FlagSet.IsLess) goto L5385;
			goto L539a;

		L5385:
			AssignCitizenType(this.processor.PullLocalWord(0xea),
				(ushort)(this.processor.PullLocalWord(0xe8) + 1));

			goto L53aa;

		L539a:
			AssignCitizenType(this.processor.PullLocalWord(0xea), 1);

		L53aa:
			goto L53b5;

		L53ad:
			this.owner.Libc.CopyText(0xba06, "A City must have five\npopulation units to support\ntaxmen or scientists.\n");
			this.owner.Dialogs.DisplayMenu(0xba06, 32, 32, 1);

			goto L045f;

		L53b5:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			goto L12c2;

		L53c5:
			goto L045f;

		L53c8:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x70);
			if (this.processor.FlagSet.IsEqual) goto L53d2;
			goto L563f;

		L53d2:
			this.processor.CompareWord(this.processor.PullLocalWord(0xb8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L53dc;
			goto L53ed;

		L53dc:
			this.processor.RegAcc.Value16 = 0x0;
			this.processor.EmitLocalWord(0xbe, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xb6, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xb8, 0x1);

		L53ed:
			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0xaa),
				(ushort)((this.processor.PullLocalWord(0xb6) << 4) + 0xa0),
				(ushort)((this.processor.PullLocalWord(0xbe) << 4) + 0x38),
				16, 16,
				this.processor.PullGlobalWord(0x19d4),
				0, 0);

			this.owner.Dialogs.RenderBox(
				this.processor.PullLocalSignedWord(0xb6) * 16 + 160,
				this.processor.PullLocalSignedWord(0xbe) * 16 + 56,
				15, 15, 15);

			this.owner.TextPainter.AwaitTime(10);

			this.owner.VideoCard.RenderImage(
				this.processor.PullGlobalWord(0x19d4),
				0, 0, 16, 16,
				this.processor.PullGlobalWord(0xaa),
				(ushort)((this.processor.PullLocalWord(0xb6) << 4) + 0xa0),
				(ushort)((this.processor.PullLocalWord(0xbe) << 4) + 0x38));

			this.owner.TextPainter.AwaitTime(10);

			this.processor.EmitGlobalWord(0x6b64, 0x1);

			this.owner.Libc.KeyAvailable();

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L54b4;
			goto L53ed;

		L54b4:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xb6);
			this.processor.EmitLocalWord(0xf6, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0xbe, 0xfc);

			this.owner.Dialogs.ObtainNavigationKey();

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			goto L551f;

		L54d4:
			this.processor.EmitLocalWord(0xbe, this.processor.DecrementWord(this.processor.PullLocalWord(0xbe)));
			goto L556a;

		L54db:
			this.processor.EmitLocalWord(0xbe, this.processor.DecrementWord(this.processor.PullLocalWord(0xbe)));
			this.processor.IncrementLocalWord(0xb6);
			goto L556a;

		L54e6:
			this.processor.EmitLocalWord(0xb6, this.processor.IncrementWord(this.processor.PullLocalWord(0xb6)));
			goto L556a;

		L54ed:
			this.processor.EmitLocalWord(0xb6, this.processor.IncrementWord(this.processor.PullLocalWord(0xb6)));
			this.processor.IncrementLocalWord(0xbe);
			goto L556a;

		L54f8:
			this.processor.EmitLocalWord(0xbe, this.processor.IncrementWord(this.processor.PullLocalWord(0xbe)));
			goto L556a;

		L54ff:
			this.processor.EmitLocalWord(0xb6, this.processor.DecrementWord(this.processor.PullLocalWord(0xb6)));
			this.processor.IncrementLocalWord(0xbe);
			goto L556a;

		L550a:
			this.processor.EmitLocalWord(0xb6, this.processor.DecrementWord(this.processor.PullLocalWord(0xb6)));
			goto L556a;

		L5511:
			this.processor.EmitLocalWord(0xb6, this.processor.DecrementWord(this.processor.PullLocalWord(0xb6)));
			this.processor.DecrementLocalWord(0xbe);
			goto L556a;

		L551f:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4b00);
			if (this.processor.FlagSet.IsNotEqual) goto L5527;
			goto L550a;

		L5527:
			if (this.processor.FlagSet.IsLessOrEqual) goto L552c;
			goto L5547;

		L552c:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4700);
			if (this.processor.FlagSet.IsNotEqual) goto L5534;
			goto L5511;

		L5534:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4800);
			if (this.processor.FlagSet.IsNotEqual) goto L553c;
			goto L54d4;

		L553c:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4900);
			if (this.processor.FlagSet.IsNotEqual) goto L5544;
			goto L54db;

		L5544:
			goto L556a;

		L5547:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4d00);
			if (this.processor.FlagSet.IsNotEqual) goto L554f;
			goto L54e6;

		L554f:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4f00);
			if (this.processor.FlagSet.IsNotEqual) goto L5557;
			goto L54ff;

		L5557:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5000);
			if (this.processor.FlagSet.IsNotEqual) goto L555f;
			goto L54f8;

		L555f:
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x5100);
			if (this.processor.FlagSet.IsNotEqual) goto L5567;
			goto L54ed;

		L5567:

		L556a:
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0xb6));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsLessOrEqual) goto L557e;
			goto L55f5;

		L557e:
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0xbe));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x2);
			if (this.processor.FlagSet.IsLessOrEqual) goto L5592;
			goto L55f5;

		L5592:
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0xbe));

			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.owner.Libc.AbsoluteValue((short)this.processor.PullLocalWord(0xb6));

			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegSource.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x4);
			if (this.processor.FlagSet.IsLess) goto L55b6;
			goto L55f5;

		L55b6:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			this.processor.RegAcc.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.X);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xb6));

			this.processor.RegBase.Value16 = (ushort)((short)this.owner.World.Towns[townIdx].Place.Y);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.PullLocalWord(0xbe));

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.RegAcc.Value16, this.processor.RegBase.Value16];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = (byte)(this.owner.World.PersonParticipantIdx & 0xff);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsEqual) goto L55f5;
			goto L5605;

		L55f5:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xf6);
			this.processor.EmitLocalWord(0xb6, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0xfc, 0xbe);

		L5605:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0xd);
			if (this.processor.FlagSet.IsNotEqual) goto L560f;
			goto L5619;

		L560f:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x20);
			if (this.processor.FlagSet.IsEqual) goto L5619;
			goto L562c;

		L5619:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xb6);
			this.processor.EmitLocalWord(0xf6, this.processor.RegAcc.Value16);
			this.processor.CopyLocalWord(0xbe, 0xfc);
			goto L5aa6;

		L562c:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x1b);
			if (this.processor.FlagSet.IsEqual) goto L5636;
			goto L53ed;

		L5636:
			this.processor.EmitLocalWord(0xb8, 0x0);
			goto L045f;

		L563f:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x4d);
			if (this.processor.FlagSet.IsEqual) goto L5649;
			goto L5671;

		L5649:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0xca), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L5653;
			goto L5671;

		L5653:
			this.processor.EmitLocalWord(0xca, this.processor.BitXorWord(this.processor.PullLocalWord(0xca), 0x1));
			this.processor.CopyLocalWord(0xfe, 0xda);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			goto L12c2;

		L5671:
			this.processor.CompareWord(this.processor.PullLocalWord(0xf0), 0x72);
			if (this.processor.FlagSet.IsNotEqual) goto L567b;
			goto L56a6;

		L567b:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L5685;
			goto L56c8;

		L5685:
			this.processor.CompareWord(this.owner.GlobDb3c, 0xe7);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5690;
			goto L56c8;

		L5690:
			this.processor.CompareWord(this.owner.GlobDb3e, 0xbe);
			if (this.processor.FlagSet.IsGreater) goto L569b;
			goto L56c8;

		L569b:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x10e);
			if (this.processor.FlagSet.IsLess) goto L56a6;
			goto L56c8;

		L56a6:
			this.owner.TurnLogic.AwaitForKey();

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);
			
			this.owner.CityNaming.DisplayCityLabelPrompt(townIdx);
			
			this.processor.EmitGlobalWord(0x6b64, 0x1);
			goto L045f;

		L56c8:
			this.processor.CompareWord(this.owner.GlobDb3a, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L56d2;
			goto L5cba;

		L56d2:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x120);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L56dd;
			goto L5719;

		L56dd:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x60);
			if (this.processor.FlagSet.IsLess) goto L56e7;
			goto L5719;

		L56e7:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x58);
			if (this.processor.FlagSet.IsGreater) goto L56f1;
			goto L5719;

		L56f1:
			this.processor.TestBitsByte(this.processor.PullLocalByte(0xca), 0x2);
			if (this.processor.FlagSet.IsNotEqual) goto L56fb;
			goto L5719;

		L56fb:
			this.processor.EmitLocalWord(0xca, this.processor.BitXorWord(this.processor.PullLocalWord(0xca), 0x1));
			this.processor.CopyLocalWord(0xfe, 0xda);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			goto L12c2;

		L5719:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x12c);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5724;
			goto L58ac;

		L5724:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x5e);
			if (this.processor.FlagSet.IsLess) goto L572e;
			goto L58ac;

		L572e:
			this.processor.RegAcc.Value16 = this.owner.GlobDb3e;
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = 0x6;
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x40));
			this.processor.EmitLocalWord(0x106, this.processor.RegAcc.Value16);

		L5740:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xda);
			this.processor.EmitLocalWord(0xbc, this.processor.RegAcc.Value16);
			goto L574f;

		L574b:
			this.processor.EmitLocalWord(0xbc, this.processor.IncrementWord(this.processor.PullLocalWord(0xbc)));

		L574f:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x18);
			if (this.processor.FlagSet.IsLess) goto L5759;
			goto L58ac;

		L5759:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xbc);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegCount.Value16 = this.processor.BitAndWord(this.processor.RegCount.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers0);
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers1);
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L5783;
			goto L58a9;

		L5783:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.DecrementLocalWord(0x106);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L5793;
			goto L58a9;

		L5793:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x80);
			if (this.processor.FlagSet.IsEqual) goto L57a5;
			goto L58a9;

		L57a5:
			this.owner.Libc.CopyText(0xba06, "Do you want to sell\nyour ");

			this.processor.ScaleIndex(0x1e, this.processor.PullLocalWord(0xbc));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xbb8);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, " for ");

			this.owner.Libc.AppendText(0xba06,
				this.owner.Libc.IntegerToText((short)(0xa * (short)this.processor.PullUInt16(this.processor.SegData.Value16,
					(ushort)((0x1e * this.processor.PullLocalWord(0xbc) + 0xbd0) & 0xffff))), 10));

			this.owner.Libc.AppendText(0xba06, "$?\n No.\n Yes.\n");

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

			this.owner.Startup.DisplayMouseCursor();

			this.owner.Dialogs.DisplayMenu(0xba06, 128, 80, 1);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L5844;
			goto L58a1;

		L5844:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xbc));
			this.processor.RegAcc.Value16 = 0xa;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbd0)));
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold += (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xbc);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			uint scratchUint = (uint)((long)this.owner.World.Towns[townIdx].StructureMarkers0 |
				((long)this.owner.World.Towns[townIdx].StructureMarkers1 << 16));
			uint scratch1 = (uint)((long)this.processor.RegCount.Value16 | ((long)this.processor.RegBase.Value16 << 16));
			scratchUint -= scratch1;
			this.owner.World.Towns[townIdx].StructureMarkers0 = (ushort)(scratchUint & 0xffff);
			this.owner.World.Towns[townIdx].StructureMarkers1 = (ushort)((scratchUint & 0xffff0000) >> 16);

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker |= 0x80;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xbc);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x8);
			if (this.processor.FlagSet.IsEqual) goto L589b;
			goto L58a1;

		L589b:
			this.processor.EmitGlobalWord(0x6b64, 0x1);

		L58a1:
			this.owner.Startup.ConcealMouseCursor();

			goto L045f;

		L58a9:
			goto L574b;

		L58ac:
			this.processor.CompareWord(this.owner.GlobDb3c, 0xc8);
			if (this.processor.FlagSet.IsLess) goto L58b7;
			goto L596b;

		L58b7:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x14);
			if (this.processor.FlagSet.IsLess) goto L58c1;
			goto L596b;

		L58c1:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0x5);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L58d3;
			goto L5953;

		L58d3:
			this.processor.RegAcc.Value16 = this.owner.GlobDb3c;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x10);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0xf4);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0x50));
			this.processor.EmitLocalWord(0xea, this.processor.SubtractWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16));
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5902;
			goto L590e;

		L5902:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x8);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L590e;
			goto L5911;

		L590e:
			goto L045f;

		L5911:
			ObtainCitizenType(this.processor.PullLocalWord(0xea));

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x3);
			if (this.processor.FlagSet.IsLess) goto L592b;
			goto L5940;

		L592b:
			AssignCitizenType(this.processor.PullLocalWord(0xea),
				(ushort)(this.processor.PullLocalWord(0xe8) + 1));

			goto L5950;

		L5940:
			AssignCitizenType(this.processor.PullLocalWord(0xea), 1);

		L5950:
			goto L595b;

		L5953:
			this.owner.Libc.CopyText(0xba06, "A City must have five\npopulation units to support\ntaxmen or scientists.\n");
			this.owner.Dialogs.DisplayMenu(0xba06, 32, 32, 1);

			goto L045f;

		L595b:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			goto L12c2;

		L596b:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x64);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5975;
			goto L5a60;

		L5975:
			this.processor.CompareWord(this.owner.GlobDb3c, 0x2580);
			if (this.processor.FlagSet.IsLess) goto L5980;
			goto L5a60;

		L5980:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x74);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L598a;
			goto L5a60;

		L598a:
			this.processor.RegAcc.Value16 = this.owner.GlobDb3e;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x74);
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);
			this.processor.RegCount.Value16 = 0x6;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.owner.GlobDb3c;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x64);
			this.processor.RegBase.Value16 = this.processor.RegCount.Value16;
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);
			this.processor.RegBase.Value16 = this.processor.SumWord(this.processor.RegBase.Value16, this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0x106, this.processor.RegBase.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x4e);
			this.processor.CompareWord(this.processor.PullLocalWord(0x106), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsLess) goto L59cd;
			goto L5a60;

		L59cd:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74)));
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.TestBitsByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x9);
			if (this.processor.FlagSet.IsNotEqual) goto L59ee;
			goto L5a18;

		L59ee:
			this.processor.RegDest.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegDest.Value16 = this.processor.ShiftLeftWord(this.processor.RegDest.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x74)));
			this.processor.AddScaledToSource(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.RegAcc.LowByte = 0x22;
			this.processor.MultiplySignedByte(this.processor.RegAcc, this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d7)));
			this.processor.RegBase.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x3;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x113a)));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegSource.Value16 + 0x81d8), this.processor.RegAcc.LowByte);

		L5a18:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74)));
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4),
				this.processor.BitAndByte(this.processor.PullUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81d4)), 0x30));
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0x106);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			this.processor.RegAcc.Value16 = 0xc;
			this.processor.ScaleAccumulatorToSource(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegSource.Value16 - 0x74)));
			this.processor.ScaleIndexToBase(0x600, this.processor.PullGlobalWord(0x6548));
			this.processor.EmitUInt8(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + this.processor.RegSource.Value16 + 0x81da), 0xff);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			goto L12c2;

		L5a60:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x18);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5a6a;
			goto L5cba;

		L5a6a:
			this.processor.CompareWord(this.owner.GlobDb3e, 0x68);
			if (this.processor.FlagSet.IsLess) goto L5a74;
			goto L5cba;

		L5a74:
			this.processor.RegAcc.Value16 = this.owner.GlobDb3c;
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0xa);
			this.processor.EmitLocalWord(0xf6, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.owner.GlobDb3e;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x18);
			this.processor.DivideAccumulatorByPowerOfTwo(0x4);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.DecrementWord(this.processor.RegAcc.Value16);
			this.processor.EmitLocalWord(0xfc, this.processor.RegAcc.Value16);

		L5aa6:
			ObtainCityTileIndex(this.processor.PullLocalSignedWord(0xf6),
				this.processor.PullLocalSignedWord(0xfc));

			this.processor.EmitLocalWord(0xea, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0xffff);
			if (this.processor.FlagSet.IsNotEqual) goto L5ac4;
			goto L5cb7;

		L5ac4:
			if (this.processor.PullLocalWord(0xea) == 20)
			{
				// A click on the city square in the middle of the map: all citizens are taken off their tiles
				// (also entertainers, taxmen and scientists) and the game's own assignment puts them back on the best tiles,
				// first enough food so the city doesn't starve, then the best mix of food, shields and trade.
				this.owner.World.Towns[townIdx].LaborerMarkers0 = 0;
				this.owner.World.Towns[townIdx].LaborerMarkers1 = 0;
				this.processor.EmitGlobalWord(0xe8b8, 0);
				goto L045f;
			}

			this.processor.CompareWord(this.processor.PullLocalWord(0xea), 0x14);
			if (this.processor.FlagSet.IsLess) goto L5ace;
			goto L5cb7;

		L5ace:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.ScaleIndex(0x1c, (ushort)townIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.World.Towns[townIdx].Place.X +
				this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].X));

			this.processor.RegBase.Value16 = (ushort)((short)(this.owner.World.Towns[townIdx].Place.Y +
				this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].Y));

			this.processor.RegAcc.LowByte = (byte)this.owner.World.ChartSight[this.processor.RegAcc.Value16, this.processor.RegBase.Value16];
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegData.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegData.Value16 = this.processor.ShiftLeftWord(this.processor.RegData.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L5b13;
			goto L5cb7;

		L5b13:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x10a);
			this.processor.EmitGlobalWord(0x70dc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xa);
			this.processor.EmitGlobalWord(0x70de, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xea);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegCount.Value16 &= this.owner.World.Towns[townIdx].LaborerMarkers0;
			this.processor.RegBase.Value16 &= this.owner.World.Towns[townIdx].LaborerMarkers1;
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsEqual) goto L5b4a;
			goto L5b6e;

		L5b4a:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.RegDest.Value16 = (ushort)((short)((10 * this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].X) +
				(this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].Y * 2)));

			this.processor.CompareWord(this.processor.PullUInt16(this.processor.SegStack.Value16, (ushort)(this.processor.RegFrame.Value16 + this.processor.RegDest.Value16 - 0x8e)), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L5b6b;
			goto L5b6e;

		L5b6b:
			goto L12c2;

		L5b6e:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xea);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.owner.World.Towns[townIdx].LaborerMarkers0 ^= this.processor.RegCount.Value16;
			this.owner.World.Towns[townIdx].LaborerMarkers1 ^= this.processor.RegBase.Value16;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xea);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegCount.Value16 &= this.owner.World.Towns[townIdx].LaborerMarkers0;
			this.processor.RegBase.Value16 &= this.owner.World.Towns[townIdx].LaborerMarkers1;
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L5bbb;
			goto L5be5;

		L5bbb:
			this.processor.EmitLocalWord(0xae, this.processor.IncrementWord(this.processor.PullLocalWord(0xae)));

			RenderCityTile(townIdx, this.processor.PullLocalWord(0xea), marker);

			this.processor.CompareWord(this.processor.PullGlobalWord(0xe8b8), 0x0);
			if (this.processor.FlagSet.IsNotEqual)
			{
				this.processor.EmitGlobalWord(0xe8b8, this.processor.DecrementWord(this.processor.PullGlobalWord(0xe8b8)));
				this.processor.DecrementLocalWord(0x50);
			}
			goto L5c78;

		L5be5:
			this.processor.EmitLocalWord(0xae, this.processor.DecrementWord(this.processor.PullLocalWord(0xae)));
			this.processor.EmitLocalWord(0xbc, 0x0);
			goto L5bf6;

		L5bf2:
			this.processor.EmitLocalWord(0xbc, this.processor.IncrementWord(this.processor.PullLocalWord(0xbc)));

		L5bf6:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x3);
			if (this.processor.FlagSet.IsLess) goto L5c00;
			goto L5c45;

		L5c00:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.ScaleIndex(0x1c, (ushort)townIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			MayCityWorkTile(
				this.owner.World.Towns[townIdx].Place.X +
					this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].X,
				this.owner.World.Towns[townIdx].Place.Y +
					this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].Y,
				this.processor.PullLocalWord(0xbc));

			this.processor.EmitLocalWord(0xcc, this.processor.RegAcc.Value16);
			this.processor.RegBase.Value16 = this.processor.PullLocalWord(0xbc);
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xcc);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x70da), 
				this.processor.SubtractWord(this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x70da)), this.processor.RegAcc.Value16));
			goto L5bf2;

		L5c45:
			this.processor.RegSource.Value16 = this.processor.PullLocalWord(0xea);
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.ScaleIndex(0x1c, (ushort)townIdx);
			this.processor.RegDest.Value16 = this.processor.RegAcc.Value16;

			this.owner.MapOps.RenderMapTile(
				this.owner.World.Towns[townIdx].Place.X +
					this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].X,
				this.owner.World.Towns[townIdx].Place.Y +
					this.owner.TownDeltas[this.processor.PullLocalWord(0xea)].Y);

			this.processor.EmitGlobalWord(0xe8b8, this.processor.IncrementWord(this.processor.PullGlobalWord(0xe8b8)));
			this.processor.IncrementLocalWord(0x50);

		L5c78:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x50);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.LowByte = 0x1a;
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.owner.World.Towns[townIdx].LaborerMarkers0;
			this.processor.RegBase.Value16 = this.owner.World.Towns[townIdx].LaborerMarkers1;
			this.processor.RegCount.Value16 = this.processor.BitAndWord(this.processor.RegCount.Value16, 0xffff);
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16, 0x3ff);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.RegCount.Value16);
			this.processor.RegData.Value16 = this.processor.SumCarryWord(this.processor.RegData.Value16, this.processor.RegBase.Value16);
			this.owner.World.Towns[townIdx].LaborerMarkers0 = this.processor.RegAcc.Value16;
			this.owner.World.Towns[townIdx].LaborerMarkers1 = this.processor.RegData.Value16;
			this.processor.CompareWord(this.processor.PullLocalWord(0xae), 0x0);
			if (this.processor.FlagSet.IsGreater) goto L5cae;
			goto L5cb4;

		L5cae:
			goto L045f;

		L5cb4:
			goto L12c2;

		L5cb7:
			goto L045f;

		L5cba:
			goto L5cc2;

		L5cbd:
			this.owner.Conquest.AwaitForKeyPressOrMouseClick();

		L5cc2:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xc2);
			this.processor.EmitGlobalWord(0xd4cc, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xd0);
			this.processor.EmitGlobalWord(0xd75e, this.processor.RegAcc.Value16);

		L5cd0:
			this.processor.RegAcc.Value16 = (ushort)marker;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegData.Value16 = this.processor.BitOrWord(this.processor.RegData.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L5cdb;
			goto L68cc;

		L5cdb:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L5ce5;
			goto L62d8;

		L5ce5:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e2);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x0);
			if (this.processor.FlagSet.IsLess) goto L5cfa;
			goto L5f3f;

		L5cfa:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L5d0c;
			goto L5df3;

		L5d0c:
			this.owner.Libc.CopyText(0xba06, "Civil Disorder in\n");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "! Mayor\nflees in panic.\n");

			this.owner.Services.PerformSound(0x24, 0);

			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L5d4d;
			goto L5d9f;

		L5d4d:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L5d5f;
			goto L5d9f;

		L5d5f:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6b92), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L5d69;
			goto L5d9f;

		L5d69:
			this.owner.CityScene.DisplayCityScreen(townIdx, -2);
			
			this.owner.CityScene.RiotAnimation();

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.TurnHooks.RepaintMainScreen();

			this.processor.EmitGlobalWord(0x6b92, 0x1);
			goto L5db9;

		L5d9f:
			this.processor.EmitGlobalWord(0x2f9e, 0x4);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

		L5db9:
			this.owner.Services.PerformSound(1, 0);

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L5dd7;
			goto L5df3;

		L5dd7:
			this.processor.EmitGlobalWord(0xb1e8, 0x1);
			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L5de7;
			goto L5df3;

		L5de7:
			this.owner.HelpSystem.DisplayHelpPopup(0x2708);

		L5df3:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L5e05;
			goto L5eb4;

		L5e05:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker |= 1;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsEqual) goto L5e24;
			goto L5ea1;

		L5e24:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5e36;
			goto L5e54;

		L5e36:			
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));
			
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]--;

		L5e54:
			this.owner.Advisors.PickCityProduction(this.processor.PullGlobalSignedWord(0x6548), townIdx);
			
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].PresentMakingIdx = (sbyte)this.processor.RegCount.LowByte;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5e83;
			goto L5ea1;

		L5e83:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);

			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));
			
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]++;

		L5ea1:
			this.owner.TurnLogic.RepaintTileIfVisible(this.processor.PullLocalWord(0xd8),
				this.processor.PullLocalWord(0xe4));

			goto L5f3c;

		L5eb4:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind == 5) goto L5ec4;
			goto L5f3c;

		L5ec4:
			this.owner.Services.PerformSound(0x24, 0);

			this.owner.Libc.CopyText(0xba06, "Discontented citizens of\n");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, " revolt:\nGovernment Collapses!\n");

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L5f07;
			goto L5f20;

		L5f07:
			this.owner.Paper.DisplayNewspaper(townIdx);

			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0x6548);
			this.processor.RegCount.LowByte = 0x4;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, this.processor.RegCount.LowByte);

			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Relations[0] |= 4;

		L5f20:
			this.owner.Politics.SwitchGovernment(this.processor.PullGlobalSignedWord(0x6548), 0);
			
			this.owner.Services.PerformSound(1, 0);

		L5f3c:
			goto L606c;

		L5f3f:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5f49;
			goto L604f;

		L5f49:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L5f5b;
			goto L604f;

		L5f5b:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker &= 0xfe;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L5f7a;
			goto L5fc2;

		L5f7a:
			this.processor.EmitGlobalWord(0x2f9e, 0x4);

			this.owner.Libc.CopyText(0xba06, "Order restored\nin ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			goto L603f;

		L5fc2:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L5fd4;
			goto L5ff2;

		L5fd4:
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));
			
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]--;

		L5ff2:
			this.owner.Advisors.PickCityProduction(this.processor.PullGlobalSignedWord(0x6548), townIdx);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].PresentMakingIdx = (sbyte)this.processor.RegCount.LowByte;

			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].PresentMakingIdx, 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L6021;
			goto L603f;

		L6021:			
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].PresentMakingIdx;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegSource.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegSource.Value16 = this.processor.ShiftLeftWord(this.processor.RegSource.Value16, 0x1);
			
			this.processor.ScaleIndexToBase(0x38, this.processor.PullGlobalWord(0x6548));

			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].TroopsInsideMaking[this.owner.World.Towns[townIdx].PresentMakingIdx]++;

		L603f:
			this.owner.TurnLogic.RepaintTileIfVisible(this.processor.PullLocalWord(0xd8),
				this.processor.PullLocalWord(0xe4));

		L604f:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind != 0) goto L605f;
			goto L606c;

		L605f:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xe17a);
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold += (short)this.processor.RegAcc.Value16;

		L606c:
			this.processor.CompareWord(this.processor.PullGlobalWord(0x70e4), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L6076;
			goto L6213;

		L6076:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0x2);
			if (this.processor.FlagSet.IsGreater) goto L6088;
			goto L6213;

		L6088:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.IncrementWord(this.processor.RegAcc.Value16);
			this.processor.HalveAccumulator();
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e2));
			if (this.processor.FlagSet.IsLessOrEqual) goto L60a4;
			goto L6213;

		L60a4:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind != 0) goto L60b4;
			goto L6213;

		L60b4:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x40);
			if (this.processor.FlagSet.IsEqual) goto L60c6;
			goto L61ac;

		L60c6:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L60d8;
			goto L619c;

		L60d8:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind != 0) goto L60e8;
			goto L619c;

		L60e8:
			this.owner.Libc.CopyText(0xba06, "'We love the ");

			this.processor.RegBase.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x19b2)));

			this.owner.Libc.AppendText(0xba06, "'\nday celebrated in\n");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "!\n");

			this.owner.Services.PerformSound(0x22, 0);

			this.processor.TestBitsByte((byte)(this.owner.World.SessionPrefMarkers & 0xff), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L6155;
			goto L6185;

		L6155:
			this.owner.CityScene.DisplayCityScreen(townIdx, -2);
			
			this.owner.CityScene.RenderCitySceneryForHuman();

			this.owner.Startup.QueryForDisk(0, 1);

			this.owner.TurnHooks.RepaintMainScreen();

			goto L6190;

		L6185:
			this.owner.Paper.DisplayNewspaper(townIdx);

		L6190:
			this.owner.Services.PerformSound(1, 0);

		L619c:
			this.owner.World.Towns[townIdx].ConditionMarker |= 0x40;
			goto L6210;

		L61ac:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind >= 4) goto L61bc;
			goto L6210;

		L61bc:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0x48);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xe3c6));
			this.processor.RegCount.Value16 = this.processor.SumWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.PullGlobalWord(0x70da));
			if (this.processor.FlagSet.IsLess) goto L61df;
			goto L6210;

		L61df:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.CompareByte((byte)this.owner.World.Towns[townIdx].TrueExtent, 0xa);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L61f1;
			goto L6204;

		L61f1:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers0, 0x100);
			if (this.processor.FlagSet.IsNotEqual) goto L6204;
			goto L6210;

		L6204:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].TrueExtent++;

		L6210:
			goto L62d8;

		L6213:
			this.processor.CompareWord(this.processor.PullLocalWord(0xe8), 0x0);
			if (this.processor.FlagSet.IsGreaterOrEqual) goto L621d;
			goto L62d8;

		L621d:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x40);
			if (this.processor.FlagSet.IsNotEqual) goto L622f;
			goto L62d8;

		L622f:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegCount.LowByte = this.processor.PullGlobalByte(0x6548);
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, this.processor.RegCount.LowByte);
			this.processor.TestBitsWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.ParticipantMarkers);
			if (this.processor.FlagSet.IsNotEqual) goto L6241;
			goto L62cb;

		L6241:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind != 0) goto L6251;
			goto L62cb;

		L6251:
			this.owner.Services.PerformSound(0x23, 0);

			this.owner.Libc.CopyText(0xba06, "'We love the ");

			this.processor.RegBase.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind;
			this.processor.RegBase.Value16 = this.processor.ShiftLeftWord(this.processor.RegBase.Value16, 0x1);
			this.owner.Libc.AppendText(0xba06, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x19b2)));

			this.owner.Libc.AppendText(0xba06, "'\ncelebration canceled\nin ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.owner.Paper.DisplayNewspaper(townIdx);
			
			this.owner.Services.PerformSound(1, 0);

		L62cb:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.owner.World.Towns[townIdx].ConditionMarker &= 0xbf;

		L62d8:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e6);
			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L62eb;
			goto L635d;

		L62eb:
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx != -1) goto L62f5;
			goto L630d;

		L62f5:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548),
				this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual)
				goto L630d;

			goto L6327;

		L630d:
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx == -1) goto L6317;
			goto L635d;

		L6317:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].InquiryHeadway != 0) goto L6327;
			goto L635d;

		L6327:
			this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx = -1;
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L6337;
			goto L6351;

		L6337:
			this.owner.Startup.DisplayMouseCursorFully();
			
			this.owner.Advisors.UncoverTechnology(this.processor.PullGlobalSignedWord(0x6548), 0);
			
			this.owner.Startup.ConcealMouseCursorToSavedLevel();

		L6351:
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].InquiryHeadway = 0;

		L635d:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind == 0)
			{
				this.processor.EmitLocalWord(0xe8, 0x0);
			}

			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].InquiryHeadway += (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L638d;
			goto L63ba;

		L638d:
			if (this.owner.World.HardnessTier == 0) goto L6397;
			goto L63ba;

		L6397:
			if (this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx >= 0 &&
				(this.owner.World.ScienceLeadingFoundVia[this.owner.World.Participants[this.owner.World.PersonParticipantIdx].PresentInquiryIdx] & 7) != 0)
			{
				this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].InquiryHeadway +=
					this.processor.PullLocalSignedWord(0xe8);
			}

		L63ba:
			PlayerEnjoysWonderEffect(this.processor.PullGlobalSignedWord(0x6548), 0x14);
			
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual) goto L63d2;
			goto L63e5;

		L63d2:
			this.processor.RegAcc.Value16 = this.processor.PullLocalWord(0xe8);
			this.processor.HalveAccumulator();
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].InquiryHeadway += (short)this.processor.RegAcc.Value16;

		L63e5:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L63f1;
			goto L63fb;

		L63f1:
			this.processor.RegAcc.Value16 = 0xe;
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, (ushort)this.owner.World.HardnessTier);
			goto L6403;

		L63fb:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.HardnessTier;
			this.processor.RegAcc.Value16 = this.processor.ShiftLeftWord(this.processor.RegAcc.Value16, 0x1);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0x6);

		L6403:
			this.processor.EmitLocalWord(0xf4, this.processor.RegAcc.Value16);
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0xd2de);
			this.processor.EmitLocalWord(0xf4, this.processor.SumWord(this.processor.PullLocalWord(0xf4), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L641a;
			goto L6441;

		L641a:
			this.processor.RegAcc.Value16 = 0xb;
			this.processor.RegAcc.Value16 -= (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].FoundScienceTally;
			this.processor.CompareWord(this.processor.RegAcc.Value16, this.processor.PullLocalWord(0xf4));
			if (this.processor.FlagSet.IsGreater) goto L6430;
			goto L6441;

		L6430:
			this.processor.RegAcc.Value16 = 0xb;
			this.processor.RegAcc.Value16 -= (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].FoundScienceTally;
			this.processor.EmitLocalWord(0xf4, this.processor.RegAcc.Value16);

		L6441:
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L644d;
			goto L6457;

		L644d:
			this.processor.CompareWord(this.processor.PullGlobalWord(0xb1e8), 0x0);
			if (this.processor.FlagSet.IsEqual) goto L6457;
			goto L64b8;

		L6457:
			if (this.owner.World.Season < 0)
			{
				this.processor.RegCount.Value16 = 0x1;
			}
			else
			{
				this.processor.RegCount.Value16 = 0x2;
			}

			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].InquiryHeadway;
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].FoundScienceTally;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullLocalWord(0xf4));
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L6488;
			goto L64b8;

		L6488:
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].InquiryHeadway = 0;
			this.processor.TestBitsByte((byte)(this.owner.World.DiagMarkers & 0xff), 0x8);
			if (this.processor.FlagSet.IsNotEqual) goto L649e;
			goto L64b8;

		L649e:
			this.owner.Startup.DisplayMouseCursorFully();

			this.owner.Advisors.UncoverTechnology(this.processor.PullGlobalSignedWord(0x6548), 0);
			
			this.owner.Startup.ConcealMouseCursorToSavedLevel();

		L64b8:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e2));
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Tally += (short)this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.PersonParticipantIdx;
			this.processor.CompareWord(this.processor.PullGlobalWord(0x6548), this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsEqual) goto L64e3;
			goto L68cc;

		L64e3:
			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70dc);
			this.processor.SignExtendWord(this.processor.RegAcc, this.processor.RegData);
			this.processor.RegCount.Value16 = this.processor.PullGlobalWord(0x6c98);
			this.processor.DivideSignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.RegCount.Value16);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x14);
			this.processor.EmitLocalWord(0xe0, this.processor.RegAcc.Value16);
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.processor.RegAcc.LowByte = (byte)this.owner.World.Towns[townIdx].TrueExtent;
			this.processor.SignExtendByte(this.processor.RegAcc);
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullGlobalWord(0xb882));
			this.processor.DivideAccumulatorByPowerOfTwo(0x2);
			this.processor.EmitLocalWord(0xe0, this.processor.SumWord(this.processor.PullLocalWord(0xe0), this.processor.RegAcc.Value16));
			this.processor.RegAcc.Value16 = (ushort)this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].FoundScienceTally;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, (ushort)this.owner.World.HardnessTier);
			this.processor.HalveAccumulator();
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, 0x100);
			this.processor.RegAcc.Value16 = this.processor.NegateWord(this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(this.processor.RegAcc.Value16));

			this.processor.RegCount.Value16 = this.processor.PullLocalWord(0xe0);
			this.processor.RegCount.Value16 = this.processor.ShiftLeftWord(this.processor.RegCount.Value16, 0x1);
			this.processor.CompareWord(this.processor.RegCount.Value16, this.processor.RegAcc.Value16);
			if (this.processor.FlagSet.IsGreater) goto L6545;
			goto L6640;

		L6545:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(20));

			this.processor.EmitLocalWord(0xe8, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.TownDeltas[this.processor.PullLocalWord(0xe8)].X +
				this.processor.PullLocalSignedWord(0xd8)));
			this.processor.EmitLocalWord(0xc6, this.processor.RegAcc.Value16);

			this.processor.RegAcc.Value16 = (ushort)((short)(this.owner.TownDeltas[this.processor.PullLocalWord(0xe8)].Y +
				this.processor.PullLocalSignedWord(0xe4)));
			this.processor.EmitLocalWord(0xd2, this.processor.RegAcc.Value16);

			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x40);
			if (this.processor.FlagSet.IsEqual) goto L6590;
			goto L6640;

		L6590:
			this.owner.MapOps.ObtainTerrainType(
				this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0xa);
			if (this.processor.FlagSet.IsNotEqual) goto L65a8;
			goto L6640;

		L65a8:
			this.owner.MapOps.ObtainTileImprovements(
				this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2));

			this.processor.TestBitsByte(this.processor.RegAcc.LowByte, 0x1);
			if (this.processor.FlagSet.IsEqual) goto L65bf;
			goto L6640;

		L65bf:
			AttachSmog(this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2));

			this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0xc6) - 8,
				this.processor.PullLocalSignedWord(0xd2) - 6);

			this.owner.MapOps.RepaintMapTile(
				this.processor.PullLocalSignedWord(0xc6),
				this.processor.PullLocalSignedWord(0xd2));

			this.processor.EmitGlobalWord(0x2f9e, 0x6);

			this.owner.Libc.CopyText(0xba06, "Pollution near ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.owner.TurnHooks.DisplayDialog(0xba06, 80, 64);

		L6640:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x1);
			if (this.processor.FlagSet.IsNotEqual) goto L6652;
			goto L672d;

		L6652:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsWord(this.owner.World.Towns[townIdx].StructureMarkers1, 0x10);
			if (this.processor.FlagSet.IsNotEqual) goto L6665;
			goto L672d;

		L6665:
			this.processor.RegAcc.Value16 = (ushort)(this.owner.Libc.Dice.DrawInt(3));

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L6679;
			goto L672d;

		L6679:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x42);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual)
				goto L6691;

			goto L672d;

		L6691:
			this.owner.MapOps.FocusMapView(this.owner.World.PersonParticipantIdx,
				this.processor.PullLocalSignedWord(0xd8) - 8,
				this.processor.PullLocalSignedWord(0xe4) - 6);

			this.owner.Diplomats.RenderExplosion(
				this.processor.PullLocalSignedWord(0xd8),
				this.processor.PullLocalSignedWord(0xe4));

			this.owner.Combat.EnactNuclearFallout(
				this.processor.PullLocalSignedWord(0xd8),
				this.processor.PullLocalSignedWord(0xe4));
			
			this.processor.ScaleIndexToBase(0x1c, (ushort)townIdx);
			this.owner.World.Towns[townIdx].StructureMarkers0 &= 0xffff;
			this.owner.World.Towns[townIdx].StructureMarkers1 &= 0xffef;

			this.owner.Services.PerformSound(0x24, 0);

			this.owner.Libc.CopyText(0xba06, "Nuclear Catastrophe\nin ");

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "!\nContamination feared!\n");

			this.owner.Paper.DisplayNewspaper(townIdx);
			
			this.owner.Services.PerformSound(1, 0);

		L672d:
			this.processor.EmitLocalWord(0xbc, 0x1);
			goto L673a;

		L6736:
			this.processor.EmitLocalWord(0xbc, this.processor.IncrementWord(this.processor.PullLocalWord(0xbc)));

		L673a:
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x18);
			if (this.processor.FlagSet.IsLess) goto L6744;
			goto L68cc;

		L6744:
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xbc);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.processor.RegCount.Value16 = this.processor.BitAndWord(this.processor.RegCount.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers0);
			this.processor.RegBase.Value16 = this.processor.BitAndWord(this.processor.RegBase.Value16,
				this.owner.World.Towns[townIdx].StructureMarkers1);
			this.processor.RegBase.Value16 = this.processor.BitOrWord(this.processor.RegBase.Value16, this.processor.RegCount.Value16);
			if (this.processor.FlagSet.IsNotEqual) goto L676e;
			goto L68c9;

		L676e:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind != 0) goto L677e;
			goto L68c9;

		L677e:
			this.processor.RegAcc.Value16 = 0x1e;
			this.processor.ScaleAccumulatorToBase(this.processor.PullLocalWord(0xbc));
			this.processor.RegAcc.Value16 = this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbd2));
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold -= (short)this.processor.RegAcc.Value16;
			this.processor.CompareWord(this.processor.PullLocalWord(0xbc), 0x1);
			if (this.processor.FlagSet.IsEqual) goto L679f;
			goto L680b;

		L679f:
			if (this.owner.World.HardnessTier >= 2) goto L67a9;
			goto L67b3;

		L67a9:
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold--;

		L67b3:
			if (this.owner.World.HardnessTier >= 4) goto L67bd;
			goto L67c7;

		L67bd:
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold--;

		L67c7:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x22);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L67df;

			goto L67e9;

		L67df:
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold--;

		L67e9:
			this.processor.RegAcc.Value16 = this.owner.Advisors.PlayerKnowsTechnology(this.processor.PullGlobalSignedWord(0x6548), 0x25);
			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsNotEqual)
				goto L6801;

			goto L680b;

		L6801:
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold--;

		L680b:
			if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold < 0) goto L681b;
			goto L68c9;

		L681b:
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold = 0;
			this.processor.RegAcc.Value16 = 0x1;
			this.processor.RegData.Value16 = 0x0;
			this.processor.RegCount.LowByte = this.processor.PullLocalByte(0xbc);
			this.processor.SplitDoubleWord(this.processor.RegAcc, this.processor.RegData, this.processor.JoinDoubleWord(this.processor.RegAcc.Value16, this.processor.RegData.Value16) << this.processor.RegCount.LowByte);

			this.processor.RegAcc.Value16 = this.processor.InvertWord(this.processor.RegAcc.Value16);
			this.processor.RegData.Value16 = this.processor.InvertWord(this.processor.RegData.Value16);
			this.processor.RegCount.Value16 = this.processor.RegAcc.Value16;
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.RegBase.Value16 = this.processor.RegData.Value16;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);
			this.owner.World.Towns[townIdx].StructureMarkers0 &= this.processor.RegCount.Value16;
			this.owner.World.Towns[townIdx].StructureMarkers1 &= this.processor.RegBase.Value16;
			this.processor.EmitGlobalByte(0xba06, 0x0);

			this.owner.Conquest.ObtainCityLabel(townIdx);

			this.owner.Libc.AppendText(0xba06, "\ncan't maintain\n");

			this.processor.ScaleIndex(0x1e, this.processor.PullLocalWord(0xbc));
			this.processor.RegAcc.Value16 = this.processor.SumWord(this.processor.RegAcc.Value16, 0xbb8);
			this.owner.Libc.AppendText(0xba06, this.processor.RegAcc.Value16);

			this.owner.Libc.AppendText(0xba06, ".\n");

			this.processor.EmitGlobalWord(0x2f9e, 0x4);

			this.owner.TurnHooks.DisplayDialog(0xba06, 100, 80);

			this.processor.ScaleIndexToBase(0x1e, this.processor.PullLocalWord(0xbc));
			this.processor.RegAcc.Value16 = 0xa;
			this.processor.MultiplySignedWord(this.processor.RegAcc, this.processor.RegData, this.processor.PullUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0xbd0)));
			this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].Gold += (short)this.processor.RegAcc.Value16;

		L68c9:
			goto L6736;

		L68cc:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToBase((ushort)townIdx);
			this.processor.TestBitsByte(this.owner.World.Towns[townIdx].ConditionMarker, 0x4);
			if (this.processor.FlagSet.IsNotEqual) goto L68de;
			goto L690f;

		L68de:
			this.processor.RegAcc.Value16 = 0x1c;
			this.processor.ScaleAccumulatorToSource((ushort)townIdx);

			// ??? this playerID reference needs to be checked!
			this.owner.UnitOps.ContainsAdjacentForeignUnit(this.owner.World.Towns[townIdx].ParticipantIdx,
				this.owner.World.Towns[townIdx].Place.X,
				this.owner.World.Towns[townIdx].Place.Y);

			this.processor.CompareWord(this.processor.RegAcc.Value16, 0x0);
			if (this.processor.FlagSet.IsEqual) goto L6904;
			goto L690f;

		L6904:
			this.owner.UnitOps.KeepUnitsInCityList(townIdx);

		L690f:
			this.processor.RegBase.Value16 = this.processor.PullGlobalWord(0xaa);
			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.RegBase.Value16 + 0x10), 0x1);

			this.owner.Startup.DisplayMouseCursor();

			this.processor.RegAcc.Value16 = this.processor.PullGlobalWord(0x70e2);
			this.processor.RegAcc.Value16 = this.processor.SubtractWord(this.processor.RegAcc.Value16, this.processor.PullGlobalWord(0x70e4));

		L6927:
			this.processor.RegSource.Value16 = this.processor.UnstackWord();
			this.processor.RegDest.Value16 = this.processor.UnstackWord();
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("HandleCity");
		}

		/// <summary>
		/// A tile of the city (TownDeltas[townTile] from the city): with marker 1 draws it on the city screen with the icons of
		/// its food, shields and trade (MayCityWorkTile 0 - 2; closer together the more there are, a mark when there are none),
		/// else puts the city's player on the tile in map layer 2 (0x2494 set meanwhile). Either way the amounts are added to the
		/// totals at 0x70da.
		/// </summary>
		public void RenderCityTile(short townIdx, ushort townTile, short marker)
		{
			this.processor.Journal.OpenScope($"RenderCityTile({townIdx}, {townTile}, {marker})");

			short horizLoc = (short)(this.owner.World.Towns[townIdx].Place.X + this.owner.TownDeltas[townTile].X);
			short vertLoc = (short)(this.owner.World.Towns[townIdx].Place.Y + this.owner.TownDeltas[townTile].Y);
			bool shown = marker == 1;
			short total = 0;
			short spacing = 0;

			if (shown)
			{
				this.owner.MapOps.RenderMapTile(horizLoc, vertLoc);

				for (ushort resource = 0; resource < 3; resource++)
				{
					total += (short)MayCityWorkTile(horizLoc, vertLoc, resource);
				}

				spacing = (short)(total <= 4 ? 8 : total <= 6 ? 5 : 3);
			}
			else
			{
				this.processor.EmitGlobalWord(0x2494, 1);

				this.owner.Services.AssignDot(2, horizLoc + 0x50, vertLoc, (ushort)this.owner.World.Towns[townIdx].ParticipantIdx);
			}

			short iconHoriz = 0;
			short iconVert = 0;

			for (ushort resource = 0; resource < 3; resource++)
			{
				short amount = (short)MayCityWorkTile(horizLoc, vertLoc, resource);

				this.processor.EmitGlobalWord((ushort)((resource << 1) + 0x70da), (ushort)(this.processor.PullGlobalWord((ushort)((resource << 1) + 0x70da)) + amount));

				for (; amount > 0; amount--)
				{
					if (!shown)
						continue;

					this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa),
						161 + (this.owner.TownDeltas[townTile].X * 16) + iconHoriz, 57 + (this.owner.TownDeltas[townTile].Y * 16) + iconVert,
						this.processor.PullGlobalWord((ushort)(((resource + 8) << 1) + 0xd4ce)));

					if (iconHoriz < 8)
					{
						iconHoriz += spacing;
					}
					else
					{
						iconHoriz = 0;
						iconVert += 8;
					}
				}
			}

			if (shown && total == 0)
			{
				this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa),
					165 + (this.owner.TownDeltas[townTile].X * 16), 61 + (this.owner.TownDeltas[townTile].Y * 16),
					this.processor.PullGlobalWord((0xd << 1) + 0xd4ce));
			}

			this.processor.EmitGlobalWord(0x2494, 0);

			this.processor.Journal.CloseScope("RenderCityTile");
		}

		/// <summary>
		/// What the tile yields to the city 0x653e of player 0x6548: food (arg3 0), shields (1) or trade (2). The terrain's
		/// value (0x290, or 0x374 with the special resource) with irrigation (bit 2), mine (4) and road (8, trade on terrain 0 - 2)
		/// — the real improvements only with DiagMarkers bit 2, else irrigation (terrain 0 - 2) or mine and a road (not on terrain
		/// 1) assumed; no shields on every other tile of terrain 2 and 11; the Colossus, the railroad (bit 0x10: 1.5 times), -1 above
		/// 2 under governments 0 - 1 unless the city celebrates (0x40; 0xe3c2 -= 2 while 0x2494), +1 trade under government 2+
		/// celebrating or 4+, and pollution (bit 0x40) halves it. 0 outside the map, never negative.
		/// </summary>
		public ushort MayCityWorkTile(int horizLoc, int vertLoc, ushort arg3)
		{
			this.processor.Journal.OpenScope($"MayCityWorkTile({horizLoc}, {vertLoc}, {arg3})");

			short yield = 0;

			if (this.owner.MapOps.VerifyMapLimits(horizLoc, vertLoc) != 0)
			{
				ushort terrain = this.owner.MapOps.ObtainTerrainType(horizLoc, vertLoc);
				ushort terrainRecord = (ushort)((terrain * 0x13) + arg3);
				ushort yieldTable = (ushort)(this.owner.MapOps.ContainsSpecialResource(horizLoc, vertLoc) != 0 ? 0x374 : 0x290);

				yield = (sbyte)this.processor.PullGlobalByte((ushort)(terrainRecord + yieldTable));

				ushort improvements = this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc);

				if ((this.owner.World.DiagMarkers & 2) == 0)
				{
					improvements = (ushort)((short)terrain > 2 ? 4 : 2);

					if (terrain != 1)
					{
						improvements |= 8;
					}
				}

				if (terrain != MapRules.OceanTerrain)
				{
					// Terrain records of 0xc words at 0x44a: irrigation and mining bonus minus 1
					if (arg3 == 0 && (improvements & 2) != 0)
					{
						yield += (short)(0xffff - this.processor.PullGlobalWord((ushort)((terrain * 0xc) + 0x44a)));
					}

					if (arg3 == 1 && (improvements & 4) != 0)
					{
						yield += (short)(0xffff - this.processor.PullGlobalWord((ushort)((terrain * 0xc) + 0x44e)));
					}

					if (arg3 == 2 && (improvements & 8) != 0 && (short)terrain <= 2)
					{
						yield++;
					}
				}

				if (arg3 == 1 && (terrain == 2 || terrain == 0xb) && ((7 * horizLoc + 11 * vertLoc) & 2) != 0)
				{
					yield = 0;
				}

				ushort townIdx = this.processor.PullGlobalWord(0x653e);

				if (yield != 0 && arg3 == 2 && ObtainWonderCityID(3) == townIdx)
				{
					yield++;
				}

				if ((improvements & 0x10) != 0)
				{
					yield += (short)(yield / 2);
				}

				if (yield > 2 && (this.owner.World.Towns[townIdx].ConditionMarker & 0x40) == 0)
				{
					if (this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind <= 1)
					{
						yield--;
					}

					if (this.processor.PullGlobalWord(0x2494) != 0)
					{
						this.processor.EmitGlobalWord(0xe3c2, (ushort)(this.processor.PullGlobalWord(0xe3c2) - 2));
					}
				}

				if (yield != 0 && arg3 == 2)
				{
					if (this.processor.PullGlobalWord(0x2494) != 0)
					{
						this.processor.EmitGlobalWord(0xdb42, (ushort)(this.processor.PullGlobalWord(0xdb42) + 1));
					}

					short needed = (short)((this.owner.World.Towns[townIdx].ConditionMarker & 0x40) != 0 ? 2 : 4);

					if (needed <= this.owner.World.Participants[this.processor.PullGlobalWord(0x6548)].RegimeKind)
					{
						yield++;
					}
				}

				if ((improvements & 0x40) != 0)
				{
					yield = (short)((yield + 1) / 2);
				}

				if (yield < 0)
				{
					yield = 0;
				}
			}

			this.processor.RegAcc.Value16 = (ushort)yield;
			this.processor.Journal.CloseScope("MayCityWorkTile");

			return (ushort)yield;
		}

		/// <summary>
		/// Does the player have the effect of the wonder: it stands in a city of the player and is not obsolete
		/// </summary>
		/// <returns>1 or 0</returns>
		public ushort PlayerEnjoysWonderEffect(short participantIdx, short miracleIdent)
		{
			this.processor.Journal.OpenScope($"PlayerEnjoysWonderEffect({participantIdx}, {miracleIdent})");

			bool enjoys = false;

			if (!IsWonderObsolete(miracleIdent))
			{
				ushort townIdx = (ushort)this.owner.World.MarvelTownIdx[miracleIdent];

				enjoys = townIdx != 0xffff && this.owner.World.Towns[townIdx].ParticipantIdx == participantIdx;
			}

			this.processor.RegAcc.Value16 = (ushort)(enjoys ? 1 : 0);

			this.processor.Journal.CloseScope("PlayerEnjoysWonderEffect");

			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// The city of the wonder, also when the wonder is not finished (-1)
		/// </summary>
		/// <returns>City index, or 0xffff when the wonder is obsolete</returns>
		public ushort ObtainWonderCityID(short miracleIdent)
		{
			this.processor.Journal.OpenScope($"ObtainWonderCityID({miracleIdent})");

			ushort townIdx = IsWonderObsolete(miracleIdent) ? (ushort)0xffff : (ushort)this.owner.World.MarvelTownIdx[miracleIdent];
			this.processor.RegAcc.Value16 = townIdx;

			this.processor.Journal.CloseScope("ObtainWonderCityID");

			return townIdx;
		}

		/// <summary>
		/// A wonder has no effect any more when any civilization knows the technology that makes it obsolete
		/// (word table at 0x10fe of the data segment)
		/// </summary>
		private bool IsWonderObsolete(short miracleIdent)
		{
			short obsoletingTechnology = this.processor.PullInt16(this.processor.SegData.Value16, (ushort)(0x10fe + (miracleIdent << 1)));

			for (short participantIdx = 1; participantIdx < 8; participantIdx++)
			{
				if (this.owner.Advisors.PlayerKnowsTechnology(participantIdx, obsoletingTechnology) != 0)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Pollution (improvement bit 0x40) appears on the tile, unless it is there already; the human player sees it
		/// </summary>
		public void AttachSmog(int horizLoc, int vertLoc)
		{
			this.processor.Journal.OpenScope($"AttachSmog({horizLoc}, {vertLoc})");

			if ((this.owner.MapOps.ObtainTileImprovements(horizLoc, vertLoc) & 0x40) == 0)
			{
				this.owner.MapOps.AssignTileImprovements(0x40, horizLoc, vertLoc);

				this.owner.MapOps.KeepKnownTileImprovements(horizLoc, vertLoc);

				this.owner.World.DirtyCellTally++;
			}

			this.processor.Journal.CloseScope("AttachSmog");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <param name="param2"></param>
		public void AssignCitizenType(ushort arg1, ushort arg2)
		{
			this.processor.Journal.OpenScope($"AssignCitizenType({arg1}, {arg2})");

			short cityIdx = (short)this.processor.PullGlobalWord(0x653e);
			ushort currentMarkers = this.owner.World.Towns[cityIdx].LaborerMarkers2;
			ushort updated = CitizenSpecialistRules.SetSpecialistType(currentMarkers, arg1, (CitizenSpecialistRules.SpecialistType)arg2);
			this.owner.World.Towns[cityIdx].LaborerMarkers2 = updated;

			this.processor.Journal.CloseScope("AssignCitizenType");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <returns></returns>
		public ushort ObtainCitizenType(ushort arg1)
		{
			this.processor.Journal.OpenScope($"ObtainCitizenType({arg1})");

			short cityIdx = (short)this.processor.PullGlobalWord(0x653e);
			ushort markers = this.owner.World.Towns[cityIdx].LaborerMarkers2;
			var specType = CitizenSpecialistRules.GetSpecialistType(markers, arg1);
			this.processor.RegAcc.Value16 = (ushort)specType;

			this.processor.Journal.CloseScope("ObtainCitizenType");
			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="param1"></param>
		/// <returns></returns>
		public ushort TallySpecialists(ushort arg1)
		{
			this.processor.Journal.OpenScope($"TallySpecialists({arg1})");

			short cityIdx = (short)this.processor.PullGlobalWord(0x653e);
			ushort markers = this.owner.World.Towns[cityIdx].LaborerMarkers2;
			int tally = CitizenSpecialistRules.CountSpecialists(markers, (CitizenSpecialistRules.SpecialistType)arg1);
			this.processor.RegAcc.Value16 = (ushort)tally;

			this.processor.Journal.CloseScope("TallySpecialists");
			return this.processor.RegAcc.Value16;
		}

		/// <summary>
		/// Keeps the specialists of the city in range: entertainers (0x70e2) and taxmen (0x70e4) are clamped to the city size,
		/// scientists (0x6542) become taxmen while there are fewer taxmen than scientists, then specialists are given up
		/// (scientists first, then entertainers, and a taxman each time) until all of them fit in the size less the workers
		/// (clamped to 0 - 99)
		/// </summary>
		public void ReshuffleCitizens(short townIdx, ushort workerTally)
		{
			this.processor.Journal.OpenScope($"ReshuffleCitizens({townIdx}, {workerTally})");

			City town = this.owner.World.Towns[townIdx];

			this.processor.EmitGlobalWord(0x70e2,
				(ushort)this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0x70e2), 0, town.TrueExtent));

			while (this.processor.PullGlobalWord(0x6542) != 0 &&
				this.processor.PullGlobalSignedWord(0x70e4) < this.processor.PullGlobalSignedWord(0x6542))
			{
				this.processor.EmitGlobalWord(0x6542, (ushort)(this.processor.PullGlobalWord(0x6542) - 1));
				this.processor.EmitGlobalWord(0x70e4, (ushort)(this.processor.PullGlobalWord(0x70e4) + 1));
			}

			ushort taxmen = (ushort)this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0x70e4), 0, town.TrueExtent);

			while (true)
			{
				this.processor.EmitGlobalWord(0x70e4, taxmen);

				short room = (short)this.owner.Helpers.VerifyValueSpan(town.TrueExtent - (short)workerTally, 0, 99);

				if ((short)(this.processor.PullGlobalWord(0x70e2) + this.processor.PullGlobalWord(0x70e4)) <= room)
					break;

				if (this.processor.PullGlobalWord(0x6542) != 0)
				{
					this.processor.EmitGlobalWord(0x6542, (ushort)(this.processor.PullGlobalWord(0x6542) - 1));
				}
				else
				{
					this.processor.EmitGlobalWord(0x70e2, (ushort)(this.processor.PullGlobalWord(0x70e2) - 1));
					this.processor.EmitGlobalWord(0x70e2,
						(ushort)this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0x70e2), 0, town.TrueExtent));
				}

				this.processor.EmitGlobalWord(0x70e4, (ushort)(this.processor.PullGlobalWord(0x70e4) - 1));
				taxmen = (ushort)this.owner.Helpers.VerifyValueSpan(this.processor.PullGlobalSignedWord(0x70e4), 0, town.TrueExtent);
			}

			this.processor.Journal.CloseScope("ReshuffleCitizens");
		}

		/// <summary>
		/// The citizens of the city from (horizLoc, vertLoc): the happy (0x70e2), the content, the unhappy (0x70e4, the first
		/// 0x6542 of them marked) and the arg4 specialists (ObtainCitizenType), 7 pixels apart or narrower to fit in arg5, with
		/// gaps between the groups.
		/// </summary>
		/// <returns>The distance of the icons, also in AX</returns>
		public ushort RenderCitizenIcons(short townIdx, short horizLoc, short vertLoc, ushort arg4, ushort arg5)
		{
			this.processor.Journal.OpenScope($"RenderCitizenIcons({townIdx}, {horizLoc}, {vertLoc}, {arg4}, {arg5})");

			// The gap after the happy citizens depends on the frame word 0x2, which is never written (the leftovers of the
			// functions called before at this depth), like in the original; the final values of the locals stay in the frame
			this.processor.EnterFrame(0xa);

			sbyte size = this.owner.World.Towns[townIdx].TrueExtent;

			this.processor.EmitLocalByte(0xa, (byte)size);

			ushort step = (short)(7 * size) > (short)arg5 ? (ushort)CPU.SignedQuotient((short)arg5, size) : (ushort)7;
			short citizenIdx;

			for (citizenIdx = 0; citizenIdx < (short)this.processor.PullGlobalWord(0x70e2); citizenIdx++)
			{
				horizLoc = RenderCitizen(horizLoc, vertLoc, this.processor.PullGlobalWord((ushort)(((citizenIdx & 1) << 1) + 0x6e96)), step);
			}

			if (this.processor.PullLocalWord(0x2) != 0)
			{
				horizLoc += 2;
			}

			for (citizenIdx = 0; (short)(this.owner.World.Towns[townIdx].TrueExtent - arg4 - this.processor.PullGlobalWord(0x70e2) -
				this.processor.PullGlobalWord(0x70e4)) > citizenIdx; citizenIdx++)
			{
				horizLoc = RenderCitizen(horizLoc, vertLoc, this.processor.PullGlobalWord((ushort)(((citizenIdx & 1) << 1) + 0x6e9a)), step);
			}

			if (citizenIdx != 0)
			{
				horizLoc += 2;
			}

			for (citizenIdx = 0; citizenIdx < (short)this.processor.PullGlobalWord(0x70e4); citizenIdx++)
			{
				this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), horizLoc, vertLoc,
					this.processor.PullGlobalWord((ushort)(((citizenIdx & 1) << 1) + 0x6e9e)));

				if (citizenIdx < (short)this.processor.PullGlobalWord(0x6542))
				{
					this.owner.VideoCard.SubstituteHue(this.processor.PullGlobalWord(0xaa), horizLoc, vertLoc, 12, 14, 5, 12);
				}

				horizLoc += (short)step;
			}

			horizLoc += 4;

			for (citizenIdx = 0; citizenIdx < (short)arg4; citizenIdx++)
			{
				ObtainCitizenType((ushort)citizenIdx);

				horizLoc = RenderCitizen(horizLoc, vertLoc, this.processor.PullGlobalWord((ushort)((this.processor.RegAcc.Value16 << 1) + 0x6ea0)), step);
			}

			this.processor.EmitLocalWord(0x6, (ushort)citizenIdx);
			this.processor.EmitLocalWord(0x8, step);

			this.processor.RegAcc.Value16 = step;
			this.processor.LeaveFrame();
			// Far return
			this.processor.Journal.CloseScope("RenderCitizenIcons");

			return step;
		}

		/// <summary>One citizen icon at the place</summary>
		/// <returns>The place of the next one</returns>
		private short RenderCitizen(short horizLoc, short vertLoc, ushort picture, ushort step)
		{
			this.owner.Services.RenderPictureToScreen(this.processor.PullGlobalWord(0xaa), horizLoc, vertLoc, picture);

			return (short)(horizLoc + step);
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPosSrc"></param>
		/// <param name="xPosDst"></param>
		/// <param name="yPosDst"></param>
		public void RenderResourceIcon(short horizLocOrigin, short horizLocTo, short vertLocTo)
		{
			this.processor.Journal.OpenScope($"RenderResourceIcon({horizLocOrigin}, {horizLocTo}, {vertLocTo})");

			// function body
			if (horizLocOrigin < 22 || horizLocOrigin > 24)
			{
				horizLocOrigin--;

				if (horizLocOrigin >= 24)
				{
					horizLocOrigin -= 3;
				}

				if (horizLocOrigin >= 40)
				{
					this.owner.VideoCard.RenderImage(
						this.processor.PullGlobalWord(0x19e8),
						((horizLocOrigin & 1) * 19) + 161, 100, 18, 10,
						this.processor.PullGlobalWord(0xaa),
						horizLocTo, vertLocTo);
				}
				else
				{
					this.owner.VideoCard.RenderImage(
						this.processor.PullGlobalWord(0x19e8),
						((horizLocOrigin / 5) * 19) + 161,
						((horizLocOrigin % 5) * 10) + 50,
						18, 10,
						this.processor.PullGlobalWord(0xaa),
						horizLocTo, vertLocTo);
				}
			}

			// Far return
			this.processor.Journal.CloseScope("RenderResourceIcon");
		}

		/// <summary>
		/// A box from one corner to the other: the background pattern (FloodBoxWithMotif) and the frame (RenderBox, style 1)
		/// </summary>
		public void RenderFrame(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1)
		{
			this.processor.Journal.OpenScope($"RenderFrame({horizLoc}, {vertLoc}, {horizSpot1}, {vertSpot1})");

			int breadth = horizSpot1 - horizLoc;
			int altitude = vertSpot1 - vertLoc;

			FloodBoxWithMotif(horizLoc, vertLoc, breadth, altitude);

			this.owner.Dialogs.RenderBox(horizLoc, vertLoc, breadth, altitude, 1);

			this.processor.Journal.CloseScope("RenderFrame");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="width"></param>
		/// <param name="height"></param>
		public void FloodBoxWithMotif(int horizLoc, int vertLoc, int breadth, int altitude)
		{
			this.processor.Journal.OpenScope($"FloodBoxWithMotif({horizLoc}, {vertLoc}, {breadth}, {altitude})");

			// function body
			if (this.processor.PullGlobalWord(0xd762) == 0)
			{
				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), horizLoc, vertLoc, breadth, altitude, 9);
			}
			else
			{
				int boxVertLoc = vertLoc;
				int boxAltitude = altitude;

				while (boxAltitude > 0)
				{
					int tileAltitude = Math.Min(boxAltitude, 16);
					int boxHorizLoc = horizLoc;
					int boxBreadth = breadth;

					while (boxBreadth > 0)
					{
						int tileBreadth = Math.Min(boxBreadth, 16);

						this.owner.VideoCard.RenderImage(this.processor.PullGlobalWord(0x19e8),
							208, 100, tileBreadth, tileAltitude,
							this.processor.PullGlobalWord(0xaa),
							boxHorizLoc, boxVertLoc);

						boxHorizLoc += tileBreadth;
						boxBreadth -= tileBreadth;
					}

					boxVertLoc += tileAltitude;
					boxAltitude -= tileAltitude;
				}
			}
		
			// Far return
			this.processor.Journal.CloseScope("FloodBoxWithMotif");
		}

		/// <summary>
		/// ?
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		/// <param name="xPos1"></param>
		/// <param name="yPos1"></param>
		/// <param name="stringPtr"></param>
		/// <param name="mode"></param>
		public void RenderLabelBox(int horizLoc, int vertLoc, int horizSpot1, int vertSpot1, ushort charsAddr, ushort style)
		{
			this.processor.Journal.OpenScope($"RenderLabelBox({horizLoc}, {vertLoc}, {horizSpot1}, {vertSpot1}, 0x{charsAddr:x4}, {style})");

			// function body
			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc, horizSpot1 - horizLoc, vertSpot1 - vertLoc, style);
		
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc, horizSpot1, vertLoc, (ushort)((style < 8) ? (style + 8) : 7));
		
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizLoc, vertLoc, horizLoc, vertSpot1, (ushort)((style < 8) ? (style + 8) : 7));
		
			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizLoc + 1, vertSpot1, horizSpot1, vertSpot1, (ushort)((style < 8) ? 8 : (style - 8)));

			this.owner.VideoCard.RenderLine(this.processor.PullGlobalWord(0xaa),
				horizSpot1, vertLoc, horizSpot1, vertSpot1, (ushort)((style < 0x8) ? 8 : (style - 8)));

			this.owner.TextPainter.RenderCenteredTextToScreen0(charsAddr,
				((horizLoc + horizSpot1) / 2) + 1, ((vertLoc + vertSpot1) / 2) - 2, (ushort)(style ^ 8));

			// Far return
			this.processor.Journal.CloseScope("RenderLabelBox");
		}

		/// <summary>
		/// Draws the small map of the city screen: the 80 x 50 tiles around the human player's view (columns from HorizBegin - 40),
		/// every seen tile as a 2 x 2 dot (ocean in colour 1, land in colour 2), two tiles in every 3 pixels: the four tiles of
		/// a 2 x 2 block are shifted by the offsets at 0x280c (columns) and 0x2810 (rows)
		/// </summary>
		public void RenderMiniMap()
		{
			this.processor.Journal.OpenScope("RenderMiniMap()");

			this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 101, 117, 120, 75, 0);

			short humanIdx = this.owner.World.PersonParticipantIdx;

			for (int vertLoc = 0; vertLoc < 50; vertLoc++)
			{
				for (int column = 0; column < 80; column++)
				{
					int horizLoc = this.owner.Routing.TunePosition(this.owner.World.Participants[humanIdx].HorizBegin + column - 40);

					if ((this.owner.World.ChartSight[(ushort)horizLoc, vertLoc] & (1 << humanIdx)) == 0)
						continue;

					ushort quarter = (ushort)(((vertLoc & 1) << 1) + (column & 1));
					int dotHoriz = (column / 2) * 3 + (sbyte)this.processor.PullGlobalByte((ushort)(0x280c + quarter)) + 0x65;
					int dotVert = (vertLoc / 2) * 3 + (sbyte)this.processor.PullGlobalByte((ushort)(0x2810 + quarter)) + 0x76;
					ushort hue = (ushort)(this.owner.MapOps.ObtainTerrainType((short)horizLoc, vertLoc) == MapRules.OceanTerrain ? 1 : 2);

					this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa), dotHoriz, dotVert, 2, 2, hue);
				}
			}

			this.owner.Dialogs.RenderBox(100, 117, 121, 75, 9);

			this.processor.Journal.CloseScope("RenderMiniMap");
		}

		/// <summary>
		/// A dot of 2 x 2 pixels on the small world map (only when 0x2496 = 2), the world centred on the start column of the
		/// human player
		/// </summary>
		public void RenderMiniMapDot(int horizLoc, int vertLoc, ushort style)
		{
			this.processor.Journal.OpenScope($"RenderMiniMapDot({horizLoc}, {vertLoc}, {style})");

			if (this.processor.PullGlobalWord(0x2496) == 2)
			{
				horizLoc = this.owner.Routing.TunePosition(
					horizLoc - this.owner.World.Participants[this.owner.World.PersonParticipantIdx].HorizBegin + 40);

				this.owner.Services.FloodBox(this.processor.PullGlobalWord(0xaa),
					((horizLoc * 3) / 2) + 101, ((vertLoc * 3) / 2) + 118, 2, 2, style);
			}

			this.processor.Journal.CloseScope("RenderMiniMapDot");
		}
	}
}
