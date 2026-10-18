using System;
using System.Collections.Generic;
using System.Linq;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential;

public sealed class BatchNinetyTwoDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandlePlayerTurnHumanChoiceMatchesOriginalCode()
	{
		// The human chooses with keys and clicks: the menu bar, the world map, the palace, the panel, a click held next to the
		// unit (a destination), the GoTo pointer, a city of the human, an own unit, the Civilopedia of a terrain
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckChoices(bay, ways);

		foreach (string way in new[] { "menu bar", "palace", "civilopedia", "city screen", "unit picked", "held destination", "goto", "view moved" })
			Assert.True(ways.Contains(way), $"No turn met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	/// <summary>A key, or a click (button 1, or 2 held) at a place of the screen; Near: next to the active unit</summary>
	private sealed record Input(int Key, int Button, int Horiz, int Vert, int Near);

	private void CheckChoices(int bay, HashSet<string> ways)
	{
		Check(bay, 12, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			// The inputs of the turn, then the keys of the golden master and Escape (without it a GoTo pointer or a picked unit
			// may wait for keys without end)
			List<Input> inputs = [];

			for (int idx = chance.Next(4, 12); idx > 0; idx--)
			{
				inputs.Add(chance.Next(12) switch
				{
					0 => new Input(0x20, 0, 0, 0, 0),
					1 => new Input(0x0d, 0, 0, 0, 0),
					2 => new Input('g', 0, 0, 0, 0),
					// The map view (a city, a unit, a tile), the world map, the palace, the panel below, the menu bar
					3 => new Input(0, 1, chance.Next(0x50, 0x140), chance.Next(8, 0xc8), 0),
					// (on the edges of the tiles)
					4 => new Input(0, 1, 0x50 + 16 * chance.Next(0, 15), 8 + 16 * chance.Next(0, 12), 0),
					5 => new Input(0, 1, chance.Next(0, 0x50), chance.Next(8, 0x3a), 0),
					6 => new Input(0, 1, chance.Next(0, 0x50), chance.Next(0x3a, 0x48), 0),
					7 => new Input(0, 1, chance.Next(0, 0x50), chance.Next(0x48, 0xc8), 0),
					8 => new Input(0, 1, chance.Next(0x10, 0x140), chance.Next(0, 9), 0),
					// Held on the map view (often next to the unit: a destination), or a click there after 'g'
					9 => new Input(0, 2, chance.Next(0x50, 0x140), chance.Next(8, 0xc8), chance.Next(3) == 0 ? 0 : chance.Next(1, 9)),
					10 => new Input(0, 1, 0, 0, chance.Next(1, 9)),
					// A city of the human on the map view (the view moves there first)
					_ => new Input(0, 1, 0, 0, -1),
				});
			}

			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			bool everything = chance.Next(4) == 0;
			// The first turn (its help), sentried units (a click on one does not pick it)
			bool first = chance.Next(4) == 0;
			int[] sentried = Enumerable.Range(0, 128).Where(troop => chance.Next(5) == 0).ToArray();
			Dictionary<string, int> before = new Dictionary<string, int>(pair.Rewritten.Calls);
			Dictionary<string, int> given = [];

			return new Scenario(
				$"HandlePlayerTurn({person}), inputs {string.Join(" ", inputs.Select(input => input.Key != 0 ? $"k{input.Key:x}" : $"b{input.Button}:{input.Horiz},{input.Vert},{input.Near}"))}, everything {everything}",
				game =>
				{
					// The inputs come when the game looks for a key; the mouse is there only from a click to the next key (the
					// screens wait for it otherwise)
					game.Processor.EmitUInt16(0x3b01, 0x1a3c, 0);
					game.Processor.EmitUInt16(0x3b01, 0xd806, (ushort)(everything ? 1 : 0));

					if (first)
						game.World.RoundTally = 0;

					foreach (int troop in sentried)
					{
						ushort unit = UnitMemory.Address(person, troop);

						if (game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind)) != 0xff)
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Status), (byte)(game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Status)) | 1));
					}
					Queue<Input> pending = new Queue<Input>(inputs);
					int[] script = { 0x20, 0x0d, 0x31, 0x1b };
					int scriptIdx = 0;
					int typed = 0;
					bool withhold = false;
					MapLayers chart = game.MapOps.Chart;

					game.Processor.InputStarved = waiting =>
					{
						game.Processor.Idle();

						if (!waiting && withhold)
						{
							withhold = false;
							return;
						}

						if (++typed > 3000)
							throw new InvalidOperationException("The turn asked for more than 3000 keys");

						// A click while the game only polls (it comes with the next look at the mouse)
						if (!waiting && pending.Count > 0 && pending.Peek().Key == 0)
						{
							Input click = pending.Dequeue();
							int horiz = click.Horiz;
							int vert = click.Vert;

							if (click.Near < 0)
							{
								var town = game.World.Towns.FirstOrDefault(town => town.ConditionMarker != 0xff && town.ParticipantIdx == person);

								if (town != null)
								{
									game.Processor.Storage.EmitUInt16(0x3b01, 0xd4cc, (ushort)Math.Max(0, town.Place.X - 7));
									game.Processor.Storage.EmitUInt16(0x3b01, 0xd75e, (ushort)Math.Max(0, town.Place.Y - 5));
									horiz = 0x50 + (town.Place.X - Math.Max(0, town.Place.X - 7)) * 16 + 8;
									vert = 8 + (town.Place.Y - Math.Max(0, town.Place.Y - 5)) * 16 + 8;
								}
							}
							else if (click.Near != 0)
							{
								// Next to the active unit of the turn (the local 0x32 of HandlePlayerTurn)
								ushort troop = game.Processor.Storage.PullUInt16(0x3b01, (ushort)(frame - 0x32));

								if (troop < 128)
								{
									ushort unit = UnitMemory.Address(person, troop);
									int unitHoriz = game.Processor.Storage.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz)) + chart.DirectionHoriz(click.Near);
									int unitVert = game.Processor.Storage.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert)) + chart.DirectionVert(click.Near);
									horiz = 0x50 + (unitHoriz - game.Processor.Storage.PullInt16(0x3b01, 0xd4cc)) * 16 + 8;
									vert = 8 + (unitVert - game.Processor.Storage.PullInt16(0x3b01, 0xd75e)) * 16 + 8;
								}
							}

							game.Processor.Storage.EmitUInt16(0x3b01, 0x586e, (ushort)Math.Clamp(horiz, 0, 319));
							game.Processor.Storage.EmitUInt16(0x3b01, 0x5870, (ushort)Math.Clamp(vert, 0, 199));
							game.Processor.Storage.EmitUInt16(0x3b01, 0x5872, 0);
							game.Processor.Storage.EmitUInt16(0x3b01, 0x5874, (ushort)click.Button);
							game.Processor.Storage.EmitUInt16(0x3b01, 0x1a3c, 1);
							given[click.Button == 2 ? "held" : "click"] = given.GetValueOrDefault(click.Button == 2 ? "held" : "click") + 1;
							withhold = true;
							return;
						}

						int key;

						if (pending.Count > 0 && pending.Peek().Key != 0)
							key = pending.Dequeue().Key;
						else
						{
							key = script[scriptIdx];
							scriptIdx = (scriptIdx + 1) % script.Length;
						}

						withhold = true;
						// (the mouse stays where it was clicked until the next key)
						game.Processor.Storage.EmitUInt16(0x3b01, 0x1a3c, 0);

						lock (game.VideoCard.Tokens)
							game.VideoCard.Tokens.Enqueue(key);
					};
				},
				game => new PlayerTurnReference(game).HandlePlayerTurn(person),
				game => game.TurnLogic.HandlePlayerTurn(person),
				Register.Kept,
				// What the turn met, from the calls
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					List<string> met = [];

					if (Delta("ManageMenuBar") > 0)
						met.Add("menu bar");

					if (Delta("ImportPalaceGraphics") > 0)
						met.Add("palace");

					if (Delta("DisplayCivilopediaRecord") > 0)
						met.Add("civilopedia");

					if (Delta("RenewCityScreen") > 0)
						met.Add("city screen");

					if (Delta("DisplayBattleScreen") > 0)
						met.Add("unit picked");

					if (given.ContainsKey("held") && Delta("MergeCoordinates") >= 0 && Delta("AssignMouseCursorImage") > 0)
						met.Add("held destination");

					if (inputs.Any(input => input.Key == 'g') && given.ContainsKey("click"))
						met.Add("goto");

					if (given.ContainsKey("click") && Delta("FocusMapView") > 0)
						met.Add("view moved");

					foreach (string way in met)
						ways.Add(way);

					return met.Count == 0 ? "nothing" : string.Join("+", met);
				},
				[(0x3b01, (ushort)(frame - 0x80), 0x80)]);
		}, output, [], null);
	}
}
