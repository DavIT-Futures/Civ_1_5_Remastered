using System;

namespace Civ_1_5_Remastered.Acoustics.Drivers
{
	public class MachineCodeInterpreter
	{
		private const int StorageExtent = 0x100000;
		private const ushort GivebackSection = 0xffff;
		private const ushort GivebackDisplacement = 0xfff0;

		private readonly byte[] store = new byte[StorageExtent];

		private readonly ushort[] cells = new ushort[8];

		private readonly ushort[] sections = new ushort[4];

		private ushort registerInstruction;
		private bool carryBit, parityBit, auxiliaryBit, zeroBit, signBit, trapBit, interruptBit, directionBit, overflowBit;

		private int sectionReplace = -1;

		private int modeField, registerField, registerOrMemoryField;
		private ushort realDisplacement;
		private int realSection;

		public Func<ushort, byte> GatePull { get; set; } = gate => 0xff;
		public Action<ushort, byte> GateEmit { get; set; } = (gate, amount) => { };
		public Action<MachineCodeInterpreter, byte> Signal { get; set; } = (processor, numeral) => { };
		public byte[] Storage => this.store;
		public ushort RegisterAccumulator { get => this.cells[0]; set => this.cells[0] = value; }
		public ushort RegisterCount { get => this.cells[1]; set => this.cells[1] = value; }
		public ushort RegisterData { get => this.cells[2]; set => this.cells[2] = value; }
		public ushort RegisterBase { get => this.cells[3]; set => this.cells[3] = value; }
		public ushort RegisterStack { get => this.cells[4]; set => this.cells[4] = value; }
		public ushort RegisterFrame { get => this.cells[5]; set => this.cells[5] = value; }
		public ushort RegisterSource { get => this.cells[6]; set => this.cells[6] = value; }
		public ushort RegisterDestination { get => this.cells[7]; set => this.cells[7] = value; }
		public ushort SegmentExtra { get => this.sections[0]; set => this.sections[0] = value; }
		public ushort SegmentCode { get => this.sections[1]; set => this.sections[1] = value; }
		public ushort SegmentStack { get => this.sections[2]; set => this.sections[2] = value; }
		public ushort SegmentData { get => this.sections[3]; set => this.sections[3] = value; }
		public bool CarryMarker { get => this.carryBit; set => this.carryBit = value; }

		public long CommandTally { get; private set; }

		public ushort FlagSet
		{
			get
			{
				return (ushort)(0xf002 | (this.carryBit ? 1 : 0) | (this.parityBit ? 4 : 0) | (this.auxiliaryBit ? 0x10 : 0) | (this.zeroBit ? 0x40 : 0) |
					(this.signBit ? 0x80 : 0) | (this.trapBit ? 0x100 : 0) | (this.interruptBit ? 0x200 : 0) | (this.directionBit ? 0x400 : 0) | (this.overflowBit ? 0x800 : 0));
			}
			set
			{
				this.carryBit = (value & 1) != 0;
				this.parityBit = (value & 4) != 0;
				this.auxiliaryBit = (value & 0x10) != 0;
				this.zeroBit = (value & 0x40) != 0;
				this.signBit = (value & 0x80) != 0;
				this.trapBit = (value & 0x100) != 0;
				this.interruptBit = (value & 0x200) != 0;
				this.directionBit = (value & 0x400) != 0;
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
			this.RegisterStack -= 2;
			EmitWord(this.SegmentStack, this.RegisterStack, amount);
		}

		public ushort TakeFromStack()
		{
			ushort amount = PullWord(this.SegmentStack, this.RegisterStack);
			this.RegisterStack += 2;
			return amount;
		}

		public void InvokeFar(ushort section, ushort displacement, params ushort[] arguments)
		{
			for (int index = arguments.Length - 1; index >= 0; index--)
			{
				PutOnStack(arguments[index]);
			}

			PutOnStack(GivebackSection);
			PutOnStack(GivebackDisplacement);
			this.SegmentCode = section;
			this.registerInstruction = displacement;

			long bound = this.CommandTally + 50_000_000;

			while (this.SegmentCode != GivebackSection || this.registerInstruction != GivebackDisplacement)
			{
				Advance();

				if (this.CommandTally > bound)
					throw new InvalidOperationException($"Sound driver doesn't return (stuck at {this.SegmentCode:x4}:{this.registerInstruction:x4})");
			}

			this.RegisterStack += (ushort)(arguments.Length * 2);
		}

		public void Advance()
		{
			this.CommandTally++;
			this.sectionReplace = -1;
			int redo = 0;
			ushort commandBegin = this.registerInstruction;

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
				else if (instruction == 0xf0) { }
				else break;
			}

			if (instruction < 0x40 && (instruction & 7) < 6)
			{
				int action = instruction >> 3;

				switch (instruction & 7)
				{
					case 0:
						ParseModeRegisterOrMemory();
						{
							int outcome = Arithmetic(action, PullRegisterOrMemoryByte(), ObtainByteRegister(this.registerField), false);
							if (action != 7) EmitRegisterOrMemoryByte((byte)outcome);
						}
						return;
					case 1:
						ParseModeRegisterOrMemory();
						{
							int outcome = Arithmetic(action, PullRegisterOrMemoryWord(), this.cells[this.registerField], true);
							if (action != 7) EmitRegisterOrMemoryWord((ushort)outcome);
						}
						return;
					case 2:
						ParseModeRegisterOrMemory();
						{
							int outcome = Arithmetic(action, ObtainByteRegister(this.registerField), PullRegisterOrMemoryByte(), false);
							if (action != 7) AssignByteRegister(this.registerField, (byte)outcome);
						}
						return;
					case 3:
						ParseModeRegisterOrMemory();
						{
							int outcome = Arithmetic(action, this.cells[this.registerField], PullRegisterOrMemoryWord(), true);
							if (action != 7) this.cells[this.registerField] = (ushort)outcome;
						}
						return;
					case 4:
						{
							int outcome = Arithmetic(action, ObtainByteRegister(0), ConsumeByte(), false);
							if (action != 7) AssignByteRegister(0, (byte)outcome);
						}
						return;
					default:
						{
							int outcome = Arithmetic(action, this.RegisterAccumulator, ConsumeWord(), true);
							if (action != 7) this.RegisterAccumulator = (ushort)outcome;
						}
						return;
				}
			}

			switch (instruction)
			{
				case 0x06: PutOnStack(this.SegmentExtra); return;
				case 0x07: this.SegmentExtra = TakeFromStack(); return;
				case 0x0e: PutOnStack(this.SegmentCode); return;
				case 0x16: PutOnStack(this.SegmentStack); return;
				case 0x17: this.SegmentStack = TakeFromStack(); return;
				case 0x1e: PutOnStack(this.SegmentData); return;
				case 0x1f: this.SegmentData = TakeFromStack(); return;

				case 0x27:
					{
						int lowByte = ObtainByteRegister(0);
						bool carry = this.carryBit;
						if ((lowByte & 0xf) > 9 || this.auxiliaryBit) { lowByte += 6; this.auxiliaryBit = true; }
						if (lowByte > 0x9f || carry) { lowByte += 0x60; this.carryBit = true; }
						AssignByteRegister(0, (byte)lowByte);
						AssignSignZeroParity(lowByte, false);
					}
					return;
				case 0x2f:
					{
						int lowByte = ObtainByteRegister(0);
						bool carry = this.carryBit;
						if ((lowByte & 0xf) > 9 || this.auxiliaryBit) { lowByte -= 6; this.auxiliaryBit = true; }
						if (lowByte > 0x9f || carry) { lowByte -= 0x60; this.carryBit = true; }
						AssignByteRegister(0, (byte)lowByte);
						AssignSignZeroParity(lowByte, false);
					}
					return;
				case 0x37:
				case 0x3f:
					{
						int lowByte = ObtainByteRegister(0);
						int highByte = ObtainByteRegister(4);
						if ((lowByte & 0xf) > 9 || this.auxiliaryBit)
						{
							lowByte = instruction == 0x37 ? lowByte + 6 : lowByte - 6;
							highByte = instruction == 0x37 ? highByte + 1 : highByte - 1;
							this.auxiliaryBit = this.carryBit = true;
						}
						else
						{
							this.auxiliaryBit = this.carryBit = false;
						}
						this.RegisterAccumulator = (ushort)(((highByte & 0xff) << 8) | (lowByte & 0xf));
					}
					return;
			}

			if (instruction >= 0x40 && instruction <= 0x47) { this.cells[instruction & 7] = (ushort)Increase(this.cells[instruction & 7], true); return; }
			if (instruction >= 0x48 && instruction <= 0x4f) { this.cells[instruction & 7] = (ushort)Decrease(this.cells[instruction & 7], true); return; }
			if (instruction >= 0x50 && instruction <= 0x57)
			{
				ushort amount = this.cells[instruction & 7];
				PutOnStack(amount);
				return;
			}
			if (instruction >= 0x58 && instruction <= 0x5f) { this.cells[instruction & 7] = TakeFromStack(); return; }

			if (instruction >= 0x70 && instruction <= 0x7f)
			{
				sbyte shift = (sbyte)ConsumeByte();
				if (TestCondition(instruction & 0xf))
					this.registerInstruction = (ushort)(this.registerInstruction + shift);
				return;
			}

			if (instruction >= 0x91 && instruction <= 0x97)
			{
				ushort scratchAlternate = this.RegisterAccumulator;
				this.RegisterAccumulator = this.cells[instruction & 7];
				this.cells[instruction & 7] = scratchAlternate;
				return;
			}

			if (instruction >= 0xb0 && instruction <= 0xb7) { AssignByteRegister(instruction & 7, ConsumeByte()); return; }
			if (instruction >= 0xb8 && instruction <= 0xbf) { this.cells[instruction & 7] = ConsumeWord(); return; }

			switch (instruction)
			{
				case 0x60:
					{
						ushort stackPointer = this.RegisterStack;
						PutOnStack(this.RegisterAccumulator); PutOnStack(this.RegisterCount); PutOnStack(this.RegisterData); PutOnStack(this.RegisterBase);
						PutOnStack(stackPointer); PutOnStack(this.RegisterFrame); PutOnStack(this.RegisterSource); PutOnStack(this.RegisterDestination);
					}
					return;
				case 0x61:
					this.RegisterDestination = TakeFromStack(); this.RegisterSource = TakeFromStack(); this.RegisterFrame = TakeFromStack(); TakeFromStack();
					this.RegisterBase = TakeFromStack(); this.RegisterData = TakeFromStack(); this.RegisterCount = TakeFromStack(); this.RegisterAccumulator = TakeFromStack();
					return;
				case 0x68: PutOnStack(ConsumeWord()); return;
				case 0x6a: PutOnStack((ushort)(sbyte)ConsumeByte()); return;

				case 0x80:
				case 0x82:
					ParseModeRegisterOrMemory();
					{
						int outcome = Arithmetic(this.registerField, PullRegisterOrMemoryByte(), ConsumeByte(), false);
						if (this.registerField != 7) EmitRegisterOrMemoryByte((byte)outcome);
					}
					return;
				case 0x81:
					ParseModeRegisterOrMemory();
					{
						int outcome = Arithmetic(this.registerField, PullRegisterOrMemoryWord(), ConsumeWord(), true);
						if (this.registerField != 7) EmitRegisterOrMemoryWord((ushort)outcome);
					}
					return;
				case 0x83:
					ParseModeRegisterOrMemory();
					{
						int outcome = Arithmetic(this.registerField, PullRegisterOrMemoryWord(), (ushort)(sbyte)ConsumeByte(), true);
						if (this.registerField != 7) EmitRegisterOrMemoryWord((ushort)outcome);
					}
					return;
				case 0x84: ParseModeRegisterOrMemory(); Arithmetic(4, PullRegisterOrMemoryByte(), ObtainByteRegister(this.registerField), false); return;
				case 0x85: ParseModeRegisterOrMemory(); Arithmetic(4, PullRegisterOrMemoryWord(), this.cells[this.registerField], true); return;
				case 0x86:
					ParseModeRegisterOrMemory();
					{
						byte scratchAlternate = PullRegisterOrMemoryByte();
						EmitRegisterOrMemoryByte(ObtainByteRegister(this.registerField));
						AssignByteRegister(this.registerField, scratchAlternate);
					}
					return;
				case 0x87:
					ParseModeRegisterOrMemory();
					{
						ushort scratchAlternate = PullRegisterOrMemoryWord();
						EmitRegisterOrMemoryWord(this.cells[this.registerField]);
						this.cells[this.registerField] = scratchAlternate;
					}
					return;
				case 0x88: ParseModeRegisterOrMemory(); EmitRegisterOrMemoryByte(ObtainByteRegister(this.registerField)); return;
				case 0x89: ParseModeRegisterOrMemory(); EmitRegisterOrMemoryWord(this.cells[this.registerField]); return;
				case 0x8a: ParseModeRegisterOrMemory(); AssignByteRegister(this.registerField, PullRegisterOrMemoryByte()); return;
				case 0x8b: ParseModeRegisterOrMemory(); this.cells[this.registerField] = PullRegisterOrMemoryWord(); return;
				case 0x8c: ParseModeRegisterOrMemory(); EmitRegisterOrMemoryWord(this.sections[this.registerField & 3]); return;
				case 0x8d: ParseModeRegisterOrMemory(); this.cells[this.registerField] = this.realDisplacement; return;
				case 0x8e: ParseModeRegisterOrMemory(); this.sections[this.registerField & 3] = PullRegisterOrMemoryWord(); return;
				case 0x8f: ParseModeRegisterOrMemory(); EmitRegisterOrMemoryWord(TakeFromStack()); return;
				case 0x90: return;
				case 0x98: this.RegisterAccumulator = (ushort)(sbyte)ObtainByteRegister(0); return;
				case 0x99: this.RegisterData = (ushort)((this.RegisterAccumulator & 0x8000) != 0 ? 0xffff : 0); return;
				case 0x9a:
					{
						ushort displacement = ConsumeWord();
						ushort section = ConsumeWord();
						PutOnStack(this.SegmentCode);
						PutOnStack(this.registerInstruction);
						this.SegmentCode = section;
						this.registerInstruction = displacement;
					}
					return;
				case 0x9b: return;
				case 0x9c: PutOnStack(this.FlagSet); return;
				case 0x9d: this.FlagSet = TakeFromStack(); return;
				case 0x9e: this.FlagSet = (ushort)((this.FlagSet & 0xff00) | ObtainByteRegister(4)); return;
				case 0x9f: AssignByteRegister(4, (byte)this.FlagSet); return;
				case 0xa0: AssignByteRegister(0, PullByte(ContentSection, ConsumeWord())); return;
				case 0xa1: this.RegisterAccumulator = PullWord(ContentSection, ConsumeWord()); return;
				case 0xa2: EmitByte(ContentSection, ConsumeWord(), ObtainByteRegister(0)); return;
				case 0xa3: EmitWord(ContentSection, ConsumeWord(), this.RegisterAccumulator); return;
				case 0xa8: Arithmetic(4, ObtainByteRegister(0), ConsumeByte(), false); return;
				case 0xa9: Arithmetic(4, this.RegisterAccumulator, ConsumeWord(), true); return;

				case 0xa4:
				case 0xa5:
				case 0xa6:
				case 0xa7:
				case 0xaa:
				case 0xab:
				case 0xac:
				case 0xad:
				case 0xae:
				case 0xaf:
					TextInstruction(instruction, redo);
					return;

				case 0xc0:
				case 0xc1:
					ParseModeRegisterOrMemory();
					{
						bool term = instruction == 0xc1;
						int amount = term ? PullRegisterOrMemoryWord() : PullRegisterOrMemoryByte();
						int outcome = ShiftOrRotate(this.registerField, amount, ConsumeByte(), term);
						if (term) EmitRegisterOrMemoryWord((ushort)outcome); else EmitRegisterOrMemoryByte((byte)outcome);
					}
					return;
				case 0xc2:
					{
						ushort tally = ConsumeWord();
						this.registerInstruction = TakeFromStack();
						this.RegisterStack += tally;
					}
					return;
				case 0xc3: this.registerInstruction = TakeFromStack(); return;
				case 0xc4:
				case 0xc5:
					ParseModeRegisterOrMemory();
					this.cells[this.registerField] = PullWord(RealSection, this.realDisplacement);
					if (instruction == 0xc4)
						this.SegmentExtra = PullWord(RealSection, (ushort)(this.realDisplacement + 2));
					else
						this.SegmentData = PullWord(RealSection, (ushort)(this.realDisplacement + 2));
					return;
				case 0xc6: ParseModeRegisterOrMemory(); EmitRegisterOrMemoryByte(ConsumeByte()); return;
				case 0xc7: ParseModeRegisterOrMemory(); EmitRegisterOrMemoryWord(ConsumeWord()); return;
				case 0xc8:
					{
						ushort extent = ConsumeWord();
						ConsumeByte();
						PutOnStack(this.RegisterFrame);
						this.RegisterFrame = this.RegisterStack;
						this.RegisterStack -= extent;
					}
					return;
				case 0xc9: this.RegisterStack = this.RegisterFrame; this.RegisterFrame = TakeFromStack(); return;
				case 0xca:
					{
						ushort tally = ConsumeWord();
						this.registerInstruction = TakeFromStack();
						this.SegmentCode = TakeFromStack();
						this.RegisterStack += tally;
					}
					return;
				case 0xcb: this.registerInstruction = TakeFromStack(); this.SegmentCode = TakeFromStack(); return;
				case 0xcc: RaiseInterrupt(3); return;
				case 0xcd: RaiseInterrupt(ConsumeByte()); return;
				case 0xce: if (this.overflowBit) RaiseInterrupt(4); return;
				case 0xcf: this.registerInstruction = TakeFromStack(); this.SegmentCode = TakeFromStack(); this.FlagSet = TakeFromStack(); return;

				case 0xd0:
				case 0xd1:
				case 0xd2:
				case 0xd3:
					ParseModeRegisterOrMemory();
					{
						bool term = (instruction & 1) != 0;
						int tally = instruction >= 0xd2 ? ObtainByteRegister(1) : 1;
						int amount = term ? PullRegisterOrMemoryWord() : PullRegisterOrMemoryByte();
						int outcome = ShiftOrRotate(this.registerField, amount, tally, term);
						if (term) EmitRegisterOrMemoryWord((ushort)outcome); else EmitRegisterOrMemoryByte((byte)outcome);
					}
					return;
				case 0xd4:
					{
						byte denominator = ConsumeByte();
						if (denominator == 0) { RaiseInterrupt(0); return; }
						int lowByte = ObtainByteRegister(0);
						this.RegisterAccumulator = (ushort)(((lowByte / denominator) << 8) | (lowByte % denominator));
						AssignSignZeroParity(this.RegisterAccumulator & 0xff, false);
					}
					return;
				case 0xd5:
					{
						byte multiplier = ConsumeByte();
						int lowByte = (ObtainByteRegister(0) + ObtainByteRegister(4) * multiplier) & 0xff;
						this.RegisterAccumulator = (ushort)lowByte;
						AssignSignZeroParity(lowByte, false);
					}
					return;
				case 0xd6: AssignByteRegister(0, (byte)(this.carryBit ? 0xff : 0)); return;
				case 0xd7: AssignByteRegister(0, PullByte(ContentSection, (ushort)(this.RegisterBase + ObtainByteRegister(0)))); return;
				case 0xd8:
				case 0xd9:
				case 0xda:
				case 0xdb:
				case 0xdc:
				case 0xdd:
				case 0xde:
				case 0xdf:
					ParseModeRegisterOrMemory();
					return;

				case 0xe0:
				case 0xe1:
				case 0xe2:
					{
						sbyte shift = (sbyte)ConsumeByte();
						this.RegisterCount--;
						bool leap = this.RegisterCount != 0 && (instruction == 0xe2 || (instruction == 0xe1 ? this.zeroBit : !this.zeroBit));
						if (leap) this.registerInstruction = (ushort)(this.registerInstruction + shift);
					}
					return;
				case 0xe3:
					{
						sbyte shift = (sbyte)ConsumeByte();
						if (this.RegisterCount == 0) this.registerInstruction = (ushort)(this.registerInstruction + shift);
					}
					return;
				case 0xe4: AssignByteRegister(0, this.GatePull(ConsumeByte())); return;
				case 0xe5:
					{
						byte gate = ConsumeByte();
						this.RegisterAccumulator = (ushort)(this.GatePull(gate) | (this.GatePull((ushort)(gate + 1)) << 8));
					}
					return;
				case 0xe6: this.GateEmit(ConsumeByte(), ObtainByteRegister(0)); return;
				case 0xe7:
					{
						byte gate = ConsumeByte();
						this.GateEmit(gate, (byte)this.RegisterAccumulator);
						this.GateEmit((ushort)(gate + 1), (byte)(this.RegisterAccumulator >> 8));
					}
					return;
				case 0xe8:
					{
						ushort shift = ConsumeWord();
						PutOnStack(this.registerInstruction);
						this.registerInstruction += shift;
					}
					return;
				case 0xe9: { ushort shift = ConsumeWord(); this.registerInstruction += shift; } return;
				case 0xea:
					{
						ushort displacement = ConsumeWord();
						this.SegmentCode = ConsumeWord();
						this.registerInstruction = displacement;
					}
					return;
				case 0xeb: { sbyte shift = (sbyte)ConsumeByte(); this.registerInstruction = (ushort)(this.registerInstruction + shift); } return;
				case 0xec: AssignByteRegister(0, this.GatePull(this.RegisterData)); return;
				case 0xed: this.RegisterAccumulator = (ushort)(this.GatePull(this.RegisterData) | (this.GatePull((ushort)(this.RegisterData + 1)) << 8)); return;
				case 0xee: this.GateEmit(this.RegisterData, ObtainByteRegister(0)); return;
				case 0xef:
					this.GateEmit(this.RegisterData, (byte)this.RegisterAccumulator);
					this.GateEmit((ushort)(this.RegisterData + 1), (byte)(this.RegisterAccumulator >> 8));
					return;
				case 0xf4: return;
				case 0xf5: this.carryBit = !this.carryBit; return;
				case 0xf6:
				case 0xf7:
					ThirdOpcodeGroup(instruction == 0xf7);
					return;
				case 0xf8: this.carryBit = false; return;
				case 0xf9: this.carryBit = true; return;
				case 0xfa: this.interruptBit = false; return;
				case 0xfb: this.interruptBit = true; return;
				case 0xfc: this.directionBit = false; return;
				case 0xfd: this.directionBit = true; return;
				case 0xfe:
					ParseModeRegisterOrMemory();
					if (this.registerField == 0) EmitRegisterOrMemoryByte((byte)Increase(PullRegisterOrMemoryByte(), false));
					else if (this.registerField == 1) EmitRegisterOrMemoryByte((byte)Decrease(PullRegisterOrMemoryByte(), false));
					else throw UnsupportedOpcode(commandBegin, instruction);
					return;
				case 0xff:
					FifthOpcodeGroup(commandBegin, instruction);
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
			byte amount = PullByte(this.SegmentCode, this.registerInstruction);
			this.registerInstruction++;
			return amount;
		}

		private ushort ConsumeWord()
		{
			ushort amount = PullWord(this.SegmentCode, this.registerInstruction);
			this.registerInstruction += 2;
			return amount;
		}

		private void AssignLogicFlags(int outcome, bool term)
		{
			this.carryBit = false;
			this.overflowBit = false;
			this.auxiliaryBit = false;
			AssignSignZeroParity(outcome, term);
		}

		private void AssignSignZeroParity(int outcome, bool term)
		{
			int filter = term ? 0xffff : 0xff;
			this.zeroBit = (outcome & filter) == 0;
			this.signBit = (outcome & (term ? 0x8000 : 0x80)) != 0;
			this.parityBit = EvenParity(outcome);
		}

		private byte ObtainByteRegister(int ordinal)
		{
			ushort amount = this.cells[ordinal & 3];
			return (byte)(ordinal < 4 ? amount : amount >> 8);
		}

		private void AssignByteRegister(int ordinal, byte amount)
		{
			int index = ordinal & 3;
			if (ordinal < 4)
				this.cells[index] = (ushort)((this.cells[index] & 0xff00) | amount);
			else
				this.cells[index] = (ushort)((this.cells[index] & 0x00ff) | (amount << 8));
		}

		private void ParseModeRegisterOrMemory()
		{
			byte addressingMode = ConsumeByte();
			this.modeField = addressingMode >> 6;
			this.registerField = (addressingMode >> 3) & 7;
			this.registerOrMemoryField = addressingMode & 7;

			if (this.modeField == 3)
				return;

			int standardSection = 3;
			int displacement;

			switch (this.registerOrMemoryField)
			{
				case 0: displacement = this.RegisterBase + this.RegisterSource; break;
				case 1: displacement = this.RegisterBase + this.RegisterDestination; break;
				case 2: displacement = this.RegisterFrame + this.RegisterSource; standardSection = 2; break;
				case 3: displacement = this.RegisterFrame + this.RegisterDestination; standardSection = 2; break;
				case 4: displacement = this.RegisterSource; break;
				case 5: displacement = this.RegisterDestination; break;
				case 6:
					if (this.modeField == 0)
					{
						displacement = ConsumeWord();
					}
					else
					{
						displacement = this.RegisterFrame;
						standardSection = 2;
					}
					break;
				default: displacement = this.RegisterBase; break;
			}

			if (this.modeField == 1)
				displacement += (sbyte)ConsumeByte();
			else if (this.modeField == 2)
				displacement += ConsumeWord();

			this.realDisplacement = (ushort)displacement;
			this.realSection = this.sectionReplace >= 0 ? this.sectionReplace : standardSection;
		}

		private byte PullRegisterOrMemoryByte() => this.modeField == 3 ? ObtainByteRegister(this.registerOrMemoryField) : PullByte(RealSection, this.realDisplacement);

		private ushort PullRegisterOrMemoryWord() => this.modeField == 3 ? this.cells[this.registerOrMemoryField] : PullWord(RealSection, this.realDisplacement);

		private void EmitRegisterOrMemoryByte(byte amount)
		{
			if (this.modeField == 3)
				AssignByteRegister(this.registerOrMemoryField, amount);
			else
				EmitByte(RealSection, this.realDisplacement, amount);
		}

		private void EmitRegisterOrMemoryWord(ushort amount)
		{
			if (this.modeField == 3)
				this.cells[this.registerOrMemoryField] = amount;
			else
				EmitWord(RealSection, this.realDisplacement, amount);
		}

		private int Arithmetic(int action, int left, int right, bool term)
		{
			int filter = term ? 0xffff : 0xff;
			int polarityBinary = term ? 0x8000 : 0x80;
			int outcome;

			switch (action)
			{
				case 0:
				case 2:
					{
						int carry = (action == 2 && this.carryBit) ? 1 : 0;
						outcome = left + right + carry;
						this.carryBit = outcome > filter;
						this.auxiliaryBit = ((left ^ right ^ outcome) & 0x10) != 0;
						this.overflowBit = ((~(left ^ right) & (left ^ outcome)) & polarityBinary) != 0;
						AssignSignZeroParity(outcome, term);
						return outcome & filter;
					}
				case 3:
				case 5:
				case 7:
					{
						int debt = (action == 3 && this.carryBit) ? 1 : 0;
						outcome = left - right - debt;
						this.carryBit = outcome < 0;
						this.auxiliaryBit = ((left ^ right ^ outcome) & 0x10) != 0;
						this.overflowBit = (((left ^ right) & (left ^ outcome)) & polarityBinary) != 0;
						AssignSignZeroParity(outcome, term);
						return action == 7 ? left : outcome & filter;
					}
				case 1:
					outcome = left | right;
					break;
				case 4:
					outcome = left & right;
					break;
				default:
					outcome = left ^ right;
					break;
			}

			AssignLogicFlags(outcome, term);
			return outcome & filter;
		}

		private int Increase(int amount, bool term)
		{
			bool carry = this.carryBit;
			int outcome = Arithmetic(0, amount, 1, term);
			this.carryBit = carry;
			return outcome;
		}

		private int Decrease(int amount, bool term)
		{
			bool carry = this.carryBit;
			int outcome = Arithmetic(5, amount, 1, term);
			this.carryBit = carry;
			return outcome;
		}

		private int ShiftOrRotate(int action, int amount, int tally, bool term)
		{
			int digits = term ? 16 : 8;
			int filter = term ? 0xffff : 0xff;
			int polarityBinary = term ? 0x8000 : 0x80;

			tally &= 0x1f;
			if (tally == 0)
				return amount;

			int outcome = amount & filter;

			for (int index = 0; index < tally; index++)
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
							bool carry = (outcome & polarityBinary) != 0;
							outcome = ((outcome << 1) | (this.carryBit ? 1 : 0)) & filter;
							this.carryBit = carry;
						}
						break;
					case 3:
						{
							bool carry = (outcome & 1) != 0;
							outcome = (outcome >> 1) | (this.carryBit ? polarityBinary : 0);
							this.carryBit = carry;
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
					AssignSignZeroParity(outcome, term);
					break;
				case 5:
					this.overflowBit = tally == 1 && (amount & polarityBinary) != 0;
					AssignSignZeroParity(outcome, term);
					break;
				default:
					this.overflowBit = false;
					AssignSignZeroParity(outcome, term);
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
			return new InvalidOperationException($"Sound driver: unsupported instruction 0x{instruction:x2} at {this.SegmentCode:x4}:{commandBegin:x4}");
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
			int advance = (term ? 2 : 1) * (this.directionBit ? -1 : 1);
			bool contrast = instruction == 0xa6 || instruction == 0xa7 || instruction == 0xae || instruction == 0xaf;

			while (true)
			{
				if (redo != 0 && this.RegisterCount == 0)
					return;

				switch (instruction)
				{
					case 0xa4: EmitByte(this.SegmentExtra, this.RegisterDestination, PullByte(ContentSection, this.RegisterSource)); this.RegisterSource = (ushort)(this.RegisterSource + advance); this.RegisterDestination = (ushort)(this.RegisterDestination + advance); break;
					case 0xa5: EmitWord(this.SegmentExtra, this.RegisterDestination, PullWord(ContentSection, this.RegisterSource)); this.RegisterSource = (ushort)(this.RegisterSource + advance); this.RegisterDestination = (ushort)(this.RegisterDestination + advance); break;
					case 0xa6: Arithmetic(7, PullByte(ContentSection, this.RegisterSource), PullByte(this.SegmentExtra, this.RegisterDestination), false); this.RegisterSource = (ushort)(this.RegisterSource + advance); this.RegisterDestination = (ushort)(this.RegisterDestination + advance); break;
					case 0xa7: Arithmetic(7, PullWord(ContentSection, this.RegisterSource), PullWord(this.SegmentExtra, this.RegisterDestination), true); this.RegisterSource = (ushort)(this.RegisterSource + advance); this.RegisterDestination = (ushort)(this.RegisterDestination + advance); break;
					case 0xaa: EmitByte(this.SegmentExtra, this.RegisterDestination, ObtainByteRegister(0)); this.RegisterDestination = (ushort)(this.RegisterDestination + advance); break;
					case 0xab: EmitWord(this.SegmentExtra, this.RegisterDestination, this.RegisterAccumulator); this.RegisterDestination = (ushort)(this.RegisterDestination + advance); break;
					case 0xac: AssignByteRegister(0, PullByte(ContentSection, this.RegisterSource)); this.RegisterSource = (ushort)(this.RegisterSource + advance); break;
					case 0xad: this.RegisterAccumulator = PullWord(ContentSection, this.RegisterSource); this.RegisterSource = (ushort)(this.RegisterSource + advance); break;
					case 0xae: Arithmetic(7, ObtainByteRegister(0), PullByte(this.SegmentExtra, this.RegisterDestination), false); this.RegisterDestination = (ushort)(this.RegisterDestination + advance); break;
					default: Arithmetic(7, this.RegisterAccumulator, PullWord(this.SegmentExtra, this.RegisterDestination), true); this.RegisterDestination = (ushort)(this.RegisterDestination + advance); break;
				}

				if (redo == 0)
					return;

				this.RegisterCount--;

				if (contrast && ((redo == 2 && !this.zeroBit) || (redo == 1 && this.zeroBit)))
					return;
			}
		}

		private void ThirdOpcodeGroup(bool term)
		{
			ParseModeRegisterOrMemory();

			switch (this.registerField)
			{
				case 0:
				case 1:
					if (term) Arithmetic(4, PullRegisterOrMemoryWord(), ConsumeWord(), true);
					else Arithmetic(4, PullRegisterOrMemoryByte(), ConsumeByte(), false);
					return;
				case 2:
					if (term) EmitRegisterOrMemoryWord((ushort)~PullRegisterOrMemoryWord());
					else EmitRegisterOrMemoryByte((byte)~PullRegisterOrMemoryByte());
					return;
				case 3:
					if (term) { int amount = PullRegisterOrMemoryWord(); EmitRegisterOrMemoryWord((ushort)Arithmetic(5, 0, amount, true)); this.carryBit = amount != 0; }
					else { int amount = PullRegisterOrMemoryByte(); EmitRegisterOrMemoryByte((byte)Arithmetic(5, 0, amount, false)); this.carryBit = amount != 0; }
					return;
				case 4:
					if (term)
					{
						uint outcome = (uint)this.RegisterAccumulator * PullRegisterOrMemoryWord();
						this.RegisterAccumulator = (ushort)outcome;
						this.RegisterData = (ushort)(outcome >> 16);
						this.carryBit = this.overflowBit = this.RegisterData != 0;
					}
					else
					{
						int outcome = ObtainByteRegister(0) * PullRegisterOrMemoryByte();
						this.RegisterAccumulator = (ushort)outcome;
						this.carryBit = this.overflowBit = (outcome & 0xff00) != 0;
					}
					return;
				case 5:
					if (term)
					{
						int outcome = (short)this.RegisterAccumulator * (short)PullRegisterOrMemoryWord();
						this.RegisterAccumulator = (ushort)outcome;
						this.RegisterData = (ushort)(outcome >> 16);
						this.carryBit = this.overflowBit = outcome != (short)outcome;
					}
					else
					{
						int outcome = (sbyte)ObtainByteRegister(0) * (sbyte)PullRegisterOrMemoryByte();
						this.RegisterAccumulator = (ushort)outcome;
						this.carryBit = this.overflowBit = outcome != (sbyte)outcome;
					}
					return;
				case 6:
					if (term)
					{
						uint denominator = PullRegisterOrMemoryWord();
						uint numerator = ((uint)this.RegisterData << 16) | this.RegisterAccumulator;
						if (denominator == 0 || numerator / denominator > 0xffff) { RaiseInterrupt(0); return; }
						this.RegisterAccumulator = (ushort)(numerator / denominator);
						this.RegisterData = (ushort)(numerator % denominator);
					}
					else
					{
						int denominator = PullRegisterOrMemoryByte();
						int numerator = this.RegisterAccumulator;
						if (denominator == 0 || numerator / denominator > 0xff) { RaiseInterrupt(0); return; }
						this.RegisterAccumulator = (ushort)(((numerator % denominator) << 8) | (numerator / denominator));
					}
					return;
				default:
					if (term)
					{
						int denominator = (short)PullRegisterOrMemoryWord();
						int numerator = (int)(((uint)this.RegisterData << 16) | this.RegisterAccumulator);
						if (denominator == 0) { RaiseInterrupt(0); return; }
						int division = numerator / denominator;
						if (division != (short)division) { RaiseInterrupt(0); return; }
						this.RegisterAccumulator = (ushort)division;
						this.RegisterData = (ushort)(numerator % denominator);
					}
					else
					{
						int denominator = (sbyte)PullRegisterOrMemoryByte();
						int numerator = (short)this.RegisterAccumulator;
						if (denominator == 0) { RaiseInterrupt(0); return; }
						int division = numerator / denominator;
						if (division != (sbyte)division) { RaiseInterrupt(0); return; }
						this.RegisterAccumulator = (ushort)(((numerator % denominator & 0xff) << 8) | (division & 0xff));
					}
					return;
			}
		}

		private void FifthOpcodeGroup(ushort commandBegin, byte instruction)
		{
			ParseModeRegisterOrMemory();

			switch (this.registerField)
			{
				case 0: EmitRegisterOrMemoryWord((ushort)Increase(PullRegisterOrMemoryWord(), true)); return;
				case 1: EmitRegisterOrMemoryWord((ushort)Decrease(PullRegisterOrMemoryWord(), true)); return;
				case 2:
					{
						ushort aim = PullRegisterOrMemoryWord();
						PutOnStack(this.registerInstruction);
						this.registerInstruction = aim;
					}
					return;
				case 3:
					{
						ushort displacement = PullWord(RealSection, this.realDisplacement);
						ushort section = PullWord(RealSection, (ushort)(this.realDisplacement + 2));
						PutOnStack(this.SegmentCode);
						PutOnStack(this.registerInstruction);
						this.SegmentCode = section;
						this.registerInstruction = displacement;
					}
					return;
				case 4: this.registerInstruction = PullRegisterOrMemoryWord(); return;
				case 5:
					{
						ushort displacement = PullWord(RealSection, this.realDisplacement);
						this.SegmentCode = PullWord(RealSection, (ushort)(this.realDisplacement + 2));
						this.registerInstruction = displacement;
					}
					return;
				case 6: PutOnStack(PullRegisterOrMemoryWord()); return;
				default: throw UnsupportedOpcode(commandBegin, instruction);
			}
		}
	}
}
