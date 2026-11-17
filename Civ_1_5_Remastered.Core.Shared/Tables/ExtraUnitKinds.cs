using System;
using Civ_1_5_Remastered.GameModel.Rules;

namespace Civ_1_5_Remastered.Core.Shared.Tables
{
	/// <summary>
	/// Unit kinds the remaster adds after the 28 kinds of the original game. Their records have the layout of the
	/// original unit table (see <see cref="UnitKindTable"/>), but live here: the original table is followed by the
	/// leader table in memory and is stored in saved games, so it cannot grow.
	/// </summary>
	/// <remarks>
	/// The original save format knows only 28 kinds: a saved game writes every unit of an extra kind as its
	/// <see cref="StandIn"/>, and the remastered extras file remembers the real kind (see RemasterExtras).
	/// The map pictures of a civilization have 32 places for 28 kinds, so up to four extra kinds have room.
	/// </remarks>
	public static partial class ExtraUnitKinds
	{
		public const int NameLength = 0xc;

		public const int Gunpowder = 0x22;

		public const int IronWorking = 0x12;

		private static readonly Kind[] Kinds =
		{
			// Pikemen: a defender of the Iron Working age between the Phalanx and the Musketeers.
			new Kind("Pikemen", UnitKinds.Phalanx, 25, obsoleteBy: Gunpowder, landGroup: UnitKindTable.LandUnits, moves: 1, attack: 1, defence: 3,
				role: UnitKindTable.DefenceTroops, technology: IronWorking,
				civilopedia: "Pikemen were foot soldiers armed with pikes, spears four to six metres long. Standing shoulder to shoulder in deep " +
					"ranks, they faced the enemy with a wall of iron points that stopped charging horsemen and sheltered the troops behind it. " +
					"Iron Working made strong spearheads cheap enough to arm whole formations, and the pike held the battlefield until " +
					"firearms took its place.\n\nPikemen are the strongest defenders before Gunpowder. They cost 25 resources, and a " +
					"Phalanx can be upgraded to Pikemen in a city.",
				advice: "We should build Pikemen\nto defend our cities."),
		};

		public static int Count => Kinds.Length;

		public static bool Contains(int kind) => kind >= UnitKindTable.OriginalCount && kind < UnitKindTable.OriginalCount + Kinds.Length;

		public static string Name(int kind) => Of(kind).Name;

		/// <summary>The Civilopedia text of <paramref name="kind"/>.</summary>
		public static string Civilopedia(int kind) => Of(kind).CivilopediaText;

		/// <summary>What an advisor says when the kind is a production choice.</summary>
		public static string Advice(int kind) => Of(kind).Advice;

		/// <summary>The original kind that stands for <paramref name="kind"/> in the original save format.</summary>
		public static int StandIn(int kind) => Contains(kind) ? Of(kind).StandIn : kind;

		/// <summary>The price in shields (the original table counts in tens of shields).</summary>
		public static int ShieldPrice(int kind) => Of(kind).ShieldPrice;

		/// <summary>A word of the record of <paramref name="kind"/>, as the original table would hold it.</summary>
		public static ushort Word(int kind, int field) => (ushort)(Of(kind).Record[field] | (Of(kind).Record[field + 1] << 8));

		/// <summary>A byte of the record of <paramref name="kind"/> (the name takes the first bytes).</summary>
		public static byte Byte(int kind, int field) => Of(kind).Record[field];

		private static Kind Of(int kind) =>
			Contains(kind) ? Kinds[kind - UnitKindTable.OriginalCount] : throw new ArgumentOutOfRangeException(nameof(kind), kind, "Not an extra unit kind.");
	}
}
