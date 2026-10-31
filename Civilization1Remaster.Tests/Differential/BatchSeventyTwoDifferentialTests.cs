using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Civilization1Remaster.Tests.GoldenMaster;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchSeventyTwoDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void DisplayCivilizationScreenMatchesOriginalCode()
	{
		int[] keys = [0x20, 0x0d];
		int step = 0;

		Check(1, 4, (chance, pair) =>
		{
			int mode = (step++) % 4;
			short participantIdx = (mode & 1) == 0 ? (short)1 : (short)2;
			ushort isVga = (mode & 2) == 0 ? (ushort)1 : (ushort)0;
			int palaces = Calls(pair.Rewritten.Engine, pair, "ImportPalaceGraphics");
			int cycles = Calls(pair.Rewritten.Engine, pair, "ActivateHueCycle");
			int sceneries = Calls(pair.Rewritten.Engine, pair, "DisplayCivilizationScenery");
			int dots = Calls(pair.Rewritten.Engine, pair, "AssignDot");

			return new Scenario(
				$"DisplayCivilizationScreen(participant={participantIdx}, vga={isVga})",
				game =>
				{
					game.World.PersonParticipantIdx = 1;
					game.World.Participants[1].StarshipInhabitants = 20000;
					game.World.Participants[1].StarshipLiftoffSeason = 2000;
					game.World.Participants[2].StarshipInhabitants = 15000;
					game.World.Participants[2].StarshipLiftoffSeason = 2010;
					game.World.Season = 2020;
					game.Processor.EmitGlobalWord(0xd762, isVga);
					game.VideoCard.ReserveScreen(3);
				},
				game => new IntroSequenceReference(game).ShowCivilizationScreen(participantIdx),
				game => game.Prologue.ShowCivilizationScreen(participantIdx),
				Register.Kept,
				// What the function did: the landing of the human player (its colour cycles with VGA) or the palace of
				// another player, and the dots of the flight (more without VGA, where every column shows screen 0 with one)
				game =>
				{
					if (Calls(game, pair, "DisplayCivilizationScenery") == sceneries)
						return "did not finish";

					string landing = Calls(game, pair, "ImportPalaceGraphics") > palaces ? "palace"
						: Calls(game, pair, "ActivateHueCycle") > cycles ? "landing with cycles" : "landing";

					return landing + (Calls(game, pair, "AssignDot") - dots > 320 ? ", cga" : ", vga");
				});
		}, output, ["landing with cycles, vga", "palace, vga", "landing, cga", "palace, cga"], keys);
	}

	[Fact]
	public void DisplayMainIntroMatchesOriginalCode()
	{
		int[] keys = [0x20, 0x4e, 0x0d, 0x20];
		int step = 0;

		// The driver (VGA / CGA), and how the keys come: none while the intro only looks for one (it plays to the end and
		// shows the logo), from the script (the first look finds one), a key already waiting (a choice of the menu), or a
		// key typed after some looks (100: in the middle of the credits, 450: in the second part)
		(bool Vga, int Key, string Keys)[] modes =
		[
			(true, 0, "none"), (false, 0, "none"), (true, 0, "script"), (false, 0, "script"),
			(true, 'N', "waiting"), (false, 'l', "waiting"), (true, 'E', "waiting"), (true, 'c', "waiting"), (false, 'x', "waiting"),
			(true, 'E', "100"), (false, 'C', "450"),
		];

		Check(1, modes.Length, (chance, pair) =>
		{
			(bool vga, int key, string keysMode) = modes[step++ % modes.Length];
			int cycles = Calls(pair.Rewritten.Engine, pair, "ActivateHueCycle");
			int choices = Calls(pair.Rewritten.Engine, pair, "ObtainNavigationKey");
			int logos = Calls(pair.Rewritten.Engine, pair, "AwaitForKey");
			int boxes = Calls(pair.Rewritten.Engine, pair, "FloodBox");
			int releases = Calls(pair.Rewritten.Engine, pair, "ReleaseResource");

			return new Scenario(
				$"DisplayMainIntro(vga={vga}, keys={keysMode}, key={key:x2})",
				game =>
				{
					ScriptedKeyboard keyboard = game == pair.Original.Engine ? pair.Original.Keyboard : pair.Rewritten.Keyboard;

					game.World.PersonParticipantIdx = 1;
					// The intro seeds the random generator from the wall clock (time() in seconds): one moment for both games
					game.Libc.Clock = () => new DateTime(2026, 10, 16, 12, 0, 0);
					game.Processor.EmitGlobalByte(0x1a22, vga ? (byte)0x4d : (byte)0x43);
					game.VideoCard.ReserveScreen(3);

					lock (game.VideoCard.Tokens)
					{
						game.VideoCard.Tokens.Clear();

						if (keysMode == "waiting")
							game.VideoCard.Tokens.Enqueue(key);
					}

					// Without keys while the game only looks for one (or until the look given), a key only when it waits for it
					int looks = 0;
					int lastLook = keysMode == "none" ? int.MaxValue : int.TryParse(keysMode, out int typedAt) ? typedAt : -1;

					game.Processor.InputStarved = lastLook < 0 ? keyboard.OnInputStarved : waitingForKey =>
					{
						if (waitingForKey)
						{
							keyboard.OnInputStarved(true);
							return;
						}

						game.Processor.Idle();

						// The screens of every column go into the trace (the credits and the timing of the scrolling are seen)
						keyboard.KeyTyping?.Invoke();

						if (++looks == lastLook)
						{
							lock (game.VideoCard.Tokens)
								game.VideoCard.Tokens.Enqueue(key);
						}
					};
				},
				game => new IntroSequenceReference(game).ShowMainIntro(),
				game => game.Prologue.ShowMainIntro(),
				Register.Kept,
				// What the function did, from its calls and the choice it left in 0x6b32
				game =>
				{
					if (Calls(game, pair, "ReleaseResource") == releases)
						return "did not finish";

					string driver = Calls(game, pair, "ActivateHueCycle") > cycles ? "vga" : "cga";
					string credits = Calls(game, pair, "FloodBox") - boxes > 300 ? "credits to the end" : "credits cut";
					string end = Calls(game, pair, "AwaitForKey") > logos ? "logo"
						: Calls(game, pair, "ObtainNavigationKey") > choices ? $"choice {(short)game.Processor.Storage.PullUInt16(0x3b01, 0x6b32)}" : "nothing";

					return $"{driver}, {credits}, {end}";
				});
		}, output,
		// A key that cuts the intro short stays in the buffer, so it always chooses (a key that is not N, L, E or C: -1)
		[
			"vga, credits to the end, logo", "cga, credits to the end, logo",
			"vga, credits cut, choice -1", "cga, credits cut, choice 0", "vga, credits cut, choice 0", "cga, credits cut, choice 1",
			"vga, credits cut, choice 2", "vga, credits cut, choice 3", "cga, credits cut, choice -1", "cga, credits to the end, choice 3",
		], keys);
	}
}
