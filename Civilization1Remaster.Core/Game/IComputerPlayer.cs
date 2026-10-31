using System;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;
using Civilization1Remaster.UI;

namespace Civilization1Remaster.Game
{
	/// <summary>The operations of ComputerPlayer that the other parts of the game use</summary>
	public interface IComputerPlayer
	{
		/// <summary>
		/// The yearly survey of a player (every player, also the human one): counts the units and cities by continent, marks
		/// the 4 x 4 blocks of the map where they are (0x3772:0x2438), picks the approach of every continent (0 build up,
		/// 1 defend, 2 the enemy is stronger, 5 attack), and for a computer player also changes the government, gives up
		/// obsolete units and sends the units to the strategic locations (0xe2c2, a table of 32 per player)
		/// </summary>
		void OperateAITurn(short participantIdx);

		/// <summary>
		/// The move of a barbarian unit for ShiftAIUnit: a unit in the rows 0 - 1 or from 48 on is taken off; a ship (group 5)
		/// carries its troops to land, the others attack, take or wander (ManageBarbarianSeaTransport,
		/// ManageBarbarianFieldUnit). Returns the order.
		/// </summary>
		ushort ManageBarbarianUnit(short participantIdx, short troopIdx);

		/// <summary>
		/// Sends to the city the loaded transports at sea of the players 1 - 7 (not the human player, and only those without
		/// relations bit 2 towards the human player) that are closer to it than 3 turns of moves
		/// </summary>
		void PostCityDefenders(short townIdx);

		/// <summary>
		/// Sets up the strategic locations of the player again: its 32 entries at 0xe2c2 + 32 * player become empty (0xff, with
		/// 0 at 0xb2be + ...), then a unit is sent to each of its 16 locations that is in use with a state up to 2
		/// </summary>
		void ReimposeStrategicLocations(short participantIdx);

		/// <summary>
		/// Puts the place into the list of strategic locations of the player (32 entries a player in the byte arrays at 0xe598
		/// horizontal, 0xe798 vertical, 0xe2c2 role, 0xb2be strategy), unless it is there already for the role with no higher
		/// strategy. On the turn of an AI player (0x6b90), for the roles 3 and 4 the first unit of the role on the same continent
		/// with enough moves left (twice the distance) goes there. The place takes the first free entry or the first one with a
		/// lower strategy (the rest move one on); with none, it is not listed.
		/// </summary>
		void SendUnitToStrategicLocation(short participantIdx, short horizLoc, short vertLoc, short live, short strategy);

		/// <summary>
		/// Makes room at the place of the list of the player (32 entries a player in the byte arrays at 0xb2be, 0xe2c2, 0xe598
		/// and 0xe798): the used entries from the place up to 30 move one place on
		/// </summary>
		void FreeStrategicLocation(short participantIdx, ushort place);

		/// <summary>Clears the 16 tactical sites of the player (none, no strategy)</summary>
		void ClearStrategicLocations(short participantIdx);

		/// <summary>
		/// Notes a strategic location of the player (the 16 places are sorted by strategy, highest first): nothing when it is
		/// already there with the same state and at least the strategy; otherwise it goes in before the first free place or
		/// the first place with a lower strategy (AffixStrategicLocation moves the rest down); no room, no change
		/// </summary>
		void AttachStrategicLocation(short participantIdx, ushort horizLoc, ushort vertLoc, ushort live, ushort strategy);

		/// <summary>
		/// Makes room at the place of the strategic locations of the player: the used ones from the place up to 14 move one place on
		/// </summary>
		void AffixStrategicLocation(short participantIdx, ushort place);

		/// <summary>
		/// The strategic locations of the player of the kind (Live = role) not farther than reach from the position are given up
		/// (Live = -1)
		/// </summary>
		void DisableStrategicLocation(short participantIdx, ushort role, int horizLoc, int vertLoc, ushort reach);

		/// <summary>
		/// The cities of the player choose their production again (PickNewCityProduction): all of them, or with continent not 0xffff
		/// only the cities on that continent
		/// </summary>
		void RefreshCityProduction(short participantIdx, ushort continent);

		/// <summary>
		/// The city chooses its production again (ResearchOffice.PickCityProduction); the count of the units of each type
		/// its player is making (TroopsInsideMaking) follows the change
		/// </summary>
		void PickNewCityProduction(short townIdx);

		/// <summary>
		/// The direction (0 - 8, 0 is the tile itself) of the ocean tile at or around (horizLoc, vertLoc), without units
		/// unless it is the tile itself, with the most empty land tiles around it (with relations bit 2 towards the human
		/// player, land of the human player, map layer 2 at column + 80, does not count); -1 without such an ocean tile
		/// </summary>
		ushort MayAIEnterTile(short participantIdx, ushort horizLoc, ushort vertLoc);

		ushort ShiftAIUnit(short participantIdx, short troopIdx);
	}
}
