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

public sealed class BatchNinetyThreeDifferentialTests(ITestOutputHelper output)
{
	[Fact]
	public void HandlePlayerTurnSimpleOrdersMatchOriginalCode()
	{
		// The simple orders of the human: Space, wait, sentry, unload, GoTo on / off, disband, pillage (on improved tiles), and
		// the units with a destination
		HashSet<string> ways = [];

		foreach (int bay in new[] { 5, 9, 13 })
			CheckChoices(bay, ways);

		foreach (string way in new[] { "disband", "pillage", "pillage refused", "sentry", "unload", "carry out goto", "goto cursor" })
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

			for (int idx = chance.Next(4, 16); idx > 0; idx--)
			{
				// ('g' only twice in a row: GoTo on and off; on, followed by other keys, it may loop without end when no unit is
				// active, e.g. 's', 'g', Enter)
				int[] keys = { 0x20, 'w', 's', 'u', 'g', 'D', 'P', 'P', 0x0d };
				int key = keys[chance.Next(keys.Length)];
				inputs.Add(new Input(key, 0, 0, 0, 0));

				if (key == 'g')
					inputs.Add(new Input(key, 0, 0, 0, 0));
			}

			// The first unit of a stack of the human becomes a transport (no ships with units in the saves), unloaded first
			bool transport = chance.Next(3) == 0;

			if (transport)
				inputs.Insert(0, new Input('u', 0, 0, 0, 0));

			// Improvements under some units of the human (irrigation, mine, road, railroad), destinations of some units
			int[] improved = Enumerable.Range(0, 128).Where(_ => chance.Next(3) == 0).ToArray();
			int[] improvements = improved.Select(_ => new[] { 2, 4, 8, 0x10, 0xa, 0x18 }[chance.Next(6)]).ToArray();
			// (destinations of land units only: a ship sent to land may never end its turn)
			int[] travelling = Enumerable.Range(0, 128).Where(_ => chance.Next(8) == 0).ToArray();

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

					IMapPlanes layers = game.MapOps.Chart;

					for (int idx = 0; idx < improved.Length; idx++)
					{
						ushort unit = UnitMemory.Address(person, improved[idx]);
						int horizLoc = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz));
						int vertLoc = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert));

						if (game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind)) == 0xff || vertLoc >= 50 || (layers.Improvements(horizLoc, vertLoc) & 1) != 0)
							continue;

						game.VideoCard.AssignDot(2, horizLoc, vertLoc + 100, (byte)(layers.LowImprovements(horizLoc, vertLoc) | (improvements[idx] & 0xe)), 0);
						game.VideoCard.AssignDot(2, horizLoc, vertLoc + 150, (byte)(layers.HighImprovements(horizLoc, vertLoc) | (improvements[idx] >> 4)), 0);
					}

					foreach (int troop in travelling)
					{
						ushort unit = UnitMemory.Address(person, troop);

						byte kind = game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind));

						if (kind == 0xff || game.Processor.PullUInt16(0x3b01, (ushort)(UnitKindTable.Origin + kind * UnitKindTable.Stride + UnitKindTable.LandGroup)) != UnitKindTable.LandUnits)
							continue;

						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelHoriz), (byte)((game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Horiz)) + 2) % 80));
						game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.TravelVert), game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Vert)));
					}

					if (transport)
					{
						for (int troop = 0; troop < 128; troop++)
						{
							ushort unit = UnitMemory.Address(person, troop);

							if (game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind)) != 0xff &&
								game.Processor.PullUInt8(0x3b01, (ushort)(unit + UnitMemory.FollowingInStack)) != 0xff)
							{
								game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Kind), 0x18);
								game.Processor.EmitUInt8(0x3b01, (ushort)(unit + UnitMemory.Status), 0);
								break;
							}
						}
					}

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

					if (inputs.Any(input => input.Key == 'D') && Delta("EliminateUnit") > 0)
						met.Add("disband");

					if (Delta("WipeDiplomacyFlags") > 0)
						met.Add("pillage");

					if (Delta("DisplayHelpForHuman") > 0)
						met.Add("pillage refused");

					if (inputs.Any(input => input.Key == 's') && Enumerable.Range(0, 128).Any(troop => !sentried.Contains(troop) &&
						(game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(person, troop) + UnitMemory.Status)) & 1) != 0 &&
						game.Processor.Storage.PullUInt8(0x3b01, (ushort)(UnitMemory.Address(person, troop) + UnitMemory.Kind)) != 0xff))
						met.Add("sentry");

					if (Delta("LocateBestGuardian") > 0)
						met.Add("unload");

					if (Delta("CarryOutGoto") > 0)
						met.Add("carry out goto");

					if (inputs.Any(input => input.Key == 'g') && Delta("RepaintMapTile") > 0)
						met.Add("goto cursor");

					foreach (string way in met)
						ways.Add(way);

					return met.Count == 0 ? "nothing" : string.Join("+", met);
				});
		}, output, [], null);
	}
}
