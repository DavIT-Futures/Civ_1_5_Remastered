using System.Drawing;
using Civilization1Remaster.Emulation;
using Xunit;

namespace Civilization1Remaster.Tests
{
	/// <summary>
	/// The variable access helpers of the processor must do exactly what the instruction sequences they replaced did.
	/// Every test runs both on the same random processor state and compares the results and the whole memory.
	/// </summary>
	public class ProcessorVariableTests : IDisposable
	{
		// Every processor starts a clock thread, which runs until the processor is told that the application quits
		private readonly List<CPU> created = new List<CPU>();

		public void Dispose()
		{
			foreach (CPU processor in this.created)
			{
				processor.WhenApplicationQuits();
			}
		}

		private sealed class QuietHost : IMachineHost
		{
			public Point PointerSite => Point.Empty;
			public ushort PointerClicks => 0;
			public Queue<int> InputTokens { get; } = new Queue<int>();
			public object InputLatch { get; } = new object();
			public LogWrapper IrqJournal { get; } = new LogWrapper("irq.log");
			public void OnPointerEvent() { }
			public void OnClockInterrupt() { }
			public void ReadKeyBlocking() { }
		}

		private CPU CreateProcessor(Random random)
		{
			CPU processor = new CPU(new QuietHost(), new LogWrapper("test.log"));
			this.created.Add(processor);
			random.NextBytes(processor.Storage.StoragePayload.AsSpan(0, 0x80000));
			processor.SegStack.Value16 = (ushort)random.Next(0x1000, 0x3000);
			processor.SegData.Value16 = (ushort)random.Next(0x1000, 0x3000);
			processor.RegFrame.Value16 = (ushort)random.Next(0x400, 0xf000);
			return processor;
		}

		/// <summary>An identical copy of the processor state, so two sequences can run side by side</summary>
		private CPU Copy(CPU original, Random random)
		{
			CPU copy = new CPU(new QuietHost(), new LogWrapper("test.log"));
			this.created.Add(copy);
			original.Storage.StoragePayload.AsSpan().CopyTo(copy.Storage.StoragePayload);
			copy.SegStack.Value16 = original.SegStack.Value16;
			copy.SegData.Value16 = original.SegData.Value16;
			copy.RegFrame.Value16 = original.RegFrame.Value16;
			copy.RegStack.Value16 = original.RegStack.Value16;
			return copy;
		}

		private static void AssertSameState(CPU expected, CPU actual)
		{
			Assert.True(expected.Storage.StoragePayload.AsSpan().SequenceEqual(actual.Storage.StoragePayload), "memory differs");
			Assert.Equal(expected.FlagSet.Datum, actual.FlagSet.Datum);
			Assert.Equal(expected.RegFrame.Value16, actual.RegFrame.Value16);
			Assert.Equal(expected.RegStack.Value16, actual.RegStack.Value16);
			Assert.Equal(expected.SegStack.Value16, actual.SegStack.Value16);
			Assert.Equal(expected.SegData.Value16, actual.SegData.Value16);
		}

		[Fact]
		public void LocalVariablesAreReadFromBelowTheFramePointer()
		{
			Random random = new Random(11);
			for (int trial = 0; trial < 40; trial++)
			{
				CPU processor = CreateProcessor(random);
				int distance = random.Next(1, 0x250);
				ushort offset = (ushort)(processor.RegFrame.Value16 - distance);

				Assert.Equal(processor.PullUInt8(processor.SegStack.Value16, offset), processor.PullLocalByte(distance));
				Assert.Equal(processor.PullInt8(processor.SegStack.Value16, offset), processor.PullLocalSignedByte(distance));
				Assert.Equal(processor.PullUInt16(processor.SegStack.Value16, offset), processor.PullLocalWord(distance));
				Assert.Equal(processor.PullInt16(processor.SegStack.Value16, offset), processor.PullLocalSignedWord(distance));
			}
		}

		[Fact]
		public void LocalVariablesAreWrittenBelowTheFramePointer()
		{
			Random random = new Random(12);
			for (int trial = 0; trial < 40; trial++)
			{
				CPU original = CreateProcessor(random);
				CPU helper = Copy(original, random);
				int distance = random.Next(1, 0x250);
				ushort offset = (ushort)(original.RegFrame.Value16 - distance);
				ushort word = (ushort)random.Next(0x10000);

				original.EmitUInt8(original.SegStack.Value16, offset, (byte)word);
				helper.EmitLocalByte(distance, (byte)word);
				AssertSameState(original, helper);

				original.EmitUInt16(original.SegStack.Value16, offset, word);
				helper.EmitLocalWord(distance, word);
				AssertSameState(original, helper);

				original.EmitInt16(original.SegStack.Value16, offset, (short)word);
				helper.EmitLocalSignedWord(distance, (short)word);
				AssertSameState(original, helper);
			}
		}

		[Fact]
		public void GlobalVariablesAreReadFromTheDataSegment()
		{
			Random random = new Random(13);
			for (int trial = 0; trial < 40; trial++)
			{
				CPU processor = CreateProcessor(random);
				ushort address = (ushort)random.Next(0x10000);

				Assert.Equal(processor.PullUInt8(processor.SegData.Value16, address), processor.PullGlobalByte(address));
				Assert.Equal(processor.PullInt8(processor.SegData.Value16, address), processor.PullGlobalSignedByte(address));
				Assert.Equal(processor.PullUInt16(processor.SegData.Value16, address), processor.PullGlobalWord(address));
				Assert.Equal(processor.PullInt16(processor.SegData.Value16, address), processor.PullGlobalSignedWord(address));
			}
		}

		[Fact]
		public void GlobalVariablesAreWrittenToTheDataSegment()
		{
			Random random = new Random(14);
			for (int trial = 0; trial < 40; trial++)
			{
				CPU original = CreateProcessor(random);
				CPU helper = Copy(original, random);
				ushort address = (ushort)random.Next(0x10000);
				ushort word = (ushort)random.Next(0x10000);

				original.EmitUInt8(original.SegData.Value16, address, (byte)word);
				helper.EmitGlobalByte(address, (byte)word);
				AssertSameState(original, helper);

				original.EmitUInt16(original.SegData.Value16, address, word);
				helper.EmitGlobalWord(address, word);
				AssertSameState(original, helper);

				original.EmitInt16(original.SegData.Value16, address, (short)word);
				helper.EmitGlobalSignedWord(address, (short)word);
				AssertSameState(original, helper);
			}
		}

		[Fact]
		public void FramesAreOpenedAndClosedLikeTheInstructionsTheyReplace()
		{
			Random random = new Random(15);
			for (int trial = 0; trial < 40; trial++)
			{
				ushort size = (ushort)random.Next(0, 0x300);

				// start of a function without local variables
				CPU original = CreateProcessor(random);
				original.RegStack.Value16 = (ushort)random.Next(0x400, 0xf000);
				CPU helper = Copy(original, random);
				original.StackWord(original.RegFrame.Value16);
				original.RegFrame.Value16 = original.RegStack.Value16;
				helper.EnterFrame();
				AssertSameState(original, helper);

				// start of a function with local variables
				original = CreateProcessor(random);
				original.RegStack.Value16 = (ushort)random.Next(0x400, 0xf000);
				helper = Copy(original, random);
				original.StackWord(original.RegFrame.Value16);
				original.RegFrame.Value16 = original.RegStack.Value16;
				original.RegStack.Value16 = original.SubtractWord(original.RegStack.Value16, size);

				// the local variables start at 0
				for (int place = 0; place < size; place++)
					original.Storage.EmitUInt8(original.SegStack.Value16, (ushort)(original.RegStack.Value16 + place), 0);

				helper.EnterFrame(size);
				AssertSameState(original, helper);

				// end of a function
				original = CreateProcessor(random);
				original.RegStack.Value16 = (ushort)random.Next(0x400, 0xf000);
				helper = Copy(original, random);
				original.RegStack.Value16 = original.RegFrame.Value16;
				original.RegFrame.Value16 = original.UnstackWord();
				helper.LeaveFrame();
				AssertSameState(original, helper);
			}
		}

		private static void RandomizeRegisters(CPU processor, Random random)
		{
			processor.RegAcc.Value16 = (ushort)random.Next(0x10000);
			processor.RegData.Value16 = (ushort)random.Next(0x10000);
			processor.RegSource.Value16 = (ushort)random.Next(0x10000);
			processor.RegBase.Value16 = (ushort)random.Next(0x10000);
			processor.FlagSet.Datum = (ushort)random.Next(0x10000);
		}

		private static void AssertSameRegisters(CPU expected, CPU actual)
		{
			Assert.Equal(expected.RegAcc.Value16, actual.RegAcc.Value16);
			Assert.Equal(expected.RegData.Value16, actual.RegData.Value16);
			Assert.Equal(expected.RegSource.Value16, actual.RegSource.Value16);
			Assert.Equal(expected.RegBase.Value16, actual.RegBase.Value16);
			Assert.Equal(expected.FlagSet.Datum, actual.FlagSet.Datum);
		}

		private static void AssertSameRegisterState(CPU expected, CPU actual)
		{
			AssertSameState(expected, actual);
			AssertSameRegisters(expected, actual);
		}

		private CPU CopyWithRegisters(CPU original, Random random)
		{
			CPU copy = Copy(original, random);
			copy.RegAcc.Value16 = original.RegAcc.Value16;
			copy.RegData.Value16 = original.RegData.Value16;
			copy.RegSource.Value16 = original.RegSource.Value16;
			copy.RegBase.Value16 = original.RegBase.Value16;
			copy.FlagSet.Datum = original.FlagSet.Datum;
			return copy;
		}

		[Fact]
		public void TableOffsetsAreScaledLikeTheInstructionsTheyReplace()
		{
			Random random = new Random(16);
			for (int trial = 0; trial < 40; trial++)
			{
				ushort size = (ushort)random.Next(1, 0x700), index = (ushort)random.Next(0, 0x400);

				CPU original = CreateProcessor(random); RandomizeRegisters(original, random);
				CPU helper = CopyWithRegisters(original, random);
				original.RegAcc.Value16 = size;
				original.MultiplySignedWord(original.RegAcc, original.RegData, index);
				helper.ScaleIndex(size, index);
				AssertSameRegisterState(original, helper);

				original = CreateProcessor(random); RandomizeRegisters(original, random);
				helper = CopyWithRegisters(original, random);
				original.RegAcc.Value16 = size;
				original.MultiplySignedWord(original.RegAcc, original.RegData, index);
				original.RegSource.Value16 = original.RegAcc.Value16;
				helper.ScaleIndexToSource(size, index);
				AssertSameRegisterState(original, helper);

				original = CreateProcessor(random); RandomizeRegisters(original, random);
				helper = CopyWithRegisters(original, random);
				original.RegAcc.Value16 = size;
				original.MultiplySignedWord(original.RegAcc, original.RegData, index);
				original.RegBase.Value16 = original.RegAcc.Value16;
				helper.ScaleIndexToBase(size, index);
				AssertSameRegisterState(original, helper);

				original = CreateProcessor(random); RandomizeRegisters(original, random);
				helper = CopyWithRegisters(original, random);
				original.RegAcc.Value16 = size;
				original.MultiplySignedWord(original.RegAcc, original.RegData, index);
				original.RegSource.Value16 = original.SumWord(original.RegSource.Value16, original.RegAcc.Value16);
				helper.AddScaledToSource(size, index);
				AssertSameRegisterState(original, helper);

				original = CreateProcessor(random); RandomizeRegisters(original, random);
				helper = CopyWithRegisters(original, random);
				original.MultiplySignedWord(original.RegAcc, original.RegData, index);
				original.RegSource.Value16 = original.RegAcc.Value16;
				helper.ScaleAccumulatorToSource(index);
				AssertSameRegisterState(original, helper);

				original = CreateProcessor(random); RandomizeRegisters(original, random);
				helper = CopyWithRegisters(original, random);
				original.MultiplySignedWord(original.RegAcc, original.RegData, index);
				original.RegBase.Value16 = original.RegAcc.Value16;
				helper.ScaleAccumulatorToBase(index);
				AssertSameRegisterState(original, helper);
			}
		}

		[Fact]
		public void CountersAndCopiesOfLocalVariablesMatchTheirInstructions()
		{
			Random random = new Random(17);
			for (int trial = 0; trial < 40; trial++)
			{
				int distance = random.Next(1, 0x250), other = random.Next(1, 0x250);

				CPU original = CreateProcessor(random); RandomizeRegisters(original, random);
				CPU helper = CopyWithRegisters(original, random);
				original.EmitLocalWord(distance, original.IncrementWord(original.PullLocalWord(distance)));
				helper.IncrementLocalWord(distance);
				AssertSameRegisterState(original, helper);

				original = CreateProcessor(random); RandomizeRegisters(original, random);
				helper = CopyWithRegisters(original, random);
				original.EmitLocalWord(distance, original.DecrementWord(original.PullLocalWord(distance)));
				helper.DecrementLocalWord(distance);
				AssertSameRegisterState(original, helper);

				original = CreateProcessor(random); RandomizeRegisters(original, random);
				helper = CopyWithRegisters(original, random);
				original.RegAcc.Value16 = original.PullLocalWord(distance);
				original.EmitLocalWord(other, original.RegAcc.Value16);
				helper.CopyLocalWord(distance, other);
				AssertSameRegisterState(original, helper);
			}
		}

		[Fact]
		public void SavedRegistersMatchPushAndPopInstructions()
		{
			Random random = new Random(18);
			for (int trial = 0; trial < 40; trial++)
			{
				CPU original = CreateProcessor(random); RandomizeRegisters(original, random);
				original.RegDest.Value16 = (ushort)random.Next(0x10000);
				original.RegStack.Value16 = (ushort)random.Next(0x400, 0xf000);
				CPU helper = CopyWithRegisters(original, random);
				helper.RegDest.Value16 = original.RegDest.Value16;
				original.StackWord(original.RegDest.Value16);
				original.StackWord(original.RegSource.Value16);
				helper.PreserveIndexRegisters();
				AssertSameRegisterState(original, helper);

				original.RegDest.Value16 = 0; original.RegSource.Value16 = 0;
				helper.RegDest.Value16 = 0; helper.RegSource.Value16 = 0;
				original.RegSource.Value16 = original.UnstackWord();
				original.RegDest.Value16 = original.UnstackWord();
				helper.RestoreIndexRegisters();
				AssertSameRegisterState(original, helper);
				Assert.Equal(original.RegDest.Value16, helper.RegDest.Value16);

				original = CreateProcessor(random); RandomizeRegisters(original, random);
				original.RegStack.Value16 = (ushort)random.Next(0x400, 0xf000);
				helper = CopyWithRegisters(original, random);
				original.StackWord(original.RegSource.Value16);
				helper.PreserveSource();
				AssertSameRegisterState(original, helper);
				original.RegSource.Value16 = original.UnstackWord();
				helper.RestoreSource();
				AssertSameRegisterState(original, helper);
			}
		}

		[Fact]
		public void AccumulatorArithmeticMatchesItsInstructions()
		{
			Random random = new Random(19);
			for (int trial = 0; trial < 40; trial++)
			{
				ushort shift = (ushort)random.Next(0, 15);
				CPU original = CreateProcessor(random); RandomizeRegisters(original, random);
				original.RegCount.Value16 = (ushort)random.Next(0x10000);
				CPU helper = CopyWithRegisters(original, random);
				helper.RegCount.Value16 = original.RegCount.Value16;

				original.RegAcc.Value16 = original.BitOrWord(original.RegAcc.Value16, original.RegAcc.Value16);
				helper.TestAccumulator();
				AssertSameRegisterState(original, helper);

				original.SignExtendWord(original.RegAcc, original.RegData);
				original.RegAcc.Value16 = original.BitXorWord(original.RegAcc.Value16, original.RegData.Value16);
				original.RegAcc.Value16 = original.SubtractWord(original.RegAcc.Value16, original.RegData.Value16);
				helper.AbsoluteAccumulator();
				AssertSameRegisterState(original, helper);

				original = CreateProcessor(random); RandomizeRegisters(original, random);
				helper = CopyWithRegisters(original, random);
				original.SignExtendWord(original.RegAcc, original.RegData);
				original.RegAcc.Value16 = original.SubtractWord(original.RegAcc.Value16, original.RegData.Value16);
				original.RegAcc.Value16 = original.ShiftRightSignedWord(original.RegAcc.Value16, 0x1);
				helper.HalveAccumulator();
				AssertSameRegisterState(original, helper);

				original = CreateProcessor(random); RandomizeRegisters(original, random);
				original.RegCount.Value16 = (ushort)random.Next(0x10000);
				helper = CopyWithRegisters(original, random);
				helper.RegCount.Value16 = original.RegCount.Value16;
				original.SignExtendWord(original.RegAcc, original.RegData);
				original.RegAcc.Value16 = original.BitXorWord(original.RegAcc.Value16, original.RegData.Value16);
				original.RegAcc.Value16 = original.SubtractWord(original.RegAcc.Value16, original.RegData.Value16);
				original.RegCount.Value16 = shift;
				original.RegAcc.Value16 = original.ShiftRightSignedWord(original.RegAcc.Value16, original.RegCount.LowByte);
				original.RegAcc.Value16 = original.BitXorWord(original.RegAcc.Value16, original.RegData.Value16);
				original.RegAcc.Value16 = original.SubtractWord(original.RegAcc.Value16, original.RegData.Value16);
				helper.DivideAccumulatorByPowerOfTwo(shift);
				AssertSameRegisterState(original, helper);
				Assert.Equal(original.RegCount.Value16, helper.RegCount.Value16);
			}
		}

		[Fact]
		public void SignedQuotientDividesLikeTheInstructions()
		{
			Random random = new Random(17);
			CPU processor = CreateProcessor(random);
			short[] edges = { 0, 1, -1, 2, -2, 7, -7, 32000, short.MaxValue, short.MinValue };

			for (int trial = 0; trial < 4000; trial++)
			{
				short dividend = trial < 100 ? edges[trial % 10] : (short)random.Next(0x10000);
				short divisor = trial < 100 ? edges[trial / 10] : (short)random.Next(0x10000);
				string expected, actual;

				try
				{
					processor.RegAcc.Value16 = (ushort)dividend;
					processor.SignExtendWord(processor.RegAcc, processor.RegData);
					processor.DivideSignedWord(processor.RegAcc, processor.RegData, (ushort)divisor);
					expected = processor.RegAcc.Value16.ToString();
				}
				catch (Exception failure)
				{
					expected = failure.Message;
				}

				try
				{
					actual = ((ushort)CPU.SignedQuotient(dividend, divisor)).ToString();
				}
				catch (Exception failure)
				{
					actual = failure.Message;
				}

				Assert.True(expected == actual, $"{dividend} / {divisor}: {expected} -> {actual}");
			}
		}
	}
}
