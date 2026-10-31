using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.Rules;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>The operations of ResearchOffice that the other parts of the game use</summary>
	public interface IResearchOffice
	{
		/// <summary>
		/// The turn of every city of the player (a city that needs it, 0xb1e8, is handled once more with the screen, 2 = again
		/// from the start); then an AI player (with the 4 of the diagnostic markers) sets its taxes: more luxuries with a city
		/// in disorder, less with no city of size above 5 that ended its turn well every fourth turn, science by the leaning of
		/// its leader and its gold, none after the last technology (0x3f).
		/// </summary>
		void HandleAllCities(short participantIdx);

		/// <summary>
		/// Destroys the city at the position: notes it in the replay, takes it from its player (and the unit it was building),
		/// removes it from the map, the trade routes of the other cities and the wonders (city 128 = destroyed), eliminates the
		/// units it supported, applies 0x1643 to the stack still on the tile, and redraws the territory around it (map layer 2;
		/// the outer ring only when the nearest city, 0x6c9a, is more than 3 away). The stack frame and the saved SI and DI stay
		/// like in the original: VisitEachUnitInStack changes SI, and the unit functions run at the same stack depth.
		/// </summary>
		void FinalizeProduction(short townIdx, ushort horizLoc, ushort vertLoc);

		/// <summary>Keeps the screen in the back screen before the screen of an advisor</summary>
		void RenderAdvisorScenery();

		/// <summary>Puts the screen back from the back screen after the screen of an advisor</summary>
		void RecoverAdvisorScenery();

		/// <summary>
		/// Opens the city screen from an advisor: the advisor screen is kept, the city is handled with its screen (HandleCity),
		/// then the advisor screen comes back, or the main screen when the city screen asked for it (0x6b64)
		/// </summary>
		void RenewCityScreen(short townIdx);

		/// <summary>
		/// What a city builds next. The menu text (TextBuffer.Address, "What shall we build in ...") lists the units, improvements and
		/// wonders the player can build, with a list of the choices in the frame (bp - 0xdc, units >= 0, improvements and
		/// wonders negative, 0x63 / 0x62 for "more..."); every choice gets a score for the city (lower is better). A computer
		/// player takes the best one (1 / 3 / 7 for a player without units or the barbarians); for the human player the advisors
		/// name their best unit and improvement once, then the menu asks, with the Civilopedia on demand.
		/// </summary>
		ushort PickCityProduction(short participantIdx, short townIdx);

		/// <summary>
		/// Appends " (n turns)" or " (1 turn)" to the text at TextBuffer.Address: the turns the city needs for the production of cost price
		/// (in tens of shields), with the shields per turn 0x70dc - 0xd2f6 (1 - 99); 1 - 999 turns
		/// </summary>
		void ComposeProductionOption(short townIdx, ushort price);

		/// <summary>
		/// The player's research is done (participantIdx1: where it came from, see EarnTechnology). An AI player (and the
		/// human player in turn 0) takes the researchable technology with the highest random draw of 4 * ObtainTechnologyInfo
		/// (not the human player's current research for himself); on the easiest level a technology the human player doesn't
		/// know only one time in 2; returns it (-1 for none or barbarians). The human player gets his research (the discovery
		/// picture with its icon, or the newspaper), the science advisor's recommendation (easiest level, preference 1) and
		/// chooses the next research from the menu (on harder levels from the third on only every third one; asking for the
		/// Civilopedia shows it and asks again with the same choices; none left: future technology 71); returns the choice.
		/// The list of the choices and the file name are in the stack frame, and SI gets the values of the original before the
		/// calls (DisplayMenu leaves it in DI), like in the original.
		/// </summary>
		ushort UncoverTechnology(short participantIdx, short participantIdx1);

		/// <summary>
		/// The player learns a technology (from participantIdx1: himself or 0 by research, -2 the Great Library, else a player);
		/// 0x47 (future technology) is only counted. After turn 0: the human player and the players he has contact with
		/// announce it (with the wonders and units it allows, for the others), a player without contact may send an emissary
		/// the first time anybody learns it (technology 8, one time in 3); the human player sees the Civilopedia, the new
		/// governments with 0x14, the help with 5; technologies that make barracks obsolete sell them; the first player to learn
		/// a technology cancels the effect of the human player's wonders it makes obsolete; the owner of the Great Library
		/// (wonder 5) learns it when two players know it. SI and DI get the values of the original before the calls.
		/// </summary>
		void EarnTechnology(short participantIdx, short scienceIdx, short participantIdx1);

		/// <summary>1 when the player knows the technology, else 0</summary>
		ushort PlayerKnowsTechnology(short participantIdx, short scienceIdx);

		/// <summary>
		/// Calculates the AI leader preference value for technology research.
		/// </summary>
		/// <param name="participantIdx"></param>
		/// <param name="scienceIdx"></param>
		ushort ObtainTechnologyInfo(short participantIdx, ushort scienceIdx);
	}
}
