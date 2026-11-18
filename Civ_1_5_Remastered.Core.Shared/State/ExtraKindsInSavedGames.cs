using Civ_1_5_Remastered.Core.Shared.Tables;
using Civ_1_5_Remastered.GameModel.State;

namespace Civ_1_5_Remastered.Core.Shared.State
{
	/// <summary>
	/// The original save format knows 28 unit kinds and 24 city improvements. While a game is written, every unit,
	/// production and kept unit of an extra kind turns into its stand-in (Pikemen into a Phalanx), the tallies of the extra
	/// kind are added to the stand-in's, a city making an extra improvement makes its stand-in (a Stockade: City Walls)
	/// and the city bits of extra improvements are cleared, so the original game reads a whole, consistent game; the
	/// remastered extras file keeps the real kinds and bits, which come back after writing and after loading.
	/// </summary>
	public static class ExtraKindsInSavedGames
	{
		private const int KindBits = 0x3f;

		private const int ExtraStructuresShift = 24;

		/// <summary>Turns every extra kind into its stand-in and remembers it in the extras.</summary>
		public static void Hide(GameState world, UnitMemory troops)
		{
			RemasterExtras extras = world.Extras;
			extras.ClearExtraKinds();

			for (int participantIndex = 0; participantIndex < RemasterExtras.ParticipantTally; participantIndex++)
			{
				for (int troopIndex = 0; troopIndex < RemasterExtras.TroopTally; troopIndex++)
				{
					int kind = troops.TroopKind(participantIndex, troopIndex);

					if (ExtraUnitKinds.Contains(kind))
					{
						extras.AssignTroopKind(participantIndex, troopIndex, (byte)kind);
						AssignTroopKind(world, troops, participantIndex, troopIndex, ExtraUnitKinds.StandIn(kind));
					}
				}

				Player player = world.Participants[participantIndex];

				for (int place = 0; place < ExtraUnitKinds.Count && place < RemasterExtras.ExtraKindPlaces; place++)
				{
					int kind = UnitKindTable.OriginalCount + place;
					int standIn = ExtraUnitKinds.StandIn(kind);
					short[][] lists = Lists(player);

					for (int list = 0; list < lists.Length; list++)
					{
						extras.AssignKindTally(participantIndex, list, place, lists[list][kind]);
						lists[list][standIn] += lists[list][kind];
						lists[list][kind] = 0;
					}
				}
			}

			for (int townIndex = 0; townIndex < RemasterExtras.TownTally && townIndex < world.Towns.Length; townIndex++)
			{
				City town = world.Towns[townIndex];

				if (ExtraUnitKinds.Contains(town.PresentMakingIndex))
				{
					extras.AssignMakingKind(townIndex, (byte)town.PresentMakingIndex);
					town.PresentMakingIndex = (sbyte)ExtraUnitKinds.StandIn(town.PresentMakingIndex);
				}
				else if (ExtraBuildings.Contains(-town.PresentMakingIndex))
				{
					extras.AssignMakingKind(townIndex, (byte)town.PresentMakingIndex);
					town.PresentMakingIndex = (sbyte)-ExtraBuildings.StandIn(-town.PresentMakingIndex);
				}

				if (town.ConditionMarker != 0xff)
				{
					extras.AssignExtraStructures(townIndex, (byte)(town.Structures >> ExtraStructuresShift));
					town.Structures &= (1u << ExtraStructuresShift) - 1;
				}

				for (int place = 0; place < RemasterExtras.KeptTroopPlaces; place++)
				{
					byte kept = (byte)town.Unfamiliar[place];
					int kind = kept & KindBits;

					if (kept != 0xff && ExtraUnitKinds.Contains(kind))
					{
						extras.AssignKeptKind(townIndex, place, (byte)kind);
						town.Unfamiliar[place] = (sbyte)((kept & ~KindBits) | ExtraUnitKinds.StandIn(kind));
					}
				}
			}
		}

		/// <summary>Brings back the extra kinds the extras remember, where the game still holds their stand-ins.</summary>
		public static void Reveal(GameState world, UnitMemory troops)
		{
			RemasterExtras extras = world.Extras;

			for (int participantIndex = 0; participantIndex < RemasterExtras.ParticipantTally; participantIndex++)
			{
				for (int troopIndex = 0; troopIndex < RemasterExtras.TroopTally; troopIndex++)
				{
					int kind = extras.TroopKind(participantIndex, troopIndex);

					if (ExtraUnitKinds.Contains(kind) && troops.TroopKind(participantIndex, troopIndex) == ExtraUnitKinds.StandIn(kind))
					{
						AssignTroopKind(world, troops, participantIndex, troopIndex, kind);
					}
				}

				Player player = world.Participants[participantIndex];

				for (int place = 0; place < ExtraUnitKinds.Count && place < RemasterExtras.ExtraKindPlaces; place++)
				{
					int kind = UnitKindTable.OriginalCount + place;
					int standIn = ExtraUnitKinds.StandIn(kind);
					short[][] lists = Lists(player);

					for (int list = 0; list < lists.Length; list++)
					{
						short tally = extras.KindTally(participantIndex, list, place);
						lists[list][kind] = tally;
						lists[list][standIn] -= tally;
					}
				}
			}

			for (int townIndex = 0; townIndex < RemasterExtras.TownTally && townIndex < world.Towns.Length; townIndex++)
			{
				City town = world.Towns[townIndex];
				int making = extras.MakingKind(townIndex);
				int makingBuilding = -(sbyte)(byte)making;

				if (ExtraUnitKinds.Contains(making) && town.PresentMakingIndex == ExtraUnitKinds.StandIn(making))
				{
					town.PresentMakingIndex = (sbyte)making;
				}
				else if (ExtraBuildings.Contains(makingBuilding) && town.PresentMakingIndex == -ExtraBuildings.StandIn(makingBuilding))
				{
					town.PresentMakingIndex = (sbyte)-makingBuilding;
				}

				if (town.ConditionMarker != 0xff)
				{
					town.Structures |= (uint)extras.ExtraStructures(townIndex) << ExtraStructuresShift;
				}

				for (int place = 0; place < RemasterExtras.KeptTroopPlaces; place++)
				{
					byte kept = (byte)town.Unfamiliar[place];
					int kind = extras.KeptKind(townIndex, place);

					if (kept != 0xff && ExtraUnitKinds.Contains(kind) && (kept & KindBits) == ExtraUnitKinds.StandIn(kind))
					{
						town.Unfamiliar[place] = (sbyte)((kept & ~KindBits) | kind);
					}
				}
			}
		}

		private static short[][] Lists(Player player) => new[] { player.LiveTroops, player.TroopsInsideMaking, player.MissingTroops };

		private static void AssignTroopKind(GameState world, UnitMemory troops, int participantIndex, int troopIndex, int kind)
		{
			troops.Assign(participantIndex, troopIndex, UnitMemory.Kind, (byte)kind);
			world.Participants[participantIndex].Troops[troopIndex].KindIndex = (short)kind;
		}
	}
}
