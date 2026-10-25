using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;

namespace Civilization1Remaster.Tests
{
	/// <summary>
	/// A spaceship part of a kind without a free place (SpaceshipSlotRules, TurnEvents.ClassicSpaceshipSlots): off (the game)
	/// the kind is marked complete and the player told "No further ... required."; on (the tests) the part goes past the
	/// table of places, as the translated code did.
	/// </summary>
	public class SpaceshipSlotTests
	{
		[Fact]
		public void FullKindIsMarkedByItsDialogLine()
		{
			Assert.Equal(0, SpaceshipSlotRules.CompleteBit(3, 1, classic: false));
			Assert.Equal(1, SpaceshipSlotRules.CompleteBit(4, 1, classic: false));
			Assert.Equal(2, SpaceshipSlotRules.CompleteBit(8, 2, classic: false));
			Assert.Equal(8, SpaceshipSlotRules.CompleteBit(8, 2, classic: true));

			Assert.Equal(0x4bd0 + (3 * 0x1a), SpaceshipSlotRules.PartName(3, 1, classic: false));
			Assert.Equal(0x4bd0 + (6 * 0x1a), SpaceshipSlotRules.PartName(3, 1, classic: true));
		}

		[Fact]
		public void FullStructureSideGivesTheOtherSide()
		{
			Assert.Equal(5, SpaceshipSlotRules.StructureSlot(5, 0x30, classic: false));
			Assert.Equal(0x30, SpaceshipSlotRules.StructureSlot(0x63, 0x30, classic: false));
			Assert.Equal(SpaceshipSlotRules.NoSlot, SpaceshipSlotRules.StructureSlot(0x63, 0x63, classic: false));
			Assert.Equal(0x63, SpaceshipSlotRules.StructureSlot(0x63, 0x30, classic: true));
		}

		/// <summary>
		/// The human player gets a component while every place of the first component kind (3) is taken: Enter takes the
		/// first line, Enter closes the message, Escape leaves the dialog shown again
		/// </summary>
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void FullComponentKindIsComplete(bool classic)
		{
			string? gameFolder = GoldenMasterTests.FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			using HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"), [0x0d, 0x0d, 0x1b]);
			game.LoadAutosave(9, GoldenMasterTests.RandomSeed);
			GameEngine engine = game.Engine;
			engine.TurnHooks.ClassicSpaceshipSlots = classic;
			short human = engine.World.PersonParticipantIdx;
			game.Deadline = Environment.TickCount64 + 60_000;

			// Every place of kind 3 taken
			for (ushort placeIdx = 0; engine.Processor.Storage.PullUInt8(0x375a, (ushort)(3 * placeIdx)) != 0xff; placeIdx++)
			{
				ushort place = (ushort)(3 * placeIdx);

				if (engine.Processor.Storage.PullUInt8(0x375a, (ushort)(place + 2)) == 3)
				{
					int column = (sbyte)engine.Processor.Storage.PullUInt8(0x375a, place);
					int row = (sbyte)engine.Processor.Storage.PullUInt8(0x375a, (ushort)(place + 1));
					engine.Processor.Storage.EmitUInt8(0x3772, (ushort)((12 * column) + row + (0xb4 * human) + 0x16e0), 3);
				}
			}

			string name = engine.Processor.PullText(Civilization1Remaster.Emulation.CPU.AsLinearAddress(
				engine.Processor.SegData.Value16, SpaceshipSlotRules.PartName(3, 1, classic: false)));
			Assert.False(string.IsNullOrWhiteSpace(name));

			// The texts of the dialogs
			List<string> dialogs = [];
			Action<string>? earlier = engine.Journal.ScopeListener;
			engine.Journal.ScopeListener = scope =>
			{
				earlier?.Invoke(scope);

				if (scope.StartsWith("DisplayDialog("))
					dialogs.Add(engine.Words.ToString());
			};

			engine.RocketScreen.AttachSpaceshipPart(human, 1);

			Assert.Equal(!classic, (engine.Globals.SpaceshipComponentsComplete.Value & 1) != 0);
			Assert.Equal(!classic, dialogs.Contains($"No further {name} required.\n"));
		}
	}
}
