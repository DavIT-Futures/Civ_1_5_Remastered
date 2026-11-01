namespace Civ_1_5_Remastered.Core.Shared.Services
{
	public interface ICivilWar
	{
		ushort DivideCivilization(short participantIndex);

		void AssignCityOwner(short townIndex, short participantIndex);

		ushort ComputeDistanceToCapital(short participantIndex);
	}
}
