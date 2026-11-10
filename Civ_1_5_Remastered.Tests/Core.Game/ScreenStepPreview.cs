using System.Reflection;
using Civ_1_5_Remastered.Tests.Solution;

namespace Civ_1_5_Remastered.Tests.Core.Game
{
	/// <summary>
	/// Opt-in tool: plays a screen of the game over the map and saves a PNG each time it waits for a key
	/// (CIV_PREVIEW=folder, CIV_PREVIEW_VIEW=20x15, CIV_PREVIEW_SCREEN=audience|capture, CIV_PREVIEW_LEADER=16, CIV_PREVIEW_REGIME=2m).
	/// </summary>
	public class ScreenStepPreview
	{
		[Fact]
		public async Task StepThroughScreen()
		{
			string? output = Environment.GetEnvironmentVariable("CIV_PREVIEW");

			if (GameFiles.Folder == null || output == null)
			{
				return;
			}

			string[] size = (Environment.GetEnvironmentVariable("CIV_PREVIEW_VIEW") ?? "20x15").Split('x');
			string screen = Environment.GetEnvironmentVariable("CIV_PREVIEW_SCREEN") ?? "audience";

			await Task.Run(() =>
			{
				using ComputerExpansionSimulation game = new ComputerExpansionSimulation(Environment.GetEnvironmentVariable("CIV_PREVIEW_SAVE") ?? "CIVIL0.SVE");
				short person = game.Engine.World.PersonParticipantIndex;
				game.Engine.World.ParticipantMarkers = (short)(1 << person);

				if (Environment.GetEnvironmentVariable("CIV_PREVIEW_VGA") != null)
				{
					// Mode 1 is the 16-colour graphics: sprites cut from TER257.PIC.
					game.Engine.Globals.Screen.VideoGraphicsMode = 1;
					game.Engine.NewGameWizard.ShowTribeSelection();
				}
				else
				{
					// The 256-colour graphics of the desktop game, prepared as after loading a game.
					game.Engine.Globals.Screen.VideoGraphicsMode = 0;
					game.Engine.ImageToolkit.LoadPictureOrColors(3, 0, 0, Civ_1_5_Remastered.Core.Shared.Texts.GameTexts.FileSpritesPicture, 0);
					game.Engine.NewGameWizard.ShowTribeSelection();
					game.Engine.ImageToolkit.LoadPictureOrColors(-1, 0, 0, Civ_1_5_Remastered.Core.Shared.Texts.GameTexts.FileSpaceshipPicture, 1);
				}

				game.Engine.MapViewport.Request(int.Parse(size[0]), int.Parse(size[1]));
				game.Engine.TurnHooks.RepaintMainScreen();
				var home = game.Engine.World.Towns.First(candidate => candidate.ConditionMarker != 0xff && candidate.ParticipantIndex == person).Place;
				game.Engine.MapOperations.FocusMapView(person, home.X - game.Engine.MapViewport.MiddleColumn, home.Y - game.Engine.MapViewport.MiddleRow);
				MethodInfo save = typeof(MapScreenPreview).GetMethod("Save", BindingFlags.NonPublic | BindingFlags.Static)!;
				save.Invoke(null, new object[] { game.Engine.VideoCard.Displays[0], Path.Combine(output, $"{screen}_start.png") });

				short other = 1;

				while (other == person || game.Engine.World.Participants[other].TownTally == 0)
				{
					other++;
				}

				var city = game.Engine.World.Towns.First(candidate => candidate.ConditionMarker != 0xff && candidate.ParticipantIndex == other).Place;

				// CIV_PREVIEW_LEADER=16 shows the other side with the given leader (16 Poland, 17 Japan).
				if (short.TryParse(Environment.GetEnvironmentVariable("CIV_PREVIEW_LEADER"), out short leader))
				{
					game.Engine.World.Participants[other].ChiefVisuals = leader;
				}

				// CIV_PREVIEW_REGIME=2m shows the other side under government 2 with modern advisors (a = ancient).
				string? regime = Environment.GetEnvironmentVariable("CIV_PREVIEW_REGIME");

				if (regime != null)
				{
					game.Engine.World.Participants[other].RegimeKind = short.Parse(regime[..1]);
					ushort modern = 1 << (0x14 & 0xf);
					ushort[] found = game.Engine.World.Participants[other].FoundScienceMarkers;
					found[0x14 >> 4] = regime.EndsWith('m') ? (ushort)(found[0x14 >> 4] | modern) : (ushort)(found[0x14 >> 4] & ~modern);
				}

				game.Waiting = step =>
				{
					lock (game.Engine.VideoCard.VideoLatch)
					{
						save.Invoke(null, new object[] { game.Engine.VideoCard.Displays[0], Path.Combine(output, $"{screen}_{step:00}.png") });
					}
				};

				lock (game.Engine.VideoCard.Tokens)
				{
					game.Engine.VideoCard.Tokens.Clear();
				}

				int ticks = 0;
				bool running = true;
				Thread? film = null;

				if (Environment.GetEnvironmentVariable("CIV_PREVIEW_FILM") != null)
				{
					film = new Thread(() =>
					{
						// Animations do not wait for keys: take a picture every 150 ms.
						while (running && ticks < 60)
						{
							Thread.Sleep(150);

							lock (game.Engine.VideoCard.VideoLatch)
							{
								save.Invoke(null, new object[] { game.Engine.VideoCard.Displays[0], Path.Combine(output, $"{screen}_film{ticks++:00}.png") });
							}
						}
					})
					{ IsBackground = true };
				}

				game.RealTime = film != null;
				film?.Start();

				short enemyTown = (short)Array.FindIndex(game.Engine.World.Towns, candidate => candidate.ConditionMarker != 0xff && candidate.ParticipantIndex == other);

				switch (screen)
				{
					case "audience":
						game.Engine.Audience.LaunchAudience(other, city.X, city.Y, 0);
						break;

					case "meeting":
						// Units meeting: the audience starts from the map during a move.
						game.Engine.World.Participants[person].Relations[other] &= 0xfffd;
						game.Engine.World.Participants[other].Relations[person] &= 0xfffd;
						game.Engine.Politics.BargainPeaceTreaty(person, other, city.X, city.Y);
						break;

					case "capture":
						game.Engine.World.SessionPreferenceMarkers |= 8;
						game.Engine.Conquest.SeizeCity(person, enemyTown, 0);
						break;

					case "building":
						// A city finished a building: the panorama with the new building, then the map.
						game.Engine.CityScene.ShowCityScreen((short)Array.FindIndex(game.Engine.World.Towns, candidate => candidate.ConditionMarker != 0xff && candidate.ParticipantIndex == person), 3);
						game.Engine.TurnHooks.RepaintMainScreen();
						break;

					case "parade":
						// What AnnounceCapture shows with animations on: the panorama, the parade, then the map.
						game.Engine.CityScene.ShowCityScreen(enemyTown, -2);
						game.Engine.CityScene.DrawCitySceneryForPlayer(person);
						game.Engine.TurnHooks.RepaintMainScreen();
						break;

					case "newspaper":
						game.Engine.World.SessionPreferenceMarkers &= ~8;
						game.Engine.Conquest.SeizeCity(person, enemyTown, 0);
						break;
				}

				running = false;
				film?.Join();
				game.Waiting = null;
				save.Invoke(null, new object[] { game.Engine.VideoCard.Displays[0], Path.Combine(output, $"{screen}_end.png") });
			}).WaitAsync(TimeSpan.FromMinutes(3));
		}
	}
}
