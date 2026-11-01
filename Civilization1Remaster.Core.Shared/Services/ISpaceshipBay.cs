namespace Civilization1Remaster.Core.Shared.Services
{
	public interface ISpaceshipBay
	{
		void DrawSpaceScenery();

		void LoadSpaceScenery();

		ushort DrawSpaceship(short participantIndex, short frames, short launchable);

		ushort ObtainSpaceshipPartValue(short participantIndex, ushort row, ushort column);

		void DrawStarField();

		ushort ComputeSpaceshipSuccess(short participantIndex, short partKind);

		void AttachSpaceshipPart(short participantIndex, ushort partKind);

		void ChooseNationToView();

		void FireSpaceship(short participantIndex);
	}
}
