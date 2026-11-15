using System;
using System.Collections.Generic;

namespace Civ_1_5_Remastered.Core.Shared.State
{
	/// <summary>
	/// The EUROPE world of the main menu: a fixed map like EARTH (Europe, North Africa and the Near East, with a
	/// rich New World across the Atlantic, nobody's start), drawn from tools/maps. The game marks it with
	/// <c>EarthGame = 2</c>, so everything a fixed world does (no random land, fixed starts) applies.
	/// </summary>
	public static class EuropeMap
	{
		public const ushort Marker = 2;

		/// <summary>EUROPE with true start locations: <c>EarthGame = 3</c> while the game is set up (then it is Europe as any).</summary>
		public const ushort TrueStartsMarker = 3;

		public const int PolishLeader = 16;

		/// <summary>
		/// What the human player may play with true start locations: the civilizations at home on the map (Europe,
		/// Egypt, Babylon and the Mongols of the Volga), none from far away (no Americans, Chinese, Aztecs, Zulus,
		/// Indians or Japanese). Poles, Romans, Russians, Germans, French, English, Greeks, Babylonians, Egyptians, Mongols.
		/// </summary>
		public static readonly int[] PlayableLeaders = [16, 1, 9, 3, 11, 14, 6, 2, 4, 15];

		/// <summary>The map picture, in the format of the original map.pic; it ships in Assets/Pictures.</summary>
		public const string Picture = "europe.pic";

		/// <summary>The columns of the New World (and the sea around it), across the Atlantic at the left edge.</summary>
		public const int NewWorldFirstColumn = 2;
		public const int NewWorldLastColumn = 12;

		/// <summary>Where each leader starts (tile column and row); the civilizations outside Europe start in its edges.</summary>
		private static readonly Dictionary<int, (byte Column, byte Row)> Starts = new()
		{
			[1] = (38, 34),   // Romans: Rome
			[2] = (72, 45),   // Babylonians: Babylon
			[3] = (39, 22),   // Germans: Berlin
			[4] = (58, 47),   // Egyptians: Memphis (no city stands in the last two rows of the map)
			[5] = (19, 21),   // Americans: Ireland
			[6] = (50, 39),   // Greeks: Athens
			[7] = (73, 41),   // Indians: Persia
			[9] = (65, 18),   // Russians: Moscow
			[10] = (36, 40),  // Zulus: Carthage
			[11] = (28, 26),  // French: Paris
			[12] = (22, 36),  // Aztecs: Spain
			[13] = (76, 18),  // Chinese: the middle Volga
			[14] = (25, 23),  // English: London
			[15] = (73, 27),  // Mongols: Sarai
			[16] = (46, 25),  // Poles: Krakow
			[17] = (44, 14),  // Japanese: Scandinavia
		};

		/// <summary>
		/// The opponents of the true start locations, one for each slot (1 to 7): Russians, Babylonians, French,
		/// Egyptians, Chinese, English and Mongols; Poland plays in every game and takes the slot of one of them.
		/// </summary>
		private static readonly int[] RivalLeaders = [-1, 9, 2, 11, 4, 13, 14, 15];

		/// <summary>The slot of the Chinese, the one Poland takes first: China plays only in about half of the games.</summary>
		private const int ChineseSlot = 5;

		public static bool Chosen(ushort earthGame)
		{
			return earthGame == Marker || earthGame == TrueStartsMarker;
		}

		/// <summary>Whether Europe is played with the true start locations: always the same seven civilizations.</summary>
		public static bool TrueStarts(ushort earthGame)
		{
			return earthGame == TrueStartsMarker;
		}

		/// <summary>
		/// The leaders of the seven slots (index 1 to 7) with true start locations. The human player's civilization
		/// takes its own slot (Poland a slot drawn like below); every other slot plays its opponent of
		/// <see cref="RivalLeaders"/>, except one that Poland takes when the human player does not play it: the
		/// slot of the Chinese in half of the games, otherwise another one drawn at random. <paramref name="draw"/>
		/// gives a number from 0 to its argument less one.
		/// </summary>
		public static int[] TrueStartLineup(int playerLeader, Func<int, int> draw)
		{
			int[] lineup = (int[])RivalLeaders.Clone();

			int PolishSlot(int taken)
			{
				if (taken != ChineseSlot && draw(2) == 0)
				{
					return ChineseSlot;
				}

				List<int> free = new List<int>();

				for (int slot = 1; slot <= 7; slot++)
				{
					if (slot != taken && slot != ChineseSlot)
					{
						free.Add(slot);
					}
				}

				return free[draw(free.Count)];
			}

			int playerSlot = playerLeader == PolishLeader ? PolishSlot(0) : playerLeader & 7;
			lineup[playerSlot] = playerLeader;

			if (playerLeader != PolishLeader)
			{
				lineup[PolishSlot(playerSlot)] = PolishLeader;
			}

			return lineup;
		}

		/// <summary>Whether a game is played on Europe: the marker kept with the game (RemasterExtras.World).</summary>
		public static bool Played(byte world)
		{
			return world == Marker;
		}

		/// <summary>
		/// Europe does not wrap: the map ends at its left and right edges. A step from the last column to the first
		/// (or back) is the only way across the seam, so it is forbidden to every unit.
		/// </summary>
		public static bool CrossesEdge(int fromColumn, int toColumn)
		{
			return Math.Abs(fromColumn - toColumn) > 1;
		}

		public static bool InNewWorld(int column)
		{
			return column >= NewWorldFirstColumn && column <= NewWorldLastColumn;
		}

		/// <summary>The New World is rich: besides the usual ones, about one tile in four has a special resource.</summary>
		public static bool RichResource(int column, int row)
		{
			return InNewWorld(column) && row > 1 && row < 48 && Mix(column, row) % 4 == 0;
		}

		/// <summary>And about one tile in seven holds a village of its peoples, never on a tile with a resource.</summary>
		public static bool ExtraVillage(int column, int row)
		{
			return InNewWorld(column) && row > 1 && row < 48 && !RichResource(column, row) && Mix(column, row) % 7 == 3;
		}

		public static (short Column, short Row) Start(int leader)
		{
			return Starts.TryGetValue(leader, out (byte Column, byte Row) start) ? (start.Column, start.Row) : ((short)40, (short)25);
		}

		private static int Mix(int column, int row)
		{
			unchecked
			{
				uint mixed = ((uint)column * 2654435761u) ^ ((uint)row * 40503u) ^ 0x5bd1e995u;
				mixed ^= mixed >> 13;
				return (int)(mixed % 1009u);
			}
		}
	}
}
