using System.Drawing;
using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>The operations of SettlementAndScoring that the other parts of the game use</summary>
	public interface ISettlementAndScoring
	{
		/// <summary>
		/// Changes the terrain under a new city to what irrigation makes of it, when irrigation changes the terrain type.
		/// The irrigation result of a terrain type (12 bytes per type, at 0x44a) is -1 when it can't be irrigated,
		/// -2 when it is irrigated without change, other values are the new terrain (as the settlers code uses it).
		/// </summary>
		/// <param name="xPos"></param>
		/// <param name="yPos"></param>
		void ClearCitySquare(int horizLoc, int vertLoc);

		/// <summary>
		/// Founds a city of the player at the place in the first free city slot (none: -1, the human player is told). Its name
		/// is the next free one of the leader's 16 names (then of the shared names 0xe0 - 0xff, then any); the human player may
		/// rename it (cancelled: -1, the name stays taken). The tile loses its improvements and gets city and road (and
		/// irrigation where the terrain allows); production settlers (militia 4 with technology 0x22, 5 with 0x40); the first
		/// city gets the palace; coastal (2, ocean of more than 256 tiles or for the human player) and river/hills (8) flags;
		/// its 21 tiles marked in the owner's colour on the power map (screen 2) and the outer ones made at most colour 8; a
		/// human neighbour learns of an AI city next to his city or with wonder 0x13. Returns the city. SI gets the values of
		/// the original before the calls (dialogs leave it in DI).
		/// </summary>
		ushort EstablishCity(short participantIdx, ushort unused, int horizLoc, int vertLoc, ushort townExtent);

		/// <summary>
		/// A random event in the city (one in ten each): earthquake near hills (a building), plague without aqueduct (0x100;
		/// technology 10 protects), flood near a river (0x800 terrain, without city walls 0x80), fire near mountains (0x20,
		/// without temple 8), famine (without granary 4), pirates/vandals (a building, without aqueduct), volcano near
		/// mountains (0x400, without barracks 2), and three in ten a revolt when the unhappy outnumber the happy: a demand,
		/// food and production lost, and a city of another player with more prosperity (happy - unhappy * 32 / distance) may
		/// buy it. A human player sees the message (the first line for a city he only sees). SI and DI get the values of the
		/// original before the calls (HandleCity reads below the stack).
		/// </summary>
		void HandleCityDisasters(short townIdx);

		/// <summary>
		/// The final score of the player; with the marker also its screen (the preamble, the note of the spaceship, the screen
		/// put back). Returns the score.
		/// </summary>
		ushort ShowFinalScore(short participantIdx, bool marker);

		/// <summary>
		/// The next icon of the score screen: 8 pixels right of the last one (0xb25c); after x 292 a new row (0xb2bc grows by
		/// 0x5116) starting at x 16 or 20. Nothing is drawn below row 160.
		/// </summary>
		/// <param name="icon">The citizen picture of the icon (CitizenPicture)</param>
		void DrawScoreIcon(ushort icon);
	}
}
