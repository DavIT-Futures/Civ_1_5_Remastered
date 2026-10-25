using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Engine;

namespace Civilization1Remaster.Tests.GoldenMaster
{
	/// <summary>
	/// Runs the emulated game without a window, sound or real time: it loads an autosave, seeds the random generator with a
	/// fixed value and plays whole turns, the human player is played by a scripted keyboard (ScriptedKeyboard).
	/// </summary>
	/// <remarks>
	/// The start does what GameStartup.RunGame does before the main loop, without the intro and the menus:
	/// the parts of MainIntro.DisplayMainIntro that set up the machine, then the steps of GameStartup.ImportGameDuringPlay
	/// (Load Game during play). The game runs on a copy of the game folder, without the sound drivers (*.CVL),
	/// because every turn writes an autosave; the saved games of the test (savesFolder) replace the ones of the player.
	/// </remarks>
	public sealed class HeadlessGame : IDisposable
	{
		/// <summary>
		/// The fixed clock gives a timer interrupt after this many memory accesses (and at every wait for the timer or a key).
		/// The game also waits in loops that only read a counter of the timer interrupt (the palette fades), so it can't be too big.
		/// Since B6.0 (2026-10-25) the clock counts the calls of game functions, not the memory accesses: the interrupts come
		/// at the same calls in the frozen game and in the current one, however many memory accesses the code makes.
		/// </summary>
		public const int ClockSpan = 50;

		private readonly string folder;
		private readonly CPU processor;
		private readonly ScriptedKeyboard keyboard;
		private readonly Dictionary<string, int> calls = new Dictionary<string, int>(StringComparer.Ordinal);

		public HeadlessGame(string gameFolder, string? savesFolder = null, int[]? script = null, bool classic = false)
		{
			this.folder = Path.Combine(Path.GetTempPath(), "Civ1GoldenMaster", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(this.folder);

			foreach (string route in Directory.GetFiles(gameFolder))
			{
				if (!route.EndsWith(".CVL", StringComparison.OrdinalIgnoreCase))
					File.Copy(route, Path.Combine(this.folder, Path.GetFileName(route)));
			}

			if (savesFolder != null)
			{
				foreach (string route in Directory.GetFiles(savesFolder, "AUTO*.*"))
					File.Copy(route, Path.Combine(this.folder, Path.GetFileName(route)), true);
			}

			this.Engine = new GameEngine(null);
			this.processor = this.Engine.Processor;
			this.processor.StandardFolder = this.folder + Path.DirectorySeparatorChar;
			this.processor.FixedClockSpan = ClockSpan;

			// The remaster plays by default (the baselines since phase 1, 2026-10-26); the comparisons with the original
			// translated code (DifferentialPair, DualGame) ask for the classic game: every Classic switch on
			if (classic)
				TurnClassic(this.Engine.TurnHooks);

			this.keyboard = new ScriptedKeyboard(this.processor, this.Engine.VideoCard.Tokens, this.Engine.VideoCard.VideoLatch,
				script ?? ScriptedKeyboard.DefaultScript);
			this.processor.InputStarved = this.keyboard.OnInputStarved;

			// What the player sees when a key is typed goes into the trace: windows and messages that a function takes away
			// again before it returns are compared too
			this.keyboard.KeyTyping = () => this.Trace?.Add("screens " + string.Join(" ",
				this.Engine.VideoCard.Displays.Select(display => GameSnapshot.Fnv(display.Datum.PictureStorage).ToString("x8"))));

			this.Engine.Journal.ScopeListener = CountCall;
			Dual.LeftoverWatcher.Attach(this.processor, this.Engine.Journal);
			this.processor.Heartbeat = CheckDeadline;
		}

		/// <summary>
		/// The game as the translated code of the original played it: every remaster change off (the Classic switches of
		/// TurnEvents). The state of the tag classic-final, before the new baselines.
		/// </summary>
		public static void TurnClassic(Civilization1Remaster.Game.TurnEvents turnHooks)
		{
			turnHooks.ClassicHealing = true;
			turnHooks.ClassicTechnologyNews = true;
			turnHooks.ClassicRailroads = true;
			turnHooks.ClassicWonderCities = true;
			turnHooks.ClassicSchismHomes = true;
			turnHooks.ClassicSchism = true;
			turnHooks.ClassicStarvation = true;
			turnHooks.ClassicStackStrength = true;
			turnHooks.ClassicMilitaryReportRows = true;
			turnHooks.ClassicTenths = true;
			turnHooks.ClassicSpaceshipPictures = true;
			turnHooks.ClassicDisasterDraws = true;
			turnHooks.ClassicReplayPopulation = true;
			turnHooks.ClassicEmissaryThreat = true;
			turnHooks.ClassicFreshGameRuins = true;
			turnHooks.ClassicSpaceshipSlots = true;
			turnHooks.ClassicStackLeftovers = true;
		}

		public GameEngine Engine { get; }

		public ScriptedKeyboard Keyboard => this.keyboard;

		/// <summary>How many times each game function was called since the load or the start of the turn</summary>
		public IReadOnlyDictionary<string, int> Calls => this.calls;

		/// <summary>When not null, every call of a game function is added to it with its arguments (the differential tests)</summary>
		public List<string>? Trace { get; set; }

		/// <summary>The temporary copy of the game folder the game runs in</summary>
		public string Folder => this.folder;

		/// <summary>The game end flag (0xdc48), not 0 when the game is over or the turn was left</summary>
		public ushort GameEndFlag => this.processor.PullGlobalWord(0xdc48);

		/// <summary>
		/// Prepares the machine, seeds the random generator and loads AUTOnn.SVE / AUTOnn.MAP
		/// </summary>
		public void LoadAutosave(int bay, uint randomSeed)
		{
			this.Engine.PrepareMachine();

			// The frame of GameStartup.RunGame, the game functions below are called from it (some of them leave
			// through the frame pointer of their caller)
			this.processor.EnterFrame(0xa);

			// GameStartup.RunGame: VGA, no sound, keyboard only (no mouse)
			this.processor.EmitGlobalByte(0x1a22, 0x4d);
			this.processor.EmitGlobalByte(0x1a30, 0x4e);
			this.processor.EmitGlobalWord(0x1a3c, 0x0);

			PrepareLikeMainIntro(randomSeed);

			// GameStartup.RunGame after the intro
			this.Engine.Startup.QueryForDisk(0, 1);

			this.processor.StackWord(0);
			this.processor.StackWord(0x0055);
			this.Engine.VideoCard.ObtainFreeStorage();
			this.processor.UnstackDoubleWord();

			this.processor.StackWord(0);
			this.processor.StackWord(0x0110);
			this.Engine.ImageToolkit.ImportIcon(0x277);
			this.processor.UnstackDoubleWord();
			this.processor.EmitGlobalWord(0x6e92, this.processor.RegAcc.Value16);

			LoadLikeImportGameDuringPlay(bay);

			this.processor.EmitGlobalWord(0xdc48, 0x0);
			this.calls.Clear();
		}

		/// <summary>
		/// Plays one turn of all players (TurnEvents.HandleGameEnd, one step of the main loop of GameStartup.RunGame)
		/// </summary>
		public void PlayTurn()
		{
			this.calls.Clear();
			this.keyboard.ResetTurnCounter();

			this.Engine.TurnHooks.HandleGameEnd();
		}

		public GameSnapshot Capture()
		{
			return GameSnapshot.Capture(this.Engine, this.calls, this.keyboard.KeysThisTurn);
		}

		/// <summary>
		/// The parts of MainIntro.DisplayMainIntro that set up the machine; the pictures, the credits and the menu are left out
		/// </summary>
		private void PrepareLikeMainIntro(uint randomSeed)
		{
			this.processor.EmitGlobalWord(0xdeba, 0x1);

			for (ushort disk = 1; disk < 8; disk++)
			{
				this.Engine.Startup.QueryForDisk(disk, 0);

				if (this.processor.RegAcc.Value16 == 0)
					break;

				this.processor.EmitGlobalWord(0xdeba, (ushort)(disk + 1));
			}

			// VGA graphics (0x1a22 is 'M'), not EGA
			this.processor.EmitGlobalWord(0xd762, 0x1);
			this.processor.EmitGlobalWord(0xb20e, 0x0);

			this.processor.EmitUInt16(this.processor.SegData.Value16, (ushort)(this.processor.PullGlobalWord(0xaa) + 0x10), 0x1);

			this.processor.EmitGlobalWord(0x6dfc, 0x0);

			for (ushort screen = 0; screen < 3; screen++)
			{
				this.Engine.VideoCard.ReserveScreen(screen);
				this.processor.EmitGlobalWord(0x32d6, this.processor.RegAcc.Value16);

				if (this.processor.RegAcc.Value16 == 0)
					this.processor.EmitGlobalWord(0x6dfc, 0x1);
			}

			this.Engine.Services.PrepareSoundDriver();
			this.Engine.Services.LaunchTimer();

			// Instead of CommonUtilities.SeedRandomizer, which takes the clock. The random generator is used already while
			// loading (the dissolve of MapManagement.FocusMapView). The world seed (ChanceRoot) comes from the save.
			this.Engine.Libc.SeedRandom(randomSeed);
		}

		/// <summary>
		/// The steps of GameStartup.ImportGameDuringPlay with an autosave
		/// </summary>
		private void LoadLikeImportGameDuringPlay(int bay)
		{
			this.processor.EmitGlobalWord(0xdc48, 0x0);
			this.processor.EmitGlobalWord(0xd4ca, 0x0);

			if (this.Engine.SaveLoad.ImportGameFromSlot((ushort)(GameLoadAndSave.BackupBayMarker | bay)) == 0)
				throw new InvalidOperationException($"Autosave AUTO{bay:00} could not be loaded");

			this.Engine.Startup.ConcealMouseCursor();

			this.Engine.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 7);

			this.Engine.NewGameWizard.DisplayTribeSelection();

			this.processor.EmitGlobalWord(0x2f98, 0x1);

			this.Engine.Services.FloodBox(this.processor.PullGlobalWord(0xaa), 0, 0, 320, 200, 0);

			this.Engine.ImageToolkit.ImportPictureOrColors(-1, 0, 0, 0x1c0c, 1);

			this.Engine.Helpers.SetupHueCycling();

			this.Engine.Startup.DisplayMouseCursor();

			this.Engine.Startup.FreshWorldChoices();

			this.processor.EmitGlobalWord(0x6b32, 0x1);
			this.processor.EmitGlobalWord(0xd4cc, (ushort)(this.Engine.World.Participants[this.Engine.World.PersonParticipantIdx].HorizBegin - 7));
			this.processor.EmitGlobalWord(0xd75e, 0x13);

			this.Engine.TurnHooks.RepaintMainScreen();
		}

		private string lastScope = "";
		private long scopeTally = 0;

		/// <summary>
		/// When set (Environment.TickCount64), a game function called after it stops the run with a ScenarioHangException:
		/// an endless loop of the original or of a mutant fails the differential test at once instead of running for hours
		/// </summary>
		public long Deadline { get; set; }
		private System.Threading.Timer? probe;

		/// <summary>Diagnostics: writes the last called function every few seconds into a file (CIV1_GOLDEN_MASTER_TRACE=1)</summary>
		public void StartProbe(string route)
		{
			System.Diagnostics.Stopwatch watch = System.Diagnostics.Stopwatch.StartNew();
			this.probe = new System.Threading.Timer(_ =>
			{
				File.AppendAllText(route, $"{watch.Elapsed.TotalSeconds:0.0}s scopes={this.scopeTally} keys={this.keyboard.KeysTyped} last={this.lastScope}" + Environment.NewLine);
			}, null, 2000, 2000);
		}

		public string LastScope => this.lastScope;

		private void CheckDeadline()
		{
			if (this.Deadline != 0 && Environment.TickCount64 > this.Deadline)
			{
				this.Deadline = 0;
				throw new ScenarioHangException(this.lastScope);
			}
		}

		private void CountCall(string scope)
		{
			CheckDeadline();
			this.lastScope = scope;
			this.Trace?.Add(scope.Replace(this.folder, "<game folder>", StringComparison.Ordinal));
			this.scopeTally++;
			int end = scope.IndexOf('(');
			string name = end < 0 ? scope : scope.Substring(0, end);

			this.calls.TryGetValue(name, out int tally);
			this.calls[name] = tally + 1;
		}

		public void Dispose()
		{
			this.probe?.Dispose();
			this.processor.InputStarved = null;
			this.processor.Shutdown();

			try
			{
				Directory.Delete(this.folder, true);
			}
			catch (IOException)
			{
			}
			catch (UnauthorizedAccessException)
			{
			}
		}
	}

	/// <summary>A run of a differential scenario took longer than its deadline (an endless loop)</summary>
	public sealed class ScenarioHangException(string lastScope) : Exception($"the run took too long (endless loop?), last call: {lastScope}")
	{
	}
}
