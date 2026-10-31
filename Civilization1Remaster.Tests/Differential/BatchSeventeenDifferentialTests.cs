using System.Linq;
using System.Text;
using Civilization1Remaster.Game;
using Civilization1Remaster.Tests.Differential.Reference;
using Xunit;
using static Civilization1Remaster.Tests.Differential.DifferentialRun;
using Register = Civilization1Remaster.Tests.Differential.DifferentialPair.Register;

namespace Civilization1Remaster.Tests.Differential
{
	/// <summary>
	/// The specialists of a city, a new strategic location, the next unit of a stack and the line breaks of a text against
	/// their original code
	/// </summary>
	public class BatchSeventeenDifferentialTests(ITestOutputHelper output)
	{
		[Fact]
		public void ReshuffleCitizensMatchesOriginalCode()
		{
			Check(10, 300, (chance, pair) =>
			{
				short[] towns = Enumerable.Range(0, 128).Select(idx => (short)idx)
					.Where(idx => pair.Original.Engine.World.Towns[idx].ConditionMarker != 0xff).ToArray();
				short townIdx = towns[chance.Next(towns.Length)];
				int size = pair.Original.Engine.World.Towns[townIdx].TrueExtent;
				ushort entertainers = (ushort)chance.Next(-2, size + 3), taxmen = (ushort)chance.Next(-2, size + 3);
				ushort scientists = (ushort)chance.Next(0, size + 3), workers = (ushort)chance.Next(0, size + 2);

				return new Scenario($"ReshuffleCitizens({townIdx}, {workers}), size {size}, specialists {(short)entertainers} {(short)taxmen} {scientists}",
					game =>
					{
						game.Processor.EmitGlobalWord(0x70e2, entertainers);
						game.Processor.EmitGlobalWord(0x70e4, taxmen);
						game.Processor.EmitGlobalWord(0x6542, scientists);
					},
					game => new CityManagementReference(game).ReshuffleCitizens(townIdx, workers),
					game => game.CityOps.ReshuffleCitizens(townIdx, workers),
					Register.Kept,
					game => game.Processor.PullGlobalWord(0x6542) < scientists ? "scientists moved" :
						game.Processor.PullGlobalWord(0x70e2) < entertainers ? "entertainers given up" : "fits");
			}, output, ["scientists moved", "entertainers given up", "fits"]);
		}

		[Fact]
		public void AttachStrategicLocationMatchesOriginalCode()
		{
			Check(3, 200, (chance, pair) =>
			{
				short participantIdx = (short)chance.Next(1, 8);
				bool full = chance.Next(5) == 0;
				var sites = Enumerable.Range(0, 16).Select(idx => !full && chance.Next(3) == 0 ?
					((sbyte)-1, 0, 0, (byte)0) : ((sbyte)chance.Next(0, 4), chance.Next(80), chance.Next(1, 49), (byte)(full ? 8 : chance.Next(6)))).ToArray();

				// Mostly a new place, sometimes one already noted (with another state or strategy)
				var known = sites[chance.Next(16)];
				bool repeat = known.Item1 != -1 && chance.Next(2) == 0;
				ushort horizLoc = (ushort)(repeat ? known.Item2 : chance.Next(80)), vertLoc = (ushort)(repeat ? known.Item3 : chance.Next(1, 49));
				ushort live = (ushort)(repeat && chance.Next(2) == 0 ? known.Item1 : chance.Next(0, 4));
				ushort strategy = (ushort)chance.Next(0, 7);

				return new Scenario($"AttachStrategicLocation({participantIdx}, {horizLoc}, {vertLoc}, {live}, {strategy}), full {full}",
					game =>
					{
						for (int siteIdx = 0; siteIdx < 16; siteIdx++)
						{
							var site = game.World.Participants[participantIdx].TacticalSites[siteIdx];
							(site.Live, int horiz, int vert, site.Strategy) = sites[siteIdx];
							site.Place = new System.Drawing.Point(horiz, vert);
						}
					},
					game => new AIPlayerReference(game).AttachStrategicLocation(participantIdx, horizLoc, vertLoc, live, strategy),
					game => game.Bot.AttachStrategicLocation(participantIdx, horizLoc, vertLoc, live, strategy),
					Register.Kept,
					game => game.World.Participants[participantIdx].TacticalSites.Any(site => site.Place.X == horizLoc && site.Place.Y == vertLoc &&
						site.Live == (sbyte)live && site.Strategy == strategy) ? (repeat ? "known place" : "inserted") : "not noted");
			}, output, ["known place", "inserted", "not noted"]);
		}

		[Fact]
		public void LocateBestGuardianMatchesOriginalCode()
		{
			Check(3, 200, (chance, pair) =>
			{
				GameEngine engine = pair.Original.Engine;
				UnitMemory troops = new UnitMemory(engine.Processor);
				short participantIdx = (short)chance.Next(8);
				short[] own = Enumerable.Range(0, UnitMemory.TroopTally).Select(idx => (short)idx)
					.Where(idx => troops.Exists(participantIdx, idx)).ToArray();

				if (own.Length == 0)
					return null;

				// Sometimes a new ring of a few units of the player (in random order)
				short[] ring = own.Length > 1 && chance.Next(2) == 0 ?
					own.OrderBy(_ => chance.Next()).Take(chance.Next(2, System.Math.Min(own.Length, 20) + 1)).ToArray() : [];
				short troopIdx = ring.Length > 0 ? ring[chance.Next(ring.Length)] : own[chance.Next(own.Length)];

				return new Scenario($"LocateBestGuardian({participantIdx}, {troopIdx}), ring of {ring.Length}",
					game =>
					{
						UnitMemory memory = new UnitMemory(game.Processor);

						for (int idx = 0; idx < ring.Length; idx++)
							memory.AssignFollowing(participantIdx, ring[idx], (byte)ring[(idx + 1) % ring.Length]);
					},
					game => new PlayerTurnReference(game).LocateBestGuardian(participantIdx, (ushort)troopIdx),
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.TurnLogic.LocateBestGuardian(participantIdx, (ushort)troopIdx)),
					Register.Acc | Register.Kept,
					game => new UnitMemory(game.Processor).Following(participantIdx, troopIdx) == -1 ? "alone" :
						game.Processor.RegAcc.Value16 == (ushort)troopIdx ? "itself" : ring.Length > 16 ? "next of a long ring" : "next");
			}, output, ["alone", "next", "next of a long ring"]);
		}

		[Fact]
		public void ArrangeTextRowMatchesOriginalCode()
		{
			string[] words = { "The ", "city ", "of ", "Rome ", "^", "\n", "  ", "Babylon. ", "x", "celebrates ", "^" };

			Check(3, 300, (chance, pair) =>
			{
				StringBuilder text = new StringBuilder();

				for (int pieceIdx = chance.Next(1, 25); pieceIdx > 0; pieceIdx--)
					text.Append(words[chance.Next(words.Length)]);

				string composed = text.ToString();
				short breadth = (short)chance.Next(5, 40);

				return new Scenario($"ArrangeTextRow({breadth}), text \"{composed.Replace("\n", "\\n")}\"",
					game => game.Processor.RegAcc.Value16 = ReturnInAx.Of(game.Libc.CopyText(0xba06, composed)),
					game => new TextBlockRendererReference(game).ArrangeTextRow(breadth),
					game => game.BlockText.ArrangeTextRow(breadth),
					Register.Kept,
					game => (composed.Contains('^') ? "^, " : "") + (composed.Length > breadth ? "wrapped" : "short"));
			}, output, ["^, wrapped", "wrapped", "short"]);
		}
	}
}
