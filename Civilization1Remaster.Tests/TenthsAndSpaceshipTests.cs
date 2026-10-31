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
	/// The tenths of the census and of the spaceship (TenthsRules, WorldEvents.ClassicTenths) and the pictures of the spaceship
	/// parts (SpaceshipPictureRules, WorldEvents.ClassicSpaceshipPictures): off (the game) the remaster writes "4.5" for 45 and
	/// draws the parts, on (the tests) it writes "4.4" and reads the picture from part &lt;&lt; 11, as the translated code did.
	/// </summary>
	public class TenthsAndSpaceshipTests
	{
		[Theory]
		[InlineData(45, 4, 5)]
		[InlineData(20, 2, 0)]
		[InlineData(9, 0, 9)]
		[InlineData(137, 13, 7)]
		public void TenthsAreTheLastDigit(short tenths, short whole, short tenth)
		{
			Assert.Equal(whole, TenthsRules.Whole(tenths));
			Assert.Equal(tenth, TenthsRules.Tenth(tenths, classic: false));
		}

		[Fact]
		public void ClassicTenthsRepeatTheWholePart()
		{
			Assert.Equal(4, TenthsRules.Tenth(45, classic: true));
			Assert.Equal(13, TenthsRules.Tenth(137, classic: true));
		}

		[Fact]
		public void PicturesOfThePartsFollowEachOther()
		{
			Assert.Equal(0x682a, SpaceshipPictureRules.PictureAddress(0, classic: false));
			Assert.Equal(0x6830, SpaceshipPictureRules.PictureAddress(3, classic: false));
			Assert.Equal(0x0000, SpaceshipPictureRules.PictureAddress(0, classic: true));
			Assert.Equal(0x1800, SpaceshipPictureRules.PictureAddress(3, classic: true));
		}

		/// <summary>
		/// The census of the human player: the children of a family with their tenths, the same game shown with and without
		/// ClassicTenths ("2.2 children" against "2.6 children" in this game)
		/// </summary>
		[Fact]
		public void CensusWritesTheTenths()
		{
			string[] classic = CensusLine(classic: true).Split('.');
			string[] remaster = CensusLine(classic: false).Split('.');

			Assert.Equal(classic[0], classic[1]);
			Assert.Equal(classic[0], remaster[0]);
			Assert.NotEqual(remaster[0], remaster[1]);
			Assert.Single(remaster[1]);
		}

		/// <summary>The line of children of the census (the text stays in the buffer until the next game function)</summary>
		private static string CensusLine(bool classic)
		{
			using HeadlessGame game = Load();
			GameEngine engine = game.Engine;
			engine.TurnHooks.ClassicTenths = classic;

			HashSet<string> lines = [];
			Listen(engine, scope =>
			{
				string text = engine.Words.ToString();

				if (text.EndsWith(" children"))
					lines.Add(text[..^" children".Length]);
			});

			engine.Atlas.DisplayCensus(engine.World.PersonParticipantIdx);

			return Assert.Single(lines);
		}

		/// <summary>
		/// The spaceship screen of the human player with one component: the picture of the part. (The mass and the flight time
		/// are written one after the other without a game function between them, so their tenths are checked by the rule.)
		/// </summary>
		[Theory]
		[InlineData(false)]
		[InlineData(true)]
		public void SpaceshipDrawsTheParts(bool classic)
		{
			using HeadlessGame game = Load();
			GameEngine engine = game.Engine;
			engine.TurnHooks.ClassicTenths = classic;
			engine.TurnHooks.ClassicSpaceshipPictures = classic;
			short human = engine.World.PersonParticipantIdx;

			// The pictures of the parts cut out, then a component (kind 3) on its first place of the layout (table 0x375a: column,
			// row, kind; the grid of the player at 0x3772:0x16e0 + 0xb4 * player + 0xc * column + row)
			engine.RocketScreen.RenderSpaceScenery();
			ushort place = (ushort)(3 * Enumerable.Range(0, 0x80).First(idx => engine.Processor.Storage.PullUInt8(0x375a, (ushort)((3 * idx) + 2)) == 3));
			byte column = engine.Processor.Storage.PullUInt8(0x375a, place);
			byte row = engine.Processor.Storage.PullUInt8(0x375a, (ushort)(place + 1));
			byte kind = engine.Processor.Storage.PullUInt8(0x375a, (ushort)(place + 2));
			engine.Processor.Storage.EmitUInt8(0x3772, (ushort)(0x16e0 + (0xb4 * human) + (0xc * column) + row), kind);

			// The picture the screen cut out for the part (0x682a + 2 * part), and the word the classic code reads (part << 11)
			ushort cutOut = engine.Processor.PullGlobalWord((ushort)(0x682a + (2 * kind)));
			ushort picture = classic ? engine.Processor.PullGlobalWord((ushort)(kind << 11)) : cutOut;

			List<string> drawn = [];
			Listen(engine, scope =>
			{
				if (scope.StartsWith("RenderPictureToScreen("))
					drawn.Add(scope);
			});

			engine.RocketScreen.RenderSpaceship(human, -1, 0);

			// The part at its place of the screen, with the picture the screen cut out (classic: the word at part << 11)
			string part = $", {(column * 16) - 16}, {(row * 16) + 8}, 0x{picture:x4})";
			Assert.Contains(drawn, scope => scope.EndsWith(part));
			Assert.Equal(!classic, picture == cutOut);
		}

		private static HeadlessGame Load()
		{
			string? gameFolder = GoldenMasterTests.FindGameFolder();
			Assert.SkipWhen(gameFolder == null, "The CIVILIZATION game folder (the original game files) was not found");

			HeadlessGame game = new HeadlessGame(gameFolder!, Path.Combine(GoldenMasterTests.SourceFolder(), "Saves"));
			game.LoadAutosave(9, GoldenMasterTests.RandomSeed);
			return game;
		}

		/// <summary>Calls the action for every game function the engine enters, besides the listener of the test game</summary>
		private static void Listen(GameEngine engine, Action<string> action)
		{
			Action<string>? earlier = engine.Journal.ScopeListener;
			engine.Journal.ScopeListener = scope =>
			{
				earlier?.Invoke(scope);
				action(scope);
			};
		}
	}
}
