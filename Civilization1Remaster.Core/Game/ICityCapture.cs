namespace Civilization1Remaster.Core.Game
{
	public interface ICityCapture
	{
		void SeizeCity(short participantIndex, short townIndex, ushort marker);

		void DestroySpaceship(short participantIndex);

		ushort ComputeCityPlunder(short townIndex);

		void ChooseTechnologyToTake(short firstParticipantIndex, short secondParticipantIndex);

		void ObtainCityLabel(short townIndex);

		short WaitForKeyPressOrMouseClick();

		ushort FoundTradeRoute(short participantIndex, short troopIndex, short townIndex);
	}
}
