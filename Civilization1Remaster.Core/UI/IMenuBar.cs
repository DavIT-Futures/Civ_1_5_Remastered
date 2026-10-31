using Civilization1Remaster.Emulation;

namespace Civilization1Remaster.UI
{
	/// <summary>The operations of MenuBar that the other parts of the game use</summary>
	public interface IMenuBar
	{
		/// <summary>
		/// Opens a menu of the menu bar (marker 0 - 4: Game, Orders, Advisors, World, Civilopedia; -1: the one under the mouse,
		/// column / 60), with the screen kept in the back screen 0x19d4. Afterwards, by the menu's result in 0x654a: 1 draws the
		/// map screen again, 0 puts the screen back.
		/// </summary>
		void ManageMenuBar(short participantIdx, short troopIdx, short marker);

		/// <summary>
		/// The Game menu: Tax Rate, Luxuries Rate, FindCity, Save Game, REVOLUTION!, Retire and QUIT to DOS are passed to the
		/// turn as commands (0xd4ca, the last two with 0xdc48), Load Game chooses the saved game to load after the turn, Options
		/// toggles the options until Escape, View Replay (after the end of the game, 0x100 of the spaceships) shows the replay.
		/// </summary>
		void ShowGameMenu();

		/// <summary>
		/// The Orders menu of the unit (not for -1 and 128): No Orders, for settlers the city, road / railroad, irrigation or
		/// the change of terrain (0x44a), mines or the change (0x44e), pollution, fortress (needs technology 0x2d); Fortify for
		/// land units; Wait, Sentry, GoTo; Pillage on an improved tile; Home City in a city; Unload for transports (role 5 or
		/// type 0x17) with units; Disband. Lines that cannot be chosen are set in 0xb276. The key of the chosen line goes to 0xd4ca
		/// (-1 when none).
		/// </summary>
		void ShowOrdersMenu(short participantIdx, short troopIdx);

		/// <summary>
		/// The Advisors menu: City Status, Military (the score list when all units are shown, 0xd806, else the unit counts),
		/// Intelligence, Attitude, Trade and Science; the screen is put back from 0x19d4 before the chosen report
		/// </summary>
		void ShowAdvisorMenu();

		/// <summary>
		/// The World menu: Wonders, Top 5 Cities, Civilization Score, World Map, Demographics and SpaceShips (disabled, 0xb276,
		/// when nobody has a spaceship); the screen is put back from 0x19d4 before the chosen report
		/// </summary>
		void ShowWorldMenu();

		/// <summary>
		/// The Civilopedia menu: the chosen part is shown (0x654a = -1 afterwards), Escape sets 0x654a to 1
		/// </summary>
		void ShowCivilopediaMenu();
	}
}
