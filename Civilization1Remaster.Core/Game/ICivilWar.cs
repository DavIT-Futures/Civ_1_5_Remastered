namespace Civilization1Remaster.Core.Game
{
	public interface ICivilWar
	{
		ushort DivideCivilization(short participantIdx);

		void AssignCityOwner(short townIdx, short participantIdx);

		ushort ComputeDistanceToCapital(short participantIdx);
	}
}
