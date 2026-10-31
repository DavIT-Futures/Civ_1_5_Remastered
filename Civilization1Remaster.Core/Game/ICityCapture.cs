using Civilization1Remaster.Emulation;
using Civilization1Remaster.Game.State;

namespace Civilization1Remaster.Game
{
	/// <summary>The operations of CityCapture that the other parts of the game use</summary>
	public interface ICityCapture
	{
		/// <summary>
		/// The player takes the city (marker: by a diplomat, without damage). A captured capital of a big civilization of a
		/// better standing than the human player may split it (the new capital stops its production); a capital ends the
		/// spaceship. The plunder (up to the owner's gold) goes from the owner to the player. Barbarians with a land unit
		/// (role up to 2, unless 0x28bc) destroy the city; otherwise the city loses its production, some buildings (half of
		/// them by chance unless marker) and one size (unless marker and size 1). A city that remains: replay, strategic
		/// location, the power map marks around it. The human player hears of it (capture message, city screen or newspaper),
		/// else spies of known players report. Then technology, owner of the tiles, the owner's units of this city outside or
		/// in a destroyed one disappear, the production of the city moves to the new owner and the old one may be finished.
		/// SI and DI get the values of the original before the calls.
		/// </summary>
		void SeizeCity(short participantIdx, short townIdx, ushort marker);

		/// <summary>
		/// The player loses its spaceship: when it was launched (StarshipMarkers bit 8 + player) a message "... spaceship" with
		/// the text 0x28ca (also built: bit player) or 0x28dd; both bits are cleared and the parts wiped
		/// </summary>
		void EliminateSpaceship(short participantIdx);

		/// <summary>
		/// The gold taken with a city: the owner's gold * city size / (total size of the owner's cities + 1), divided first
		/// when the product would pass 32000. Signed word arithmetic like in the original (also its division errors).
		/// </summary>
		/// <returns>The gold, also in AX</returns>
		ushort ComputeCityPlunder(short townIdx);

		/// <summary>
		/// Lets participantIdx1 take a technology known by participantIdx2 that participantIdx1 does not know yet: the human player
		/// chooses from a dialog (king dialog when 0x3934 != -1), an AI takes the technology with the highest ObtainTechnologyInfo
		/// value (showing a notice when taken from the human player)
		/// </summary>
		void ChooseTechnologyToTake(short participantIdx1, short participantIdx2);

		/// <summary>
		/// Appends the name of the city (13 characters of GameState.TownLabels) to the text at TextBuffer.Address, "NONE" for city -1
		/// </summary>
		void ObtainCityLabel(short townIdx);

		/// <summary>
		/// Waits for a key or a mouse click; a key is taken (with the second code of an extended key)
		/// </summary>
		short AwaitForKeyPressOrMouseClick();

		/// <summary>
		/// A caravan enters a city: the revenue ((distance + 10) * (trade of both cities) / 24, halved on the same continent and
		/// for an own city, less a third each with the technologies 0x31 and 0x26 of the city's owner) goes to the gold and the
		/// science of the player (a message for the human player), the caravan is removed and the city becomes a trade route of
		/// its home city, in place of the route with the least trade (an empty slot first), unless that one has more.
		/// </summary>
		/// <remarks>The continent of the home city is looked up at (x, x), like in the original.</remarks>
		/// <returns>1, also in AX</returns>
		ushort FoundTradeRoute(short participantIdx, short troopIdx, short townIdx);
	}
}
