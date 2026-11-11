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

		/// <summary>The map picture, in the format of the original map.pic; it ships in Assets/Pictures.</summary>
		public const string Picture = "europe.pic";

		/// <summary>Where each leader starts (tile column and row); the civilizations outside Europe start in its edges.</summary>
		private static readonly Dictionary<int, (byte Column, byte Row)> Starts = new()
		{
			[1] = (38, 34),   // Romans: Rome
			[2] = (72, 45),   // Babylonians: Babylon
			[3] = (39, 22),   // Germans: Berlin
			[4] = (58, 48),   // Egyptians: Memphis
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

		public static bool Chosen(ushort earthGame)
		{
			return earthGame == Marker;
		}

		public static (short Column, short Row) Start(int leader)
		{
			return Starts.TryGetValue(leader, out (byte Column, byte Row) start) ? (start.Column, start.Row) : ((short)40, (short)25);
		}
	}
}
