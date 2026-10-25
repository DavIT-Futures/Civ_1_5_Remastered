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

	/// <summary>B6.2: the units are kept in C#: the memory under them stays empty, UnitMemory and the memory reads agree</summary>
	[Fact]
	public void UnitsLiveInCSharp()
	{
		string? gameFolder = GoldenMasterTests.FindGameFolder();
		Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

		using HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"));
		game.LoadAutosave(9, GoldenMasterTests.RandomSeed);
		GameEngine engine = game.Engine;
		Civilization1Remaster.Game.UnitMemory troops = new Civilization1Remaster.Game.UnitMemory(engine.Processor);

		int size = Civilization1Remaster.Game.UnitMemory.ParticipantStride * 8;
		uint origin = CPU.AsLinearAddress(GameSnapshot.DataSection, Civilization1Remaster.Game.UnitMemory.Origin);
		Assert.All(engine.Processor.Storage.StoragePayload.Skip((int)origin).Take(size), value => Assert.Equal(0, value));

		// The loaded game has units, the same through UnitMemory and through the memory
		byte[] records = engine.UnitRecords;
		Assert.Equal(size, records.Length);
		Assert.Contains(records, value => value != 0 && value != 0xff);

		for (int player = 0; player < 8; player++)
		{
			for (int unit = 0; unit < 0x80; unit += 7)
			{
				ushort address = Civilization1Remaster.Game.UnitMemory.Address(player, unit);
				Assert.Equal(engine.Processor.PullGlobalByte((ushort)(address + 3)), troops.Pull(player, unit, 3));
			}
		}

		// A write through UnitMemory is seen through the memory, and the other way round
		troops.Assign(1, 5, 1, 0x2a);
		Assert.Equal(0x2a, engine.Processor.PullGlobalByte((ushort)(Civilization1Remaster.Game.UnitMemory.Address(1, 5) + 1)));
		engine.Processor.EmitGlobalByte((ushort)(Civilization1Remaster.Game.UnitMemory.Address(2, 6) + 2), 0x17);
		Assert.Equal(0x17, troops.Pull(2, 6, 2));
		Assert.All(engine.Processor.Storage.StoragePayload.Skip((int)origin).Take(size), value => Assert.Equal(0, value));
	}

	/// <summary>B6.3 / B6.4 / B6.5: the tables of CIV.EXE, the named globals and the boxes of the screens are kept in C#, the memory under them stays empty</summary>
	[Fact]
	public void TablesAndGlobalsLiveInCSharp()
	{
		string? gameFolder = GoldenMasterTests.FindGameFolder();
		Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

		using HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"));
		game.LoadAutosave(9, GoldenMasterTests.RandomSeed);
		GameEngine engine = game.Engine;
		byte[] payload = engine.Processor.Storage.StoragePayload;

		// Every moved range: nothing in the memory block
		foreach (MovedRanges.Range range in MovedRanges.All)
		{
			uint origin = CPU.AsLinearAddress(GameSnapshot.DataSection, range.Address);
			Assert.All(payload.Skip((int)origin).Take(range.Length), value => Assert.Equal(0, value));
		}

		// The tables: the C# bytes are what the memory gives (the name of the first terrain, "Desert")
		Assert.Equal("Desert", System.Text.Encoding.ASCII.GetString(engine.Tables.Content, 0, 6));
		Assert.Equal(engine.Processor.PullGlobalWord(0x1882 + 2), engine.Tables.Word(0x1882 + 2));

		// The boxes of the screens: each one points at itself from the word after it, the typeface of the drawing box
		for (int boxIdx = 0; boxIdx < ScreenBoxes.Addresses.Length; boxIdx++)
			Assert.Equal(ScreenBoxes.Addresses[boxIdx], engine.Processor.PullGlobalWord((ushort)(ScreenBoxes.Addresses[boxIdx] + 0x12)));

		Assert.Equal(engine.Processor.PullGlobalWord(0x98 + 0x10), (ushort)(engine.Boxes.Content[0][0x10] | (engine.Boxes.Content[0][0x11] << 8)));

		// A global: the field and the memory are one, both ways
		engine.Processor.EmitGlobalWord(engine.Globals.CityOwner.Address, 0x1234);
		Assert.Equal(0x1234, engine.Globals.CityOwner.Value);
		engine.Globals.CityOwner.Value = 0x0506;
		Assert.Equal(0x0506, engine.Processor.PullGlobalWord(engine.Globals.CityOwner.Address));
		Assert.Equal(0x06, engine.Processor.PullGlobalByte(engine.Globals.CityOwner.Address));
		engine.Processor.EmitGlobalByte((ushort)(engine.Globals.CityOwner.Address + 1), 0x07);
		Assert.Equal(0x0706, engine.Globals.CityOwner.Value);
	}

	/// <summary>B6.2: a saved game has the units of the C# array, and loading it puts them back there (and the names, B6.3)</summary>
	[Fact]
	public void SavedGameKeepsTheUnits()
	{
		string? gameFolder = GoldenMasterTests.FindGameFolder();
		Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

		using HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"));
		game.LoadAutosave(9, GoldenMasterTests.RandomSeed);
		GameEngine engine = game.Engine;
		Civilization1Remaster.Game.UnitMemory troops = new Civilization1Remaster.Game.UnitMemory(engine.Processor);

		byte[] saved = engine.UnitRecords;

		// The names of the leaders, civilizations (plural) and civilizations: the texts at the pointers 0x19a2 / 0x1992 / 0x1982
		(ushort Pointer, int Length)[] nameBlocks = [(0x19a2, 0x70), (0x1992, 0x60), (0x1982, 0x58)];
		byte[][] names = nameBlocks.Select(block => Enumerable.Range(0, block.Length)
			.Select(idx => engine.Processor.PullGlobalByte((ushort)(engine.Processor.PullGlobalWord(block.Pointer) + idx))).ToArray()).ToArray();
		Assert.True(engine.SaveLoad.StoreGameContent("B62TEST.SVE"));

		// Other units meanwhile, then the saved game again
		for (int player = 0; player < 8; player++)
		{
			for (int unit = 0; unit < 0x80; unit++)
				troops.Assign(player, unit, 3, 0x55);
		}

		foreach ((ushort pointer, int length) in nameBlocks)
		{
			for (int idx = 0; idx < length; idx++)
				engine.Processor.EmitGlobalByte((ushort)(engine.Processor.PullGlobalWord(pointer) + idx), (byte)'x');
		}

		Assert.True(engine.SaveLoad.ImportGameContent("B62TEST.SVE"));
		Assert.Equal(saved, engine.UnitRecords);

		// Other names meanwhile would not be kept: the saved ones come back each to its place
		for (int blockIdx = 0; blockIdx < nameBlocks.Length; blockIdx++)
		{
			ushort start = engine.Processor.PullGlobalWord(nameBlocks[blockIdx].Pointer);
			Assert.Equal(names[blockIdx], Enumerable.Range(0, nameBlocks[blockIdx].Length)
				.Select(idx => engine.Processor.PullGlobalByte((ushort)(start + idx))).ToArray());
		}
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
