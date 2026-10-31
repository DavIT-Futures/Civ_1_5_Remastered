using System.Runtime.InteropServices;

namespace Civilization1Remaster.Core.Game
{
	/// <summary>The operations of ForeignAffairs that the other parts of the game use</summary>
	public interface IForeignAffairs
	{
		/// <summary>
		/// The treaties of a player each turn. Anarchy ends (the human player every 4th turn, others every 4th turn or with
		/// the wonder effect 1) in despotism, with the revolution flag 4 of the relation 0 (an AI player with it goes back to
		/// its government). For every other player: a broken treaty (0x100) is forgotten one time in 3 (war again, 0x200);
		/// every 16th turn the peace of a war (1 without 2) ends with a message when the human player knows one of them,
		/// and the flags 1 and 0x10 are cleared both ways. Every 32nd turn (the player's turn, 4 * player) an AI player that
		/// asked for help (0x20) against a player at war with the human player sends an emissary: a much weaker one pays
		/// tribute for an alliance, a stronger one threatens (the name of the enemy; with ClassicEmissaryThreat read at
		/// 0x1992 + 16 * player, past the table, like the translated code). SI and DI get the values of the original before
		/// the calls.
		/// </summary>
		void HandlePeaceTreaties(short participantIdx);

		/// <summary>
		/// The player's government becomes regimeKind. A human player (ParticipantMarkers) chooses it from the governments it
		/// may have (all with the wonder effect 1, else despotism plus those of the technologies 5, 0x2b, 0x2c and 4; regimeKind 0
		/// gives anarchy without asking), then the revolution screen. For an AI player a known civilization (0x40 of the human's
		/// relation) is announced as overthrown (anarchy, 4 of the relation 0) or as changed to the new government after it;
		/// the luxuries become 4 - government / 2. A new government goes into the replay and ends the 8 of every relation.
		/// </summary>
		void SwitchGovernment(short participantIdx, short regimeKind);

		/// <summary>
		/// Two players (not the barbarians) meet: with the human player an audience with the other one (unless in contact and
		/// met in the last 16 turns); both get contact (0x11). Two AI players (one meeting in four, or the first one) without
		/// vendetta (8) decide: when one of them wants war (AIWeighsTreaty, without the wonder effect 7 of the other and with a
		/// government below 4) a treaty or a first contact ends in war, else they exchange technologies and make peace; the
		/// human player hears of it when it knows one of them (0x40).
		/// </summary>
		void BargainPeaceTreaty(short participantIdx1, short participantIdx2, int horizLoc, int vertLoc);

		/// <summary>
		/// Sets the relation bits between the two players (both ways); a new peace (2) goes into the replay and clears the bit
		/// 0x20
		/// </summary>
		void AssignDiplomacyFlags(short participantIdx1, short participantIdx2, ushort markers);

		/// <summary>
		/// Clears the relation bits between the two players (both ways); a peace ended (2) goes into the replay and clears the
		/// bit 4
		/// </summary>
		void ClearDiplomacyFlags(short participantIdx1, short participantIdx2, ushort markers);

		/// <summary>
		/// Whether the AI player 1 accepts peace with the AI player 2 (only with the 1 of the diagnostic markers, else always):
		/// not when the other one is weaker than the human player and the first is at peace with the human (without the 4 of the
		/// other's relation), not with fewer spaceship parts (unit 25), not when on a continent with its cities its defence (half)
		/// plus attack is below the other's defence; then it accepts when 4 * its defence / (1 + the other's attack) on the
		/// shared continents is at least its peace treaties with the others (+1 with the 2 of the relation) minus the leader's
		/// aggression plus 4, or when it has no cities on them.
		/// </summary>
		/// <returns>1 when it accepts, else 0, also in AX</returns>
		ushort AIWeighsTreaty(short participantIdx1, short participantIdx2);

		/// <summary>
		/// Two players exchange technologies when each knows one the other does not (the last such of the 72 for each side)
		/// </summary>
		/// <remarks>In AX: 1 when they exchanged, else 0</remarks>
		void SwapTechnology(short participantIdx1, short participantIdx2);

		/// <summary>
		/// Distance from the position to the nearest city of the player with a palace (structure bit 0), at most 16
		/// </summary>
		/// <returns>Distance, also in AX</returns>
		ushort FindNearestCityOfPlayer(short participantIdx, int horizLoc, int vertLoc);
	}
}
