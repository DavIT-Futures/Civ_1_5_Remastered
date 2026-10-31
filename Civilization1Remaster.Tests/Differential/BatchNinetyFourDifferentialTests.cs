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

public sealed class BatchNinetyFourDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandlePlayerTurnHomePollutionFortressMatchesOriginalCode()
	{
		// The orders 'h' (home city, a plane flying home), 'p' (settlers clean up pollution) and 'f' (fortify, a fortress)
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckChoices(bay, ways);

		foreach (string way in new[] { "home city", "plane home", "pollution work", "pollution cleaned", "pollution refused", "fortify", "fortress", "fortress refused" })
			Assert.True(ways.Contains(way), $"No turn met '{way}' (met: {string.Join(", ", ways.Order())})");
	}

	/// <summary>A key, or a click (button 1, or 2 held) at a place of the screen; Near: next to the active unit</summary>
	private sealed record Input(int Key, int Button, int Horiz, int Vert, int Near);

	private void CheckChoices(int bay, HashSet<string> ways)
	{
		Check(bay, 20, (chance, pair) =>
		{
			GameEngine engine = pair.Rewritten.Engine;
			short person = engine.World.PersonParticipantIdx;
			// The inputs of the turn, then the keys of the golden master and Escape (without it a GoTo pointer or a picked unit
			// may wait for keys without end)
			List<Input> inputs = [];

			for (int idx = chance.Next(4, 16); idx > 0; idx--)
			{
				int[] keys = { 0x20, 'h', 'p', 'p', 'f', 'f', 0x0d };
				inputs.Add(new Input(keys[chance.Next(keys.Length)], 0, 0, 0, 0));
			}

			// The first unit of the human that moves becomes a bomber (none in the saves), sent home first; units in cities
			// without a home city now and then
			bool homeless = chance.Next(2) == 0;
			bool bomber = chance.Next(3) == 0;

			if (bomber)
				inputs.Insert(0, new Input('h', 0, 0, 0, 0));

			// Pollution and fortresses under the settlers, their work so far, Construction known
			int[] polluted = Enumerable.Range(0, 128).Where(_ => chance.Next(2) == 0).ToArray();
			byte[] work = Enumerable.Range(0, 128).Select(_ => (byte)(chance.Next(3) == 0 ? chance.Next(2, 4) : chance.Next(0, 10))).ToArray();
			bool construction = chance.Next(4) != 0;
			bool[] fortress = Enumerable.Range(0, 128).Select(_ => chance.Next(4) == 0).ToArray();
			byte[] bases = Enumerable.Range(0, 128).Select(troop => engine.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(person, troop) + UnitMemory.BaseTown))).ToArray();
			bool[] inTown = Enumerable.Range(0, 128).Select(troop => engine.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(person, troop) + UnitMemory.Vert)) is byte vert && vert < 50 &&
				(engine.MapOps.Chart.Improvements(engine.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(person, troop) + UnitMemory.Horiz)), vert) & 1) != 0).ToArray();
			short dirt = engine.World.DirtyCellTally;

			ushort frame = (ushort)(engine.Processor.RegStack.Value16 - 2);
			bool everything = false;
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

					if (bomber)
					{
						for (int troop = 0; troop < 128; troop++)
						{
							ushort unit = UnitMemory.Address(person, troop);

							if (game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind)) != 0xff &&
								(game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Status)) & 9) == 0)
							{
								game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), 0xf);
								game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Status), 0);
								game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.UnusualSteps), 2);
								break;
							}
						}
					}

					IMapPlanes layers = game.MapOps.Chart;

					for (int troop = 0; troop < 128 && homeless; troop++)
					{
						ushort unit = UnitMemory.Address(person, troop);
						int horizLoc = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz));
						int vertLoc = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert));

						if (game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind)) != 0xff && vertLoc < 50 && (layers.Improvements(horizLoc, vertLoc) & 1) != 0)
							game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.BaseTown), 0xff);
					}

					foreach (int troop in polluted)
					{
						ushort unit = UnitMemory.Address(person, troop);
						int horizLoc = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz));
						int vertLoc = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert));

						if (game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind)) != 0 || vertLoc >= 50)
							continue;

						// (pollution 0x40 and fortress 0x20 are bits 2 and 1 of the high layer)
						game.VideoCard.AssignDot(2, horizLoc, vertLoc + 150, (byte)(layers.HighImprovements(horizLoc, vertLoc) | (fortress[troop] ? 2 : 4)), 0);
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.UnusualSteps), work[troop]);
					}

					if (construction)
						game.World.Participants[person].FoundScienceMarkers[0x2d >> 4] |= (ushort)(1 << (0x2d & 0xf));
					else
						game.World.Participants[person].FoundScienceMarkers[0x2d >> 4] &= unchecked((ushort)~(1 << (0x2d & 0xf)));

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
					IMapPlanes chart = game.MapOps.Chart;

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
								ushort troop = ReferenceEquals(game, pair.Rewritten.Engine) ? game.TurnLogic.ActiveTroop :
									game.Processor.Storage.PullUInt16(0x3b01, (ushort)(frame - 0x32));

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
				game => new TurnControllerReference(game).HandlePlayerTurn(person),
				game => game.TurnLogic.HandlePlayerTurn(person),
				Register.Kept,
				// What the turn met, from the calls
				game =>
				{
					int Delta(string name) => Calls(game, pair, name) - before.GetValueOrDefault(name);
					List<string> met = [];

					byte Unit(int troop, int offset) => game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(person, troop) + offset));

					if (inputs.Any(input => input.Key == 'h') && Enumerable.Range(0, 128).Any(troop => Unit(troop, UnitMemory.Kind) != 0xff && Unit(troop, UnitMemory.BaseTown) != (homeless && inTown[troop] ? 0xff : bases[troop])))
						met.Add("home city");

					if (Delta("LocateNearestFriendlyCity") > 0)
						met.Add("plane home");

					if (Enumerable.Range(0, 128).Any(troop => Unit(troop, UnitMemory.Kind) == 0 && (Unit(troop, UnitMemory.Status) & 0x82) == 0x82))
						met.Add("pollution work");

					if (game.World.DirtyCellTally < dirt)
						met.Add("pollution cleaned");

					if (Delta("DisplayHelpForHuman") > 0)
						met.Add("pollution refused");

					if (inputs.Any(input => input.Key == 'f') && Enumerable.Range(0, 128).Any(troop => Unit(troop, UnitMemory.Kind) is not (0xff or 0) && (Unit(troop, UnitMemory.Status) & 4) != 0))
						met.Add("fortify");

					if (Delta("AssignTileImprovements") > 0)
						met.Add("fortress");

					if (inputs.Any(input => input.Key == 'f') && Delta("PanMapToPosition") > 0)
						met.Add("fortress refused");

					foreach (string way in met)
						ways.Add(way);

					return met.Count == 0 ? "nothing" : string.Join("+", met);
				});
		}, output, [], null);
	}
}
