using System;

namespace Civilization1Remaster.Acoustics
{
	/// <summary>
	/// Small 8086 (real mode) interpreter. It runs the original sound driver code (*.CVL files),
	/// which only needs plain CPU instructions and port I/O to talk to the sound hardware.
	/// </summary>
	public class X86Interpreter
	{
		private const int StorageExtent = 0x100000;
		private const ushort GivebackSection = 0xffff;
		private const ushort GivebackDisplacement = 0xfff0;

		private readonly byte[] store = new byte[StorageExtent];

		// Registers: AX, CX, DX, BX, SP, BP, SI, DI (in the same order as the instruction encoding)
		private readonly ushort[] cells = new ushort[8];

		// Segments: ES, CS, SS, DS
		private readonly ushort[] sections = new ushort[4];

		private ushort regInstr;
		private bool carryBit, parityBit, auxBit, zeroBit, signBit, trapBit, irqBit, dirBit, overflowBit;

		// Current instruction state
		private int sectionReplace = -1;

		private int modField, regField, rmField;
		private ushort realDisplacement;
		private int realSection;

		public Func<ushort, byte> GatePull { get; set; } = gate => 0xff;
		public Action<ushort, byte> GateEmit { get; set; } = (gate, amount) => { };
		public Action<X86Interpreter, byte> Signal { get; set; } = (processor, numeral) => { };
		public byte[] Storage => this.store;
		public ushort RegAcc { get => this.cells[0]; set => this.cells[0] = value; }
		public ushort RegCount { get => this.cells[1]; set => this.cells[1] = value; }
		public ushort RegData { get => this.cells[2]; set => this.cells[2] = value; }
		public ushort RegBase { get => this.cells[3]; set => this.cells[3] = value; }
		public ushort RegStack { get => this.cells[4]; set => this.cells[4] = value; }
		public ushort RegFrame { get => this.cells[5]; set => this.cells[5] = value; }
		public ushort RegSource { get => this.cells[6]; set => this.cells[6] = value; }
		public ushort RegDest { get => this.cells[7]; set => this.cells[7] = value; }
		public ushort SegExtra { get => this.sections[0]; set => this.sections[0] = value; }
		public ushort SegCode { get => this.sections[1]; set => this.sections[1] = value; }
		public ushort SegStack { get => this.sections[2]; set => this.sections[2] = value; }
		public ushort SegData { get => this.sections[3]; set => this.sections[3] = value; }
		public bool CyMarker { get => this.carryBit; set => this.carryBit = value; }

		/// <summary>
		/// Number of executed instructions, for diagnostics
		/// </summary>
		public long CommandTally { get; private set; }

		public ushort FlagSet
		{
			get
			{
				return (ushort)(0xf002 | (this.carryBit ? 1 : 0) | (this.parityBit ? 4 : 0) | (this.auxBit ? 0x10 : 0) | (this.zeroBit ? 0x40 : 0) |
					(this.signBit ? 0x80 : 0) | (this.trapBit ? 0x100 : 0) | (this.irqBit ? 0x200 : 0) | (this.dirBit ? 0x400 : 0) | (this.overflowBit ? 0x800 : 0));
			}
			set
			{
				this.carryBit = (value & 1) != 0;
				this.parityBit = (value & 4) != 0;
				this.auxBit = (value & 0x10) != 0;
				this.zeroBit = (value & 0x40) != 0;
				this.signBit = (value & 0x80) != 0;
				this.trapBit = (value & 0x100) != 0;
				this.irqBit = (value & 0x200) != 0;
				this.dirBit = (value & 0x400) != 0;
				this.overflowBit = (value & 0x800) != 0;
			}
		}

		private ushort RealSection => this.sections[this.realSection];
		private ushort ContentSection => this.sections[this.sectionReplace >= 0 ? this.sectionReplace : 3];

		public byte PullByte(ushort section, ushort displacement) => this.store[FlatAddress(section, displacement)];

		public ushort PullWord(ushort section, ushort displacement) =>
			(ushort)(this.store[FlatAddress(section, displacement)] | (this.store[FlatAddress(section, (ushort)(displacement + 1))] << 8));

		public void EmitByte(ushort section, ushort displacement, byte amount) => this.store[FlatAddress(section, displacement)] = amount;

		public void EmitWord(ushort section, ushort displacement, ushort amount)
		{
			this.store[FlatAddress(section, displacement)] = (byte)amount;
			this.store[FlatAddress(section, (ushort)(displacement + 1))] = (byte)(amount >> 8);
		}

		public void PutOnStack(ushort amount)
		{
			this.RegStack -= 2;
			EmitWord(this.SegStack, this.RegStack, amount);
		}

		public ushort TakeFromStack()
		{
			ushort amount = PullWord(this.SegStack, this.RegStack);
			this.RegStack += 2;
			return amount;
		}

		/// <summary>
		/// Calls a far procedure and runs until it returns.
		/// Parameters are pushed in C order (last parameter first).
		/// </summary>
		public void InvokeFar(ushort section, ushort displacement, params ushort[] arguments)
		{
			for (int idx = arguments.Length - 1; idx >= 0; idx--)
			{
				PutOnStack(arguments[idx]);
			}

			PutOnStack(GivebackSection);
			PutOnStack(GivebackDisplacement);
			this.SegCode = section;
			this.regInstr = displacement;

			long bound = this.CommandTally + 50_000_000;

			while (this.SegCode != GivebackSection || this.regInstr != GivebackDisplacement)
			{
				Advance();

				if (this.CommandTally > bound)
					throw new InvalidOperationException($"Sound driver doesn't return (stuck at {this.SegCode:x4}:{this.regInstr:x4})");
			}

			this.RegStack += (ushort)(arguments.Length * 2);
		}

		/// <summary>
		/// Executes one instruction
		/// </summary>
		public void Advance()
		{
			this.CommandTally++;
			this.sectionReplace = -1;
			int redo = 0; // 0 none, 1 REPNE, 2 REP/REPE
			ushort commandBegin = this.regInstr;

			byte instruction;

			while (true)
			{
				instruction = ConsumeByte();

				if (instruction == 0x26) this.sectionReplace = 0;
				else if (instruction == 0x2e) this.sectionReplace = 1;
				else if (instruction == 0x36) this.sectionReplace = 2;
				else if (instruction == 0x3e) this.sectionReplace = 3;
				else if (instruction == 0xf2) redo = 1;
				else if (instruction == 0xf3) redo = 2;
				else if (instruction == 0xf0) { } // LOCK
				else break;
			}

			// ALU operations 0x00 - 0x3f
			if (instruction < 0x40 && (instruction & 7) < 6)
			{
				int action = instruction >> 3;

				switch (instruction & 7)
				{
					case 0:
						ParseModRm();
						{
							int outcome = Arith(action, PullRm8(), ObtainReg8(this.regField), false);
							if (action != 7) EmitRm8((byte)outcome);
						}
						return;
					case 1:
						ParseModRm();
						{
							int outcome = Arith(action, PullRm16(), this.cells[this.regField], true);
							if (action != 7) EmitRm16((ushort)outcome);
						}
						return;
					case 2:
						ParseModRm();
						{
							int outcome = Arith(action, ObtainReg8(this.regField), PullRm8(), false);
							if (action != 7) AssignReg8(this.regField, (byte)outcome);
						}
						return;
					case 3:
						ParseModRm();
						{
							int outcome = Arith(action, this.cells[this.regField], PullRm16(), true);
							if (action != 7) this.cells[this.regField] = (ushort)outcome;
						}
						return;
					case 4:
						{
							int outcome = Arith(action, ObtainReg8(0), ConsumeByte(), false);
							if (action != 7) AssignReg8(0, (byte)outcome);
						}
						return;
					default:
						{
							int outcome = Arith(action, this.RegAcc, ConsumeWord(), true);
							if (action != 7) this.RegAcc = (ushort)outcome;
						}
						return;
				}
			}

			switch (instruction)
			{
				case 0x06: PutOnStack(this.SegExtra); return;
				case 0x07: this.SegExtra = TakeFromStack(); return;
				case 0x0e: PutOnStack(this.SegCode); return;
				case 0x16: PutOnStack(this.SegStack); return;
				case 0x17: this.SegStack = TakeFromStack(); return;
				case 0x1e: PutOnStack(this.SegData); return;
				case 0x1f: this.SegData = TakeFromStack(); return;

				case 0x27: // DAA
					{
						int lo8 = ObtainReg8(0);
						bool cy = this.carryBit;
						if ((lo8 & 0xf) > 9 || this.auxBit) { lo8 += 6; this.auxBit = true; }
						if (lo8 > 0x9f || cy) { lo8 += 0x60; this.carryBit = true; }
						AssignReg8(0, (byte)lo8);
						AssignSZP(lo8, false);
					}
					return;
				case 0x2f: // DAS
					{
						int lo8 = ObtainReg8(0);
						bool cy = this.carryBit;
						if ((lo8 & 0xf) > 9 || this.auxBit) { lo8 -= 6; this.auxBit = true; }
						if (lo8 > 0x9f || cy) { lo8 -= 0x60; this.carryBit = true; }
						AssignReg8(0, (byte)lo8);
						AssignSZP(lo8, false);
					}
					return;
				case 0x37: // AAA
				case 0x3f: // AAS
					{
						int lo8 = ObtainReg8(0);
						int hi8 = ObtainReg8(4);
						if ((lo8 & 0xf) > 9 || this.auxBit)
						{
							lo8 = instruction == 0x37 ? lo8 + 6 : lo8 - 6;
							hi8 = instruction == 0x37 ? hi8 + 1 : hi8 - 1;
							this.auxBit = this.carryBit = true;
						}
						else
						{
							this.auxBit = this.carryBit = false;
						}
						this.RegAcc = (ushort)(((hi8 & 0xff) << 8) | (lo8 & 0xf));
					}
					return;
			}

			if (instruction >= 0x40 && instruction <= 0x47) { this.cells[instruction & 7] = (ushort)Increase(this.cells[instruction & 7], true); return; }
			if (instruction >= 0x48 && instruction <= 0x4f) { this.cells[instruction & 7] = (ushort)Decrease(this.cells[instruction & 7], true); return; }
			if (instruction >= 0x50 && instruction <= 0x57)
			{
				// PUSH SP pushes the new value on 8086, the old value on later CPUs, drivers don't depend on it
				ushort amount = this.cells[instruction & 7];
				PutOnStack(amount);
				return;
			}
			if (instruction >= 0x58 && instruction <= 0x5f) { this.cells[instruction & 7] = TakeFromStack(); return; }

			if (instruction >= 0x70 && instruction <= 0x7f)
			{
				sbyte shift = (sbyte)ConsumeByte();
				if (TestCondition(instruction & 0xf))
					this.regInstr = (ushort)(this.regInstr + shift);
				return;
			}

			if (instruction >= 0x91 && instruction <= 0x97)
			{
				ushort scratchAlt = this.RegAcc;
				this.RegAcc = this.cells[instruction & 7];
				this.cells[instruction & 7] = scratchAlt;
				return;
			}

			if (instruction >= 0xb0 && instruction <= 0xb7) { AssignReg8(instruction & 7, ConsumeByte()); return; }
			if (instruction >= 0xb8 && instruction <= 0xbf) { this.cells[instruction & 7] = ConsumeWord(); return; }

			switch (instruction)
			{
				case 0x60: // PUSHA (186)
					{
						ushort stackptr = this.RegStack;
						PutOnStack(this.RegAcc); PutOnStack(this.RegCount); PutOnStack(this.RegData); PutOnStack(this.RegBase);
						PutOnStack(stackptr); PutOnStack(this.RegFrame); PutOnStack(this.RegSource); PutOnStack(this.RegDest);
					}
					return;
				case 0x61: // POPA (186)
					this.RegDest = TakeFromStack(); this.RegSource = TakeFromStack(); this.RegFrame = TakeFromStack(); TakeFromStack();
					this.RegBase = TakeFromStack(); this.RegData = TakeFromStack(); this.RegCount = TakeFromStack(); this.RegAcc = TakeFromStack();
					return;
				case 0x68: PutOnStack(ConsumeWord()); return;
				case 0x6a: PutOnStack((ushort)(sbyte)ConsumeByte()); return;

				case 0x80:
				case 0x82:
					ParseModRm();
					{
						int outcome = Arith(this.regField, PullRm8(), ConsumeByte(), false);
						if (this.regField != 7) EmitRm8((byte)outcome);
					}
					return;
				case 0x81:
					ParseModRm();
					{
						int outcome = Arith(this.regField, PullRm16(), ConsumeWord(), true);
						if (this.regField != 7) EmitRm16((ushort)outcome);
					}
					return;
				case 0x83:
					ParseModRm();
					{
						int outcome = Arith(this.regField, PullRm16(), (ushort)(sbyte)ConsumeByte(), true);
						if (this.regField != 7) EmitRm16((ushort)outcome);
					}
					return;
				case 0x84: ParseModRm(); Arith(4, PullRm8(), ObtainReg8(this.regField), false); return;
				case 0x85: ParseModRm(); Arith(4, PullRm16(), this.cells[this.regField], true); return;
				case 0x86:
					ParseModRm();
					{
						byte scratchAlt = PullRm8();
						EmitRm8(ObtainReg8(this.regField));
						AssignReg8(this.regField, scratchAlt);
					}
					return;
				case 0x87:
					ParseModRm();
					{
						ushort scratchAlt = PullRm16();
						EmitRm16(this.cells[this.regField]);
						this.cells[this.regField] = scratchAlt;
					}
					return;
				case 0x88: ParseModRm(); EmitRm8(ObtainReg8(this.regField)); return;
				case 0x89: ParseModRm(); EmitRm16(this.cells[this.regField]); return;
				case 0x8a: ParseModRm(); AssignReg8(this.regField, PullRm8()); return;
				case 0x8b: ParseModRm(); this.cells[this.regField] = PullRm16(); return;
				case 0x8c: ParseModRm(); EmitRm16(this.sections[this.regField & 3]); return;
				case 0x8d: ParseModRm(); this.cells[this.regField] = this.realDisplacement; return;
				case 0x8e: ParseModRm(); this.sections[this.regField & 3] = PullRm16(); return;
				case 0x8f: ParseModRm(); EmitRm16(TakeFromStack()); return;
				case 0x90: return;
				case 0x98: this.RegAcc = (ushort)(sbyte)ObtainReg8(0); return;
				case 0x99: this.RegData = (ushort)((this.RegAcc & 0x8000) != 0 ? 0xffff : 0); return;
				case 0x9a:
					{
						ushort displacement = ConsumeWord();
						ushort section = ConsumeWord();
						PutOnStack(this.SegCode);
						PutOnStack(this.regInstr);
						this.SegCode = section;
						this.regInstr = displacement;
					}
					return;
				case 0x9b: return; // WAIT
				case 0x9c: PutOnStack(this.FlagSet); return;
				case 0x9d: this.FlagSet = TakeFromStack(); return;
				case 0x9e: this.FlagSet = (ushort)((this.FlagSet & 0xff00) | ObtainReg8(4)); return;
				case 0x9f: AssignReg8(4, (byte)this.FlagSet); return;
				case 0xa0: AssignReg8(0, PullByte(ContentSection, ConsumeWord())); return;
				case 0xa1: this.RegAcc = PullWord(ContentSection, ConsumeWord()); return;
				case 0xa2: EmitByte(ContentSection, ConsumeWord(), ObtainReg8(0)); return;
				case 0xa3: EmitWord(ContentSection, ConsumeWord(), this.RegAcc); return;
				case 0xa8: Arith(4, ObtainReg8(0), ConsumeByte(), false); return;
				case 0xa9: Arith(4, this.RegAcc, ConsumeWord(), true); return;

				case 0xa4: case 0xa5: case 0xa6: case 0xa7:
				case 0xaa: case 0xab: case 0xac: case 0xad: case 0xae: case 0xaf:
					TextInstruction(instruction, redo);
					return;

				case 0xc0:
				case 0xc1:
					ParseModRm();
					{
						bool term = instruction == 0xc1;
						int amount = term ? PullRm16() : PullRm8();
						int outcome = ShiftOrRotate(this.regField, amount, ConsumeByte(), term);
						if (term) EmitRm16((ushort)outcome); else EmitRm8((byte)outcome);
					}
					return;
				case 0xc2:
					{
						ushort tally = ConsumeWord();
						this.regInstr = TakeFromStack();
						this.RegStack += tally;
					}
					return;
				case 0xc3: this.regInstr = TakeFromStack(); return;
				case 0xc4:
				case 0xc5:
					ParseModRm();
					this.cells[this.regField] = PullWord(RealSection, this.realDisplacement);
					if (instruction == 0xc4)
						this.SegExtra = PullWord(RealSection, (ushort)(this.realDisplacement + 2));
					else
						this.SegData = PullWord(RealSection, (ushort)(this.realDisplacement + 2));
					return;
				case 0xc6: ParseModRm(); EmitRm8(ConsumeByte()); return;
				case 0xc7: ParseModRm(); EmitRm16(ConsumeWord()); return;
				case 0xc8: // ENTER (186), only level 0 is used by compilers
					{
						ushort extent = ConsumeWord();
						ConsumeByte();
						PutOnStack(this.RegFrame);
						this.RegFrame = this.RegStack;
						this.RegStack -= extent;
					}
					return;
				case 0xc9: this.RegStack = this.RegFrame; this.RegFrame = TakeFromStack(); return;
				case 0xca:
					{
						ushort tally = ConsumeWord();
						this.regInstr = TakeFromStack();
						this.SegCode = TakeFromStack();
						this.RegStack += tally;
					}
					return;
				case 0xcb: this.regInstr = TakeFromStack(); this.SegCode = TakeFromStack(); return;
				case 0xcc: RaiseInterrupt(3); return;
				case 0xcd: RaiseInterrupt(ConsumeByte()); return;
				case 0xce: if (this.overflowBit) RaiseInterrupt(4); return;
				case 0xcf: this.regInstr = TakeFromStack(); this.SegCode = TakeFromStack(); this.FlagSet = TakeFromStack(); return;

				case 0xd0: case 0xd1: case 0xd2: case 0xd3:
					ParseModRm();
					{
						bool term = (instruction & 1) != 0;
						int tally = instruction >= 0xd2 ? ObtainReg8(1) : 1;
						int amount = term ? PullRm16() : PullRm8();
						int outcome = ShiftOrRotate(this.regField, amount, tally, term);
						if (term) EmitRm16((ushort)outcome); else EmitRm8((byte)outcome);
					}
					return;
				case 0xd4: // AAM
					{
						byte denominator = ConsumeByte();
						if (denominator == 0) { RaiseInterrupt(0); return; }
						int lo8 = ObtainReg8(0);
						this.RegAcc = (ushort)(((lo8 / denominator) << 8) | (lo8 % denominator));
						AssignSZP(this.RegAcc & 0xff, false);
					}
					return;
				case 0xd5: // AAD
					{
						byte multiplier = ConsumeByte();
						int lo8 = (ObtainReg8(0) + ObtainReg8(4) * multiplier) & 0xff;
						this.RegAcc = (ushort)lo8;
						AssignSZP(lo8, false);
					}
					return;
				case 0xd6: AssignReg8(0, (byte)(this.carryBit ? 0xff : 0)); return; // SALC
				case 0xd7: AssignReg8(0, PullByte(ContentSection, (ushort)(this.RegBase + ObtainReg8(0)))); return;
				case 0xd8: case 0xd9: case 0xda: case 0xdb: case 0xdc: case 0xdd: case 0xde: case 0xdf:
					ParseModRm(); // FPU instructions are ignored
					return;

				case 0xe0: // LOOPNE
				case 0xe1: // LOOPE
				case 0xe2: // LOOP
					{
						sbyte shift = (sbyte)ConsumeByte();
						this.RegCount--;
						bool leap = this.RegCount != 0 && (instruction == 0xe2 || (instruction == 0xe1 ? this.zeroBit : !this.zeroBit));
						if (leap) this.regInstr = (ushort)(this.regInstr + shift);
					}
					return;
				case 0xe3:
					{
						sbyte shift = (sbyte)ConsumeByte();
						if (this.RegCount == 0) this.regInstr = (ushort)(this.regInstr + shift);
					}
					return;
				case 0xe4: AssignReg8(0, this.GatePull(ConsumeByte())); return;
				case 0xe5:
					{
						byte gate = ConsumeByte();
						this.RegAcc = (ushort)(this.GatePull(gate) | (this.GatePull((ushort)(gate + 1)) << 8));
					}
					return;
				case 0xe6: this.GateEmit(ConsumeByte(), ObtainReg8(0)); return;
				case 0xe7:
					{
						byte gate = ConsumeByte();
						this.GateEmit(gate, (byte)this.RegAcc);
						this.GateEmit((ushort)(gate + 1), (byte)(this.RegAcc >> 8));
					}
					return;
				case 0xe8:
					{
						ushort shift = ConsumeWord();
						PutOnStack(this.regInstr);
						this.regInstr += shift;
					}
					return;
				case 0xe9: { ushort shift = ConsumeWord(); this.regInstr += shift; } return;
				case 0xea:
					{
						ushort displacement = ConsumeWord();
						this.SegCode = ConsumeWord();
						this.regInstr = displacement;
					}
					return;
				case 0xeb: { sbyte shift = (sbyte)ConsumeByte(); this.regInstr = (ushort)(this.regInstr + shift); } return;
				case 0xec: AssignReg8(0, this.GatePull(this.RegData)); return;
				case 0xed: this.RegAcc = (ushort)(this.GatePull(this.RegData) | (this.GatePull((ushort)(this.RegData + 1)) << 8)); return;
				case 0xee: this.GateEmit(this.RegData, ObtainReg8(0)); return;
				case 0xef:
					this.GateEmit(this.RegData, (byte)this.RegAcc);
					this.GateEmit((ushort)(this.RegData + 1), (byte)(this.RegAcc >> 8));
					return;
				case 0xf4: return; // HLT, nothing to wait for
				case 0xf5: this.carryBit = !this.carryBit; return;
				case 0xf6:
				case 0xf7:
					OpcodeGroup3(instruction == 0xf7);
					return;
				case 0xf8: this.carryBit = false; return;
				case 0xf9: this.carryBit = true; return;
				case 0xfa: this.irqBit = false; return;
				case 0xfb: this.irqBit = true; return;
				case 0xfc: this.dirBit = false; return;
				case 0xfd: this.dirBit = true; return;
				case 0xfe:
					ParseModRm();
					if (this.regField == 0) EmitRm8((byte)Increase(PullRm8(), false));
					else if (this.regField == 1) EmitRm8((byte)Decrease(PullRm8(), false));
					else throw UnsupportedOpcode(commandBegin, instruction);
					return;
				case 0xff:
					OpcodeGroup5(commandBegin, instruction);
					return;
			}

			throw UnsupportedOpcode(commandBegin, instruction);
		}

		private static int FlatAddress(ushort section, ushort displacement) => ((section << 4) + displacement) & (StorageExtent - 1);

		private static bool EvenParity(int amount)
		{
			amount &= 0xff;
			amount ^= amount >> 4;
			amount ^= amount >> 2;
			amount ^= amount >> 1;
			return (amount & 1) == 0;
		}

		private byte ConsumeByte()
		{
			byte amount = PullByte(this.SegCode, this.regInstr);
			this.regInstr++;
			return amount;
		}

		private ushort ConsumeWord()
		{
			ushort amount = PullWord(this.SegCode, this.regInstr);
			this.regInstr += 2;
			return amount;
		}

		private void AssignLogicFlags(int outcome, bool term)
		{
			this.carryBit = false;
			this.overflowBit = false;
			this.auxBit = false;
			AssignSZP(outcome, term);
		}

		private void AssignSZP(int outcome, bool term)
		{
			int filter = term ? 0xffff : 0xff;
			this.zeroBit = (outcome & filter) == 0;
			this.signBit = (outcome & (term ? 0x8000 : 0x80)) != 0;
			this.parityBit = EvenParity(outcome);
		}

		private byte ObtainReg8(int ordinal)
		{
			ushort amount = this.cells[ordinal & 3];
			return (byte)(ordinal < 4 ? amount : amount >> 8);
		}

		private void AssignReg8(int ordinal, byte amount)
		{
			int idx = ordinal & 3;
			if (ordinal < 4)
				this.cells[idx] = (ushort)((this.cells[idx] & 0xff00) | amount);
			else
				this.cells[idx] = (ushort)((this.cells[idx] & 0x00ff) | (amount << 8));
		}

		private void ParseModRm()
		{
			byte addrmode = ConsumeByte();
			this.modField = addrmode >> 6;
			this.regField = (addrmode >> 3) & 7;
			this.rmField = addrmode & 7;

			if (this.modField == 3)
				return;

			int standardSection = 3; // DS
			int displacement;

			switch (this.rmField)
			{
				case 0: displacement = this.RegBase + this.RegSource; break;
				case 1: displacement = this.RegBase + this.RegDest; break;
				case 2: displacement = this.RegFrame + this.RegSource; standardSection = 2; break;
				case 3: displacement = this.RegFrame + this.RegDest; standardSection = 2; break;
				case 4: displacement = this.RegSource; break;
				case 5: displacement = this.RegDest; break;
				case 6:
					if (this.modField == 0)
					{
						displacement = ConsumeWord();
					}
					else
					{
						displacement = this.RegFrame;
						standardSection = 2;
					}
					break;
				default: displacement = this.RegBase; break;
			}

			if (this.modField == 1)
				displacement += (sbyte)ConsumeByte();
			else if (this.modField == 2)
				displacement += ConsumeWord();

			this.realDisplacement = (ushort)displacement;
			this.realSection = this.sectionReplace >= 0 ? this.sectionReplace : standardSection;
		}

		private byte PullRm8() => this.modField == 3 ? ObtainReg8(this.rmField) : PullByte(RealSection, this.realDisplacement);

		private ushort PullRm16() => this.modField == 3 ? this.cells[this.rmField] : PullWord(RealSection, this.realDisplacement);

		private void EmitRm8(byte amount)
		{
			if (this.modField == 3)
				AssignReg8(this.rmField, amount);
			else
				EmitByte(RealSection, this.realDisplacement, amount);
		}

		private void EmitRm16(ushort amount)
		{
			if (this.modField == 3)
				this.cells[this.rmField] = amount;
			else
				EmitWord(RealSection, this.realDisplacement, amount);
		}

		// operation: 0 ADD, 1 OR, 2 ADC, 3 SBB, 4 AND, 5 SUB, 6 XOR, 7 CMP
		private int Arith(int action, int lhs, int rhs, bool term)
		{
			int filter = term ? 0xffff : 0xff;
			int polarityBinary = term ? 0x8000 : 0x80;
			int outcome;

			switch (action)
			{
				case 0:
				case 2:
					{
						int cy = (action == 2 && this.carryBit) ? 1 : 0;
						outcome = lhs + rhs + cy;
						this.carryBit = outcome > filter;
						this.auxBit = ((lhs ^ rhs ^ outcome) & 0x10) != 0;
						this.overflowBit = ((~(lhs ^ rhs) & (lhs ^ outcome)) & polarityBinary) != 0;
						AssignSZP(outcome, term);
						return outcome & filter;
					}
				case 3:
				case 5:
				case 7:
					{
						int debt = (action == 3 && this.carryBit) ? 1 : 0;
						outcome = lhs - rhs - debt;
						this.carryBit = outcome < 0;
						this.auxBit = ((lhs ^ rhs ^ outcome) & 0x10) != 0;
						this.overflowBit = (((lhs ^ rhs) & (lhs ^ outcome)) & polarityBinary) != 0;
						AssignSZP(outcome, term);
						return action == 7 ? lhs : outcome & filter;
					}
				case 1:
					outcome = lhs | rhs;
					break;
				case 4:
					outcome = lhs & rhs;
					break;
				default:
					outcome = lhs ^ rhs;
					break;
			}

			AssignLogicFlags(outcome, term);
			return outcome & filter;
		}

		private int Increase(int amount, bool term)
		{
			bool cy = this.carryBit;
			int outcome = Arith(0, amount, 1, term);
			this.carryBit = cy;
			return outcome;
		}

		private int Decrease(int amount, bool term)
		{
			bool cy = this.carryBit;
			int outcome = Arith(5, amount, 1, term);
			this.carryBit = cy;
			return outcome;
		}

		// operation: 0 ROL, 1 ROR, 2 RCL, 3 RCR, 4 SHL, 5 SHR, 6 SAL, 7 SAR
		private int ShiftOrRotate(int action, int amount, int tally, bool term)
		{
			int digits = term ? 16 : 8;
			int filter = term ? 0xffff : 0xff;
			int polarityBinary = term ? 0x8000 : 0x80;

			tally &= 0x1f;
			if (tally == 0)
				return amount;

			int outcome = amount & filter;

			for (int idx = 0; idx < tally; idx++)
			{
				switch (action)
				{
					case 0:
						this.carryBit = (outcome & polarityBinary) != 0;
						outcome = ((outcome << 1) | (this.carryBit ? 1 : 0)) & filter;
						break;
					case 1:
						this.carryBit = (outcome & 1) != 0;
						outcome = (outcome >> 1) | (this.carryBit ? polarityBinary : 0);
						break;
					case 2:
						{
							bool cy = (outcome & polarityBinary) != 0;
							outcome = ((outcome << 1) | (this.carryBit ? 1 : 0)) & filter;
							this.carryBit = cy;
						}
						break;
					case 3:
						{
							bool cy = (outcome & 1) != 0;
							outcome = (outcome >> 1) | (this.carryBit ? polarityBinary : 0);
							this.carryBit = cy;
						}
						break;
					case 4:
					case 6:
						this.carryBit = (outcome & polarityBinary) != 0;
						outcome = (outcome << 1) & filter;
						break;
					case 5:
						this.carryBit = (outcome & 1) != 0;
						outcome >>= 1;
						break;
					default:
						this.carryBit = (outcome & 1) != 0;
						outcome = (outcome >> 1) | (outcome & polarityBinary);
						break;
				}
			}

			switch (action)
			{
				case 0:
				case 2:
					this.overflowBit = ((outcome & polarityBinary) != 0) != this.carryBit;
					break;
				case 1:
				case 3:
					this.overflowBit = ((outcome ^ (outcome << 1)) & polarityBinary) != 0;
					break;
				case 4:
				case 6:
					this.overflowBit = ((outcome & polarityBinary) != 0) != this.carryBit;
					AssignSZP(outcome, term);
					break;
				case 5:
					this.overflowBit = tally == 1 && (amount & polarityBinary) != 0;
					AssignSZP(outcome, term);
					break;
				default:
					this.overflowBit = false;
					AssignSZP(outcome, term);
					break;
			}

			_ = digits;
			return outcome;
		}

		private void RaiseInterrupt(byte numeral)
		{
			this.Signal(this, numeral);
		}

		private Exception UnsupportedOpcode(ushort commandBegin, byte instruction)
		{
			return new InvalidOperationException($"Sound driver: unsupported instruction 0x{instruction:x2} at {this.SegCode:x4}:{commandBegin:x4}");
		}

		private bool TestCondition(int cipher)
		{
			switch (cipher)
			{
				case 0x0: return this.overflowBit;
				case 0x1: return !this.overflowBit;
				case 0x2: return this.carryBit;
				case 0x3: return !this.carryBit;
				case 0x4: return this.zeroBit;
				case 0x5: return !this.zeroBit;
				case 0x6: return this.carryBit || this.zeroBit;
				case 0x7: return !this.carryBit && !this.zeroBit;
				case 0x8: return this.signBit;
				case 0x9: return !this.signBit;
				case 0xa: return this.parityBit;
				case 0xb: return !this.parityBit;
				case 0xc: return this.signBit != this.overflowBit;
				case 0xd: return this.signBit == this.overflowBit;
				case 0xe: return this.zeroBit || this.signBit != this.overflowBit;
				default: return !this.zeroBit && this.signBit == this.overflowBit;
			}
		}

		private void TextInstruction(byte instruction, int redo)
		{
			bool term = (instruction & 1) != 0;
			int advance = (term ? 2 : 1) * (this.dirBit ? -1 : 1);
			bool contrast = instruction == 0xa6 || instruction == 0xa7 || instruction == 0xae || instruction == 0xaf;

			while (true)
			{
				if (redo != 0 && this.RegCount == 0)
					return;

				switch (instruction)
				{
					case 0xa4: EmitByte(this.SegExtra, this.RegDest, PullByte(ContentSection, this.RegSource)); this.RegSource = (ushort)(this.RegSource + advance); this.RegDest = (ushort)(this.RegDest + advance); break;
					case 0xa5: EmitWord(this.SegExtra, this.RegDest, PullWord(ContentSection, this.RegSource)); this.RegSource = (ushort)(this.RegSource + advance); this.RegDest = (ushort)(this.RegDest + advance); break;
					case 0xa6: Arith(7, PullByte(ContentSection, this.RegSource), PullByte(this.SegExtra, this.RegDest), false); this.RegSource = (ushort)(this.RegSource + advance); this.RegDest = (ushort)(this.RegDest + advance); break;
					case 0xa7: Arith(7, PullWord(ContentSection, this.RegSource), PullWord(this.SegExtra, this.RegDest), true); this.RegSource = (ushort)(this.RegSource + advance); this.RegDest = (ushort)(this.RegDest + advance); break;
					case 0xaa: EmitByte(this.SegExtra, this.RegDest, ObtainReg8(0)); this.RegDest = (ushort)(this.RegDest + advance); break;
					case 0xab: EmitWord(this.SegExtra, this.RegDest, this.RegAcc); this.RegDest = (ushort)(this.RegDest + advance); break;
					case 0xac: AssignReg8(0, PullByte(ContentSection, this.RegSource)); this.RegSource = (ushort)(this.RegSource + advance); break;
					case 0xad: this.RegAcc = PullWord(ContentSection, this.RegSource); this.RegSource = (ushort)(this.RegSource + advance); break;
					case 0xae: Arith(7, ObtainReg8(0), PullByte(this.SegExtra, this.RegDest), false); this.RegDest = (ushort)(this.RegDest + advance); break;
					default: Arith(7, this.RegAcc, PullWord(this.SegExtra, this.RegDest), true); this.RegDest = (ushort)(this.RegDest + advance); break;
				}

				if (redo == 0)
					return;

				this.RegCount--;

				if (contrast && ((redo == 2 && !this.zeroBit) || (redo == 1 && this.zeroBit)))
					return;
			}
		}

		private void OpcodeGroup3(bool term)
		{
			ParseModRm();

			switch (this.regField)
			{
				case 0:
				case 1:
					if (term) Arith(4, PullRm16(), ConsumeWord(), true);
					else Arith(4, PullRm8(), ConsumeByte(), false);
					return;
				case 2:
					if (term) EmitRm16((ushort)~PullRm16());
					else EmitRm8((byte)~PullRm8());
					return;
				case 3:
					if (term) { int amount = PullRm16(); EmitRm16((ushort)Arith(5, 0, amount, true)); this.carryBit = amount != 0; }
					else { int amount = PullRm8(); EmitRm8((byte)Arith(5, 0, amount, false)); this.carryBit = amount != 0; }
					return;
				case 4:
					if (term)
					{
						uint outcome = (uint)this.RegAcc * PullRm16();
						this.RegAcc = (ushort)outcome;
						this.RegData = (ushort)(outcome >> 16);
						this.carryBit = this.overflowBit = this.RegData != 0;
					}
					else
					{
						int outcome = ObtainReg8(0) * PullRm8();
						this.RegAcc = (ushort)outcome;
						this.carryBit = this.overflowBit = (outcome & 0xff00) != 0;
					}
					return;
				case 5:
					if (term)
					{
						int outcome = (short)this.RegAcc * (short)PullRm16();
						this.RegAcc = (ushort)outcome;
						this.RegData = (ushort)(outcome >> 16);
						this.carryBit = this.overflowBit = outcome != (short)outcome;
					}
					else
					{
						int outcome = (sbyte)ObtainReg8(0) * (sbyte)PullRm8();
						this.RegAcc = (ushort)outcome;
						this.carryBit = this.overflowBit = outcome != (sbyte)outcome;
					}
					return;
				case 6:
					if (term)
					{
						uint denominator = PullRm16();
						uint numerator = ((uint)this.RegData << 16) | this.RegAcc;
						if (denominator == 0 || numerator / denominator > 0xffff) { RaiseInterrupt(0); return; }
						this.RegAcc = (ushort)(numerator / denominator);
						this.RegData = (ushort)(numerator % denominator);
					}
					else
					{
						int denominator = PullRm8();
						int numerator = this.RegAcc;
						if (denominator == 0 || numerator / denominator > 0xff) { RaiseInterrupt(0); return; }
						this.RegAcc = (ushort)(((numerator % denominator) << 8) | (numerator / denominator));
					}
					return;
				default:
					if (term)
					{
						int denominator = (short)PullRm16();
						int numerator = (int)(((uint)this.RegData << 16) | this.RegAcc);
						if (denominator == 0) { RaiseInterrupt(0); return; }
						int division = numerator / denominator;
						if (division != (short)division) { RaiseInterrupt(0); return; }
						this.RegAcc = (ushort)division;
						this.RegData = (ushort)(numerator % denominator);
					}
					else
					{
						int denominator = (sbyte)PullRm8();
						int numerator = (short)this.RegAcc;
						if (denominator == 0) { RaiseInterrupt(0); return; }
						int division = numerator / denominator;
						if (division != (sbyte)division) { RaiseInterrupt(0); return; }
						this.RegAcc = (ushort)(((numerator % denominator & 0xff) << 8) | (division & 0xff));
					}
					return;
			}
		}

		private void OpcodeGroup5(ushort commandBegin, byte instruction)
		{
			ParseModRm();

			switch (this.regField)
			{
				case 0: EmitRm16((ushort)Increase(PullRm16(), true)); return;
				case 1: EmitRm16((ushort)Decrease(PullRm16(), true)); return;
				case 2:
					{
						ushort aim = PullRm16();
						PutOnStack(this.regInstr);
						this.regInstr = aim;
					}
					return;
				case 3:
					{
						ushort displacement = PullWord(RealSection, this.realDisplacement);
						ushort section = PullWord(RealSection, (ushort)(this.realDisplacement + 2));
						PutOnStack(this.SegCode);
						PutOnStack(this.regInstr);
						this.SegCode = section;
						this.regInstr = displacement;
					}
					return;
				case 4: this.regInstr = PullRm16(); return;
				case 5:
					{
						ushort displacement = PullWord(RealSection, this.realDisplacement);
						this.SegCode = PullWord(RealSection, (ushort)(this.realDisplacement + 2));
						this.regInstr = displacement;
					}
					return;
				case 6: PutOnStack(PullRm16()); return;
				default: throw UnsupportedOpcode(commandBegin, instruction);
			}
		}
	}
}
