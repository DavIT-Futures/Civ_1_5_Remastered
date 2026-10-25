using System;
using System.IO;
using System.Linq;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;

namespace Civilization1Remaster.Tests.Dual;

/// <summary>
/// The projection of the data moved out of the emulated memory (MovedData.Project, B6.0): the snapshot writes it back at
/// its address of a copy of the data segment, and nothing else changes.
/// </summary>
public class MovedDataTests
{
	[Fact]
	public void ProjectionWritesTheMovedBytesAtTheirAddress()
	{
		string? gameFolder = GoldenMasterTests.FindGameFolder();
		Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

		using HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"));
		game.LoadAutosave(9, GoldenMasterTests.RandomSeed);
		GameEngine engine = game.Engine;

		byte[] memory = new byte[0x10000];
		Array.Copy(engine.Processor.Storage.StoragePayload, CPU.AsLinearAddress(GameSnapshot.DataSection, 0), memory, 0, memory.Length);

		// Data given as the memory has it: the copy stays the same
		byte[] same = (byte[])memory.Clone();
		MovedData.Project(engine, same, [new MovedData.Entry("same", 0x6548, 2, _ => memory[0x6548..0x654a])]);
		Assert.Equal(memory, same);

		// Other data: exactly its bytes change
		byte[] other = (byte[])memory.Clone();
		MovedData.Project(engine, other, [new MovedData.Entry("other", 0x6548, 2, _ => [(byte)(memory[0x6548] ^ 0xff), (byte)(memory[0x6549] ^ 0xff)])]);
		Assert.Equal(new[] { 0x6548, 0x6549 }, Enumerable.Range(0, memory.Length).Where(idx => memory[idx] != other[idx]));

		// The length must match
		Assert.Throws<InvalidOperationException>(() =>
			MovedData.Project(engine, (byte[])memory.Clone(), [new MovedData.Entry("short", 0x6548, 2, _ => [1])]));
	}

	/// <summary>B6.1: the message buffer is kept in C#, the memory under it stays empty, and the memory reads give the array</summary>
	[Fact]
	public void MessageBufferLivesInCSharp()
	{
		string? gameFolder = GoldenMasterTests.FindGameFolder();
		Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

		using HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"));
		game.LoadAutosave(9, GoldenMasterTests.RandomSeed);
		GameEngine engine = game.Engine;

		engine.Words.Set("Babylonians");
		engine.Words.Append(" develop Writing!");

		uint origin = CPU.AsLinearAddress(GameSnapshot.DataSection, TextBuffer.Address);
		Assert.All(engine.Processor.Storage.StoragePayload.Skip((int)origin).Take(TextBuffer.Size), value => Assert.Equal(0, value));

		Assert.Equal("Babylonians develop Writing!", engine.Words.ToString());
		Assert.Equal("Babylonians develop Writing!", System.Text.Encoding.ASCII.GetString(engine.Words.Bytes, 0, 28));
		Assert.Equal((byte)'B', engine.Processor.PullGlobalByte(TextBuffer.Address));
		Assert.Equal((ushort)('a' << 8 | 'B'), engine.Processor.PullGlobalWord(TextBuffer.Address));

		// A block of bytes copied into the buffer and over its end (EmitChunk): the part in the buffer goes to the array
		byte[] block = Enumerable.Range(1, 8).Select(value => (byte)value).ToArray();
		engine.Processor.Storage.EmitChunk(GameSnapshot.DataSection, TextBuffer.Address + TextBuffer.Size - 4, block, 0, block.Length);
		Assert.Equal(new byte[] { 1, 2, 3, 4 }, engine.Words.Bytes[^4..]);
		Assert.Equal(new byte[] { 5, 6, 7, 8 }, engine.Processor.Storage.StoragePayload[(int)(origin + TextBuffer.Size)..(int)(origin + TextBuffer.Size + 4)]);
		Assert.All(engine.Processor.Storage.StoragePayload.Skip((int)origin).Take(TextBuffer.Size), value => Assert.Equal(0, value));
	}
}
