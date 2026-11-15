using Civ_1_5_Remastered.Core.Shared.State;
using Civ_1_5_Remastered.Imaging.Pictures;

namespace Civ_1_5_Remastered.Tests.Core.Shared
{
	public class EuropeMapTests
	{
		private const byte Ocean = 1;
		private const byte Mountains = 13;

		private static readonly int[] Leaders = [1, 2, 3, 4, 5, 6, 7, 9, 10, 11, 12, 13, 14, 15, 16, 17];

		[Fact]
		public void EveryLeaderStartsOnLand()
		{
			using VideoGraphicsBitmap map = Map();

			foreach (int leader in Leaders)
			{
				(short column, short row) = EuropeMap.Start(leader);
				Assert.NotEqual(Ocean, map.ObtainDot(column, row));
				Assert.True(Civ_1_5_Remastered.GameModel.Rules.CitySitePlanner.IsCityRow(row), $"leader {leader} starts in row {row}, where no city is founded");
			}
		}

		[Fact]
		public void EveryRiverIsDrawn()
		{
			using VideoGraphicsBitmap map = Map();

			const byte river = 9;

			// The game draws a river only towards a river or the sea at one of its four sides.
			for (int row = 1; row < 49; row++)
			{
				for (int column = 1; column < 79; column++)
				{
					if (map.ObtainDot(column, row) != river)
					{
						continue;
					}

					byte[] sides = [map.ObtainDot(column, row - 1), map.ObtainDot(column + 1, row), map.ObtainDot(column, row + 1), map.ObtainDot(column - 1, row)];
					Assert.True(sides.Any(side => side == river || side == Ocean), $"the river at {column},{row} stands alone");
				}
			}
		}

		[Fact]
		public void TrueStartsOfferTheCivilizationsAtHome()
		{
			Assert.True(EuropeMap.Chosen(EuropeMap.TrueStartsMarker));
			Assert.True(EuropeMap.TrueStarts(EuropeMap.TrueStartsMarker));
			Assert.False(EuropeMap.TrueStarts(EuropeMap.Marker));

			// Poles, Romans, Russians, Germans, French, English, Greeks, Babylonians, Egyptians, Mongols.
			Assert.Equal([16, 1, 9, 3, 11, 14, 6, 2, 4, 15], EuropeMap.PlayableLeaders);
		}

		[Fact]
		public void TrueStartLineupsDrawTheOpponentsAroundThePlayer()
		{
			int[] rivals = [9, 2, 11, 4, 13, 14, 15];
			Random random = new Random(7);
			int chinese = 0;
			int games = 0;

			foreach (int player in EuropeMap.PlayableLeaders)
			{
				for (int game = 0; game < 200; game++)
				{
					int[] lineup = EuropeMap.TrueStartLineup(player, random.Next);
					int[] slots = lineup.Skip(1).ToArray();
					games++;

					// Seven different civilizations: the player, Poland always, the rest from the opponents.
					Assert.Equal(7, slots.Distinct().Count());
					Assert.Contains(player, slots);
					Assert.Contains(16, slots);
					Assert.All(slots.Where(leader => leader != player && leader != 16), leader => Assert.Contains(leader, rivals));

					for (int slot = 1; slot <= 7; slot++)
					{
						// Every original leader in its own slot; Poland in any.
						if (lineup[slot] != 16)
						{
							Assert.Equal(slot, lineup[slot] & 7);
						}

						Assert.Contains(lineup[slot], Leaders);
					}

					chinese += slots.Contains(13) ? 1 : 0;
				}
			}

			// China is the optional opponent: it plays in about half of the games.
			Assert.InRange(chinese, games / 3, games * 2 / 3);
		}

		[Fact]
		public void TheStartsAreApart()
		{
			var starts = Leaders.Select(EuropeMap.Start).ToList();

			for (int first = 0; first < starts.Count; first++)
			{
				for (int second = first + 1; second < starts.Count; second++)
				{
					int distance = Math.Max(Math.Abs(starts[first].Column - starts[second].Column), Math.Abs(starts[first].Row - starts[second].Row));
					Assert.True(distance >= 3, $"leaders {Leaders[first]} and {Leaders[second]} start {distance} tiles apart");
				}
			}
		}

		[Fact]
		public void TheNewWorldLiesBeyondTheSea()
		{
			using VideoGraphicsBitmap map = Map();

			for (int row = 0; row < 50; row++)
			{
				// The east edge is a single range of mountains from top to bottom, the west edge sea before the New
				// World; nobody steps across the seam between them.
				Assert.Equal(Mountains, map.ObtainDot(79, row));
				Assert.Equal(Ocean, map.ObtainDot(0, row));
				Assert.Equal(Ocean, map.ObtainDot(1, row));
			}

			int newWorld = Enumerable.Range(0, 50).SelectMany(row => Enumerable.Range(2, 10).Select(column => map.ObtainDot(column, row))).Count(colour => colour != Ocean);
			Assert.True(newWorld > 180, $"the New World has {newWorld} tiles of land");
		}

		[Theory]
		[InlineData(79, 0, true)]
		[InlineData(0, 79, true)]
		[InlineData(40, 41, false)]
		[InlineData(0, 1, false)]
		[InlineData(78, 79, false)]
		public void TheEdgeCannotBeCrossed(int fromColumn, int toColumn, bool crosses)
		{
			Assert.Equal(crosses, EuropeMap.CrossesEdge(fromColumn, toColumn));
		}

		[Fact]
		public void TheNewWorldIsRich()
		{
			using VideoGraphicsBitmap map = Map();
			int land = 0, resources = 0, villages = 0;

			for (int row = 0; row < 50; row++)
			{
				for (int column = EuropeMap.NewWorldFirstColumn; column <= EuropeMap.NewWorldLastColumn; column++)
				{
					if (map.ObtainDot(column, row) == Ocean)
					{
						continue;
					}

					land++;
					resources += EuropeMap.RichResource(column, row) ? 1 : 0;
					villages += EuropeMap.ExtraVillage(column, row) ? 1 : 0;
					Assert.False(EuropeMap.RichResource(column, row) && EuropeMap.ExtraVillage(column, row));
				}
			}

			// About one tile in four with a resource and one in seven with a village, on top of the usual ones.
			Assert.InRange(resources, land / 6, land / 3);
			Assert.InRange(villages, land / 12, land / 4);
			Assert.False(EuropeMap.RichResource(40, 25));
			Assert.False(EuropeMap.ExtraVillage(40, 25));
		}

		[Fact]
		public void TheStraitsLetShipsAndArmiesThrough()
		{
			using VideoGraphicsBitmap map = Map();

			// The Bosporus: a chessboard of land and sea. Ships sail from the Mediterranean into the Black Sea and
			// armies walk from Thrace into Anatolia, both diagonally.
			HashSet<(int Column, int Row)> sea = Flood(map, (40, 38), true);
			Assert.Contains((60, 33), sea);
			Assert.Contains((41, 18), sea);

			HashSet<(int Column, int Row)> land = Flood(map, EuropeMap.Start(3), false);
			Assert.Contains(((int)EuropeMap.Start(6).Column, (int)EuropeMap.Start(6).Row), land);
			Assert.Contains((60, 38), land);
			Assert.Contains(((int)EuropeMap.Start(9).Column, (int)EuropeMap.Start(9).Row), land);
			Assert.DoesNotContain((5, 30), land);
		}

		private static HashSet<(int Column, int Row)> Flood(VideoGraphicsBitmap map, (int Column, int Row) start, bool water)
		{
			HashSet<(int Column, int Row)> seen = new() { start };
			Queue<(int Column, int Row)> open = new();
			open.Enqueue(start);

			while (open.Count > 0)
			{
				(int column, int row) = open.Dequeue();

				for (int stepColumn = -1; stepColumn <= 1; stepColumn++)
				{
					for (int stepRow = -1; stepRow <= 1; stepRow++)
					{
						(int Column, int Row) next = (column + stepColumn, row + stepRow);

						if (next.Column >= 0 && next.Column < 80 && next.Row >= 0 && next.Row < 50 &&
							(map.ObtainDot(next.Column, next.Row) == Ocean) == water && seen.Add(next))
						{
							open.Enqueue(next);
						}
					}
				}
			}

			return seen;
		}

		private static VideoGraphicsBitmap Map()
		{
			return VideoGraphicsBitmap.ViaFile(Path.Combine(AppContext.BaseDirectory, "Assets", "Pictures", "EUROPE.PIC"), out _)!;
		}
	}
}
