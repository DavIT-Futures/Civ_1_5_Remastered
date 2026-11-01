namespace Civ_1_5_Remastered.Core.Shared.Services
{
	public interface IForeignAffairs
	{
		void HandlePeaceTreaties(short participantIndex);

		void SwitchGovernment(short participantIndex, short regimeKind);

		void BargainPeaceTreaty(short firstParticipantIndex, short secondParticipantIndex, int horizontalLocation, int verticalLocation);

		void AssignDiplomacyFlags(short firstParticipantIndex, short secondParticipantIndex, ushort markers);

		void ClearDiplomacyFlags(short firstParticipantIndex, short secondParticipantIndex, ushort markers);

		ushort ComputerWeighsTreaty(short firstParticipantIndex, short secondParticipantIndex);

		void SwapTechnology(short firstParticipantIndex, short secondParticipantIndex);

		ushort FindNearestCityOfPlayer(short participantIndex, int horizontalLocation, int verticalLocation);
	}
}
