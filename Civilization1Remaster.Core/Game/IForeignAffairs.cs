namespace Civilization1Remaster.Core.Game
{
	public interface IForeignAffairs
	{
		void HandlePeaceTreaties(short participantIdx);

		void SwitchGovernment(short participantIdx, short regimeKind);

		void BargainPeaceTreaty(short participantIdx1, short participantIdx2, int horizLoc, int vertLoc);

		void AssignDiplomacyFlags(short participantIdx1, short participantIdx2, ushort markers);

		void ClearDiplomacyFlags(short participantIdx1, short participantIdx2, ushort markers);

		ushort AIWeighsTreaty(short participantIdx1, short participantIdx2);

		void SwapTechnology(short participantIdx1, short participantIdx2);

		ushort FindNearestCityOfPlayer(short participantIdx, int horizLoc, int vertLoc);
	}
}
