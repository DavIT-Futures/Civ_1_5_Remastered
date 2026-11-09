using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using Civ_1_5_Remastered.Core.Composition;
using Civ_1_5_Remastered.Core.Shared.State;
using Civ_1_5_Remastered.GameModel.State;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>
	/// Plays rounds of a saved game without a human: every civilization is computer controlled, dialogs are
	/// dismissed automatically. Used to measure how fast the computer expands.
	/// </summary>
	internal sealed class ComputerExpansionSimulation : IDisposable
	{
		private const ushort FadeBeatsAddress = 0x68;

		private readonly GameEngine engine;
		private readonly string work;
		private readonly Thread dismisser;
		private volatile bool finished;

		public ComputerExpansionSimulation(string saveName)
		{
			string folder = Path.GetDirectoryName(GameFiles.ScratchFile("simulation"))!;
			this.work = Path.Combine(folder, Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(this.work);
			string stem = Path.GetFileNameWithoutExtension(saveName);

			// The whole game folder is copied: the game looks for its picture files when it shows a screen.
			foreach (string file in Directory.GetFiles(GameFiles.Folder!))
			{
				string name = Path.GetFileName(file);

				if ((!name.StartsWith("AUTO", StringComparison.OrdinalIgnoreCase) && !name.StartsWith("CIVIL", StringComparison.OrdinalIgnoreCase)) ||
					name.StartsWith(stem + ".", StringComparison.OrdinalIgnoreCase))
				{
					File.Copy(file, Path.Combine(this.work, name));
				}
			}

			this.engine = new GameEngine(new SilentGameHost());
			this.engine.Processor.StandardFolder = GameFiles.Folder + Path.DirectorySeparatorChar;
			this.engine.PrepareMachine();
			this.engine.Processor.StandardFolder = this.work + Path.DirectorySeparatorChar;
			for (ushort display = 0; display < 4; display++)
			{
				this.engine.VideoCard.ReserveScreen(display);
			}

			this.Loaded = this.engine.SaveLoad.LoadGameContent(stem + ".SVE");
			this.engine.World.ParticipantMarkers = 0;

			this.dismisser = new Thread(() =>
			{
				Stopwatch idle = Stopwatch.StartNew();
				Stopwatch clock = Stopwatch.StartNew();
				int waits = 0;

				while (!this.finished)
				{
					lock (this.engine.VideoCard.Tokens)
					{
						if (this.engine.VideoCard.Tokens.Count != 0)
						{
							idle.Restart();
						}
						else if (this.Waiting != null)
						{
							// Step mode: once the game has waited a while for a key, show the screen, then press the key.
							if (idle.ElapsedMilliseconds > 400)
							{
								this.Waiting(waits++);
								this.engine.VideoCard.Tokens.Enqueue(this.StepKey);
								idle.Restart();
							}
						}
						else if (this.Feeding)
						{
							this.engine.VideoCard.Tokens.Enqueue(0x1b);
						}
					}

					// No clock runs in tests: let every wait for the timer pass at once, colour fades included.
					if (this.RealTime)
					{
						// The clock ticks 18.2 times a second, as the PC timer did.
						if (clock.ElapsedMilliseconds >= 55)
						{
							clock.Restart();
							this.engine.Processor.EmitGlobalWord(TimerState.BeatsAddress, (ushort)(this.engine.Processor.PullGlobalWord(TimerState.BeatsAddress) + 1));
						}
					}
					else
					{
						this.engine.Processor.EmitGlobalWord(TimerState.BeatsAddress, 0x7fff);
					}

					this.engine.Processor.EmitGlobalWord(FadeBeatsAddress, 0);

					Thread.Sleep(1);
				}
			})
			{ IsBackground = true };

			this.dismisser.Start();
		}

		/// <summary>Turns the remastered computer expansion on or off (it is on by default).</summary>
		public static bool Expansion
		{
			set
			{
				ExpansionType().GetProperty("Enabled", BindingFlags.Static | BindingFlags.Public)!.SetValue(null, value);
			}
		}

		public bool Loaded { get; }

		/// <summary>Whether Esc keys are fed to the game (on by default); off lets a screen wait for a key.</summary>
		public bool Feeding { get; set; } = true;

		/// <summary>Step mode: called with a counter each time the game waits for a key, before <see cref="StepKey"/> is pressed.</summary>
		public Action<int>? Waiting { get; set; }

		public int StepKey { get; set; } = 0x0d;

		/// <summary>Lets the timer run at its real pace (animations take their time) instead of passing every wait at once.</summary>
		public bool RealTime { get; set; }

		public GameEngine Engine => this.engine;

		/// <summary>
		/// Plays one round like the game does, except that nobody is the person: while a civilization plays,
		/// the person index points at another one (the barbarians, or civilization 1 during the barbarian turn),
		/// so no civilization is handled as human.
		/// </summary>
		public void PlayRound()
		{
			object events = this.engine.TurnHooks;
			MethodInfo play = events.GetType().GetMethod("PlayTurnOfPlayer", BindingFlags.Instance | BindingFlags.NonPublic)!;
			MethodInfo finish = events.GetType().GetMethod("FinishTurn", BindingFlags.Instance | BindingFlags.NonPublic)!;
			GameState world = this.engine.World;

			this.engine.Globals.Turn.UnitLimitTold = 0;
			this.engine.Globals.Turn.RiotShown = 0;
			this.engine.Globals.Turn.LiveRealmCount = 0;
			world.HighestScienceTally = 0;

			for (short participant = 0; participant < 8; participant++)
			{
				if ((world.LiveRealms & (1 << participant)) != 0)
				{
					this.engine.Globals.Turn.LiveRealmCount++;
					world.HighestScienceTally = Math.Max(world.HighestScienceTally, world.Participants[participant].FoundScienceTally);
				}
			}

			for (short participant = 0; participant < 8; participant++)
			{
				world.Participants[participant].Tally = 0;

				if ((world.LiveRealms & (1 << participant)) == 0)
				{
					continue;
				}

				world.PersonParticipantIndex = (short)(participant == 0 ? 1 : 0);
				play.Invoke(events, new object?[] { participant });
			}

			world.PersonParticipantIndex = 0;
			finish.Invoke(events, new object?[] { world.Participants[0].Gold });
		}

		public int[] CitiesPerCivilization()
		{
			int[] cities = new int[8];

			foreach (City town in this.engine.World.Towns)
			{
				if (town.ConditionMarker != 0xff && town.ParticipantIndex >= 0 && town.ParticipantIndex < 8)
				{
					cities[town.ParticipantIndex]++;
				}
			}

			return cities;
		}

		/// <summary>Per civilization: settlers alive, and for each city its size, whether it has a defender and a free site nearby, and what it builds.</summary>
		public string Diagnose()
		{
			Type expansion = ExpansionType();
			MethodInfo site = expansion.GetMethod("SiteFrom")!;
			MethodInfo defender = expansion.GetMethod("HasDefender")!;
			UnitMemory troops = new UnitMemory(this.engine.Processor);
			System.Text.StringBuilder report = new System.Text.StringBuilder();
			int[] values = new int[16];
			int land = 0;

			for (int column = 0; column < 80; column++)
			{
				for (int row = 0; row < 50; row++)
				{
					if (this.engine.MapOperations.Chart.Terrain(column, row) != 10)
					{
						land++;
						values[Math.Clamp((int)this.engine.VideoCard.ObtainDot(2, column + 80, row), 0, 15)]++;
					}
				}
			}

			List<Point> cities = this.engine.World.Towns.Where(town => town.ConditionMarker != 0xff).Select(town => town.Place).ToList();
			int freeSites = 0;

			for (int column = 0; column < 80; column++)
			{
				for (int row = 2; row < 48; row++)
				{
					Point tile = new Point(column, row);

					if (this.engine.MapOperations.Chart.Terrain(column, row) != 10 && this.engine.VideoCard.ObtainDot(2, column + 80, row) >= 8 &&
						cities.All(city => Civ_1_5_Remastered.GameModel.Rules.CitySitePlanner.Distance(city, tile) >= 5))
					{
						freeSites++;
					}
				}
			}

			report.AppendLine($"  land {land} free sites {freeSites} site values {string.Join(",", values)}");

			for (short participant = 1; participant < 8; participant++)
			{
				Player player = this.engine.World.Participants[participant];

				if (player.TownTally == 0)
				{
					continue;
				}

				report.Append($"  civ {participant}: settlers {player.LiveTroops[0]} making {player.TroopsInsideMaking[0]} |");

				foreach (City town in this.engine.World.Towns)
				{
					if (town.ConditionMarker == 0xff || town.ParticipantIndex != participant)
					{
						continue;
					}

					bool free = site.Invoke(null, new object?[] { this.engine, troops, participant, (short)-1, town.Place }) != null;
					bool guarded = (bool)defender.Invoke(null, new object?[] { this.engine, troops, participant, town.Place })!;
					report.Append($" {town.TrueExtent}{(guarded ? "D" : "-")}{(free ? "S" : "-")}{town.PresentMakingIndex}");
				}

				report.AppendLine();
			}

			return report.ToString();
		}

		public void Dispose()
		{
			this.finished = true;
			this.dismisser.Join();

			try
			{
				Directory.Delete(this.work, true);
			}
			catch (IOException)
			{
			}
		}

		private static Type ExpansionType() => typeof(GameEngine).Assembly.GetReferencedAssemblies()
			.Select(Assembly.Load)
			.Append(typeof(GameEngine).Assembly)
			.Select(assembly => assembly.GetType("Civ_1_5_Remastered.Core.Game.Opponents.ComputerExpansion"))
			.First(type => type != null)!;
	}
}
