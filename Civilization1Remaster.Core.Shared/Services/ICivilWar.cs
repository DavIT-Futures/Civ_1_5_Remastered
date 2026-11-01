namespace Civilization1Remaster.Core.Shared.Services
{
	public interface ICivilWar
	{
		ushort DivideCivilization(short participantIndex);

		void AssignCityOwner(short townIndex, short participantIndex);

		ushort ComputeDistanceToCapital(short participantIndex);
	}
}
