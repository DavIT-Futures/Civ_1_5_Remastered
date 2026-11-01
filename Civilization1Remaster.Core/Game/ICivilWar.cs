namespace Civilization1Remaster.Core.Game
{
	public interface ICivilWar
	{
		ushort DivideCivilization(short participantIndex);

		void AssignCityOwner(short townIndex, short participantIndex);

		ushort ComputeDistanceToCapital(short participantIndex);
	}
}
