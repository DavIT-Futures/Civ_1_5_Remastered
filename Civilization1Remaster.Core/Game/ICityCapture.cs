namespace Civilization1Remaster.Core.Game
{
	public interface ICityCapture
	{
		void SeizeCity(short participantIdx, short townIdx, ushort marker);

		void DestroySpaceship(short participantIdx);

		ushort ComputeCityPlunder(short townIdx);

		void ChooseTechnologyToTake(short participantIdx1, short participantIdx2);

		void ObtainCityLabel(short townIdx);

		short WaitForKeyPressOrMouseClick();

		ushort FoundTradeRoute(short participantIdx, short troopIdx, short townIdx);
	}
}
